using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;
    
    internal class StringTrieMap<TValue> : IDictionary<string, TValue>, 
        ICollection<KeyValuePair<string, TValue>>,
        IEnumerable<KeyValuePair<string, TValue>>, 
        IReadOnlyDictionary<string, TValue>, 
        IReadOnlyCollection<KeyValuePair<string, TValue>>, 
        IAppender<KeyValuePair<string, TValue>>,
        ISerializable, 
        IDeserializationCallback,
        IDisposable
    {
        [DebuggerDisplay("'{Value}' {ChildrenCount} {IsEndOfWord}")]
        internal class TrieNode<TValue>
        {
            public char Value;
            public Map<char, TrieNode<TValue>> Children;
            public bool IsEndOfWord;
            public TValue Reference;
            public int ChildrenCount => Children.Count; 

            public TrieNode(char value)
            {
                Value = value;
                Children = new Map<char, TrieNode<TValue>>();
            }

            public TrieNode<TValue> GetChildNode(char c)
            {
                return Children.GetOrDefault(c);
            }

            public void AddChild(char c, TrieNode<TValue> newNode)
            {
                Children[c] = newNode;
            }

            public void Dispose()
            {
                foreach (var child in Children)
                {
                    child.Value.Dispose();
                }
            
                Children.Dispose();
            }
        }
        
        private TrieNode<TValue> root = new('\0');

        private bool m_caseSensitive = false;

        public StringTrieMap(bool caseSensitive = true)
        {
            this.m_caseSensitive = caseSensitive;
        }

        bool IReadOnlyDictionary<string, TValue>.ContainsKey(string key)
        {
            return ContainsKey(key);
        }
        
        public bool ContainsKey([NotNull] string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            
            var word = m_caseSensitive ? key : key.ToLower();
            var currentNode = root;
            foreach (var c in word)
            {
                currentNode = currentNode.GetChildNode(c);
                if (currentNode == null)
                {
                    return false;
                }
            }
            return currentNode.IsEndOfWord;
        }

        public bool TryGetValue(string key, out TValue value)
        {
            value = ValueByRef(key, out var success);

            return success;
        }
        
        [NonSerialized]
        private static TValue s_nullRef;
    
        public ref TValue ValueByRef(string key, out bool success)
        {
            var word = key;
            success = false;
            
            if (m_caseSensitive == false)
            {
                word = word.ToLower();
            }
            
            var currentNode = root;
            foreach (var c in word)
            {
                currentNode = currentNode.GetChildNode(c);
                if (currentNode == null)
                {
                    return ref s_nullRef;
                }
            }

            if (currentNode.IsEndOfWord)
            {
                success = true;

                return ref currentNode.Reference;
            }

            return ref s_nullRef;
        }

        public void Add(string key, TValue value)
        {
            var add = true;
            Insert(key, ref value, ref add);
        }

        bool IDictionary<string, TValue>.ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        private void Insert(string key, ref TValue value, ref bool add)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var word = key;

            if (m_caseSensitive == false)
            {
                word = word.ToLower();
            }

            if (TryGetValue(key, out var _))
            {
                if (add)
                {
                    throw new ArgumentException($"Key '{key}' is already exists.");
                }
            }

            var currentNode = root;

            foreach (var c in word)
            {
                var childNode = currentNode.GetChildNode(c);
                if (childNode != null)
                {
                    currentNode = childNode;
                }
                else
                {
                    var newNode = new TrieNode<TValue>(c)
                    {
                        Reference = value
                    };
                    currentNode.AddChild(c, newNode);
                    currentNode = newNode;
                }
            }

            currentNode.IsEndOfWord = true;

            currentNode.Reference = value;

            if (add)
            {
                Count++;
            }
        }

        public bool Remove(KeyValuePair<string, TValue> item)
        {
            return Remove(item.Key);
        }

        public void Append(KeyValuePair<string, TValue> value)
        {
            Add(value.Key, value.Value);
        }

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public TValue this[string key]
        {
            get
            {
                var value = ValueByRef(key, out var found);

                if (found)
                {
                    return value;
                }
                
                throw new KeyNotFoundException($"Key '{key}' was not found.");
            }
            set
            {
                var set = false;
                Insert(key, ref value, ref set);
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, TValue>.Keys
        {
            get
            {
                foreach (var (key, _) in GetKeyValues())
                {
                    yield return key;
                }
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<string, TValue>.Values
        {
            get
            {
                foreach (var (_, value) in GetKeyValues())
                {
                    yield return value;
                }
            }
        }

        ICollection<string> IDictionary<string, TValue>.Keys
        {
            get
            {
                var data = new Data<string>(Count);
                
                foreach (var (key, _) in GetKeyValues())
                {
                    data.Add(key);
                }

                return data;
            }
        }

        ICollection<TValue> IDictionary<string, TValue>.Values
        {
            get
            {
                var data = new Data<TValue>(Count);
                
                foreach (var (_, value) in GetKeyValues())
                {
                    data.Add(value);
                }

                return data;
            }
        }

        public IEnumerable<(string Key, TValue Reference)> GetKeyValues()
        {
            var stack = new Data<(TrieNode<TValue> Node, string Prefix)>();
            stack.Push((root, string.Empty));
         
            while (stack.Count > 0)
            {
                var (node, prefix) = stack.Pop();

                if (node.IsEndOfWord)
                {
                    yield return (prefix, node.Reference);
                }

                foreach (var child in node.Children)
                {
                    stack.Push((child.Value, prefix + child.Value.Value));
                }
            }
        }

        public void Add(KeyValuePair<string, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            var stack = new Data<TrieNode<TValue>>();
            stack.Push(root);
         
            while (stack.Count > 0)
            {
                var node = stack.Pop();

                foreach (var child in node.Children)
                {
                    stack.Push(child.Value);
                }
                
                node.Children.Clear();
            }
            
            root = new('\0');
            
            Count = 0;
        }

        public bool Contains(KeyValuePair<string, TValue> item)
        {
            if (TryGetValue(item.Key, out var value))
            {
                return EqualityComparer<TValue>.Default.Equals(value ,item.Value);
            }

            return false;
        }

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
                throw new ArgumentException("The number of elements in the source collection is greater than the available space from arrayIndex to the end of the destination array.");
            }

            foreach (var pair in this)
            {
                array[arrayIndex++] = pair;
            }
        }

        public Data<KeyValuePair<string, TValue>> ToData()
        {
            var data = new Data<KeyValuePair<string, TValue>>(Count);

            var keyValues = GetKeyValues();

            foreach (var kv in keyValues)
            {
                data.Add(new KeyValuePair<string, TValue>(kv.Key, kv.Reference));
            }

            return data;
        }

        public bool Remove(string key)
        {
            var word = key;
            
            if (m_caseSensitive == false)
            {
                word = word.ToLower();
            }
            var currentNode = root;
            foreach (var c in word)
            {
                currentNode = currentNode.GetChildNode(c);
                if (currentNode == null)
                {
                    return false;
                }
            }

            if (currentNode.IsEndOfWord)
            {
                currentNode.Reference = default;
                currentNode.IsEndOfWord = false;
                Count--;

                return true;
            }

            return false;
        }

        public void Dispose()
        {
            var q = new Data<TrieNode<TValue>>().AsQueue();
            
            q.Enqueue(root);

            while (q.Any)
            {
                var node = q.Dequeue();

                foreach (var child in node.Children)
                {
                    q.Enqueue(child.Value);
                }

                node.Dispose();
            }
            
            Clear();
        }

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            var stack = new Stack<(TrieNode<TValue> Node, string Prefix)>();
            stack.Push((root, string.Empty));

            while (stack.Count > 0)
            {
                var (node, prefix) = stack.Pop();

                if (node.IsEndOfWord)
                {
                    yield return new KeyValuePair<string, TValue>(prefix, node.Reference);
                }

                foreach (var child in node.Children)
                {
                    stack.Push((child.Value, prefix + child.Key));
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
          
            info.AddValue("Count", Count);
            info.AddValue("Items", ToData());
        }

        public void OnDeserialization(object sender)
        {
        }
    }