using System;
using System.Collections.Generic;

namespace Konsarpoo.Collections
{
    public partial class Data<T>
    {
        /// <summary>
        /// Cpp vector API.
        /// Resizes the container to contain count elements.
        /// If the current size is greater than count, the container is reduced to its first count elements.
        ///   If the current size is less than count,
        /// 1) additional default-inserted elements are appended
        /// 2) additional copies of value are appended.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="defaultValue"></param>
        public void Resize(int size, T defaultValue = default)
        {
            if (size > m_count)
            {
                Ensure(size, defaultValue);
            }
            else
            {
                while (m_count != 0 && m_count > size)
                {
                    RemoveLast();
                }
            }
        }

        /// <summary>
        /// Array API. Ensures that current Data&lt;T&gt; container has given size.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="defaultValue"></param>
        public void Ensure(int size, T defaultValue = default)
        {
            if (m_count >= size)
            {
                return;
            }

            unchecked { ++m_version; }
            
            var maxSizeOfArray = m_maxSizeOfArray;

            if (m_root == null)
            {
                //common case
                var arrayAllocator = m_allocator.GetDataArrayAllocator();
                
                var storeNode = new StoreNode(arrayAllocator, maxSizeOfArray, Math.Min(maxSizeOfArray, size));

                int startIndex = 0;

                m_count = storeNode.m_size;

                m_root = storeNode;
                m_tailStoreNode = storeNode;

                var setupDefaultValueForArray = EqualityComparer<T>.Default.Equals(defaultValue, Default) == false;

                if (setupDefaultValueForArray || arrayAllocator.CleanArrayReturn == false)
                {
                    var storage = storeNode.Storage;

                    Array.Fill(storage, defaultValue, startIndex, m_count - startIndex);

                    storeNode.Storage = storage;
                }

                var restSize = size - m_count;

                while (restSize > 0)
                {
                    INode node1 = m_root;
                    INode node2;
                    if (node1.Ensure(ref restSize, ref defaultValue, out node2, m_allocator) == false)
                    {
                        m_root = new LinkNode((ushort)(node1.Level + 1), maxSizeOfArray, node1, m_allocator, node2);
                    }
                }

                m_count = size;
                
                UpdateLastNode();

                return;
            }

            if (m_root != null)
            {
                var restSize = size - m_count;
                
                while (restSize > 0)
                {
                    INode node1 = m_root;
                    INode node2;
                    if (node1.Ensure(ref restSize, ref defaultValue, out node2, m_allocator) == false)
                    {
                        m_root = new LinkNode((ushort)(node1.Level + 1), maxSizeOfArray, node1, m_allocator, node2);
                    }
                }

            }

            m_count = size;

            UpdateLastNode();
        }
    }
}