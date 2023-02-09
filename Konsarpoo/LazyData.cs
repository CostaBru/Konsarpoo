using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Konsarpoo.Collections;

public class LazyData<T> : Data<T>
{
    private LazyData()
    {
    }

    /// <summary>
    /// Returns default if array storage was not initialized.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public T GetLazy(int index)
    {
        return m_root.Get(index);
    }

    /// <summary>
    /// Creates a lazy data collection that allocates actual array on write access request.
    /// </summary>
    /// <param name="size"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static LazyData<T> Create(int size, T defaultValue = default)
    {
        var lazyData = new LazyData<T>();

        lazyData.EnsureLazy(size, defaultValue);

        return lazyData;
    }

    private void EnsureLazy(int size, T defaultValue = default)
    {
        if (m_count > size)
        {
            return;
        }

        unchecked
        {
            ++m_version;
        }

        var maxSizeOfArray = m_maxSizeOfArray < 0 ? ArrayPoolGlobalSetup.MaxSizeOfArray : m_maxSizeOfArray;

        var storeNode = new LazyStoreNode(m_pool, maxSizeOfArray) { Default = defaultValue, m_size = Math.Min(size, maxSizeOfArray) };

        m_count = storeNode.m_size;

        m_root = storeNode;

        var restSize = size - m_count;

        while (restSize > 0)
        {
            INode node1 = m_root;
            INode node2;
            if (node1.Ensure(ref restSize, ref defaultValue, out node2) == false)
            {
                m_root = new LinkNode(node1.Level + 1, m_maxSizeOfArray, node1, m_nodesPool, node2);
            }
        }

        m_count = size;
    }

    /// <summary>
    /// Tree lazy data storage class.
    /// </summary>
    [DebuggerDisplay("Lazy store. Size: {m_size}. Data array len {m_data.m_items.Length}.")]
    internal sealed class LazyStoreNode : INode
    {
        public int Level => 0;

        public T Get(int index)
        {
            if (m_data.m_items.Length == 0)
            {
                CreateData();
            }

            return m_data[index];
        }

        private void CreateData()
        {
            T[] vals = m_data.m_arrayPool.Rent(m_data.Size);

            if (EqualityComparer<T>.Default.Equals(Default, Data<T>.Default) == false ||
                s_itemsArrayPool.CleanArrayReturn == false)
            {
                Array.Fill(vals, Default, 0, m_data.Size);
            }

            m_data.m_items = vals;
        }

        public void Set(int index, T value)
        {
            if (m_data.m_items.Length == 0)
            {
                CreateData();
            }

            m_data[index] = value;
        }

        public T[] Storage => m_data.m_items.Length == 0 ? null : m_data.m_items;

        public int m_size;

        public int Size => m_data.m_items.Length == 0 ? m_size : m_data.m_size;

        public T Default;

        private StoreNode m_data;

        public LazyStoreNode(IArrayPool<T> pool, int maxCapacity)
        {
            m_data = new StoreNode(pool, maxCapacity, 0);
        }

        public LazyStoreNode(LazyStoreNode node)
        {
            m_data = new StoreNode(node.m_data);
        }

        public LazyStoreNode(IArrayPool<T> pool, int maxCapacity, int capacity, T item)
        {
            m_data = new StoreNode(pool, maxCapacity, capacity);

            m_data.AddItem(item);
        }

        public ref T this[int index]
        {
            get
            {
                if (m_data.m_items.Length == 0)
                {
                    CreateData();
                }

                return ref m_data.m_items[index];
            }
        }

        public bool Add(ref T item, out INode node, int capacity = 16)
        {
            if (m_size == m_data.m_maxCapacity)
            {
                node = new LazyStoreNode(m_data.m_arrayPool, m_data.m_maxCapacity, capacity, item);
                return false;
            }

            m_data.AddItem(item);
            node = this;
            return true;
        }

        public void Clear()
        {
            if (m_data.m_items.Length == 0)
            {
                m_size = 0;
            }
            else
            {
                m_data.Clear();
            }
        }

        public bool RemoveLast()
        {
            if (m_data.m_items.Length == 0)
            {
                if (m_size > 0)
                {
                    m_size--;

                    return true;
                }

                return false;
            }

            return m_data.RemoveLast();
        }

        public bool Ensure(ref int extraSize, ref T defaultValue, out INode node)
        {
            var restOfThis = m_data.m_maxCapacity - m_size;

            //can extend this
            if (extraSize <= restOfThis)
            {
                Ensure(defaultValue, m_size + extraSize);

                m_size += extraSize;

                extraSize = 0;

                node = this;

                return true;
            }

            if (restOfThis > 0)
            {
                Ensure(defaultValue, m_size + restOfThis);

                m_size += restOfThis;

                extraSize -= restOfThis;

                node = this;

                return true;
            }

            if (extraSize > 0)
                //allocate new with rest
            {
                var arraySize = Math.Min(m_data.m_maxCapacity, extraSize);

                extraSize -= arraySize;

                var storeNode = new LazyStoreNode(m_data.m_arrayPool, m_data.m_maxCapacity) { m_size = arraySize };

                node = storeNode;

                return false;
            }

            node = this;

            return true;
        }

        private void Ensure(T defaultValue, int newSize)
        {
            m_size = newSize;

            Default = defaultValue;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (m_data.m_items.Length == 0)
            {
                for (int i = 0; i < m_size; i++)
                {
                    yield return Default;
                }
            }
            else
            {
                var enumerator = m_data.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}