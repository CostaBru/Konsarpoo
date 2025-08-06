using System;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections;

internal class DataFlatStore<T>
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
    private GcArrayPoolMixedAllocator<Store> m_nodeArrayPoolMixed = new();
    private ArrayAllocatorAllocator<T> m_arrayPool = new();

    public DataFlatStore(int maxSizeOfArray)
    {
        m_maxSizeOfArray = maxSizeOfArray;
        m_stepBase = (ushort)Math.Log(m_maxSizeOfArray, 2);
        
        var store = new Store(m_arrayPool.Rent(m_maxSizeOfArray), 0);
        m_lastStore = store;

        m_storage = new PoolListBase<Store>(maxCapacity: 1048576);
        m_storage.Add(store, m_nodeArrayPoolMixed);
    }

    public T this[int index]
    {
        get
        {
            var current = index >> m_stepBase;
            var next = index - (current << m_stepBase);

            if (current < 0 || current > m_storage.Count)
            {
                throw new IndexOutOfRangeException($"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_storage.Count}.");
            }

            return m_storage[current].Storage[next];
        }
        set
        {
            var current = index >> m_stepBase;
            var next = index - (current << m_stepBase);

            if (current < 0 || current > m_storage.Count)
            {
                throw new IndexOutOfRangeException($"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_storage.Count}.");
            }

            m_storage[current].Storage[next] = value;
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

        m_storage.Clear(m_nodeArrayPoolMixed);
    }

    public int Count => m_count;

    public void Add(T item)
    {
        Store store = m_lastStore;

        if (store.Size == m_maxSizeOfArray)
        {
            m_storage.Add(m_lastStore = store = new Store(m_arrayPool.Rent(m_maxSizeOfArray), 0), m_nodeArrayPoolMixed);
        }

        store.Storage[store.Size] = item;
        store.Size++;

        m_count++;  
    }
}