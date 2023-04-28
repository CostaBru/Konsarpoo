﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

/// <summary>
/// An O(1) LFU cache eviction data structure plus simple key obsolescence tracking/cleaning. Uses predefined frequency buckets to avoid un
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
    [NonSerialized]
    private TimeSpan m_obsolescenceData;
    
    private static readonly int[] m_fibonachi = new int[] { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597, 2584, 4181, 6765, 10946, 17711, 28657, 46368, 75025, 121393, 196418, 317811 };

    public class DataVal
    {
        public TValue Value;
        public long AccessTickCount;
        public int AccessCount;
        public FreqNode FreqNode;
    }
   
    public class FreqNode
    {
        public ICollection<TKey> Keys;

        public FreqNode NextNode;
        public FreqNode PrevNode;

        public int FreqValue;

        public FreqNode(ICollection<TKey> collection)
        {
            NextNode = this;
            PrevNode = this;
            Keys = collection;
        }
    }

    [NonSerialized]
    private IDictionary<TKey, DataVal> m_map;

    [NonSerialized] 
    private FreqNode m_root;
    
    [NonSerialized] 
    private FreqNode m_mostFreqNode;
        
    [NonSerialized]
    private static TValue s_nullRef;

    private IStopwatch m_stopwatch;
    
    [NonSerialized]
    private ICollection<TKey> m_obsoleteKeys;

    [NonSerialized]
    private readonly Func<TValue, TValue> m_copyStrategy;

    /// <summary>
    /// Default class constructor.
    /// </summary>
    public LfuCache() : this(0, 0, null)
    {
    }
    
    /// <summary>
    /// Class constructor accepting predefined freq buckets.
    /// </summary>
    public LfuCache(int[] freqBuckets) : this(0, 0, null, freqBuckets, null)
    {
    }

    /// <summary>
    /// LfuCache class constructor with keys comparer parameter.
    /// </summary>
    /// <param name="maxSizeStorageNodeArray"></param>
    /// <param name="comparer"></param>
    /// <param name="capacity"></param>
    public LfuCache(int capacity, int maxSizeStorageNodeArray, IEqualityComparer<TKey> comparer) : this(capacity, maxSizeStorageNodeArray, comparer, null)
    {
    }

    /// <summary>
    /// LfuCache class constructor with value copy strategy.
    /// </summary>
    /// <param name="copyStrategy"></param>
    public LfuCache(Func<TValue, TValue> copyStrategy): this(0,0,null, copyStrategy)
    {
    }

    /// <summary>
    /// LfuCache class constructor with keys comparer parameter.
    /// </summary>
    /// <param name="maxSizeStorageNodeArray"></param>
    /// <param name="comparer"></param>
    /// <param name="capacity"></param>
    /// <param name="copyStrategy"></param>
    public LfuCache(int capacity, int maxSizeStorageNodeArray, IEqualityComparer<TKey> comparer, Func<TValue, TValue> copyStrategy = null) 
        : this(capacity, maxSizeStorageNodeArray, comparer, null, null, copyStrategy)
    {
    }

    /// <summary>
    /// LfuCache class constructor with keys comparer parameter.
    /// </summary>
    /// <param name="maxSizeStorageNodeArray"></param>
    /// <param name="comparer"></param>
    /// <param name="capacity"></param>
    /// <param name="freqRanges"></param>
    /// <param name="setFactory"></param>
    /// <param name="copyStrategy"></param>
    public LfuCache(int capacity, int maxSizeStorageNodeArray, IEqualityComparer<TKey> comparer, int[] freqRanges, Func<ICollection<TKey>> setFactory, Func<TValue, TValue> copyStrategy = null)
    {
        Func<ICollection<TKey>> defFactory = () => new Set<TKey>();
        
        m_map = new Map<TKey, DataVal>(capacity, maxSizeStorageNodeArray, comparer);
        m_root = new FreqNode(setFactory?.Invoke() ?? defFactory());
        m_mostFreqNode = m_root;
        m_copyStrategy = copyStrategy;
        m_obsoleteKeys = setFactory?.Invoke() ?? defFactory();

        CreateFreqBuckets(freqRanges, setFactory ?? defFactory);
    }

    private void CreateFreqBuckets(int[] freqRanges, Func<ICollection<TKey>> setFactory)
    {
        var ranges = freqRanges ?? m_fibonachi;

        var prevNode = m_root;

        for (int i = 0; i < ranges.Length; i++)
        {
            var freqNode = new FreqNode(setFactory());

            freqNode.FreqValue = ranges[i];
            freqNode.PrevNode = prevNode;

            prevNode.NextNode = freqNode;

            prevNode = freqNode;
        }
    }

    /// <summary>
    /// LfuCache pool set up constructor.
    /// </summary>
    /// <param name="map"></param>
    /// <param name="setFactory"></param>
    /// <param name="freqRanges"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public LfuCache([NotNull] IDictionary<TKey, DataVal> map, [NotNull] Func<ICollection<TKey>> setFactory, int[] freqRanges = null)
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        if (setFactory == null)
        {
            throw new ArgumentNullException(nameof(setFactory));
        }

        m_map = map;
        
        m_root = new(setFactory());
        m_mostFreqNode = m_root;
        m_obsoleteKeys = setFactory();
        
        CreateFreqBuckets(freqRanges, setFactory);
    }

    /// <summary>
    /// Starts tracking obsolescence of data on access.
    /// </summary>
    public void StartTrackingObsolescence(IStopwatch stopwatch, TimeSpan obsolescenceTime)
    {
        m_obsolescenceData = obsolescenceTime;
        
        m_stopwatch = stopwatch;

        m_stopwatch.Start();
    }

    /// <summary>
    /// Stops tracking obsolescence of data on access.
    /// </summary>
    public void StopTrackingObsolescence()
    {
        m_obsolescenceData = TimeSpan.FromTicks(-1);
        
        m_stopwatch?.Stop();

        m_stopwatch = null;
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
    public bool Get([NotNull] TKey key, out TValue value)
    {
        return TryGetValue(key, out value);
    }
    
    /// <summary>
    /// Attempts to get the value associated with the specified key in a cache if value missing call missingValue delegate.
    /// </summary>
    /// <returns></returns>
    public TValue GetSet([NotNull] TKey key, Func<TKey, LfuCache<TKey, TValue>, TValue> missingValue)
    {
        if (TryGetValue(key, out var value))
        {
            return value;
        }

        return missingValue(key, this);
    }
    
    /// <summary>
    /// Attempts to get the value associated with the specified key in a cache if value missing call missingValue delegate.
    /// </summary>
    /// <returns></returns>
    public TValue GetSet<TParam>([NotNull] TKey key, TParam p1, Func<TParam, TKey, LfuCache<TKey, TValue>, TValue> missingValue)
    {
        if (TryGetValue(key, out var value))
        {
            return value;
        }

        return missingValue(p1, key, this);
    }
    
    /// <summary>
    /// Attempts to get the value associated with the specified key in a cache if value missing call missingValue delegate.
    /// </summary>
    /// <returns></returns>
    public TValue GetSet<TParam1, TParam2>([NotNull] TKey key, TParam1 p1, TParam2 p2,  Func<TParam1, TParam2, TKey, LfuCache<TKey, TValue>, TValue> missingValue)
    {
        if (TryGetValue(key, out var value))
        {
            return value;
        }

        return missingValue(p1, p2, key, this);
    }
    
    /// <summary>
    /// Attempts to get the value associated with the specified key in a cache if value missing call missingValue delegate.
    /// </summary>
    /// <returns></returns>
    public TValue GetSet<TParam1, TParam2, TParam3>([NotNull] TKey key, TParam1 p1, TParam2 p2, TParam3 p3, Func<TParam1, TParam2, TParam3, TKey, LfuCache<TKey, TValue>, TValue> missingValue)
    {
        if (TryGetValue(key, out var value))
        {
            return value;
        }

        return missingValue(p1, p2, p3, key, this);
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

            if (m_copyStrategy != null)
            {
                value = m_copyStrategy(data.Value);
            }
            else
            {
                value = data.Value;
            }

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
           return data.AccessCount;
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
        lock (data)
        {
            var freqNode = data.FreqNode;

            data.AccessCount++;

            if (data.AccessCount > freqNode.FreqValue)
            {
                var nextNode = freqNode.NextNode;

                if (ReferenceEquals(nextNode, freqNode) == false)
                {
                    freqNode.Keys.Remove(key);
                    
                    if (freqNode.Keys.Count == 0)
                    {
                        DeleteNode(freqNode);
                    }
                    
                    nextNode.Keys.Add(key);

                    if (nextNode.FreqValue > m_mostFreqNode.FreqValue)
                    {
                        m_mostFreqNode = nextNode;
                    }

                    data.FreqNode = nextNode;
                }
            }

            if (m_stopwatch != null)
            {
                data.AccessTickCount = m_stopwatch.ElapsedTicks;

                m_obsoleteKeys.Remove(key);
            }

            if (hasValue)
            {
                if (m_copyStrategy == null)
                {
                    data.Value = value;
                }
                else
                {
                    data.Value = m_copyStrategy(value);
                }
            }
        }
    }

    private void DeleteNode(FreqNode freqNode)
    {
        if (m_mostFreqNode == freqNode)
        {
            m_mostFreqNode = m_mostFreqNode.PrevNode;
        }
        
        freqNode.Keys.Clear();
    }

    /// <summary>
    /// Adds or updates cache item.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public TValue Put(TKey key, TValue value)
    {
        AddOrUpdate(key, value);

        return value;
    }
    
    /// <summary>
    /// Adds or updates cache item.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>true if item added, false if item updated.</returns>
    public bool AddOrUpdate(TKey key, TValue value)
    {
        if (m_map.TryGetValue(key, out var data))
        {
            AccessItem(key, data, value, true);

            return false;
        }
        else
        {
            FreqNode firstNode = m_root.NextNode;
            
            if (firstNode.FreqValue > m_mostFreqNode.FreqValue)
            {
                m_mostFreqNode = firstNode;
            }

            var dataVal = new DataVal() { Value = m_copyStrategy != null ? m_copyStrategy(value) : value, FreqNode = firstNode, AccessCount = 1 };
            
            if (m_stopwatch != null)
            {
                dataVal.AccessTickCount = m_stopwatch.ElapsedTicks;
            }
            
            firstNode.Keys.Add(key);

            m_map[key] = dataVal;

            return true;
        }
    }

    /// <summary>
    /// Removes cache item. Value will be disposed if value is inherited from IDisposable interface and copy strategy is set.
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
            }

            m_map.Remove(key);
            m_obsoleteKeys.Remove(key);

            if (m_copyStrategy != null && data.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }

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
    /// Clears LFU cache. Each value will be disposed if value is inherited from IDisposable interface and copy strategy is set.
    /// </summary>
    public void Clear()
    {
        RemoveLeastUsedItems(Count);
        
        m_obsoleteKeys.Clear();
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
    /// Scans accessed items for obsolescence.
    /// </summary>
    public int ScanForObsolescence()
    {
        return ScanFrequentForObsolescence(Count);
    }

    /// <summary>
    /// Gets number of obsolete keys.
    /// </summary>
    public int ObsoleteKeysCount => m_obsoleteKeys?.Count ?? 0;
    
    /// <summary>
    /// Scans top N most freq accessed items for obsolescence.
    /// </summary>
    /// <param name="count"></param>
    public int ScanFrequentForObsolescence(int count)
    {
        if (m_stopwatch == null || count <= 0)
        {
            return 0;
        }
        
        var stopwatchElapsedTicks = m_stopwatch.ElapsedTicks;

        var node = m_mostFreqNode;

        int counter = 0;

        var obsoleteKeysCount = m_obsoleteKeys.Count;

        while (m_root != node)
        {
            var nodeKeys = node.Keys.ToData();
            
            foreach (var nodeKey in nodeKeys)
            {
                if (m_map.TryGetValue(nodeKey, out var nodeVal))
                {
                    long accessTickCount = 0;

                    lock (nodeVal)
                    {
                        accessTickCount = nodeVal.AccessTickCount;
                    }

                    if (stopwatchElapsedTicks - m_obsolescenceData.Ticks > accessTickCount)
                    {
                        m_obsoleteKeys.Add(nodeKey);
                    }

                    counter++;

                    if (counter >= count)
                    {
                        break;
                    }
                }
            }
            
            nodeKeys.Dispose();
            
            if (counter >= count)
            {
                break;
            }
            
            node = node.PrevNode;
        }

        return m_obsoleteKeys.Count - obsoleteKeysCount;
    }

    /// <summary>
    /// Resets keys obsolescence.
    /// </summary>
    public void ResetObsolescence()
    {
        m_obsoleteKeys.Clear();

        var data = m_map.ToData();

        foreach (var dataVal in data)
        {
            lock (dataVal.Value)
            {
                dataVal.Value.AccessTickCount = 0;
            }
        }
        
        data.Dispose();
    }
    
    /// <summary>
    /// Removes obsolete items from cache. Each value will be disposed if value is inherited from IDisposable interface and copy strategy is set.
    /// </summary>
    /// <returns></returns>
    public int RemoveObsoleteItems()
    {
        if (m_map.Count == 0)
        {
            return 0;
        }

        if (m_obsoleteKeys.Count == 0)
        {
            return 0;
        }

        int removedCount = 0;
        
        var keys = m_obsoleteKeys.ToData();
        
        foreach (var key in keys)
        {
            RemoveKey(key);
            removedCount++;
        }
        
        keys.Dispose();
        
        m_obsoleteKeys.Clear();

        return removedCount;
    }

    /// <summary>
    /// Removes all least frequently used items from cache. Each value will be disposed if value is inherited from IDisposable interface and copy strategy is set.
    /// </summary>
    public int RemoveLeastUsedItems(int? count = null)
    {
        if (m_map.Count == 0)
        {
            return 0;
        }

        if (count.HasValue)
        {
            var toRemove = Math.Min(m_map.Count, count.Value);

            var removedCount = toRemove;
            
            var leastFreqUsedItemsNode = m_root.NextNode;

            while (toRemove > 0)
            {
                if (leastFreqUsedItemsNode.Keys.Count > 0)
                {
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

                leastFreqUsedItemsNode = leastFreqUsedItemsNode.NextNode;
            }
            
            return removedCount;
        }
        else
        {
            var leastFreqUsedItemsNode = m_root.NextNode;

            while (leastFreqUsedItemsNode.Keys.Count == 0)
            {
                leastFreqUsedItemsNode = leastFreqUsedItemsNode.NextNode;
            }

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
        foreach (var kVal in m_map)
        {
            yield return new KeyValuePair<TKey, TValue>(kVal.Key, kVal.Value.Value);
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return m_map.Keys.GetEnumerator();
    }

    /// <summary>
    /// Disposes allocated resources. Each value will be disposed if value is inherited from IDisposable interface and copy strategy is set.
    /// </summary>
    public void Dispose()
    {
        Clear();

        if (m_obsoleteKeys is IDisposable dk)
        {
            dk.Dispose();
        }
        
        if (m_map is IDisposable dm)
        {
            dm.Dispose();
        }
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

            var thisValeFreq = kVal.Value.AccessCount;

            if (thisValeFreq != otherVal.AccessCount)
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