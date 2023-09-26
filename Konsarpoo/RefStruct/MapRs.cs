using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections.Stackalloc;

/// <summary>
/// The MapRs&lt;TKey,TValue&gt; generic class provides a mapping from a set of keys to a set of values.
/// Each addition to the Map consists of a value and its associated key.
/// Retrieving a value by using its key is very fast, close to O(1), because the MapRs&lt;TKey,TValue&gt; class is implemented as a hash table on generic contiguous memory Span of T.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
[StructLayout(LayoutKind.Auto)]
public ref struct MapRs<TKey, TValue>
{
    private const int HashCoef = Map<TKey, TValue>.HashCoef;
    private readonly Span<int> m_buckets;
    private readonly Span<Entry<TKey, TValue>> m_entries;
    private readonly int m_hashCount;

    private int m_count;
    private int m_freeCount;
    private int m_freeList;

    private IEqualityComparer<TKey> m_comparer;
    
    private static TValue s_nullRef;

    /// <summary>
    /// Constructor that takes stack allocated storage and equality comparer.
    /// </summary>
    /// <param name="buckets"></param>
    /// <param name="entries"></param>
    /// <param name="comparer"></param>
    public MapRs(ref Span<int> buckets, ref Span<Entry<TKey, TValue>> entries, IEqualityComparer<TKey> comparer = null)
    {
        m_buckets = buckets;
        m_entries = entries;
        m_comparer = comparer ?? EqualityComparer<TKey>.Default;
        m_hashCount = m_buckets.Length;
    }

    /// <summary>
    /// Constructor that fills out container with predefined data.
    /// </summary>
    /// <param name="buckets"></param>
    /// <param name="entries"></param>
    /// <param name="count"></param>
    /// <param name="hashCount"></param>
    /// <param name="comparer"></param>
    public MapRs(int[] buckets, Entry<TKey, TValue>[] entries, int hashCount, int count, IEqualityComparer<TKey> comparer)
    {
        m_buckets = new Span<int>(buckets);
        m_entries = new Span<Entry<TKey, TValue>>(entries);
        m_comparer = comparer ?? EqualityComparer<TKey>.Default;
        m_count = count;
        m_hashCount = hashCount;
    }

    /// <summary>
    /// Constructor that takes stack allocated storage.
    /// </summary>
    /// <param name="buckets"></param>
    /// <param name="entries"></param>
    /// <param name="comparer"></param>
    MapRs(ref Span<int> buckets, ref Span<Entry<TKey, TValue>> entries)
    {
        m_buckets = buckets;
        m_entries = entries;
        m_comparer = EqualityComparer<TKey>.Default;
        m_hashCount = m_buckets.Length;
    }
    
    /// <summary>
    /// Allows to enumerate key values.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MapRsKeyValueEnumerator GetEnumerator() => new MapRsKeyValueEnumerator(m_buckets, m_entries,  m_count);
   
    /// <summary>
    /// Allows to enumerate contents. 
    /// </summary>
    /// <returns></returns>
    public RsEnumerator<TKey, TValue> GetRsEnumerator() => new RsEnumerator<TKey, TValue>(new MapRsKeyEnumerator<TKey, TValue>(m_entries, m_count));
   
    public ref struct MapRsKeyValueEnumerator
    {
        private readonly Span<int> m_buckets;
        private readonly Span<Entry<TKey, TValue>> m_entries;
        
        private readonly int m_count;
        private int m_index = -1;

        public MapRsKeyValueEnumerator(Span<int> buckets, Span<Entry<TKey, TValue>> entries, int count)
        {
            m_buckets = buckets;
            m_entries = entries;
            m_count = count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = m_index + 1;
            
            while (index < m_count)
            {
                if (m_entries[index].Key.HashCode >= 0)
                {
                    m_index = index;

                    return true;
                }

                index++;
            }

            return false;
        }

        public KeyValuePair<TKey,TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new KeyValuePair<TKey,TValue>(m_entries[m_index].Key.Key, m_entries[m_index].Value);
        }
        
        public ref TKey CurrentKey
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_entries[m_index].Key.Key;
        }
        
        public ref TValue CurrentValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_entries[m_index].Value;
        }
    }
    
   

    /// <summary>
    /// Gets the number of key/value pairs contained in the Map&lt;TKey,TValue&gt;.
    /// </summary>
    public int Count => m_count;
    
    /// <summary>
    /// Array API. Gets the number of key/value pairs contained in the Map&lt;TKey,TValue&gt;.
    /// </summary>
    public double Length => m_count;
    
    /// <summary> Returns keys in Data container. </summary>
    /// <param name="allocatorSetup"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Data<TKey> GetKeys(IDataAllocatorSetup<TKey> allocatorSetup = null) 
    {
        var index = 0;

        var keys = new Data<TKey>(allocatorSetup);

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                keys.Add(m_entries[index].Key.Key);
            }

            index++;
        }

        return keys;
    }
    
    /// <summary>
    /// Returns values in data container.
    /// </summary>
    /// <param name="allocatorSetup"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Data<TValue> GetValues(IDataAllocatorSetup<TValue> allocatorSetup = null) 
    {
        var index = 0;

        var keys = new Data<TValue>(allocatorSetup);

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                keys.Add(m_entries[index].Value);
            }

            index++;
        }

        return keys;
    }

    /// <summary>
    /// Copies key value pairs to a new array.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KeyValuePair<TKey, TValue>[] ToArray()
    {
        var index = 0;

        var dataAllocatorSetup = ArrayPoolAllocatorSetup.GetDataAllocatorSetup<KeyValuePair<TKey, TValue>>();
        
        var kv = new Data<KeyValuePair<TKey, TValue>>(dataAllocatorSetup);

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                kv.Add(new KeyValuePair<TKey, TValue>(m_entries[index].Key.Key, m_entries[index].Value));
            }

            index++;
        }

        var keyValuePairs = kv.ToArray();
        
        kv.Dispose();
        
        return keyValuePairs;
    }
    
    /// <summary>
    /// Gets the keys in the MapRs&lt;TKey,TValue&gt;.
    /// </summary>
    public Data<TKey> Keys => GetKeys();
    
    /// <summary>
    /// Gets the values in the MapRs&lt;TKey,TValue&gt;. 
    /// </summary>
    public Data<TValue> Values => GetValues();

    /// <summary>
    /// Determines whether the Map&lt;TKey,TValue&gt; contains the specified key.
    /// </summary>
    /// <param name="key"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
    {
        ValueByRef(key, out var success);

        return success;
    }
    
    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>True in case of success.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        value = ValueByRef(key, out var success);
        
        return success;
    }
    
    /// <summary>
    /// Adds the specified key and value to the map.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey key, TValue value)
    {
        var add = true;
        Insert(ref key, ref value, ref add);
    }

    /// <summary>
    /// Determines whether the Map&lt;TKey,TValue&gt; contains the specified value.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="comparer"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool? ContainsValue(TValue value, IEqualityComparer<TValue> comparer = null)
    {
        var cmp = comparer ?? EqualityComparer<TValue>.Default;

        var index = 0;

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                if (cmp.Equals(value, m_entries[index].Value))
                {
                    return true;
                }
            }

            index++;
        }

        return false;
    }
  
    /// <summary>
    /// Inefficient way to get key by its index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TKey KeyAt(int index)
    {
        var i = 0;

        int ki = 0;

        while (i < m_count)
        {
            if (m_entries[i].Key.HashCode >= 0)
            {
                if (ki == index)
                {
                    return m_entries[i].Key.Key;
                }

                ki++;
            }

            i++;
        }

        throw new IndexOutOfRangeException();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ForEachKey(Action<TKey> select) 
    {
        var index = 0;

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                select(m_entries[index].Key.Key);
            }

            index++;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ForEachValue(Action<TValue> select) 
    {
        var index = 0;

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                select(m_entries[index].Value);
            }

            index++;
        }
    }
    
    /// <summary>
    /// Calls given onValue action for each value in map and pass the given target to it. 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="onValue"></param>
    /// <typeparam name="W"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AggregateValues<W>(W target, Action<W, TValue> onValue) 
    {
        var index = 0;

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                onValue(target, m_entries[index].Value);
            }

            index++;
        }
    }

    /// <summary>
    /// Calls given onValue action for each key in map and pass the given target to it. 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="onValue"></param>
    /// <typeparam name="W"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AggregateKeys<W>(W pass, Action<W, TKey> select) 
    {
        var index = 0;

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                select(pass, m_entries[index].Key.Key);
            }

            index++;
        }
    }
    
    /// <summary>
    /// Returns first or default key value pair in map.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KeyValuePair<TKey, TValue> FirstOrDefault(KeyValuePair<TKey, TValue> def = default)
    {
        if (m_count > 0)
        {
            var index = 0;
            while (index < m_count)
            {
                if (m_entries[index].Key.HashCode >= 0)
                {
                    return new KeyValuePair<TKey, TValue>(m_entries[index].Key.Key, m_entries[index].Value);
                }

                index++;
            }
        }

        return def;
    }

    /// <summary>
    /// Returns last or default key value pair in map.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KeyValuePair<TKey, TValue>? LastOrDefault(KeyValuePair<TKey, TValue> def = default)
    {
        if (m_count > 0)
        {
            var index = m_count - 1;
            while (index >= 0)
            {
                if (m_entries[index].Key.HashCode >= 0)
                {
                    return new KeyValuePair<TKey, TValue>(m_entries[index].Key.Key, m_entries[index].Value);
                }

                index--;
            }
        }

        return def;
    }
    
    /// <summary>
    /// Calls given onValue action for each value that meets where condition in list. 
    /// </summary>
    /// <param name="where"></param>
    /// <param name="onValue"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereForEach(Func<TKey, TValue, bool> where, Action<TKey, TValue> onValue)
    {
        var index = 0;
        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                if (where(m_entries[index].Key.Key, m_entries[index].Value))
                {
                    onValue(m_entries[index].Key.Key, m_entries[index].Value);
                }
            }

            index++;
        }
    }

    /// <summary>
    /// Calls given onValue action for each value that meets where condition in list. 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="where"></param>
    /// <param name="onKeyValue"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereAggregate<W>(W target, Func<W, TKey, TValue, bool> where, Action<W, TKey, TValue> onKeyValue)
    {
        var index = 0;
        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                if (where(target, m_entries[index].Key.Key, m_entries[index].Value))
                {
                    onKeyValue(target, m_entries[index].Key.Key, m_entries[index].Value);
                }
            }

            index++;
        }
    }
 
    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InsufficientMemoryException"></exception>
    public TValue this[TKey key]
    {
        get
        {
            ref TValue value = ref ValueByRef(key, out var found);

            if (found)
            {
                return value;
            }
                
            throw new KeyNotFoundException($"Key '{key}' was not found.");
        }
        set
        {
            var set = false;
            Insert(ref key, ref value, ref set);
        }
    }

    /// <summary>
    /// Returns value by its reference using key given.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="success"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue ValueByRef([NotNull] TKey key, out bool success)
    {
        success = false;

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (m_count > 0)
        {
            var hashCode = m_comparer.GetHashCode(key) & HashCoef;
            var index = hashCode % m_hashCount;
            for (var i = m_buckets[index] - 1; i >= 0;)
            {
                ref var kv = ref m_entries[i];
                
                if ((kv.Key.HashCode == hashCode) && m_comparer.Equals(kv.Key.Key, key))
                {
                    success = true;

                    return ref kv.Value;
                }

                i = kv.Key.Next;
            }
        }

        s_nullRef = default;

        return ref s_nullRef;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Insert([NotNull] ref TKey key, ref TValue value, ref bool add)
    {
        int freeList;

        int hashCode = m_comparer.GetHashCode(key) & HashCoef;

        int index = hashCode % m_hashCount;

        var bucket = m_buckets[index] - 1;

        for (int i = bucket; i >= 0;)
        {
            ref var kv = ref m_entries[i];
            if (hashCode == kv.Key.HashCode && m_comparer.Equals(kv.Key.Key, key))
            {
                if (add)
                {
                    throw new ArgumentException($"Key '{key}' is already exists.");
                }

                kv.Value = value;
            }

            i = kv.Key.Next;
        }

        if (m_freeCount > 0)
        {
            freeList = m_freeList;
            m_freeList = m_entries[freeList].Key.Next;
            m_freeCount--;
        }
        else
        {
            if (m_count == m_entries.Length)
            {
                throw new InsufficientMemoryException($"Cannot add a new item to the MapRs container. The {m_entries.Length} maximum reached.");
            }

            freeList = m_count;
            m_count++;
        }

        ref var kvf = ref m_entries[freeList];
        
        kvf.Key.HashCode = hashCode;
        kvf.Key.Next = bucket;
        kvf.Key.Key = key;
        kvf.Value = value;

        m_buckets[index] = freeList + 1;
    }
    
    /// <summary>
    /// Removes the value with the specified key from the Map&lt;TKey,TValue&gt;.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        if (m_count > 0)
        {
            int hashCode = m_comparer.GetHashCode(key) & HashCoef;

            int index = hashCode % m_hashCount;
            int last = -1;

            var entries = m_entries;
            var buckets = m_buckets;

            for (int i = buckets[index] - 1; i >= 0;)
            {
                ref var keyEntry = ref entries[i];

                if ((keyEntry.Key.HashCode == hashCode) && m_comparer.Equals(keyEntry.Key.Key, key))
                {
                    if (last < 0)
                    {
                        buckets[index] = keyEntry.Key.Next + 1;
                    }
                    else
                    {
                        entries[last].Key.Next = keyEntry.Key.Next;
                    }

                    entries[i] = new Entry<TKey, TValue>() { Key = new KeyEntry<TKey>(-1, m_freeList, default) };
                    m_freeList = i;
                    m_freeCount++;

                    return true;
                }

                last = i;
                i = keyEntry.Key.Next;
            }

        }

        return false;
    }

    /// <summary>
    /// Copies MapRs to Map container.
    /// </summary>
    /// <param name="allocatorSetup"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map<TKey, TValue> ToMap(IMapAllocatorSetup<TKey, TValue> allocatorSetup = null)
    {
        var index = 0;

        var map = new Map<TKey, TValue>(allocatorSetup, m_comparer);

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                map.Add(m_entries[index].Key.Key, m_entries[index].Value);
            }

            index++;
        }

        return map;
    }

    /// <summary>
    /// Checks whether given key value pair exists in map.
    /// </summary>
    /// <param name="keyValuePair"></param>
    /// <param name="equalityComparer"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(KeyValuePair<TKey,TValue> keyValuePair, IEqualityComparer<TValue> equalityComparer = null)
    {
       if(TryGetValue(keyValuePair.Key, out var val))
       {
           var cmp = equalityComparer ?? EqualityComparer<TValue>.Default;
           
           return cmp.Equals(val, keyValuePair.Value);
       }

       return false;
    }
    
    /// <summary>
    /// Removes all contents.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        m_buckets.Clear();
        m_entries.Clear();

        m_freeList = -1;
        m_count = 0;
        m_freeCount = 0;
    }

    /// <summary>
    /// Gets the value for given key if exist or default value.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultVal"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetOrDefault(TKey key, TValue defaultVal = default)
    {
        if(TryGetValue(key, out var val))
        {
            return val;
        }

        return defaultVal;
    }
    
    /// <summary>
    /// Attempts to add the specified key and value to the map.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InsufficientMemoryException"></exception>
    public bool TryAdd(TKey key, TValue value)
    {
        if (ContainsKey(key))
        {
            return false;
        }

        this[key] = value;

        return true;
    }
}