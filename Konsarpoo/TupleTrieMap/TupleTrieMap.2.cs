using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Konsarpoo.Collections;

[Serializable]
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