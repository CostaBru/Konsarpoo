using System;
using System.Collections.Generic;

namespace Konsarpoo.Collections;

public partial class AbstractTupleTrieMap<TKey, TValue> 
{
    private bool TryGetValueCore(TKey key, out TValue value)
    {
        value = ValueByRef(key, out var success);

        return success;
    }
   
    public ref TValue ValueByRef(TKey key, out bool success)
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

    private void Insert(TKey key, ref TValue value, ref bool add)
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

    private bool RemoveCore(TKey key)
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
}