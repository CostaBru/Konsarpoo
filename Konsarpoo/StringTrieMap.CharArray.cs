using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

public partial class StringTrieMap<TValue> 
{
    public bool ContainsKey([NotNull] IEnumerable<char> key) => TryGetValue(key, out var _);

    public bool TryGetValue(IEnumerable<char> key, out TValue value)
    {
        value = ValueByRef(key, out var success);

        return success;
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

    public void Add(IEnumerable<char> key, TValue value)
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

        if (TryGetValue(key, out var _))
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
            Count++;
            unchecked { ++m_version; }
        }

        currentNode.IsEndOfWord = true;
        currentNode.Value = value;
    }


    private IEnumerable<(IEnumerable<char> Key, TValue Reference)> GetKeyValues()
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

            foreach (var child in node.Children)
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

            foreach (var child in node.Children)
            {
                stack.Push(child.Value);
            }
        }
        
        stack.Dispose();
    }

    public bool Remove(IEnumerable<char> key)
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
            Count--;
            unchecked { ++m_version; }
            return true;
        }

        return false;
    }
}