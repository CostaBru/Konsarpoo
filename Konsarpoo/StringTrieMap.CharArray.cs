using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

public partial class StringTrieMap<TValue> : IDictionary<IEnumerable<char>, TValue>
{
    bool IDictionary<IEnumerable<char>, TValue>.ContainsKey([NotNull] IEnumerable<char> key) => TryGetValueCore(key, out var _);

    bool IDictionary<IEnumerable<char>, TValue>.TryGetValue(IEnumerable<char> key, out TValue value) => TryGetValueCore(key, out value);

    private bool TryGetValueCore(IEnumerable<char> key, out TValue value)
    {
        value = ValueByRef(key, out var success);

        return success;
    }

    TValue IDictionary<IEnumerable<char>, TValue>.this[IEnumerable<char> key]
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
                var newValue = m_missingValueFactory(new string(key.ToArray()));
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

    ICollection<IEnumerable<char>> IDictionary<IEnumerable<char>, TValue>.Keys
    {
        get
        {
            var data = new Data<IEnumerable<char>>(Count);

            foreach (var key in GetKeys())
            {
                data.Add(key);
            }

            return data;
        }
    }

    ICollection<TValue> IDictionary<IEnumerable<char>, TValue>.Values
    {
        get
        {
            var data = new Data<TValue>(Count);

            foreach (var value in Values)
            {
                data.Add(value);
            }

            return data;
        }
    }

    public ref TValue ValueByRef(IEnumerable<char> key, out bool success)
    {
        var word = key;
        success = false;

        var currentNode = m_root;

        if (m_caseSensitive == false)
        {
            foreach (var c in word)
            {
                currentNode = currentNode.GetChildNode(char.ToLower(c));
                if (currentNode == null)
                {
                    return ref s_nullRef;
                }
            }
        }
        else
        {
            foreach (var c in word)
            {
                currentNode = currentNode.GetChildNode(c);
                if (currentNode == null)
                {
                    return ref s_nullRef;
                }
            }
        }

        if (currentNode.IsEndOfWord)
        {
            success = true;

            return ref currentNode.Value;
        }

        return ref s_nullRef;
    }

    void IDictionary<IEnumerable<char>, TValue>.Add(IEnumerable<char> key, TValue value)
    {
        var add = true;
        Insert(key, ref value, ref add);
    }

    private void Insert(IEnumerable<char> key, ref TValue value, ref bool add)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (TryGetValueCore(key, out var _))
        {
            if (add)
            {
                throw new ArgumentException($"Key '{key}' is already exists.");
            }
        }

        var currentNode = m_root;

        if (m_caseSensitive == false)
        {
            foreach (var c in key)
            {
                var lower = char.ToLower(c);

                var childNode = currentNode.GetChildNode(lower);
                if (childNode != null)
                {
                    currentNode = childNode;
                }
                else
                {
                    var newNode = new TrieNode<TValue>(lower)
                    {
                        Value = value
                    };
                    currentNode.AddChild(lower, newNode);
                    currentNode = newNode;
                }
            }
        }
        else
        {
            foreach (var c in key)
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
                        Value = value
                    };
                    currentNode.AddChild(c, newNode);
                    currentNode = newNode;
                }
            }
        }

        if (add || currentNode.IsEndOfWord == false)
        {
            m_count++;
            unchecked { ++m_version; }
        }

        currentNode.IsEndOfWord = true;
        currentNode.Value = value;
    }


    private IEnumerable<(IEnumerable<char> Key, TValue Value)> GetKeyValues()
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
    
    private IEnumerable<TValue> GetValues()
    {
        var version = m_version;

        var stack = new Data<TrieNode<TValue>>();
        stack.Push(m_root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            
            CheckState(ref version);

            if (node.IsEndOfWord)
            {
                yield return node.Value;
            }

            foreach (var child in node)
            {
                stack.Push(child.Value);
            }
        }
        
        stack.Dispose();
    }

    bool IDictionary<IEnumerable<char>, TValue>.Remove(IEnumerable<char> key) => RemoveCore(key);

    private bool RemoveCore(IEnumerable<char> key)
    {
        var word = key;

        var currentNode = m_root;

        if (m_caseSensitive == false)
        {
            foreach (var c in word)
            {
                currentNode = currentNode.GetChildNode(char.ToLower(c));
                if (currentNode == null)
                {
                    return false;
                }
            }
        }
        else
        {
            foreach (var c in word)
            {
                currentNode = currentNode.GetChildNode(c);
                if (currentNode == null)
                {
                    return false;
                }
            }
        }

        if (currentNode.IsEndOfWord)
        {
            currentNode.Value = default;
            currentNode.IsEndOfWord = false;
            m_count--;
            unchecked
            {
                ++m_version;
            }

            return true;
        }

        return false;
    }

    IEnumerator<KeyValuePair<IEnumerable<char>, TValue>> IEnumerable<KeyValuePair<IEnumerable<char>, TValue>>.GetEnumerator()
    {
        var keyValues = GetKeyValues();

        foreach (var tuple in keyValues)
        {
            yield return new KeyValuePair<IEnumerable<char>, TValue>(tuple.Key, tuple.Value);
        }
    }

    void ICollection<KeyValuePair<IEnumerable<char>, TValue>>.Add(KeyValuePair<IEnumerable<char>, TValue> item)
    {
        var itemValue = item.Value;
        var add = true;
        
        Insert(item.Key, ref itemValue, ref add);
    }

    bool ICollection<KeyValuePair<IEnumerable<char>, TValue>>.Contains(KeyValuePair<IEnumerable<char>, TValue> item)
    {
        return TryGetValueCore(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
    }

    void ICollection<KeyValuePair<IEnumerable<char>, TValue>>.CopyTo(KeyValuePair<IEnumerable<char>, TValue>[] destination, int index)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }
        if ((index < 0) || (index > destination.Length))
        {
            throw new ArgumentOutOfRangeException("index");
        }
        if ((destination.Length - index) < Count)
        {
            throw new ArgumentException();
        }
            
        var entries = GetKeyValues();

        foreach (var entry in entries)
        {
            destination[index++] = new KeyValuePair<IEnumerable<char>, TValue>(entry.Key, entry.Value);
        }
    }

    bool ICollection<KeyValuePair<IEnumerable<char>, TValue>>.Remove(KeyValuePair<IEnumerable<char>, TValue> item) => RemoveCore(item.Key);
}