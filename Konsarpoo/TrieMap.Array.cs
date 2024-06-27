using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

public partial class TrieMap<TKey, TValue>
{

    private bool TryGetValueCore(IEnumerable<object> key, out TValue value)
    {
        value = ValueByRef(key, out var success);

        return success;
    }

    public TValue this[IEnumerable<object> key]
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
                var newValue = m_missingValueFactory(m_compose(key));
                var add = false;
                Insert(key, ref newValue, ref add);
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

    ICollection<IEnumerable<object>> KeysObj
    {
        get
        {
            var data = new Data<IEnumerable<object>>(Count);

            foreach (var key in GetKeys())
            {
                data.Add(m_decompose(key));
            }

            return data;
        }
    }

    public ref TValue ValueByRef(IEnumerable<object> key, out bool success)
    {
        success = false;
        var currentNode = m_root;

        TrieTailNode<TValue> tailNode = null;
        int tailNodeSuffixCursor = 0;

        foreach (var c in key)
        {
            if (tailNode != null)
            {
                if (tailNode.SuffixMatchAtPos(c, tailNodeSuffixCursor) == false)
                {
                    return ref s_nullRef;
                }
                tailNodeSuffixCursor++;
                continue;
            }

            currentNode = currentNode.GetChildNode(c);
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

    private void Insert(IEnumerable<object> key, ref TValue value, ref bool add)
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
            if (tailNode != null)
            {
                tailNode.AddSuffixChar(c);
                continue;
            }

            var childNode = currentNode.GetChildNode(c);
            if (childNode == null)
            {
                var newNode = new TrieTailNode<TValue>(c);

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

    private IEnumerable<(IEnumerable<object> Key, TValue Value)> GetKeyValues()
    {
        var version = m_version;

        //todo optimize
        
        var data = new Data<(TrieLinkNode<TValue> Node, object[] Prefix)>();
        
        var queue = data.AsQueue();
        queue.Enqueue((m_root, null));

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
                queue.Enqueue((child.Value, prefix.Concat(new [] {child.Value.KeyChar}).ToArray()));
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

    private bool RemoveCore(IEnumerable<object> key)
    {
        var currentNode = m_root;
        var treeStructure = new Data<TrieLinkNode<TValue>>();

        TrieTailNode<TValue> tailNode = null;
        int tailNodeSuffixCursor = 0;

        foreach (var c in key)
        {
            if (tailNode != null)
            {
                if (tailNode.SuffixMatchAtPos(c, tailNodeSuffixCursor) == false)
                {
                    treeStructure.Dispose();
                    return false;
                }
                tailNodeSuffixCursor++;
                continue;
            }

            treeStructure.Push(currentNode);

            currentNode = currentNode.GetChildNode(c);;
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
}