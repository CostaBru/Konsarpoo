﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections.Stackalloc;

[StructLayout(LayoutKind.Auto)]
public ref struct MapRs<TKey, TValue>
{
    private readonly Span<int> m_buckets;
    private readonly Span<Entry> m_entries;

    private int m_count;
    private int m_freeCount;
    private int m_freeList;

    private IEqualityComparer<TKey> m_comparer;
    
    private static TValue s_nullRef;
    
    public struct Entry
    {
        public KeyEntryStruct<TKey> Key;
        public TValue Value;
    }

    public MapRs(ref Span<int> buckets, ref Span<Entry> entries, IEqualityComparer<TKey> comparer)
    {
        m_buckets = buckets;
        m_entries = entries;
        m_comparer = comparer;
    }

    public MapRs(ref Span<int> buckets, ref Span<Entry> entries)
    {
        m_buckets = buckets;
        m_entries = entries;
        m_comparer = EqualityComparer<TKey>.Default;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KeyEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new(m_buckets, m_entries,  m_count);
   
    public ref struct KeyEnumerator<T>
    {
        private readonly Span<int> m_buckets;
        private readonly Span<Entry> m_entries;
        
        private readonly int m_count;
        private int m_index = -1;

        public KeyEnumerator(Span<int> buckets, Span<Entry> entries, int count)
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

    public int Count => m_count;
    public double Length => m_count;
    
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

    public Data<TKey> Keys => GetKeys();
    public Data<TValue> Values => GetValues();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
    {
        ValueByRef(key, out var success);

        return success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        value = ValueByRef(key, out var success);
        
        return success;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(TKey key, TValue value)
    {
        var add = true;
        return Insert(ref key, ref value, ref add);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryUpdate(TKey key, TValue value)
    {
        var set = false;
        return Insert(ref key, ref value, ref set);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool? ContainsValue(TValue val, IEqualityComparer<TValue> comparer = null)
    {
        var cmp = comparer ?? EqualityComparer<TValue>.Default;

        var index = 0;

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                if (cmp.Equals(val, m_entries[index].Value))
                {
                    return true;
                }
            }

            index++;
        }

        return false;
    }
  
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TKey KeyAt(int keyIndex)
    {
        var index = 0;

        int ki = 0;

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                if (ki == keyIndex)
                {
                    return m_entries[index].Key.Key;
                }

                ki++;
            }

            index++;
        }

        throw new IndexOutOfRangeException();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AggregateKeys(Action<TKey> select) 
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
    public void AggregateValues(Action<TValue> select) 
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AggregateValues<W>(W pass, Action<W, TValue> select) 
    {
        var index = 0;

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                select(pass, m_entries[index].Value);
            }

            index++;
        }
    }

    
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KeyValuePair<TKey, TValue>? FirstOrDefault()
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

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KeyValuePair<TKey, TValue>? LastOrDefault()
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

        return default;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereAggregate(Func<TKey, TValue, bool> where, Action<TKey, TValue> select)
    {
        var index = 0;
        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                if (where(m_entries[index].Key.Key, m_entries[index].Value))
                {
                    select(m_entries[index].Key.Key, m_entries[index].Value);
                }
            }

            index++;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereAggregate<W>(W pass, Func<W, TKey, TValue, bool> where, Action<W, TKey, TValue> select)
    {
        var index = 0;
        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                if (where(pass, m_entries[index].Key.Key, m_entries[index].Value))
                {
                    select(pass, m_entries[index].Key.Key, m_entries[index].Value);
                }
            }

            index++;
        }
    }
 
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
    }

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
            var hashCode = m_comparer.GetHashCode(key) & 0x7fffffff;
            for (var i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0;)
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
    private bool Insert([NotNull] ref TKey key, ref TValue value, ref bool add)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        int freeList;

        int hashCode = m_comparer.GetHashCode(key) & 0x7fffffff;

        int index = hashCode % m_buckets.Length;

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

                return true;
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
                return false;
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

        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        if (m_count > 0)
        {
            int hashCode = m_comparer.GetHashCode(key) & 0x7fffffff;

            int index = hashCode % m_count;
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

                    entries[i] = new Entry() { Key = new KeyEntryStruct<TKey>(-1, m_freeList, default) };
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        m_buckets.Clear();
        m_entries.Clear();

        m_freeList = -1;
        m_count = 0;
        m_freeCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd(TKey key, TValue val)
    {
        if (ContainsKey(key))
        {
            return false;
        }
        
        var set = false;
        return Insert(ref key, ref val, ref set);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetOrDefault(TKey key, TValue defaultVal = default)
    {
        if(TryGetValue(key, out var val))
        {
            return val;
        }

        return defaultVal;
    }
}