using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Konsarpoo.Collections.Data.Serialization;

/// <summary>
/// Memory serialization info.
/// </summary>
public class DataMemorySerializationInfo : IDataSerializationInfo
{
    private const string CapacityName = "Capacity";
    private const string NodeCapacityName = "NodeCapacity";
    private const string ElementsCountName = "ElementsCount";
    private const string ElementsName = "Elements";
    private const string ExtraMetaDataName = "ExtraMetadata";
    private const string VersionName = "Version";
    
    private readonly SerializationInfo info;
    private int m_count;
    private (int maxSizeOfArray, int dataCount, int version) m_metaData;
    private byte[] m_extraMetaDataBytes = Array.Empty<byte>();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="inf"></param>
    public DataMemorySerializationInfo(SerializationInfo inf)
    {
        info = inf;
    }
    
    private string GetElementName(int index)
    {
        if (index < s_predefinedElementsName.Length)
        {
            return s_predefinedElementsName[index];
        }

        return ElementsName + index;
    }

    private static readonly string[] s_predefinedElementsName = Enumerable.Range(0, 100).Select(i => ElementsName + i).ToArray();

    /// <summary>
    /// Updates metadata.
    /// </summary>
    /// <param name="metaData"></param>
    public void UpdateMetadata((int maxSizeOfArray, int dataCount, int version) metaData)
    {
        m_metaData = metaData;
    }

    /// <summary>
    /// Writes metadata.
    /// </summary>
    public void WriteMetadata()
    {
        info.AddValue(NodeCapacityName, m_metaData.maxSizeOfArray);
        info.AddValue(CapacityName, m_metaData.dataCount);
        info.AddValue(VersionName, m_metaData.version);
        info.AddValue(ElementsCountName, m_count);
        info.AddValue(ExtraMetaDataName, m_extraMetaDataBytes, typeof(byte[]));
    }
    
    /// <summary>
    /// Gets metadata.
    /// </summary>
    public (int maxSizeOfArray, int dataCount, int version) MetaData => m_metaData;

    /// <summary>
    /// Gets an extra metadata saved to the serialization stream.
    /// </summary>
    public byte[] ExtraMetadata => m_extraMetaDataBytes;

    /// <summary>
    /// Sets extra metadata payload.
    /// </summary>
    /// <param name="metaDataBytes"></param>
    public void SetExtraMetadata(byte[] metaDataBytes)
    {
        m_extraMetaDataBytes = metaDataBytes ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Reads metadata.
    /// </summary>
    /// <returns></returns>
    public void ReadMetadata()
    {
        var maxSizeOfArray = info.GetInt32(NodeCapacityName);
        var dataCount = info.GetInt32(CapacityName);
        var version = info.GetInt32(VersionName);
        
        m_metaData = (maxSizeOfArray, dataCount, version);
        
        m_count = info.GetInt32(ElementsCountName);
        m_extraMetaDataBytes = (byte[])info.GetValue(ExtraMetaDataName, typeof(byte[])) ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Appends an array.
    /// </summary>
    /// <param name="array"></param>
    /// <typeparam name="T"></typeparam>
    public void AppendArray<T>(T[] array)
    {
        var i = m_count;

        var elementsName = GetElementName(i);
        info.AddValue(elementsName, array, typeof(T[]));

        m_count++;
    }

    /// <summary>
    /// Writes a single array.
    /// </summary>
    /// <param name="st"></param>
    /// <typeparam name="T"></typeparam>
    public void WriteSingleArray<T>(T[] st)
    {
        info.AddValue(ElementsName, st, typeof(T[]));

        m_count++;
    }

    /// <summary>
    /// Reads an array.
    /// </summary>
    /// <param name="i"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T[] ReadArray<T>(int i)
    {
        var elementName = GetElementName(i);
        T[] objArray = (T[])info.GetValue(elementName, typeof(T[]));
        return objArray;
    }

    /// <summary>
    /// Reads a single array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T[] ReadSingleArray<T>()
    {
        T[] objArray = (T[])info.GetValue(ElementsName, typeof(T[]));
        return objArray;
    }

    /// <summary>
    /// Gets total arrays stored.
    /// </summary>
    public int ArrayCount => m_count;
}