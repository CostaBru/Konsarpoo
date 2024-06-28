using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections
{
    public partial class Data<T> 
    {
        /// <summary>
        /// Array and List API. Searches the entire sorted Data&lt;T&gt; for an element using the comparer given and returns the zero-based index of the element.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="comparer"></param>
        /// <typeparam name="V"></typeparam>
        /// <returns>The zero-based index of item in the sorted Data&lt;T&gt;, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of Count.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int BinarySearch<V>(V value, int startIndex, int count, Func<V, T, int> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            if (startIndex < 0 || startIndex >= m_count || count < 0 || count > m_count)
            {
                return -1;
            }

            //common case
            if (m_root?.Storage != null)
            {
                return new DataRs<T>(m_root.Storage, m_count)
                    .BinarySearch(value, startIndex, count, comparer);
            }

            int lo = startIndex;
            int hi = count - 1;

            while (lo <= hi)
            {
                int index = lo + ((hi - lo) >> 1);

                int order = comparer(value, ValueByRef(index));

                if (order == 0)
                {
                    return index;
                }

                if (order > 0)
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
        /// Array and List API. Searches the entire sorted Data&lt;T&gt; for an element using the default comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns>The zero-based index of item in the sorted Data&lt;T&gt;, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of Count.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BinarySearch(T value, int startIndex, int count)
        {
            if (startIndex < 0 || startIndex >= m_count || count < 0 || count > m_count)
            {
                return -1;
            }
         
            var storage = m_root?.Storage;

            //common case
            if (storage is not null)
            {
                return Array.BinarySearch(storage, startIndex, count - startIndex, value);
            }
            
            return BinarySearchSlow(value, startIndex, count, Comparer<T>.Default);
        }

        /// <summary>
        /// Array and List API. Searches the entire sorted Data&lt;T&gt; for an element using the default comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="startIndex"></param>
        /// <returns>The zero-based index of item in the sorted Data&lt;T&gt;, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of Count.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BinarySearch(T value, int startIndex = 0)
        {
            if (startIndex < 0 || startIndex >= m_count)
            {
                return -1;
            }
            
            var storage = m_root?.Storage;

            //common case
            if (storage is not null)
            {
                return Array.BinarySearch(storage, startIndex, m_count - startIndex, value);
            }

            return BinarySearchSlow(value, startIndex, m_count, Comparer<T>.Default);
        }
        
        /// <summary>
        /// Array and List API. Searches the entire sorted Data&lt;T&gt; for an element using the comparer given and returns the zero-based index of the element.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="comparer"></param>
        /// <returns>The zero-based index of item in the sorted Data&lt;T&gt;, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of Count.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BinarySearch(T value, int startIndex, int count, IComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            if (startIndex < 0 || startIndex >= m_count || count < 0 || count > m_count)
            {
                return -1;
            }
            
            var storage = m_root?.Storage;

            //common case
            if (storage is not null)
            {
                return Array.BinarySearch(storage, startIndex, count - startIndex, value, comparer);
            }

            return BinarySearchSlow(value, startIndex, count, comparer);
        }

        
        private int BinarySearchSlow(T item, int startIndex, int count, IComparer<T> comparer)
        {
            int lo = startIndex;
            int hi = count - 1;

            while (lo <= hi)
            {
                int index = lo + ((hi - lo) >> 1);

                int order = comparer.Compare(item, ValueByRef(index));

                if (order == 0)
                {
                    return index;
                }

                if (order > 0)
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
    }
}