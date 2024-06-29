using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

/// <summary>
/// Interface to access TupleTrie as a map with object[] as key.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public interface IObjectKeyTupleTrieMap<TValue>
{
    void Add([NotNull] object[] key, TValue value);
    bool ContainsKey([NotNull] object[] key);
    bool TryGetValue([NotNull] object[] key, out TValue value);
    TValue this[[NotNull] object[] key] { get; set; }
    ref TValue ValueByRef([NotNull] object[] key, out bool success);
    bool Remove([NotNull] object[] key);
    IEnumerable<(object[] Key, TValue Value)> GetObjKeyValues();
    IEnumerable<TValue> WhereKeyStartsWith([NotNull] object[] key, int count);
    int Count { get; }
}

public partial class AbstractTupleTrieMap<TKey, TValue> : IObjectKeyTupleTrieMap<TValue>
{
    void IObjectKeyTupleTrieMap<TValue>.Add([NotNull] object[] key, TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        var add = true;
        Insert(key, ref value, ref add);
    }

    bool IObjectKeyTupleTrieMap<TValue>.ContainsKey([NotNull] object[] key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
       
        var _ = ValueByRefCore(key, out var success);

        return success;
    }

    bool IObjectKeyTupleTrieMap<TValue>.TryGetValue(object[] key, out TValue value)
    {
        value = ValueByRefCore(key, out var success);

        return success;
    }
    
    //exact copy of this[TKey key]
    TValue IObjectKeyTupleTrieMap<TValue>.this[[NotNull] object[] key]
    {
        get
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var value = ValueByRefCore(key, out var found);

            if (found)
            {
                return value;
            }

            if (m_missingValueFactory != null)
            {
                var newValue = m_missingValueFactory(CreateKey(key));
                var add = false;
                Insert(key, ref newValue, ref add);
                return newValue;
            }

            throw new KeyNotFoundException($"Key '{key}' was not found.");
        }
        set
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var add = false;
            Insert(key, ref value, ref add);
        }
    }
   
    ref TValue IObjectKeyTupleTrieMap<TValue>.ValueByRef(object[] key, out bool success)
    {
        return ref ValueByRefCore(key, out success);
    }

    //exact copy of ValueByRef(TKey key, out bool success)
    private ref TValue ValueByRefCore(object[] key, out bool success)
    {
        success = false;
        var currentNode = m_root;

        TrieTailNode<TValue> tailNode = null;
        int tailNodeSuffixCursor = 0;

        for (var index = 0; index < key.Length; index++)
        {
            var c = key[index];
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

    //exact copy of Insert(TKey key, ref TValue value, ref bool add)
    private void Insert(object[] key, ref TValue value, ref bool add)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var currentNode = m_root;
        var currentNodeParent = m_root;
        TrieTailNode<TValue> tailNode = null;

        var valueAdded = false;
        for (var index = 0; index < key.Length; index++)
        {
            var c = key[index];
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

    bool IObjectKeyTupleTrieMap<TValue>.Remove([NotNull] object[] key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        return RemoveCore(key);
    }
    
    //exact copy of RemoveCore(TKey)
    private bool RemoveCore(object[] key)
    {
        var currentNode = m_root;
        var treeStructure = new Data<TrieLinkNode<TValue>>();

        TrieTailNode<TValue> tailNode = null;
        int tailNodeSuffixCursor = 0;

        for (var index = 0; index < key.Length; index++)
        {
            var c = key[index];
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

            currentNode = currentNode.GetChildNode(c);
            ;
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
    
    //exact copy of GetKeyValues() but made public
    IEnumerable<(object[] Key, TValue Value)> IObjectKeyTupleTrieMap<TValue>.GetObjKeyValues()
    {
        var version = m_version;

        var data = new Data<(TrieLinkNode<TValue> Node, object[] Prefix)>();

        var queue = data.AsQueue();
        queue.Enqueue((m_root, Array.Empty<object>()));

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

    /// <summary>
    /// Returns values where key starts with a given key part.
    /// </summary>
    /// <param name="key">key storage</param>
    /// <param name="count">key parts count to use</param>
    /// <returns></returns>
    //exact copy of WhereKeyStartsWith(TKey key, int count)
    IEnumerable<TValue> IObjectKeyTupleTrieMap<TValue>.WhereKeyStartsWith(object[] key, int count)
    {
        if (count > key.Length)
        {
            throw new ArgumentException($"given count param cannot be greater than key length. ({count} > {key.Length})");
        }
        
        var version = m_version;

        var node = m_root;
        int suffixPos = 0;

        for (int i = 0; i < count && i < key.Length; i++)
        {
            CheckState(ref version);
            
            var keyPart = key[i];

            //all other parts stored as suffix
            if (node is TrieTailNode<TValue> tn)
            {
                if (tn.SuffixMatchAtPos(keyPart, suffixPos))
                {
                    suffixPos++;
                }
                else
                {
                    yield break;
                }
            }
            else
            {
                var childNode = node.GetChildNode(keyPart);

                if (childNode == null)
                {
                    yield break;
                }

                node = childNode;
            }
        }
        
        var data = new Data<TrieLinkNode<TValue>>();
        
        var stack = data.AsQueue();
        stack.Enqueue(node);

        while (stack.Any)
        {
            var cn = stack.Dequeue();
            
            CheckState(ref version);
            
            if (cn is TrieTailNode<TValue> tn)
            {
                yield return tn.Value;
            }
            else if (cn is TrieEndLinkNode<TValue> en)
            {
                yield return en.Value;
            }
            else
            {
                foreach (var child in cn)
                {
                    stack.Enqueue(child.Value);
                }
            }
        }
    }
}