using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Konsarpoo.Collections.Stackalloc;

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
        private static readonly bool IsReferenceType = !typeof(T).IsValueType;

        [NonSerialized]
        private readonly Data<int> m_buckets;
        [NonSerialized]
        private readonly Data<KeyEntry<T>> m_slots;
        
        private IEqualityComparer<T> m_comparer;

        private int m_lastIndex;
        private int m_freeList;
        private ushort m_version;

        /// <summary>
        /// Current version of container.
        /// </summary>
        public int Version => m_version;
        
        private int m_count;

        /// <summary>Initializes a new instance of the Set class that is empty and uses the default equality comparer for the set type.</summary>
        public Set()
          : this(0, 0, EqualityComparer<T>.Default)
        {
        }

        /// <summary>Initializes a new instance of the Set class that is empty and uses the default equality comparer for the set type.</summary>
        /// <param name="capacity">default capacity of internal storage.</param>
        public Set(int capacity)
          : this(capacity, 0, EqualityComparer<T>.Default)
        {
        }
        
        /// <summary>Initializes a new instance of the Set class that is empty and uses equality comparer given for the set type.</summary>
        /// <param name="comparer"> Equality comparer.</param>
        public Set([CanBeNull] IEqualityComparer<T> comparer) 
            : this(0,0, comparer)
        {
           
        }

        /// <summary>Initializes a new instance of the Set class that is empty and uses equality comparer given for the set type and pool set up.</summary>
        /// <param name="allocatorSetup"></param>
        /// <param name="comparer"> Equality comparer.</param>
        public Set(ISetAllocatorSetup<T> allocatorSetup, IEqualityComparer<T> comparer = null) 
            : this(0,0, allocatorSetup, comparer)
        {
           
        }

        /// <summary>Initializes a new instance of the Set class that is empty and uses the equality comparer given for the set type.</summary>
        /// <param name="capacity">default capacity of internal storage.</param>
        /// <param name="maxSizeStorageNodeArray"></param>
        /// <param name="comparer"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Set(int capacity, int maxSizeStorageNodeArray, IEqualityComparer<T> comparer) : this(capacity, maxSizeStorageNodeArray, null, comparer)
        {
        }

        /// <summary>Initializes a new instance of the Set class that is empty and uses the equality comparer given for the set type.</summary>
        /// <param name="capacity">default capacity of internal storage.</param>
        /// <param name="maxSizeStorageNodeArray"></param>
        /// <param name="allocatorSetup"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Set(int capacity, int maxSizeStorageNodeArray, ISetAllocatorSetup<T> allocatorSetup) : this(capacity, maxSizeStorageNodeArray, allocatorSetup, null)
        {
        }

        /// <summary>Initializes a new instance of the Set class that is empty and uses the equality comparer given for the set type.</summary>
        /// <param name="capacity">default capacity of internal storage.</param>
        /// <param name="maxSizeStorageNodeArray"></param>
        /// <param name="allocatorSetup"></param>
        /// <param name="comparer"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Set(int capacity, int maxSizeStorageNodeArray, ISetAllocatorSetup<T> allocatorSetup, IEqualityComparer<T> comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            var setPoolSetup = allocatorSetup ?? KonsarpooAllocatorGlobalSetup.DefaultAllocatorSetup.GetSetAllocator<T>();
            
            m_buckets = new (capacity, maxSizeStorageNodeArray, setPoolSetup?.GetBucketsAllocatorSetup());
            m_slots = new (capacity, maxSizeStorageNodeArray, setPoolSetup?.GeStorageAllocatorSetup());
            
            if (comparer == null)
            {
                comparer = EqualityComparer<T>.Default;
            }
            m_comparer = comparer;
            m_lastIndex = 0;
            m_count = 0;
            m_freeList = -1;

            if (capacity <= 0)
            {
                return;
            }
            
            Initialize(Prime.GetPrime(capacity));
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
        public Set(IEnumerable<T> collection, IEqualityComparer<T> comparer) : this(collection, null, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Set class. Uses equality comparer given for the set type and fills the set with given collection.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="allocatorSetup"></param>
        /// <param name="comparer"></param>
        public Set(IEnumerable<T> collection, ISetAllocatorSetup<T> allocatorSetup, IEqualityComparer<T> comparer) : this(0, 0, allocatorSetup, comparer)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (collection is Set<T> objSet && AreEqualityComparersEqual(this, objSet))
            {
                int count = objSet.m_count;
                if (count == 0)
                {
                    return;
                }
                
                m_buckets = new(objSet.m_buckets);
                m_slots = new(objSet.m_slots);

                m_lastIndex = objSet.m_lastIndex;
                m_freeList = objSet.m_freeList;

                m_count = count;
            }
            else
            {
                int capacity = !(collection is ICollection<T> objs) ? 0 : objs.Count;
                
                Initialize(Prime.GetPrime(capacity));

                UnionWith(collection);
            }
        }
        
        /// <summary>
        /// Destructor called by GC. Shouldn't be called if instance is properly disposed beforehand.
        /// </summary>
        ~Set()
        {
            DisposeCore();
        }

        /// <summary>
        /// Clears container. Suppresses instance finalization.
        /// </summary>
        public void Dispose()
        {
            DisposeCore();

            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Gets the IEqualityComparer&lt;T&gt; that is used to determine equality of values for the set.
        /// </summary>
        public IEqualityComparer<T> Comparer => m_comparer;

        /// <summary>
        /// Gets the number of values contained in the Set&lt;T&gt;.
        /// </summary>
        public int Count => m_count;
        
        /// <summary>
        /// Array API. Gets the number of values contained in the Set&lt;T&gt;.
        /// </summary>
        public int Length => m_count;
        
        /// <summary>
        /// Gets an object that can be used to synchronize access to the Set&lt;T&gt; class instance.
        /// </summary>
        public object SyncRoot => this;

        /// <summary>
        /// Python List API. Adds new item to the end of the Set&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        public void Append(T item)
        {
            Add(item);
        }

        /// <summary>
        /// Returns the actual buckets count. If the value is equal to values count resize will happen on text insert.
        /// </summary>
        public int BucketCount => m_buckets.Count;
        
        /// <summary>
        /// Returns the bucket index for given key.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetBucketIndex([NotNull]ref T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            int hashCode = 0;
            if (IsReferenceType)
            {
                if (item == null)
                {
                    hashCode = 0;
                }
                else
                {
                    hashCode = m_comparer.GetHashCode(item) & int.MaxValue;
                }
            }
            else
            {
                hashCode = m_comparer.GetHashCode(item) & int.MaxValue;
            }

            return hashCode % m_buckets.m_count;
        }
        
        /// <summary>Adds the specified element to a set.</summary>
        /// <param name="value">The element to add to the set.</param>
        /// <returns>
        /// <see langword="true" /> if the element is added to the set object; <see langword="false" /> if the element is already present.</returns>
        public bool Add(T value)
        {
            if (m_buckets.m_count == 0)
            {
                Initialize(prime: 2);
            }
            
            var hashCode = m_comparer.GetHashCode(value) & int.MaxValue;

            int storageIndex = hashCode % m_buckets.m_count;

            var slotArr = m_slots.m_root?.Storage;

            if (slotArr != null)
            {
                var bucketsArray = m_buckets.m_root.Storage;

                var start = bucketsArray[storageIndex];

                for (int? i = start - 1; i >= 0; i = slotArr[i.Value].Next)
                {
                    ref var s = ref slotArr[i.Value];

                    if (s.HashCode == hashCode && m_comparer.Equals(s.Key, value))
                    {
                        return false;
                    }
                }
            }
            else
            {
                var start = m_buckets.ValueByRef(storageIndex);

                for (int? i = start - 1; i >= 0; i = m_slots.ValueByRef(i.Value).Next)
                {
                    ref var s = ref m_slots.ValueByRef(i.Value);

                    if (s.HashCode == hashCode && m_comparer.Equals(s.Key, value))
                    {
                        return false;
                    }
                }
            }

            int index;
            if (m_freeList >= 0)
            {
                index = m_freeList;
                m_freeList = m_slots.ValueByRef(index).Next;
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
            
            slotArr = m_slots.m_root?.Storage;

            if (slotArr != null)
            {
                var bucketsArray = m_buckets.m_root?.Storage;
                
                var bucket = bucketsArray[storageIndex];

                ref var slot = ref slotArr[index];

                slot.HashCode = hashCode;
                slot.Key = value;
                slot.Next = bucket - 1;
                
                bucketsArray[storageIndex] = index + 1;
            }
            else
            {
                var bucket = m_buckets.ValueByRef(storageIndex);

                ref var slot = ref m_slots.ValueByRef(index);

                slot.HashCode = hashCode;
                slot.Key = value;
                slot.Next = bucket - 1;
                
                m_buckets.ValueByRef(storageIndex) = index + 1;
            }

            ++m_count;
            unchecked { ++m_version; }
          
            return true;
        }

        /// <summary>Removes all elements from a Set object.</summary>
        public void Clear()
        {
            m_slots.Clear();
            m_buckets.Clear();

            m_lastIndex = 0;
            m_count = 0;
            m_freeList = -1;
            
            unchecked { ++m_version; }
        }
        
        private void DisposeCore()
        {
            m_buckets?.Dispose();
            m_slots?.Dispose();

            m_freeList = -1;
            m_count = 0;
            m_version = ushort.MaxValue;
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
        public bool Contains(T item)
        {
            if (m_buckets.m_count == 0)
            {
                return false;
            }
            
            var slotArray = m_slots.m_root?.Storage;

            if (slotArray != null)
            {
                return new SetRs<T>(m_buckets.m_root.Storage, slotArray, m_buckets.m_count, m_count, m_comparer)
                    .Contains(item);
            }
            
            int hashCode = IsReferenceType && item == null ? 0 : m_comparer.GetHashCode(item) & int.MaxValue;
            
            var storageIndex = hashCode % m_buckets.m_count;
            
            int start;
            
            var bucketsArray = m_buckets.m_root?.Storage;

            if (bucketsArray != null)
            {
                start = bucketsArray[storageIndex];
            }
            else
            {
                start = m_buckets.ValueByRef(storageIndex);
            }

            for (int? index = start - 1; index >= 0; index = m_slots.ValueByRef(index.Value).Next)
            {
                ref var slot = ref m_slots.ValueByRef(index.Value);

                if (slot.HashCode == hashCode && m_comparer.Equals(slot.Key, item))
                {
                    return true;
                }
            }

            return false;
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool ICollection<T>.IsReadOnly => false;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
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
        public bool Discard(T item)
        {
            return Remove(item);
        }
        
        /// <summary>
        /// Removes the item from the Set&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Remove(T item)
        {
            if (m_buckets.Count > 0)
            {
                int hashCode = 0;
                if (IsReferenceType)
                {
                    if (item == null)
                    {
                        hashCode = 0;
                    }
                    else
                    {
                        hashCode = m_comparer.GetHashCode(item) & int.MaxValue;
                    }
                }
                else
                {
                    hashCode = m_comparer.GetHashCode(item) & int.MaxValue;
                }

                int index1 = hashCode % m_buckets.Count;
                int last = -1;

                var start = m_buckets.ValueByRef(index1);

                for (int index = start - 1; index >= 0; index = m_slots.ValueByRef(index).Next)
                {
                    ref var currentEntry = ref m_slots.ValueByRef(index);

                    if (currentEntry.HashCode == hashCode && m_comparer.Equals(currentEntry.Key, item))
                    {
                        if (last < 0)
                        {
                            m_buckets.ValueByRef(index1) = currentEntry.Next  + 1;
                        }
                        else
                        {
                            m_slots.ValueByRef(last).Next = currentEntry.Next;
                        }

                        currentEntry.HashCode = -1;
                        currentEntry.Key = default(T);
                        currentEntry.Next = m_freeList;

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
            
            var slotArr =m_slots.m_root?.Storage;
            
            if (slotArr != null)
            {
                for (int i = 0; i < m_lastIndex && i < slotArr.Length; ++i)
                {
                    if (slotArr[i].HashCode >= 0)
                    {
                        if (version != m_version)
                        {
                            throw new InvalidOperationException($"Set collection was modified during enumeration.");
                        }
                        
                        yield return slotArr[i].Key;
                    }
                }
            }
            else 
            {
                for (int i = 0; i < m_lastIndex; ++i)
                {
                    if (m_slots.ValueByRef(i).HashCode >= 0)
                    {
                        if (version != m_version)
                        {
                            throw new InvalidOperationException($"Set collection was modified during enumeration.");
                        }
                        
                        yield return m_slots.ValueByRef(i).Key;
                    }
                }
            }
        }
       
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IEnumerator<T> GetEnumerator()
        {
            return Values().GetEnumerator();
        }
       
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
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

            if (ReferenceEquals(other, this) || (other is Set<T> otherSet && HashSetEquals(otherSet, this, this.m_comparer)))
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
        public void CopyTo(IList<T> array)
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
        public void CopyTo([NotNull] IList<T> array, int arrayIndex, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "An array index is negative.");
            }
            
            if (arrayIndex >= array.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), $"An array index '{arrayIndex}' is greater or equal than array length ({array.Count}).");
            }
               
            if (count > m_count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Copy count is greater than the number of elements from start to the end of collection.");
            }
            
            if (count > array.Count - arrayIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Copy count is greater than the number of elements from arrayIndex to the end of destinationArray");
            }
            
            int num = 0;
            for (int index = 0; index < m_lastIndex && num < count; ++index)
            {
                ref var valueByRef = ref m_slots.ValueByRef(index);
                if (valueByRef.HashCode >= 0)
                {
                    array[arrayIndex + num] = valueByRef.Key;
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
                if (byRef.HashCode >= 0)
                {
                    ref T obj = ref byRef.Key;

                    if (match(obj) && Remove(obj))
                    {
                        ++num;
                    }
                }
            }
            return num;
        }

        private void Initialize(int prime)
        {

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

            var bucketsArray = m_buckets.m_root?.Storage;
            var slotsArray = m_slots.m_root?.Storage;
            
            if (bucketsArray != null && slotsArray != null)
            {
                var bucketsArr = bucketsArray;
                var slotsArr = slotsArray;

                for (int slotIndex = 0; slotIndex < m_lastIndex && slotIndex < slotsArr.Length ; ++slotIndex)
                {
                    ref var slot = ref slotsArr[slotIndex];
                
                    int bucketIndex = slot.HashCode % newSize;

                    slot.Next = m_buckets.ValueByRef(bucketIndex) - 1;

                    bucketsArr[bucketIndex] = slotIndex + 1;
                }
            }
            else
            {
                for (int slotIndex = 0; slotIndex < m_lastIndex; ++slotIndex)
                {
                    ref var slot = ref m_slots.ValueByRef(slotIndex);
                
                    int bucketIndex = slot.HashCode % newSize;

                    slot.Next = m_buckets.ValueByRef(bucketIndex) - 1;

                    m_buckets.ValueByRef(bucketIndex) = slotIndex + 1;
                }
            }
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

        private static int ExpandPrime(int oldSize)
        {
            int min = 2 * oldSize;
            if ((uint) min > 2146435069U && 2146435069 > oldSize)
            {
                return 2146435069;
            }
            return Prime.GetPrime(min);
        }
    }
}

