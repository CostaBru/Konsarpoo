using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using JetBrains.Annotations;

namespace Konsarpoo.Collections.Data.Serialization;

/// <summary>
/// Serialization of arrays of data into a stream with support for compression/encryption pipelines.
/// </summary>
[DebuggerDisplay("DataCount = {DataCount}, ArrayCount = {ArrayCount}, MaxSizeOfArray = {m_maxSizeOfArray}, Version = {m_version}, CanFlush = {CanFlush}")]
public class DataStreamSerialization : IDataSerializationInfo, IDisposable
{
    private Stream m_fileStream;
    private BinaryWriter m_writer;
    private BinaryReader m_reader;
    private int m_arrayCount;
    private int m_dataCount;
    private int m_version;
    private int m_maxSizeOfArray;
    private readonly Func<DataStreamSerialization, byte[], byte[]> m_writeProcessPipeline;
    private readonly Func<DataStreamSerialization, byte[], byte[]> m_readProcessPipeline;
    private bool m_disposed;
    protected static readonly long m_metaSize = sizeof(int) * 5; // maxSizeOfArray, dataCount, version, arraysCount, extra metadata size
    private long[] m_offsetTable;
    private byte[] m_extraMetaData = Array.Empty<byte>();

    private long GetExtraMetadataSize => m_extraMetaData.Length;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStreamSerialization"/> class.
    /// </summary>
    /// <param name="fileStream"></param>
    /// <param name="maxSizeOfArray"></param>
    /// <param name="writeProcessPipeline"></param>
    /// <param name="readProcessPipeline"></param>
    /// <param name="arrayCapacity"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public DataStreamSerialization([NotNull] Stream fileStream,
        int maxSizeOfArray,
        [NotNull] Func<DataStreamSerialization, byte[], byte[]> writeProcessPipeline,
        [NotNull] Func<DataStreamSerialization, byte[], byte[]> readProcessPipeline, 
        int arrayCapacity = 0)
    {
        m_fileStream = fileStream ?? throw new ArgumentNullException(nameof(fileStream));
        m_writer = new BinaryWriter(m_fileStream);
        m_reader = new BinaryReader(m_fileStream);

        m_maxSizeOfArray = maxSizeOfArray;
        
        m_writeProcessPipeline = writeProcessPipeline ?? throw new ArgumentNullException(nameof(writeProcessPipeline));
        m_readProcessPipeline = readProcessPipeline ?? throw new ArgumentNullException(nameof(readProcessPipeline));

        m_offsetTable = new long[arrayCapacity];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStreamSerialization"/> class.
    /// </summary>
    /// <param name="fileStream"></param>
    /// <param name="writeProcessPipeline"></param>
    /// <param name="readProcessPipeline"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public DataStreamSerialization([NotNull] Stream fileStream,
        [NotNull] Func<DataStreamSerialization, byte[], byte[]> writeProcessPipeline,
        [NotNull] Func<DataStreamSerialization, byte[], byte[]> readProcessPipeline)
    {
        m_fileStream = fileStream ?? throw new ArgumentNullException(nameof(fileStream));
        m_writer = new BinaryWriter(m_fileStream);
        m_reader = new BinaryReader(m_fileStream);
        
        m_writeProcessPipeline = writeProcessPipeline ?? throw new ArgumentNullException(nameof(writeProcessPipeline));
        m_readProcessPipeline = readProcessPipeline ?? throw new ArgumentNullException(nameof(readProcessPipeline));
        
        var metaData = ReadMetaDataCore();
        
      
        m_version = metaData.version;
        m_dataCount = metaData.dataCount;
        m_maxSizeOfArray = metaData.maxSizeOfArray;
        m_version = metaData.version;
        m_arrayCount = metaData.dataCount;
        m_extraMetaData = metaData.extaMetaData;

        if (m_arrayCount > 0)
        {
            m_offsetTable = ReadOffsetTableInfo();
        }
        else
        {
            m_offsetTable = new long[0];
        }
    }

    /// <summary>
    /// Maximum size of a single array.
    /// </summary>
    public int MaxSizeOfArray => m_maxSizeOfArray;
    /// <summary>
    /// Count of data items across all arrays.
    /// </summary>
    public int DataCount => m_dataCount;
    /// <summary>
    /// Age of the data.
    /// </summary>
    public int Version => m_version;

    private int m_edit = 0;
    
    /// <summary>
    /// Begin a write operation. Call EndWrite when done.
    /// </summary>
    public void BeginWrite()
    {
        m_edit++;
    }

    /// <summary>
    /// End a write operation. Flushes if no more active write operations.
    /// </summary>
    public void EndWrite()
    {
        m_edit--;

        if (m_edit <= 0)
        {
            m_edit = 0;
            m_writer.Flush();
        }
    }

    /// <summary>
    /// Flushes any pending writes to the underlying stream if not in a write operation.
    /// </summary>
    public void Flush()
    {
        if (CanFlush)
        {
            m_writer.Flush();
        }
    }

    /// <summary>
    /// True if not in a write operation and can flush.
    /// </summary>
    public bool CanFlush => m_edit == 0;
    
    /// <summary>
    /// Sets extra metadata payload.
    /// </summary>
    /// <param name="metaDataBytes"></param>
    public void UpdateExtraMetadata(byte[] metaDataBytes)
    {
        var newOffset = m_metaSize + metaDataBytes.Length;

        var copySize = m_fileStream.Length - m_metaSize;

        var chunks = CopyMemory(m_metaSize, copySize);

        WriteMemory(newOffset, chunks);

        m_fileStream.Seek(m_metaSize - sizeof(int), SeekOrigin.Begin);
        m_writer.Write(metaDataBytes.Length);
        m_writer.Write(metaDataBytes);

        if (CanFlush)
        {
            m_writer.Flush();
        }
        
        m_extraMetaData = metaDataBytes;
    }

    public byte[] ExtraMetadata => m_extraMetaData;
    
    /// <summary>
    /// Reads extra metadata payload.
    /// </summary>
    public byte[] ReadExtraMetadata()
    {
        m_fileStream.Seek(m_metaSize - sizeof(int), SeekOrigin.Begin);
        var metaSize = m_reader.ReadInt32();

        var extraMeta = new byte[metaSize];
        
        for (int i = 0; i < metaSize; i++)
        {
            extraMeta[i] = m_reader.ReadByte();
        }

        return extraMeta;
    }

    /// <summary>
    /// Sets the metadata at the start of the stream.
    /// </summary>
    /// <param name="metaData"></param>
    public void WriteMetadata((int maxSizeOfArray, int dataCount, int version) metaData) 
    {
        m_fileStream.Seek(0, SeekOrigin.Begin);

        m_writer.Write(metaData.maxSizeOfArray);
        m_writer.Write(metaData.dataCount);
        m_writer.Write(metaData.version);
        m_writer.Write(m_arrayCount);
        m_writer.Write(m_extraMetaData.Length);

        if (CanFlush)
        {
            m_writer.Flush();
        }
    }

    /// <summary>
    /// Sets the capacity of the array offset table. Existing data will be moved if necessary.
    /// </summary>
    /// <param name="capacity"></param>
    public void SetArrayCapacity(int capacity)
    {
        SetCapacityCore(capacity);
    }

    private void SetCapacityCore(int capacity)
    {
        var offsetTableBeforeResize = m_offsetTable.Length;

        if (capacity > offsetTableBeforeResize)
        {
            Array.Resize(ref m_offsetTable, capacity);
            
            if (m_arrayCount > 0)
            {
                var newOffset = GetArrayOffset(0, m_offsetTable.Length);

                MoveData(offsetTableBeforeResize, newOffset);
            }
        
            WriteOffsetTableInfo(m_offsetTable);
        }
        else
        {
            throw new NotSupportedException("Trimming is not supported");
        }
    }

    private long[] ReadOffsetTableInfo()
    {
        m_fileStream.Seek(m_metaSize + GetExtraMetadataSize, SeekOrigin.Begin);

        var offsetTableCapacity = m_reader.ReadInt32();

        long[] offsetTable = new long[offsetTableCapacity];

        for (int i = 0; i < offsetTable.Length; i++)
        {
            offsetTable[i] = m_reader.ReadInt64();
        }
        
        return offsetTable;
    }


    public void WriteMetadata()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Reads the metadata from the start of the stream.
    /// </summary>
    /// <returns></returns>
    public void ReadMetadata()
    {
        var metadata = ReadMetaDataCore();
        
        m_maxSizeOfArray =  metadata.maxSizeOfArray;
        m_dataCount = metadata.dataCount;
        m_version = metadata.version;
    }

    public (int maxSizeOfArray, int dataCount, int version) MetaData => (m_maxSizeOfArray, m_dataCount, m_version);

    private (int maxSizeOfArray, int dataCount, int version, int count, byte[] extaMetaData) ReadMetaDataCore()
    {
        if (m_fileStream.Length == 0)
        {
            return (0, 0, 0, 0, Array.Empty<byte>());
        }
        
        m_fileStream.Seek(0, SeekOrigin.Begin);

        if (m_fileStream.Length < m_metaSize)
        {
            throw new InvalidOperationException("File stream does not have metadata written.");
        }
        
        int maxSize = m_reader.ReadInt32();
        int count = m_reader.ReadInt32();
        int version = m_reader.ReadInt32();
        int arrayCount = m_reader.ReadInt32(); 
        var metaSize = m_reader.ReadInt32();
        
        var extraMeta = new byte[metaSize];
        
        for (int i = 0; i < metaSize; i++)
        {
            extraMeta[i] = m_reader.ReadByte();
        }
        
        return (maxSize, count, version, arrayCount, extraMeta);
    }

    private void WriteArrayCapacity(int count)
    {
        m_fileStream.Seek(sizeof(int) * 3, SeekOrigin.Begin);
        m_writer.Write(count);
    }

    void IDataSerializationInfo.UpdateMetadata((int maxSizeOfArray, int dataCount, int version) metaData)
    {
        WriteMetadata(metaData);
    }

    /// <summary>
    /// Appends a new array to the stream.
    /// </summary>
    /// <param name="array"></param>
    /// <typeparam name="T"></typeparam>
    public void AppendArray<T>(T[] array)
    {
        if (m_arrayCount == m_offsetTable.Length)
        {
            var offsetTableBeforeResize = m_offsetTable.Length;

            EnsureOffsetTableCapacity(Math.Max(offsetTableBeforeResize * 2, 2));
            
            if (offsetTableBeforeResize > 0)
            {
                var newOffset = GetArrayOffset(0, m_offsetTable.Length);
                
                MoveData(offsetTableBeforeResize, newOffset);
            }
        }

        var arrayIndex = m_arrayCount;
        
        var written = WriteArrayToStream(arrayIndex, array);
        
        m_arrayCount++;

        WriteArrayCapacity(m_arrayCount);
        UpdateArrayOffset(arrayIndex, written.offset, written.byteSize);
        WriteOffsetTableInfo(m_offsetTable);
        
        if (CanFlush)
        {
            m_writer.Flush();
        }
    }

    private void MoveData(int offsetTableBeforeResize, long newOffset)
    {
        var dataOffset = GetArrayOffset(0, offsetTableBeforeResize);

        var copySize = m_fileStream.Length - dataOffset;

        var chunks = CopyMemory(dataOffset, copySize);

        WriteMemory(newOffset, chunks);
    }

    /// <summary>
    /// Writes an array at the specified index, shifting data if necessary.
    /// </summary>
    /// <param name="arrayIndex"></param>
    /// <param name="array"></param>
    /// <typeparam name="T"></typeparam>
    public void WriteArray<T>(int arrayIndex, T[] array)
    {
        WriteArrayAt(arrayIndex, array);
    }

    public int ArrayCount => m_arrayCount;

    /// <summary>
    /// Appends a single array to the stream.
    /// </summary>
    /// <param name="array"></param>
    /// <typeparam name="T"></typeparam>
    public void WriteSingleArray<T>(T[] array)
    {
        AppendArray(array);
    }

    /// <summary>
    /// Clears all data, resetting to an empty state.
    /// </summary>
    public void Clear()
    {
        m_arrayCount = 0;
        m_dataCount = 0;
        m_offsetTable = new long[0];
        // Truncate to metadata only
        m_fileStream.SetLength(m_metaSize);
        m_extraMetaData = Array.Empty<byte>();
        WriteMetadata((m_maxSizeOfArray, 0, m_version));
        if (CanFlush)
        {
            m_writer.Flush();
        }
    }

    /// <summary>
    /// Reads a single array from the stream.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T[] ReadSingleArray<T>()
    {
        return ReadArray<T>(0);
    }

    /// <summary>
    /// Reads the array at the specified index from the stream.
    /// </summary>
    /// <param name="arrayIndex"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T[] ReadArray<T>(int arrayIndex)
    {
        var arrayOffset = GetArrayOffset(arrayIndex, m_offsetTable.Length);
        
        m_fileStream.Seek(arrayOffset, SeekOrigin.Begin);
        
        return ReadArrayData<T>();
    }
    
    private void EnsureOffsetTableCapacity(int requiredLength)
    {
        if (m_offsetTable.Length < requiredLength)
        {
            Array.Resize(ref m_offsetTable, requiredLength);
        }
    }

    private void WriteArrayAt<T>(int arrayIndex, T[] array)
    {
        if (arrayIndex < m_arrayCount - 1)
        {
            long nextArrayOffset = GetArrayOffset(arrayIndex + 1, m_offsetTable.Length);
            long curArrayOffset = GetArrayOffset(arrayIndex, m_offsetTable.Length);

            var copySize = m_fileStream.Length - nextArrayOffset;

            var chunks = CopyMemory(nextArrayOffset, copySize);

            (int bytesWritten, long offset) = WriteArrayToStream(arrayIndex, array);

            WriteMemory(offset + bytesWritten, chunks);

            var oldSize = nextArrayOffset - curArrayOffset;

            UpdateArrayOffset(arrayIndex, offset, bytesWritten);
        
            var deltaToApply = bytesWritten - oldSize;

            UpdateOffsetTable(arrayIndex, deltaToApply);
        }
        else
        {
            var written = WriteArrayToStream(arrayIndex, array);
            
            UpdateArrayOffset(arrayIndex, written.offset, written.byteSize);
        }

        WriteOffsetTableInfo(m_offsetTable);
        
        if (CanFlush)
        {
            m_writer.Flush();
        }
    }

    private (int byteSize, long offset) WriteArrayToStream<T>(int i, T[] array)
    {
        long offset = GetArrayOffset(i, m_offsetTable.Length);
        
        var bytes = GetBytes(array);

        var bytesSize = bytes.Length + Marshal.SizeOf<int>();

        m_fileStream.Seek(offset, SeekOrigin.Begin);
        
        m_writer.Write(bytes.Length);
        m_writer.Write(bytes);

        return (bytesSize, offset);
    }
    
    private List<(byte[] chunk, int size)> CopyMemory(long offset, long copySize)
    {
        var chunks = new List<(byte[] chunk, int size)>();

        var rest = copySize;

        m_fileStream.Seek(offset, SeekOrigin.Begin);

        var len = m_fileStream.Length - offset;

        while (rest > 0)
        {
            var chunkCopySize = (int)Math.Min(rest, len);
            
            byte[] chunk = m_reader.ReadBytes(chunkCopySize);

            chunks.Add((chunk, chunkCopySize));

            rest -= chunk.Length;
        }
        return chunks;
    }
    
    private void WriteMemory(long offset, List<(byte[] chunk, int size)> chunks)
    {
        m_fileStream.Seek(offset, SeekOrigin.Begin);
            
        foreach (var chunk in chunks)
        {
            m_writer.Write(chunk.chunk);
        }
    }

    protected byte[] GetBytes<T>(T[] array)
    {
        using var ms = new MemoryStream();
        new BinaryFormatter().Serialize(ms, array);
        byte[] data = ms.ToArray();

        if (m_writeProcessPipeline != null)
        {
            return m_writeProcessPipeline(this, data);
        }
        
        return data;
    }

    protected T[] ReadBytes<T>(byte[] data)
    {
        var bytes = data;

        if (m_readProcessPipeline != null)
        {
            bytes = m_readProcessPipeline(this, data);
        }
        
        using var ms = new MemoryStream(bytes, 0, bytes.Length);
        return (T[])new BinaryFormatter().Deserialize(ms);
    }
    
    protected long GetArrayOffset(int arrayIndex, int arrayCount)
    {
        if (arrayCount == 0 || arrayIndex == 0)
        {
            return GetBaseOffset(arrayCount);
        }

        return GetBaseOffset(arrayCount) + m_offsetTable[arrayIndex - 1];
    }
    
    private long GetBaseOffset(int offsetTableLenInFile)
    {
        return m_metaSize + GetExtraMetadataSize + sizeof(int) + sizeof(long) * offsetTableLenInFile;
    }
    
    private void UpdateArrayOffset(int arrayIndex, long offset, long bytesWritten)
    {
        var baseOffset = GetBaseOffset(m_offsetTable.Length);
        var relative = offset - baseOffset;
        
        m_offsetTable[arrayIndex] = relative + bytesWritten;
    }
    
    private void UpdateOffsetTable(int arrayIndex, long delta)
    {
        for (int i = arrayIndex + 1; i < m_arrayCount; i++)
        {
            m_offsetTable[i] += delta;
        }
    }
    
    private void WriteOffsetTableInfo(long[] offsetTable)
    {
        m_fileStream.Seek(m_metaSize + GetExtraMetadataSize, SeekOrigin.Begin);
        m_writer.Write(offsetTable.Length);
        
        for (var index = 0; index < offsetTable.Length; index++)
        {
            var val = offsetTable[index];
            m_writer.Write(val);
        }
    }

    private T[] ReadArrayData<T>()
    {
        int length = m_reader.ReadInt32();
        byte[] bytes = m_reader.ReadBytes(length);
        return ReadBytes<T>(bytes);
    }

    /// <summary>
    /// Disposes the instance and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (m_disposed)
        {
            return;
        }
        m_writer?.Dispose();
        m_reader?.Dispose();
        m_fileStream?.Dispose();
        m_disposed = true;
    }
}