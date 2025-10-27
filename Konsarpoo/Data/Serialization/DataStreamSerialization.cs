using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using JetBrains.Annotations;

namespace Konsarpoo.Collections.Data.Serialization;

/// <summary>
/// Serialization of arrays of data into a stream with support for compression/encryption pipelines. Not thread safe.
/// </summary>
[DebuggerDisplay("DataCount = {DataCount}, ArrayCount = {ArrayCount}, MaxSizeOfArray = {m_maxSizeOfArray}, Version = {m_version}, CanFlush = {CanFlush}")]
public partial class DataStreamSerialization : IDataSerializationInfo, IDisposable
{
    protected static readonly long m_metaSize = sizeof(int) * 5; // maxSizeOfArray, dataCount, version, arraysCount, extra metadata size
    
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
    private long[] m_offsetTable;
    private byte[] m_extraMetaData = Array.Empty<byte>();
    private int m_edit = 0;

    private long GetExtraMetadataSize => m_extraMetaData.Length;
    
    /// <summary>
    /// The creation a new stream constructor. Initializes a new instance of the <see cref="DataStreamSerialization"/> class. Will override existing contents of the stream.
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

        m_maxSizeOfArray = maxSizeOfArray.PowerOfTwo();
        
        m_writeProcessPipeline = writeProcessPipeline ?? throw new ArgumentNullException(nameof(writeProcessPipeline));
        m_readProcessPipeline = readProcessPipeline ?? throw new ArgumentNullException(nameof(readProcessPipeline));

        m_offsetTable = new long[arrayCapacity];
    }

    /// <summary>
    /// The opener an existing stream constructor. Initializes a new instance of the <see cref="DataStreamSerialization"/> class with contents loaded from the stream.
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
        m_maxSizeOfArray = metaData.maxSizeOfArray.PowerOfTwo();
        m_version = metaData.version;
        m_arrayCount = metaData.arrrayCount;
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
    /// Maximum size of a single array. Power of 2.
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
    public void SetExtraMetadata(byte[] metaDataBytes)
    {
        if (m_fileStream.Length > 0)
        {
            var newOffset = m_metaSize + metaDataBytes.Length;

            var copySize = m_fileStream.Length - m_metaSize;

            var chunks = CopyMemory(m_metaSize, copySize);

            WriteMemory(newOffset, chunks);
        }

        m_extraMetaData = metaDataBytes;
        
        WriteMetadataCore();

        if (CanFlush)
        {
            m_writer.Flush();
        }
    }

    /// <summary>
    /// Gets an extra metadata saved to the serialization stream.
    /// </summary>
    public byte[] ExtraMetadata => m_extraMetaData;

    /// <summary>
    /// Write the metadata at the start of the stream.
    /// </summary>
    public void WriteMetadata()
    {
        WriteMetadataCore();

        if (CanFlush)
        {
            m_writer.Flush();
        }
    }

    private void WriteMetadataCore()
    {
        m_fileStream.Seek(0, SeekOrigin.Begin);

        m_writer.Write(m_maxSizeOfArray);
        m_writer.Write(m_dataCount);
        m_writer.Write(m_version);
        m_writer.Write(m_arrayCount);
        m_writer.Write(m_extraMetaData.Length);
        m_writer.Write(m_extraMetaData);
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
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        
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
            if (m_arrayCount > 0)
            {
                throw new NotSupportedException("Trimming is not supported");
            }

            Array.Resize(ref m_offsetTable, capacity);
            WriteOffsetTableInfo(m_offsetTable);
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

    /// <summary>
    /// Updates the metadata values.
    /// </summary>
    /// <param name="metaData"></param>
    public void UpdateMetadata((int maxSizeOfArray, int dataCount, int version) metaData)
    {
        m_maxSizeOfArray = metaData.maxSizeOfArray.PowerOfTwo();
        m_dataCount = metaData.dataCount;
        m_version = metaData.version;
    }

    /// <summary>
    /// Reads the metadata from the start of the stream.
    /// </summary>
    /// <returns></returns>
    public void ReadMetadata()
    {
        var metadata = ReadMetaDataCore();
        
        m_maxSizeOfArray =  metadata.maxSizeOfArray.PowerOfTwo();
        m_dataCount = metadata.dataCount;
        m_version = metadata.version;
    }

    /// <summary>
    /// Gets the metadata tuple (maxSizeOfArray, dataCount, version).
    /// </summary>
    public (int maxSizeOfArray, int dataCount, int version) MetaData => (m_maxSizeOfArray, m_dataCount, m_version);

    private (int maxSizeOfArray, int dataCount, int version, int arrrayCount, byte[] extaMetaData) ReadMetaDataCore()
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

    /// <summary>
    /// Gets the count of arrays in the stream.
    /// </summary>
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
    /// Clears all data, capacity and extra metadata, resetting to an empty state.
    /// </summary>
    public void Clear()
    {
        m_arrayCount = 0;
        m_dataCount = 0;
        m_extraMetaData = Array.Empty<byte>();
        m_offsetTable = new long[0];
        // Truncate to metadata only
        m_fileStream.SetLength(m_metaSize);
        WriteMetadataCore();
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
        
        if(arrayOffset >= m_fileStream.Length)
        {
            throw new IndexOutOfRangeException("Array index is out of range of the stored arrays.");
        }
        
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
        
        var bytes = WriteArray(array);

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

    private byte[] WriteArray<T>(T[] array)
    {
        var data = SerializeArrayToBytes(array);

        if (m_writeProcessPipeline != null)
        {
            return m_writeProcessPipeline(this, data);
        }
        
        return data;
    }
    
    private T[] ReadArray<T>(byte[] data)
    {
        var bytes = data;

        if (m_readProcessPipeline != null)
        {
            bytes = m_readProcessPipeline(this, data);
        }
        
        return DeserializeArrayFromBytes<T>(bytes);
    }

    /// <summary>
    /// Serializes an array to a byte array using BinaryFormatter.
    /// </summary>
    /// <param name="array"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected virtual byte[] SerializeArrayToBytes<T>(T[] array)
    {
        using var ms = new MemoryStream();
        new BinaryFormatter().Serialize(ms, array);
        byte[] data = ms.ToArray();
        return data;
    }
    
    /// <summary>
    /// Deserializes an array from a byte array using BinaryFormatter.
    /// </summary>
    /// <param name="bytes"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected virtual T[] DeserializeArrayFromBytes<T>(byte[] bytes)
    {
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
        return ReadArray<T>(bytes);
    }

    /// <summary>
    /// Removes the last array from the stream. If it is empty it clear content and capacity to an empty state.
    /// </summary>
    public void RemoveLast()
    {
        if (m_arrayCount <= 0)
        {
            return;
        }
        
        m_arrayCount--;
        
        long offset = GetArrayOffset(m_arrayCount, m_offsetTable.Length);
        
        WriteArrayCapacity(m_arrayCount);
        m_offsetTable[m_arrayCount] = 0;
        WriteOffsetTableInfo(m_offsetTable);
        
        m_fileStream.SetLength(offset);

        if (m_arrayCount == 0)
        {
            Clear();
        }
        else
        {
            if (CanFlush)
            {
                m_writer.Flush();
            } 
        }
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

        DisposeCore();
     
        m_disposed = true;
    }

    /// <summary>
    /// Core dispose logic.
    /// </summary>
    protected virtual void DisposeCore()
    {
        m_writer?.Dispose();
        m_reader?.Dispose();
        m_fileStream?.Dispose();
    }
}
