using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Konsarpoo.Collections;

internal class DataFileSerialization : IDataSerializationInfo, IDisposable
{
    private readonly string m_filePath;
    private FileStream m_fileStream;
    private BinaryWriter m_writer;
    private BinaryReader m_reader;
    private int m_arrayCount;
    private int m_dataCount;
    private int m_version;
    private int m_maxSizeOfArray;
    private bool m_disposed;
    protected static readonly long m_metaSize = sizeof(int) * 4; // maxSizeOfArray, dataCount, version, arraysCount
    private long[] m_offsetTable;


    public DataFileSerialization(string filePath)
    {
        m_filePath = filePath;
        m_fileStream = new FileStream(m_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        m_writer = new BinaryWriter(m_fileStream);
        m_reader = new BinaryReader(m_fileStream);
        
        var metaData = ReadMetadata();
        
        m_arrayCount = metaData.arraysCapacity;
        m_version = metaData.version;
        m_dataCount = metaData.dataCount;
        m_maxSizeOfArray = metaData.maxSizeOfArray;
        m_version = metaData.version;
        
        if (m_arrayCount > 0)
        {
            m_offsetTable = ReadOffsetTableInfo(m_arrayCount);
        }
        else
        {
            m_offsetTable = new long[0];
        }
    }

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

    protected bool CanFlush => m_edit == 0;

    public void SetMetadata((int maxSizeOfArray, int dataCount, int version, int arraysCapacity) metaData)
    {
        m_fileStream.Seek(0, SeekOrigin.Begin);
        
        m_writer.Write(metaData.maxSizeOfArray);
        m_writer.Write(metaData.dataCount);
        m_writer.Write(metaData.version);
        m_writer.Write(metaData.arraysCapacity);

        SetCapacityCore(metaData.arraysCapacity);

        if (CanFlush)
        {
            m_writer.Flush();
        }
    }

    public void SetCapacity(int capacity)
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

    private long[] ReadOffsetTableInfo(int arrayCount)
    {
        m_fileStream.Seek(m_metaSize, SeekOrigin.Begin);
        
        long[] offsetTable = new long[arrayCount];

        for (int i = 0; i < offsetTable.Length; i++)
        {
            offsetTable[i] = m_reader.ReadInt64();
        }
        
        return offsetTable;
    }

    public (int maxSizeOfArray, int dataCount, int version, int arraysCapacity) ReadMetadata()
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

    void IDataSerializationInfo.WriteMetadata((int maxSizeOfArray, int dataCount, int version, int arraysCapacity) metaData)
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

    public void WriteSingleArray<T>(T[] array)
    {
        AppendArray(array);
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

    private byte[] GetBytes<T>(T[] array)
    {
        using var ms = new MemoryStream();
        new BinaryFormatter().Serialize(ms, array);
        byte[] data = ms.ToArray();
        return data;
    }

    private T[] ReadBytes<T>(byte[] data)
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
        return m_metaSize + Marshal.SizeOf<long>() * offsetTableLenInFile;
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