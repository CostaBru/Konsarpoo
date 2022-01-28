using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// The Map&lt;TKey,TValue&gt; generic class provides a mapping from a set of keys to a set of values. Each addition to the Map consists of a value and its associated key. Retrieving a value by using its key is very fast, close to O(1), because the Map&lt;TKey,TValue&gt; class is implemented as a hash table.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public partial class Map<TKey, TValue> :  IDictionary<TKey, TValue>, 
                                             ICollection<KeyValuePair<TKey, TValue>>,
                                             IEnumerable<KeyValuePair<TKey, TValue>>, 
                                             IReadOnlyDictionary<TKey, TValue>, 
                                             IReadOnlyCollection<KeyValuePair<TKey, TValue>>, 
                                             IAppender<KeyValuePair<TKey, TValue>>,
                                             ISerializable, 
                                             IDeserializationCallback,
                                             IDisposable
    {
        [NonSerialized]
        private IEqualityComparer<TKey> m_comparer;
        
        [NonSerialized]
        private Data<int> m_buckets;
        [NonSerialized]
        private Data<Entry> m_entries;
        [NonSerialized]
        private Data<TValue> m_entryValues;
        
        [NonSerialized]
        private int m_count;
        [NonSerialized]
        private int m_freeCount;
        [NonSerialized]
        private int m_freeList;
        [NonSerialized]
        private KeyCollection m_keys;
        [NonSerialized]
        private ValueCollection m_values;
        private int m_version;

        [NonSerialized]
        private object m_syncRoot;
        
        [NonSerialized]
        private TValue m_nullRef;


        public Map()
            : this(0, null)
        {
        }

        public Map(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        {
        }

        public Map(int capacity)
            : this(capacity, null)
        {
        }

        public Map(IReadOnlyDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        {
        }
        
        public Map(Map<TKey, TValue> dictionary)
        {
            m_comparer = dictionary.m_comparer;

            if (dictionary.m_buckets != null)
            {
                m_buckets = new(dictionary.m_buckets);
                m_entries = new(dictionary.m_entries);
                m_entryValues = new(dictionary.m_entryValues);
            }

            m_count = dictionary.m_count;
            m_freeCount = dictionary.m_freeCount;
            m_freeList = dictionary.m_freeList;
            m_version = dictionary.m_version;
        }

        public Map(IReadOnlyDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
                    : this(dictionary?.Count ?? 0, comparer)
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

        public Map(int capacity, [CanBeNull] IEqualityComparer<TKey> comparer)
        {
            switch (capacity)
            {
                case < 0:
                    throw new ArgumentOutOfRangeException(nameof(capacity));
                case > 0:
                    Initialize(capacity);
                    break;
            }

            m_comparer = comparer ?? EqualityComparer<TKey>.Default;
        }
        
        /// <summary>
        /// Destructor called by GC. Shouldn't be called if instance is properly disposed beforehand.
        /// </summary>
        ~Map()
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
        /// Adds the specified key and value to the map.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Add(TKey key, TValue value)
        {
            Insert(ref key, ref value, true);
        }
        
        /// <summary>
        /// Adds the specified key value pair to the map.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Add(KeyValuePair<TKey, TValue> value)
        {
            var val = value.Value;
            var key = value.Key;
            
            Insert(ref key, ref val, true);
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

            m_buckets?.Dispose();
            m_entries?.Dispose();
            m_entryValues?.Dispose();

            m_buckets = null;
            m_entries = null;
            m_entryValues = null;

            m_freeList = -1;
            m_count = 0;
            m_freeCount = 0;
            m_version++;
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
            var values = m_entryValues.m_root?.Storage;
            
            if (keys != null && values != null)
            {
                if (ReferenceEquals(value, null))
                {
                    for (int i = 0; i < m_count && i < keys.Length; i++)
                    {
                        ref var storeNodeItem = ref keys[i];
                        if ((storeNodeItem.HashCode >= 0))
                        {
                            if(ReferenceEquals(values[storeNodeItem.ValueRef], null))
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
                        if (key.HashCode >= 0)
                        {
                            if (comparer.Equals(values[key.ValueRef], value))
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
                        ref Entry entry = ref m_entries.ValueByRef(i);
                        if (entry.HashCode >= 0)
                        {
                            if (m_entryValues.ValueByRef(entry.ValueRef) == null)
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
                        ref Entry entry = ref m_entries.ValueByRef(j);
                        if (entry.HashCode >= 0)
                        {
                            if (comparer.Equals(m_entryValues.ValueByRef(entry.ValueRef), value))
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
            var values = m_entryValues;
            
            for (int i = 0; i < count; i++)
            {
                ref Entry entry = ref entries.ValueByRef(i);
                if (entry.HashCode >= 0)
                {
                    ref TValue val = ref values.ValueByRef(entry.ValueRef);
                    
                    destination[index++] = new KeyValuePair<TKey, TValue>(entry.Key, val);
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
            
            if (m_buckets != null)
            {
                var keys = m_entries.m_root?.Storage;
                var values = m_entryValues.m_root?.Storage;
                
                if (keys != null && values != null)
                {
                    var hashCode = m_comparer.GetHashCode(key) & 0x7fffffff;

                    for (var i = m_buckets[hashCode % m_buckets.Count] - 1; i >= 0 && i < keys.Length; i = keys[i].Next)
                    {
                        ref var storeNodeItem = ref keys[i];
                        if (storeNodeItem.HashCode == hashCode && m_comparer.Equals(storeNodeItem.Key, key))
                        {
                            success = true;

                            return ref values[storeNodeItem.ValueRef];
                        }
                    }
                }
                else
                {
                    var hashCode = m_comparer.GetHashCode(key) & 0x7fffffff;
                    for (var i = m_buckets[hashCode % m_buckets.Count] - 1; i >= 0; )
                    {
                        ref var currentEntry = ref m_entries.ValueByRef(i);
                        if ((currentEntry.HashCode == hashCode) && m_comparer.Equals(currentEntry.Key, key))
                        {
                            success = true;
                            
                            return ref m_entryValues.ValueByRef(currentEntry.ValueRef);
                        }

                        i = currentEntry.Next;
                    }
                }
            }

            return ref m_nullRef;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the Map&lt;TKey,TValue&gt;.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (m_entries?.m_root?.Storage != null && m_entryValues?.m_root?.Storage != null)
            {
                return new ArrayEnumerator(this);
            }
            
            return new Enumerator(this);
        }

        private void Initialize(int capacity)
        {
            int prime = Prime.GetPrime(capacity);

            m_buckets = new ();
            m_entries = new ();
            m_entryValues = new ();

            m_buckets.Ensure(prime);
            m_entries.Ensure(prime);
            m_entryValues.Ensure(prime);

            m_freeList = -1;
        }

        private void Insert(ref TKey key, ref TValue value, bool add)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            
            if (m_buckets == null)
            {
                Initialize(0);
            }

            int freeList;
            int hashCode = m_comparer.GetHashCode(key) & 0x7fffffff;
            int index = hashCode % m_buckets.Count;
            
            var keys = m_entries.m_root?.Storage;
            var values = m_entryValues.m_root?.Storage;

            if (keys != null && values != null)
            {
                for (int i = m_buckets[index] - 1; i >= 0; i = keys[i].Next)
                {
                    ref Entry keyEntry = ref keys[i];
                    
                    if (keyEntry.HashCode == hashCode && m_comparer.Equals(keyEntry.Key, key))
                    {
                        if (add)
                        {
                            throw new ArgumentException($"Key '{key}' is already exists.");
                        }

                        values[keyEntry.ValueRef] = value;
                        
                        m_version++;
                        return;
                    }
                }
            }
            else
            {
                for (int i = m_buckets[index] - 1; i >= 0;)
                {
                    ref Entry keyEntry = ref m_entries.ValueByRef(i);
                    
                    if (keyEntry.HashCode == hashCode && m_comparer.Equals(keyEntry.Key, key))
                    {
                        if (add)
                        {
                            throw new ArgumentException($"Key '{key}' is already exists.");
                        }

                        m_entryValues.ValueByRef(keyEntry.ValueRef) = value;
                        m_version++;
                        return;
                    }

                    i = keyEntry.Next;
                }
            }
          
            if (m_freeCount > 0)
            {
                freeList = m_freeList;
                m_freeList = m_entries[freeList].Next;
                m_freeCount--;
            }
            else
            {
                if (m_count == m_entries.Count)
                {
                    int prime = Prime.GetPrime(m_count * 2);

                    Resize(prime);
                    index = hashCode % m_buckets.Count;
                }
                freeList = m_count;
                m_count++;
            }
            m_entries[freeList] = new Entry(hashCode, m_buckets[index] - 1, key, freeList);
            m_entryValues[freeList] = value;
            m_buckets[index] = freeList + 1;
            m_version++;
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
            
            if (m_buckets != null)
            {
                int hashCode = m_comparer.GetHashCode(key) & 0x7fffffff;
                
                int index = hashCode % m_buckets.Count;
                int last = -1;

                var entries = m_entries.m_root?.Storage;
                var entryValues = m_entryValues.m_root?.Storage;
                var buckets = m_buckets.m_root?.Storage;

                if (entries != null && entryValues != null && buckets != null)
                {
                    for (int i = buckets[index] - 1; i >= 0; )
                    {
                        ref var keyEntry = ref entries[i];
                    
                        if ((keyEntry.HashCode == hashCode) && m_comparer.Equals(keyEntry.Key, key))
                        {
                            if (last < 0)
                            {
                                buckets[index] = keyEntry.Next + 1;
                            }
                            else
                            {
                                entries[last].Next = keyEntry.Next;
                            }
                            entries[i] = new Entry(-1, m_freeList, default(TKey), 0);
                            entryValues[i] = default;
                            m_freeList = i;
                            m_freeCount++;
                            m_version++;
                            return true;
                        }
                        last = i;
                        i = keyEntry.Next;
                    }
                }
                else
                {
                    for (int i = m_buckets[index] - 1; i >= 0; )
                    {
                        ref var keyEntry = ref m_entries.ValueByRef(i);
                    
                        if ((keyEntry.HashCode == hashCode) && m_comparer.Equals(keyEntry.Key, key))
                        {
                            if (last < 0)
                            {
                                m_buckets[index] = keyEntry.Next + 1;
                            }
                            else
                            {
                                m_entries.ValueByRef(last).Next = keyEntry.Next;
                            }
                            m_entries[i] = new Entry(-1, m_freeList, default(TKey), 0);
                            m_entryValues[i] = default;
                            m_freeList = i;
                            m_freeCount++;
                            m_version++;
                            return true;
                        }
                        last = i;
                        i = keyEntry.Next;
                    }
                }
            }
            return false;
        }

        private void Resize(int prime, bool forceNewHashCodes = false)
        {
            Data<int> newBuckets = new ();
            Data<Entry> newEntries = new ();
            Data<TValue> newEntryValues = new ();
            
            newEntries.Ensure(prime);
            newEntryValues.Ensure(prime);
            newBuckets.Ensure(prime);

            if (m_entries.m_root?.Storage != null && m_entryValues.m_root?.Storage != null)
            {
                var items = m_entries.m_root.Storage;
                var values = m_entryValues.m_root.Storage;

                var newBucketsArr = newBuckets.m_root?.Storage;
                var newEntriesArr = newEntries.m_root?.Storage;
                var newEntryValueArr = newEntryValues.m_root?.Storage;
                
                if (newEntriesArr != null && newEntryValueArr != null && newBucketsArr != null)
                {
                    for (var i = 0; i < m_entries.m_count && i < items.Length && i < newEntriesArr.Length && i < newEntryValueArr.Length; i++)
                    {
                        ref var keyEntry = ref items[i];
                        ref var valueEntry = ref values[keyEntry.ValueRef];
                    
                        keyEntry.ValueRef = i;

                        if (keyEntry.HashCode >= 0)
                        {
                            var bucket = keyEntry.HashCode % prime;
                            keyEntry.Next = newBucketsArr[bucket] - 1;
                            newBucketsArr[bucket] = i + 1;
                        }
                    
                        newEntriesArr[i] = keyEntry;
                        newEntryValueArr[i] = valueEntry;
                    }
                }
                else
                {
                    for (var i = 0; i < m_entries.m_count && i < items.Length; i++)
                    {
                        ref var keyEntry = ref items[i];
                        ref var valueEntry = ref values[keyEntry.ValueRef];
                    
                        keyEntry.ValueRef = i;

                        if (keyEntry.HashCode >= 0)
                        {
                            var bucket = keyEntry.HashCode % prime;
                            keyEntry.Next = newBuckets[bucket] - 1;
                            newBuckets[bucket] = i + 1;
                        }
                    
                        newEntries.ValueByRef(i) = keyEntry;
                        newEntryValues.ValueByRef(i) = valueEntry;
                    }
                }
            }
            else
            {
                for (var i = 0; i < m_entries.m_count; i++)
                {
                    ref var keyEntry = ref m_entries.ValueByRef(i);
                    ref var valueEntry = ref m_entryValues.ValueByRef(keyEntry.ValueRef);
                    
                    keyEntry.ValueRef = i;
                    
                    if (keyEntry.HashCode >= 0)
                    {
                        var bucket = keyEntry.HashCode % prime;

                        keyEntry.Next = newBuckets[bucket] - 1;
                        
                        newBuckets[bucket] = i + 1;
                    }
                    
                    newEntries.ValueByRef(i) = keyEntry;
                    newEntryValues.ValueByRef(i) = valueEntry;
                }
            }

            m_buckets?.Dispose();
            m_entries?.Dispose();
            m_entryValues?.Dispose();

            m_buckets = newBuckets;
            m_entries = newEntries;
            m_entryValues = newEntryValues;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> pair)
        {
            Add(pair.Key, pair.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> pair)
        {
            var val = ValueByRef(pair.Key, out var found);
            
            return (found && EqualityComparer<TValue>.Default.Equals(val, pair.Value));
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            CopyTo(array, index);
        }

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

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

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
                throw new KeyNotFoundException($"Key '{key}' is not found.");
            }
            set
            {
                Insert(ref key, ref value, false);
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

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        /// <summary>
        /// Gets a collection containing the keys in the Map&lt;TKey,TValue&gt;.
        /// </summary>
        public KeyCollection Keys => m_keys ??= new KeyCollection(this);

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

        /// <summary>
        /// Gets a collection containing the values in the Map&lt;TKey,TValue&gt;.
        /// </summary>
        public ValueCollection Values => m_values ??= new ValueCollection(this);

        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
        
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
        /// Python List API. Adds new item to the end of the Map&lt;TKey,TValue&gt;.
        /// </summary>
        /// <param name="value"></param>
        public void Append(KeyValuePair<TKey, TValue> value)
        {
            var val = value.Value;
            var key = value.Key;
            
            Insert(ref key, ref val, true);
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

        /// <summary>
        /// Returns a hashcode generated using default equality comparer for all items contained in Map&lt;TKey,TValue&gt;
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 7 ^ m_count.GetHashCode();

                foreach (var item in this)
                {
                    hashCode = (hashCode * 397) ^ EqualityComparer<TValue>.Default.GetHashCode(item.Value) ^ m_comparer.GetHashCode(item.Key);
                }
              
                return hashCode;
            }
        }
    }
}
