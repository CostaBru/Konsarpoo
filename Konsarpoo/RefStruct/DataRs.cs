using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections.Stackalloc;

/// <summary>
/// The universal random access data container build on generic contiguous memory Span of T.
/// It cannot contain more than predefined number of elements
/// and will throw InsufficientMemoryException exception after reaching the maximum capacity. 
/// </summary>
/// <typeparam name="T"></typeparam>
[StructLayout(LayoutKind.Auto)]
public ref struct DataRs<T> 
{
    internal Span<T> m_buffer;
    internal int m_count;

    /// <summary>
    /// Default constructor that expect the maximum storage capacity it can contain.
    /// </summary>
    /// <param name="span"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataRs(ref Span<T> span)
    {
        m_buffer = span;
        m_count = 0;
    }

    /// <summary>
    /// Constructor that fills out container with predefined data.
    /// </summary>
    /// <param name="array"></param>
    /// <param name="count"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataRs([NotNull] T[] array, int count)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (count > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        m_buffer = new Span<T>(array);
        m_count = count;
    }

    /// <summary>
    /// Allows to enumerate contents. 
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataRsEnumerator GetEnumerator() => new (m_buffer, m_count);

    /// <summary>
    /// Allows to enumerate contents. 
    /// </summary>
    /// <returns></returns>
    public RsEnumerator<T, T> GetRsEnumerator() => new (new DataRsEnumerator(m_buffer, m_count));
   
    /// <summary> DataRs Enumerator </summary>
    /// <typeparam name="T"></typeparam>
    public ref struct DataRsEnumerator
    {
        private readonly Span<T> m_span;
        private readonly int m_count;
        private int m_index;

     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DataRsEnumerator(Span<T> span, int count)
        {
            m_span = span;
            m_count = count;
            m_index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int index = m_index + 1;
            if (index < m_count)
            {
                m_index = index;
                return true;
            }

            return false;
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_span[m_index];
        }

        public int Count => m_count;
    }
    
    /// <summary>Gets  the element at the specified index.</summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is not a valid index in the DataRs container.</exception>
    /// <returns>The element at the specified index.</returns>
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

    /// <summary> Returns items count in container. </summary>
    public int Count => m_count;
    
    /// <summary> Returns items count in container. Array API. </summary>
    public int Length => m_count;

    /// <summary>Determines the index of a specific item in the DataRs container.</summary>
    /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
    /// <returns>The index of <paramref name="value" /> if found in the list; otherwise, -1.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindIndex(T value)
    {
        return IndexOf(value);
    }

    /// <summary>Determines the index of a specific item in the DataRs container.</summary>
    /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
    /// <returns>The index of <paramref name="value" /> if found in the list; otherwise, -1.</returns>
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

    /// <summary>
    /// Searches for an element that matches the conditions defined by a specified predicate, and returns the zero-based index of the first occurrence within the DataRs&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
    /// </summary>
    /// <param name="match"></param>
    /// <param name="start"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindIndex(Func<T, bool> func, int start = 0)
    {
        return IndexOf(func, start);
    }
    
    /// <summary>
    /// Searches for an element that matches the conditions defined by a specified predicate, and returns the zero-based index of the first occurrence within the DataRs&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
    /// </summary>
    /// <param name="match"></param>
    /// <param name="start"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindIndexPredicate(Predicate<T> match, int start = 0)
    {
        return IndexOfPredicate(match, start);
    }

    /// <summary>
    /// Searches for an element that matches the conditions defined by a specified predicate, and returns the zero-based index of the first occurrence within the DataRs&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
    /// </summary>
    /// <param name="match"></param>
    /// <param name="start"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(Func<T, bool> match, int start = 0)
    {
        for (int i = start; i < m_count; i++)
        {
            if (match(m_buffer[i]))
            {
                return i;
            }
        }

        return -1;
    }
    
    /// <summary>
    /// Searches for an element that matches the conditions defined by a specified predicate, and returns the zero-based index of the first occurrence within the DataRs&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
    /// </summary>
    /// <param name="match"></param>
    /// <param name="start"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOfPredicate(Predicate<T> match, int start = 0)
    {
        for (int i = start; i < m_count; i++)
        {
            if (match(m_buffer[i]))
            {
                return i;
            }
        }

        return -1;
    }
    
    /// <summary>
    /// Searches for the first equality condition matching predicate. Returns the zero-based index of the first occurrence within the DataRs&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="valueSelector"></param>
    /// <param name="start"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Searches for the first equality condition match index using T to V value selector function and default comparer. Returns the zero-based index of the first occurrence within the Data&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="valueSelector"></param>
    /// <param name="start"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindIndex<V>(V value, [NotNull] Func<T, V> valueSelector, int start = 0)
    {
        return FindIndex<V>(value, valueSelector, EqualityComparer<V>.Default, start);
    }

    /// <summary>
    /// Searches for the first equality condition match index using T to V value selector function and default comparer. Returns the zero-based index of the first occurrence within the Data&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="valueSelector"></param>
    /// <param name="start"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Searches for the last equality condition match index using T to V value comparer function. Returns the zero-based index of the last occurrence within the DataRs&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
    /// </summary>
    /// <param name="pass"></param>
    /// <param name="func"></param>
    /// <param name="startingIndex"></param>
    /// <param name="endIndex"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindLastIndex<W>(W pass, Func<W, T, bool> func, int startingIndex = int.MaxValue, int endingIndex = 0)
    {
        if (endingIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(endingIndex));
        }
        
        if (startingIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startingIndex));
        }
        
        startingIndex = Math.Min(startingIndex, m_count - 1);
        
        for (int i = startingIndex; i >= endingIndex; i--)
        {
            if (func(pass, m_buffer[i]))
            {
                return i;
            }
        }

        return -1;
    }
    
    /// <summary>
    /// Searches for the last equality condition match index a given comparer function. Returns the zero-based index of the last occurrence within the DataRs&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
    /// </summary>
    /// <param name="func"></param>
    /// <param name="startingIndex"></param>
    /// <param name="endingIndex"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindLastIndex(Func<T, bool> func, int startingIndex = int.MaxValue, int endingIndex = 0)
    {
        if (endingIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(endingIndex));
        }
        
        if (startingIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startingIndex));
        }

        startingIndex = Math.Min(startingIndex, m_count - 1);
        
        for (int i = startingIndex; i >= endingIndex; i--)
        {
            if (func(m_buffer[i]))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Array and List API. Searches the entire sorted DataRs&lt;T&gt; for an element using the default comparer and returns the zero-based index of the element.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="startIndex"></param>
    /// <param name="compare"></param>
    /// <returns>The zero-based index of item in the sorted Data&lt;T&gt;, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of Count.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int BinarySearch<TComparable>(TComparable value, int startIndex, Func<TComparable, T, int> compare) 
    {
        return BinarySearch(value, startIndex, m_count, compare);
    }

    /// <summary>
    /// Array and List API. Searches the entire sorted DataRs&lt;T&gt; for an element using the default comparer and returns the zero-based index of the element.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="compare"></param>
    /// <returns>The zero-based index of item in the sorted Data&lt;T&gt;, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of Count.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int BinarySearch<TComparable>(TComparable value, Func<TComparable, T, int> compare)
    {
        return BinarySearch(value, 0, m_count, compare);
    }

    /// <summary>
    /// Array and List API. Searches the entire sorted DataRs&lt;T&gt; for an element using the default comparer and returns the zero-based index of the element.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="count"></param>
    /// <param name="compare"></param>
    /// <param name="startIndex"></param>
    /// <returns>The zero-based index of item in the sorted Data&lt;T&gt;, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of Count.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int BinarySearch<TComparable>(TComparable value, int startIndex, int count, Func<TComparable, T, int> compare)
    {
        if (startIndex < 0 || startIndex >= m_count)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }
        
        if (count < 0 || count > m_count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        
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
    
    /// <summary>
    /// Sorts the elements in the Data&lt;T&gt; using given comparer. 
    /// </summary>
    public void Sort([NotNull] IComparer<T> compare)
    {
        if (compare == null) throw new ArgumentNullException(nameof(compare));
        
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
    
    /// <summary>
    /// Sorts the elements in the Data&lt;T&gt; using given comparison. 
    /// </summary>
    public void Sort([NotNull] Comparison<T> comparison)
    {
        if (comparison == null) throw new ArgumentNullException(nameof(comparison));
        
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

    /// <summary>
    /// Removes the element at the specified index of the DataRs&lt;T&gt;.
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="IndexOutOfRangeException"></exception>
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
    
    /// <summary>
    /// Removes the first occurrence of a specific object from the DataRs&lt;T&gt;.
    /// </summary>
    /// <param name="item"></param>
    /// <returns>True if any item was removed.</returns>
    public bool Remove(T item)
    {
        var indexOf = IndexOf(item);

        if ( indexOf < 0 || indexOf >= m_count) 
        {
            return false;
        }
       
        return RemoveAt(indexOf);
    }
    
    /// <summary>
    /// Removes the last element of the Data&lt;T&gt;.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public bool RemoveLast()
    {
        if (m_count > 0)
        {
            return RemoveAt(m_count - 1);
        }
        
        return false;
    }

    /// <summary>
    /// Removes all occurrences of a specific object from the DataRs&lt;T&gt; using allocation free values selection comparison using default comparer.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="valueSelector"></param>
    /// <typeparam name="V"></typeparam>
    /// <returns>True if any item was removed.</returns>
    public int RemoveAll<V>(V value, [NotNull] Func<T, V> valueSelector)
    {
        return RemoveAll<V>(value, valueSelector, EqualityComparer<V>.Default);
    }

    /// <summary>
    /// Removes all occurrences of a specific object from the DataRs&lt;T&gt; using allocation free values selection comparison using given comparer.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="valueSelector"></param>
    /// <param name="equalityComparer"></param>
    /// <typeparam name="V"></typeparam>
    /// <returns>True if any item was removed.</returns>
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


    /// <summary>
    /// Removes all elements that match function delegated.
    /// </summary>
    /// <param name="match"></param>
    /// <returns></returns>
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

    /// <summary>
    /// List API. Reverses the order of the elements in the entire DataRs&lt;T&gt;.
    /// </summary>
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

    /// <summary>Copies the elements of the DataRs&lt;T&gt; to an <see cref="T:System.Collections.Generic.IList" /> collection, starting at a particular <see cref="T:System.Collections.Generic.IList" /> index.</summary>
    /// <param name="target">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Collections.Generic.IList" /> must have zero-based indexing.</param>
    /// <param name="startIndex">The zero-based index in <paramref name="target" /> at which copying begins.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="target" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="startIndex" /> is less than zero.</exception>
    /// 
    /// -or-
    /// 
    /// The number of elements in the source DataRs&lt;T&gt; is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />.
    /// 
    /// -or-
    /// 
    /// The type of the source DataRs&lt;T&gt;  cannot be cast automatically to the type of the destination <paramref name="target" />.</exception>

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo([NotNull] IList<T> target, int startIndex = 0)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if(startIndex < 0 || startIndex >= m_count)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }
        
        int li = startIndex;
        for (int i = 0; i < m_count && i < target.Count; i++)
        {
            target[li] = m_buffer[i];
            
            li++;
        }
    }
    
    /// <summary>
    /// Copies the Data&lt;T&gt; or a portion of it to an array.
    /// </summary>
    /// <param name="index">Source index.</param>
    /// <param name="array">Target array.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    /// <param name="count">The number of elements to copy.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IndexOutOfRangeException">
    /// length is greater than the number of elements from sourceIndex to the end of sourceArray.
    /// -or- length is greater than the number of elements from destinationIndex to the end of destinationArray.
    /// -or- arrayIndex is greater or equal destinationArray length.
    /// -or- count is greater than collection size.
    /// </exception>
    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
        if (m_count - index < count)
        {
            throw new ArgumentOutOfRangeException();
        }

        int li = arrayIndex;
        int cnt = 0;
        for (int i = index; i < m_count && cnt < count; i++)
        {
            array[li] = m_buffer[i];
            
            li++;
            cnt++;
        }
    }

    /// <summary>
    /// Searches for a last element index that matches the given value and equality comparer, and returns the zero-based index of the first occurrence within the DataRs&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="equalityComparer"></param>
    /// <typeparam name="V"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindLastIndex(T value, IEqualityComparer<T> equalityComparer = null)
    {
        var cmp = equalityComparer  ?? EqualityComparer<T>.Default;
        
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
                if (cmp.Equals(obj, value))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Determines whether the specified DataRs&lt;T&gt; instances are considered equal by comparing type, sizes and elements.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="comparer"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SequenceEquals(DataRs<T> value, EqualityComparer<T> comparer = default)
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
    
    /// <summary>
    /// Determines whether the specified DataRs&lt;T&gt; instances are considered equal by comparing type, sizes and elements.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="comparer"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Calls the given onValue action for each item in list.
    /// </summary>
    /// <param name="onValue"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ForEach(Action<int, T> onValue)
    {
        for (int i = 0; i < m_count; i++)
        {
            onValue(i, m_buffer[i]);
        }
    }
    
    /// <summary>
    /// Returns first or default item in list.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T FirstOrDefault(T def = default) => m_count > 0 ? m_buffer[0] : def;
    
    /// <summary>
    /// Returns last or default item in list.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T LastOrDefault(T def = default) => m_count > 0 ? m_buffer[m_count - 1] : def;

    /// <summary>
    /// Calls given onValue action for each value in list and pass the given target to it. 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="onValue"></param>
    /// <typeparam name="W"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Aggregate<W>(W target, Action<W, int, T> onValue)
    {
        for (int i = 0; i < m_count; i++)
        {
            onValue(target, i, m_buffer[i]);
        }
    }
    
    /// <summary>
    /// Takes a list's first matched item with the given value and compare delegate or default if no one.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="where"></param>
    /// <typeparam name="W"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T WhereFirstOrDefault<W>(W value, Func<W, int, T, bool> where)
    {
        for (int i = 0; i < m_count; i++)
        {
            if (where(value, i, m_buffer[i]))
            {
                return m_buffer[i];
            }
        }

        return default;
    }
    
    /// <summary>
    /// Takes a list's last matched item with the given value and compare delegate or default if no one.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="where"></param>
    /// <typeparam name="W"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T WhereLastOrDefault<W>(W value, Func<W, int, T, bool> where)
    {
        for (int i = m_count; i >= 0; i--)
        {
            if (where(value, i, m_buffer[i]))
            {
                return m_buffer[i];
            }
        }

        return default;
    }

    /// <summary>
    /// Calls given onValue action for each value that meets where condition in list. 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="where"></param>
    /// <param name="onValue"></param>
    /// <typeparam name="W"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereAggregate<W>(W target, Func<W, int, T, bool> where, Action<W, int, T> onValue)
    {
        for (int i = 0; i < m_count; i++)
        {
            if (where(target, i, m_buffer[i]))
            {
                onValue(target, i, m_buffer[i]);
            }
        }
    }
    
    /// <summary>
    /// Calls given onValue action for each value that meets where condition in list. 
    /// </summary>
    /// <param name="where"></param>
    /// <param name="onValue"></param>
    /// <typeparam name="T"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WhereForEach(Func<int, T, bool> where, Action<int, T> onValue)
    {
        for (int i = 0; i < m_count; i++)
        {
            if (where(i, m_buffer[i]))
            {
                onValue(i, m_buffer[i]);
            }
        }
    }

    /// <summary>
    /// Returns true if given where condition returns true for all items in list. 
    /// </summary>
    /// <param name="where"></param>
    /// <returns></returns>
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
    
    /// <summary>
    /// Returns true if given where condition returns true for all items in list. 
    /// </summary>
    /// <param name="where"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool All<W>(W pass, Func<W, T, bool> where)
    {
        int count = 0;

        for (int i = 0; i < m_count; i++)
        {
            if (where(pass, m_buffer[i]))
            {
                count++;
            }
        }

        return count == m_count;
    }
    
    /// <summary>
    /// Returns true if given where condition returns true for any items in list. 
    /// </summary>
    /// <param name="where"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Any<W>(W pass, Func<W, T, bool> where)
    {
        int count = 0;

        for (int i = 0; i < m_count; i++)
        {
            if (where(pass, m_buffer[i]))
            {
                return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// Returns true if given where condition returns true for any items in list. 
    /// </summary>
    /// <param name="where"></param>
    /// <returns></returns>
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
    
    /// <summary>
    /// Returns average. 
    /// </summary>
    /// <param name="convert">Function to get double from T.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Average(Func<T, double> convert)
    {
        double sum = 0;
        
        for (int i = 0; i < m_count; i++)
        {
            sum += convert(m_buffer[i]);
        }

        return sum / m_count;
    }

    /// <summary>
    /// Gets Max. 
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Max()
    {
        T max = default;

        var comparer = Comparer<T>.Default;

        for (int i = 0; i < m_count; i++)
        {
            ref var x = ref m_buffer[i];

            if (i == 0)
            {
                max = x;
                continue;
            }
            
            if (comparer.Compare(x, max) > 0)
            {
                max = x;
            }
        }

        return max;
    }
    
    /// <summary>
    /// Gets Min. 
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Min()
    {
        T min = default;

        var comparer = Comparer<T>.Default;

        for (int i = 0; i < m_count; i++)
        {
            ref var x = ref m_buffer[i];

            if (i == 0)
            {
                min = x;
                continue;
            }
            
            if (comparer.Compare(x, min) < 0)
            {
                min = x;
            }
        }

        return min;
    }

    /// <summary>
    /// Copies each item that meets condition of DataRs to a new Data class instance. 
    /// </summary>
    /// <param name="where"></param>
    /// <param name="allocatorSetup"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Data<T> ToData(Func<T, bool> where, IDataAllocatorSetup<T> allocatorSetup = null)
    {
        var data = new Data<T>(allocatorSetup);

        for (int i = 0; i < m_count; i++)
        {
            if (where(m_buffer[i]))
            {
                data.Add(m_buffer[i]);
            }
        }

        return data;
    }

    /// <summary>
    /// Copies each item that meets condition of DataRs to a new Data class instance. 
    /// </summary>
    /// <param name="pass"></param>
    /// <param name="where"></param>
    /// <param name="allocatorSetup"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Data<T> ToData<W>(W pass, Func<W, T, bool> where, IDataAllocatorSetup<T> allocatorSetup = null)
    {
        var data = new Data<T>(allocatorSetup);

        for (int i = 0; i < m_count; i++)
        {
            if (where(pass, m_buffer[i]))
            {
                data.Add(m_buffer[i]);
            }
        }

        return data;
    }

    /// <summary>
    /// Copies each item that meets condition of DataRs to a new Data class instance. 
    /// </summary>
    /// <param name="where"></param>
    /// <param name="select"></param>
    /// <param name="allocatorSetup"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Data<W> ToData<W>(Func<int, T, bool> where, Func<int, T, W> select, IDataAllocatorSetup<W> allocatorSetup = null)
    {
        var data = new Data<W>(allocatorSetup);

        for (int i = 0; i < m_count; i++)
        {
            if (where(i, m_buffer[i]))
            {
                data.Add(select(i, m_buffer[i]));
            }
        }

        return data;
    }
    
    
    /// <summary>
    /// Adds a new item to the list.
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="InsufficientMemoryException"></exception>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T value)
    {
        if (m_count >= m_buffer.Length)
        {
            throw new InsufficientMemoryException($"Cannot add a new item to the DataRs list. The {m_buffer.Length} maximum reached.");
        }
        
        m_buffer[m_count] = value;
        m_count++;
    }

    /// <summary>
    /// Insert a new item at given index.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="value"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="InsufficientMemoryException"></exception>
    public void Insert(int index, T value)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be greater or equal to zero.");
        }
        
        if (m_count >= m_buffer.Length)
        {
            throw new InsufficientMemoryException($"Cannot insert a new item to the DataRs list. The {m_buffer.Length} maximum reached.");
        }

        if (index == m_count)
        {
            m_buffer[m_count] = value;
            m_count++;
            return;
        }
 
        for (var i = m_count; i > index; i--)
        {
            m_buffer[i] = m_buffer[i - 1];
        }
        
        m_count++;
 
        m_buffer[index] = value;
    }
    
    /// <summary>
    /// Adds a bunch of new items to the DataRs.
    /// </summary>
    /// <param name="list"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ref DataRs<T> list)
    {
        CheckCanAdd(list.Count);

        for (int i = 0; i < list.m_count; i++)
        {
            m_buffer[m_count] = list[i];
            m_count++;
        }
    }

    private void CheckCanAdd(int valueCount)
    {
        if (m_count >= m_buffer.Length)
        {
            throw new InsufficientMemoryException(
                $"Cannot add the {valueCount} of new items to the DataRs list. The {m_buffer.Length} maximum reached.");
        }

        if (valueCount + m_count > m_buffer.Length)
        {
            throw new InsufficientMemoryException(
                $"Cannot add the {valueCount} of new items to the DataRs list. The {m_buffer.Length} is a maximum.");
        }
    }

    /// <summary>
    /// Adds a bunch of new items to the DataRs.
    /// </summary>
    /// <param name="list"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ref SetRs<T> list)
    {
        CheckCanAdd(list.Count);
        
        var index = 0;

        while (index < list.m_count)
        {
            if (list.m_entries[index].HashCode >= 0)
            {
                m_buffer[m_count] = list.m_entries[index].Key;
                m_count++;
            }

            index++;
        }
    }
    
    /// <summary>
    /// Adds a bunch of new items to the DataRs.
    /// </summary>
    /// <param name="list"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange([NotNull] IReadOnlyList<T> list)
    {
        CheckCanAdd(list.Count);

        foreach (var v in list)
        {
            m_buffer[m_count] = v;
            m_count++;
        }
    }

    /// <summary>
    /// Array API. Ensures that current DataRs&lt;T&gt; container has given size.
    /// </summary>
    /// <param name="size"></param>
    /// <param name="defaultValue"></param>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Ensure(int size, T defaultValue = default)
    {
        var currentCount = m_count;
        if (size == currentCount)
        {
            return true;
        }
        
        if (size > m_buffer.Length)
        {
            throw new InsufficientMemoryException($"Cannot extend the DataRs list to fit the {size} items. The {m_buffer.Length} is a maximum.");
        }
        
        for (int i = currentCount; i < size; i++)
        {
            m_buffer[i] = defaultValue;
            m_count++;
        }
        
        return true;
    }
    
    /// <summary>
    /// List API. Clears Data&lt;T&gt; and returns all arrays allocated back to array pool.
    /// </summary>
    public void Clear()
    {
        m_count = 0;
    }

    /// <summary>
    /// Copies each item of DataRs to a new List class instance. 
    /// </summary>
    public List<T> ToList()
    {
        var list = new List<T>(m_count);
        
        for (int i = 0; i < m_count; i++)
        {
            list.Add(m_buffer[i]);
        }

        return list;
    }
    
    /// <summary>
    /// Copies each item of DataRs to a new Set class instance. 
    /// </summary>
    public Set<T> ToSet(ISetAllocatorSetup<T> allocatorSetup = null)
    {
        var set = new Set<T>(allocatorSetup, null);
        
        for (int i = 0; i < m_count; i++)
        {
            set.Add(m_buffer[i]);
        }
        
        return set;
    }
    
    /// <summary>
    /// Copies each item of DataRs to a new Data class instance. 
    /// </summary>
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
    
    /// <summary>
    /// Copies each item of DataRs to a new array instance. 
    /// </summary>
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