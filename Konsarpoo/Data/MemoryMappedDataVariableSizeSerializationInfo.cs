using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Konsarpoo.Collections;

public class MemoryMappedDataVariableSizeSerializationInfo : MemoryMappedDataSerializationInfo
{
    private long[] m_offsetTable;
    
    public MemoryMappedDataVariableSizeSerializationInfo(string path, int maxSizeOfArray, int arraysCount, long estimatedSizeOfArray, long maxSizeOfFile = 0) : base(path, maxSizeOfArray, arraysCount, estimatedSizeOfArray * maxSizeOfArray, maxSizeOfFile)
    {
        m_offsetTable = new long[arraysCount];
    }

    private MemoryMappedDataVariableSizeSerializationInfo(string file, long estimatedSizeOfArray) : base(file, estimatedSizeOfArray)
    {
    }

    public static MemoryMappedDataVariableSizeSerializationInfo Open(string file, long estimatedSizeOfArray)
    {
        return new MemoryMappedDataVariableSizeSerializationInfo(file, estimatedSizeOfArray);
    }
    
    private void UpdateOffsetTable(int arrayIndex, long offset, long size)
    {
        var baseOffset = GetBaseOffset();
        var l = offset - baseOffset;
        m_offsetTable[arrayIndex] = l + size;
        WriteOffsetTableInfo(m_offsetTable);
    }

    private long GetBaseOffset()
    {
        return m_metaSize + Marshal.SizeOf<long>() * m_offsetTable.Length;
    }

    protected override long GetMmapArrayOffset(int arrayIndex)
    {
        return GetBaseOffset() + m_offsetTable[arrayIndex];
    }

    protected override long GetEstimatedSizeOfArrayCore(long estimatedSizeOfT, int maxSizeOfArray)
    {
        return estimatedSizeOfT * maxSizeOfArray;
    }

    public override (int maxSizeOfArray, int dataCount, int version, int arraysCount) ReadMetaData()
    {
        var readMetaData = base.ReadMetaData();
        
        m_offsetTable = ReadOffsetTableInfo(readMetaData.arraysCount);
        
        return readMetaData;
    }

    protected override (byte[] data, long offset) WriteArrayCore<T>(int i, T[] array)
    {
        var rez = base.WriteArrayCore(i, array);
        
        if (i + 1 < m_arraysCount)
        {
            UpdateOffsetTable(i + 1, rez.offset, rez.data.Length + Marshal.SizeOf<int>());
        }
        
        return rez;
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
}