using System;
using System.Collections;
using System.Collections.Generic;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections;

internal class DataFlatStore<T> : IReadOnlyList<T>
{
    private class Store
    {
        public T[] Storage;
        public int Size;

        public Store(T[] p, int s)
        {
            Storage = p;
            Size = s;
        }
    }

    private PoolListBase<Store> m_storage;

    private int m_stepBase;
    private int m_maxSizeOfArray;
    private int m_count;
    
    private Store m_lastStore;
    private IArrayAllocator<Store> m_nodeArrayPoolMixed;
    private IArrayAllocator<T> m_arrayPool;

    public DataFlatStore(int maxSizeOfArray, IArrayAllocator<T> allocator = null)
    {
        m_maxSizeOfArray = maxSizeOfArray;
        m_stepBase = (ushort)Math.Log(m_maxSizeOfArray, 2);

        var defaultAllocatorSetup = KonsarpooAllocatorGlobalSetup.DefaultAllocatorSetup;
        
        m_arrayPool = allocator ?? defaultAllocatorSetup.GetDataStorageAllocator<T>().GetDataArrayAllocator()!;
        m_nodeArrayPoolMixed = defaultAllocatorSetup.GetDataStorageAllocator<Store>().GetDataArrayAllocator()!;
        
        var store = new Store(m_arrayPool.Rent(m_maxSizeOfArray), 0);
        m_lastStore = store;

        m_storage = new PoolListBase<Store>(m_nodeArrayPoolMixed, maxCapacity: 1048576, 2);
        m_storage.Add(store);
    }

    public T this[int index]
    {
        get
        {
            var current = index >> m_stepBase;
            var next = index - (current << m_stepBase);

            if (current < 0 || current > m_storage.m_size)
            {
                throw new IndexOutOfRangeException($"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_storage.Count}.");
            }

            return m_storage.m_items[current].Storage[next];
        }
        set
        {
            var current = index >> m_stepBase;
            var next = index - (current << m_stepBase);

            if (current < 0 || current > m_storage.m_size)
            {
                throw new IndexOutOfRangeException($"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_storage.Count}.");
            }

            m_storage.m_items[current].Storage[next] = value;
        }
    }

    public void Dispose()
    {
        Clear();
    }

    public void Clear()
    {
        foreach (var store in m_storage)
        {
            m_arrayPool.Return(store.Storage);
        }

        m_storage.Clear();
    }

    public int Count => m_count;

    public void Add(T item)
    {
        Store store = m_lastStore;

        if (store.Size == m_maxSizeOfArray)
        {
            m_storage.Add(m_lastStore = store = new Store(m_arrayPool.Rent(m_maxSizeOfArray), 0));
        }

        store.Storage[store.Size] = item;
        store.Size++;

        m_count++;  
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in m_storage.m_items)
        {
            if (item != null)
            {
                for (var i = 0; i < item.Storage.Length && i < item.Size; i++)
                {
                   yield return item.Storage[i];
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}