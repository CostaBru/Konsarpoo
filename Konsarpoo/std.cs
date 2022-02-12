using System;
using System.Collections.Generic;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// std::vector API.
    /// </summary>
    public class std
    {
        /// <summary>
        /// Creates vector from params.
        /// </summary>
        public static vector<T> make_vector<T>(params T[] items)
        {
            return new vector<T>(items);
        }
        
        /// <summary>
        /// std::vector API.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class vector<T> : Data<T>
        {
            
            /// <summary>
            /// The vector constructor accepting an enumerable.
            /// </summary>
            /// <param name="items"></param>
            public vector(IEnumerable<T> items)
            : base(items)
            {
            }
            
            /// <summary>
            /// The vector constructor accepting an readonly collection.
            /// </summary>
            /// <param name="items"></param>
            public vector(IReadOnlyCollection<T> items)
                : base(items)
            {
            }

            /// <summary>
            /// Default constructor.
            /// </summary>
            public vector()
            {
            }
            
            /// <summary>
            /// Returns the number of elements.
            /// </summary>
            public long size()
            {
                return m_count;
            }

            /// <summary>
            /// Checks whether the container is empty.
            /// </summary>
            public bool empty()
            {
                return m_count == 0;
            }

            /// <summary> Gets the first element. </summary>
            public ref T front()
            {
                return ref at(m_count - 1);
            }

            /// <summary> Gets the last element. </summary>
            public ref T back()
            {
                return ref at(0);
            }

            /// <summary>
            /// Access specified element with bounds checking.
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public ref T at(long index)
            {
                return ref this.ValueByRef((int) index);
            }

            /// <summary>
            /// Access specified element with bounds checking.
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public ref T this[long index]
            {
                get { return ref this.ValueByRef((int) index); }
            }

            /// <summary> Clears the contents. </summary>
            public void clear()
            {
                base.Clear();
            }

            /// <summary> Adds an element to the end. </summary>
            public void push_back(T value)
            {
                base.Add(value);
            }

            /// <summary> Adds an element to the end. </summary>
            public void emplace_back(ref T value)
            {
                base.Add(value);
            }

            /// <summary> Removes the last element. </summary>
            public void pop_back()
            {
                base.RemoveLast();
            }

            /// <summary> Reverse storage. </summary>
            public void reverse()
            {
                base.Reverse();
            }

            ///<summary> Inserts elements at the specified location in the container.</summary>
            /// <exception cref="IndexOutOfRangeException"></exception>
            public void insert(int index, T item)
            {
                base.Insert(index, item);
            }
            
            /// <summary> Resizes the container to contain count elements.
            /// If the current size is greater than count, the container is reduced to its first count elements.
            ///    If the current size is less than count,
            /// 1) additional default-inserted elements are appended
            /// 2) additional copies of value are appended. </summary>
            public void resize(int size, T defaultValue = default)
            {
                base.Resize(size, defaultValue);
            }
            
            /// <summary>
            /// Sorts the elements or a portion of the elements in the vector&lt;T&gt; using default IComparer&lt;T&gt; implementation. 
            /// </summary>
            public void sort()
            {
                Sort(this);
            }

            /// <summary>
            /// Sorts the elements or a portion of the elements in the vector&lt;T&gt; using the specified IComparer&lt;T&gt; implementation. 
            /// </summary>
            public void sort(IComparer<T> comparer)
            {
                Sort(this, comparer);
            }

            /// <summary>
            /// Sorts the elements or a portion of the elements in the vector&lt;T&gt; using the provided Comparison&lt;T&gt; delegate to compare data elements. 
            /// </summary>
            public void sort(Comparison<T> comparison)
            {
                Sort(this, comparison);
            }

            /// <summary>
            /// Erases the specified elements from the container at pos.
            /// </summary>
            public void erase(int index)
            {
                RemoveAt(index);
            }
            
            /// <summary>
            /// Erases the specified elements from the container. Removes the elements in the range [first, last).
            /// </summary>
            public void erase(int startIndex, int endIndex)
            {
                if (startIndex <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(startIndex));
                }

                if (endIndex <= 0 || endIndex >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(endIndex));
                }

                for (int i = endIndex; i >= startIndex; i--)
                {
                    base.RemoveAt(i);
                }
            }
        }
    }
}