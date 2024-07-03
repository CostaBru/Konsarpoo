using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml;
using System.Xml.Schema;

namespace Konsarpoo.Collections;

public partial class AbstractTupleTrieMap<TKey, TValue>
{
    private const string VersionName = "Version";
    private const string SizeName = "Size";
    private const string KeyValuePairsName = "KeyValuePairs";
    private const string MapFactoryName = "MapFactory";

    protected XmlSchema GetSchemaCore() => null;

    protected void ReadXmlCore(XmlReader reader)
    {
        var dataContractSerializer = new DataContractSerializer(typeof(KeyValuePair<TKey, TValue>));

        Clear();
        
        var add = true;
        
        reader.ReadStartElement();
        
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            reader.ReadStartElement();
            if(m_nodesMapFactory == null && reader.Name.Contains(nameof(DelegateSerializationHelper)))
            {
                var delegateSerializer = new DataContractSerializer(typeof(DelegateSerializationHelper));
                var helper = (DelegateSerializationHelper)delegateSerializer.ReadObject(reader);
                m_nodesMapFactory = helper.GetDelegate<Func<Type,IDictionary<object, TrieLinkNode<TValue>>>>();
                reader.ReadEndElement();
                continue;
            }
            var keyValue = (KeyValuePair<TKey, TValue>)dataContractSerializer.ReadObject(reader);
            reader.ReadEndElement();
            var value = keyValue.Value;
            Insert(keyValue.Key, ref value, ref add);
        }
        reader.ReadEndElement();
    }

    protected void WriteXmlCore(XmlWriter writer)
    {
        var itemSerializer = new DataContractSerializer(typeof(KeyValuePair<TKey, TValue>));
        
        if (m_nodesMapFactory != null)
        {
            var delegateSerializer = new DataContractSerializer(typeof(DelegateSerializationHelper));

            writer.WriteStartElement(MapFactoryName);
            delegateSerializer.WriteObject(writer, new DelegateSerializationHelper(m_nodesMapFactory));
            writer.WriteEndElement();
        }

        foreach (var item in GetKeyValues())
        {
            writer.WriteStartElement("Item");
            itemSerializer.WriteObject(writer, new KeyValuePair<TKey, TValue>(item.Key, item.Value));
            writer.WriteEndElement();
        }
    }

    /// <inheritdoc />
    [System.Security.SecurityCritical] // auto-generated_required
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    protected void GetObjectDataCore(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        info.AddValue(SizeName, Count);
        info.AddValue(VersionName, Version);
       
        if (m_nodesMapFactory != null)
        {
            info.AddValue(MapFactoryName, new DelegateSerializationHelper(m_nodesMapFactory));
        }
        else
        {
            info.AddValue(MapFactoryName, null);
        }

        if (Count > 0)
        {
            info.AddValue(KeyValuePairsName, ToData());
        }
    }
    
    [Serializable]
    public class DelegateSerializationHelper
    {
        public string MethodName { get; set; }
        public string TargetType { get; set; }

        public DelegateSerializationHelper() { }

        public DelegateSerializationHelper(Delegate del)
        {
            MethodName = del.Method.Name;
            TargetType = del.Method.DeclaringType.AssemblyQualifiedName;
        }

        public T GetDelegate<T>()
        {
            var targetType = Type.GetType(TargetType);
            var method = targetType.GetMethod(MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            return (T)(object)Delegate.CreateDelegate(typeof(T), method);
        }
    }

    protected void OnDeserializationCore(object sender)
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
        var helper = (DelegateSerializationHelper)siInfo.GetValue(MapFactoryName, typeof(DelegateSerializationHelper));

        if (helper != null)
        {
            m_nodesMapFactory = helper.GetDelegate<Func<Type,IDictionary<object, TrieLinkNode<TValue>>>>();
        }
        
        if (count != 0)
        {
            var data = (Data<KeyValuePair<TKey, TValue>>)siInfo.GetValue(KeyValuePairsName, typeof(Data<KeyValuePair<TKey, TValue>>));

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