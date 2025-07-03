using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Konsarpoo.Collections;

public class MemoryMappedDataFixedSizeSerializationInfo<T> : MemoryMappedDataSerializationInfo
{
    private static readonly long s_binBytesOverHead = EstimateSerializedSize(Array.Empty<T>());
    
    public MemoryMappedDataFixedSizeSerializationInfo(string path, int maxSizeOfArray, int arraysCount, long estimatedSizeOfT, long maxSizeOfFile = 0) :
        base(path, maxSizeOfArray, arraysCount, GetEstimatedSizeOfArray(estimatedSizeOfT, maxSizeOfArray), maxSizeOfFile)
    {
    }
    
    private MemoryMappedDataFixedSizeSerializationInfo(string file, long estimatedSizeOfT) : base(file, estimatedSizeOfT)
    {
    }
    

    public static MemoryMappedDataFixedSizeSerializationInfo<T> Open(string file, long estimatedSizeOfT)
    {
        return new MemoryMappedDataFixedSizeSerializationInfo<T>(file, estimatedSizeOfT);
    }

    protected override long GetEstimatedSizeOfArrayCore(long estimatedSizeOfT, int maxSizeOfArray) =>
        GetEstimatedSizeOfArray(estimatedSizeOfT, maxSizeOfArray);
    
    public static long GetEstimatedSizeOfArray(long estimatedSizeOfT, int maxSizeOfArray) =>
        s_binBytesOverHead + (estimatedSizeOfT * maxSizeOfArray) + Marshal.SizeOf<int>();
    
    public static int EstimateSerializedSize(T[] element)
    {
        using var ms = new MemoryStream();
#pragma warning disable SYSLIB0011
        new BinaryFormatter().Serialize(ms, element);
#pragma warning restore SYSLIB0011
        return (int)ms.Length; 
    }

    protected override long GetMmapArrayOffset(int arrayIndex)
    {
        return m_metaSize + arrayIndex * m_estimatedSizeOfArray;
    }
}