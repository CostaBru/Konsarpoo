using System.Linq;
using System.Runtime.Serialization;

namespace Konsarpoo.Collections;

public class DataMemorySerializationInfo : IDataSerializationInfo
{
    private const string CapacityName = "Capacity";
    private const string NodeCapacityName = "NodeCapacity";
    private const string ElementsCountName = "ElementsCount";
    private const string ElementsName = "Elements";
    private const string VersionName = "Version";
    
    private readonly SerializationInfo info;

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

    public void WriteMetaData((int maxSizeOfArray, int dataCount, int version, int arraysCount) metaData)
    {
        info.AddValue(NodeCapacityName, metaData.maxSizeOfArray);
        info.AddValue(CapacityName, metaData.dataCount);
        info.AddValue(VersionName, metaData.version);
        info.AddValue(ElementsCountName, metaData.arraysCount);
    }

    public (int maxSizeOfArray, int dataCount, int version, int arraysCount) ReadMetaData()
    {
        var maxSizeOfArray = (int)info.GetInt32(NodeCapacityName);
        var dataCount = (int)info.GetInt32(CapacityName);
        var version = (int)info.GetInt32(VersionName);
        var elementsCount = (int)info.GetInt32(ElementsCountName);
        
        return (maxSizeOfArray, dataCount, version, elementsCount);
    }

    public void WriteArray<T>(int i, T[] array)
    {
        var elementsName = GetElementName(i);
        info.AddValue(elementsName, array, typeof(T[]));
    }

    public void WriteSingleArray<T>(T[] st)
    {
        info.AddValue(ElementsName, st, typeof(T[]));
    }

    public T[] ReadArray<T>(int i)
    {
        var elementName = GetElementName(i);
        T[] objArray = (T[])info.GetValue(elementName, typeof(T[]));
        return objArray;
    }

    public T[] ReadSingleArray<T>()
    {
        T[] objArray = (T[])info.GetValue(ElementsName, typeof(T[]));
        return objArray;
    }

    public void WriteArraysCount(int count)
    {
        info.AddValue(ElementsCountName, count);
    }

    public int ReadArraysCount()
    {
        return info.GetInt32(ElementsCountName);
    }
}