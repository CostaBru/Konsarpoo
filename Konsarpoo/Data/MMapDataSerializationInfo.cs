using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Konsarpoo.Collections;

public class MemoryMappedDataSerializationInfo : IDataSerializationInfo, IDisposable
{
    private readonly MemoryMappedFile m_mmf;
    private readonly int m_maxSizeOfArray;
    private long m_capacity;
    private int m_arraysCount;
    private long[] m_offsetTable;

    private static readonly long m_metaSize = sizeof(int) * 4; // maxSizeOfArray, dataCount, version, arraysCount

    private long GetMmapArrayOffset(int arrayIndex)
    {
        return m_metaSize + Marshal.SizeOf<long>() * m_offsetTable.Length + m_offsetTable[arrayIndex];
    }

    private void UpdateOffsetTable(int arrayIndex, long offset, long size)
    {
        var baseOffset = m_metaSize + Marshal.SizeOf<long>() * m_offsetTable.Length;
        var l = offset - baseOffset;
        m_offsetTable[arrayIndex] = l + size;
        WriteOffsetTableInfo(m_offsetTable);
    }

    public MemoryMappedDataSerializationInfo(string path, int maxSizeOfArray, int arraysCount, long estimatedSizeOfT)
    {
        m_maxSizeOfArray = maxSizeOfArray;

        m_arraysCount = arraysCount;
        m_capacity = arraysCount * maxSizeOfArray * estimatedSizeOfT + m_metaSize;
        m_offsetTable = new long[arraysCount];
      
        using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        m_mmf = MemoryMappedFile.CreateFromFile(fs, null, m_capacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
    }

    public static MemoryMappedDataSerializationInfo Open(string file, long estimatedSizeOfT)
    {
        return new MemoryMappedDataSerializationInfo(file, estimatedSizeOfT);
    }

    private MemoryMappedDataSerializationInfo(string path, long estimatedSizeOfT)
    {
        m_mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null);

        var readMetaData = ReadMetaData();

        m_maxSizeOfArray = readMetaData.maxSizeOfArray;
        m_arraysCount = readMetaData.arraysCount;
        m_capacity = m_arraysCount * m_maxSizeOfArray * estimatedSizeOfT + m_metaSize;
    }

    public static int EstimateSerializedSize<T>(T element)
    {
        using var ms = new MemoryStream();
#pragma warning disable SYSLIB0011
        new BinaryFormatter().Serialize(ms, element);
#pragma warning restore SYSLIB0011
        return (int)ms.Length; 
    }

    public void WriteMetaData((int maxSizeOfArray, int dataCount, int version, int arraysCount) metaData)
    {
        using var accessor = m_mmf.CreateViewAccessor(0, m_metaSize, MemoryMappedFileAccess.Write);
        accessor.Write(0, metaData.maxSizeOfArray);
        accessor.Write(sizeof(int), metaData.dataCount);
        accessor.Write(sizeof(int) * 2, metaData.version);
        accessor.Write(sizeof(int) * 3, metaData.arraysCount);
        
        accessor.Flush();
    }

    public (int maxSizeOfArray, int dataCount, int version, int arraysCount) ReadMetaData()
    {
        using var accessor = m_mmf.CreateViewAccessor(0, m_metaSize, MemoryMappedFileAccess.Read);
        int maxSize = accessor.ReadInt32(0);
        int count = accessor.ReadInt32(sizeof(int));
        int version = accessor.ReadInt32(sizeof(int) * 2);
        int arrayCount = accessor.ReadInt32(sizeof(int) * 3);

        m_offsetTable = ReadOffsetTableInfo(arrayCount);
        m_arraysCount = arrayCount;
        
        return (maxSize, count, version, arrayCount);
    }

    private long[] ReadOffsetTableInfo(int arrayCount)
    {
        var offsetTableByteSize = arrayCount * Marshal.SizeOf<long>();
        
        using var offsetAccessor = m_mmf.CreateViewAccessor(m_metaSize, offsetTableByteSize, MemoryMappedFileAccess.Read);
        
        byte[] data = new byte[offsetTableByteSize];
        
        offsetAccessor.ReadArray(0, data, 0, offsetTableByteSize);

        var offsetTable = new long[arrayCount];
        
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);
        for (var index = 0; index < offsetTable.Length; index++)
        {
            offsetTable[index] = br.ReadInt64();
        }
        
        return offsetTable;
    }
    
    private void WriteOffsetTableInfo(long[] offsetTable)
    {
        var offsetTableByteSize = offsetTable.Length * Marshal.SizeOf<long>();
        
        using var offsetAccessor = m_mmf.CreateViewAccessor(m_metaSize, offsetTableByteSize, MemoryMappedFileAccess.Write);
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        for (var index = 0; index < offsetTable.Length; index++)
        {
            var val = offsetTable[index];
            bw.Write(val);
        }

        byte[] rawData = ms.ToArray(); 
        
        offsetAccessor.WriteArray(0, rawData, 0, rawData.Length);
        
        offsetAccessor.Flush();
    }

    public void WriteArray<T>(int i, T[] array)
    {
        using var ms = new MemoryStream();
        new BinaryFormatter().Serialize(ms, array);
        byte[] data = ms.ToArray();

        long offset = GetMmapArrayOffset(i);

        using (var accessor = m_mmf.CreateViewAccessor(offset, data.Length + 4, MemoryMappedFileAccess.Write))
        {
            accessor.Write(0, data.Length); // prefix with length
            accessor.WriteArray(4, data, 0, data.Length);
            
            accessor.Flush();
        }

        if (i + 1 < m_arraysCount)
        {
            UpdateOffsetTable(i + 1, offset, data.Length + 4);
        }
    }

    public T[] ReadArray<T>(int index) 
    {
        long offset = GetMmapArrayOffset(index);
        using var accessor = m_mmf.CreateViewAccessor(offset, 4);

        int length = accessor.ReadInt32(0);
        byte[] data = new byte[length];
        
        using var dataAccessor = m_mmf.CreateViewAccessor(offset + 4, length);
        dataAccessor.ReadArray(0, data, 0, length);

        using var ms = new MemoryStream(data);
        return (T[])new BinaryFormatter().Deserialize(ms);
    }

    public void WriteSingleArray<T>(T[] array)
    {
        WriteArray(0, array);
    }

    public T[] ReadSingleArray<T>() 
    {
        return ReadArray<T>(0);
    }

    public void Dispose()
    {
        m_mmf?.Dispose();
    }
}