using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections.Stackalloc;

public ref struct SetStruct<TKey>
{
    internal readonly Span<int> m_buckets;
    internal readonly Span<KeyEntryStruct<TKey>> m_entries;

    internal int m_count;
    private int m_freeCount;
    private int m_freeList;

    private IEqualityComparer<TKey> m_comparer;

    public SetStruct(ref Span<int> buckets, ref Span<KeyEntryStruct<TKey>> entries, IEqualityComparer<TKey> comparer)
    {
        m_buckets = buckets;
        m_entries = entries;
        m_comparer = comparer;
    }

    public SetStruct(ref Span<int> buckets, ref Span<KeyEntryStruct<TKey>> entries)
    {
        m_buckets = buckets;
        m_entries = entries;
        m_comparer = EqualityComparer<TKey>.Default;
    }

    public int Count => m_count;
    public double Length => m_count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TKey[] ToArray()
    {
        var index = 0;

        var dataAllocatorSetup = ArrayPoolAllocatorSetup.GetDataAllocatorSetup<TKey>();
        
        var kv = new Data<TKey>(dataAllocatorSetup);

        while (index < m_count)
        {
            if (m_entries[index].HashCode >= 0)
            {
                kv.Add(m_entries[index].Key);
            }

            index++;
        }

        var array = kv.ToArray();
        
        kv.Dispose();
        
        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
    {
        return Contains(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TKey value)
    {
        if (Contains(key))
        {
            value = key;

            return true;
        }

        value = default;

        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(TKey key)
    {
        var add = true;
        return Insert(ref key);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddRange(ref SetStruct<TKey> value)
    {
        if (m_count >= m_buckets.Length)
        {
            return false;
        }

        var valueCount = value.m_count;
        
        if (valueCount + m_count > m_buckets.Length)
        {
            return false;
        }
        
        var index = 0;

        while (index < value.m_count)
        {
            if (value.m_entries[index].HashCode >= 0)
            {
               Insert(ref value.m_entries[index].Key);
            }

            index++;
        }

        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddRange(ref DataStruct<TKey> value)
    {
        if (m_count >= m_buckets.Length)
        {
            return false;
        }

        var valueCount = value.m_count;
        
        if (valueCount + m_count > m_buckets.Length)
        {
            return false;
        }
        
        for (int i = 0; i < value.m_count; i++)
        {
            Insert(ref value.m_buffer[i]);
        }

        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddRange(IReadOnlyList<TKey> value)
    {
        if (m_count >= m_buckets.Length)
        {
            return false;
        }

        var valueCount = value.Count;
        
        if (valueCount + m_count > m_buckets.Length)
        {
            return false;
        }

        foreach (var v in value)
        {
            var tKey = v;
            Insert(ref tKey);
        }

        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TKey KeyAt(int keyIndex)
    {
        var index = 0;

        int ki = 0;

        while (index < m_count)
        {
            if (m_entries[index].HashCode >= 0)
            {
                if (ki == keyIndex)
                {
                    return m_entries[index].Key;
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
            if (m_entries[index].HashCode >= 0)
            {
                select(m_entries[index].Key);
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
            if (m_entries[index].HashCode >= 0)
            {
                select(pass, m_entries[index].Key);
            }

            index++;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TKey? FirstOrDefault()
    {
        if (m_count > 0)
        {
            var index = 0;
            while (index < m_count)
            {
                if (m_entries[index].HashCode >= 0)
                {
                    return m_entries[index].Key;
                }

                index++;
            }
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TKey? LastOrDefault()
    {
        if (m_count > 0)
        {
            var index = m_count - 1;
            while (index >= 0)
            {
                if (m_entries[index].HashCode >= 0)
                {
                    return m_entries[index].Key;
                }

                index--;
            }
        }

        return default;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereAggregate(Func<TKey, bool> where, Action<TKey> select)
    {
        var index = 0;
        while (index < m_count)
        {
            if (m_entries[index].HashCode >= 0)
            {
                if (where(m_entries[index].Key))
                {
                    select(m_entries[index].Key);
                }
            }

            index++;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereAggregate<W>(W pass, Func<W, TKey, bool> where, Action<W, TKey> select)
    {
        var index = 0;
        while (index < m_count)
        {
            if (m_entries[index].HashCode >= 0)
            {
                if (where(pass, m_entries[index].Key))
                {
                    select(pass, m_entries[index].Key);
                }
            }

            index++;
        }
    }
 
    public TKey this[TKey key]
    {
        get
        {
            if (ContainsKey(key))
            {
                return key;
            }
                
            throw new KeyNotFoundException($"Key '{key}' was not found.");
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains([NotNull] TKey key)
    {
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
                
                if ((kv.HashCode == hashCode) && m_comparer.Equals(kv.Key, key))
                {
                    return true;
                }

                i = kv.Next;
            }
        }
     
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Insert([NotNull] ref TKey key)
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
            if (hashCode == kv.HashCode && m_comparer.Equals(kv.Key, key))
            {
                return true;
            }

            i = kv.Next;
        }

        if (m_freeCount > 0)
        {
            freeList = m_freeList;
            m_freeList = m_entries[freeList].Next;
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
        
        kvf.HashCode = hashCode;
        kvf.Next = bucket;
        kvf.Key = key;

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

                if ((keyEntry.HashCode == hashCode) && m_comparer.Equals(keyEntry.Key, key))
                {
                    if (last < 0)
                    {
                        buckets[index] = keyEntry.Next + 1;
                    }
                    else
                    {
                        entries[last].Next = keyEntry.Next;
                    }

                    entries[i] = new KeyEntryStruct<TKey>(-1, m_freeList, default);
                    m_freeList = i;
                    m_freeCount++;

                    return true;
                }

                last = i;
                i = keyEntry.Next;
            }

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
    public Set<TKey> ToSet(ISetAllocatorSetup<TKey> allocatorSetup = null)
    {
        var set = new Set<TKey>(allocatorSetup, m_comparer);

        var index = 0;
        while (index < m_count)
        {
            if (m_entries[index].HashCode >= 0)
            {
                set.Add(m_entries[index].Key);
            }

            index++;
        }

        return set;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Data<TKey> ToData(IDataAllocatorSetup<TKey> allocatorSetup = null)
    {
        var data = new Data<TKey>(allocatorSetup);

        var index = 0;
        while (index < m_count)
        {
            if (m_entries[index].HashCode >= 0)
            {
                data.Add(m_entries[index].Key);
            }

            index++;
        }

        return data;
    }
}