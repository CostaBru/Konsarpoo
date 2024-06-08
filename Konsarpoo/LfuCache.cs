using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public ICollection<TKey> Keys;
        public FreqNode NextNode;
        public FreqNode PrevNode;
        public int FreqValue;
        public FreqNode(Func<ICollection<TKey>> setFactory)
        {
            NextNode = this;
            PrevNode = this;
            Keys = setFactory();
        }
    }
    
    [NonSerialized]
    private Func<ICollection<TKey>> m_setFactory;
    [NonSerialized]
    private IEqualityComparer<TKey> m_comparer;
    
    /// <summary> Storage of key and nodes. </summary>
    [NonSerialized]
    protected IDictionary<TKey, DataVal> MapStorage;
    
    [NonSerialized] 
    private FreqNode m_root;
    
    [NonSerialized] 
    private FreqNode m_mostFreqNode;
        
    [NonSerialized]
    private static TValue s_nullRef;
    
    [NonSerialized]
    private IStopwatch m_stopwatch;
    
    [NonSerialized]
    private ICollection<TKey> m_obsoleteKeys;
    
    [NonSerialized]
    private readonly Func<TValue, TValue> m_copyStrategy;
    
    [NonSerialized]
    private readonly Action<TKey, TValue> m_disposingStrategy;
    
    [NonSerialized]
    private long m_memoryLimit;
    [NonSerialized]
    private long m_totalMemory;
    [NonSerialized]
    private Func<TKey, TValue, long> m_getMemoryEstimate;

    private int m_version = 0;

    /// <summary>
    /// Gets currently tracking memory limit.
    /// </summary>
    public long MemoryLimitTracking => m_memoryLimit;
    
    /// <summary>
    /// Gets currently tracking total memory.
    /// </summary>
    public long TotalMemoryTracked => m_totalMemory;

    /// <summary>
    /// Gets the flag that indicates whether cache is tracking memory.
    /// </summary>
    public bool IsTrackingMemory => m_memoryLimit > 0;
    
    /// <summary>
    /// Gets the flag that indicates whether cache is tracking key obsolescence.
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
    /// LfuCache class constructor with value copy strategy abd disposing.
    /// </summary>
    /// <param name="copyStrategy"></param>
    /// <param name="disposingStrategy"></param>
    public LfuCache(Func<TValue, TValue> copyStrategy, Action<TKey, TValue> disposingStrategy = null): this(0,0,null, copyStrategy, disposingStrategy)
    {
    }

    /// <summary>
    /// LfuCache class constructor with keys comparer parameter.
    /// </summary>
    /// <param name="maxSizeStorageNodeArray"></param>
    /// <param name="comparer"></param>
    /// <param name="capacity"></param>
    /// <param name="copyStrategy"></param>
    /// <param name="disposingStrategy"></param>
    public LfuCache(int capacity, int maxSizeStorageNodeArray, IEqualityComparer<TKey> comparer, Func<TValue, TValue> copyStrategy = null,  Action<TKey, TValue> disposingStrategy = null)
    {
        m_comparer = comparer ?? EqualityComparer<TKey>.Default;
        m_setFactory = () => new Set<TKey>(capacity, maxSizeStorageNodeArray, m_comparer);

        MapStorage = new Map<TKey, DataVal>(capacity, maxSizeStorageNodeArray, m_comparer);
        m_root = new(m_setFactory);
        m_mostFreqNode = m_root;
        m_copyStrategy = copyStrategy;
        m_disposingStrategy = disposingStrategy;
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
        MapStorage = new Map<TKey, DataVal>(mapTemplate);
        MapStorage.Clear();
        m_setFactory = () => new Set<TKey>(setTemplate, m_comparer);
        m_root = new(m_setFactory);
        m_mostFreqNode = m_root;
    }
    
    /// <summary>
    /// LfuCache constructor with customizable storage.
    /// </summary>
    /// <param name="mapStorage"></param>
    /// <param name="setFactory"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public LfuCache([NotNull]IDictionary<TKey, DataVal> mapStorage, [NotNull] Func<ICollection<TKey>> setFactory)
    {
        if (mapStorage == null)
        {
            throw new ArgumentNullException(nameof(mapStorage));
        }
        if (setFactory == null)
        {
            throw new ArgumentNullException(nameof(setFactory));
        }
        
        m_comparer = EqualityComparer<TKey>.Default;
        MapStorage = mapStorage;
        m_setFactory = setFactory;
        
        m_root = new(m_setFactory);
        m_mostFreqNode = m_root;
    }

    /// <summary>
    /// Starts tracking obsolescence of data on access.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
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
            m_obsoleteKeys = m_setFactory();
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
        foreach (var kv in MapStorage)
        {
            mem += getMemoryEstimate(kv.Key, kv.Value.Value);
        }

        return mem;
    }

    /// <summary>
    /// Starts memory tracking of cached data on add/update. If limit has reached then it tries to remote obsolete and least used items from the cache. Add\Update can throw InsufficientMemoryException if new item is greater than memory limit.
    /// </summary>
    /// <param name="memoryLimit"></param>
    /// <param name="getMemoryEstimate"></param>
    /// <exception cref="InsufficientMemoryException">If total memory occupied by existing items is less than given memory limit.</exception>
    public void StartTrackingMemory(long memoryLimit, [NotNull] Func<TKey, TValue, long> getMemoryEstimate)
    {
        if (memoryLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(memoryLimit));
        }
        
        long mem = EstimateMemoryUsage(getMemoryEstimate);
    
        if (mem > memoryLimit)
        {
            throw new InsufficientMemoryException($"Total memory {mem} occupied by existing items is less than given memory limit {memoryLimit}.");
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
        return MapStorage.ContainsKey(key);
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
        value = default;
        if (MapStorage.TryGetValue(key, out var data))
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
        if (MapStorage.TryGetValue(key, out var data))
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
        if (MapStorage.TryGetValue(key, out var data))
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
        success = false;
        if (MapStorage.TryGetValue(key, out var data))
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
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => MapStorage.Keys;
    
  
    /// <inheritdoc />0
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => MapStorage.Values.Select(v => v.Value);
    private void AccessItem(TKey key, DataVal data, TValue value, bool hasValue)
    {
        var freqNode = data.FreqNode;
        var nextNode = freqNode.NextNode;
        
        if (nextNode.FreqValue != freqNode.FreqValue + 1)
        {
            nextNode = new FreqNode(m_setFactory)
            {
                FreqValue = freqNode.FreqValue + 1
            };
            
            InsertNodeAfter(freqNode, nextNode);
        }
        
        freqNode.Keys.Remove(key);
        if (freqNode.Keys.Count == 0)
        {
            DeleteNode(freqNode);

            if (freqNode.Keys is IDisposable d)
            {
                d.Dispose();
            }
        }
        
        nextNode.Keys.Add(key);
        
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
        var nextNode = freqNode.NextNode;
        
        prevNode.NextNode = nextNode;
        nextNode.PrevNode = prevNode;

        freqNode.NextNode = null;
        freqNode.PrevNode = null;
    }
    
    private void InsertNodeAfter(FreqNode curNode, FreqNode newNode)
    {
        var nextNode = curNode.NextNode;
        
        curNode.NextNode = newNode;
        nextNode.PrevNode = newNode;

        newNode.PrevNode = curNode;
        newNode.NextNode = nextNode;
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
        if (MapStorage.TryGetValue(key, out var data))
        {
            if (m_memoryLimit > 0)
            {
                var oldSize = m_getMemoryEstimate(key, data.Value);
                var newSize = m_getMemoryEstimate(key, value);

                if (newSize > m_memoryLimit)
                {
                    throw new InsufficientMemoryException($"Updated key '{key}' estimated memory '{newSize}' is greater than tracking memory limit '{m_memoryLimit}'.");
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
                var newSize = m_getMemoryEstimate(key, value);

                if (newSize > m_memoryLimit)
                {
                    throw new InsufficientMemoryException($"New key '{key}' estimated memory '{newSize}' is greater than tracking memory limit '{m_memoryLimit}'.");
                }

                TryRemoveKeysToAddNewItem(newSize);

                m_totalMemory += newSize;
            }
            
            FreqNode firstNode = m_root.NextNode;
            
            if (m_root.NextNode.FreqValue != 1)
            {
                firstNode = new FreqNode(m_setFactory) { FreqValue = 1 };

                InsertNodeAfter(m_root, firstNode);
                
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
            
            MapStorage[key] = dataVal;

            unchecked { m_version++; }

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
            var removeLeastUsedItems = RemoveLeastUsedItems(1);

            if (removeLeastUsedItems <= 0)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Removes cache item. Value will be disposed if value is inherited from IDisposable interface and copy strategy is set.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool RemoveKey(TKey key)
    {
        if (MapStorage.TryGetValue(key, out var data))
        {
            if (m_memoryLimit > 0)
            {
                var memoryEstimate = m_getMemoryEstimate(key, data.Value);

                m_totalMemory -= memoryEstimate;
            }
            
            if (m_disposingStrategy != null)
            {
                m_disposingStrategy(key, data.Value);
            }

            var freqNode = data.FreqNode;
            
            freqNode.Keys.Remove(key);
            
            if (freqNode.Keys.Count == 0)
            {
                DeleteNode(freqNode);

                if (freqNode.Keys is IDisposable d)
                {
                    d.Dispose();
                }
            }
            
            if (m_disposingStrategy == null && m_copyStrategy != null && data.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            m_obsoleteKeys?.Remove(key);
            MapStorage.Remove(key);
            
            unchecked { m_version++; }

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
        
        foreach (var kVal in MapStorage)
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
        MapStorage.Keys.CopyTo(array, arrayIndex);
    }
    
    /// <summary>
    /// Returns count of cached items.
    /// </summary>
    public int Count => MapStorage.Count;
    
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
                var accessTickCount = MapStorage[nodeKey].AccessTickCount;
                
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
        foreach (var dataVal in MapStorage)
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
        if (MapStorage.Count == 0)
        {
            return 0;
        }
        if (m_obsoleteKeys.Count == 0)
        {
            return 0;
        }
        int removedCount = 0;

        var keys = m_obsoleteKeys;
            
        while (keys.Count > 0)
        {
            var key = keys.First();

            if (RemoveKey(key))
            {
                removedCount++;
            }
            
            keys.Remove(key);
        }
        
        return removedCount;
    }

    /// <summary>
    /// Removes single obsolete item from cache. The value will be disposed if value is inherited from IDisposable interface and copy strategy is set.
    /// </summary>
    /// <returns></returns>
    public bool RemoveObsoleteItem()
    {
        if (MapStorage.Count == 0)
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

    internal bool IsBrokenFreqList()
    {
        var workingNode = m_root.NextNode;

        while (workingNode != null)
        {
            workingNode = workingNode.NextNode;

            if (workingNode.PrevNode == null || workingNode.PrevNode.NextNode == null || workingNode.PrevNode.PrevNode == null)
            {
                return true;
            }

            if (workingNode.NextNode == null)
            {
                return true;
            }

            if (ReferenceEquals(workingNode, m_root))
            {
                break;
            }
        }

        return workingNode == null;
    }
    
    /// <summary>
    /// Removes all least frequently used items from cache. Each value will be disposed if value is inherited from IDisposable interface and copy strategy is set.
    /// </summary>
    /// <param name="count">if is not set it removes all min least used items.</param>
    public int RemoveLeastUsedItems(int? count = null)
    {
        if (MapStorage.Count == 0)
        {
            return 0;
        }
        if (count.HasValue)
        {
            var toRemove = Math.Min(MapStorage.Count, count.Value);
            var removedCount = toRemove;
            
            var workingNode = m_root.NextNode;
            
            while (toRemove > 0 && workingNode != m_root)
            {
                var nextNode = workingNode.NextNode;

                var keys = workingNode.Keys;
                
                while (keys.Count > 0 && toRemove > 0)
                {
                    var key = keys.First();
                    
                    if (RemoveKey(key))
                    {
                        toRemove--;
                    }
                    keys.Remove(key);
                }

                if (workingNode.NextNode != null && keys.Count == 0)
                {
                    DeleteNode(workingNode);
                }

                workingNode = nextNode;
            }
            
            unchecked { m_version++; }
            
            return removedCount;
        }
        else
        {
            var freqNode = m_root.NextNode;
            
            while (freqNode.Keys.Count == 0 && freqNode != m_root)
            {
                var nextNode = freqNode.NextNode;
                freqNode = nextNode;
            }

            int removedCount = 0;

            var keys = freqNode.Keys;
            
            while (keys.Count > 0)
            {
                var key = keys.First();

                if (RemoveKey(key))
                {
                    removedCount++;
                }
                
                keys.Remove(key);
            }

            if (freqNode.NextNode != null && freqNode.Keys.Count == 0)
            {
                DeleteNode(freqNode);
            }
            
            unchecked { m_version++; }

            return removedCount;
        }
    }
    
    /// <inheritdoc />
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        var mapVersion = m_version;
        foreach (var kVal in MapStorage)
        {
            if (mapVersion != m_version)
            {
                throw new InvalidOperationException($"LfuCache collection was modified during enumeration.");
            }
            
            yield return new KeyValuePair<TKey, TValue>(kVal.Key, kVal.Value.Value);
        }
    }
    
    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return MapStorage.Keys.GetEnumerator();
    }
    
    /// <summary>
    /// Disposes allocated resources. Each value will be disposed if value is inherited from IDisposable interface and copy strategy is set.
    /// </summary>
    public void Dispose()
    {
        Clear();
        if (m_obsoleteKeys is IDisposable disp)
        {
            disp.Dispose();
        }
        if (MapStorage is IDisposable d)
        {
            d.Dispose();
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
        foreach (var kVal in MapStorage)
        {
            if (lfuCache.MapStorage.TryGetValue(kVal.Key, out var otherVal) == false)
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