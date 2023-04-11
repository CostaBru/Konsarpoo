using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

public partial class LfuCache<TKey, TValue>
{
    private SerializationInfo m_sInfo;
    
    private const string StorageName = "Storage";
    private const string ComparerName = "Comparer";
    
    /// <summary>
    /// Deserialization constructor.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected LfuCache(SerializationInfo info, StreamingContext context)
    {
        m_sInfo = info;
    }
    
    /// <inheritdoc />
    [System.Security.SecurityCritical]  // auto-generated_required
    public void GetObjectData([NotNull] SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }
        
        var valuePairs = m_map.Select(kv => new KeyValuePair<TKey, (TValue value, int frequency)>(kv.Key, (kv.Value.Value, kv.Value.FreqNode.FreqValue))).ToData();
        
        info.AddValue(StorageName, valuePairs, typeof(Data<KeyValuePair<TKey, (TValue value, int frequency)>>));
        info.AddValue(ComparerName, m_comparer, typeof(IEqualityComparer<TKey>));
    }

    /// <inheritdoc />
    public void OnDeserialization(object sender)
    {
        if (m_sInfo == null)
        {
            return;
        }

        var siInfo = m_sInfo;

        m_comparer = (IEqualityComparer<TKey>)siInfo.GetValue(ComparerName, typeof(IEqualityComparer<TKey>));

        var storage = (Data<KeyValuePair<TKey, (TValue value, int frequency)>>)siInfo.GetValue(StorageName, typeof(Data<KeyValuePair<TKey, (TValue value, int frequency)>>));

        if (storage is null)
        {
            throw new SerializationException("Cannot read lfu cache storage from serialization info.");
        }

        storage.OnDeserialization(this);

        FreqNode prevNode = m_root;
        FreqNode nextNode = m_root.NextNode;

        foreach (var kv in storage.GroupBy(kv => kv.Value.frequency).OrderBy(r => r.Key))
        {
            var newNode = new FreqNode(m_comparer) { FreqValue = kv.Key, PrevNode = prevNode, NextNode = nextNode };

            m_root.NextNode.PrevNode = newNode;
            m_root.NextNode = newNode;

            foreach (var st in kv)
            {
                newNode.Keys.Add(st.Key);
                m_map[st.Key] = new DataVal() { Value = st.Value.value, FreqNode = newNode };
            }

            prevNode = newNode;
            nextNode = newNode;
        }

        m_sInfo = null;
    }
}