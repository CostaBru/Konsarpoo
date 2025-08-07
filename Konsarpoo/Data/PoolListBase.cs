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
            m_items = allocator.Rent(Math.Min(capacity, m_maxCapacity));
        }
        
        public PoolListBase(int maxCapacity)
        {
            m_maxCapacity = maxCapacity;
        }
        
        public PoolListBase(int maxCapacity, T[] items)
        {
            m_maxCapacity = maxCapacity;
            m_items = items;
        }

        public PoolListBase(PoolListBase<T> poolList, IArrayAllocator<T> allocator)
        {
            var newArr = allocator.Rent(poolList.m_items.Length);

            Array.Copy(poolList.m_items, 0, newArr, 0, poolList.m_size);

            m_items = newArr;
            m_size = poolList.m_size;
            m_maxCapacity = poolList.m_maxCapacity;
        }

        public bool Add(T item, IArrayAllocator<T> arrayAllocator)
        {
            AddItem(item, arrayAllocator);
         
            return true;
        }

        public void AddItem(T item, IArrayAllocator<T> arrayAllocator)
        {
            if (m_size + 1 > (m_maxCapacity ))
            {
                throw new InvalidOperationException($"Cannot add more items. Max size {m_maxCapacity} has reached.");
            }

            AddItemCore(item, ref m_items, ref m_size, m_maxCapacity, arrayAllocator);
        }

        internal static void AddItemCore(T item, ref T[] storage, ref int size, int maxCapacity, IArrayAllocator<T> arrayAllocator)
        {
            if (size == storage.Length)
            {
                var newCapacity = Math.Min(Math.Max(storage.Length * 2, 2), maxCapacity);

                T[] vals = arrayAllocator.Rent(newCapacity);

                if (size > 0)
                {
                    Array.Copy(storage, 0, vals, 0, size);
                }

                arrayAllocator.Return(storage, clearArray: s_clearArrayOnReturn);

                storage = vals;
            }

            storage[size] = item;
            
            size++;
        }

        public void Clear(IArrayAllocator<T> arrayAllocator)
        {
            ReturnArray(arrayAllocator, ref m_items);

            m_size = 0;
        }

        public bool RemoveLast(IArrayAllocator<T> arrayAllocator)
        {
            if (m_size > 0)
            {
                m_size--;

                m_items[m_size] = Default;
            }

            if (m_size == 0)
            {
                ReturnArray(arrayAllocator, ref m_items);

                return true;
            }

            return false;
        }

        public void RemoveAt(int index, IArrayAllocator<T> arrayAllocator)
        {
            RemoveAtCore(index, ref m_size, ref m_items, arrayAllocator);
        }

        internal static void RemoveAtCore(int index, ref int size, ref T[] storage, IArrayAllocator<T> arrayAllocator)
        {
            if (size > 0)
            {
                size -= 1;

                if (index < size)
                {
                    Array.Copy(storage, index + 1, storage, index, size - index);
                }

                storage[size] = Default;

                if (size == 0)
                {
                    ReturnArray(arrayAllocator, ref storage);
                }
            }
        }

        public void Insert(int index, T item, IArrayAllocator<T> arrayAllocator)
        {
            if (m_size + 1 > (m_maxCapacity))
            {
                throw new InvalidOperationException($"Cannot add more items. Max size {m_maxCapacity} has reached");
            }
            
            if (m_size == m_items.Length)
            {
                var newCapacity = Math.Min(Math.Max(m_items.Length * 2, 2), m_maxCapacity);

                T[] vals = arrayAllocator.Rent(newCapacity);

                if (m_size > 0)
                {
                    Array.Copy(m_items, 0, vals, 0, m_size);
                }

                arrayAllocator.Return(m_items, clearArray: s_clearArrayOnReturn);

                m_items = vals;
            }

            if (index < m_size)
            {
                Array.Copy(m_items, index, m_items, index + 1, m_size - index);
            }

            m_items[index] = item;
            m_size += 1;
        }

        public int RemoveAll(Func<T, bool> match, IArrayAllocator<T> arrayAllocator)
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
                ReturnArray(arrayAllocator, ref m_items);
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

        internal int RemoveAll(T item, IComparer<T> comparer, IArrayAllocator<T> arrayAllocator)
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
                ReturnArray(arrayAllocator, ref m_items);
            }

            return num;
        }
        
        internal static void ReturnArray(IArrayAllocator<T> arrayAllocator, ref T[] storage)
        {
            if (storage.Length > 0)
            {
                arrayAllocator.Return(storage, clearArray: s_clearArrayOnReturn);
            }

            storage = Array.Empty<T>();
        }
    }
}
