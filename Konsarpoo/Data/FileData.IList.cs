using System;
using System.Collections.Generic;

namespace Konsarpoo.Collections
{
    public partial class FileData<T> : IList<T>
    {
        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the collection.</param>
        /// <returns>true if item is found in the collection; otherwise, false.</returns>
        public bool Contains(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < m_count; i++)
            {
                if (comparer.Equals(this[i], item))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Copies the elements of the collection to an Array, starting at a particular Array index.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from the collection.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="ArgumentNullException">array is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">arrayIndex is less than 0.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source collection is greater than the available space from arrayIndex to the end of the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (arrayIndex > array.Length) throw new ArgumentException("arrayIndex exceeds array length");
            if (array.Length - arrayIndex < m_count) throw new ArgumentException("Destination array is not long enough.");

            for (int i = 0; i < m_count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the collection.
        /// </summary>
        /// <param name="item">The object to remove from the collection.</param>
        /// <returns>true if item was successfully removed from the collection; otherwise, false. This method also returns false if item is not found in the collection.</returns>
        public bool Remove(T item)
        {
            int idx = IndexOf(item);
            if (idx >= 0)
            {
                RemoveAt(idx);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Determines the index of a specific item in the collection.
        /// </summary>
        /// <param name="item">The object to locate in the collection.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < m_count; i++)
            {
                if (comparer.Equals(this[i], item))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}