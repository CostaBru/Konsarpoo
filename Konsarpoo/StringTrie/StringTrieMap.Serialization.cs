using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Konsarpoo.Collections;

[KnownType(typeof(KeyValuePair<,>))]
public partial class StringTrieMap<TValue> : IXmlSerializable
{
    private const string VersionName = "Version";
    private const string SizeName = "Size";
    private const string KeyValuePairsName = "KeyValuePairs";
    private const string CaseSensitiveName = "CaseSensitive";
    
    XmlSchema IXmlSerializable.GetSchema() => null;

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
        var dataContractSerializer = new DataContractSerializer(typeof(TValue));

        Clear();

        m_caseSensitive = bool.Parse(reader.GetAttribute(CaseSensitiveName));
        m_prepareCharFunc = m_caseSensitive ? c => c : char.ToLower;
        
        var add = true;
        
        reader.ReadStartElement();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            var key = reader.GetAttribute("Key");
            reader.ReadStartElement("Item");
            var value = (TValue)dataContractSerializer.ReadObject(reader);
            reader.ReadEndElement();
            Insert(key, ref value, ref add);
        }
        reader.ReadEndElement();
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        var dataContractSerializer = new DataContractSerializer(typeof(TValue));

        writer.WriteAttributeString(CaseSensitiveName, m_caseSensitive.ToString());

        foreach (var item in GetKeyValuesString())
        {
            writer.WriteStartElement("Item");
            writer.WriteAttributeString("Key", item.Key);
            dataContractSerializer.WriteObject(writer, item.Value);
            writer.WriteEndElement();
        }
    }

    /// <inheritdoc />
    [System.Security.SecurityCritical] // auto-generated_required
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        info.AddValue(SizeName, Count);
        info.AddValue(CaseSensitiveName, m_caseSensitive);
        info.AddValue(VersionName, Version);

        if (Count > 0)
        {
            info.AddValue(KeyValuePairsName, ToData());
        }
    }

    void IDeserializationCallback.OnDeserialization(object sender)
    {
        var siInfo = m_sInfo;

        if (siInfo is null)
        {
            // It might be necessary to call OnDeserialization from a container if the container object also implements
            // OnDeserialization. However, remoting will call OnDeserialization again.
            // We can return immediately if this function is called twice. 
            // Note we set remove the serialization info from the table at the end of this method.
            return;
        }

        int realVersion = siInfo.GetInt32(VersionName);
        int count = siInfo.GetInt32(SizeName);

        m_caseSensitive = (bool)siInfo.GetValue(CaseSensitiveName, typeof(bool));
        m_prepareCharFunc = m_caseSensitive ? c => c : char.ToLower;
        
        if (count != 0)
        {
            var data = (Data<KeyValuePair<string, TValue>>)siInfo.GetValue(KeyValuePairsName,
                typeof(Data<KeyValuePair<string, TValue>>));

            if (data is null)
            {
                throw new SerializationException("Cannot read dict key values from serialization info.");
            }

            data.OnDeserialization(this);

            var add = true;

            foreach (var t in data)
            {
                var key = t.Key;
                var value = t.Value;

                Insert(key, ref value, ref add);
            }
        }

        m_version = (ushort)realVersion;
        m_sInfo = null;
    }
}