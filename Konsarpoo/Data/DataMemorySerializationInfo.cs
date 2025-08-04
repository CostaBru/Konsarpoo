using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Konsarpoo.Collections;

public class DataMemorySerializationInfo<T> 
{
    private const string CapacityName = "Capacity";
    private const string NodeCapacityName = "NodeCapacity";
    private const string ElementsCountName = "ElementsCount";
    private const string ElementsName = "Elements";
    private const string VersionName = "Version";
    
    private readonly SerializationInfo info;
    private int m_count;
    
    private static readonly ILookup<Type, (Func<object, byte[]> write, Func<byte[], object> read)> m_typesSeri = BuiltinSeriHelper.GetSeriLookup();

    private Action<SerializationInfo, string, T[]> m_writeArray;
    private Func<SerializationInfo, string, T[]> m_readArray;
    
    public DataMemorySerializationInfo(SerializationInfo inf, bool useHelper = true)
    {
        info = inf;
        
        var tuple = m_typesSeri[typeof(T)].FirstOrDefault();

        if (tuple.write != null && useHelper)
        {
            m_writeArray = (i, n, a) =>
            {
                i.AddValue(n, tuple.write(a), typeof(byte[]));
            };
            
            m_readArray = (i, n) =>
            {
                byte[] objArray = (byte[])i.GetValue(n, typeof(byte[]));

                return (T[])tuple.read(objArray);
            };
        }
        else
        {
            m_writeArray = (i, n, a) =>
            {
                i.AddValue(n, a, typeof(T[]));
            };
            
            m_readArray = (i, n) =>
            {
               return (T[])i.GetValue(n, typeof(T[]));
            };
        }
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
        var maxSizeOfArray = info.GetInt32(NodeCapacityName);
        var dataCount = info.GetInt32(CapacityName);
        var version = info.GetInt32(VersionName);
        var elementsCount = info.GetInt32(ElementsCountName);
        
        return (maxSizeOfArray, dataCount, version, elementsCount);
    }

    public void AppendArray(T[] array)
    {
        var i = m_count;

        var elementsName = GetElementName(i);

        m_writeArray(info, elementsName, array);

        m_count++;
    }

    public void WriteSingleArray(T[] st)
    {
        m_writeArray(info, ElementsName, st);
    }

    public T[] ReadArray(int i)
    {
        var elementName = GetElementName(i);

        return m_readArray(info, elementName);
    }

    public T[] ReadSingleArray()
    {
        return m_readArray(info, ElementsName);
    }
}