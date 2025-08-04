using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

internal partial class StringTrieMap<TValue> : IDictionary<IEnumerable<char>, TValue>
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
                var strKey = key as string ?? new string(key.ToArray());
                var newValue = m_missingValueFactory(strKey);
                var add = false;
                Insert(strKey, ref newValue, ref add);
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
        success = false;
        var currentNode = m_root;

        TrieTailNode<TValue> tailNode = null;
        int tailNodeSuffixCursor = 0;

        foreach (var c in key)
        {
            var charVal = m_prepareCharFunc(c);
            if (tailNode != null)
            {
                if (tailNode.SuffixMatchAtPos(charVal, tailNodeSuffixCursor) == false)
                {
                    return ref s_nullRef;
                }
                tailNodeSuffixCursor++;
                continue;
            }

            currentNode = currentNode.GetChildNode(charVal);
            if (currentNode is TrieTailNode<TValue> tn)
            {
                tailNode = tn;
                continue;
            }

            if (currentNode == null)
            {
                return ref s_nullRef;
            }
        }

        if (tailNode != null)
        {
            if (tailNodeSuffixCursor == (tailNode.Suffix?.Count ?? 0))
            {
                success = true;
                return ref tailNode.Value;
            }
            return ref s_nullRef;
        }

        if (currentNode is TrieEndLinkNode<TValue> en)
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

        var currentNode = m_root;
        var currentNodeParent = m_root;
        TrieTailNode<TValue> tailNode = null;

        var valueAdded = false;
        foreach (var c in key)
        {
            var charVal = m_prepareCharFunc(c);
            if (tailNode != null)
            {
                tailNode.AddSuffixChar(charVal);
                continue;
            }

            var childNode = currentNode.GetChildNode(charVal);
            if (childNode == null)
            {
                var newNode = new TrieTailNode<TValue>(charVal);

                currentNode.AddChild(newNode);
                currentNodeParent = currentNode;
                currentNode = newNode;

                tailNode = newNode;
                valueAdded = true;
            }
            else
            {
                if (childNode is TrieTailNode<TValue> tn)
                {
                    childNode = currentNode.SplitTailNode(tn);
                }

                currentNodeParent = currentNode;
                currentNode = childNode;
            }
        }

        if (currentNode is TrieEndLinkNode<TValue> en)
        {
            if (add && valueAdded == false)
            {
                throw new ArgumentException($"Key '{key}' already exists.");
            }

            en.Value = value;
        }
        else 
        {
            valueAdded = true;
            currentNodeParent.MakeChildEndOfWord(currentNode, value);
        }
        
        if (valueAdded)
        {
            m_count++;
            unchecked { ++m_version; }
        }
    }

    private IEnumerable<(IEnumerable<char> Key, TValue Value)> GetKeyValues()
    {
        var version = m_version;

        var data = new Data<(TrieLinkNode<TValue> Node, string Prefix)>();
        
        var queue = data.AsQueue();
        queue.Enqueue((m_root, string.Empty));

        while (queue.Any)
        {
            var (node, prefix) = queue.Dequeue();
            
            CheckState(ref version);

            if (node is TrieEndLinkNode<TValue> en)
            {
                yield return (prefix, en.Value);
            }

            foreach (var child in node)
            {
                queue.Enqueue((child.Value, child.Value.BuildString(prefix)));
            }
        }
        
        data.Dispose();
    }
    
    private IEnumerable<TValue> GetValues()
    {
        var version = m_version;
        var data = new Data<TrieLinkNode<TValue>>();
        
        var queue = data.AsQueue();
        queue.Enqueue(m_root);

        while (queue.Any)
        {
            var node = queue.Dequeue();
            
            CheckState(ref version);

            if (node is TrieEndLinkNode<TValue> en)
            {
                yield return en.Value;
            }

            foreach (var child in node)
            {
                queue.Enqueue(child.Value);
            }
        }
        
        data.Dispose();
    }

    bool IDictionary<IEnumerable<char>, TValue>.Remove(IEnumerable<char> key) => RemoveCore(key);

    private bool RemoveCore(IEnumerable<char> key)
    {
        var currentNode = m_root;
        var treeStructure = new Data<TrieLinkNode<TValue>>();

        TrieTailNode<TValue> tailNode = null;
        int tailNodeSuffixCursor = 0;

        foreach (var c in key)
        {
            var charVal = m_prepareCharFunc(c);
            if (tailNode != null)
            {
                if (tailNode.SuffixMatchAtPos(charVal, tailNodeSuffixCursor) == false)
                {
                    treeStructure.Dispose();
                    return false;
                }
                tailNodeSuffixCursor++;
                continue;
            }

            treeStructure.Push(currentNode);

            currentNode = currentNode.GetChildNode(charVal);;
            if (currentNode is TrieTailNode<TValue> tn)
            {
                tailNode = tn;
                continue;
            }
            if (currentNode == null)
            {
                treeStructure.Dispose();
                return false;
            }
        }

        if (currentNode is TrieEndLinkNode<TValue> en)
        {
            var currentNodeParent = treeStructure.Pop();
            if (currentNode.Any)
            {
                currentNodeParent.MakeChildLinkNode(en);
            }
            else
            {
                currentNodeParent.RemoveNode(currentNode);
                currentNode.Dispose();

                var node = currentNodeParent;
                while ((node is not TrieEndLinkNode<TValue>) && node.Any == false && treeStructure.Any)
                {
                    var parent = treeStructure.Pop();

                    parent.RemoveNode(node);
                    
                    node.Dispose();
                    node = parent;
                }
            }

            m_count--;
            unchecked { ++m_version; }
            return true;
        }
        return false;
    }

    IEnumerator<KeyValuePair<IEnumerable<char>, TValue>> IEnumerable<KeyValuePair<IEnumerable<char>, TValue>>.GetEnumerator()
    {
        foreach (var tuple in GetKeyValues())
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