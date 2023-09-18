using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Konsarpoo.Collections.Stackalloc;

[StructLayout(LayoutKind.Auto)]
public ref struct MapStruct<TKey, TValue>
{
    private readonly Span<int> m_buckets;
    private readonly Span<Entry> m_entries;

    private int m_count;
    private int m_freeCount;
    private int m_freeList;

    private IEqualityComparer<TKey> m_comparer;
    
    private static TValue s_nullRef;
    
    public struct KeyEntry
    {
        public KeyEntry(int hashCode, int next, TKey key)
        {
            HashCode = hashCode;
            Next = next;
            Key = key;
        }

        internal int HashCode;
        internal int Next;
        internal TKey Key;
    }
    
    public struct Entry
    {
        public KeyEntry Key;
        public TValue Value;
    }

    public MapStruct(ref Span<int> buckets, ref Span<Entry> entries, IEqualityComparer<TKey> comparer)
    {
        m_buckets = buckets;
        m_entries = entries;
        m_comparer = comparer;
    }

    public MapStruct(ref Span<int> buckets, ref Span<Entry> entries)
    {
        m_buckets = buckets;
        m_entries = entries;
        m_comparer = EqualityComparer<TKey>.Default;
    }

    public int Count => m_count;

    public bool ContainsKey(TKey key)
    {
        ValueByRef(key, out var success);

        return success;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        value = ValueByRef(key, out var success);
        
        return success;
    }
    
    public bool Add(TKey key, TValue value)
    {
        var add = true;
        return Insert(ref key, ref value, ref add);
    }
    
    public bool TryUpdate(TKey key, TValue value)
    {
        var set = false;
        return Insert(ref key, ref value, ref set);
    }
    
    public void Keys(Action<TKey> onKey) 
    {
        var index = 0;

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                onKey(m_entries[index].Key.Key);
            }

            index++;
        }
    }
    
    public void Values(Action<TValue> onValue)
    {
        var index = 0;
        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                onValue(m_entries[index].Value);
            }

            index++;
        }
    }

    public void KeyValues(Action<KeyValuePair<TKey, TValue>> onKeyValue)
    {
        var index = 0;

        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                onKeyValue(new KeyValuePair<TKey, TValue>(m_entries[index].Key.Key, m_entries[index].Value));
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
}