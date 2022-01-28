using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Represents a distinct set of values. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count {m_count}")]
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [Serializable]
    public partial class Set<T> : ICollection<T>, IReadOnlyCollection<T>, IAppender<T>, ISerializable, IDeserializationCallback, IDisposable
    {
        private static readonly bool IsReferenceType = typeof(T).IsByRef;

        [NonSerialized]
        private Data<int> m_buckets;
        [NonSerialized]
        private Data<Slot> m_slots;
        [NonSerialized]
        private object m_syncRoot;
        
        private IEqualityComparer<T> m_comparer;

        private int m_lastIndex;
        private int m_freeList;
        private int m_version;
        private int m_count;

        /// <summary>Initializes a new instance of the Set class that is empty and uses the default equality comparer for the set type.</summary>
        public Set()
          : this(EqualityComparer<T>.Default)
        {
        }

        /// <summary>Initializes a new instance of the Set class that is empty and uses the default equality comparer for the set type.</summary>
        /// <param name="capacity">default capacity of internal storage.</param>
        public Set(int capacity)
          : this(capacity, EqualityComparer<T>.Default)
        {
        }
        
        /// <summary>Initializes a new instance of the Set class that is empty and uses the equality comparer given for the set type.</summary>
        /// <param name="capacity">default capacity of internal storage.</param>
        /// <param name="comparer"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Set(int capacity, IEqualityComparer<T> comparer)
            : this(comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (capacity <= 0)
            {
                return;
            }

            Initialize(capacity);
        }

        /// <summary>Initializes a new instance of the Set class that is empty and uses equality comparer given for the set type.</summary>
        /// <param name="comparer"> Equality comparer.</param>
        public Set([CanBeNull] IEqualityComparer<T> comparer)
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<T>.Default;
            }
            m_comparer = comparer;
            m_lastIndex = 0;
            m_count = 0;
            m_freeList = -1;
        }

        /// <summary>
        /// Initializes a new instance of the Set class. Uses default equality comparer for the set type and fills the set with given collection.
        /// </summary>
        /// <param name="collection"></param>
        public Set(IEnumerable<T> collection)
          : this(collection, EqualityComparer<T>.Default)
        {
        }
        
        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="collection"></param>
        public Set([NotNull] Set<T> collection)
            : this(collection, collection.m_comparer ?? EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Set class. Uses equality comparer given for the set type and fills the set with given collection.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="comparer"></param>
        public Set(IEnumerable<T> collection, IEqualityComparer<T> comparer)
          : this(comparer)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (collection is Set<T> objSet && AreEqualityComparersEqual(this, objSet))
            {
                CopyFrom(objSet);
            }
            else
            {
                Initialize(!(collection is ICollection<T> objs) ? 0 : objs.Count);

                UnionWith(collection);
            }
        }
        
        /// <summary>
        /// Destructor called by GC. Shouldn't be called if instance is properly disposed beforehand.
        /// </summary>
        ~Set()
        {
            Clear();
        }

        /// <summary>
        /// Clears container. Suppresses instance finalization.
        /// </summary>
        public void Dispose()
        {
            Clear();

            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Gets the IEqualityComparer&lt;T&gt; that is used to determine equality of values for the set.
        /// </summary>
        public IEqualityComparer<T> Comparer => m_comparer;

        /// <summary>
        /// Gets the number of values contained in the Set&lt;TKey,TValue&gt;.
        /// </summary>
        public int Count => m_count;
        
        /// <summary>
        /// Array API. Gets the number of values contained in the Set&lt;TKey,TValue&gt;.
        /// </summary>
        public int Length => m_count;
        
        
        /// <summary>
        /// Gets an object that can be used to synchronize access to the Set&lt;T&gt; class instance.
        /// </summary>
        public object SyncRoot
        {
            get
            {
                if (m_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref m_syncRoot, new object(), null);
                }
                return m_syncRoot;
            }
        }
        
        /// <summary>
        /// Python List API. Adds new item to the end of the Set&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        public void Append(T item)
        {
            Add(item);
        }

        /// <summary>Adds the specified element to a set.</summary>
        /// <param name="value">The element to add to the set.</param>
        /// <returns>
        /// <see langword="true" /> if the element is added to the <see cref="T:System.Collections.Generic.HashSet`1" /> object; <see langword="false" /> if the element is already present.</returns>
        public bool Add(T value)
        {
            if (m_buckets == null)
            {
                Initialize(0);
            }

            int hashCode = InternalGetHashCode(ref value);
            int storageIndex = hashCode % m_buckets.Count;
            int num = 0;

            var start = m_buckets.ValueByRef(hashCode % m_buckets.Count);

            var slotArr = m_slots.m_root?.Storage;

            if (slotArr != null)
            {
                for (int? i = start - 1; i >= 0; i = slotArr[i.Value].next)
                {
                    ref var s = ref slotArr[i.Value];

                    if (s.hashCode == hashCode && m_comparer.Equals(s.value, value))
                    {
                        return false;
                    }
                    ++num;
                }
            }
            else
            {
                for (int? i = start - 1; i >= 0; i = m_slots.ValueByRef(i.Value).next)
                {
                    ref var s = ref m_slots.ValueByRef(i.Value);

                    if (s.hashCode == hashCode && m_comparer.Equals(s.value, value))
                    {
                        return false;
                    }
                    ++num;
                }
            }

            int index;
            if (m_freeList >= 0)
            {
                index = m_freeList;
                m_freeList = m_slots.ValueByRef(index).next;
            }
            else
            {
                if (m_lastIndex == m_slots.Count)
                {
                    IncreaseCapacity();
                    storageIndex = hashCode % m_buckets.Count;
                }
                index = m_lastIndex;
                ++m_lastIndex;
            }

            var bucket = m_buckets.ValueByRef(storageIndex);

            ref var slot = ref m_slots.ValueByRef(index);

            slot.hashCode = hashCode;
            slot.value = value;
            slot.next = bucket - 1;

            m_buckets.ValueByRef(storageIndex) = index + 1;

            ++m_count;
            ++m_version;
          
            return true;
        }

        /// <summary>Removes all elements from a Set object.</summary>
        public void Clear()
        {
            m_slots?.Dispose();
            m_buckets?.Dispose();

            m_slots = null;
            m_buckets = null;

            m_lastIndex = 0;
            m_count = 0;
            m_freeList = -1;
            
            ++m_version;
        }

        /// <summary>
        /// Determines whether the Set&lt;T&gt; has missing the item.
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public bool IsMissing(T item)
        {
            return !Contains(item);
        }
        
        /// <summary>
        /// Determines whether the Set&lt;T&gt; contains the item.
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Contains(T item)
        {
            if (m_buckets != null)
            {
                int hashCode = IsReferenceType && item == null ? 0 : m_comparer.GetHashCode(item) & int.MaxValue;

                var start = m_buckets.ValueByRef(hashCode % m_buckets.Count);

                if (m_slots.m_root?.Storage != null)
                {
                    var items = m_slots.m_root?.Storage;

                    for (int? index = start - 1; index >= 0; index = items[index.Value].next)
                    {
                        ref var slot = ref items[index.Value];
                        
                        if (slot.hashCode == hashCode && m_comparer.Equals(slot.value, item))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    for (int? index = start - 1; index >= 0; index = m_slots.ValueByRef(index.Value).next)
                    {
                        ref var slot = ref m_slots.ValueByRef(index.Value);

                        if (slot.hashCode == hashCode && m_comparer.Equals(slot.value, item))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        
        bool ICollection<T>.IsReadOnly => false;

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        /// <summary>
        /// Copies the Set&lt;T&gt; or a portion of it to an array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex, m_count);
        }
        
        /// <summary>
        /// Removes the item from the Set&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Remove(T item)
        {
            if (m_buckets != null)
            {
                int hashCode = InternalGetHashCode(ref item);
                int index1 = hashCode % m_buckets.Count;
                int last = -1;

                var start = m_buckets.ValueByRef(index1);

                for (int index = start - 1; index >= 0; index = m_slots.ValueByRef(index).next)
                {
                    ref var currentEntry = ref m_slots.ValueByRef(index);

                    if (currentEntry.hashCode == hashCode && m_comparer.Equals(currentEntry.value, item))
                    {
                        if (last < 0)
                        {
                            m_buckets.ValueByRef(index1) = currentEntry.next  + 1;
                        }
                        else
                        {
                            m_slots.ValueByRef(last).next = currentEntry.next;
                        }

                        currentEntry.hashCode = -1;
                        currentEntry.value = default(T);
                        currentEntry.next = m_freeList;

                        --m_count;

                        if (m_count == 0)
                        {
                            m_lastIndex = 0;
                            m_freeList = -1;
                        }
                        else
                        {
                            m_freeList = index;
                        }

                        return true;
                    }
                    last = index;
                }
            }
            return false;
        }

        /// <summary>Adds the specified enumerable to a set.</summary>
        /// <param name="enumerable">The element to add to the set.</param>
        public void AddRange(IEnumerable<T> enumerable)
        {
            UnionWith(enumerable);
        }

        /// <summary>
        /// Returns values contained in Set.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IEnumerable<T> Values()
        {
            var version = m_version;
            
            if (m_slots?.m_root?.Storage != null)
            {
                var items = m_slots.m_root.Storage;

                for (int i = 0; i < m_lastIndex; ++i)
                {
                    if (items[i].hashCode >= 0)
                    {
                        if (version != m_version)
                        {
                            throw new InvalidOperationException($"Set collection was modified during enumeration. {version - m_version} time(s). ");
                        }
                        
                        yield return items[i].value;
                    }
                }
            }
            else if(m_slots != null)
            {
                for (int i = 0; i < m_lastIndex; ++i)
                {
                    if (m_slots.ValueByRef(i).hashCode >= 0)
                    {
                        if (version != m_version)
                        {
                            throw new InvalidOperationException($"Set collection was modified during enumeration. {version - m_version} time(s).");
                        }
                        
                        yield return m_slots.ValueByRef(i).value;
                    }
                }
            }
        }
       
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Values().GetEnumerator();
        }
       
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Values().GetEnumerator();
        }
  
        /// <summary>
        /// Modifies the current Set&lt;T&gt; object to contain all elements that are present in itself, the specified collection, or both.
        /// </summary>
        /// <param name="other"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            foreach (T obj in other)
            {
                Add(obj);
            }
        }
      
        /// <summary>
        /// Removes all elements in the specified collection from the current Set&lt;T&gt; object.
        /// </summary>
        /// <param name="other"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (m_count == 0)
            {
                return;
            }

            if (other == this)
            {
                Clear();
            }
            else
            {
                foreach (T obj in other)
                {
                    Remove(obj);
                }
            }
        }
       
        /// <summary>
        /// Determines whether a Set&lt;T&gt; object is a superset of the specified collection.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool IsSupersetOf([NotNull] IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other is ICollection<T> objs)
            {
                if (objs.Count == 0)
                {
                    return true;
                }

                if (other is Set<T> set2 && AreEqualityComparersEqual(this, set2) && set2.Count > m_count)
                {
                    return false;
                }
            }
            return ContainsAllElements(other);
        }
        
        /// <summary>
        /// Determines whether the current Set&lt;T&gt; object and a specified collection share common elements.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (m_count == 0)
            {
                return false;
            }
            foreach (T obj in other)
            {
                if (Contains(obj))
                {
                    return true;
                }
            }
            return false;
        }
       
        /// <summary>
        /// Copies the Set&lt;T&gt;to an array.
        /// </summary>
        /// <param name="array"></param>
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0, m_count);
        }

        /// <summary>
        /// Copies the Set&lt;T&gt; or a portion of it to an array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <param name="count"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void CopyTo([NotNull] T[] array, int arrayIndex, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "An array index is negative.");
            }
            
            if (arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), $"An array index '{arrayIndex}' is greater or equal than array length ({array.Length}).");
            }
               
            if (count > m_count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Copy count is greater than the number of elements from start to the end of collection.");
            }
            
            if (count > array.Length - arrayIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Copy count is greater than the number of elements from arrayIndex to the end of destinationArray");
            }
            
            int num = 0;
            for (int index = 0; index < m_lastIndex && num < count; ++index)
            {
                ref var valueByRef = ref m_slots.ValueByRef(index);
                if (valueByRef.hashCode >= 0)
                {
                    array[arrayIndex + num] = valueByRef.value;
                    ++num;
                }
            }
        }

        /// <summary>
        /// Removes all elements that match the conditions defined by the specified predicate from a Set&lt;T&gt; collection.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public int RemoveWhere([NotNull] Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            int num = 0;
            for (int index = 0; index < m_lastIndex; ++index)
            {
                ref var byRef = ref m_slots.ValueByRef(index);
                if (byRef.hashCode >= 0)
                {
                    ref T obj = ref byRef.value;

                    if (match(obj) && Remove(obj))
                    {
                        ++num;
                    }
                }
            }
            return num;
        }

        private void Initialize(int capacity)
        {
            int prime = Prime.GetPrime(capacity);

            m_buckets = new (prime);
            m_slots = new (prime);

            m_buckets.Ensure(prime);
            m_slots.Ensure(prime);
            
            m_freeList = -1;
        }

        private void IncreaseCapacity()
        {
            int newSize = ExpandPrime(m_count);
            if (newSize <= m_count)
            {
                throw new ArgumentException($"Set capacity overflow happened. {newSize} is less than {m_count}.");
            }

            m_buckets.Clear();
            
            m_buckets.Ensure(newSize);
            m_slots.Ensure(newSize);

            if (m_buckets.m_root?.Storage != null && m_slots.m_root?.Storage != null)
            {
                var bucketsArr = m_buckets.m_root?.Storage;
                var slotsArr = m_slots.m_root?.Storage;

                for (int slotIndex = 0; slotIndex < m_lastIndex && slotIndex < slotsArr.Length ; ++slotIndex)
                {
                    ref var slot = ref slotsArr[slotIndex];
                
                    int bucketIndex = slot.hashCode % newSize;

                    slot.next = m_buckets.ValueByRef(bucketIndex) - 1;

                    bucketsArr[bucketIndex] = slotIndex + 1;
                }
            }
            else
            {
                for (int slotIndex = 0; slotIndex < m_lastIndex; ++slotIndex)
                {
                    ref var slot = ref m_slots.ValueByRef(slotIndex);
                
                    int bucketIndex = slot.hashCode % newSize;

                    slot.next = m_buckets.ValueByRef(bucketIndex) - 1;

                    m_buckets.ValueByRef(bucketIndex) = slotIndex + 1;
                }
            }
        }
        
        private void AddValue(int index, int hashCode, T value)
        {
            int storageIndex = hashCode % m_buckets.Count;

            var bucket = m_buckets.ValueByRef(storageIndex);

            ref var slot = ref m_slots.ValueByRef(index);

            slot.hashCode = hashCode;
            slot.value = value;
            slot.next = bucket - 1;

            m_buckets.ValueByRef(storageIndex) = index + 1;
        }

        private bool ContainsAllElements(IEnumerable<T> other)
        {
            foreach (T obj in other)
            {
                if (!Contains(obj))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool HashSetEquals(Set<T> set1, Set<T> set2, IEqualityComparer<T> comparer)
        {
            if (set1 == null)
            {
                return set2 == null;
            }

            if (set2 == null)
            {
                return false;
            }

            if (AreEqualityComparersEqual(set1, set2))
            {
                if (set1.Count != set2.Count)
                {
                    return false;
                }
                foreach (T obj in set2)
                {
                    if (!set1.Contains(obj))
                    {
                        return false;
                    }
                }
                return true;
            }
            foreach (T x in set2)
            {
                bool flag = false;
                foreach (T y in set1)
                {
                    if (comparer.Equals(x, y))
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AreEqualityComparersEqual(Set<T> set1, Set<T> set2)
        {
            return set1.Comparer.Equals((object)set2.Comparer);
        }

        private int InternalGetHashCode(ref T item)
        {
            if (IsReferenceType)
            {
                if (item == null)
                {
                    return 0;
                }
            }
            return m_comparer.GetHashCode(item) & int.MaxValue;
        }
        
        private static int ExpandPrime(int oldSize)
        {
            int min = 2 * oldSize;
            if ((uint) min > 2146435069U && 2146435069 > oldSize)
            {
                return 2146435069;
            }
            return Prime.GetPrime(min);
        }
      
        private void CopyFrom(Set<T> source)
        {
            int count = source.m_count;
            if (count == 0)
            {
                return;
            }

            int length = source.m_buckets.Count;

            if (ExpandPrime(count + 1) >= length)
            {
                m_buckets = new (source.m_buckets);
                m_slots = new (source.m_slots);

                m_lastIndex = source.m_lastIndex;
                m_freeList = source.m_freeList;
            }
            else
            {
                int lastIndex = source.m_lastIndex;
                var slots = source.m_slots;

                Initialize(count);

                int index = 0;
                for (int i = 0; i < lastIndex; ++i)
                {
                    int hashCode = slots.ValueByRef(i).hashCode;
                    if (hashCode >= 0)
                    {
                        AddValue(index, hashCode, slots[i].value);
                        ++index;
                    }
                }
                m_lastIndex = index;
            }
            m_count = count;
        }

        internal struct Slot
        {
            internal int hashCode;
            internal int next;
            internal T value;

            public override string ToString()
            {
                return $"{nameof(hashCode)}: {hashCode}, {nameof(next)}: {next}, {nameof(value)}: {value}";
            }
        }
    }
}

