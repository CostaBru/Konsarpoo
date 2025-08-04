using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Konsarpoo.Collections;

internal abstract class MemoryMappedDataSerializationInfo : IDataSerializationInfo, IDisposable
{
    protected readonly int m_maxSizeOfArray;
    protected long m_bytesCapacity;
    protected readonly long m_estimatedSizeOfArray;
    private string m_path;
    private int m_count;

    protected static readonly long m_metaSize = sizeof(int) * Marshal.SizeOf<int>(); // maxSizeOfArray, dataCount, version, arraysCount

    protected abstract long GetMmapArrayOffset(int arrayIndex);

    protected MemoryMappedFile m_mmf;
    protected MemoryMappedViewAccessor m_accessor;
    protected bool m_disposed = false;

    public MemoryMappedDataSerializationInfo(string path, 
        int maxSizeOfArray, 
        int arrayCapacity, 
        long estimatedSizeOfArray, 
        Type arrayItemType, 
        long maxSizeOfFile = 0)
    {
        m_path = path;
        m_maxSizeOfArray = maxSizeOfArray;
        m_estimatedSizeOfArray = estimatedSizeOfArray;
        m_bytesCapacity = maxSizeOfFile == 0 ? arrayCapacity * estimatedSizeOfArray + m_metaSize : maxSizeOfFile;
        m_count = 0;
        
        using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        m_mmf = MemoryMappedFile.CreateFromFile(fs, null, m_bytesCapacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);

        MemoryMappedViewAccessor memoryMappedViewAccessor = m_mmf.CreateViewAccessor();

        m_accessor = memoryMappedViewAccessor;

        if (arrayItemType == typeof(double))
        {
            m_writeBytes = m_writeBytesDouble;
            m_readBytes = m_readBytesDouble;
        }
    }

    protected abstract long GetEstimatedSizeOfArrayCore(long estimatedSizeOfT, int maxSizeOfArray);

    protected MemoryMappedDataSerializationInfo(string path, long estimatedSizeOfT, Type arrayItemType)
    {
        m_path = path;
        m_mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null);
        m_accessor = m_mmf.CreateViewAccessor();
        
        var readMetaData = ReadMetaData();

        var estimatedSizeOfArray = GetEstimatedSizeOfArrayCore(estimatedSizeOfT, readMetaData.maxSizeOfArray);

        m_maxSizeOfArray = readMetaData.maxSizeOfArray;
        m_count = readMetaData.arraysCount;
        m_bytesCapacity = readMetaData.arraysCount * estimatedSizeOfArray + m_metaSize;
        m_estimatedSizeOfArray = estimatedSizeOfArray;
        
        if (arrayItemType == typeof(double))
        {
            m_writeBytes = m_writeBytesDouble;
            m_readBytes = m_readBytesDouble;
        }
    }

    private void Resize(long dataLength)
    {
        m_accessor.Flush();
        m_accessor.Dispose();
        m_mmf.Dispose();

        m_bytesCapacity += m_bytesCapacity / 2;
        m_bytesCapacity = Math.Max(dataLength, m_bytesCapacity);
        
        using var fs = new FileStream(m_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        
        fs.SetLength(m_bytesCapacity);

        m_mmf = MemoryMappedFile.CreateFromFile(fs, null, m_bytesCapacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
        m_accessor = m_mmf.CreateViewAccessor();
    }
  
    public void WriteMetaData((int maxSizeOfArray, int dataCount, int version, int arraysCount) metaData)
    {
        m_accessor.Write(0, metaData.maxSizeOfArray);
        m_accessor.Write(sizeof(int), metaData.dataCount);
        m_accessor.Write(sizeof(int) * 2, metaData.version);
        m_accessor.Write(sizeof(int) * 3, metaData.arraysCount);
    }

    public virtual (int maxSizeOfArray, int dataCount, int version, int arraysCount) ReadMetaData()
    {
        int maxSize = m_accessor.ReadInt32(0);
        int count = m_accessor.ReadInt32(sizeof(int));
        int version = m_accessor.ReadInt32(sizeof(int) * 2);
        int arrayCount = m_accessor.ReadInt32(sizeof(int) * 3);
        
        return (maxSize, count, version, arrayCount);
    }

    public void AppendArray<T>(T[] array)
    {
        var i = m_count;
        var (__, ___) = WriteArrayCore(i, array);
        m_count++;
    }

    public void WriteArray<T>(int i, T[] array)
    {
        WriteArrayCore(i, array);
    }

    protected int ArrayCount => m_count;

    protected virtual (int bytesWritten, long offset) WriteArrayCore<T>(int i, T[] array)
    {
        if (i >= m_count - 1)
        {
            return WriteArrayToMem(i, array);
        }

        var capacity = m_bytesCapacity;

        long nextArrayOffset = GetMmapArrayOffset(i + 1);

        var copySize = capacity - nextArrayOffset;

        var pool = ArrayPool<byte>.Shared;

        var chunks = CopyMemory<T>(nextArrayOffset, copySize, pool);

        (int bytesWritten, long offset) = WriteArrayToMem(i, array);

        WriteMemory<T>(offset + bytesWritten, copySize, chunks, pool);

        return (bytesWritten, offset);
    }

    private void WriteMemory<T>(long offset, long size, List<(byte[] chunk, int size)> chunks, ArrayPool<byte> pool)
    {
        int position = 0;
            
        foreach (var chunk in chunks)
        {
            m_accessor.WriteArray(position + offset, chunk.chunk, 0, chunk.size);
                
            position += chunk.size;
                
            pool.Return(chunk.chunk);
        }
    }

    private List<(byte[] chunk, int size)> CopyMemory<T>(long offset, long copySize, ArrayPool<byte> pool)
    {
        var chunks = new List<(byte[] chunk, int size)>();

        var rest = copySize;

        int position = 0;

        while (rest > 0)
        {
            var minChunkSize = (int)Math.Min(ushort.MaxValue + 1, rest);

            var chunk = pool.Rent(minChunkSize);

            var chunkCopySize = (int)Math.Min(rest, chunk.Length);

            m_accessor.ReadArray(position + offset, chunk, 0, chunkCopySize);

            chunks.Add((chunk, chunkCopySize));

            rest -= chunkCopySize;

            position += chunkCopySize;
        }
        return chunks;
    }

   
    private (int byteSize, long offset) WriteArrayToMem<T>(int i, T[] array)
    {
        long offset = GetMmapArrayOffset(i);
        
        var bytes = GetBytes(array);

        var bytesSize = bytes.count + Marshal.SizeOf<int>();

        if (bytesSize + offset > m_bytesCapacity)
        {
            Resize(bytesSize + offset);
        }
        
        m_accessor.Write(offset, bytes.count); // prefix with length
        m_accessor.WriteArray(offset + Marshal.SizeOf<int>(), bytes.data, 0, bytes.count);

        return (bytesSize, offset);
    }

    private Func<object, (byte[] data, int count)> m_writeBytes = (array) =>
    {
        using var ms = new MemoryStream();
        new BinaryFormatter().Serialize(ms, array);
        byte[] data = ms.ToArray();
        return (data, data.Length);
    };

    private static Func<object, (byte[] data, int count)> m_writeBytesDouble = GetBytesFunc<double>();
    
    private static Func<object, (byte[] data, int count)> GetBytesFunc<T>() where T : struct
    {
        Func<object, (byte[] data, int count)> f = (arr) =>
        {
            var span = ((T[])arr).AsSpan();
            var asBytes = MemoryMarshal.AsBytes<T>(span);
            return (asBytes.ToArray(), asBytes.Length);
        };

        return f;
    }
    
    private Func<(byte[] data, int length), object> m_readBytes = (p) =>
    {
        using var ms = new MemoryStream(p.data, 0, p.length);
        return new BinaryFormatter().Deserialize(ms);
    };

    private static Func<(byte[] data, int length), object> m_readBytesDouble = ReadBytesFunc<double>();
    
    private static Func<(byte[] data, int length), object> ReadBytesFunc<T>() where T : struct
    {
        Func<(byte[] data, int length), object> f = (fb) =>
        {
            Span<byte> bytes = fb.data.AsSpan();
            Span<T> doubles = MemoryMarshal.Cast<byte, T>(bytes);
            return doubles.ToArray();
        };

        return f;
    }
    
    protected (byte[] data, int count) GetBytes(object array)
    {
        return m_writeBytes(array);
    }

    public T[] ReadArray<T>(int index) 
    {
        long offset = GetMmapArrayOffset(index);
        var accessor = m_accessor;

        int length = accessor.ReadInt32(offset);

        var pool = ArrayPool<byte>.Shared;
        var data = pool.Rent(length);

        try
        {
            accessor.ReadArray(offset + Marshal.SizeOf<int>(), data, 0, length);

            var array = GetData<T>(data, length);

            return array;
        }
        finally
        {
            pool.Return(data);
        }
    }

    protected T[] GetData<T>(byte[] data, int length)
    {
       return (T[])m_readBytes((bytes: data, length: length));
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
        if (m_disposed)
        {
            return;
        }

        m_accessor?.Flush();
        m_accessor?.Dispose();
        m_mmf?.Dispose();
        m_disposed = true;
    }
}