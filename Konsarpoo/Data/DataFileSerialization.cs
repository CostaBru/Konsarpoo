using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Konsarpoo.Collections;

internal interface IDataArrayFileAccessor
{
    T[] ReadArray<T>(int arrayIndex);
    void WriteArray<T>(int arrayIndex, T[] array);
    void AppendArray<T>(T[] array);
    void RemoveArray(int arrayIndex);
    void Clear();
    int ArrayCount { get; }
}

[DebuggerDisplay("FilePath = {m_filePath}, ArrayCount = {ArrayCount}, MaxSizeOfArray = {MaxSizeOfArray}, DataCount = {DataCount}, Version = {Version}, CanFlush = {CanFlush}")]
internal class DataFileSerialization : DataStreamSerialization
{
    private readonly string m_filePath;
    
    public DataFileSerialization(string filePath, FileMode fileMode, int maxSizeOfArray, int arrayCapacity = 0) 
        : base(new FileStream(filePath, fileMode, FileAccess.ReadWrite, FileShare.None), maxSizeOfArray, arrayCapacity)
    {
        m_filePath = filePath;
    }

    public DataFileSerialization(string filePath, FileMode fileMode) : base(new FileStream(filePath, fileMode, FileAccess.ReadWrite, FileShare.None))
    {
        m_filePath = filePath;
    }
}

[DebuggerDisplay("DataCount = {DataCount}, ArrayCount = {ArrayCount}, MaxSizeOfArray = {m_maxSizeOfArray}, Version = {m_version}, CanFlush = {CanFlush}")]
internal class DataStreamSerialization : IDataSerializationInfo, IDataArrayFileAccessor, IDisposable
{
    private Stream m_fileStream;
    private BinaryWriter m_writer;
    private BinaryReader m_reader;
    private int m_arrayCount;
    private int m_dataCount;
    private int m_version;
    private int m_maxSizeOfArray;
    private bool m_disposed;
    protected static readonly long m_metaSize = sizeof(int) * 4; // maxSizeOfArray, dataCount, version, arraysCount
    private long[] m_offsetTable;

    public DataStreamSerialization(Stream fileStream, int maxSizeOfArray, int arrayCapacity = 0)
    {
        m_fileStream = fileStream;
        m_writer = new BinaryWriter(m_fileStream);
        m_reader = new BinaryReader(m_fileStream);

        m_maxSizeOfArray = maxSizeOfArray;

        m_offsetTable = new long[arrayCapacity];
    }

    public DataStreamSerialization(Stream fileStream)
    {
        m_fileStream = fileStream;
        m_writer = new BinaryWriter(m_fileStream);
        m_reader = new BinaryReader(m_fileStream);
        
        var metaData = ReadMetadata();
        
        m_arrayCount = metaData.arraysCount;
        m_version = metaData.version;
        m_dataCount = metaData.dataCount;
        m_maxSizeOfArray = metaData.maxSizeOfArray;
        m_version = metaData.version;
        
        if (m_arrayCount > 0)
        {
            m_offsetTable = ReadOffsetTableInfo();
        }
        else
        {
            m_offsetTable = new long[0];
        }
    }

    public int MaxSizeOfArray => m_maxSizeOfArray;
    public int DataCount => m_dataCount;
    public int Version => m_version;

    private int m_edit = 0;
    
    public void BeginWrite()
    {
        m_edit++;
    }

    public void EndWrite()
    {
        m_edit--;

        if (m_edit <= 0)
        {
            m_edit = 0;
            m_writer.Flush();
        }
    }

    public void Flush()
    {
        if (CanFlush)
        {
            m_writer.Flush();
        }
    }

    public bool CanFlush => m_edit == 0;

    public void SetMetadata((int maxSizeOfArray, int dataCount, int version, int arraysCount) metaData)
    {
        m_fileStream.Seek(0, SeekOrigin.Begin);
        
        m_writer.Write(metaData.maxSizeOfArray);
        m_writer.Write(metaData.dataCount);
        m_writer.Write(metaData.version);
        m_writer.Write(metaData.arraysCount);

        if (CanFlush)
        {
            m_writer.Flush();
        }
    }

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
        m_fileStream.Seek(m_metaSize, SeekOrigin.Begin);

        var offsetTableCapacity = m_reader.ReadInt32();

        long[] offsetTable = new long[offsetTableCapacity];

        for (int i = 0; i < offsetTable.Length; i++)
        {
            offsetTable[i] = m_reader.ReadInt64();
        }
        
        return offsetTable;
    }

    public (int maxSizeOfArray, int dataCount, int version, int arraysCount) ReadMetadata()
    {
        if (m_fileStream.Length == 0)
        {
            return (0, 0, 0, 0);
        }
        
        m_fileStream.Seek(0, SeekOrigin.Begin);
        
        int maxSize = m_reader.ReadInt32();
        int count = m_reader.ReadInt32();
        int version = m_reader.ReadInt32();
        int arraysCapacity = m_reader.ReadInt32();
        
        return (maxSize, count, version, arraysCapacity);
    }

    private void WriteArrayCapacity(int count)
    {
        m_fileStream.Seek(sizeof(int) * 3, SeekOrigin.Begin);
        m_writer.Write(count);
    }

    void IDataSerializationInfo.WriteMetadata((int maxSizeOfArray, int dataCount, int version, int arraysCount) metaData)
    {
        SetMetadata(metaData);
    }

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

    public void WriteArray<T>(int arrayIndex, T[] array)
    {
        WriteArrayAt(arrayIndex, array);
    }

    public int ArrayCount => m_arrayCount;

    public void WriteSingleArray<T>(T[] array)
    {
        AppendArray(array);
    }
    
    public void RemoveArray(int arrayIndex)
    {
        if (arrayIndex < 0 || arrayIndex >= m_arrayCount)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }
        
        // compute size of array being removed
        long baseOffset = GetBaseOffset(m_offsetTable.Length);
        long start = GetArrayOffset(arrayIndex, m_offsetTable.Length);
        long end = baseOffset + m_offsetTable[arrayIndex];
        long sizeToRemove = end - start;
        if (sizeToRemove < 0)
        {
            throw new InvalidOperationException("Corrupted offset table: negative size");
        }
        if (arrayIndex < m_arrayCount - 1)
        {
            // move trailing data up
            long trailingOffset = end;
            long copySize = m_fileStream.Length - trailingOffset;
            if (copySize > 0)
            {
                var chunks = CopyMemory(trailingOffset, copySize);
                WriteMemory(start, chunks);
            }
            // adjust cumulative end offsets for subsequent arrays
            for (int j = arrayIndex + 1; j < m_arrayCount; j++)
            {
                m_offsetTable[j] -= sizeToRemove;
            }
            // shift cumulative ends left
            for (int j = arrayIndex; j < m_arrayCount - 1; j++)
            {
                m_offsetTable[j] = m_offsetTable[j + 1];
            }
        }
        // zero out last (now unused) entry (keep capacity)
        if (m_arrayCount > 0)
        {
            m_offsetTable[m_arrayCount - 1] = 0;
        }
        m_arrayCount--;
        
        // truncate file
        if (m_arrayCount == 0)
        {
            // reset fully: metadata only
            m_offsetTable = new long[0];
            var newLen = m_metaSize; // only metadata
            SetMetadata((m_maxSizeOfArray, 0, m_version, 0));
            m_fileStream.SetLength(newLen);
            if (CanFlush)
            {
                m_writer.Flush();
            }
        }
        else
        {
            long newLen = m_fileStream.Length - sizeToRemove;
            
            WriteArrayCapacity(m_arrayCount);
            WriteOffsetTableInfo(m_offsetTable);
            m_fileStream.SetLength(newLen);
        
            if (CanFlush)
            {
                m_writer.Flush();
            }
        }
    }

    public void Clear()
    {
        m_arrayCount = 0;
        m_dataCount = 0;
        m_offsetTable = new long[0];
        // Truncate to metadata only
        m_fileStream.SetLength(m_metaSize);
        SetMetadata((m_maxSizeOfArray, 0, m_version, 0));
        if (CanFlush) m_writer.Flush();
    }

    public T[] ReadSingleArray<T>()
    {
        return ReadArray<T>(0);
    }

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
            var minChunkSize = (int)Math.Min(ushort.MaxValue + 1, rest);

            var chunkCopySize = (int)Math.Min(rest, len);
            
            byte[] chunk = m_reader.ReadBytes(minChunkSize);

            chunks.Add((chunk, chunkCopySize));

            rest -= chunkCopySize;
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

    protected virtual byte[] GetBytes<T>(T[] array)
    {
        using var ms = new MemoryStream();
        new BinaryFormatter().Serialize(ms, array);
        byte[] data = ms.ToArray();
        return data;
    }

    protected virtual T[] ReadBytes<T>(byte[] data)
    {
        using var ms = new MemoryStream(data, 0, data.Length);
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
        return m_metaSize + sizeof(int) + sizeof(long) * offsetTableLenInFile;
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
        m_fileStream.Seek(m_metaSize, SeekOrigin.Begin);
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