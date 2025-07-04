using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;

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
            /// <summary>
            /// Adds item to the node or creates a new node with new item.
            /// </summary>
            /// <param name="item"></param>
            /// <param name="node"></param>
            /// <param name="capacity"></param>
            /// <returns></returns>
            bool Add(ref T item, out INode node, int capacity = 16);

            /// <summary>
            /// Clears node.
            /// </summary>
            void Clear();

            /// <summary>
            /// Removes last item.
            /// </summary>
            /// <returns></returns>
            bool RemoveLast();

            /// <summary>
            /// Make sure node has given size or max size. If max size reached return new node.
            /// </summary>
            /// <param name="size">rest size</param>
            /// <param name="defaultValue"></param>
            /// <param name="node"></param>
            /// <returns></returns>
            bool Ensure(ref int size, ref T defaultValue, out INode node);

            /// <summary>
            /// Tries to insert item into existing node. Returns true if node can fit new item. Returns false if lastItem is required to push into next index.
            /// </summary>
            /// <param name="index"></param>
            /// <param name="item"></param>
            /// <param name="lastItem"></param>
            /// <returns></returns>
            bool TryInsertAndPush(int index, ref T item, out T lastItem);

            /// <summary>
            /// Removes item at specified index. If node was full then newLastItem replaces the last item in the node
            /// </summary>
            /// <param name="index"></param>
            /// <param name="newLastValue"></param>
            /// <returns>First item in the node.</returns>
            T RemoveAtAndPop(int index, ref T newLastValue);

            /// <summary>
            /// Determines the index of a specific item in the Node.
            /// </summary>
            /// <param name="item"></param>
            /// <param name="startIndex"></param>
            int IndexOf(ref T item, int startIndex);
            
            /// <summary>
            /// Enumerates sub nodes.
            /// </summary>
            [NotNull]
            IEnumerable<INode> Nodes { get; }

            /// <summary>
            /// Gets storage node for given item index.
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            [NotNull]
            INode GetStorageNode(int index);
            
            /// <summary>
            /// Gets or sets the parent node reference.
            /// </summary>
            [CanBeNull]
            INode Parent { get; internal set; }

            /// <summary>
            /// Tries to insert array into existing node. Returns true if node can fit new item. Returns false if lastItem is required to push into next index.
            /// </summary>
            /// <param name="array"></param>
            /// <param name="size"></param>
            /// <param name="node"></param>
            /// <returns></returns>
            bool AddArray(T[] array, int size, out INode node);
        }

        /// <summary>
        /// Tree Leaf/link node class
        /// </summary>
        [DebuggerDisplay("Link. Nodes: {m_nodes.Count}, Level: {Level}")]
        protected sealed class LinkNode : INode
        {
            private const int c_linkNodeCapacity = 1024;
            
            private readonly IArrayAllocator<INode> m_nodesAllocator;
            
            private readonly PoolListBase<INode> m_nodes;
            private readonly int m_level;
            private readonly int m_stepBase;
            private readonly int m_leafCapacity;
            private INode m_parent;

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
            
            public INode GetStorageNode(int index)
            {
                var current = index >> m_stepBase;
                var next = index - (current << m_stepBase);
                
                if (current < 0 || current > m_nodes.m_size)
                {
                    throw new IndexOutOfRangeException($"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_nodes.m_size}.");
                }

                return m_nodes.m_items[current].GetStorageNode(next);
            }

            public INode Parent
            {
                get => m_parent;
                set => m_parent = value;
            }

            public T[] Storage => null;

            public int Size => m_nodes.m_size;

            public LinkNode(INode parent, int level, int leafCapacity, INode child1, IArrayAllocator<INode> nodesAllocator, INode child2 = null)
            {
                m_parent = parent;
                m_level = level;
                m_leafCapacity = leafCapacity;
                m_nodesAllocator = nodesAllocator;

                m_stepBase = (int)Math.Log(Math.Pow(c_linkNodeCapacity, m_level - 1) * m_leafCapacity, 2);

                m_nodes = new PoolListBase<INode>(m_nodesAllocator, c_linkNodeCapacity, capacity: 16);
                m_nodes.Add(child1);

                if (child2 != null)
                {
                    m_nodes.Add(child2);
                }
            }

            public LinkNode(INode parent, LinkNode linkNode)
            {
                m_parent = parent;
                m_level = linkNode.m_level;
                m_leafCapacity = linkNode.m_leafCapacity;
                m_stepBase = linkNode.m_stepBase;
                m_nodesAllocator = linkNode.m_nodesAllocator;

                m_nodes = new PoolListBase<INode>(m_nodesAllocator, c_linkNodeCapacity, capacity: linkNode.m_nodes.m_size);

                foreach (var node in linkNode.m_nodes)
                {
                    if (node is LinkNode ln)
                    {
                        m_nodes.Add(new LinkNode(this, ln));
                    }
                    else if (node is StoreNode sn)
                    {
                        m_nodes.Add(new StoreNode(this, sn));
                    }
                }
            }

            public bool Add(ref T item, out INode node, int capacity = 16)
            {
                if (m_nodes[m_nodes.m_size - 1].Add(ref item, out var node1, capacity) == false)
                {
                    if (m_nodes.m_size == c_linkNodeCapacity)
                    {
                        node = new LinkNode(m_parent, m_level, m_leafCapacity, node1, m_nodesAllocator);
                        return false;
                    }
                    m_nodes.Add(node1);
                }
                node = this;
                return true;
            }

            public bool TryInsertAndPush(int index, ref T item, out T lastItem)
            {
                var current = index >> m_stepBase;
                var next = index - (current << m_stepBase);

                if (current < 0 || current > m_nodes.m_size)
                {
                    throw new IndexOutOfRangeException(
                        $"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_nodes.m_size}.");
                }

                lastItem = default;

                for (int i = current; i < m_nodes.m_items.Length && i < m_nodes.m_size; i++)
                {
                    var node = m_nodes.m_items[i];
                    
                    if (node.TryInsertAndPush(next, ref item, out lastItem))
                    {
                        return true;
                    }

                    item = lastItem;
                    
                    next = 0;
                }

                return false;
            }

            public T RemoveAtAndPop(int index, ref T newLastValue)
            {
                var current = index >> m_stepBase;
                var next = index - (current << m_stepBase);

                if (current < 0 || current > m_nodes.m_size)
                {
                    throw new IndexOutOfRangeException(
                        $"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_nodes.m_size}.");
                }

                T pushBack = default;
                
                for (int i = m_nodes.m_size - 1; i >= current; i--)
                {
                    var node = m_nodes.m_items[i];

                    if (i != current)
                    {
                        pushBack = node.RemoveAtAndPop(0, ref newLastValue);
                        
                        if(node.Size == 0)
                        {
                            m_nodes.RemoveAt(i);
                        }
                    }
                    else
                    {
                        pushBack = node.RemoveAtAndPop(next, ref newLastValue);
                        
                        if(node.Size == 0)
                        {
                            m_nodes.RemoveAt(i);
                        }
                    }
                  
                    newLastValue = pushBack;
                }

                return pushBack;
            }

            public int IndexOf(ref T item, int startIndex)
            {
                var current = startIndex >> m_stepBase;
                var next = startIndex - (current << m_stepBase);
                
                if (current < 0 || current > m_nodes.m_size)
                {
                    return -1;
                }

                for (int i = current; i < m_nodes.m_size; i++)
                {
                    var node = m_nodes[i];

                    var subIndex = node.IndexOf(ref item, next);

                    if (subIndex >= 0)
                    {
                        return subIndex + (i << m_stepBase);
                    }

                    next = 0;
                }

                return -1;
            }

            public IEnumerable<INode> Nodes => m_nodes;
            
            public bool AddArray(T[] array, int size, out INode node)
            {
                if (m_nodes[m_nodes.m_size - 1].AddArray(array, size, out var node1) == false)
                {
                    if (m_nodes.m_size == c_linkNodeCapacity)
                    {
                        node = new LinkNode(m_parent, m_level, m_leafCapacity, node1, m_nodesAllocator);
                        return false;
                    }
                    m_nodes.Add(node1);
                }
                node = this;
                return true;
            }

            public bool Ensure(ref int size, ref T defaultValue, out INode node)
            {
                if (m_nodes[m_nodes.m_size - 1].Ensure(ref size, ref defaultValue, out var node1) == false)
                {
                    if (m_nodes.m_size == c_linkNodeCapacity)
                    {
                        node = new LinkNode(m_parent, m_level, m_leafCapacity, node1, m_nodesAllocator);
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
            private INode m_parent;
            
            public int Level => 0;

            public T[] Storage => m_items;

            public int Size => m_size;

            public INode Parent
            {
                get => m_parent;
                set => m_parent = value;
            }

            public bool AddArray(T[] array, int size, out INode node)
            {
                node = new StoreNode(m_parent, base.ArrayAllocator, array, size);
                return false;
            }

            public StoreNode(INode parent, IArrayAllocator<T> allocator, int maxCapacity, int capacity) : base(allocator, maxCapacity, capacity)
            {
                m_parent = parent;
            }
            
            public StoreNode(INode parent, IArrayAllocator<T> allocator, T[] items, int size) : base(allocator, items.Length, items)
            {
                m_parent = parent;
                m_size = size;
            }

            public StoreNode(INode parent, StoreNode poolList) : base(poolList)
            {
                m_parent = parent;
            }

            public StoreNode(INode parent, IArrayAllocator<T> allocator, int maxCapacity, int capacity, T item)
              : base(allocator, maxCapacity, capacity)
            {
                m_parent = parent;
                
                AddItem(item);
            }

            public bool Add(ref T item, out INode node, int capacity = 16)
            {
                if (m_size == m_maxCapacity)
                {
                    node = new StoreNode(m_parent, base.ArrayAllocator, m_maxCapacity, capacity, item);
                    return false;
                }
                AddItem(item);
                node = this;
                return true;
            }
            
            public bool TryInsertAndPush(int index, ref T item, out T lastItem)
            { 
                if (m_size < m_maxCapacity)
                {
                    if (m_size == m_items.Length)
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

                    lastItem = default;

                    return true;
                }
                else
                {
                    lastItem = m_items[m_size - 1];

                    Array.Copy(m_items, index, m_items, index + 1, m_size - index - 1);

                    m_items[index] = item;

                    return false;
                }
            }

            public T RemoveAtAndPop(int index, ref T newLastValue)
            {
                var pushBackValue = this.m_items[index];

                var wasFull = m_size == m_maxCapacity;
                
                RemoveAt(index);

                if (wasFull)
                {
                    Add(newLastValue);
                }

                return pushBackValue;
            }

            public int IndexOf(ref T item, int startIndex)
            {
                return Array.IndexOf(m_items, item, startIndex, m_size - startIndex);
            }

            public IEnumerable<INode> Nodes => Array.Empty<INode>();
            
            public INode GetStorageNode(int index)
            {
                return this;
            }

            public bool Ensure(ref int extraSize, ref T defaultValue, out INode node)
            {
                var restOfThis = base.m_maxCapacity - m_size;

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
                    var arraySize = Math.Min(m_maxCapacity, extraSize);

                    extraSize -= arraySize;

                    var storeNode = new StoreNode(m_parent, base.ArrayAllocator, m_maxCapacity, arraySize) { m_size = arraySize };

                    if (ArrayAllocator.CleanArrayReturn == false || EqualityComparer<T>.Default.Equals(defaultValue, Default) == false)
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
                T[] vals = base.ArrayAllocator.Rent(newSize);

                Array.Copy(m_items, 0, vals, 0, m_size);

                ArrayAllocator.Return(m_items, clearArray: s_clearArrayOnReturn);

                m_items = vals;

                if (ArrayAllocator.CleanArrayReturn == false || EqualityComparer<T>.Default.Equals(defaultValue, Default) == false)
                {
                    Array.Fill(m_items, defaultValue, m_size, newSize - m_size);
                }
            }
        }
    }
}