using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;


namespace Konsarpoo.Collections
{
    /// <summary>
    /// The universal random access data container. Supports List, Array, Stack and Queue API's. Data&lt;T&gt; has the same performance as the builtin List on defaults, because of hot path\shortcut optimizations made.
    /// Implemented as a tree of .NET sub arrays. Array allocator and max size of array per node may be set up globally or globally for T.
    /// The System.Buffers.ArrayPool&lt;T&gt;.Shared instance is the default allocator. 
    /// <remarks>
    /// Link node capacity is set to 1024 to convert outer index to internal subtree index using the circular shift in one cheap operation:
    /// <code>
    /// //Link Node constructor:
    /// this.m_stepBase = Math.Log(Math.Pow(1024, m_level - 1) * m_leafCapacity, 2);
    /// ...
    /// T ref this[int index] => get
    /// {
    ///     var current = index &gt;&gt; this.m_stepBase;
    ///     var next = index - (current &lt;&lt; this.m_stepBase);
    ///     return ref this.m_nodes.m_items[current][next];
    /// }
    /// </code>  
    /// </remarks>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count {m_count}")]
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [Serializable]
    public partial class Data<T> : IList<T>, IReadOnlyList<T>, ICollection, IList, IAppender<T>, IDisposable, IStack<T>, ISerializable, IDeserializationCallback
    {
        /// <summary>
        /// Default T value cached.
        /// </summary>
        public static readonly T Default = default;
        
        private static volatile bool s_clearArrayOnReturn = KonsarpooAllocatorGlobalSetup.ClearArrayOnReturn;
        private static Action<IReadOnlyList<T>> s_disposingHandler;

        [NonSerialized] private readonly IDataAllocatorSetup<T> m_allocator;
        
        private int m_maxSizeOfArray;

        internal int MaxSizeOfArray => m_maxSizeOfArray;
        
        internal int GetStoreNodesCount()
        {
            if (m_root == null)
            {
                return 0;
            }
            
            if (m_root.HasStorage)
            {
                return 1;
            }
            
            return GetStoreNodes(m_root).Count();
        }

        /// <summary>
        /// Tree root.
        /// </summary>
        [NonSerialized]
        protected internal INode m_root;
        
        /// <summary>
        /// Count of items stored.
        /// </summary>
        protected internal int m_count;
        
        /// <summary>
        /// Current version of container.
        /// </summary>
        protected internal ushort m_version;

        
        /// <summary>
        /// Current version of container.
        /// </summary>
        public int Version => m_version;
      
        /// <summary>
        /// Sets recycling behaviour if set to true array is subject to clear before returning back to pool.
        /// </summary>
        public static void SetClearArrayOnReturn(bool? val)
        {
            s_clearArrayOnReturn = val ?? KonsarpooAllocatorGlobalSetup.ClearArrayOnRequest;
            PoolList<T>.s_clearArrayOnReturn = s_clearArrayOnReturn;
        }
        
        /// <summary>
        /// Sets disposing handler.
        /// </summary>
        public static void SetDisposingHandler(Action<IReadOnlyList<T>> disposingHandler)
        {
            s_disposingHandler = disposingHandler;
        }

        /// <summary> Default Data&lt;T&gt; constructor.</summary>
        public Data()
        {
            var dataStorageAllocator = KonsarpooAllocatorGlobalSetup.DefaultAllocatorSetup.GetDataStorageAllocator<T>();

            m_allocator = dataStorageAllocator;

            var maxSizeOfArray = dataStorageAllocator.MaxSizeOfArray ?? KonsarpooAllocatorGlobalSetup.MaxSizeOfArray;

            var alignedSize = (maxSizeOfArray == 0
                ? 16
                : Math.Max(16, 1 << (int)Math.Round(Math.Log(maxSizeOfArray, 2))));

            m_maxSizeOfArray = alignedSize;
        }

        /// <summary> Data&lt;T&gt; constructor accepting array storage and total items count.</summary>
        public Data(IEnumerable<T[]> arrays, int totalCount)
        {
            var length = arrays.First().Length;

            if (length > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException($"Arrays length is too large. Must be less than {ushort.MaxValue}.");
            }
            
            var dataStorageAllocator = new DefaultGcAllocatorSetup((ushort)length);

            var dataAllocatorSetup = dataStorageAllocator.GetDataStorageAllocator<T>();

            m_allocator = dataAllocatorSetup;

            CreateFromArrays(arrays, totalCount);
        }
        
        /// <summary>
        /// Data class constructor that allows setup default capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public Data(int capacity) : this(capacity, null, null)
        {
        }

        /// <summary>
        /// Data class constructor that allows setup default capacity, instance max size of sub array node and pool instances.
        /// </summary>
        /// <param name="capacity">Default capacity.</param>
        /// <param name="maxSizeOfArray">Max size of sub array node</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Data(int capacity, int? maxSizeOfArray) : this(capacity, maxSizeOfArray, null)
        {
        }

        /// <summary>
        /// Data class constructor that allows setup pool instances.
        /// </summary>
        /// <param name="allocatorSetup">Pool setup.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Data(IDataAllocatorSetup<T> allocatorSetup) : this(0, null, allocatorSetup)
        {
        }

        /// <summary>
        /// Data class constructor that allows setup default capacity, instance max size of sub array node and pool instances.
        /// </summary>
        /// <param name="capacity">Default capacity.</param>
        /// <param name="maxSizeOfArray">Max size of sub array node</param>
        /// <param name="allocatorSetup">Pool setup.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Data(int capacity, int? maxSizeOfArray, [CanBeNull] IDataAllocatorSetup<T> allocatorSetup)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            var dataAllocator = allocatorSetup ?? KonsarpooAllocatorGlobalSetup.DefaultAllocatorSetup.GetDataStorageAllocator<T>();

            var defaultArraySize = dataAllocator.MaxSizeOfArray ?? KonsarpooAllocatorGlobalSetup.MaxSizeOfArray;

            m_allocator = dataAllocator;

            var size = maxSizeOfArray ?? defaultArraySize;

            var alignedSize = (size == 0
                ? 16
                : Math.Max(16, 1 << (int)Math.Round(Math.Log(size, 2))));

            m_maxSizeOfArray = alignedSize;

            if (capacity > 0)
            {
                var ms = m_maxSizeOfArray;
                
                var initialCapacity = (Math.Max(ushort.MaxValue + 1, 1 << (int)Math.Log(capacity > ms ? ms : capacity, 2.0)));

                var storeNode = new StoreNode(m_allocator.GetDataArrayAllocator(), m_maxSizeOfArray, initialCapacity);
                
                m_root = storeNode;
                m_tailStoreNode = storeNode;
            }
        }

        /// <summary>
        /// Data constructor accepting an enumerable. Tries to cast it to a materialized collection to reserve appreciate capacity first.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Data([NotNull] IEnumerable<T> enumerable) : this(enumerable, null)
        {
        }

        /// <summary>
        /// Data constructor accepting an enumerable. Tries to cast it to a materialized collection to reserve appreciate capacity first.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="allocatorSetup"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Data([NotNull] IEnumerable<T> enumerable, IDataAllocatorSetup<T> allocatorSetup) : this(0, 0, allocatorSetup)
        {
            switch (enumerable)
            {
                case null:
                    throw new ArgumentNullException(nameof(enumerable));
                
                case Data<T> list:
                    m_maxSizeOfArray = list.m_maxSizeOfArray;
                    m_allocator = list.m_allocator;
                    CreateFromList(list);
                    break;
                
                default:
                    AddRange(enumerable);
                    break;
            }
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="source"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Data([NotNull] Data<T> source) 
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            m_maxSizeOfArray = source.m_maxSizeOfArray;
            m_allocator = source.m_allocator;
            
            CreateFromList(source);
        }

        /// <summary>
        /// Destructor called by GC. Shouldn't be called if instance is properly disposed beforehand.
        /// </summary>
        ~Data()
        {
            s_disposingHandler?.Invoke(this);
            
            Clear();
        }

        /// <summary>
        /// Clears container and returns all node arrays back to array allocator. Suppresses instance finalization. Do not use this object after dispose.
        /// </summary>
        public void Dispose()
        {
            s_disposingHandler?.Invoke(this);

            Clear();

            m_version = ushort.MaxValue;
            
            GC.SuppressFinalize(this);
        }
      
        /// <summary>
        /// Returns the internal tree root. 
        /// </summary>
        [CanBeNull]
        public INode GetRoot()
        {
            return m_root;
        }
       
        void ICollection.CopyTo(Array array, int index)
        {
            CopyTo((T[])array, index);
        }

        /// <summary>
        /// Returns items count in container.
        /// </summary>
        public int Count => m_count;
        
        /// <summary>
        /// Array API. Returns items count in container.
        /// </summary>
        public int Length => m_count;
        
        /// <summary>
        /// Gets an object that can be used to synchronize access to the Data class instance.
        /// </summary>
        public object SyncRoot => this;

        bool ICollection.IsSynchronized => false;

        bool IList.IsReadOnly => false;
        
        bool ICollection<T>.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        T IList<T>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }

        T IReadOnlyList<T>.this[int index] => this[index];

        /// <summary>
        /// Gets or sets value by zero based index.
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public T this[int index]
        {
            get => ValueByRef(index);
            set => ValueByRef(index) = value;
        }

        /// <summary>
        /// Gets value by zero based index if the given index is out of range it returns a given default value.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="defaultVal"></param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public T GetOrDefault(int index, T defaultVal = default)
        {
            if (index >= m_count)
            {
                return defaultVal;
            }

            return ValueByRef(index);
        }

        /// <summary>
        /// Places the given value at the specified index. If the given index is out of range it resizes the Data collection to fit it.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void PlaceAt(int index, T value)
        {
            if (index >= m_count)
            {
                Ensure(index + 1);
            }

            ValueByRef(index) = value;
        }

        /// <summary>
        /// Array API to access element by its reference. 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ValueByRef(int index)
        {
            var root = m_root;
            
            if (root?.HasStorage ?? false)
            {
                if (index >= root.Size)
                {
                    throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({root.Size}).");
                }

                return ref root.Storage[index];
            }

            return ref ValueByRefSlow(index);
        }
        
        /// <summary>
        /// Gets access to storage node by item index. 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        [CanBeNull]
        public INode GetStorageNode(int index)
        {
            if (index < 0 || index >= m_count)
            {
                throw new IndexOutOfRangeException($"The index value ${index} given is out of range. Data size is {m_count}.");
            }

            if (m_root?.HasStorage ?? false)
            {
                return m_root;
            }

            return m_root?.GetStorageNode(index);
        }

        private ref T ValueByRefSlow(int index)
        {
            if (m_root == null)
            {
                throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_count}).");
            }

            return ref m_root[index];
        }

        /// <summary>
        /// List API. Reverses the order of the elements in the entire Data&lt;T&gt;.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reverse()
        {
            //common case

            if (m_root?.HasStorage ?? false)
            {
                Array.Reverse(m_root?.Storage, 0, m_count);

                return;
            }

            ReverseSlow();
        }

        private void ReverseSlow()
        {
            for(int i = 0; i < m_count / 2; i++)
            {
                T temp = ValueByRef(i);
                ValueByRef(i) = ValueByRef(m_count - i - 1);
                ValueByRef(m_count - i - 1) = temp;
            }
        }

        /// <summary>
        /// Array and List API. Determines the index of a specific item in the Data&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item, int startIndex = 0)
        {
            if (startIndex < 0 || startIndex >= m_count)
            {
                return -1;
            }

            //common case

            if (m_root?.HasStorage ?? false)
            {
                return Array.IndexOf(m_root?.Storage, item, startIndex, m_count - startIndex);
            }

            return IndexOfSlow(item, startIndex);
        }

        /// <summary>
        /// Array and List API. Determines the index of a specific item in the Data&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IList<T>.IndexOf(T item)
        {
            //common case

            if (m_root?.HasStorage ?? false)
            {
                return Array.IndexOf(m_root.Storage!, item, 0, m_count);
            }
            
            return IndexOfSlow(item, 0);
        }
        
        private int IndexOfSlow(T item, int startIndex)
        {
            return m_root?.IndexOf(ref item, startIndex) ?? -1;
        }

        /// <summary>
        /// List API. Inserts an item to the Data&lt;T&gt; at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            if (index > m_count)
            {
                throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_count}).");
            }
            
            if (m_root is StoreNode node)
            {
                //inlined method StoreNode.Insert 
                if (node.m_size < node.m_maxCapacity - 1)
                {
                    if (node.m_size == node.m_items.Length)
                    {
                        var newCapacity = Math.Min(Math.Max(node.m_items.Length * 2, 2), node.m_maxCapacity);

                        var dataArrayAllocator = m_allocator.GetDataArrayAllocator();
                        
                        T[] vals =  dataArrayAllocator.Rent(newCapacity);

                        if (node.m_size > 0)
                        {
                            Array.Copy(node.m_items, 0, vals, 0, node.m_size);
                        }

                        dataArrayAllocator.Return(node.m_items, clearArray: s_clearArrayOnReturn);

                        node.m_items = vals;
                    }

                    if (index < node.m_size)
                    {
                        Array.Copy(node.m_items, index, node.m_items, index + 1, node.m_size - index);
                    }

                    node.m_items[index] = item;
                    node.m_size += 1;

                    m_count++;
                    unchecked { ++m_version; }
                    return;
                }
            }

            InsertSlow(index, item);
        }

        private void InsertSlow(int index, T item)
        {
            if (index == m_count)
            {
                Add(item);
                return;
            }

            if (m_root == null)
            {
                Add(item);
            }
            else
            {
                if (m_root.TryInsertAndPush(index, ref item, out var lastItem, m_allocator) == false)
                {
                    AddSlow(ref lastItem);
                }
                else
                {
                    m_count++;
                    unchecked { ++m_version; }
                }
            }
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the Data&lt;T&gt;.
        /// </summary>
        /// <param name="items"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddRange([NotNull] IEnumerable<T> items)
        {
            switch (items)
            {
                case null:
                {
                    throw new ArgumentNullException(nameof(items));
                }
                case Data<T> list:
                {
                    var count = Count;

                    var newCount = count + list.Count;
                
                    Ensure(newCount);

                    if ((list.m_root?.HasStorage ?? false) && (m_root?.HasStorage ?? false))
                    {
                        Array.Copy(list.m_root.Storage, 0, m_root.Storage, count, list.Count);
                    }
                    else
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            ValueByRef(count + i) = list.ValueByRef(i);
                        }
                    }

                    break;
                }
                case T[] arr:
                {
                    var count = Count;
                
                    var newCount = count + arr.Length;
                
                    Ensure(newCount);

                    if (m_root?.HasStorage ?? false)
                    {
                        arr.CopyTo(m_root?.Storage, count);
                    }
                    else
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            ValueByRef(count + i) = arr[i];
                        }
                    }

                    break;
                }
                case IReadOnlyList<T> rList:
                {
                    var count = Count;
                
                    var newCount = count + rList.Count;
                
                    Ensure(newCount);

                    if (m_root?.HasStorage ?? false)
                    {
                        for (int i = 0; i < rList.Count; i++)
                        {
                            (m_root?.Storage)[count + i] = rList[i];
                        }
                    }
                    else
                    {
                        for (int i = 0; i < rList.Count; i++)
                        {
                            ValueByRef(count + i) = rList[i];
                        }
                    }

                    break;
                }
                case IReadOnlyCollection<T> rColl:
                {
                    var count = Count;
                
                    var newCount = count + rColl.Count;
                
                    Ensure(newCount);

                    using var enumerator = rColl.GetEnumerator();

                    if (m_root?.HasStorage ?? false)
                    {
                        for (int i = count; i < newCount; i++)
                        {
                            enumerator.MoveNext();
                        
                            m_root.Storage![i] = enumerator.Current;
                        }
                    }
                    else
                    {
                        for (int i = count; i < newCount; i++)
                        {
                            enumerator.MoveNext();
                    
                            ValueByRef(i) = enumerator.Current;
                        }
                    }

                    break;
                }
                default:
                {
                    foreach (T obj in items)
                    {
                        Add(obj);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Python List API. Adds new item to the end of the Data&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(T item)
        {
            Add(item);
        }

        private StoreNode m_tailStoreNode;
        
        /// <summary>
        /// List API. Adds an object to the end of the Data&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            var storeNode = m_tailStoreNode;
            
            if (storeNode != null)
            {
                T[] array = storeNode.Storage;
                int size = storeNode.Size;
                
                if (size < array.Length)
                {
                    array[size] = item;
                    storeNode.m_size = size + 1;

                    unchecked { ++m_version; }
                    m_count++;
                    
                    return;
                }
                
                if (size < storeNode.m_maxCapacity)
                {
                    var newCapacity = Math.Min(Math.Max(storeNode.m_items.Length * 2, 2), storeNode.m_maxCapacity);

                    var arrayAllocator = m_allocator.GetDataArrayAllocator();
                    
                    T[] vals = arrayAllocator.Rent(newCapacity);

                    if (size > 0)
                    {
                        Array.Copy(storeNode.m_items, 0, vals, 0, size);
                    }

                    arrayAllocator.Return(storeNode.m_items, clearArray: s_clearArrayOnReturn);

                    storeNode.m_items = vals;
                    
                    storeNode.m_items[size] = item;
                    storeNode.m_size = size + 1;
                
                    unchecked { ++m_version; }
                    ++m_count;
                    
                    return;
                }
            }
            
            AddSlow(ref item);
        }
       

        private void AddSlow(ref T item)
        {
            var maxSizeOfArray = m_maxSizeOfArray;
            
            var root = m_root;
            
            if (root == null)
            {
                var storeNode = new StoreNode(m_allocator.GetDataArrayAllocator(), maxSizeOfArray, 2);

                storeNode.m_items[0] = item;
                storeNode.m_size = 1;

                m_root = storeNode;
                m_tailStoreNode = storeNode;

                unchecked { ++m_version; }
                ++m_count;
                
                return;
            }

            var add = root.Add(ref item, out var newNode, 16, m_allocator);
            
            unchecked { ++m_version; }
            ++m_count;
            
            if (add == false)
            {
                m_root = new LinkNode((ushort)(root.Level + 1), maxSizeOfArray, root, m_allocator, newNode);

                m_tailStoreNode = (StoreNode)m_root.GetStorageNode(m_count - 1);
            }
        }

        /// <summary>
        /// List API. Clears Data&lt;T&gt; and returns all arrays allocated back to array pool.
        /// </summary>
        public void Clear()
        {
            if (m_count == 0)
            {
                return;
            }

            m_root?.Clear(m_allocator);
            m_count = 0;
            
            unchecked { ++m_version; }

            m_root = null;
            m_tailStoreNode = null;
        }

        /// <summary>
        /// Determines whether an element is in the Data&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        /// <summary>
        /// Copies the Data&lt;T&gt; or a portion of it to an array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(0, array, arrayIndex, m_count);
        }
        
        /// <summary>
        /// Copies the array or a portion of it to the Data&amp;lt;T&amp;gt.
        /// </summary>
        /// <param name="index">Source index.</param>
        /// <param name="array">Target array.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IndexOutOfRangeException">
        /// length is greater than the number of elements from sourceIndex to the end of sourceArray.
        /// -or- length is greater than the number of elements from destinationIndex to the end of destinationArray.
        /// -or- arrayIndex is greater or equal destinationArray length.
        /// -or- count is greater than collection size.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(int index, [NotNull] T[] array, int arrayIndex, int count)
        {
            if (count == 0 || m_root == null)
            {
                return;
            }
            
            if (m_count - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count),$"Cannot copy {count} elements from collection with size {m_count} starting at {index} to array with length {array.Length}.");
            }

            if (m_root.HasStorage)
            {
                Array.Copy(array, arrayIndex, m_root.Storage!, index, count);

                return;
            }

            CopyFromSlow(index, array, arrayIndex, count);
        }
        
        private void CopyFromSlow(int index, T[] array, int arrayIndex, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex),"An array index is negative.");
            }
            
            if (arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex),$"An array index '{arrayIndex}' is greater or equal than array length ({array.Length}).");
            }
               
            if (count > m_count)
            {
                throw new ArgumentOutOfRangeException(nameof(count),$"Copy count is greater than the number of elements from start to the end of collection.");
            }
            
            if (count + arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count),$"Copy count is greater than the number of elements from arrayIndex to the end of destinationArray");
            }

            var cnt = 0;

            for (int i = index; i < array.Length && cnt < count; i++)
            {
                m_root[i] = array[arrayIndex++];

                cnt++;
            }
        }

        /// <summary>
        /// Copies the Data&lt;T&gt; or a portion of it to an array.
        /// </summary>
        /// <param name="index">Source index.</param>
        /// <param name="array">Target array.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IndexOutOfRangeException">
        /// length is greater than the number of elements from sourceIndex to the end of sourceArray.
        /// -or- length is greater than the number of elements from destinationIndex to the end of destinationArray.
        /// -or- arrayIndex is greater or equal destinationArray length.
        /// -or- count is greater than collection size.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int index, [NotNull] T[] array, int arrayIndex, int count)
        {
            if (count == 0)
            {
                return;
            }
            
            if (m_count - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count),$"Cannot copy {count} elements from collection with size {m_count} starting at {index} to array with length {array.Length}.");
            }

            if (m_root == null)
            {
                return;
            }

            if (m_root.HasStorage)
            {
                Array.Copy(m_root.Storage!, index, array, arrayIndex, count);

                return;
            }

            CopyToSlow(index, array, arrayIndex, count);
        }

        private void CopyToSlow(int index, T[] array, int arrayIndex, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex),"An array index is negative.");
            }
            
            if (arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex),$"An array index '{arrayIndex}' is greater or equal than array length ({array.Length}).");
            }
               
            if (count > m_count)
            {
                throw new ArgumentOutOfRangeException(nameof(count),$"Copy count is greater than the number of elements from start to the end of collection.");
            }
            
            if (count + arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count),$"Copy count is greater than the number of elements from arrayIndex to the end of destinationArray");
            }

            var cnt = 0;

            for (int i = index; i < array.Length && cnt < count; i++)
            {
                array[arrayIndex++] = m_root[i];

                cnt++;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the Data&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if any item was removed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            var indexOf = IndexOf(item);

            if (indexOf < 0)
            {
                return false;
            }

            RemoveAt(indexOf);

            return true;
        }

        /// <summary>
        /// Removes all occurrences of a specific object from the Data&lt;T&gt; using allocation free values selection comparison using default comparer.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueSelector"></param>
        /// <typeparam name="V"></typeparam>
        /// <returns>True if any item was removed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RemoveAll<V>(V value, [NotNull] Func<T, V> valueSelector)
        {
            if (m_count == 0)
            {
                return 0;
            }
            
            if (valueSelector == null)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }
            
            return RemoveAll<V>(value, valueSelector, EqualityComparer<V>.Default);
        }

        /// <summary>
        /// Removes all occurrences of a specific object from the Data&lt;T&gt; using allocation free values selection comparison using comparer given.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueSelector"></param>
        /// <param name="equalityComparer"></param>
        /// <typeparam name="V"></typeparam>
        /// <returns>True if any item was removed.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public int RemoveAll<V>(V value, [NotNull] Func<T, V> valueSelector, IEqualityComparer<V> equalityComparer)
        {
            if (m_count == 0)
            {
                return 0;
            }
            
            if (valueSelector == null)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }
            
            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            int counter = 0;
            int matchedIndex;
            do
            {
                matchedIndex = -1;

                for (int i = 0; i < m_count; i++)
                {
                    ref var listItem = ref ValueByRef(i);

                    if (equalityComparer.Equals(valueSelector(listItem), value))
                    {
                        matchedIndex = i;
                        break;
                    }
                }

                if (matchedIndex >= 0)
                {
                    RemoveAt(matchedIndex);
                    counter++;
                }
            }
            while (m_count > 0 && matchedIndex >= 0);

            return counter;
        }

        /// <summary>
        /// Removes all occurrences of a specific object from the Data&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if any item was removed.</returns>
        public int RemoveAll(T item)
        {
            if (m_count == 0)
            {
                return 0;
            }
            
            return RemoveAll(item, Comparer<T>.Default);
        }

        /// <summary>
        /// Removes all occurrences of a specific object from the Data&lt;T&gt; using comparer given.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="comparer"></param>
        /// <returns>True if any item was removed.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RemoveAll(T item, [NotNull] IComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }
            
            if (m_count == 0)
            {
                return 0;
            }
            
            if (m_root is StoreNode storeNode)
            {
                var allocator = m_allocator.GetDataArrayAllocator();
                
                var removeAll = storeNode.RemoveAll(item, comparer, allocator);

                m_count -= removeAll;

                if (m_count <= 0)
                {
                    storeNode.Clear(allocator);

                    m_root = null;
                    m_tailStoreNode = null;
                    
                    m_count = 0;
                }
                
                unchecked { ++m_version; }
                
                return removeAll;
            }

            return RemoveAllSlow(item, comparer);
        }

        private int RemoveAllSlow(T item, IComparer<T> comparer)
        {
            int counter = 0;
            int matchedIndex;
            do
            {
                matchedIndex = -1;

                for (int i = 0; i < m_count; i++)
                {
                    ref var listItem = ref this.ValueByRef(i);

                    if (comparer.Compare(listItem, item) == 0)
                    {
                        matchedIndex = i;
                        break;
                    }
                }

                if (matchedIndex >= 0)
                {
                    RemoveAt(matchedIndex);
                    counter++;
                }
            } while (m_count > 0 && matchedIndex >= 0);

            return counter;
        }

        /// <summary>
        /// Removes all elements that match function delegated.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RemoveAll(Func<T, bool> match)
        {
            if (m_count == 0)
            {
                return 0;
            }
            
            if (m_root is StoreNode simpleNode)
            {
                var removeAll = simpleNode.RemoveAll(match, m_allocator.GetDataArrayAllocator());

                m_count -= removeAll;

                if (m_count <= 0)
                {
                    m_root = null;
                    m_tailStoreNode = null;
                    
                    m_count = 0;
                }
                
                return removeAll;
            }

            return RemoveAllSlow(match);
        }

        private StoreNode UpdateLastNode()
        {
            var tailStoreNode = m_root as StoreNode ?? m_root?.GetStorageNode(m_count - 1) as StoreNode;

            m_tailStoreNode = tailStoreNode;

            if (ReferenceEquals(m_tailStoreNode, tailStoreNode) == false)
            {
                //
            }
            
            return m_tailStoreNode;
        }

        private int RemoveAllSlow(Func<T, bool> match)
        {
            int counter = 0;
            int matchedIndex;
            do
            {
                matchedIndex = -1;

                for (int i = 0; i < m_count; i++)
                {
                    ref var item = ref this.ValueByRef(i);

                    if (match(item))
                    {
                        matchedIndex = i;
                        break;
                    }
                }

                if (matchedIndex >= 0)
                {
                    RemoveAt(matchedIndex);
                    counter++;
                }
            } while (m_count > 0 && matchedIndex >= 0);

            return counter;
        }

        /// <summary>
        /// Removes the element at the specified index of the Data&lt;T&gt;.
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
            if (index >= m_count)
            {
                throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_count}).");
            }
            
            if (m_root is StoreNode simpleNode)
            {
                simpleNode.RemoveAt(index, m_allocator.GetDataArrayAllocator());

                m_count--;
                unchecked { ++m_version; }

                if (m_count <= 0)
                {
                    m_root = null;
                    m_tailStoreNode = null;
                    m_count = 0;
                }

                return;
            }

            RemoveAtSlow(index);
        }

        private void RemoveAtSlow(int index)
        {
            if (m_root == null)
            {
                throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_count}).");
            }

            T newLastItem = default;

            bool last = false;
            
            m_root.RemoveAtAndPop(index, ref newLastItem, m_allocator, ref last);
            
            m_count--;
            unchecked { ++m_version; }

            if (m_count <= 0)
            {
                m_root = null;
                m_tailStoreNode = null;
                m_count = 0;
            }
            else
            {
                UpdateLastNode();
            }
        }

        object IList.this[int index]
        {
            get
            {
                return ValueByRef(index);
            }
            set
            {
                ValueByRef(index) = (T)value;
            }
        }

        /// <summary>
        /// Removes the last element of the Data&lt;T&gt;.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void RemoveLast()
        {
            if (m_count == 0)
            {
                throw new InvalidOperationException("Cannot remove last item because collection is empty.");
            }
            
            if (m_root == null)
            {
                return;
            }

            m_root.RemoveLast(m_allocator);

            unchecked { ++m_version; }
            
            m_count--;

            if (m_count <= 0)
            {
                m_root.Clear(m_allocator);
                m_root = null;
                m_tailStoreNode = null;
                m_count = 0;
            }
            else
            {
                UpdateLastNode();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the Data&lt;T&gt;.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public IEnumerator<T> GetEnumerator()
        {
            if (m_root == null)
            {
                yield break;
            }
            
            var version = m_version;

            if (m_root.HasStorage)
            {
                for (int i = 0; i < m_count && i < m_root.Storage!.Length; i++)
                {
                    CheckState(ref version);

                    yield return m_root.Storage[i];
                }
            }
            else
            {
                var enumerator = m_root.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    CheckState(ref version);

                    yield return enumerator.Current;
                }

                enumerator.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckState(ref ushort version)
        {
            if (version != m_version)
            {
                throw new InvalidOperationException($"Data collection was modified during enumeration.");
            }
        }

        /// <summary>
        /// Gets the flag indicating whether Data has tree storage initialized. 
        /// </summary>
        public bool HasList => m_root != null;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by a specified predicate, and returns the zero-based index of the first occurrence within the Data&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
        /// </summary>
        /// <param name="match"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindIndex([NotNull] Predicate<T> match, int start = 0)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            
            if (start >= m_count || start < 0)
            {
                return -1;
            }

            if (m_root is StoreNode simpleNode)
            {
                return simpleNode.FindIndex(match, start);
            }

            return FindIndexSlow(match, start);
        }

        private int FindIndexSlow(Predicate<T> match, int start)
        {
            for (int index = start; index < m_count; ++index)
            {
                if (match(ValueByRef(index)))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Searches for the first equality condition match index using T to V value selector function and default comparer. Returns the zero-based index of the first occurrence within the Data&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueSelector"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindIndex<V>(V value, Func<T, V> valueSelector, int start = 0, int end = int.MaxValue)
        {
            return FindIndex<V>(value, valueSelector, EqualityComparer<V>.Default, start, end);
        }

        /// <summary>
        /// Searches for the first equality condition match index using T to V value selector function and comparer given. Returns the zero-based index of the first occurrence within the Data&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueSelector"></param>
        /// <param name="equalityComparer"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public int FindIndex<V>(V value, [NotNull] Func<T, V> valueSelector, IEqualityComparer<V> equalityComparer, int start = 0, int end = int.MaxValue)
        {
            if (valueSelector == null)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }
            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            end = Math.Min(m_count, end);
            
            if (start >= m_count || start < 0)
            {
                return -1;
            }

            for (int index = start; index < end; ++index)
            {
                if (equalityComparer.Equals(valueSelector(ValueByRef(index)), value))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Searches for the last equality condition match index using T to V value selector function and comparer given. Returns the zero-based index of the last occurrence within the Data&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueSelector"></param>
        /// <param name="startingIndex"></param>
        /// <param name="endingIndex"></param>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindLastIndex<V>(V value, Func<T, V> valueSelector, int startingIndex = int.MinValue, int endingIndex = 0)
        {
            return FindLastIndex<V>(value, valueSelector, EqualityComparer<V>.Default, startingIndex, endingIndex);
        }
        
        /// <summary>
        /// Searches for the last equality condition match index using T to V value selector function and comparer given. Returns the zero-based index of the last occurrence within the Data&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueSelector"></param>
        /// <param name="equalityComparer"></param>
        /// <param name="startingIndex"></param>
        /// <param name="endingIndex"></param>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindLastIndex<V>(V value, [NotNull] Func<T, V> valueSelector, [NotNull] IEqualityComparer<V> equalityComparer, int startingIndex = int.MinValue, int endingIndex = 0)
        {
            if (valueSelector == null)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }

            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }
            
            if (startingIndex >= m_count)
            {
                return -1;
            }
            
            if (startingIndex == int.MinValue)
            {
                startingIndex = m_count - 1;
            }

            for (int index = startingIndex; index >= endingIndex; --index)
            {
                ref var valueByRef = ref ValueByRef(index);

                if (equalityComparer.Equals(valueSelector(valueByRef), value))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns current instance as storage with exposed Stack API only.
        /// </summary>
        public IStack<T> AsStack()
        {
            return this;
        }
        
        /// <summary>
        /// Returns current instance as Qu storage with exposed Queue API only.
        /// </summary>
        public IQueue<T> AsQueue()
        {
            return new Qu<T>(this);
        }
        
        private void CreateFromList(Data<T> source)
        {
            if (source.m_root != null && (source.m_root is StoreNode || source.m_root is LinkNode))
            {
                if (source.m_root is StoreNode simpleNode)
                {
                    var storeNode = new StoreNode(simpleNode, m_allocator.GetDataArrayAllocator());
                    
                    m_root = storeNode;

                    m_tailStoreNode = storeNode;
                    
                    m_count = source.m_count;
                }
                else if (source.m_root is LinkNode linkNode)
                {
                    m_root = new LinkNode(linkNode, m_allocator);
                    
                    m_count = source.m_count;
                    
                    UpdateLastNode();
                    
                }
            }
            else
            {
                AddRange(source);
            }
        }
    }
}

