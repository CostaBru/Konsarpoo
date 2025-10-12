using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Data.Serialization;

namespace Konsarpoo.Collections;

public partial class AbstractTupleTrieMap<TKey, TValue>: IDataSerializable
{
    private const string MapFactoryName = "MapFactory";

    [Serializable]
    private struct TrieMapExtraInfo
    {
        public DelegateSerializationHelper NodesMapFactoryHelper;
    }

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
        SerializeTo(new DataMemorySerializationInfo(info));
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
        if (m_sInfo == null)
        {
            return;
        }

        DeserializeFrom(new DataMemorySerializationInfo(m_sInfo));

        m_sInfo = null;
    }
    
    private static readonly IDataAllocatorSetup<KeyValuePair<TKey, TValue>> m_serializationAllocatorSetup = GcAllocatorSetup.GetDataPoolSetup<KeyValuePair<TKey, TValue>>();
      
    /// <summary>
    /// Serializes the current instance to the provided <see cref="IDataSerializationInfo"/> implementation.
    /// </summary>
    /// <param name="info"></param>
    public virtual void SerializeTo([NotNull] IDataSerializationInfo info)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        
        var data = new Data<KeyValuePair<TKey, TValue>>(m_serializationAllocatorSetup);
        data.Ensure(Count);

        int i = 0;
        foreach (var kv in GetKeyValues())
        {
            data[i] = new KeyValuePair<TKey, TValue>(kv.Key, kv.Value);
            i++;
        }

        if (m_nodesMapFactory != null)
        {
            var trieMapExtraInfo = new TrieMapExtraInfo()
            {
                NodesMapFactoryHelper = new DelegateSerializationHelper(m_nodesMapFactory)
            };

            var serializeWithDcs = SerializeHelper.SerializeWithDcs(trieMapExtraInfo);
            
            info.SetExtraMetadata(Encoding.UTF8.GetBytes(serializeWithDcs));
        }

        data.SerializeTo(info);
    }

    /// <summary>
    /// Deserializes the current instance from the provided <see cref="IDataSerializationInfo"/> implementation.
    /// </summary>
    /// <param name="info"></param>
    /// <exception cref="SerializationException"></exception>
    public virtual void DeserializeFrom([NotNull] IDataSerializationInfo info)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        
        Clear();
        
        using var data = new Data<KeyValuePair<TKey, TValue>>(m_serializationAllocatorSetup);
        data.DeserializeFrom(info);
        
        if (info.ExtraMetadata.Length > 0)
        {
            var xml = Encoding.UTF8.GetString(info.ExtraMetadata);
            var deserializeWithDcs = SerializeHelper.DeserializeWithDcs<TrieMapExtraInfo>(xml);
            var trieMapExtraInfo = deserializeWithDcs;
            m_nodesMapFactory = trieMapExtraInfo.NodesMapFactoryHelper.GetDelegate<Func<Type, IDictionary<object, TrieLinkNode<TValue>>>>();
        }

        var add = true;
        
        foreach (var t in data)
        {
            var key = t.Key;
            var value = t.Value;
            
            Insert(key, ref value, ref add);
        }
        m_version = data.m_version;
    }
}