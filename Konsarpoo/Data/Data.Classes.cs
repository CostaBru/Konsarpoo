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
            ref T GetItemByRef(int index);

            /// <summary>
            /// Return item using zero based index.
            /// </summary>
            /// <param name="index"></param>
            T this[int index]
            {
                get;
                set;
            }

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
            int Size { get; set; }
            
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
            /// Clears node on dispose.
            /// </summary>
            /// <param name="allocator"></param>
            void Dispose(IDataAllocatorSetup<T> allocator);

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
            IStoreNode GetStorageNode(int index);
       
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
        
        public interface IStoreNode: INode
        {
            T[] GetStorage();
            
            int MaxCapacity { get; }

            void AddItem(T item, IDataAllocatorSetup<T> allocator);
            
            void RemoveItemAt(int index, IDataAllocatorSetup<T> arrayAllocator);
            
            void InsertItem(int index, T item, IDataAllocatorSetup<T> allocator);
            
            int RemoveAll(T item, IComparer<T> comparer, IDataAllocatorSetup<T> allocator);
           
            int RemoveAll(Func<T, bool> match, IDataAllocatorSetup<T> allocator);
            
            int FindIndex(Predicate<T> match, int start);
            
            void OnStorageDone(IDataAllocatorSetup<T> allocator = null);
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

            public ref T GetItemByRef(int index)
            {
                var current = index >> m_stepBase;
                var next = index - (current << m_stepBase);

                if (current < 0 || current > m_nodes.m_size)
                {
                    throw new IndexOutOfRangeException(
                        $"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_nodes.m_size}.");
                }

                return ref m_nodes.m_items[current].GetItemByRef(next);
            }

            public T this[int index]
            {
                get
                {
                    var current = index >> m_stepBase;
                    var next = index - (current << m_stepBase);

                    if (current < 0 || current > m_nodes.m_size)
                    {
                        throw new IndexOutOfRangeException(
                            $"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_nodes.m_size}.");
                    }

                    return m_nodes.m_items[current][next];
                }
                set
                {
                    var current = index >> m_stepBase;
                    var next = index - (current << m_stepBase);

                    if (current < 0 || current > m_nodes.m_size)
                    {
                        throw new IndexOutOfRangeException(
                            $"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_nodes.m_size}.");
                    }

                    m_nodes.m_items[current][next] = value;
                }
            }

            public IStoreNode GetStorageNode(int index)
            {
                var current = index >> m_stepBase;
                var next = index - (current << m_stepBase);

                if (current < 0 || current > m_nodes.m_size)
                {
                    throw new IndexOutOfRangeException(
                        $"The index value ${index} given is out of range. Nodes index ${current}, nodes size is {m_nodes.m_size}.");
                }

                return m_nodes.m_items[current].GetStorageNode(next);
            }

            public T[] Storage => null;

            public bool HasStorage => false;

            public int Size
            {
                get
                {
                    return m_nodes.m_size;
                }
                set
                {
                    if (value > m_leafCapacity)
                    {
                        throw new InvalidOperationException("Cannot set size greater than capacity.");
                    }
                    
                    m_nodes.m_size = value;
                }
            }

            public LinkNode(ushort level, int leafCapacity, INode child1, IDataAllocatorSetup<T> nodesAllocator,
                INode child2 = null)
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

                m_nodes = new PoolListBase<INode>(nodesAllocator, c_linkNodeCapacity,
                    capacity: linkNode.m_nodes.m_size);

                foreach (var node in linkNode.m_nodes)
                {
                    if (node is LinkNode ln)
                    {
                        m_nodes.Add(new LinkNode(ln, allocator), nodesAllocator);
                    }
                    else if (node is StoreNode sn)
                    {
                        m_nodes.Add(new StoreNode(sn, allocator), nodesAllocator);
                    }
                }
            }

            public bool Add(ref T item, out INode node, int capacity, IDataAllocatorSetup<T> allocator)
            {
                if (m_nodes.GetItemByRef(m_nodes.m_size - 1).Add(ref item, out var node1, capacity, allocator) == false)
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

                var pushed = TryInsertAndPushCore(current, next, ref item, out lastItem, allocator, m_nodes);

                return pushed;
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

                return RemoveAtAndPopCore(ref newLastValue, allocator, ref last, current, next, m_nodes);
            }

            public int IndexOf(ref T item, int startIndex)
            {
                var current = startIndex >> m_stepBase;
                var next = startIndex - (current << m_stepBase);

                if (current < 0 || current > m_nodes.m_size)
                {
                    return -1;
                }

                return IndexOfCore(ref item, current, next, m_nodes, m_stepBase);
            }


            public IEnumerable<INode> Nodes => m_nodes;

            public bool AddArray(T[] array, int size, out INode node, IDataAllocatorSetup<T> allocator)
            {
                if (m_nodes.GetItemByRef(m_nodes.m_size - 1).AddArray(array, size, out var node1, allocator) == false)
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

            public bool Ensure(ref int size, 
                ref T defaultValue, 
                out INode node,
                IDataAllocatorSetup<T> allocator)
            {
                if (m_nodes.GetItemByRef(m_nodes.m_size - 1).Ensure(ref size, ref defaultValue, out var node1, allocator) == false)
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

            public void Dispose(IDataAllocatorSetup<T> allocator)
            {
                Clear(allocator);
            }

            public bool RemoveLast(IDataAllocatorSetup<T> allocator)
            {
                var node = m_nodes.GetItemByRef(m_nodes.m_size - 1);

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
                        var storage = storeNode.m_items;

                        for (int i = 0; i < storeNode.m_size && i < storage.Length; i++)
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

            internal static bool TryInsertAndPushCore(int current,
                int next, 
                ref T item,
                out T lastItem,
                IDataAllocatorSetup<T> allocator, 
                PoolListBase<INode> nodesStorage)
            {
                lastItem = default;

                bool pushed = false;

                for (int i = current; i < nodesStorage.m_items.Length && i < nodesStorage.m_size; i++)
                {
                    var node = nodesStorage.m_items[i];

                    if (node.TryInsertAndPush(next, ref item, out lastItem, allocator))
                    {
                        pushed = true;
                        break;
                    }

                    item = lastItem;

                    next = 0;
                }

                return pushed;
            }

            internal static T RemoveAtAndPopCore(ref T newLastValue, 
                IDataAllocatorSetup<T> allocator, 
                ref bool last,
                int current, 
                int next, 
                PoolListBase<INode> nodesStore)
            {
                T pushBack = default;

                var nodesArrayAllocator = allocator.GetNodesArrayAllocator();

                for (int i = nodesStore.m_size - 1; i >= current; i--)
                {
                    var node = nodesStore.m_items[i];

                    if (i != current)
                    {
                        pushBack = node.RemoveAtAndPop(0, ref newLastValue, allocator, ref last);

                        if (node.Size == 0)
                        {
                            nodesStore.RemoveAt(i, nodesArrayAllocator);
                        }
                    }
                    else
                    {
                        bool _ = true;

                        pushBack = node.RemoveAtAndPop(next, ref newLastValue, allocator, ref _);

                        if (node.Size == 0)
                        {
                            nodesStore.RemoveAt(i, nodesArrayAllocator);
                        }
                    }

                    newLastValue = pushBack;
                }

                return pushBack;
            }

            internal static int IndexOfCore(ref T item, int current, int next, PoolListBase<INode> nodeStorage, ushort stepBase)
            {
                for (int i = current; i < nodeStorage.m_size; i++)
                {
                    var node = nodeStorage.GetItemByRef(i);

                    var subIndex = node.IndexOf(ref item, next);

                    if (subIndex >= 0)
                    {
                        return subIndex + (i << stepBase);
                    }

                    next = 0;
                }

                return -1;
            }
        }


        /// <summary>
        /// Tree data storage class.
        /// </summary>
        [DebuggerDisplay("Store. Size: {StoreNode.m_size}")]
        protected sealed class StoreNode : PoolListBase<T>, IStoreNode
        {
            public static readonly T Default = default(T);
            internal static volatile bool s_clearArrayOnReturn = KonsarpooAllocatorGlobalSetup.ClearArrayOnReturn;
           
            public int Level => 0;

            T[] INode.Storage => m_items;

            public T[] Storage => m_items;

            public void OnStorageDone(IDataAllocatorSetup<T> allocator = null)
            {
            }

            public bool HasStorage => true;

            public int Size
            {
                get
                {
                    return m_size;
                }
                set
                {
                    if (value > m_maxCapacity)
                    {
                        throw new InvalidOperationException("Cannot set size greater than capacity.");
                    }

                    m_size = value;
                }
            }

            public bool AddArray(T[] array, int size, out INode node, IDataAllocatorSetup<T> allocator)
            {
                node = new StoreNode(array, size);
                return false;
            }

            T[] IStoreNode.GetStorage()
            {
                return Storage;
            }

            public StoreNode(IDataAllocatorSetup<T> allocator, int maxCapacity, int capacity) : base(allocator.GetDataArrayAllocator(), maxCapacity, capacity)
            {
            }
            
            public StoreNode(T[] items, int size) : base(items.Length, items)
            {
                m_size = size;
            }

            public StoreNode(StoreNode poolList, IDataAllocatorSetup<T> allocator) : base(poolList, allocator.GetDataArrayAllocator())
            {
            }

            public StoreNode(IDataAllocatorSetup<T> allocator, int maxCapacity, int capacity, T item) : base(allocator.GetDataArrayAllocator(), maxCapacity, capacity)
            {
                AddItem(item, allocator);
            }
            
            public void AddItem(T item, IDataAllocatorSetup<T> allocator)
            {
                AddItemCore(item, ref m_items, ref m_size, m_maxCapacity, allocator.GetDataArrayAllocator());
            }

            public bool Add(ref T item, out INode node, int capacity, IDataAllocatorSetup<T> allocator)
            {
                if (m_size == m_maxCapacity)
                {
                    node = new StoreNode(allocator, m_maxCapacity, capacity, item);
                    return false;
                }

                AddItem(item, allocator.GetDataArrayAllocator());
                node = this;
                return true;
            }

            public void RemoveItemAt(int index, IDataAllocatorSetup<T> arrayAllocator)
            {
                RemoveAt(index, arrayAllocator.GetDataArrayAllocator());
            }

            public int RemoveAll(T item, IComparer<T> comparer, IDataAllocatorSetup<T> allocator)
            {
                return base.RemoveAll(item, comparer, allocator.GetDataArrayAllocator());
            }

            public void Clear(IDataAllocatorSetup<T> allocator)
            {
                ReturnArray(allocator.GetDataArrayAllocator(), ref m_items);

                m_size = 0;
            }

            public void Dispose(IDataAllocatorSetup<T> allocator)
            {
                Clear(allocator);
            }

            public bool RemoveLast(IDataAllocatorSetup<T> allocator)
            {
                var removeLast = RemoveLast(allocator.GetDataArrayAllocator());

                return removeLast;
            }

            public void InsertItem(int index, T item, IDataAllocatorSetup<T> allocator)
            {
                base.Insert(index, item, allocator.GetDataArrayAllocator());
            }

            public int MaxCapacity => m_maxCapacity;
            
            public int RemoveAll(Func<T, bool> match, IDataAllocatorSetup<T> allocator)
            {
                return RemoveAll(match, allocator.GetDataArrayAllocator());
            }

            public bool TryInsertAndPush(int index, ref T item, out T lastItem, IDataAllocatorSetup<T> allocator)
            {
                var storage = Storage!;

                var pushed = TryInsertAndPushCore(index, item, out lastItem, allocator, storage, this);

                return pushed;
            }

            public T RemoveAtAndPop(int index, ref T newLastValue, IDataAllocatorSetup<T> allocator, ref bool last)
            {
                var pushBackValue = RemoveAtAndPopCore(index, newLastValue, allocator, ref last, this);

                return pushBackValue;
            }

            public int IndexOf(ref T item, int startIndex)
            {
                return Array.IndexOf(m_items, item, startIndex, m_size - startIndex);
            }

            public IEnumerable<INode> Nodes => Array.Empty<INode>();

            public IStoreNode GetStorageNode(int index)
            {
                return this;
            }

            public bool Ensure(ref int extraSize, ref T defaultValue, out INode node, IDataAllocatorSetup<T> allocator)
            {
                var ensureCore = EnsureCore(this, ref extraSize, defaultValue, out node, allocator, this);

                return ensureCore;
            }
        
            internal static T RemoveAtAndPopCore(int index, T newLastValue, IDataAllocatorSetup<T> allocator, ref bool last, PoolListBase<T> arrayStore)
            {
                var storage = arrayStore.m_items;
                
                var pushBackValue = storage[index];

                var dataArrayAllocator = allocator.GetDataArrayAllocator();
                
                arrayStore.RemoveAt(index, dataArrayAllocator);

                if (last == false)
                {
                    last = true;
                }
                else
                {
                    arrayStore.AddItem(newLastValue, dataArrayAllocator);
                }

                return pushBackValue;
            }
            
            internal static bool EnsureCore(INode thisNode, ref int extraSize, T defaultValue, out INode node, IDataAllocatorSetup<T> allocator, PoolListBase<T> arrayStore)
            {
                var restOfThis = arrayStore.m_maxCapacity - arrayStore.m_size;

                var arrayAllocator = allocator.GetDataArrayAllocator();
                
                //can extend this
                if (extraSize <= restOfThis)
                {
                    Ensure(defaultValue, (arrayStore.m_size + extraSize), allocator, arrayStore);

                    arrayStore.m_size += extraSize;

                    extraSize = 0;

                    node = thisNode;

                    return true;
                }

                if (restOfThis > 0)
                {
                    Ensure(defaultValue, (arrayStore.m_size + restOfThis), allocator, arrayStore);

                    arrayStore.m_size += restOfThis;
                    
                    extraSize -= restOfThis;
                    
                    node = thisNode;

                    return true;
                }
                
                if(extraSize > 0)
                    //allocate new with rest
                {
                    var arraySize = Math.Min(arrayStore.m_maxCapacity, extraSize);

                    extraSize -= arraySize;

                    var storeNode = new StoreNode(allocator, arrayStore.m_maxCapacity, arraySize);

                    storeNode.m_size = arraySize;

                    if (arrayAllocator.CleanArrayReturn == false || EqualityComparer<T>.Default.Equals(defaultValue, Default) == false)
                    {
                        Array.Fill(storeNode.Storage, defaultValue, 0, arraySize);
                    }

                    node = storeNode;
                    
                    return false;
                }
                
                node = thisNode;

                return true;
            }
            
            private static void Ensure(T defaultValue, int newSize, IDataAllocatorSetup<T> allocator, PoolListBase<T> arrayStore)
            {
                var storage = arrayStore.m_items;

                var arrayAllocator = allocator.GetDataArrayAllocator();

                T[] vals = arrayAllocator.Rent(newSize);

                Array.Copy(storage, 0, vals, 0, arrayStore.m_size);

                arrayAllocator.Return(storage, clearArray: s_clearArrayOnReturn);

                storage = vals;

                if (arrayAllocator.CleanArrayReturn == false || EqualityComparer<T>.Default.Equals(defaultValue, Default) == false)
                {
                    Array.Fill(storage, defaultValue, arrayStore.m_size, newSize - arrayStore.m_size);
                }

                arrayStore.m_items = storage;
            }
            
            internal static bool TryInsertAndPushCore(int index, T item, out T lastItem, IDataAllocatorSetup<T> allocator, T[] storage, PoolListBase<T> arrayStore)
            {
                bool pushed = false;

                if (arrayStore.m_size < arrayStore.m_maxCapacity)
                {
                    if (arrayStore.m_size == storage.Length)
                    {
                        var newCapacity = Math.Min(Math.Max(storage.Length * 2, 2), arrayStore.m_maxCapacity);

                        var arrayAllocator = allocator.GetDataArrayAllocator();
                        
                        T[] vals = arrayAllocator.Rent(newCapacity);

                        if (arrayStore.m_size > 0)
                        {
                            Array.Copy(storage, 0, vals, 0, arrayStore.m_size);
                        }

                        arrayAllocator.Return(storage, clearArray: s_clearArrayOnReturn);

                        storage = vals;
                        arrayStore.m_items = vals;
                    }

                    if (index < arrayStore.m_size)
                    {
                        Array.Copy(storage, index, storage, index + 1, arrayStore.m_size - index);
                    }

                    storage[index] = item;
                    arrayStore.m_size += 1;

                    lastItem = default;

                    pushed = true;
                }
                else
                {
                    lastItem = storage[arrayStore.m_size - 1];

                    Array.Copy(storage, index, storage, index + 1, arrayStore.m_size - index - 1);

                    storage[index] = item;

                    pushed = false;
                }

                return pushed;
            }
        }
    }
}
