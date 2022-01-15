using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    public partial class Data<T>
    {
         /// <summary>
        /// Base tree node interface
        /// </summary>
        public interface INodeBase : IEnumerable<T>
        {
            /// <summary>
            /// Level of current leave.
            /// </summary>
            int Level { get; }

            /// <summary>
            /// Return item by reference using zero based index.
            /// </summary>
            /// <param name="index"></param>
            ref T this[int index] { get; }
            
            /// <summary>
            /// Returns internal storage array if node is store node otherwise null.
            /// </summary>
            [CanBeNull] 
            T[] Storage { get; }
            
            /// <summary>
            /// Gets size of node.
            /// </summary>
            int Size { get; }
        }

        /// <summary>
        /// Tree node interface.
        /// </summary>
        public interface INode : INodeBase 
        {
            bool Add(ref T item, out INode node, int capacity = 16);

            void Clear();

            bool RemoveLast();

            bool Ensure(ref int size, ref T defaultValue, out INode node,  int maxSizeOfArray);
        }

        /// <summary>
        /// Tree Leaf/link node class
        /// </summary>
        [DebuggerDisplay("Link. Nodes: {m_nodes.Count}, Level: {Level}")]
        private sealed class LinkNode : INode
        {
            private const int c_intermediateCapacity = 1024;
            
            private readonly PoolListBase<INode> m_nodes;
            private readonly int m_level;
            private readonly int m_stepBase;
            private readonly int m_leafCapacity;
            private readonly IArrayPool<INode> m_nodesPool;

            public int Level => m_level;

            
            public ref T this[int index]
            {
                get
                {
                    var current = index >> m_stepBase;
                    var next = index - (current << m_stepBase);

                    if (current < 0 || current > m_nodes.m_size)
                    {
                        throw new IndexOutOfRangeException($"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_nodes.m_size}.");
                    }

                    return ref m_nodes.m_items[current][next];
                }
            }

            public T[] Storage => null;

            public int Size => m_nodes.m_size;

            public LinkNode(int level, int leafCapacity, INode child1, IArrayPool<INode> nodesPool, INode child2 = null)
            {
                m_level = level;
                m_leafCapacity = leafCapacity;
                m_nodesPool = nodesPool;

                m_stepBase = (int)Math.Log(Math.Pow(c_intermediateCapacity, m_level - 1) * m_leafCapacity, 2);

                m_nodes = new PoolListBase<INode>(nodesPool, c_intermediateCapacity, capacity: 16);
                m_nodes.Add(child1);

                if (child2 != null)
                {
                    m_nodes.Add(child2);
                }
            }

            public LinkNode(LinkNode linkNode, IArrayPool<INode> nodesPool)
            {
                m_level = linkNode.m_level;
                m_leafCapacity = linkNode.m_leafCapacity;
                m_stepBase = linkNode.m_stepBase;
                m_nodesPool = nodesPool;

                m_nodes = new PoolListBase<INode>(m_nodesPool, c_intermediateCapacity, capacity: linkNode.m_nodes.m_size);

                foreach (var node in linkNode.m_nodes)
                {
                    if (node is LinkNode ln)
                    {
                        m_nodes.Add(new LinkNode(ln, nodesPool));
                    }
                    else if (node is StoreNode sn)
                    {
                        m_nodes.Add(new StoreNode(sn));
                    }
                }
            }

            public bool Add(ref T item, out INode node, int capacity = 16)
            {
                if (m_nodes[m_nodes.Count - 1].Add(ref item, out var node1, capacity) == false)
                {
                    if (m_nodes.m_size == c_intermediateCapacity)
                    {
                        node = new LinkNode(m_level, m_leafCapacity, node1, m_nodesPool);
                        return false;
                    }
                    m_nodes.Add(node1);
                }
                node = this;
                return true;
            }
            
            public bool Ensure(ref int size, ref T defaultValue, out INode node, int maxSizeOfArray)
            {
                if (m_nodes[m_nodes.Count - 1].Ensure(ref size, ref defaultValue, out var node1, maxSizeOfArray) == false)
                {
                    if (m_nodes.m_size == c_intermediateCapacity)
                    {
                        node = new LinkNode(m_level, m_leafCapacity, node1, m_nodesPool);
                        return false;
                    }
                    m_nodes.Add(node1);
                }
                node = this;
                return true;
            }

            public void Clear()
            {
                foreach (INode node in m_nodes)
                {
                    node.Clear();
                }

                m_nodes.Clear();
            }

            public bool RemoveLast()
            {
                var node = m_nodes[m_nodes.m_size - 1];

                node.RemoveLast();

                if (node.Size <= 0)
                {
                    m_nodes.RemoveLast();
                }

                return m_nodes.m_size == 0;
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (INode node in m_nodes)
                {
                    if (node is StoreNode storeNode)
                    {
                        for (int i = 0; i < storeNode.m_size && i < storeNode.m_items.Length; i++)
                        {
                            yield return storeNode.m_items[i];
                        }
                    }
                    else
                    {
                        foreach (T obj in node)
                        {
                            yield return obj;
                        }
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Tree data storage class.
        /// </summary>
        [DebuggerDisplay("Store. Size: {m_size}")]
        internal sealed class StoreNode : PoolListBase<T>, INode
        {
            public int Level => 0;

            public T[] Storage => m_items;

            public int Size => m_size;

            public StoreNode(IArrayPool<T> pool, int maxCapacity, int capacity) : base(pool, maxCapacity, capacity)
            {
            }

            public StoreNode(StoreNode poolList) : base(poolList)
            {
            }

            public StoreNode(IArrayPool<T> pool, int maxCapacity, int capacity, T item)
              : base(pool, maxCapacity, capacity)
            {
                AddItem(item);
            }

            public bool Add(ref T item, out INode node, int capacity = 16)
            {
                if (m_size == m_maxCapacity)
                {
                    node = new StoreNode(base.m_arrayPool, m_maxCapacity, capacity, item);
                    return false;
                }
                AddItem(item);
                node = this;
                return true;
            }

            public bool Ensure(ref int extraSize, ref T defaultValue, out INode node, int maxSizeOfArray)
            {
                var restOfThis = maxSizeOfArray - m_size;

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
                
                if(extraSize > 0)
                //allocate new with rest
                {
                    var arraySize = Math.Min(maxSizeOfArray, extraSize);

                    extraSize -= arraySize;

                    var storeNode = new StoreNode(base.m_arrayPool, m_maxCapacity, arraySize) { m_size = arraySize };

                    if (EqualityComparer<T>.Default.Equals(defaultValue, Default) == false)
                    {
                        Array.Fill(storeNode.m_items, defaultValue, 0, arraySize);
                    }

                    node = storeNode;
                    
                    return false;
                }
                
                node = this;

                return true;
            }

            private void Ensure(T defaultValue, int newSize)
            {
                T[] vals = base.m_arrayPool.Rent(newSize);

                Array.Copy(m_items, 0, vals, 0, m_size);

                base.m_arrayPool.Return(m_items, clearArray: true);

                m_items = vals;

                if (EqualityComparer<T>.Default.Equals(defaultValue, Default) == false)
                {
                    Array.Fill(m_items, defaultValue, m_size, newSize - m_size);
                }
            }
        }
    }
}