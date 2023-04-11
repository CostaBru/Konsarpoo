using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

/// <summary>
/// An O(1) LFU cache eviction data structure.
/// <see ref="https://github.com/papers-we-love/papers-we-love/blob/main/caching/a-constant-algorithm-for-implementing-the-lfu-cache-eviction-scheme.pdf"/>
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
[DebuggerDisplay("Count = {Count}")]
[Serializable]
public partial class LfuCache<TKey, TValue> : 
    IReadOnlyDictionary<TKey, TValue>, 
    IAppender<KeyValuePair<TKey, TValue>>,
    IReadOnlyCollection<TKey>,
    ICollection<TKey>,
    ISerializable, 
    IDeserializationCallback,
    IDisposable
{
    [NonSerialized]
    private IEqualityComparer<TKey> m_comparer;

    [NonSerialized]
    private readonly Map<TKey, DataVal> m_map;

    [NonSerialized] private readonly FreqNode m_root;
    
    public LfuCache() : this(null)
    {
    }

    public LfuCache(IEqualityComparer<TKey> comparer)
    {
        m_comparer = comparer ?? EqualityComparer<TKey>.Default;

        m_map = new(m_comparer);
        m_root = new(m_comparer);
    }
    
    private class DataVal
    {
        public TValue Value;
        public FreqNode FreqNode;
    }
   
    private class FreqNode
    {
        public Set<TKey> Keys;

        public FreqNode NextNode;
        public FreqNode PrevNode;

        public int FreqValue;

        public FreqNode(IEqualityComparer<TKey> comparer)
        {
            NextNode = this;
            PrevNode = this;
            Keys = new Set<TKey>(comparer);
        }
    }


    /// <inheritdoc />
    public bool ContainsKey(TKey key)
    {
        return m_map.ContainsKey(key);
    }

    /// <summary>
    /// Access cached item.
    /// </summary>
    /// <returns></returns>
    public bool TryGetValue([NotNull] TKey key, out TValue value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        
        value = default;

        if (m_map.TryGetValue(key, out var data))
        {
            AccessItem(key, data, default, false);

            value = data.Value;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns key access frequency.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public int GetFrequency([NotNull] TKey key)
    {
        if (m_map.TryGetValue(key, out var data))
        {
           return data.FreqNode.FreqValue;
        }

        return 0;
    }
    
    [NonSerialized]
    private static TValue s_nullRef;

    /// <summary>
    /// Returns value by its reference using key given.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="success"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public ref TValue ValueByRef([NotNull] TKey key, out bool success)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        success = false;

        if (m_map.TryGetValue(key, out var data))
        {
            AccessItem(key, data, default, false);
            
            success = true;
            
            return ref data.Value;
        }

        return ref s_nullRef;
    }

    /// <inheritdoc />
    public TValue this[TKey key]
    {
        get
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"{key}");
        }
        set
        {
            AddOrUpdate(key, value);
        }
    }

    /// <inheritdoc />
    public IEnumerable<TKey> Keys => m_map.Keys;

    /// <inheritdoc />
    public IEnumerable<TValue> Values => m_map.Values.Select(v => v.Value);

    private void AccessItem(TKey key, DataVal data, TValue value, bool hasValue)
    {
        var freqNode = data.FreqNode;

        var nextNode = freqNode.NextNode;

        if (nextNode.FreqValue != freqNode.FreqValue + 1)
        {
            nextNode = new FreqNode(m_comparer)
            {
                NextNode = freqNode.NextNode,
                PrevNode = freqNode,
                FreqValue = freqNode.FreqValue + 1
            };

            freqNode.NextNode = nextNode;
        }

        freqNode.Keys.Remove(key);
        nextNode.Keys.Add(key);

        if (freqNode.Keys.Count == 0)
        {
            DeleteNode(freqNode);
            
            freqNode.Keys.Dispose();
        }

        data.FreqNode = nextNode;
        
        if (hasValue)
        {
            data.Value = value;
        }
    }

    private static void DeleteNode(FreqNode freqNode)
    {
        var prevNode = freqNode.PrevNode;

        prevNode.NextNode = freqNode.NextNode;
        freqNode.NextNode.PrevNode = prevNode;
    }

    /// <summary>
    /// Adds or updates cache item.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void AddOrUpdate(TKey key, TValue value)
    {
        if (m_map.TryGetValue(key, out var data))
        {
            AccessItem(key, data, value, true);
        }
        else
        {
            FreqNode firstNode = m_root.NextNode;
            
            if (m_root.NextNode.FreqValue != 1)
            {
                firstNode = new FreqNode(m_comparer) { FreqValue = 1, PrevNode = m_root, NextNode = firstNode };

                m_root.NextNode.PrevNode = firstNode;

                m_root.NextNode = firstNode;
            }

            firstNode.Keys.Add(key);
            
            m_map[key] = new DataVal() { Value = value, FreqNode = firstNode };
        }
    }

    /// <summary>
    /// Removes cache item.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool RemoveKey(TKey key)
    {
        if (m_map.TryGetValue(key, out var data))
        {
            data.FreqNode.Keys.Remove(key);
            
            if (data.FreqNode.Keys.Count == 0)
            {
                DeleteNode(data.FreqNode);
                
                data.FreqNode.Keys.Dispose();
            }

            m_map.Remove(key);

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Append(KeyValuePair<TKey, TValue> value)
    {
        AddOrUpdate(value.Key, value.Value);
    }

    void ICollection<TKey>.Add(TKey item)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public void Clear()
    {
        RemoveLfuItems(Count);
    }

    /// <inheritdoc />
    bool ICollection<TKey>.Contains(TKey key)
    {
        return m_map.ContainsKey(key);
    }

    /// <inheritdoc />
    public void CopyTo(TKey[] array, int arrayIndex)
    {
        m_map.Keys.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    bool ICollection<TKey>.Remove(TKey key)
    {
        return RemoveKey(key);
    }

    /// <summary>
    /// Returns count of cached items.
    /// </summary>
    public int Count => m_map.Count;
    
    /// <inheritdoc />
    bool ICollection<TKey>.IsReadOnly => false;

    /// <summary>
    /// Removes all least frequently used items from cache.
    /// </summary>
    public int RemoveLfuItems(int? count = null)
    {
        if (m_map.Count == 0)
        {
            return 0;
        }

        if (count.HasValue)
        {
            var toRemove = Math.Min(m_map.Count, count.Value);

            var removedCount = toRemove;
            
            while (toRemove > 0)
            {
                var leastFreqUsedItemsNode = m_root.NextNode;

                var keys = leastFreqUsedItemsNode.Keys.ToArray();

                foreach (var key in keys)
                {
                    RemoveKey(key);
                    toRemove--;
                    if (toRemove <= 0)
                    {
                        break;
                    }
                }
            }
            
            return removedCount;
        }
        else
        {
            var leastFreqUsedItemsNode = m_root.NextNode;

            var keys = leastFreqUsedItemsNode.Keys.ToArray();

            int removedCount = 0;
            foreach (var key in keys)
            {
                if (RemoveKey(key))
                {
                    removedCount++;
                }
            }

            return removedCount;
        }
    }

    /// <inheritdoc />
    IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
    {
        return m_map.Keys.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        var mapVersion = m_map.Version;

        foreach (var kVal in m_map)
        {
            if (mapVersion != m_map.Version)
            {
                throw new InvalidOperationException($"LfuCache collection was modified during enumeration.");
            }
            
            yield return new KeyValuePair<TKey, TValue>(kVal.Key, kVal.Value.Value);
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return m_map.Keys.GetEnumerator();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Clear();

        m_map.Dispose();
    }
}