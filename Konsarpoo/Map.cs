using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// The Map&lt;TKey,TValue&gt; generic class provides a mapping from a set of keys to a set of values. Each addition to the Map consists of a value and its associated key. Retrieving a value by using its key is very fast, close to O(1), because the Map&lt;TKey,TValue&gt; class is implemented as a hash table. It has support of python default dict api (EnsureValues method call sets a missing value factory up)
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public partial class Map<TKey, TValue> : IDictionary<TKey, TValue>, 
                                             ICollection<KeyValuePair<TKey, TValue>>,
                                             IEnumerable<KeyValuePair<TKey, TValue>>, 
                                             IReadOnlyDictionary<TKey, TValue>, 
                                             IReadOnlyCollection<KeyValuePair<TKey, TValue>>, 
                                             IAppender<KeyValuePair<TKey, TValue>>,
                                             ISerializable, 
                                             IDeserializationCallback,
                                             IDisposable
    {
        internal const int HashCoef = 0x7fffffff;

        [NonSerialized]
        private IEqualityComparer<TKey> m_comparer;
        
        [NonSerialized]
        internal readonly Data<int> m_buckets;
        [NonSerialized]
        private readonly Data<Entry<TKey, TValue>> m_entries;
       
        
        [NonSerialized]
        private int m_count;
        [NonSerialized]
        private int m_freeCount;
        [NonSerialized]
        private int m_freeList;
        
        private ushort m_version;

        /// <summary>
        /// Current version of container.
        /// </summary>
        public int Version => m_version;
        
        [NonSerialized]
        private static TValue s_nullRef;

        [NonSerialized]
        [CanBeNull]
        private Func<TKey, TValue> m_missingValueFactory;

        /// <summary>
        /// Default Map constructor.
        /// </summary>
        public Map()
            : this(0, 0, null)
        {
        }

        /// <summary>
        /// Default Map constructor that takes equality comparer.
        /// </summary>
        /// <param name="comparer"></param>
        public Map(IEqualityComparer<TKey> comparer)
            : this(0, 0, comparer)
        {
        }

        /// <summary>
        /// Map constructor that takes initial capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public Map(int capacity)
            : this(capacity,0, null)
        {
        }
        
        /// <summary>
        /// Constructor that takes initial capacity and default equality comparer.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="comparer"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Map(int capacity, [CanBeNull] IEqualityComparer<TKey> comparer) : this(capacity, 0, comparer) 
        {
        }

        /// <summary>
        /// Constructor that takes another dictionary.
        /// </summary>
        /// <param name="dictionary"></param>
        public Map(IReadOnlyDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        {
        }
        
        /// <summary>
        /// Copying constructor.
        /// </summary>
        /// <param name="dictionary"></param>
        public Map(Map<TKey, TValue> dictionary)
        {
            m_comparer = dictionary.m_comparer;

            var mapAllocatorSetup = KonsarpooAllocatorGlobalSetup.DefaultAllocatorSetup.GetMapAllocator<TKey, TValue>();
            
            m_buckets = new(dictionary.m_buckets, mapAllocatorSetup.GetBucketAllocatorSetup());
            m_entries = new Data<Entry<TKey, TValue>>(dictionary.m_entries, mapAllocatorSetup.GetStorageAllocatorSetup());

            m_count = dictionary.m_count;
            m_freeCount = dictionary.m_freeCount;
            m_freeList = dictionary.m_freeList;
            m_version = dictionary.m_version;
            m_missingValueFactory = dictionary.m_missingValueFactory;
        }

        /// <summary>
        /// Constructor with ready to use data store instances.
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="buckets"></param>
        /// <param name="entries"></param>
        public Map([NotNull] Data<int> buckets, [NotNull] Data<Entry<TKey, TValue>> entries, [CanBeNull] IEqualityComparer<TKey> comparer = null)
        {
            m_buckets = buckets ?? throw new ArgumentNullException(nameof(buckets));
            m_entries = entries ?? throw new ArgumentNullException(nameof(entries));
            m_comparer = comparer ?? EqualityComparer<TKey>.Default;;
        }

        /// <summary>
        /// Constructor with max size of array per node.
        /// </summary>
        /// <param name="maxSizeStorageNodeArray"></param>
        /// <param name="capacity"></param>
        /// <param name="comparer"></param>
        public Map(int capacity, int maxSizeStorageNodeArray, [CanBeNull] IEqualityComparer<TKey> comparer = null) : this(capacity, maxSizeStorageNodeArray, null, comparer)
        {
        }

        /// <summary>
        /// Constructor with pool set up and comparer.
        /// </summary>
        /// <param name="mapAllocatorSetup"></param>
        /// <param name="comparer"></param>
        public Map(IMapAllocatorSetup<TKey, TValue> mapAllocatorSetup, [CanBeNull] IEqualityComparer<TKey> comparer = null) : this(0, 0, mapAllocatorSetup, comparer)
        {
        }

        /// <summary>
        /// Constructor with max size of array per node.
        /// </summary>
        /// <param name="maxSizeStorageNodeArray"></param>
        /// <param name="capacity"></param>
        /// <param name="mapAllocatorSetup"></param>
        /// <param name="comparer"></param>
        public Map(int capacity, int maxSizeStorageNodeArray, IMapAllocatorSetup<TKey, TValue> mapAllocatorSetup, [CanBeNull] IEqualityComparer<TKey> comparer = null)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            var poolSetup = mapAllocatorSetup ?? KonsarpooAllocatorGlobalSetup.DefaultAllocatorSetup.GetMapAllocator<TKey, TValue>();

            m_comparer = comparer ?? EqualityComparer<TKey>.Default;
            
            m_buckets = new (capacity, maxSizeStorageNodeArray, poolSetup?.GetBucketAllocatorSetup());
            m_entries = new (capacity, maxSizeStorageNodeArray, poolSetup?.GetStorageAllocatorSetup());
            m_comparer = comparer ?? EqualityComparer<TKey>.Default;

            if (capacity > 0)
            {
                Initialize(Prime.GetPrime(capacity));
            }
        }

        /// <summary>
        /// Constructor that takes another dictionary and equality comparer.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="comparer"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Map(IReadOnlyDictionary<TKey, TValue> dictionary,  IEqualityComparer<TKey> comparer)
            : this(dictionary, 0, null, comparer)
        {
        }

        /// <summary>
        /// Constructor that takes another dictionary and equality comparer.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="mapAllocatorSetup"></param>
        /// <param name="comparer"></param>
        /// <param name="maxSizeStorageNodeArray"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Map(IReadOnlyDictionary<TKey, TValue> dictionary, int maxSizeStorageNodeArray, IMapAllocatorSetup<TKey, TValue> mapAllocatorSetup, IEqualityComparer<TKey> comparer)
                    : this(dictionary?.Count ?? 0, maxSizeStorageNodeArray, mapAllocatorSetup, comparer)
        {
            if (ReferenceEquals(dictionary, null))
            {
                throw new ArgumentNullException("dictionary");
            }
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                Add(pair.Key, pair.Value);
            }
        }

       /// <summary>
       /// Sets a missing value factory delegate up which would be called instead of throwing the KeyNotFound exception.
       /// </summary>
       /// <param name="missingValueFactory"></param>
       public void EnsureValues([CanBeNull] Func<TKey, TValue> missingValueFactory)
       {
           m_missingValueFactory = missingValueFactory;
       }
       
       /// <summary>
       /// Allocates the internal storage to fit a given number of items beforehand.
       /// </summary>
       /// <param name="capacity"></param>
       public void EnsureCapacity(int capacity)
       {
           var prime = Prime.GetPrime(capacity);

           m_buckets.Ensure(prime);
           m_entries.Ensure(prime);
       }
        
        /// <summary>
        /// Destructor called by GC. Shouldn't be called if instance is properly disposed beforehand.
        /// </summary>
        ~Map()
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

        private void DisposeCore()
        {
            m_buckets?.Dispose();
            m_entries?.Dispose();
            
            m_freeList = -1;
            m_count = 0;
            m_freeCount = 0;
            m_version = ushort.MaxValue;
        }

        /// <summary>
        /// Adds the specified key and value to the map.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            var add = true;
            Insert(ref key, ref value, ref add);
        }
        
        /// <summary>
        /// Adds the specified key and value to the map.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Put(TKey key, TValue value)
        {
            var add = true;
            Insert(ref key, ref value, ref add);
        }
        
        /// <summary>
        /// Adds the specified key value pair to the map.
        /// </summary>
        /// <param name="value"></param>
        public void Add(KeyValuePair<TKey, TValue> value)
        {
            var val = value.Value;
            var key = value.Key;
            var add = true;
            Insert(ref key, ref val, ref add);
        }

        /// <summary>
        /// Removes all contents.
        /// </summary>
        public void Clear()
        {
            if (m_count <= 0)
            {
                return;
            }

            m_buckets.Clear();
            m_entries.Clear();

            m_freeList = -1;
            m_count = 0;
            m_freeCount = 0;
            unchecked { ++m_version; }
        }

        /// <summary>
        /// Determines whether the Map&lt;TKey,TValue&gt; contains the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public bool ContainsKey(TKey key)
        {
            ValueByRef(key, out var found);
            
            return found;
        }

        /// <summary>
        /// Determines whether the Map&lt;TKey,TValue&gt; has the specified key missing.
        /// </summary>
        /// <param name="key"></param>
        public bool MissingKey(TKey key)
        {
            ValueByRef(key, out var found);
            
            return !found;
        }

        /// <summary>
        /// Determines whether the Map&lt;TKey,TValue&gt; contains the specified value.
        /// </summary>
        /// <param name="value"></param>
        public bool ContainsValue(TValue value)
        {
            if (m_entries == null)
            {
                return false;
            }
            
            var keys = m_entries.m_root?.Storage;
            
            if (keys != null)
            {
                if (ReferenceEquals(value, null))
                {
                    for (int i = 0; i < m_count && i < keys.Length; i++)
                    {
                        ref var storeNodeItem = ref keys[i];
                        if ((storeNodeItem.Key.HashCode >= 0))
                        {
                            if(ReferenceEquals(storeNodeItem.Value, null))
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    var comparer = EqualityComparer<TValue>.Default;
                    for (int j = 0; j < m_count && j < keys.Length; j++)
                    {
                        ref var key = ref keys[j];
                        if (key.Key.HashCode >= 0)
                        {
                            if (comparer.Equals(key.Value, value))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                if (value == null)
                {
                    for (int i = 0; i < m_count; i++)
                    {
                        ref var entry = ref m_entries.ValueByRef(i);
                        if (entry.Key.HashCode >= 0)
                        {
                            if (entry.Value == null)
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    var comparer = EqualityComparer<TValue>.Default;
                    for (int j = 0; j < m_count; j++)
                    {
                        ref var entry = ref m_entries.ValueByRef(j);
                        if (entry.Key.HashCode >= 0)
                        {
                            if (comparer.Equals(entry.Value, value))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void CopyTo(IList<KeyValuePair<TKey, TValue>> destination, int index)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if ((index < 0) || (index > destination.Count))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((destination.Count - index) < Count)
            {
                throw new ArgumentException();
            }
            
            int count = m_count;
            var entries = m_entries;
            
            for (int i = 0; i < count; i++)
            {
                ref var entry = ref entries.ValueByRef(i);
                if (entry.Key.HashCode >= 0)
                {
                    destination[index++] = new KeyValuePair<TKey, TValue>(entry.Key.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// Returns value by its reference using key given.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="success"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ref TValue ValueByRef([NotNull] TKey key, out bool success)
        {
            success = false;
            
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            
            if (m_buckets.m_count > 0)
            {
                Entry<TKey, TValue>[] keys = (m_entries.m_root as Data<Entry<TKey, TValue>>.StoreNode)?.Storage;
                
                if (keys != null)
                {
                    return ref new MapRs<TKey, TValue>(m_buckets.m_root.Storage, keys, m_buckets.m_count, m_count, m_comparer)
                        .ValueByRef(key, out success);
                }
                
                var hashCode = m_comparer.GetHashCode(key) & HashCoef;

                for (var i = m_buckets[hashCode % m_buckets.m_count] - 1; i >= 0; )
                {
                    ref var currentEntry = ref m_entries.ValueByRef(i);
                    if ((currentEntry.Key.HashCode == hashCode) && m_comparer.Equals(currentEntry.Key.Key, key))
                    {
                        success = true;
                            
                        return ref currentEntry.Value;
                    }

                    i = currentEntry.Key.Next;
                }
            }

            s_nullRef = default;
            
            return ref s_nullRef;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the Map&lt;TKey,TValue&gt;.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (m_entries.m_root?.Storage != null)
            {
                return new ArrayEnumerator(this);
            }
            
            return new Enumerator(this);
        }

        private void Initialize(int prime)
        {
            m_buckets.Ensure(prime);
            m_entries.Ensure(prime);

            m_freeList = -1;
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
        public int GetBucketIndex([NotNull]ref  TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            
            var hashCode = m_comparer.GetHashCode(key) & HashCoef;
           
            return hashCode % m_buckets.m_count;
        }

        private void Insert([NotNull] ref TKey key, ref TValue value, ref bool add)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (m_buckets.m_count == 0)
            {
                Initialize(prime: 2);
            }

            int freeList;
            
            int hashCode = m_comparer.GetHashCode(key) & HashCoef;

            int index = hashCode % m_buckets.m_count;
            
            var keys = (m_entries.m_root as Data<Entry<TKey, TValue>>.StoreNode)?.Storage;

            var bucket = m_buckets[index] - 1;

            if (keys != null)
            {
                for (int i = bucket; i >= 0; i = keys[i].Key.Next)
                {
                    ref var keyEntry = ref keys[i];
                    
                    if (keyEntry.Key.HashCode == hashCode && m_comparer.Equals(keyEntry.Key.Key, key))
                    {
                        if (add)
                        {
                            throw new ArgumentException($"Key '{key}' is already exists.");
                        }

                        keyEntry.Value = value;
                        return;
                    }
                }
            }
            else
            {
                for (int i = bucket; i >= 0;)
                {
                    ref var keyEntry = ref m_entries.ValueByRef(i);
                    
                    if (keyEntry.Key.HashCode == hashCode && m_comparer.Equals(keyEntry.Key.Key, key))
                    {
                        if (add)
                        {
                            throw new ArgumentException($"Key '{key}' is already exists.");
                        }

                        keyEntry.Value = value;
                        return;
                    }

                    i = keyEntry.Key.Next;
                }
            }
          
            if (m_freeCount > 0)
            {
                freeList = m_freeList;
                m_freeList = m_entries[freeList].Key.Next;
                m_freeCount--;
            }
            else
            {
                if (m_count == m_entries.m_count)
                {
                    int prime = Prime.GetPrime(m_count * 2);

                    Resize(prime);
                    
                    index = hashCode % m_buckets.m_count;

                    bucket = m_buckets[index] - 1;
                }
                
                freeList = m_count;
                m_count++;
            }

            var entriesArray = (m_entries.m_root as Data<Entry<TKey, TValue>>.StoreNode)?.Storage;

            if (entriesArray != null)
            {
                ref var valueByRef = ref entriesArray[freeList];
            
                valueByRef.Key.HashCode = hashCode;
                valueByRef.Key.Next = bucket;
                valueByRef.Key.Key = key;
                valueByRef.Value = value;
            }
            else
            {
                ref var valueByRef = ref m_entries.ValueByRef(freeList);
            
                valueByRef.Key.HashCode = hashCode;
                valueByRef.Key.Next = bucket;
                valueByRef.Key.Key = key;
                valueByRef.Value = value;
            }
            
            var bucketsArray = (m_buckets.m_root as Data<int>.StoreNode)?.Storage;
            
            if (bucketsArray != null)
            {
                bucketsArray[index] = freeList + 1;
            }
            else
            {
                m_buckets[index] = freeList + 1;
            }
            
            unchecked { ++m_version; }
        }

        /// <summary>
        /// Removes the value with the specified key from the Map&lt;TKey,TValue&gt;.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Remove([NotNull] TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            
            if (m_buckets.Count > 0)
            {
                int hashCode = m_comparer.GetHashCode(key) & HashCoef;
                
                int index = hashCode % m_buckets.m_count;
                int last = -1;

                var entries = m_entries.m_root?.Storage;
                var buckets = m_buckets.m_root?.Storage;

                if (entries != null && buckets != null)
                {
                    for (int i = buckets[index] - 1; i >= 0; )
                    {
                        ref var keyEntry = ref entries[i];
                    
                        if ((keyEntry.Key.HashCode == hashCode) && m_comparer.Equals(keyEntry.Key.Key, key))
                        {
                            if (last < 0)
                            {
                                buckets[index] = keyEntry.Key.Next + 1;
                            }
                            else
                            {
                                entries[last].Key.Next = keyEntry.Key.Next;
                            }

                            entries[i] = new Entry<TKey, TValue>() { Key = new KeyEntry<TKey>(-1, m_freeList, default) };
                            m_freeList = i;
                            m_freeCount++;
                            unchecked { ++m_version; }
                            return true;
                        }
                        last = i;
                        i = keyEntry.Key.Next;
                    }
                }
                else
                {
                    for (int i = m_buckets[index] - 1; i >= 0; )
                    {
                        ref var keyEntry = ref m_entries.ValueByRef(i);
                    
                        if ((keyEntry.Key.HashCode == hashCode) && m_comparer.Equals(keyEntry.Key.Key, key))
                        {
                            if (last < 0)
                            {
                                m_buckets[index] = keyEntry.Key.Next + 1;
                            }
                            else
                            {
                                m_entries.ValueByRef(last).Key.Next = keyEntry.Key.Next;
                            }

                            m_entries[i] = new Entry<TKey, TValue>() { Key = new KeyEntry<TKey>(-1, m_freeList, default) };
                            m_freeList = i;
                            m_freeCount++;
                            unchecked { ++m_version; }
                            return true;
                        }
                        last = i;
                        i = keyEntry.Key.Next;
                    }
                }
            }
            return false;
        }

        
        private void Resize(int prime, bool forceNewHashCodes = false)
        {
            var entriesCount = m_entries.m_count;

            m_entries.Ensure(prime);

            var bucketsArray = (m_buckets.m_root as Data<int>.StoreNode)?.Storage;

            if (bucketsArray != null)
            {
                Array.Clear(bucketsArray, 0, bucketsArray.Length);
            }
            else
            {
                m_buckets.Clear();
            }
            
            m_buckets.Ensure(prime);

            var entries = (m_entries.m_root as Data<Entry<TKey, TValue>>.StoreNode)?.Storage;
            var bucketsValues = (m_buckets.m_root as Data<int>.StoreNode)?.Storage;

            if (entries != null && bucketsValues != null)
            {
                for (var i = 0; i < entriesCount && i < entries.Length; i++)
                {
                    ref var keyEntry = ref entries[i];

                    if (keyEntry.Key.HashCode >= 0)
                    {
                        var bucket = keyEntry.Key.HashCode % prime;
                        keyEntry.Key.Next = bucketsValues[bucket] - 1;
                        bucketsValues[bucket] = i + 1;
                    }
                }
            }
            else
            {
                for (var i = 0; i < entriesCount; i++)
                {
                    ref var keyEntry = ref m_entries.ValueByRef(i);
                    
                    if (keyEntry.Key.HashCode >= 0)
                    {
                        var bucket = keyEntry.Key.HashCode % prime;

                        keyEntry.Key.Next = m_buckets[bucket] - 1;
                        
                        m_buckets[bucket] = i + 1;
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> pair)
        {
            Add(pair.Key, pair.Value);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> pair)
        {
            var val = ValueByRef(pair.Key, out var found);
            
            return (found && EqualityComparer<TValue>.Default.Equals(val, pair.Value));
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            CopyTo(array, index);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> pair)
        {
            var value = ValueByRef(pair.Key, out var found);
            
            if (found && EqualityComparer<TValue>.Default.Equals(value, pair.Value))
            {
                Remove(pair.Key);
                return true;
            }
            return false;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
        
        /// <summary>
        /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
        /// </summary>
        /// <returns></returns>
        public TValue GetSet([NotNull] TKey key, Func<TKey, Map<TKey, TValue>, TValue> missingValue)
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            return missingValue(key, this);
        }
    
        /// <summary>
        /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
        /// </summary>
        /// <returns></returns>
        public TValue GetSet<TParam>([NotNull] TKey key, TParam p1, Func<TParam, TKey, Map<TKey, TValue>, TValue> missingValue)
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            return missingValue(p1, key, this);
        }
    
        /// <summary>
        /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
        /// </summary>
        /// <returns></returns>
        public TValue GetSet<TParam1, TParam2>([NotNull] TKey key, TParam1 p1, TParam2 p2,  Func<TParam1, TParam2, TKey, Map<TKey, TValue>, TValue> missingValue)
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            return missingValue(p1, p2, key, this);
        }
    
        /// <summary>
        /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
        /// </summary>
        /// <returns></returns>
        public TValue GetSet<TParam1, TParam2, TParam3>([NotNull] TKey key, TParam1 p1, TParam2 p2, TParam3 p3, Func<TParam1, TParam2, TParam3, TKey, Map<TKey, TValue>, TValue> missingValue)
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            return missingValue(p1, p2, p3, key, this);
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>True in case of success.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            value = ValueByRef(key, out var found);
            if (found)
            {
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Attempts to add the specified key and value to the map.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool TryAdd(TKey key, TValue value)
        {
            if (ContainsKey(key))
            {
                return false;
            }

            this[key] = value;

            return true;
        }
        
        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public TValue this[TKey key]
        {
            get
            {
                var value = ValueByRef(key, out var found);

                if (found)
                {
                    return value;
                }

                if (m_missingValueFactory != null)
                {
                    var newValue = m_missingValueFactory(key);
                    var set = false;
                    Insert(ref key, ref newValue, ref set);

                    return newValue;
                }
                
                throw new KeyNotFoundException($"Key '{key}' was not found.");
            }
            set
            {
                var set = false;
                Insert(ref key, ref value, ref set);
            }
        }
        
        /// <summary>
        /// Gets the IEqualityComparer&lt;T&gt; that is used to determine equality of keys for the map.
        /// </summary>
        public IEqualityComparer<TKey> Comparer => m_comparer;

        /// <summary>
        /// Gets the number of key/value pairs contained in the Map&lt;TKey,TValue&gt;.
        /// </summary>
        public int Count => (m_count - m_freeCount);
        
        /// <summary>
        /// Array API. Gets the number of key/value pairs contained in the Map&lt;TKey,TValue&gt;.
        /// </summary>
        public int Length => Count;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        /// <summary>
        /// Gets a collection containing the keys in the Map&lt;TKey,TValue&gt;.
        /// </summary>
        public KeyCollection Keys => new KeyCollection(this);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

        /// <summary>
        /// Gets a collection containing the values in the Map&lt;TKey,TValue&gt;.
        /// </summary>
        public ValueCollection Values => new ValueCollection(this);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        /// <summary>
        /// Creates a copy of contents in array.
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            var pairs = new KeyValuePair<TKey, TValue>[this.Count];
            
            CopyTo(pairs, 0);

            return pairs;
        }

        /// <summary>
        /// Inefficient way to get key by its index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public TKey KeyAt(int index) => Keys.ElementAt(index);
        
        /// <summary>
        /// Gets an object that can be used to synchronize access to the Map&lt;TKey,TValue&gt; class instance.
        /// </summary>
        public object SyncRoot => this;

        /// <summary>
        /// Python List API. Adds new item to the end of the Map&lt;TKey,TValue&gt;.
        /// </summary>
        /// <param name="value"></param>
        public void Append(KeyValuePair<TKey, TValue> value)
        {
            var val = value.Value;
            var key = value.Key;
            var add = true;
            Insert(ref key, ref val, ref add);
        }
        /// <summary>
        /// Determines whether the specified Map&lt;TKey,TValue&gt; instances are considered equal by comparing type, sizes and elements.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            
            return EqualsDict((Map<TKey, TValue>) obj);
        }
    }
}
