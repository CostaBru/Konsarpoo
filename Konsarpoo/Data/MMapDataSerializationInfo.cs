using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Konsarpoo.Collections;

public abstract class MemoryMappedDataSerializationInfo : IDataSerializationInfo, IDisposable
{
    protected readonly int m_maxSizeOfArray;
    protected long m_capacity;
    protected int m_arraysCount;
    protected readonly long m_estimatedSizeOfArray;
    private string m_path;
    private int m_count;

    protected static readonly long m_metaSize = sizeof(int) * Marshal.SizeOf<int>(); // maxSizeOfArray, dataCount, version, arraysCount

    protected abstract long GetMmapArrayOffset(int arrayIndex);

    protected MemoryMappedFile m_mmf;

    public MemoryMappedDataSerializationInfo(string path, int maxSizeOfArray, int arraysCount, long estimatedSizeOfArray, long maxSizeOfFile = 0)
    {
        m_path = path;
        m_maxSizeOfArray = maxSizeOfArray;
        m_arraysCount = arraysCount;
        m_estimatedSizeOfArray = estimatedSizeOfArray;
        m_capacity = maxSizeOfFile == 0 ? arraysCount * estimatedSizeOfArray + m_metaSize : maxSizeOfFile;
      
        using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        m_mmf = MemoryMappedFile.CreateFromFile(fs, null, m_capacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
    }

    protected abstract long GetEstimatedSizeOfArrayCore(long estimatedSizeOfT, int maxSizeOfArray);

    protected MemoryMappedDataSerializationInfo(string path, long estimatedSizeOfT)
    {
        m_path = path;
        m_mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null);

        var readMetaData = ReadMetaData();

        var estimatedSizeOfArray = GetEstimatedSizeOfArrayCore(estimatedSizeOfT, readMetaData.maxSizeOfArray);

        m_maxSizeOfArray = readMetaData.maxSizeOfArray;
        m_arraysCount = readMetaData.arraysCount;
        m_capacity = m_arraysCount * estimatedSizeOfArray + m_metaSize;
        m_estimatedSizeOfArray = estimatedSizeOfArray;
    }

    private void Resize(long dataLength)
    {
        m_mmf.Dispose();

        m_capacity += m_capacity / 2;

        m_capacity = Math.Max(dataLength, m_capacity);
        
        using var fs = new FileStream(m_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        
        fs.SetLength(m_capacity);

        m_mmf = MemoryMappedFile.CreateFromFile(fs, null, m_capacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
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

    public void AppendArray<T>(T[] array)
    {
        var i = m_count;
        var (data, offset) = WriteArrayCore(i, array);
        m_count++;
    }

    protected virtual (byte[] data, long offset) WriteArrayCore<T>(int i, T[] array)
    {
        var data = GetBytes(array);

        long offset = GetMmapArrayOffset(i);

        var dataLength = data.Length + Marshal.SizeOf<int>();

        if (dataLength + offset > m_capacity)
        {
            Resize(dataLength + offset);
        }
        
        using (var accessor = m_mmf.CreateViewAccessor(offset, dataLength, MemoryMappedFileAccess.Write))
        {
            accessor.Write(0, data.Length); // prefix with length
            accessor.WriteArray(Marshal.SizeOf<int>(), data, 0, data.Length);
            
            accessor.Flush();
        }

        return (data, offset);
    }

    protected virtual byte[] GetBytes<T>(T[] array)
    {
        using var ms = new MemoryStream();
        new BinaryFormatter().Serialize(ms, array);
        byte[] data = ms.ToArray();
        return data;
    }

    public T[] ReadArray<T>(int index) 
    {
        long offset = GetMmapArrayOffset(index);
        using var accessor = m_mmf.CreateViewAccessor(offset, 4);

        int length = accessor.ReadInt32(0);
        byte[] data = new byte[length];
        
        using var dataAccessor = m_mmf.CreateViewAccessor(offset + Marshal.SizeOf<int>(), length);
        dataAccessor.ReadArray(0, data, 0, length);

        return GetData<T>(data);
    }

    protected virtual T[] GetData<T>(byte[] data)
    {
        using var ms = new MemoryStream(data);
        return (T[])new BinaryFormatter().Deserialize(ms);
    }

    public void WriteSingleArray<T>(T[] array)
    {
        AppendArray(array);
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