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
    private const string VersionName = "Version";
    
    private readonly SerializationInfo info;
    private int m_count;

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
    /// Writes metadata.
    /// </summary>
    /// <param name="metaData"></param>
    public void WriteMetadata((int maxSizeOfArray, int dataCount, int version, int arraysCount) metaData)
    {
        info.AddValue(NodeCapacityName, metaData.maxSizeOfArray);
        info.AddValue(CapacityName, metaData.dataCount);
        info.AddValue(VersionName, metaData.version);
        info.AddValue(ElementsCountName, metaData.arraysCount);
    }

    /// <summary>
    /// Reads metadata.
    /// </summary>
    /// <returns></returns>
    public (int maxSizeOfArray, int dataCount, int version, int arraysCount) ReadMetadata()
    {
        var maxSizeOfArray = info.GetInt32(NodeCapacityName);
        var dataCount = info.GetInt32(CapacityName);
        var version = info.GetInt32(VersionName);
        var elementsCount = info.GetInt32(ElementsCountName);
        
        return (maxSizeOfArray, dataCount, version, elementsCount);
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
}