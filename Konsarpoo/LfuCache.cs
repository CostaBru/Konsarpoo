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
    ICollection<KeyValuePair<TKey, TValue>>,
    IReadOnlyDictionary<TKey, TValue>, 
    IAppender<KeyValuePair<TKey, TValue>>,
    ISerializable, 
    IDeserializationCallback,
    IDisposable
{
    public class DataVal
    {
        public TValue Value;
        public FreqNode FreqNode;
    }
   
    public class FreqNode
    {
        public Set<TKey> Keys;

        public FreqNode NextNode;
        public FreqNode PrevNode;

        public int FreqValue;

        public FreqNode(Set<TKey> setTemplate)
        {
            NextNode = this;
            PrevNode = this;
            Keys = new Set<TKey>(setTemplate);
        }
    }
    
    [NonSerialized]
    private Set<TKey> m_setTemplate;

    [NonSerialized]
    private IEqualityComparer<TKey> m_comparer;

    [NonSerialized]
    private Map<TKey, DataVal> m_map;

    [NonSerialized] 
    private FreqNode m_root;
        
    [NonSerialized]
    private static TValue s_nullRef;
    
    /// <summary>
    /// Default class constructor.
    /// </summary>
    public LfuCache() : this(0, 0, null)
    {
    }

    /// <summary>
    /// LfuCache class constructor with keys comparer parameter.
    /// </summary>
    /// <param name="maxSizeStorageNodeArray"></param>
    /// <param name="comparer"></param>
    /// <param name="capacity"></param>
    public LfuCache(int capacity, int maxSizeStorageNodeArray, IEqualityComparer<TKey> comparer)
    {
        m_comparer = comparer ?? EqualityComparer<TKey>.Default;
        m_setTemplate = new Set<TKey>(capacity, maxSizeStorageNodeArray, m_comparer);
        
        m_map = new(capacity, maxSizeStorageNodeArray, m_comparer);
        m_root = new(m_setTemplate);
    }
    
    /// <summary>
    /// LfuCache pool set up constructor.
    /// </summary>
    /// <param name="mapTemplate"></param>
    /// <param name="setTemplate"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public LfuCache([NotNull] Map<TKey, DataVal> mapTemplate, [NotNull] Set<TKey> setTemplate)
    {
        if (mapTemplate == null)
        {
            throw new ArgumentNullException(nameof(mapTemplate));
        }

        if (setTemplate == null)
        {
            throw new ArgumentNullException(nameof(setTemplate));
        }
        
        m_comparer = mapTemplate.Comparer;

        m_map = new Map<TKey, DataVal>(mapTemplate);
        m_map.Clear();

        m_setTemplate = new (setTemplate, m_comparer);
        m_setTemplate.Clear();
        
        m_root = new(m_setTemplate);
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key)
    {
        return m_map.ContainsKey(key);
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key in a cache.
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

            throw new KeyNotFoundException($"Key '{key}' was not found in cache.");
        }
        set
        {
            AddOrUpdate(key, value);
        }
    }

    /// <inheritdoc />
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => m_map.Keys;

    /// <inheritdoc />0
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => m_map.Values.Select(v => v.Value);

    private void AccessItem(TKey key, DataVal data, TValue value, bool hasValue)
    {
        var freqNode = data.FreqNode;

        var nextNode = freqNode.NextNode;

        if (nextNode.FreqValue != freqNode.FreqValue + 1)
        {
            nextNode = new FreqNode(m_setTemplate)
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
                firstNode = new FreqNode(m_setTemplate) { FreqValue = 1, PrevNode = m_root, NextNode = firstNode };

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

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        this.AddOrUpdate(item.Key, item.Value);
    }

    /// <summary>
    /// Clears LFU cache.
    /// </summary>
    public void Clear()
    {
        RemoveLfuItems(Count);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        if (this.TryGetValue(item.Key, out var itemVal))
        {
            return EqualityComparer<TValue>.Default.Equals(itemVal , item.Value);
        }

        return false;
    }

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if ((arrayIndex < 0) || (arrayIndex > array.Length))
        {
            throw new ArgumentOutOfRangeException("arrayIndex");
        }
        if ((array.Length - arrayIndex) < Count)
        {
            throw new ArgumentException();
        }
        
        foreach (var kVal in m_map)
        {
            array[arrayIndex] = new KeyValuePair<TKey, TValue>(kVal.Key, kVal.Value.Value);

            arrayIndex++;
        }
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        return RemoveKey(item.Key);
    }

    /// <summary>
    /// Copies keys to given collection.
    /// </summary>
    /// <param name="array"></param>
    /// <param name="arrayIndex"></param>
    public void CopyKeysTo(TKey[] array, int arrayIndex)
    {
        m_map.Keys.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Returns count of cached items.
    /// </summary>
    public int Count => m_map.Count;

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

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

                var keys = leastFreqUsedItemsNode.Keys.ToData();

                foreach (var key in keys)
                {
                    RemoveKey(key);
                    toRemove--;
                    if (toRemove <= 0)
                    {
                        break;
                    }
                }
                
                keys.Dispose();
            }
            
            return removedCount;
        }
        else
        {
            var leastFreqUsedItemsNode = m_root.NextNode;

            var keys = leastFreqUsedItemsNode.Keys.ToData();

            int removedCount = 0;
            foreach (var key in keys)
            {
                RemoveKey(key);
                removedCount++;
            }

            return removedCount;
        }
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
        m_setTemplate.Dispose();
    }

    /// <summary> Returns true if given cache has the same keys, values and frequencies otherwise it returns false.</summary>
    /// <returns></returns>
    public bool DeepEquals([CanBeNull] LfuCache<TKey, TValue> lfuCache, IEqualityComparer<TValue> valueComparer = null)
    {
        if (lfuCache == null)
        {
            return false;
        }
        
        if (Count != lfuCache.Count)
        {
            return false;
        }

        var equalityComparer = valueComparer ?? EqualityComparer<TValue>.Default;

        foreach (var kVal in m_map)
        {
            if (lfuCache.m_map.TryGetValue(kVal.Key, out var otherVal) == false)
            {
                return false;
            }

            if (equalityComparer.Equals(kVal.Value.Value, otherVal.Value) == false)
            {
                return false;
            }

            var thisValeFreq = kVal.Value.FreqNode.FreqValue;

            if (thisValeFreq != otherVal.FreqNode.FreqValue)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates a full copy of Lfu cache.
    /// </summary>
    /// <returns></returns>
    public LfuCache<TKey, TValue> Copy()
    {
        return SerializeHelper.Clone<LfuCache<TKey, TValue>>(this);
    }
}