using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using JetBrains.Annotations;


namespace Konsarpoo.Collections
{
    /// <summary>
    /// The universal random access data container. Supports List, Array, Stack and Queue API's. Data&lt;T&gt; has the same performance as the builtin List on defaults, because of hot path\shortcut optimizations made.
    /// Implemented as a tree of .NET sub arrays. Array allocator and max size of array per node may be set up for each instance, globally or globally for T.
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
        public static readonly T Default = default;
        public const int SmallListCount = 2;

        private static volatile int s_maxSizeOfArray = -1;
        private static volatile IArrayPool<T> s_pool = new DefaultMixedAllocator<T>();
        private static volatile IArrayPool<INode> s_nodesPool = new DefaultMixedAllocator<INode>();
        
        private readonly int m_maxSizeOfArray = s_maxSizeOfArray < 0 ? ArrayPoolGlobalSetup.MaxSizeOfArray : s_maxSizeOfArray;
        
        [NonSerialized]
        private readonly IArrayPool<T> m_pool = s_pool;
        [NonSerialized]
        private readonly IArrayPool<INode> m_nodesPool = s_nodesPool;
        
        [NonSerialized]
        protected internal INode m_root;
        
        protected internal int m_count;
        protected internal int m_version;

        [NonSerialized]
        private T m_val0;
        [NonSerialized]
        private T m_val1;

        [NonSerialized]
        private object m_syncRoot;
      
        /// <summary>
        /// Sets up global T maximum size of sub array.
        /// </summary>
        /// <param name="val">1024 * 1024 is max and 64 is min.</param>
        public static void SetMaxSizeOfArrayBucket(int val)
        {
            s_maxSizeOfArray = Math.Min(1024 * 1024, Math.Max(64, 1 << (int)Math.Round(Math.Log(val, 2))));
        }
        
        /// <summary>
        /// Sets up global T array pool.
        /// </summary>
        /// <param name="pool"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void SetArrayPool([NotNull] IArrayPool<T> pool)
        {
            s_pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }
        
        /// <summary>
        /// Sets up global T array pool for nodes.
        /// </summary>
        /// <param name="pool"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void SetNodesArrayPool([NotNull] IArrayPool<INode> pool)
        {
            s_nodesPool = pool ?? throw new ArgumentNullException(nameof(pool));
        }
        
        /// <summary> Default Data&lt;T&gt; constructor.</summary>
        public Data()
        {
        }

        /// <summary>
        /// Data class constructor that allows setup default capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public Data(int capacity) : this(capacity, s_maxSizeOfArray, (null, null))
        {
        }
      
        /// <summary>
        /// Data class constructor that allows setup default capacity, instance max size of sub array node and pool instances.
        /// </summary>
        /// <param name="capacity">Default capacity.</param>
        /// <param name="maxSizeOfArray">Max size of sub array node</param>
        /// <param name="poolSetup">Pool setup.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Data(int capacity, int maxSizeOfArray, (IArrayPool<T> dataPool, IArrayPool<INode> nodesPool) poolSetup)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            m_pool = poolSetup.dataPool ?? s_pool;
            m_nodesPool = poolSetup.nodesPool ?? s_nodesPool;

            var defaultArraySize = s_maxSizeOfArray <= 0 ? ArrayPoolGlobalSetup.MaxSizeOfArray : s_maxSizeOfArray;

            m_maxSizeOfArray = maxSizeOfArray <= 0
                ? defaultArraySize
                : Math.Min(defaultArraySize, Math.Max(4, 1 << (int)Math.Round(Math.Log(maxSizeOfArray, 2))));

            var initialCapacity = 1 << (int)Math.Log(capacity < SmallListCount ? 2.0 : (capacity > m_maxSizeOfArray ? m_maxSizeOfArray : capacity), 2.0);

            m_root = new StoreNode(m_pool, m_maxSizeOfArray, initialCapacity);
        }
      
        /// <summary>
        /// Data constructor accepting an enumerable. Tries to cast it to a materialized collection to reserve appreciate capacity first.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Data([NotNull] IEnumerable<T> enumerable)
        {
            switch (enumerable)
            {
                case null:
                    throw new ArgumentNullException(nameof(enumerable));
                
                case Data<T> list:
                    m_maxSizeOfArray = list.m_maxSizeOfArray;
                    m_pool = list.m_pool;
                    m_nodesPool = list.m_nodesPool;
                
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
            m_pool = source.m_pool;
            m_nodesPool = source.m_nodesPool;
            
            CreateFromList(source);
        }

        /// <summary>
        /// Destructor called by GC. Shouldn't be called if instance is properly disposed beforehand.
        /// </summary>
        ~Data()
        {
            Clear();
        }

        /// <summary>
        /// Clears container and returns all node arrays back to array allocator. Suppresses instance finalization.
        /// </summary>
        public void Dispose()
        {
            Clear();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns the internal tree root. 
        /// </summary>
        [CanBeNull]
        public INodeBase GetRoot()
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
        public object SyncRoot
        {
            get
            {
                if (this.m_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref this.m_syncRoot, new object(), (object)null);
                }
                return this.m_syncRoot;
            }
        }

        bool ICollection.IsSynchronized => false;

        public bool IsReadOnly => false;

        bool IList.IsFixedSize => false;

        T IList<T>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }

        T IReadOnlyList<T>.this[int index] => this[index];

        public T this[int index]
        {
            get => ValueByRef(index);
            set => ValueByRef(index) = value;
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
            if (m_root?.Storage != null)
            {
                if (index >= m_root.Size)
                {
                    throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_root.Size}).");
                }

                return ref m_root.Storage[index];
            }

            return ref ValueByRefSlow(index);
        }

        private ref T ValueByRefSlow(int index)
        {
            if (m_root == null)
            {
                switch (m_count)
                {
                    case 0:
                        throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_count}).");

                    case 1:

                        if (index == 0)
                        {
                            return ref m_val0;
                        }

                        throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_count}).");
                    case 2:
                    {
                        switch (index)
                        {
                            case 0:
                                return ref m_val0;
                            case 1:
                                return ref m_val1;
                        }

                        throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_count}).");
                    }
                }

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
            if (m_root?.Storage != null)
            {
                Array.Reverse(m_root.Storage, 0, m_count);

                return;
            }

            ReverseSlow();
        }

        private void ReverseSlow()
        {
            for (int upTo = 0; upTo < m_count; upTo++)
            {
                var downTo = m_count - upTo - 1;

                if (downTo + 1 <= upTo)
                {
                    break;
                }

                var uptoValue = ValueByRef(upTo);

                ValueByRef(upTo) = ValueByRef(downTo);

                ValueByRef(downTo) = uptoValue;
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
            if (m_root?.Storage != null)
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
        public int IndexOf(T item)
        {
            //common case
            if (m_root?.Storage != null)
            {
                return Array.IndexOf(m_root?.Storage, item, 0, m_count);
            }
            
            return IndexOfSlow(item, 0);
        }
        
        private int IndexOfSlow(T item, int startIndex)
        {
            var comparer = EqualityComparer<T>.Default;

            int num = 0;

            for (int i = startIndex; i < m_count; i++)
            {
                if (comparer.Equals(ValueByRef(i), item))
                {
                    return num;
                }

                ++num;
            }

            return -1;
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
                if (node.m_items.Length == 0)
                {
                    var count = Math.Max(4, node.m_size * 2);

                    node.m_items = m_pool.Rent(count);
                }
                else  if (node.m_size < node.m_maxCapacity - 1)
                {
                    if (node.m_size == node.m_items.Length)
                    {
                        int newCapacity = Math.Min(node.m_items.Length == 0 ? 4 : node.m_items.Length * 2, node.m_maxCapacity);

                        T[] vals = m_pool.Rent(newCapacity);

                        if (node.m_size > 0)
                        {
                            Array.Copy(node.m_items, 0, vals, 0, node.m_size);
                        }

                        m_pool.Return(node.m_items, clearArray: true);

                        node.m_items = vals;
                    }

                    if (index < node.m_size)
                    {
                        Array.Copy(node.m_items, index, node.m_items, index + 1, node.m_size - index);
                    }

                    node.m_items[index] = item;
                    node.m_size += 1;

                    m_count++;
                    m_version++;
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
                switch (m_count)
                {
                    case 0:
                        if (index == 0)
                        {
                            Add(item);
                        }

                        break;
                    case 1:
                        switch (index)
                        {
                            case 0:
                            {
                                _.Swap(ref m_val0, ref item);
                                Add(item);
                                break;
                            }
                            case 1:
                            {
                                Add(item);
                                break;
                            }
                            default:
                            {
                                throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_count}).");
                            }
                        }

                        break;
                    case 2:
                        switch (index)
                        {
                            case 0:
                                _.Swap(ref m_val0, ref item);
                                _.Swap(ref m_val1, ref item);
                                Add(item);
                                break;
                            case 1:
                                _.Swap(ref m_val1, ref item);
                                Add(item);
                                break;
                            case 2:
                                Add(item);
                                break;
                            default:
                            {
                                throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_count}).");
                            }
                        }

                        break;
                }
            }
            else
            {
                T current = item;

                for (int i = index; i < m_count; ++i)
                {
                    T toMove = ValueByRef(i);
                    ValueByRef(i) = current;
                    current = toMove;
                }

                Add(current);
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

                    if (list.m_root?.Storage != null && m_root?.Storage != null)
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

                    if (m_root?.Storage != null)
                    {
                        arr.CopyTo(m_root.Storage, count);
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

                    if (m_root?.Storage != null)
                    {
                        for (int i = 0; i < rList.Count; i++)
                        {
                            m_root.Storage[count + i] = rList[i];
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
                
                    if (m_root?.Storage != null)
                    {
                        for (int i = count; i < newCount; i++)
                        {
                            enumerator.MoveNext();
                        
                            m_root.Storage[i] = enumerator.Current;
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

        /// <summary>
        /// List API. Adds an object to the end of the Data&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (m_root is StoreNode node)
            {
                //inlined method StoreNode.Add 
                if (node.m_size < node.m_maxCapacity)
                {
                    if (node.m_items.Length == 0)
                    {
                        var items = m_pool.Rent(Math.Max(4, node.m_size * 2));

                        node.m_items = items;
                    }
                    else if (node.m_size == node.m_items.Length)
                    {
                        int newCapacity = Math.Min(node.m_items.Length == 0 ? 4 : node.m_items.Length * 2, node.m_maxCapacity);
                            
                        T[] vals = m_pool.Rent(newCapacity);

                        if (node.m_size > 0)
                        {
                            Array.Copy(node.m_items, 0, vals, 0, node.m_size);
                        }

                        m_pool.Return(node.m_items, clearArray: true);

                        node.m_items = vals;
                    }

                    node.m_items[node.m_size] = item;

                    node.m_size++;

                    ++m_version;
                    ++m_count;
                    return;
                }
            }

            AddSlow(item);
        }

        private void AddSlow(T item)
        {
            if (m_root == null)
            {
                switch (m_count)
                {
                    case 0:
                        m_val0 = item;
                        ++m_version;
                        ++m_count;
                        return;
                    case 1:
                        m_val1 = item;
                        ++m_version;
                        ++m_count;
                        return;
                    default:
                    {
                        var storeNode = new StoreNode(m_pool, m_maxSizeOfArray, SmallListCount * 2);

                        storeNode.m_items[0] = m_val0;
                        storeNode.m_items[1] = m_val1;

                        storeNode.m_items[SmallListCount] = item;

                        storeNode.m_size = SmallListCount + 1;

                        m_val0 = Default;
                        m_val1 = Default;

                        m_root = storeNode;

                        ++m_version;
                        ++m_count;
                        return;
                    }
                }
            }

            if (m_root != null)
            {
                INode node1 = m_root;
                if (!node1.Add(ref item, out var node2))
                {
                    m_root = new LinkNode(node1.Level + 1, m_maxSizeOfArray, node1, m_nodesPool, node2);
                }
            }

            ++m_version;
            ++m_count;
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

            m_root?.Clear();
            m_count = 0;
            ++m_version;

            m_root = null;

            m_val0 = Default;
            m_val1 = Default;
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

            if (m_root?.Storage != null)
            {
                Array.Copy(m_root.Storage, index, array, arrayIndex, count);

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
            
            if (m_root == null)
            {
                for (int i = index; i < array.Length && cnt < count; i++)
                {
                    array[arrayIndex++] = this[i];

                    cnt++;
                }
            }
            else
            {
                for (int i = index; i < array.Length && cnt < count; i++)
                {
                    array[arrayIndex++] = m_root[i];
                    
                    cnt++;
                }
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
                var removeAll = storeNode.RemoveAll(item, comparer);

                m_count -= removeAll;

                if (m_count <= 0)
                {
                    storeNode.Clear();

                    m_root = null;
                    
                    m_count = 0;
                }
                
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
                var removeAll = simpleNode.RemoveAll(match);

                m_count -= removeAll;

                if (m_count <= 0)
                {
                    m_root = null;
                    
                    m_count = 0;
                }
                
                return removeAll;
            }

            return RemoveAllSlow(match);
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
                simpleNode.RemoveAt(index);

                m_count--;
                ++m_version;

                if (m_count <= 0)
                {
                    m_root = null;
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
                switch (index)
                {
                    case 0:
                        m_val0 = m_val1;
                        m_val1 = Default;
                        break;
                    case 1:
                        m_val1 = Default;
                        break;

                    default:
                    {
                        throw new IndexOutOfRangeException($"Index '{index}' is greater or equal the size of collection ({m_count}).");
                    }
                }

                m_count--;
                ++m_version;
            }
            else
            {
                for (int i = index + 1; i < m_count; ++i)
                {
                    ValueByRef(i - 1) = ValueByRef(i);
                }

                RemoveLast();
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
                switch (m_count)
                {
                    case 0:
                        break;
                    case 1:
                        m_val0 = Default;
                        
                        ++m_version;
                        m_count--;
                        break;
                    default:
                        m_val1 = Default;
                        
                        ++m_version;
                        m_count--;
                        break;
                }
            }
            else
            {
                m_root.RemoveLast();

                ++m_version;
                m_count--;

                if (m_count <= 0)
                {
                    m_root.Clear();
                    m_root = null;
                    m_count = 0;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the Data&lt;T&gt;.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public IEnumerator<T> GetEnumerator()
        {
            var version = m_version;

            var storage = m_root?.Storage;
            
            if (storage is not null)
            {
                for (int i = 0; i < m_count && i < storage.Length; i++)
                {
                    CheckState(ref version);

                    yield return storage[i];
                }
            }
            else if (m_root is null)
            {
                switch (m_count)
                {
                    case 0: yield break;

                    case 1: yield return m_val0; break;
                    case 2: yield return m_val0; CheckState(ref version); yield return m_val1; break;
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
        private void CheckState(ref int version)
        {
            if (version != m_version)
            {
                throw new InvalidOperationException($"Data collection was modified during enumeration. ({m_version - version} time(s).");
            }
        }

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
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindIndex<V>(V value, Func<T, V> valueSelector, int start = 0)
        {
            return FindIndex<V>(value, valueSelector, EqualityComparer<V>.Default, start);
        }

        /// <summary>
        /// Searches for the first equality condition match index using T to V value selector function and comparer given. Returns the zero-based index of the first occurrence within the Data&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueSelector"></param>
        /// <param name="equalityComparer"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public int FindIndex<V>(V value, [NotNull] Func<T, V> valueSelector, IEqualityComparer<V> equalityComparer, int start = 0)
        {
            if (valueSelector == null)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }
            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }
            
            if (start >= m_count || start < 0)
            {
                return -1;
            }

            for (int index = start; index < m_count; ++index)
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
        /// <param name="end"></param>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindLastIndex<V>(V value, Func<T, V> valueSelector, int end = int.MinValue)
        {
            return FindLastIndex<V>(value, valueSelector, EqualityComparer<V>.Default, end);
        }
        
        /// <summary>
        /// Searches for the last equality condition match index using T to V value selector function and comparer given. Returns the zero-based index of the last occurrence within the Data&lt;T&gt; or a portion of it. This method returns -1 if an item that matches the conditions is not found.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueSelector"></param>
        /// <param name="equalityComparer"></param>
        /// <param name="end"></param>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindLastIndex<V>(V value, [NotNull] Func<T, V> valueSelector, [NotNull] IEqualityComparer<V> equalityComparer, int end = int.MinValue)
        {
            if (valueSelector == null)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }

            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }
            
            if (end >= m_count)
            {
                return -1;
            }
            
            if (end == int.MinValue)
            {
                end = m_count - 1;
            }

            for (int index = end; index >= 0; --index)
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
            if (source.m_root != null)
            {
                if (source.m_root is StoreNode simpleNode)
                {
                    m_root = new StoreNode(simpleNode);

                    m_count = source.m_count;
                }
                else if (source.m_root is LinkNode linkNode)
                {
                    m_root = new LinkNode(linkNode, m_nodesPool);

                    m_count = source.m_count;
                }
            }
            else
            {
                m_val0 = source.m_val0;
                m_val1 = source.m_val1;

                m_count = source.m_count;
            }
        }
    }
}

