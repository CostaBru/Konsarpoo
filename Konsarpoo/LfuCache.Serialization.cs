using System;
using System.Collections.Concurrent;
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

        var valuePairs = new Data<KeyValuePair<TKey, (TValue value, int frequency, int accessCount)>>(m_map.Count);

        valuePairs.AddRange(m_map.Select(kv => new KeyValuePair<TKey, (TValue value, int frequency, int accessCount)>(kv.Key, (kv.Value.Value, kv.Value.FreqNode.FreqValue, kv.Value.AccessCount))));

        info.AddValue(StorageName, valuePairs, typeof(Data<KeyValuePair<TKey,(TValue value, int frequency, int accessCount)>>));

        if (m_map is Map<TKey, DataVal> mp)
        {
            info.AddValue(ComparerName, mp.Comparer, typeof(IEqualityComparer<TKey>));
        }
        
        if (m_map is Dictionary<TKey, DataVal> mdp)
        {
            info.AddValue(ComparerName, mdp.Comparer, typeof(IEqualityComparer<TKey>));
        }
    }

    /// <inheritdoc />
    public void OnDeserialization(object sender)
    {
        if (m_sInfo == null)
        {
            return;
        }

        var siInfo = m_sInfo;

        var storage = (Data<KeyValuePair<TKey, (TValue value, int frequency, int accessCount)>>)siInfo.GetValue(StorageName, typeof(Data<KeyValuePair<TKey, (TValue value, int frequency, int accessCount)>>));

        if (storage is null)
        {
            throw new SerializationException("Cannot read lfu cache storage from serialization info.");
        }

        storage.OnDeserialization(this);
        
        var comparer = (IEqualityComparer<TKey>)siInfo.GetValue(ComparerName, typeof(IEqualityComparer<TKey>));

        if (m_root == null)
        {
            m_root = new FreqNode(new Set<TKey>(comparer));
        }

        if (m_map == null)
        {
            m_map = new Map<TKey, DataVal>(comparer);
        }
       
        lock (m_root)
        {
            FreqNode prevNode = m_root;
            FreqNode nextNode = m_root.NextNode;

            foreach (var kv in storage.GroupBy(kv => kv.Value.frequency).OrderBy(r => r.Key))
            {
                var newNode =
                    new FreqNode(new Set<TKey>(comparer))
                    {
                        FreqValue = kv.Key,
                        PrevNode = prevNode,
                        NextNode = nextNode
                    };

                m_root.NextNode.PrevNode = newNode;
                m_root.NextNode = newNode;

                foreach (var st in kv)
                {
                    newNode.Keys.Add(st.Key);
                    m_map[st.Key] = new DataVal() { Value = st.Value.value, FreqNode = newNode, AccessCount = st.Value.accessCount };
                }

                prevNode = newNode;
                nextNode = newNode;
            }
        }

        m_sInfo = null;
    }
}