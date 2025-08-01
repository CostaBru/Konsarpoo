using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Konsarpoo.Collections;

public class MemoryMappedDataVariableSizeSerializationInfo : MemoryMappedDataSerializationInfo
{
    private long[] m_offsetTable;
    
    public MemoryMappedDataVariableSizeSerializationInfo(string path, int maxSizeOfArray, int arraysCount, long estimatedSizeOfArray,  Type arrayItemType, long maxSizeOfFile = 0) : base(path, maxSizeOfArray, arraysCount, estimatedSizeOfArray * maxSizeOfArray, arrayItemType, maxSizeOfFile)
    {
        m_offsetTable = new long[arraysCount];
    }

    private MemoryMappedDataVariableSizeSerializationInfo(string file, long estimatedSizeOfArray, Type arrayItemType) : base(file, estimatedSizeOfArray, arrayItemType)
    {
    }

    public static MemoryMappedDataVariableSizeSerializationInfo Open(string file, long estimatedSizeOfArray, Type arrayItemType)
    {
        return new MemoryMappedDataVariableSizeSerializationInfo(file, estimatedSizeOfArray, arrayItemType);
    }
    
    private void AppendNextArrayOffset(int arrayIndex, long offset, long bytesWritten)
    {
        var baseOffset = GetBaseOffset();
        var l = offset - baseOffset;
        m_offsetTable[arrayIndex + 1] = l + bytesWritten;
        WriteOffsetTableInfo(m_offsetTable);
    }
    
    private void UpdateOffsetTable(int arrayIndex, long offset, long bytesWritten, long delta)
    {
        var baseOffset = GetBaseOffset();
        
        var l = offset - baseOffset;
        m_offsetTable[arrayIndex] = l + bytesWritten;

        for (int i = arrayIndex + 1; i < arrayIndex + bytesWritten; i++)
        {
            m_offsetTable[i] += delta;
        }
        
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

    protected override (int bytesWritten, long offset) WriteArrayCore<T>(int i, T[] array)
    {
        var ( bytesWritten, offset) = base.WriteArrayCore(i, array);

        var arrayCount = ArrayCount;

        if (i + 1 < m_offsetTable.Length)
        {
            if (i >= arrayCount - 1)
            {
                AppendNextArrayOffset(i, offset, bytesWritten);
            }
            else
            {
                var oldSize = m_offsetTable[i + 1] - m_offsetTable[i];

                var delta = bytesWritten - oldSize;

                UpdateOffsetTable(i, offset, bytesWritten, delta);
            }
        }

        return (bytesWritten, offset);
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