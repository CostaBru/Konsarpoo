using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Base class for pool list. Cannot has size larger than max capacity. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("PoolList. Size: {m_size}")]
    internal class PoolListBase<T> : IEnumerable, IEnumerable<T>
    {
        public static readonly T Default = default(T);
        
        [NotNull] 
        protected readonly IArrayAllocator<T> ArrayAllocator;

        internal readonly int m_maxCapacity;
        
        internal static volatile bool s_clearArrayOnReturn = KonsarpooAllocatorGlobalSetup.ClearArrayOnReturn;
        
        [NotNull] 
        public T[] m_items = Array.Empty<T>();

        public int m_size;

        public int Count => m_size;

        public ref T this[int index]
        {
            get
            {
                if (index >= m_size)
                {
                    throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_size}).");
                }

                return ref m_items[index];
            }
        }

        public PoolListBase(IArrayAllocator<T> allocator, int maxCapacity, int capacity)
        {
            m_maxCapacity = maxCapacity;
            
            ArrayAllocator = allocator;

            m_items = ArrayAllocator.Rent(Math.Min(capacity, maxCapacity));
        }

        public PoolListBase(PoolListBase<T> poolList)
        {
            ArrayAllocator = poolList.ArrayAllocator;

            var newArr = ArrayAllocator.Rent(poolList.m_items.Length);

            Array.Copy(poolList.m_items, 0, newArr, 0, poolList.m_size);

            m_items = newArr;
            m_size = poolList.m_size;
            m_maxCapacity = poolList.m_maxCapacity;
        }

        public bool Add(T item)
        {
            AddItem(item);
         
            return true;
        }

        public void AddItem(T item)
        {
            if (m_size + 1 > m_maxCapacity)
            {
                throw new InvalidOperationException($"Cannot add more items. Max size {m_maxCapacity} has reached.");
            }
            
            if (m_items.Length == 0)
            {
                T[] vals = ArrayAllocator.Rent(2);

                m_items = vals;
            }
            else if (m_size == m_items.Length)
            {
                int newCapacity = Math.Min(m_items.Length * 2, m_maxCapacity);

                T[] vals = ArrayAllocator.Rent(newCapacity);

                if (m_size > 0)
                {
                    Array.Copy(m_items, 0, vals, 0, m_size);
                }

                ArrayAllocator.Return(m_items, clearArray: s_clearArrayOnReturn);

                m_items = vals;
            }

            m_items[m_size] = item;

            m_size++;
        }

        public void Clear()
        {
            ReturnArray();

            m_size = 0;
        }

        public bool RemoveLast()
        {
            if (m_size > 0)
            {
                m_size--;

                m_items[m_size] = Default;
            }

            if (m_size == 0)
            {
                ReturnArray();

                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            if (m_size > 0)
            {
                m_size -= 1;

                if (index < m_size)
                {
                    Array.Copy(m_items, index + 1, m_items, index, m_size - index);
                }

                if (m_size >= 0)
                {
                    m_items[m_size] = Default;
                }

                if (m_size == 0)
                {
                    ReturnArray();
                }
            }
        }

        public void Insert(int index, T item)
        {
            if (m_size + 1 > m_maxCapacity)
            {
                throw new InvalidOperationException($"Cannot add more items. Max size {m_maxCapacity} has reached");
            }
            
            if (m_items.Length == 0)
            {
                T[] vals = ArrayAllocator.Rent(2);

                m_items = vals;
            }
            else if (m_size == m_items.Length)
            {
                int newCapacity = Math.Min(m_items.Length * 2, m_maxCapacity);

                T[] vals = ArrayAllocator.Rent(newCapacity);

                if (m_size > 0)
                {
                    Array.Copy(m_items, 0, vals, 0, m_size);
                }

                ArrayAllocator.Return(m_items, clearArray: s_clearArrayOnReturn);

                m_items = vals;
            }

            if (index < m_size)
            {
                Array.Copy(m_items, index, m_items, index + 1, m_size - index);
            }

            m_items[index] = item;
            m_size += 1;
        }

        public int RemoveAll(Func<T, bool> match)
        {
            int index1 = 0;
            while (index1 < m_size && !match(m_items[index1]))
            {
                ++index1;
            }

            if (index1 >= m_size)
            {
                return 0;
            }

            int index2 = index1 + 1;
            while (index2 < m_size)
            {
                while (index2 < m_size && match(m_items[index2]))
                {
                    ++index2;
                }
                if (index2 < m_size)
                {
                    m_items[index1++] = m_items[index2++];
                }
            }

            Array.Clear(m_items, index1, m_size - index1);

            int num = m_size - index1;
            m_size = index1;
            
            if (m_size == 0)
            {
                ReturnArray();
            }

            return num;
        }

        public int FindIndex(Predicate<T> match, int start)
        {
            return new DataRs<T>(m_items, m_size)
                .FindIndexPredicate(match, start);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int index = 0; index < m_size && index < m_items.Length; index++)
            {
                yield return m_items[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal int RemoveAll(T item, IComparer<T> comparer)
        {
            int index1 = 0;
            while (index1 < m_size && comparer.Compare(m_items[index1], item) != 0)
            {
                ++index1;
            }

            if (index1 >= m_size)
            {
                return 0;
            }

            int index2 = index1 + 1;
            while (index2 < m_size)
            {
                while (index2 < m_size && comparer.Compare(m_items[index2], item) == 0)
                {
                    ++index2;
                }
                if (index2 < m_size)
                {
                    m_items[index1++] = m_items[index2++];
                }
            }

            Array.Clear(m_items, index1, m_size - index1);

            int num = m_size - index1;
            m_size = index1;

            if (m_size == 0)
            {
                ReturnArray();
            }

            return num;
        }
        
        internal void ReturnArray()
        {
            if (m_items.Length > 0)
            {
                ArrayAllocator.Return(m_items, clearArray: s_clearArrayOnReturn);
            }

            m_items = Array.Empty<T>();
        }
    }
}
