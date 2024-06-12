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

        if (currentNode is TrieEndNode<TValue> en)
        {
            success = true;

            return ref en.Value;
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
        var currentNodeParent = m_root;
        var newNodeAdded = (TrieLinkNode<TValue>) null;

        if (m_caseSensitive == false)
        {
            foreach (var c in key)
            {
                var lower = char.ToLower(c);

                var childNode = currentNode.GetChildNode(lower);
                if (childNode == null)
                {
                    var newNode = new TrieLinkNode<TValue>(lower);
                    currentNode.AddChild(newNode);
                    currentNodeParent = currentNode;
                    currentNode = newNode;
                    newNodeAdded = newNode;
                }
                else
                {
                    currentNodeParent = currentNode;
                    currentNode = childNode;
                }
            }
        }
        else
        {
            foreach (var c in key)
            {
                var childNode = currentNode.GetChildNode(c);
                if (childNode == null)
                {
                    var newNode = new TrieLinkNode<TValue>(c);

                    currentNode.AddChild(newNode);
                    currentNodeParent = currentNode;
                    currentNode = newNode;
                    newNodeAdded = newNode;
                }
                else
                {
                    currentNodeParent = currentNode;
                    currentNode = childNode;
                }
            }
        }

        if (currentNode is TrieEndNode<TValue> en)
        {
            en.Value = value;
        }
        else
        {
            currentNodeParent.MakeChildEndOfWord(currentNode, value);

            if (add || ReferenceEquals(newNodeAdded, currentNode))
            {
                m_count++;
                unchecked
                {
                    ++m_version;
                }
            }
        }
    }

    private IEnumerable<(IEnumerable<char> Key, TValue Value)> GetKeyValues()
    {
        var version = m_version;
        
        var stack = new Data<(TrieLinkNode<TValue> Node, string Prefix)>();
        stack.Push((m_root, string.Empty));

        while (stack.Count > 0)
        {
            var (node, prefix) = stack.Pop();
            
            CheckState(ref version);

            if (node is TrieEndNode<TValue> en)
            {
                yield return (prefix, en.Value);
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

        var stack = new Data<TrieLinkNode<TValue>>();
        stack.Push(m_root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            
            CheckState(ref version);

            if (node is TrieEndNode<TValue> en)
            {
                yield return en.Value;
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
        var data = new Data<TrieLinkNode<TValue>>();
        var treeStructure = data.AsStack();

        if (m_caseSensitive == false)
        {
            foreach (var c in word)
            {
                var node = currentNode.GetChildNode(char.ToLower(c));

                treeStructure.Push(node);
                currentNode = node;
                
                if (currentNode == null)
                {
                    data.Dispose();
                    return false;
                }
            }
        }
        else
        {
            foreach (var c in word)
            {
                var node = currentNode.GetChildNode(c);
                
                treeStructure.Push(node);
                currentNode = node;
                
                if (currentNode == null)
                {
                    data.Dispose();
                    return false;
                }
            }
        }

        if (currentNode is TrieEndNode<TValue> en)
        {
            var currentNodeParent = treeStructure.Pop();
            currentNodeParent = treeStructure.Pop();

            if (currentNode.Any)
            {
                currentNodeParent.MakeChildLinkNode(en);
            }
            else
            {
                currentNodeParent.RemoveNode(currentNode);
                
                currentNode.Dispose();

                var node = currentNodeParent;

                while (node.Any == false)
                {
                    var parent = treeStructure.Pop();

                    parent.RemoveNode(node);
                    
                    node.Dispose();

                    node = parent;
                }
            }

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