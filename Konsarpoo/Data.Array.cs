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
            if (m_count > size)
            {
                return;
            }

            if (m_root == null && size <= SmallListCount)
            {
                switch (m_count)
                {
                    case 0:
                        {
                            m_val0 = defaultValue;
                            m_val1 = defaultValue;

                            m_count = size;
                            ++m_version;
                            return;
                        }
                    case 1:

                        m_val1 = defaultValue;

                        m_count = size;
                        ++m_version;
                        return;
                    case 2:

                        m_count = size;
                        return;
                }
            }
            else
            {
                if (m_root == null)
                {
                    //common case
                    var storeNode = new StoreNode(m_pool, m_maxSizeOfArray, size);

                    int startIndex = 0;

                    if (m_count > 0)
                    {
                        storeNode.m_items[0] = m_val0;
                        m_val0 = Default;

                        startIndex++;
                        storeNode.m_size++;

                        if (m_count > 1)
                        {
                            storeNode.m_items[1] = m_val1;
                            m_val1 = Default;

                            startIndex++;
                            storeNode.m_size++;
                        }
                    }

                    m_count = storeNode.m_size;
                    ++m_version;

                    m_root = storeNode;

                    var setupDefaultValueForArray = EqualityComparer<T>.Default.Equals(defaultValue, Default) == false;

                    if (setupDefaultValueForArray)
                    {
                        Array.Fill(storeNode.m_items, defaultValue, startIndex, m_count - startIndex);
                    }

                    var restSize = size - m_count;

                    while (restSize > 0)
                    {
                        INode node1 = m_root;
                        INode node2;
                        if (node1.Ensure(ref restSize, ref defaultValue, out node2, m_maxSizeOfArray) == false)
                        {
                            m_root = new LinkNode(node1.Level + 1, m_maxSizeOfArray, node1, m_nodesPool, node2);
                        }
                    }
                    
                    m_count = size;

                    return;
                }
            }

            if (m_root != null)
            {
                var restSize = size - m_count;
                
                while (restSize > 0)
                {
                    INode node1 = m_root;
                    INode node2;
                    if (node1.Ensure(ref restSize, ref defaultValue, out node2, m_maxSizeOfArray) == false)
                    {
                        m_root = new LinkNode(node1.Level + 1, m_maxSizeOfArray, node1, m_nodesPool, node2);
                    }
                }
            }

            m_count = size;
        }
    }
}