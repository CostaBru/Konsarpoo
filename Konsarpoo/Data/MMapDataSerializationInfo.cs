using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Konsarpoo.Collections;

public abstract class MemoryMappedDataSerializationInfo : IDataSerializationInfo, IDisposable
{
    protected readonly MemoryMappedFile m_mmf;
    protected readonly int m_maxSizeOfArray;
    protected long m_capacity;
    protected int m_arraysCount;
    protected readonly long m_estimatedSizeOfArray;

    protected static readonly long m_metaSize = sizeof(int) * Marshal.SizeOf<int>(); // maxSizeOfArray, dataCount, version, arraysCount

    protected abstract long GetMmapArrayOffset(int arrayIndex);


    public MemoryMappedDataSerializationInfo(string path, int maxSizeOfArray, int arraysCount, long estimatedSizeOfArray)
    {
        m_maxSizeOfArray = maxSizeOfArray;
        m_arraysCount = arraysCount;
        m_estimatedSizeOfArray = estimatedSizeOfArray;
        m_capacity = arraysCount * estimatedSizeOfArray + m_metaSize;
      
        using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        m_mmf = MemoryMappedFile.CreateFromFile(fs, null, m_capacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
    }

    protected abstract long GetEstimatedSizeOfArrayCore(long estimatedSizeOfT, int maxSizeOfArray);

    protected MemoryMappedDataSerializationInfo(string path, long estimatedSizeOfT)
    {
        m_mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null);

        var readMetaData = ReadMetaData();

        var estimatedSizeOfArray = GetEstimatedSizeOfArrayCore(estimatedSizeOfT, readMetaData.maxSizeOfArray);

        m_maxSizeOfArray = readMetaData.maxSizeOfArray;
        m_arraysCount = readMetaData.arraysCount;
        m_capacity = m_arraysCount * estimatedSizeOfArray + m_metaSize;
        m_estimatedSizeOfArray = estimatedSizeOfArray;
    }

    public static int EstimateSerializedEmptyArraySize<T>()
    {
        using var ms = new MemoryStream();
#pragma warning disable SYSLIB0011
        new BinaryFormatter().Serialize(ms, Array.Empty<T>());
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

    public virtual (int maxSizeOfArray, int dataCount, int version, int arraysCount) ReadMetaData()
    {
        using var accessor = m_mmf.CreateViewAccessor(0, m_metaSize, MemoryMappedFileAccess.Read);
        int maxSize = accessor.ReadInt32(0);
        int count = accessor.ReadInt32(sizeof(int));
        int version = accessor.ReadInt32(sizeof(int) * 2);
        int arrayCount = accessor.ReadInt32(sizeof(int) * 3);
        
        return (maxSize, count, version, arrayCount);
    }

    public void WriteArray<T>(int i, T[] array)
    {
        var (data, offset) = WriteArrayCore(i, array);
    }

    protected virtual (byte[] data, long offset) WriteArrayCore<T>(int i, T[] array)
    {
        using var ms = new MemoryStream();
        new BinaryFormatter().Serialize(ms, array);
        byte[] data = ms.ToArray();

        long offset = GetMmapArrayOffset(i);

        using (var accessor = m_mmf.CreateViewAccessor(offset, data.Length + Marshal.SizeOf<int>(), MemoryMappedFileAccess.Write))
        {
            accessor.Write(0, data.Length); // prefix with length
            accessor.WriteArray(Marshal.SizeOf<int>(), data, 0, data.Length);
            
            accessor.Flush();
        }

        return (data, offset);
    }

    public T[] ReadArray<T>(int index) 
    {
        long offset = GetMmapArrayOffset(index);
        using var accessor = m_mmf.CreateViewAccessor(offset, 4);

        int length = accessor.ReadInt32(0);
        byte[] data = new byte[length];
        
        using var dataAccessor = m_mmf.CreateViewAccessor(offset + Marshal.SizeOf<int>(), length);
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