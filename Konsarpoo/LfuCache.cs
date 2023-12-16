using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;
  
namespace Konsarpoo.Collections;
/// <summary>
/// An O(1) LFU cache eviction data structure with extra tracking of memory and/or key obsolescence features. 
/// <see ref="https://github.com/papers-we-love/papers-we-love/blob/main/caching/a-constant-algorithm-for-implementing-the-lfu-cache-eviction-scheme.pdf"/>
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
[DebuggerDisplay("Count = {Count}, MemLimit = {MemoryLimitTracking}, TotalMem = {TotalMemoryTracked}, Timeout = {m_obsolescenceTimeout}, ObsoleteCount = {ObsoleteKeysCount}")]
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
    private TimeSpan m_obsolescenceTimeout;
    public class DataVal
    {
        public TValue Value;
        public long AccessTickCount;
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
    private FreqNode m_mostFreqNode;
        
    [NonSerialized]
    private static TValue s_nullRef;
    private IStopwatch m_stopwatch;
    
    [NonSerialized]
    private Set<TKey> m_obsoleteKeys;
    [NonSerialized]
    private readonly Func<TValue, TValue> m_copyStrategy;
    
    [NonSerialized]
    private long m_memoryLimit;
    [NonSerialized]
    private long m_totalMemory;
    [NonSerialized]
    private Func<TKey, TValue, long> m_getMemoryEstimate;

    /// <summary>
    /// Gets currently tracking memory limit.
    /// </summary>
    public long MemoryLimitTracking => m_memoryLimit;
    
    /// <summary>
    /// Gets currently tracking total memory.
    /// </summary>
    public long TotalMemoryTracked => m_totalMemory;

    /// <summary>
    /// Gets a flag indicating weather cache is tracking memory.
    /// </summary>
    public bool IsTrackingMemory => m_memoryLimit > 0;
    
    /// <summary>
    /// Gets a flag indicating weather cache is tracking key obsolescence.
    /// </summary>
    public bool IsTrackingObsolescence => m_stopwatch != null;
    
    /// <summary>
    /// Gets number of obsolete keys.
    /// </summary>
    public int ObsoleteKeysCount => m_obsoleteKeys?.Count ?? 0;
    
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
    {
        m_comparer = comparer ?? EqualityComparer<TKey>.Default;
        m_setTemplate = new Set<TKey>(capacity, maxSizeStorageNodeArray, m_comparer);
        
        m_map = new(capacity, maxSizeStorageNodeArray, m_comparer);
        m_root = new(m_setTemplate);
        m_mostFreqNode = m_root;
        m_copyStrategy = copyStrategy;
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
        m_mostFreqNode = m_root;
    }
    
    /// <summary>
    /// Starts tracking obsolescence of data on access.
    /// </summary>
    public void StartTrackingObsolescence([NotNull] IStopwatch stopwatch, TimeSpan obsolescenceTime)
    {
        if (obsolescenceTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(obsolescenceTime));
        }
        
        m_obsolescenceTimeout = obsolescenceTime;
        
        m_stopwatch = stopwatch ?? throw new ArgumentNullException(nameof(stopwatch));
        if (m_obsoleteKeys == null)
        {
            m_obsoleteKeys = new Set<TKey>(m_setTemplate);
        }
        m_stopwatch.Start();
    }
    
    /// <summary>
    /// Stops tracking obsolescence of data on access.
    /// </summary>
    public void StopTrackingObsolescence()
    {
        m_obsolescenceTimeout = TimeSpan.FromTicks(-1);
        
        m_stopwatch?.Stop();
        m_stopwatch = null;
    }

    /// <summary>
    /// Gets a total memory used by cache with a given memory estimation function.
    /// </summary>
    /// <param name="getMemoryEstimate"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public long EstimateMemoryUsage([NotNull] Func<TKey, TValue, long> getMemoryEstimate)
    {
        if (getMemoryEstimate == null)
        {
            throw new ArgumentNullException(nameof(getMemoryEstimate));
        }
        
        long mem = 0;
        foreach (var kv in m_map)
        {
            mem += getMemoryEstimate(kv.Key, kv.Value.Value);
        }

        return mem;
    }

    /// <summary>
    /// Starts memory tracking of cached data on add/update. If limit is reached it tries to remote least used items from the cache or throws InsufficientMemoryException.
    /// </summary>
    /// <param name="memoryLimit"></param>
    /// <param name="getMemoryEstimate"></param>
    /// <exception cref="InsufficientMemoryException">If existing total memory is</exception>
    public void StartTrackingMemory(long memoryLimit, [NotNull] Func<TKey, TValue, long> getMemoryEstimate)
    {
        if (memoryLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(memoryLimit));
        }
        
        long mem = EstimateMemoryUsage(getMemoryEstimate);
    
        if (mem > memoryLimit)
        {
            throw new InsufficientMemoryException();
        }
        
        m_memoryLimit = memoryLimit;
        m_getMemoryEstimate = getMemoryEstimate ?? throw new ArgumentNullException(nameof(getMemoryEstimate));
        m_totalMemory = mem;
    }

    /// <summary>
    /// Stops memory tracking.
    /// </summary>
    public void StopTrackingMemory()
    {
        m_memoryLimit = 0;
        m_totalMemory = 0;
        m_getMemoryEstimate = null;
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
           return data.FreqNode.FreqValue;
        }
        return 0;
    }
    /// <summary>
    /// Returns key access time.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public TimeSpan GetLastAccessTime([NotNull] TKey key)
    {
        if (m_map.TryGetValue(key, out var data))
        {
            return TimeSpan.FromTicks(data.AccessTickCount);
        }
        return TimeSpan.Zero;
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

    /// <summary>
    /// Returns the actual buckets count. If the value is equal to values count resize will happen on text insert.
    /// </summary>
    public int BucketCount => m_map.BucketCount;

    /// <summary>
    /// Returns the bucket index for given key.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetBucketIndex([NotNull] ref TKey item)
    {
        return m_map.GetBucketIndex(ref item);
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
        
        if (nextNode.FreqValue > m_mostFreqNode.FreqValue)
        {
            m_mostFreqNode = nextNode;
        }
        data.FreqNode = nextNode;
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
    private void DeleteNode(FreqNode freqNode)
    {
        if (m_mostFreqNode == freqNode)
        {
            m_mostFreqNode = m_mostFreqNode.PrevNode;
        }
        
        var prevNode = freqNode.PrevNode;
        prevNode.NextNode = freqNode.NextNode;
        freqNode.NextNode.PrevNode = prevNode;
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
            if (m_memoryLimit > 0)
            {
                var oldSize = m_getMemoryEstimate(key, data.Value);
                var newSize = m_getMemoryEstimate(key, value);

                if (newSize > m_memoryLimit)
                {
                    throw new InsufficientMemoryException();
                }

                AccessItem(key, data, value, true);

                if(m_totalMemory - oldSize + newSize > m_memoryLimit)
                {
                    m_totalMemory -= oldSize;

                    TryRemoveKeysToAddNewItem(newSize);

                    m_totalMemory += newSize;
                }
                
                return false;
            }

            AccessItem(key, data, value, true);
            return false;
        }
        else
        {
            if (m_memoryLimit > 0)
            {
                var memoryEstimate = m_getMemoryEstimate(key, value);

                if (memoryEstimate > m_memoryLimit)
                {
                    throw new InsufficientMemoryException();
                }

                TryRemoveKeysToAddNewItem(memoryEstimate);

                m_totalMemory += memoryEstimate;
            }
            
            FreqNode firstNode = m_root.NextNode;
            
            if (m_root.NextNode.FreqValue != 1)
            {
                firstNode = new FreqNode(m_setTemplate) { FreqValue = 1, PrevNode = m_root, NextNode = firstNode };
                m_root.NextNode.PrevNode = firstNode;
                m_root.NextNode = firstNode;
                
                if (firstNode.FreqValue > m_mostFreqNode.FreqValue)
                {
                    m_mostFreqNode = firstNode;
                }
            }
            firstNode.Keys.Add(key);
            var dataVal = new DataVal() { Value = m_copyStrategy != null ? m_copyStrategy(value) : value, FreqNode = firstNode };
            
            if (m_stopwatch != null)
            {
                dataVal.AccessTickCount = m_stopwatch.ElapsedTicks;
            }
            
            m_map[key] = dataVal;
            return true;
        }
    }

    private void TryRemoveKeysToAddNewItem(long memoryEstimate)
    {
        if (m_totalMemory + memoryEstimate > m_memoryLimit)
        {
            if (IsTrackingObsolescence)
            {
                RemoveObsoleteItem();

                if (m_totalMemory + memoryEstimate > m_memoryLimit)
                {
                    RemoveObsoleteItems();
                }
            }
        }

        while (m_totalMemory + memoryEstimate > m_memoryLimit)
        {
            RemoveLeastUsedItems(1);
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
            if (m_memoryLimit > 0)
            {
                var memoryEstimate = m_getMemoryEstimate(key, data.Value);

                m_totalMemory -= memoryEstimate;
            }
            
            data.FreqNode.Keys.Remove(key);
            
            if (data.FreqNode.Keys.Count == 0)
            {
                DeleteNode(data.FreqNode);
                
                data.FreqNode.Keys.Dispose();
            }
            m_map.Remove(key);
            m_obsoleteKeys?.Remove(key);
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
            foreach (var nodeKey in node.Keys)
            {
                var accessTickCount = m_map[nodeKey].AccessTickCount;
                
                if (stopwatchElapsedTicks - m_obsolescenceTimeout.Ticks > accessTickCount)
                {
                    m_obsoleteKeys.Add(nodeKey);
                }
                
                counter++;
                if (counter >= count)
                {
                    break;
                }
            }
            
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
        m_obsoleteKeys?.Clear();
        foreach (var dataVal in m_map)
        {
            dataVal.Value.AccessTickCount = 0;
        }
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
    /// Removes single obsolete item from cache. The value will be disposed if value is inherited from IDisposable interface and copy strategy is set.
    /// </summary>
    /// <returns></returns>
    public bool RemoveObsoleteItem()
    {
        if (m_map.Count == 0)
        {
            return false;
        }

        if (m_obsoleteKeys.Count == 0)
        {
            return false;
        }

        var key = m_obsoleteKeys.First();

        var removeKey = RemoveKey(key);

        m_obsoleteKeys.Remove(key);
        
        return removeKey;
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
    /// <summary>
    /// Disposes allocated resources. Each value will be disposed if value is inherited from IDisposable interface and copy strategy is set.
    /// </summary>
    public void Dispose()
    {
        Clear();
        m_obsoleteKeys?.Dispose();
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