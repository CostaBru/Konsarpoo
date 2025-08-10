using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections
{
    public partial class FileData<T>
    {
        internal class FileStoreNode : IStoreNode
        {
            private readonly Func<int> m_newStoreNodeId;
            private readonly int m_arrayIndex;
            private readonly IDataArrayFileAccessor m_accessor;
            internal PoolListBase<T> m_list;
            
            public int Level => 0;

            public int MaxCapacity => m_list.m_maxCapacity;

            public ref T GetItemByRef(int index)
            {
                if (index >= m_list.m_size)
                {
                    throw new IndexOutOfRangeException(
                        $"Index '{index}' is greater or equal the size of collection ({m_list.m_size}).");
                }

                OnStorageAccess();

                return ref m_list.m_items[index];
            }

            public T this[int index]
            {
                get
                {
                    if (index >= m_list.m_size)
                    {
                        throw new IndexOutOfRangeException(
                            $"Index '{index}' is greater or equal the size of collection ({m_list.m_size}).");
                    }

                    OnStorageAccess();

                    return m_list.m_items[index];
                }
                set
                {
                    if (index >= m_list.m_size)
                    {
                        throw new IndexOutOfRangeException(
                            $"Index '{index}' is greater or equal the size of collection ({m_list.m_size}).");
                    }

                    OnStorageAccess();

                    m_list.m_items[index] = value;
                    
                    OnStorageDone();
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

            public T[] RawStorage => m_list.m_items;
            
            public T[] Storage
            {
                get
                {
                    OnStorageAccess();
                    
                    return m_list.m_items;
                }
            }

            public void OnStorageAccess()
            {
                if (m_arrayIndex < m_accessor.ArrayCount)
                {
                    m_list.m_items = m_accessor.ReadArray<T>(m_arrayIndex);
                }
            }

            public void OnStorageDone(IDataAllocatorSetup<T> allocator = null)
            {
                if (m_arrayIndex < m_accessor.ArrayCount)
                {
                    m_accessor.WriteArray(m_arrayIndex, m_list.m_items);
                }
                else
                {
                    m_accessor.AppendArray(m_list.m_items);
                }

                if (allocator != null)
                {
                    allocator.GetDataArrayAllocator().Return(m_list.m_items);

                    m_list.m_items = Array.Empty<T>();
                }
            }
            

            public void OnStorageChanged(IDataAllocatorSetup<T> allocator)
            {
                OnStorageDone(allocator);
            }

            public void InsertItem(int index, T item, IDataAllocatorSetup<T> allocator)
            {
                OnStorageAccess();

                m_list.Insert(index, item, allocator.GetDataArrayAllocator());
                
                OnStorageChanged(allocator);
            }
            
            public int RemoveAll(Func<T, bool> match, IDataAllocatorSetup<T> allocator)
            {
                OnStorageAccess();

                var removeAll = m_list.RemoveAll(match, allocator.GetDataArrayAllocator());
                
                OnStorageChanged(allocator);
                
                return removeAll;
            }

            public void AddItem(T item, IDataAllocatorSetup<T> allocator)
            {
                OnStorageAccess();
                
                m_list.Add(item, allocator.GetDataArrayAllocator());
                
                OnStorageChanged(allocator);
            }

            public bool HasStorage => true;

            public int Size
            {
                get
                {
                    return m_list.m_size;
                }
                set
                {
                    if (value > m_list.m_maxCapacity)
                    {
                        throw new InvalidOperationException("Cannot set size greater than capacity.");
                    }
                    
                    m_list.m_size = value;
                }
            }

            IStoreNode INode.GetStorageNode(int index)
            {
                return this;
            }

            public bool AddArray(T[] array, int size, out INode node, IDataAllocatorSetup<T> allocator)
            {
                if (array.Length == 0)
                {
                    node = new FileStoreNode(m_newStoreNodeId, m_newStoreNodeId(), m_accessor, size, m_list.m_maxCapacity);
                }
                else
                {
                    node = new FileStoreNode(m_newStoreNodeId, m_newStoreNodeId(), m_accessor, array, size);
                }
                
                return false;
            }

            T[] IStoreNode.GetStorage()
            {
                OnStorageAccess();

                return RawStorage;
            }

            /// <summary>
            /// Empty node constuctor.
            /// </summary>
            public FileStoreNode(Func<int> newStoreNodeId, int arrayIndex, IDataArrayFileAccessor accessor, IDataAllocatorSetup<T> allocator, int maxCapacity, int capacity)
            {
                m_newStoreNodeId = newStoreNodeId;
                m_arrayIndex = arrayIndex;
                m_accessor = accessor;
                m_list = new PoolListBase<T>(allocator.GetDataArrayAllocator(), maxCapacity, capacity);
            }

            /// <summary>
            /// Ensure or copy constructor
            /// </summary>
            public FileStoreNode(Func<int> newStoreNodeId, int arrayIndex, IDataArrayFileAccessor accessor, T[] items, int size)
            {
                m_newStoreNodeId = newStoreNodeId;
                m_arrayIndex = arrayIndex;
                m_accessor = accessor;
                m_list = new PoolListBase<T>(items.Length, items);
                m_list.m_size = size;
                
                OnStorageDone();
            }
            
            /// <summary>
            /// Loading data constructor
            /// </summary>
            public FileStoreNode(Func<int> newStoreNodeId, int arrayIndex, IDataArrayFileAccessor accessor, int size, int maxCapacity)
            {
                m_newStoreNodeId = newStoreNodeId;
                m_arrayIndex = arrayIndex;
                m_accessor = accessor;
                m_list = new PoolListBase<T>(maxCapacity, Array.Empty<T>());
                m_list.m_size = size;
            }

            /// <summary>
            /// Deep copy constructor
            /// </summary>
            public FileStoreNode(Func<int> newStoreNodeId, FileStoreNode node, IArrayAllocator<T> allocator)
            {
                m_newStoreNodeId = newStoreNodeId;
                m_arrayIndex = node.m_arrayIndex;
                m_accessor = node.m_accessor;
                m_list = new PoolListBase<T>(node.m_list, allocator);
            }

            /// <summary>
            /// New store node constructor
            /// </summary>
            public FileStoreNode(Func<int> newStoreNodeId, int arrayIndex, IDataArrayFileAccessor accessor, IDataAllocatorSetup<T> allocator, int maxCapacity, int capacity, T item)
            {
                m_newStoreNodeId = newStoreNodeId;
                m_arrayIndex = arrayIndex;
                m_accessor = accessor;
                var arrayAllocator = allocator.GetDataArrayAllocator();
                m_list = new PoolListBase<T>(arrayAllocator, maxCapacity, capacity);
                m_list.AddItem(item, arrayAllocator);
                OnStorageChanged(allocator);
            }

            public bool Add(ref T item, out INode node, int capacity, IDataAllocatorSetup<T> allocator)
            {
                if (m_list.m_size == m_list.m_maxCapacity)
                {
                    node = new FileStoreNode(m_newStoreNodeId, m_newStoreNodeId(), m_accessor, allocator, m_list.m_maxCapacity, capacity, item);
                    return false;
                }

                OnStorageAccess();
               
                m_list.AddItem(item, allocator.GetDataArrayAllocator());
               
                OnStorageChanged(allocator);
                
                node = this;
                
                return true;
            }

            public void RemoveItemAt(int index, IDataAllocatorSetup<T> arrayAllocator)
            {
                OnStorageAccess();

                m_list.RemoveAt(index, arrayAllocator.GetDataArrayAllocator());

                OnStorageChanged(arrayAllocator);
            }

            public void Clear(IDataAllocatorSetup<T> allocator)
            {
                OnStorageAccess();

                m_list.Clear(allocator.GetDataArrayAllocator());

                OnStorageChanged(allocator);
            }

            public void Dispose(IDataAllocatorSetup<T> allocator)
            {
                m_list.Clear(allocator.GetDataArrayAllocator());
            }

            public bool RemoveLast(IDataAllocatorSetup<T> allocator)
            {
                OnStorageAccess();

                var removeLast = m_list.RemoveLast(allocator.GetDataArrayAllocator());

                OnStorageChanged(allocator);

                return removeLast;
            }

            public bool TryInsertAndPush(int index, ref T item, out T lastItem, IDataAllocatorSetup<T> allocator)
            {
                OnStorageAccess();

                var storage = RawStorage!;

                var pushed = StoreNode.TryInsertAndPushCore(index, item, out lastItem, allocator, storage, m_list);

                OnStorageChanged(allocator);

                return pushed;
            }

            public T RemoveAtAndPop(int index, ref T newLastValue, IDataAllocatorSetup<T> allocator, ref bool last)
            {
                OnStorageAccess();

                var pushBackValue = StoreNode.RemoveAtAndPopCore(index, newLastValue, allocator, ref last, m_list);

                OnStorageChanged(allocator);

                return pushBackValue;
            }

            public int IndexOf(ref T item, int startIndex)
            {
                return Array.IndexOf(m_list.m_items, item, startIndex, m_list.m_size - startIndex);
            }

            public IEnumerable<INode> Nodes => Array.Empty<INode>();

            public bool Ensure(ref int extraSize, ref T defaultValue, out INode node, IDataAllocatorSetup<T> allocator)
            {
                if (Size < MaxCapacity)
                {
                    OnStorageAccess();

                    var ensureCore = EnsureCore(this, ref extraSize, defaultValue, allocator, out node);

                    OnStorageChanged(allocator);

                    return ensureCore;
                }
                else
                {
                    return EnsureCore(this, ref extraSize, defaultValue, allocator, out node); 
                }
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

            public int RemoveAll(T item, IComparer<T> comparer, IDataAllocatorSetup<T> allocator)
            {
                OnStorageAccess();

                var removeAll = m_list.RemoveAll(item, comparer, allocator.GetDataArrayAllocator());

                if (removeAll > 0)
                {
                    OnStorageChanged(allocator);
                }
                else
                {
                    OnStorageDone(allocator);
                }

                return removeAll;
            }
        
            public int FindIndex(Predicate<T> match, int start)
            {
                var storage = Storage;

                var index = new DataRs<T>(storage, m_list.m_size)
                    .FindIndexPredicate(match, start);
                
                OnStorageDone();
                
                return index;
            }
            
            internal static bool EnsureCore(FileStoreNode storeNode, ref int extraSize, T defaultValue, IDataAllocatorSetup<T> arrayAllocator, out INode node)
            {
                var restOfThis = storeNode.MaxCapacity - storeNode.Size;

                //can extend this
                if (extraSize <= restOfThis)
                {
                    Ensure(defaultValue, (storeNode.Size + extraSize), arrayAllocator, storeNode);

                    storeNode.Size += extraSize;

                    extraSize = 0;

                    node = storeNode;

                    return true;
                }

                if (restOfThis > 0)
                {
                    Ensure(defaultValue, (storeNode.Size + restOfThis), arrayAllocator, storeNode);

                    storeNode.Size += restOfThis;
                    
                    extraSize -= restOfThis;
                    
                    node = storeNode;

                    return true;
                }
                
                if(extraSize > 0)
                    //allocate new with rest
                {
                    var arraySize = Math.Min(storeNode.MaxCapacity, extraSize);

                    extraSize -= arraySize;

                    var newId = storeNode.m_newStoreNodeId();

                    var dataArrayAllocator = arrayAllocator.GetDataArrayAllocator();

                    var newArray = dataArrayAllocator.Rent(arraySize);

                    var newStoreNode = new FileStoreNode(storeNode.m_newStoreNodeId, newId, storeNode.m_accessor, newArray, arraySize);

                    if (dataArrayAllocator.CleanArrayReturn == false || EqualityComparer<T>.Default.Equals(defaultValue, Default) == false)
                    {
                        Array.Fill(newStoreNode.Storage, defaultValue, 0, arraySize);
                    }
                    
                    newStoreNode.OnStorageDone(arrayAllocator);

                    node = newStoreNode;
                    
                    return false;
                }
                
                node = storeNode;

                return true;
            }
            
            private static void Ensure(T defaultValue, int newSize, IDataAllocatorSetup<T> allocator, FileStoreNode storeNode)
            {
                var storage = storeNode.RawStorage;

                var arrayAllocator = allocator.GetDataArrayAllocator();

                T[] vals = arrayAllocator.Rent(newSize);

                Array.Copy(storage, 0, vals, 0, storeNode.Size);

                arrayAllocator.Return(storage, clearArray: PoolListBase<T>.s_clearArrayOnReturn);

                storage = vals;

                if (arrayAllocator.CleanArrayReturn == false || EqualityComparer<T>.Default.Equals(defaultValue, Default) == false)
                {
                    Array.Fill(storage, defaultValue, storeNode.Size, newSize - storeNode.Size);
                }

                storeNode.m_list.m_items = storage;
            }
        }

        /// <summary>
        /// Tree Leaf/link node class
        /// </summary>
        [DebuggerDisplay("FileLinkNode. Nodes: {m_nodes.Count}, Level: {Level}")]
        protected internal class FileLinkNode : INode
        {
            private readonly Func<int> m_newStoreNodeId;
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

            public FileLinkNode(Func<int> newStoreNodeId, ushort level, int leafCapacity, INode child1, IDataAllocatorSetup<T> nodesAllocator,
                INode child2 = null)
            {
                m_newStoreNodeId = newStoreNodeId;
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

            public FileLinkNode(Func<int> newStoreNodeId, FileLinkNode linkNode, IDataAllocatorSetup<T> allocator)
            {
                m_newStoreNodeId = newStoreNodeId;
                m_level = linkNode.m_level;
                m_leafCapacity = linkNode.m_leafCapacity;
                m_stepBase = linkNode.m_stepBase;

                var nodesAllocator = allocator.GetNodesArrayAllocator();
                var dataArrayAllocator = allocator.GetDataArrayAllocator();

                m_nodes = new PoolListBase<INode>(nodesAllocator, c_linkNodeCapacity,
                    capacity: linkNode.m_nodes.m_size);

                int arrays = 0;

                foreach (var node in linkNode.m_nodes)
                {
                    if (node is FileLinkNode ln)
                    {
                        m_nodes.Add(new FileLinkNode(newStoreNodeId, ln, allocator), nodesAllocator);
                    }
                    else if (node is FileStoreNode sn)
                    {
                        m_nodes.Add(new FileStoreNode(newStoreNodeId, sn, dataArrayAllocator), nodesAllocator);

                        arrays++;
                    }
                }
            }

            public bool Add(ref T item, out INode node, int capacity, IDataAllocatorSetup<T> allocator)
            {
                if (m_nodes.GetItemByRef(m_nodes.m_size - 1).Add(ref item, out var node1, capacity, allocator) == false)
                {
                    if (m_nodes.m_size == c_linkNodeCapacity)
                    {
                        node = new FileLinkNode(m_newStoreNodeId, m_level, m_leafCapacity, node1, allocator);
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

                var pushed = LinkNode.TryInsertAndPushCore(current, next, ref item, out lastItem, allocator, m_nodes);

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

                return LinkNode.RemoveAtAndPopCore(ref newLastValue, allocator, ref last, current, next, m_nodes);
            }

            public int IndexOf(ref T item, int startIndex)
            {
                var current = startIndex >> m_stepBase;
                var next = startIndex - (current << m_stepBase);

                if (current < 0 || current > m_nodes.m_size)
                {
                    return -1;
                }

                return LinkNode.IndexOfCore(ref item, current, next, m_nodes, m_stepBase);
            }

            public IEnumerable<INode> Nodes => m_nodes;

            public virtual bool AddArray(T[] array, int size, out INode node, IDataAllocatorSetup<T> allocator)
            {
                if (m_nodes.GetItemByRef(m_nodes.m_size - 1).AddArray(array, size, out var node1, allocator) == false)
                {
                    if (m_nodes.m_size == c_linkNodeCapacity)
                    {
                        node = new FileLinkNode(m_newStoreNodeId, m_level, m_leafCapacity, node1, allocator);
                        return false;
                    }

                    m_nodes.Add(node1, allocator.GetNodesArrayAllocator());
                }

                node = this;
                return true;
            }

            public virtual bool Ensure(ref int size, ref T defaultValue, out INode node,
                IDataAllocatorSetup<T> allocator)
            {
                if (m_nodes.GetItemByRef(m_nodes.m_size - 1).Ensure(ref size, ref defaultValue, out var node1, allocator) == false)
                {
                    if (m_nodes.m_size == c_linkNodeCapacity)
                    {
                        node = new FileLinkNode(m_newStoreNodeId, m_level, m_leafCapacity, node1, allocator);
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
                m_nodes.Clear(allocator.GetNodesArrayAllocator());
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
                    if (node is IStoreNode storeNode)
                    {
                        var storage = storeNode.GetStorage();

                        for (int i = 0; i < storeNode.Size && i < storage.Length; i++)
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
    }
}
