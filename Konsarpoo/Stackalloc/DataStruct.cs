using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections.Stackalloc;

[StructLayout(LayoutKind.Auto)]
public ref struct DataStruct<T> 
{
    private Span<T> m_buffer;
    private int m_count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataStruct(ref Span<T> span)
    {
        m_buffer = span;
        m_count = 0;
    }
    
    public ref T this[int index]
    {
        get
        {
            if (index < 0 || index >= m_count)
            {
                throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_count}).");
            }
            
            return ref m_buffer[index];
        }
    }

    public int Count => m_count;
    public int Length => m_count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindIndex(T value)
    {
        return IndexOf(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(T value)
    {
        for (int i = 0; i < m_count; i++)
        {
            var obj = m_buffer[i];
            
            if (obj == null)
            {
                if (value == null)
                {
                    return i;
                }
            }
            else
            {
                if (obj.Equals(value))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindIndex(Func<T, bool> func)
    {
        return IndexOf(func);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(Func<T, bool> func)
    {
        for (int i = 0; i < m_count; i++)
        {
            if (func(m_buffer[i]))
            {
                return i;
            }
        }

        return -1;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindIndex<W>(W pass, Func<W, T, bool> func, int start = 0)
    {
        for (int i = start; i < m_count; i++)
        {
            if (func(pass, m_buffer[i]))
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindIndex<V>(V value, [NotNull] Func<T, V> valueSelector, int start = 0)
    {
        return FindIndex<V>(value, valueSelector, EqualityComparer<V>.Default, start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindIndex<V>(V value, [NotNull] Func<T, V> valueSelector, IEqualityComparer<V> equalityComparer, int start = 0)
    {
        for (int i = start; i < m_count; i++)
        {
            if (equalityComparer.Equals(value, valueSelector(m_buffer[i])))
            {
                return i;
            }
        }

        return -1;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf<W>(W pass, Func<W, T, bool> func)
    {
        for (int i = m_count - 1; i >= 0; i--)
        {
            if (func(pass, m_buffer[i]))
            {
                return i;
            }
        }

        return -1;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(Func<T, bool> func)
    {
        for (int i = m_count - 1; i >= 0; i--)
        {
            if (func(m_buffer[i]))
            {
                return i;
            }
        }

        return -1;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int BinarySearch<TComparable>(TComparable value, int startIndex, Func<TComparable, T, int> compare) 
    {
        return BinarySearch(value, startIndex, m_count, compare);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int BinarySearch<TComparable>(TComparable value, Func<TComparable, T, int> compare)
    {
        return BinarySearch(value, 0, m_count, compare);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int BinarySearch<TComparable>(TComparable value, int startIndex, int count, Func<TComparable, T, int> compare) 
    {
        int lo = startIndex;
        int hi = count - 1;

        while (lo <= hi)
        {
            int index = lo + ((hi - lo) >> 1);

            var comp = compare(value, m_buffer[index]);

            if (comp == 0)
            {
                return index;
            }

            if (comp > 0)
            {
                lo = index + 1;
            }
            else
            {
                hi = index - 1;
            }
        }
        return ~lo;
    }
    
    public void Sort(IComparer<T> compare)
    {
        var arrayPool = ArrayPool<T>.Shared;
        
        var rent = arrayPool.Rent(m_count);
        
        for (int i = 0; i < m_count && i < rent.Length; i++)
        {
            rent[i] = m_buffer[i];
        }
        
        Array.Sort(rent, 0, m_count, compare);
        
        for (int i = 0; i < rent.Length && i < m_count; i++)
        {
            m_buffer[i] = rent[i];
        }
        
        arrayPool.Return(rent);
    }
    
    public void Sort(Comparison<T> comparison)
    {
        var arrayPool = ArrayPool<T>.Shared;
        
        var rent = arrayPool.Rent(m_count);
        
        for (int i = 0; i < m_count && i < rent.Length; i++)
        {
            rent[i] = m_buffer[i];
        }
        
        Array.Sort(rent, 0, m_count, new Comparer(comparison));
        
        for (int i = 0; i < rent.Length && i < m_count; i++)
        {
            m_buffer[i] = rent[i];
        }
        
        arrayPool.Return(rent);
    }
    
    private class Comparer : IComparer<T>
    {
        private readonly Comparison<T> m_comparison;

        public Comparer(Comparison<T> comparison)
        {
            m_comparison = comparison;
        }

        public int Compare(T x, T y)
        {
            return m_comparison(x, y);
        }
    }

    public bool RemoveAt(int index)
    {
        if ( index < 0 || index >= m_count) 
        {
            return false;
        }
      
        m_count -= 1;
        for (int j = index; j < m_count; j++) 
        {
            m_buffer[j] = m_buffer[j + 1];
        }

        return true;
    }
    
    public bool Remove(T value)
    {
        var indexOf = IndexOf(value);

        if ( indexOf < 0 || indexOf >= m_count) 
        {
            return false;
        }
       
        return RemoveAt(indexOf);
    }

    public int RemoveAll<V>(V value, [NotNull] Func<T, V> valueSelector)
    {
        return RemoveAll<V>(value, valueSelector, EqualityComparer<V>.Default);
    }

    public int RemoveAll<V>(V value, [NotNull] Func<T, V> valueSelector, IEqualityComparer<V> equalityComparer)
    {
        if (m_count == 0)
        {
            return 0;
        }
        
        int counter = 0;
        int matchedIndex;
        do
        {
            matchedIndex = -1;

            for (int i = 0; i < m_count; i++)
            {
                ref var item = ref m_buffer[i];

                if (equalityComparer.Equals(value, valueSelector(item)))
                {
                    matchedIndex = i;
                    break;
                }
            }

            if (matchedIndex >= 0)
            {
                RemoveAt(matchedIndex);
                counter++;
            }
        } while (m_count > 0 && matchedIndex >= 0);

        return counter;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RemoveAll(Func<T, bool> match)
    {
        if (m_count == 0)
        {
            return 0;
        }
        
        int counter = 0;
        int matchedIndex;
        do
        {
            matchedIndex = -1;

            for (int i = 0; i < m_count; i++)
            {
                ref var item = ref m_buffer[i];

                if (match(item))
                {
                    matchedIndex = i;
                    break;
                }
            }

            if (matchedIndex >= 0)
            {
                RemoveAt(matchedIndex);
                counter++;
            }
        } while (m_count > 0 && matchedIndex >= 0);

        return counter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reverse()
    {
        int i = 0;
        int j = m_count - 1;
        while (i < j)
        {
            (m_buffer[i], m_buffer[j]) = (m_buffer[j], m_buffer[i]);
            i++;
            j--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(IList<T> target, int startIndex = 0)
    {
        int li = startIndex;
        for (int i = 0; i < m_count && i < target.Count; i++)
        {
            target[li] = m_buffer[i];
            
            li++;
        }
    }
    
    public void CopyTo(int index, T[] target, int arrayIndex, int count)
    {
        if (m_count - index < count)
        {
            throw new ArgumentOutOfRangeException();
        }

        int li = arrayIndex;
        int cnt = 0;
        for (int i = index; i < m_count && cnt < count; i++)
        {
            target[li] = m_buffer[i];
            
            li++;
            cnt++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(T value)
    {
        for (int i = m_count - 1; i >= 0; i--)
        {
            var obj = m_buffer[i];
            
            if (obj == null)
            {
                if (value == null)
                {
                    return i;
                }
            }
            else
            {
                if (obj.Equals(value))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SequenceEquals(DataStruct<T> value, EqualityComparer<T> comparer = default)
    {
        var cmp = comparer ?? EqualityComparer<T>.Default;

        if (m_count != value.m_count)
        {
            return false;
        }
        
        for (int i = 0; i < m_buffer.Length && i < m_count && i < value.m_buffer.Length; i++)
        {
            if (cmp.Equals(m_buffer[i], value[i]) == false)
            {
                return false;
            }
        }

        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SequenceEquals(IReadOnlyList<T> value, EqualityComparer<T> comparer = default)
    {
        var cmp = comparer ?? EqualityComparer<T>.Default;

        if (m_count != value.Count)
        {
            return false;
        }
        
        for (int i = 0; i < m_buffer.Length && i < m_count && i < value.Count; i++)
        {
            if (cmp.Equals(m_buffer[i], value[i]) == false)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Aggregate(Action<int, T> onValue)
    {
        for (int i = 0; i < m_count; i++)
        {
            onValue(i, m_buffer[i]);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T FirstOrDefault() => m_count > 0 ? m_buffer[0] : default;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T LastOrDefault() => m_count > 0 ? m_buffer[m_count - 1] : default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Aggregate<W>(W pass, Action<W, int, T> onValue)
    {
        for (int i = 0; i < m_count; i++)
        {
            onValue(pass, i, m_buffer[i]);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T WhereFirstOrDefault<W>(W pass, Func<W, int, T, bool> where)
    {
        for (int i = 0; i < m_count; i++)
        {
            if (where(pass, i, m_buffer[i]))
            {
                return m_buffer[i];
            }
        }

        return default;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereAggregate<W>(W pass, Func<W, int, T, bool> where, Action<W, int, T> select)
    {
        for (int i = 0; i < m_count; i++)
        {
            if (where(pass, i, m_buffer[i]))
            {
                select(pass, i, m_buffer[i]);
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereAggregate(Func<int, T, bool> where, Action<int, T> select)
    {
        for (int i = 0; i < m_count; i++)
        {
            if (where(i, m_buffer[i]))
            {
                select(i, m_buffer[i]);
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Data<T> ToData(Func<T, bool> where)
    {
        var data = new Data<T>();

        for (int i = 0; i < m_count; i++)
        {
            if (where(m_buffer[i]))
            {
                data.Add(m_buffer[i]);
            }
        }

        return data;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool All(Func<T, bool> where)
    {
        int count = 0;

        for (int i = 0; i < m_count; i++)
        {
            if (where(m_buffer[i]))
            {
                count++;
            }
        }

        return count == m_count;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Any(Func<T, bool> where)
    {
        int count = 0;

        for (int i = 0; i < m_count; i++)
        {
            if (where(m_buffer[i]))
            {
                return true;
            }
        }

        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Data<W> ToData<W>(Func<int, T, bool> where, Func<int, T, W> select)
    {
        var data = new Data<W>();

        for (int i = 0; i < m_count; i++)
        {
            if (where(i, m_buffer[i]))
            {
                data.Add(select(i, m_buffer[i]));
            }
        }

        return data;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(T value)
    {
        if (m_count >= m_buffer.Length)
        {
            return false;
        }
        
        m_buffer[m_count] = value;
        m_count++;

        return true;
    }

    public bool Insert(int index, T value)
    {
        if (m_count >= m_buffer.Length)
        {
            return false;
        }

        if (index == m_count)
        {
            m_buffer[m_count] = value;
            m_count++;
            return true;
        }
 
        for (var i = m_count; i > index; i--)
        {
            m_buffer[i] = m_buffer[i - 1];
        }
        
        m_count++;
 
        m_buffer[index] = value;

        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddRange(ref DataStruct<T> value)
    {
        if (m_count >= m_buffer.Length)
        {
            return false;
        }

        var valueCount = value.Count;
        
        if (valueCount + m_count > m_buffer.Length)
        {
            return false;
        }

        for (int i = 0; i < value.m_count; i++)
        {
            m_buffer[m_count] = value[i];
            m_count++;
        }

        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddRange(IReadOnlyList<T> value)
    {
        if (m_count >= m_buffer.Length)
        {
            return false;
        }

        var valueCount = value.Count;
        
        if (valueCount + m_count > m_buffer.Length)
        {
            return false;
        }

        foreach (var v in value)
        {
            m_buffer[m_count] = v;
            m_count++;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Ensure(int newCount, T value = default)
    {
        var currentCount = m_count;
        if (newCount == currentCount)
        {
            return true;
        }
        
        if (newCount >= m_buffer.Length)
        {
            return false;
        }
        
        for (int i = currentCount; i < newCount; i++)
        {
            m_buffer[i] = value;
            m_count++;
        }
        
        return true;
    }

    public void Clear()
    {
        m_count = 0;
    }

    public List<T> ToList()
    {
        var list = new List<T>(m_count);
        
        for (int i = 0; i < m_count; i++)
        {
            list.Add(m_buffer[i]);
        }

        return list;
    }
    
    public Set<T> ToSet(ISetAllocatorSetup<T> allocatorSetup = null)
    {
        var set = new Set<T>(allocatorSetup, null);
        
        for (int i = 0; i < m_count; i++)
        {
            set.Add(m_buffer[i]);
        }
        
        return set;
    }
    
    public Data<T> ToData(IDataAllocatorSetup<T> allocatorSetup = null)
    {
        var list = new Data<T>(allocatorSetup);

        list.Ensure(m_count);
        
        for (int i = 0; i < m_count; i++)
        {
            list.ValueByRef(i) = m_buffer[i];
        }

        return list;
    }
    
    public T[] ToArray()
    {
        var array = new T[m_count];
        
        for (int i = 0; i < m_count; i++)
        {
            array[i] = m_buffer[i];
        }

        return array;
    }
}