using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Konsarpoo.Collections;

public sealed class TupleTrieMapDebugView<T1, T2, V>
{
    private readonly IReadOnlyDictionary<(T1,T2), V> m_dict;

    public TupleTrieMapDebugView(IReadOnlyDictionary<(T1,T2), V> dict)
    {
        m_dict = dict;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<(T1,T2), V>[] Items => this.m_dict.ToArray();
}

[Serializable]
[DebuggerTypeProxy(typeof(TupleTrieMapDebugView<,,>))]
[DebuggerDisplay("Count = {Count}")]
public class TupleTrieMap<T1, T2, TVal> : AbstractTupleTrieMap<(T1, T2), TVal>, IXmlSerializable, ISerializable, IDeserializationCallback
{
    protected override (T1, T2) ConcatKeyWith((T1, T2) key, object val, int pos)
    {
        return pos switch
        {
            0 => ((T1)val, default(T2)),
            1 => (key.Item1, (T2)val),
            _ => key
        };
    }

    protected override (T1, T2) CreateKey(object[] obj)
    {
        return ((T1)obj[0], (T2)obj[1]);
    }

    public TupleTrieMap() : base()
    {
    }

    public TupleTrieMap(TupleTrieMap<T1, T2, TVal> copyFromMap) : base(copyFromMap)
    {
    }

    public TupleTrieMap(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public XmlSchema GetSchema() => GetSchemaCore();

    public void ReadXml(XmlReader reader)
    {
        ReadXmlCore(reader);
    }

    public void WriteXml(XmlWriter writer)
    {
        WriteXmlCore(writer);
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        GetObjectDataCore(info, context);
    }

    public void OnDeserialization(object sender)
    {
        OnDeserializationCore(sender);
    }
}