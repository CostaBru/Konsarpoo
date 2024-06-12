using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;
    
    /// <summary>
    /// The StringTrieMap&lt;TValue&gt; generic class that provides a mapping from a set of string keys to a set of values. Implemented as String Trie to store keys more efficiently.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    [DebuggerTypeProxy(typeof(TrieMapDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public partial class StringTrieMap<TValue> : 
        IDictionary<string, TValue>, 
        ICollection<KeyValuePair<string, TValue>>,
        IEnumerable<KeyValuePair<string, TValue>>, 
        IReadOnlyDictionary<string, TValue>, 
        IReadOnlyCollection<KeyValuePair<string, TValue>>, 
        IAppender<KeyValuePair<string, TValue>>,
        ISerializable, 
        IDeserializationCallback,
        IDisposable
    {
        [NonSerialized]
        private TrieNode<TValue> m_root = new('\0');
        
        [NonSerialized]
        private ushort m_version;
        
        [NonSerialized]
        [CanBeNull]
        private SerializationInfo m_sInfo;
        
        [NonSerialized] 
        private static TValue s_nullRef;
        
        [NonSerialized]
        [CanBeNull]
        private Func<string, TValue> m_missingValueFactory;
        
        [NonSerialized]
        private int m_count;
        
        private bool m_caseSensitive = false;

        /// <summary>
        /// Checks that both map and readonly dictionary has the same keys and values.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==([CanBeNull] StringTrieMap<TValue> a, [CanBeNull] IReadOnlyDictionary<string, TValue> b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
            if (RuntimeHelpers.Equals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.EqualsDict(b);
        }

        /// <summary>
        /// Checks that both map and readonly dictionary does not have the same keys and values.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(StringTrieMap<TValue> a, IReadOnlyDictionary<string, TValue> b) => !(a == b);

        /// <summary>
        /// Default StringTrieMap constructor. Case sensitive by default.
        /// </summary>
        public StringTrieMap() : this(true)
        {
        }

        /// <summary>
        /// StringTrieMap constructor with case sensitivity option.
        /// </summary>
        /// <param name="caseSensitive"></param>
        public StringTrieMap(bool caseSensitive)
        {
            this.m_caseSensitive = caseSensitive;
        }

        /// <summary>
        /// Copying constructor.
        /// </summary>
        /// <param name="copyFromMap"></param>
        public StringTrieMap(StringTrieMap<TValue> copyFromMap)
        {
            if (copyFromMap == null)
            {
                throw new ArgumentNullException(nameof(copyFromMap));
            }

            m_caseSensitive = copyFromMap.m_caseSensitive;
            m_missingValueFactory = copyFromMap.m_missingValueFactory;
            m_count = copyFromMap.m_count;

            var node = copyFromMap.m_root;

            var otherStack = new Data<TrieNode<TValue>>(copyFromMap.Count);
            otherStack.Push(node);

            var currentNode = m_root;
            
            var thisStack = new Data<TrieNode<TValue>>(copyFromMap.Count);
            thisStack.Push(currentNode);

            while (otherStack.Any)
            {
                var copyFrom = otherStack.Pop();
                var copyTo = thisStack.Pop();
                
                foreach (var child in copyFrom)
                {
                    var newNode = new TrieNode<TValue>(child.Key)
                    {
                        IsEndOfWord = child.Value.IsEndOfWord,
                        Value = child.Value.Value
                    };

                    copyTo.AddChild(child.Key, newNode);
                    
                    otherStack.Push(child.Value);
                    thisStack.Push(newNode);
                }
            }
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected StringTrieMap(SerializationInfo info, StreamingContext context) 
        {
            m_sInfo = info;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Clear();
        }
        
        /// <summary>
        /// SyncRoot object for thread safety.
        /// </summary>
        public object? SyncRoot => this;
        
        /// <summary>
        /// Current version of container.
        /// </summary>
        public int Version => m_version;
        
        /// <summary>
        /// Length of the collection.
        /// </summary>
        public int Length => Count;

        /// <inheritdoc />
        public int Count => m_count;

        /// <inheritdoc />
        public bool IsReadOnly => false;
        
        /// <summary>
        /// Case sensitivity of the map.
        /// </summary>
        public bool CaseSensitive => m_caseSensitive;

        /// <summary>
        /// Enumerator for the map.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            foreach (var tuple in GetKeyValuesString())
            {
                yield return new KeyValuePair<string, TValue>(tuple.Key, tuple.Value);
            }
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<string, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<string, TValue> item)
        {
            if (TryGetValueCore(item.Key, out var value))
            {
                return EqualityComparer<TValue>.Default.Equals(value ,item.Value);
            }

            return false;
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException(
                    "The number of elements in the source collection is greater than the available space from arrayIndex to the end of the destination array.");
            }

            foreach (var pair in GetKeyValuesString())
            {
                array[arrayIndex++] = new KeyValuePair<string, TValue>(pair.Key, pair.Value);
            }
        }
        
          
        IEnumerator IEnumerable.GetEnumerator() => GetKeyValuesString().GetEnumerator();

        /// <summary>
        /// Values of the map.
        /// </summary>
        public IEnumerable<TValue> Values => GetValues();
        
        public bool Remove(KeyValuePair<string, TValue> item) => Remove(item.Key);

        /// <inheritdoc />
        public bool ContainsKey(string key) => TryGetValueCore(key, out var _);

        /// <inheritdoc />
        public bool TryGetValue(string key, out TValue value) => TryGetValueCore(key, out value);

        /// <inheritdoc />
        public bool Remove(string key) => RemoveCore(key);

        bool IReadOnlyDictionary<string, TValue>.ContainsKey(string key) => TryGetValueCore(key, out var _);

        bool IDictionary<string, TValue>.TryGetValue(string key, out TValue value) => TryGetValueCore(key, out value);
        
        /// <inheritdoc />
        public void Add(string key, TValue value)
        {
            var add = true;
            Insert(key, ref value, ref add);
        }
        
        /// <summary>
        /// Indexer for the map. Throws KeyNotFoundException if key is not found.
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        public TValue this[string key]
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
                    Insert(key, ref newValue, ref set);

                    return newValue;
                }

                throw new KeyNotFoundException($"Key '{key}' was not found.");
            }
            set
            {
                var add = false;
                Insert(key, ref value, ref add);
            }
        }

        ICollection<string> IDictionary<string, TValue>.Keys
        {
            get
            {
                var data = new Data<string>(Count);
                
                data.AddRange(GetKeys());

                return data;
            }
        }

        ICollection<TValue> IDictionary<string, TValue>.Values
        {
            get
            {
                var data = new Data<TValue>(Count);
                
                data.AddRange(Values);

                return data;
            }
        }

        /// <inheritdoc />
        public void Append(KeyValuePair<string, TValue> value)
        {
            Add(value.Key, value.Value);
        }
        
        /// <inheritdoc />
        IEnumerable<string> IReadOnlyDictionary<string, TValue>.Keys
        {
            get
            {
                foreach (var kv in GetKeyValuesString())
                {
                    yield return kv.Key;
                }
            }
        }
        
        private IEnumerable<string> GetKeys()
        {
            foreach (var kv in GetKeyValuesString())
            {
                yield return kv.Key;
            }
        }
        
        /// <summary>
        /// Converts the map to a given list.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="index"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void CopyTo(IList<KeyValuePair<string, TValue>> destination, int index)
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
            
            var entries = GetKeyValuesString();

            foreach (var entry in entries)
            {
                destination[index++] = new KeyValuePair<string, TValue>(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Copies the elements of the map to a data collection, starting at a particular index.
        /// </summary>
        /// <returns></returns>
        public Data<KeyValuePair<string, TValue>> ToData()
        {
            var data = new Data<KeyValuePair<string, TValue>>(Count);

            var keyValues = GetKeyValuesString();

            foreach (var kv in keyValues)
            {
                data.Add(new KeyValuePair<string, TValue>(kv.Key, kv.Value));
            }

            return data;
        }

        /// <summary>
        /// Converts the map to an array.
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<string, TValue>[] ToArray()
        {
            var keyValuePairs = new KeyValuePair<string, TValue>[Count];

            int index = 0;
            foreach (var kv in GetKeyValuesString())
            {
                keyValuePairs[index] = new KeyValuePair<string, TValue>(kv.Key, kv.Value);

                index++;
            }

            return keyValuePairs;
        }
        
        /// <summary>
        /// Adds the specified key and value to the map.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Put(string key, TValue value)
        {
            var add = true;
            Insert(key, ref value, ref add);
        }
        
         /// <summary>
        /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
        /// </summary>
        /// <returns></returns>
        public TValue GetSet([NotNull] string key, Func<string, StringTrieMap<TValue>, TValue> missingValue)
        {
            if (TryGetValueCore(key, out var value))
            {
                return value;
            }

            return missingValue(key, this);
        }
    
        /// <summary>
        /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
        /// </summary>
        /// <returns></returns>
        public TValue GetSet<TParam>([NotNull] string key, TParam p1, Func<TParam, string, StringTrieMap<TValue>, TValue> missingValue)
        {
            if (TryGetValueCore(key, out var value))
            {
                return value;
            }

            return missingValue(p1, key, this);
        }
    
        /// <summary>
        /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
        /// </summary>
        /// <returns></returns>
        public TValue GetSet<TParam1, TParam2>([NotNull] string key, TParam1 p1, TParam2 p2,  Func<TParam1, TParam2, string, StringTrieMap<TValue>, TValue> missingValue)
        {
            if (TryGetValueCore(key, out var value))
            {
                return value;
            }

            return missingValue(p1, p2, key, this);
        }
    
        /// <summary>
        /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
        /// </summary>
        /// <returns></returns>
        public TValue GetSet<TParam1, TParam2, TParam3>([NotNull] string key, TParam1 p1, TParam2 p2, TParam3 p3, Func<TParam1, TParam2, TParam3, string, StringTrieMap<TValue>, TValue> missingValue)
        {
            if (TryGetValueCore(key, out var value))
            {
                return value;
            }

            return missingValue(p1, p2, p3, key, this);
        }
        
        /// <summary>
        /// Sets a missing value factory delegate up which would be called instead of throwing the KeyNotFound exception.
        /// </summary>
        /// <param name="missingValueFactory"></param>
        public void EnsureValues([CanBeNull] Func<string, TValue> missingValueFactory) => m_missingValueFactory = missingValueFactory;

        /// <summary>
        /// Inefficient way to get key by its index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public string KeyAt(int index) => GetKeys().ElementAt(index);

        /// <summary>
        /// Checks that this map and readonly dictionary has the same keys and values.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected bool EqualsDict([NotNull] IReadOnlyDictionary<string, TValue> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Count == other.Count)
            {
                foreach (var kv in this)
                {
                    if (!(other.TryGetValue(kv.Key, out var otherValue)))
                    {
                        return false;
                    }

                    if (!(EqualityComparer<TValue>.Default.Equals(kv.Value, otherValue)))
                    {
                        return false;
                    }
                }

                foreach (var kv in other)
                {
                    if (!(TryGetValueCore(kv.Key, out var otherValue)))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified Map&lt;TKey,TValue&gt; instances are considered equal by comparing type, sizes and elements.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return EqualsDict((StringTrieMap<TValue>)obj);
        }

        /// <inheritdoc />
        public void Clear()
        {
            var trieNode = m_root;
        
            m_root = new('\0');
            m_count = 0;
            unchecked { ++m_version; }

            var stack = new Data<TrieNode<TValue>>();
            stack.Push(trieNode);

            while (stack.Count > 0)
            {
                var node = stack.Pop();

                foreach (var child in node)
                {
                    stack.Push(child.Value);
                }
                
                node.Dispose();
            }
        }

        /// <summary>
        /// Returns values where key starts with a given substring. If substring is empty it returns all values.
        /// </summary>
        /// <param name="substring"></param>
        /// <returns></returns>
        public IEnumerable<TValue> WhereKeyStartsWith(string substring) => WhereKeyCore(substring, (p, s) => p.StartsWith(s));

        /// <summary>
        /// Returns values where key contains a given substring. If substring is empty it returns all values.
        /// </summary>
        /// <param name="substring"></param>
        /// <returns></returns>
        public IEnumerable<TValue> WhereKeyContains(string substring) => WhereKeyCore(substring, (p, s) => p.Contains(s));

        private IEnumerable<TValue> WhereKeyCore(string substring, Func<string, string, bool> func)
        {
            if (string.IsNullOrEmpty(substring))
            {
                foreach (var value in Values)
                {
                    yield return value;
                }

                yield break;
            }

            if (m_caseSensitive == false)
            {
                substring = substring.ToLower();
            }

            var version = m_version;

            var data = new Data<(TrieNode<TValue> Node, string Prefix)>();
            
            var stack = data.AsQueue();
            stack.Enqueue((m_root, string.Empty));

            while (stack.Any)
            {
                var (node, prefix) = stack.Dequeue();

                CheckState(ref version);

                //null prefix means that key is already starts with the substring and we provide all sub nodes.
                if (node.IsEndOfWord && (prefix == null || func(prefix, substring)))
                {
                    yield return node.Value;

                    foreach (var child in node)
                    {
                        stack.Enqueue((child.Value, null));
                    }
                }
                else
                {
                    if (prefix == null)
                    {
                        foreach (var child in node)
                        {
                            stack.Enqueue((child.Value, null));
                        }
                    }
                    else
                    {
                        foreach (var child in node)
                        {
                            stack.Enqueue((child.Value, prefix + child.Value.KeyChar));
                        }
                    }
                }
            }

            data.Dispose();
        }
        
        /// <summary>
        /// Returns values where key ends with a given substring. If substring is empty it returns all values.
        /// </summary>
        /// <param name="substring"></param>
        /// <returns></returns>
        public IEnumerable<TValue> WhereKeyEndsWith(string substring)
        {
            if (string.IsNullOrEmpty(substring))
            {
                foreach (var value in Values)
                {
                    yield return value;
                }

                yield break;
            }

            if (m_caseSensitive == false)
            {
                substring = substring.ToLower();
            }

            var version = m_version;

            var data = new Data<(TrieNode<TValue> Node, string Prefix)>();
            
            var stack = data.AsQueue();
            stack.Enqueue((m_root, string.Empty));

            while (stack.Any)
            {
                var (node, prefix) = stack.Dequeue();

                CheckState(ref version);

                //IsEndOfWord and key is already ends with the substring we skip all sub nodes.
                if (node.IsEndOfWord && prefix.EndsWith(substring))
                {
                    yield return node.Value;
                }
                else
                {
                    foreach (var child in node)
                    {
                        stack.Enqueue((child.Value, prefix + child.Value.KeyChar));
                    }
                }
            }

            data.Dispose();
        }

        /// <summary>
        /// Checks if the map contains a given value. Is not efficient.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ContainsValue(TValue value)
        {
            foreach (var v in this.Values)
            {
                if (EqualityComparer<TValue>.Default.Equals(v, value))
                {
                    return true;
                }
            }

            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckState(ref ushort version)
        {
            if (version != m_version)
            {
                throw new InvalidOperationException($"Data collection was modified during enumeration.");
            }
        }
        
        private IEnumerable<(string Key, TValue Value)> GetKeyValuesString()
        {
            var version = m_version;
            
            var stack = new Data<(TrieNode<TValue> Node, string Prefix)>();
            stack.Push((m_root, string.Empty));
         
            while (stack.Count > 0)
            {
                var (node, prefix) = stack.Pop();
                
                CheckState(ref version);

                if (node.IsEndOfWord)
                {
                    yield return (prefix, node.Value);
                }

                foreach (var child in node)
                {
                    stack.Push((child.Value, prefix + child.Value.KeyChar));
                }
            }
            
            stack.Dispose();
        }
    }