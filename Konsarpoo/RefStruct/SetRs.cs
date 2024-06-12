using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections.Stackalloc;

/// <summary>
/// Represents a distinct set of values built on generic contiguous memory Span of T. 
/// </summary>
/// <typeparam name="T"></typeparam>
[StructLayout(LayoutKind.Auto)]
public ref struct SetRs<T>
{
    internal readonly Span<int> m_buckets;
    internal readonly Span<KeyEntry<T>> m_entries;

    private readonly int m_hashCount;
    
    internal int m_count;
    private int m_freeCount;
    private int m_freeList;

    private IEqualityComparer<T> m_comparer;

    /// <summary>
    /// Constructor takes stack allocated storage and equalityComparer.
    /// </summary>
    /// <param name="buckets"></param>
    /// <param name="entries"></param>
    /// <param name="comparer"></param>
    public SetRs(ref Span<int> buckets, ref Span<KeyEntry<T>> entries, IEqualityComparer<T> comparer)
    {
        m_buckets = buckets;
        m_entries = entries;
        m_comparer = comparer;
        m_hashCount = buckets.Length;
    }

    /// <summary>
    /// Constructor takes stack allocated storage.
    /// </summary>
    /// <param name="buckets"></param>
    /// <param name="entries"></param>
    public SetRs(ref Span<int> buckets, ref Span<KeyEntry<T>> entries)
    {
        m_buckets = buckets;
        m_entries = entries;
        m_comparer = EqualityComparer<T>.Default;
        m_hashCount = buckets.Length;
    }

    /// <summary>
    /// Constructor that fills out container with predefined data.
    /// </summary>
    /// <param name="buckets"></param>
    /// <param name="entries"></param>
    /// <param name="hashCount"></param>
    /// <param name="count"></param>
    public SetRs(int[] buckets, KeyEntry<T>[] entries, int hashCount, int count, IEqualityComparer<T> comparer)
    {
        m_buckets = new Span<int>(buckets);
        m_entries = new Span<KeyEntry<T>>(entries);
        m_comparer = comparer ?? EqualityComparer<T>.Default;
        m_hashCount = hashCount;
        m_count = count;
    }
    
    /// <summary>
    /// Allows enumerate set.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SetRsEnumerator GetEnumerator() => new(m_buckets, m_entries,  m_count);
    
    /// <summary>
    /// Allows to enumerate contents. 
    /// </summary>
    /// <returns></returns>
    public RsEnumerator<T, T> GetRsEnumerator() => new RsEnumerator<T, T>(new SetRsEnumerator(m_buckets, m_entries,  m_count));
   
    public ref struct SetRsEnumerator
    {
        internal readonly Span<int> m_buckets;
        internal readonly Span<KeyEntry<T>> m_entries;
        private readonly int m_count;
        private int m_index = -1;

        public SetRsEnumerator(Span<int> buckets, Span<KeyEntry<T>> entries, int count)
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
                if (m_entries[index].HashCode >= 0)
                {
                    m_index = index;

                    return true;
                }

                index++;
            }

            return false;
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_entries[m_index].Key;
        }

        public int Count => m_count;
    }

    /// <summary>
    /// Gets the number of values contained in the SetRs&lt;TKey,TValue&gt;.
    /// </summary>
    public int Count => m_count;
    
    /// <summary>
    /// Array API. Gets the number of values contained in the Set&lt;T&gt;.
    /// </summary>
    public double Length => m_count;

    /// <summary>
    /// Copies set to a new array.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ToArray()
    {
        var index = 0;

        var dataAllocatorSetup = ArrayPoolAllocatorSetup.GetDataAllocatorSetup<T>();
        
        var kv = new Data<T>(dataAllocatorSetup);

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

    /// <summary>
    /// Map api: Checks whether given key exist in the set.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(T key)
    {
        return Contains(key);
    }

    /// <summary>
    /// Set Map API &lt;TKey, bool&gt; Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>True in case of success.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(T key, out bool value)
    {
        if (Contains(key))
        {
            value = true;

            return true;
        }

        value = false;

        return false;
    }
    
    /// <summary>Adds the specified element to a set.</summary>
    /// <param name="value">The element to add to the set.</param>
    /// <returns>
    /// <see langword="true" /> if the element is added to the set object; <see langword="false" /> if the element is already present.</returns>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(T key)
    {
        return Insert(ref key);
    }
   
    private void CheckCanAdd(int valueCount)
    {
        if (m_count >= m_buckets.Length)
        {
            throw new InsufficientMemoryException(
                $"Cannot add the {valueCount} of new items to the StackRs container. The {m_buckets.Length} maximum reached.");
        }

        if (valueCount + m_count > m_buckets.Length)
        {
            throw new InsufficientMemoryException(
                $"Cannot add the {valueCount} of new items to the StackRs container. The {m_buckets.Length} is a maximum.");
        }
    }
    
    /// <summary>
    /// Adds a bunch of new items to the DataRs.
    /// </summary>
    /// <param name="list"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddRange(ref SetRs<T> list)
    {
        CheckCanAdd(list.Count);
        
        var index = 0;

        while (index < list.m_count)
        {
            if (list.m_entries[index].HashCode >= 0)
            {
               Insert(ref list.m_entries[index].Key);
            }

            index++;
        }

        return true;
    }
    
    /// <summary>
    /// Adds a bunch of new items to the DataRs.
    /// </summary>
    /// <param name="list"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddRange(ref DataRs<T> list)
    {
        CheckCanAdd(list.Count);
        
        for (int i = 0; i < list.m_count; i++)
        {
            Insert(ref list.m_buffer[i]);
        }

        return true;
    }
    
    /// <summary>
    /// Adds a bunch of new items to the DataRs.
    /// </summary>
    /// <param name="list"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddRange([NotNull] IReadOnlyList<T> list)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }
        
        CheckCanAdd(list.Count);

        foreach (var v in list)
        {
            var tKey = v;
            Insert(ref tKey);
        }

        return true;
    }

    /// <summary>
    /// Inefficient way to get key by its index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T KeyAt(int index)
    {
        var i = 0;

        int ki = 0;

        while (i < m_count)
        {
            if (m_entries[i].HashCode >= 0)
            {
                if (ki == index)
                {
                    return m_entries[i].Key;
                }

                ki++;
            }

            i++;
        }

        throw new IndexOutOfRangeException();
    }
    
    /// <summary>
    /// Calls the given onValue action for each item in list.
    /// </summary>
    /// <param name="onValue"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ForEach(Action<T> onValue) 
    {
        var index = 0;

        while (index < m_count)
        {
            if (m_entries[index].HashCode >= 0)
            {
                onValue(m_entries[index].Key);
            }

            index++;
        }
    }

    /// <summary>
    /// Calls given onValue action for each value in list and pass the given target to it. 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="onValue"></param>
    /// <typeparam name="W"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Aggregate<W>(W target, Action<W, T> onValue) 
    {
        var index = 0;

        while (index < m_count)
        {
            if (m_entries[index].HashCode >= 0)
            {
                onValue(target, m_entries[index].Key);
            }

            index++;
        }
    }
    
    /// <summary>
    /// Returns first or default item in list.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T FirstOrDefault(T defaultVal = default)
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

        return defaultVal;
    }

    /// <summary>
    /// Returns last or default item in list.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T LastOrDefault(T defaultVal = default)
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

        return defaultVal;
    }
    
    /// <summary>
    /// Calls given onValue action for each value that meets where condition in list. 
    /// </summary>
    /// <param name="where"></param>
    /// <param name="onValue"></param>
    /// <typeparam name="T"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereForEach(Func<T, bool> where, Action<T> onValue)
    {
        var index = 0;
        while (index < m_count)
        {
            if (m_entries[index].HashCode >= 0)
            {
                if (where(m_entries[index].Key))
                {
                    onValue(m_entries[index].Key);
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
    /// <param name="onValue"></param>
    /// <typeparam name="W"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereAggregate<W>(W target, Func<W, T, bool> where, Action<W, T> onValue)
    {
        var index = 0;
        while (index < m_count)
        {
            if (m_entries[index].HashCode >= 0)
            {
                if (where(target, m_entries[index].Key))
                {
                    onValue(target, m_entries[index].Key);
                }
            }

            index++;
        }
    }
 
    /// <summary>
    /// Set Map API &lt;TKey, bool&gt;. Gets or sets value in set.
    /// </summary>
    /// <param name="key"></param>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="InsufficientMemoryException"></exception>
    public T this[T key]
    {
        get
        {
            if (ContainsKey(key))
            {
                return key;
            }
                
            throw new KeyNotFoundException($"Key '{key}' was not found.");
        }
        set
        {
            Add(key);
        }
    }
    
    /// <summary>
    /// Determines whether the SetRs&lt;T&gt; contains the item.
    /// </summary>
    /// <param name="item"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains([NotNull] T item)
    {
        if (m_count > 0)
        {
            var hashCode = m_comparer.GetHashCode(item) & int.MaxValue;
            for (var i = m_buckets[hashCode % m_hashCount] - 1; i >= 0;)
            {
                ref var kv = ref m_entries[i];
                
                if ((kv.HashCode == hashCode) && m_comparer.Equals(kv.Key, item))
                {
                    return true;
                }

                i = kv.Next;
            }
        }
     
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Insert([NotNull] ref T key)
    {
        int freeList;

        int hashCode = m_comparer.GetHashCode(key) & int.MaxValue;

        int index = hashCode % m_hashCount;

        var bucket = m_buckets[index] - 1;

        for (int i = bucket; i >= 0;)
        {
            ref var kv = ref m_entries[i];
            if (hashCode == kv.HashCode && m_comparer.Equals(kv.Key, key))
            {
                return false;
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
                throw new InsufficientMemoryException($"Cannot add a new item to the SetRs container. The {m_entries.Length} maximum reached.");
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
    
    /// <summary>
    /// Removes the item from the Set&lt;T&gt;.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T key)
    {
        if (m_count > 0)
        {
            int hashCode = m_comparer.GetHashCode(key) & int.MaxValue;

            int index = hashCode % m_hashCount;
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

                    entries[i] = new KeyEntry<T>(-1, m_freeList, default);
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

    /// <summary>Removes all elements from a SetRs container.</summary>
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
    /// Copies all contents to a new Set container.
    /// </summary>
    /// <param name="allocatorSetup"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Set<T> ToSet(ISetAllocatorSetup<T> allocatorSetup = null)
    {
        var set = new Set<T>(allocatorSetup, m_comparer);

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
    
    /// <summary>
    /// Copies all contents to a new Data container.
    /// </summary>
    /// <param name="allocatorSetup"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Data<T> ToData(IDataAllocatorSetup<T> allocatorSetup = null)
    {
        var data = new Data<T>(allocatorSetup);

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