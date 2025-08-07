using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections
{
    public partial class Data<T>
    {
        /// <summary>
        /// Tree node interface.
        /// </summary>
        public interface INode : IEnumerable<T>
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
            /// Returns true if node is storage node.
            /// </summary>
            bool HasStorage { get; }
            
            /// <summary>
            /// Gets size of node.
            /// </summary>
            int Size { get; }
            
            /// <summary>
            /// Adds item to the node or creates a new node with new item.
            /// </summary>
            /// <param name="item"></param>
            /// <param name="node"></param>
            /// <param name="capacity"></param>
            /// <param name="allocator"></param>
            /// <returns></returns>
            bool Add(ref T item, out INode node, int capacity, IDataAllocatorSetup<T> allocator);

            /// <summary>
            /// Clears node.
            /// </summary>
            void Clear(IDataAllocatorSetup<T> allocator);

            /// <summary>
            /// Removes last item.
            /// </summary>
            /// <returns></returns>
            bool RemoveLast(IDataAllocatorSetup<T> allocator);

            /// <summary>
            /// Make sure node has given size or max size. If max size reached return new node.
            /// </summary>
            /// <param name="size">rest size</param>
            /// <param name="defaultValue"></param>
            /// <param name="node"></param>
            /// <param name="allocator"></param>
            /// <returns></returns>
            bool Ensure(ref int size, ref T defaultValue, out INode node, IDataAllocatorSetup<T> allocator);

            /// <summary>
            /// Tries to insert item into existing node. Returns true if node can fit new item. Returns false if lastItem is required to push into next index.
            /// </summary>
            /// <param name="index"></param>
            /// <param name="item"></param>
            /// <param name="lastItem"></param>
            /// <param name="allocator"></param>
            /// <returns></returns>
            bool TryInsertAndPush(int index, ref T item, out T lastItem, IDataAllocatorSetup<T> allocator);

            /// <summary>
            /// Removes item at specified index. If node was full then newLastItem replaces the last item in the node
            /// </summary>
            /// <param name="index"></param>
            /// <param name="newLastValue"></param>
            /// <param name="allocator"></param>
            /// <param name="last"></param>
            /// <returns>First item in the node.</returns>
            T RemoveAtAndPop(int index, ref T newLastValue, IDataAllocatorSetup<T> allocator, ref bool last);

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
            /// Tries to insert array into existing node. Returns true if node can fit new item. Returns false if lastItem is required to push into next index.
            /// </summary>
            /// <param name="array"></param>
            /// <param name="size"></param>
            /// <param name="node"></param>
            /// <param name="allocator"></param>
            /// <returns></returns>
            bool AddArray(T[] array, int size, out INode node, IDataAllocatorSetup<T> allocator);
        }

        /// <summary>
        /// Tree Leaf/link node class
        /// </summary>
        [DebuggerDisplay("Link. Nodes: {m_nodes.Count}, Level: {Level}")]
        protected sealed class LinkNode : INode
        {
            private const int c_linkNodeCapacity = ushort.MaxValue + 1;
            
            private readonly PoolListBase<INode> m_nodes;
            private readonly ushort m_level;
            private readonly ushort m_stepBase;
            private readonly int m_leafCapacity;

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

            public T[] Storage => null;

            public bool HasStorage => false;

            public int Size => m_nodes.m_size;

            public LinkNode(ushort level, int leafCapacity, INode child1, IDataAllocatorSetup<T> nodesAllocator,  INode child2 = null)
            {
                m_level = level;
                m_leafCapacity = leafCapacity;

                m_stepBase = (ushort)Math.Log(Math.Pow(c_linkNodeCapacity, m_level - 1) * m_leafCapacity, 2);

                var nodesArrayAllocator = nodesAllocator.GetNodesArrayAllocator();
                
                m_nodes = new PoolListBase<INode>(nodesArrayAllocator, c_linkNodeCapacity, capacity: 16);
                m_nodes.Add(child1, nodesArrayAllocator);

                if (child2 != null)
                {
                    m_nodes.Add(child2, nodesArrayAllocator);
                }
            }

            public LinkNode(LinkNode linkNode, IDataAllocatorSetup<T> allocator)
            {
                m_level = linkNode.m_level;
                m_leafCapacity = linkNode.m_leafCapacity;
                m_stepBase = linkNode.m_stepBase;

                var nodesAllocator = allocator.GetNodesArrayAllocator();
                var dataArrayAllocator = allocator.GetDataArrayAllocator();
                
                m_nodes = new PoolListBase<INode>(nodesAllocator, c_linkNodeCapacity, capacity: linkNode.m_nodes.m_size);

                foreach (var node in linkNode.m_nodes)
                {
                    if (node is LinkNode ln)
                    {
                        m_nodes.Add(new LinkNode(ln, allocator), nodesAllocator);
                    }
                    else if (node is StoreNode sn)
                    {
                        m_nodes.Add(new StoreNode(sn, dataArrayAllocator), nodesAllocator);
                    }
                }
            }

            public bool Add(ref T item, out INode node, int capacity, IDataAllocatorSetup<T> allocator)
            {
                if (m_nodes[m_nodes.m_size - 1].Add(ref item, out var node1, capacity, allocator) == false)
                {
                    if (m_nodes.m_size == c_linkNodeCapacity)
                    {
                        node = new LinkNode(m_level, m_leafCapacity, node1, allocator);
                        return false;
                    }
                    m_nodes.Add(node1, allocator.GetNodesArrayAllocator());
                }
                node = this;
                return true;
            }

            public bool TryInsertAndPush(int index, ref T item, out T lastItem, IDataAllocatorSetup<T> allocator)
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
                    
                    if (node.TryInsertAndPush(next, ref item, out lastItem, allocator))
                    {
                        return true;
                    }

                    item = lastItem;
                    
                    next = 0;
                }

                return false;
            }

            public T RemoveAtAndPop(int index, ref T newLastValue, IDataAllocatorSetup<T> allocator, ref bool last)
            {
                var current = index >> m_stepBase;
                var next = index - (current << m_stepBase);

                if (current < 0 || current > m_nodes.m_size)
                {
                    throw new IndexOutOfRangeException(
                        $"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_nodes.m_size}.");
                }

                T pushBack = default;
                
                var nodesArrayAllocator = allocator.GetNodesArrayAllocator();
                
                for (int i = m_nodes.m_size - 1; i >= current; i--)
                {
                    var node = m_nodes.m_items[i];

                    if (i != current)
                    {
                        pushBack = node.RemoveAtAndPop(0, ref newLastValue, allocator, ref last);
                        
                        if(node.Size == 0)
                        {
                            m_nodes.RemoveAt(i, nodesArrayAllocator);
                        }
                    }
                    else
                    {
                        bool _ = true;
                        
                        pushBack = node.RemoveAtAndPop(next, ref newLastValue, allocator, ref _);
                        
                        if(node.Size == 0)
                        {
                            m_nodes.RemoveAt(i, nodesArrayAllocator);
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
            
            public bool AddArray(T[] array, int size, out INode node, IDataAllocatorSetup<T> allocator)
            {
                if (m_nodes[m_nodes.m_size - 1].AddArray(array, size, out var node1, allocator) == false)
                {
                    if (m_nodes.m_size == c_linkNodeCapacity)
                    {
                        node = new LinkNode(m_level, m_leafCapacity, node1, allocator);
                        return false;
                    }
                    m_nodes.Add(node1, allocator.GetNodesArrayAllocator());
                }
                node = this;
                return true;
            }

            public bool Ensure(ref int size, ref T defaultValue, out INode node, IDataAllocatorSetup<T> allocator)
            {
                if (m_nodes[m_nodes.m_size - 1].Ensure(ref size, ref defaultValue, out var node1, allocator) == false)
                {
                    if (m_nodes.m_size == c_linkNodeCapacity)
                    {
                        node = new LinkNode(m_level, m_leafCapacity, node1, allocator);
                        return false;
                    }
                    m_nodes.Add(node1, allocator.GetNodesArrayAllocator());
                }
                node = this;
                return true;
            }

            public void Clear(IDataAllocatorSetup<T> allocator)
            {
                foreach (INode node in m_nodes)
                {
                    node.Clear(allocator);
                }

                m_nodes.Clear(allocator.GetNodesArrayAllocator());
            }

            public bool RemoveLast(IDataAllocatorSetup<T> allocator)
            {
                var node = m_nodes[m_nodes.m_size - 1];

                node.RemoveLast(allocator);

                if (node.Size <= 0)
                {
                    m_nodes.RemoveLast(allocator.GetNodesArrayAllocator());
                }

                return m_nodes.m_size == 0;
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (INode node in m_nodes)
                {
                    if (node is StoreNode storeNode)
                    {
                        var storage = storeNode.m_list.m_items;

                        for (int i = 0; i < storeNode.m_list.m_size && i < storage.Length; i++)
                        {
                            yield return storage[i];
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
        [DebuggerDisplay("Store. Size: {m_list.m_size}")]
        internal class StoreNode : INode
        {
            public int Level => 0;

            public ref T this[int index]
            {
                get
                {
                    if (index >= m_list.m_size)
                    {
                        throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_list.m_size}).");
                    }
                    
                    return ref m_list.m_items[index];
                }
            }

            T[] INode.Storage
            {
                get
                {
                    OnStorageAccess();
                    
                    return m_list.m_items;
                }
            }
            
            public T[] Storage => m_list.m_items;

            public virtual void OnStorageAccess()
            {
            }
            
            public virtual void OnStorageDone()
            {
            }

            public virtual void OnStorageChanged()
            {
            }

            public bool HasStorage => true;

            public int Size => m_list.m_size;

            internal PoolListBase<T> m_list;

            public bool AddArray(T[] array, int size, out INode node, IDataAllocatorSetup<T> allocator)
            {
                node = new StoreNode(array, size);
                return false;
            }

            public StoreNode(IArrayAllocator<T> allocator, int maxCapacity, int capacity)
            {
                m_list = new PoolListBase<T>(allocator, maxCapacity, capacity);
            }
            
            public StoreNode(T[] items, int size) 
            {
                m_list = new PoolListBase<T>(items.Length, items);
                m_list.m_size = size;
            }

            public StoreNode(StoreNode poolList, IArrayAllocator<T> allocator) 
            {
                m_list = new PoolListBase<T>(poolList.m_list, allocator);
            }

            public StoreNode(IArrayAllocator<T> allocator, int maxCapacity, int capacity, T item)
            {
                m_list = new PoolListBase<T>(allocator, maxCapacity, capacity);
                m_list.AddItem(item, allocator);
                OnStorageChanged();
            }

            public bool Add(ref T item, out INode node, int capacity, IDataAllocatorSetup<T> allocator)
            {
                if (m_list.m_size == m_list.m_maxCapacity)
                {
                    node = new StoreNode(allocator.GetDataArrayAllocator(), m_list.m_maxCapacity, capacity, item);
                    return false;
                }

                OnStorageAccess();
                m_list.AddItem(item, allocator.GetDataArrayAllocator());
                OnStorageChanged();
                node = this;
                return true;
            }

            public void RemoveItemAt(int index, IArrayAllocator<T> arrayAllocator)
            {
                OnStorageAccess();
                
                m_list.RemoveAt(index, arrayAllocator);

                OnStorageChanged(); 
            }

            public void Clear(IDataAllocatorSetup<T> allocator)
            {
                OnStorageAccess();
                
                m_list.Clear(allocator.GetDataArrayAllocator());
                
                OnStorageChanged(); 
            }

            public bool RemoveLast(IDataAllocatorSetup<T> allocator)
            {
                OnStorageAccess();
                
                var removeLast = m_list.RemoveLast(allocator.GetDataArrayAllocator());
                
                OnStorageChanged(); 

                return removeLast;
            }

            public bool TryInsertAndPush(int index, ref T item, out T lastItem, IDataAllocatorSetup<T> allocator)
            {
                OnStorageAccess();
                
                var storage = Storage!;
                
                if (m_list.m_size < m_list.m_maxCapacity)
                {
                    if (m_list.m_size == storage.Length)
                    {
                        var newCapacity = Math.Min(Math.Max(storage.Length * 2, 2), m_list.m_maxCapacity);

                        var arrayAllocator = allocator.GetDataArrayAllocator();
                        
                        T[] vals = arrayAllocator.Rent(newCapacity);

                        if (m_list.m_size > 0)
                        {
                            Array.Copy(storage, 0, vals, 0, m_list.m_size);
                        }

                        arrayAllocator.Return(storage, clearArray: s_clearArrayOnReturn);

                        storage = vals;
                        m_list.m_items = vals;
                    }

                    if (index < m_list.m_size)
                    {
                        Array.Copy(storage, index, storage, index + 1, m_list.m_size - index);
                    }

                    storage[index] = item;
                    m_list.m_size += 1;
                    
                    OnStorageChanged();

                    lastItem = default;

                    return true;
                }
                else
                {
                    lastItem = storage[m_list.m_size - 1];

                    Array.Copy(storage, index, storage, index + 1, m_list.m_size - index - 1);

                    storage[index] = item;
                    
                    OnStorageChanged();

                    return false;
                }
            }

            public T RemoveAtAndPop(int index, ref T newLastValue, IDataAllocatorSetup<T> allocator, ref bool last)
            {
                OnStorageAccess();
                
                var storage = this.Storage!;
                
                var pushBackValue = storage[index];

                var dataArrayAllocator = allocator.GetDataArrayAllocator();
                
                m_list.RemoveAt(index, dataArrayAllocator);

                if (last == false)
                {
                    last = true;
                }
                else
                {
                    m_list.AddItem(newLastValue, dataArrayAllocator);
                }

                OnStorageChanged();

                return pushBackValue;
            }

            public int IndexOf(ref T item, int startIndex)
            {
                return Array.IndexOf(m_list.m_items, item, startIndex, m_list.m_size - startIndex);
            }

            public IEnumerable<INode> Nodes => Array.Empty<INode>();
            
            public INode GetStorageNode(int index)
            {
                return this;
            }

            public bool Ensure(ref int extraSize, ref T defaultValue, out INode node, IDataAllocatorSetup<T> allocator)
            {
                var restOfThis = m_list.m_maxCapacity - m_list.m_size;

                var arrayAllocator = allocator.GetDataArrayAllocator();
                
                //can extend this
                if (extraSize <= restOfThis)
                {
                    Ensure(defaultValue, (m_list.m_size + extraSize), arrayAllocator);

                    m_list.m_size += extraSize;

                    extraSize = 0;

                    node = this;

                    return true;
                }

                if (restOfThis > 0)
                {
                    Ensure(defaultValue, (m_list.m_size + restOfThis), arrayAllocator);

                    m_list.m_size += restOfThis;
                    
                    extraSize -= restOfThis;
                    
                    node = this;

                    return true;
                }
                
                if(extraSize > 0)
                //allocate new with rest
                {
                    var arraySize = Math.Min(m_list.m_maxCapacity, extraSize);

                    extraSize -= arraySize;

                    var storeNode = new StoreNode(arrayAllocator, m_list.m_maxCapacity, arraySize);

                    storeNode.m_list.m_size = arraySize;

                    if (arrayAllocator.CleanArrayReturn == false || EqualityComparer<T>.Default.Equals(defaultValue, Default) == false)
                    {
                         storeNode.OnStorageAccess();

                        Array.Fill(storeNode.Storage, defaultValue, 0, arraySize);

                        storeNode.OnStorageChanged();
                    }

                    node = storeNode;
                    
                    return false;
                }
                
                node = this;

                return true;
            }

            private void Ensure(T defaultValue, int newSize, IArrayAllocator<T> allocator)
            {
                OnStorageAccess();
                
                var storage = Storage;

                T[] vals = allocator.Rent(newSize);

                Array.Copy(storage, 0, vals, 0, m_list.m_size);

                allocator.Return(storage, clearArray: s_clearArrayOnReturn);

                storage = vals;

                if (allocator.CleanArrayReturn == false || EqualityComparer<T>.Default.Equals(defaultValue, Default) == false)
                {
                    Array.Fill(storage, defaultValue, m_list.m_size, newSize - m_list.m_size);
                }

                m_list.m_items = storage;
                
                OnStorageChanged();
            }

            public IEnumerator<T> GetEnumerator()
            {
                OnStorageAccess();
                
                var items = m_list.m_items;
                
                for (var i = 0; i < items.Length && i < m_list.m_size; i++)
                {
                    var item = items[i];
                    
                    yield return item;
                }
                
                OnStorageDone();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int RemoveAll(T item, IComparer<T> comparer, IArrayAllocator<T> allocator)
            {
                OnStorageAccess();

                var removeAll = m_list.RemoveAll(item, comparer, allocator);

                if (removeAll > 0)
                {
                    OnStorageChanged();
                }
                else
                {
                    OnStorageDone();
                }

                return removeAll;
            }

            public int RemoveAll(Func<T, bool> match, IArrayAllocator<T> allocator)
            {
                OnStorageAccess();

                var removeAll = m_list.RemoveAll(match, allocator);

                if (removeAll > 0)
                {
                    OnStorageChanged();
                }
                else
                {
                    OnStorageDone();
                }

                return removeAll;
            }

            public int FindIndex(Predicate<T> match, int start)
            {
                var storage = m_list.m_items;
                
                return new DataRs<T>(storage, m_list.m_size)
                    .FindIndexPredicate(match, start);
            }
        }

        internal class FileStoreNode : StoreNode
        {
            public int ArrayIndex { get; set; }

            public IDataArrayFileAccessor DataArrayFileAccessor  { get; set; }
            
            public FileStoreNode(IArrayAllocator<T> allocator, int maxCapacity, int capacity, int arrayIndex, IDataArrayFileAccessor dataArrayFileAccessor) : base(allocator, maxCapacity, capacity)
            {
                ArrayIndex = arrayIndex;
                DataArrayFileAccessor = dataArrayFileAccessor;
            }

            public FileStoreNode(T[] items, int size, int arrayIndex, IDataArrayFileAccessor dataArrayFileAccessor) : base(items, size)
            {
                ArrayIndex = arrayIndex;
                DataArrayFileAccessor = dataArrayFileAccessor;
            }

            public FileStoreNode(StoreNode poolList, IArrayAllocator<T> allocator, int arrayIndex, IDataArrayFileAccessor dataArrayFileAccessor) : base(poolList, allocator)
            {
                ArrayIndex = arrayIndex;
                DataArrayFileAccessor = dataArrayFileAccessor;
            }

            public FileStoreNode(IArrayAllocator<T> allocator, int maxCapacity, int capacity, T item, int arrayIndex, IDataArrayFileAccessor dataArrayFileAccessor) : base(allocator, maxCapacity, capacity, item)
            {
                ArrayIndex = arrayIndex;
                DataArrayFileAccessor = dataArrayFileAccessor;
            }

            public override void OnStorageAccess()
            {
                m_list.m_items = DataArrayFileAccessor.ReadArray<T>(ArrayIndex);
            }

            public override void OnStorageChanged()
            {
                DataArrayFileAccessor.WriteArray<T>(ArrayIndex, m_list.m_items);
            }

            public override void OnStorageDone()
            {
            }
        }
    }
}