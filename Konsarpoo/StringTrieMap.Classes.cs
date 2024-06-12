using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Konsarpoo.Collections;

public partial class StringTrieMap<TValue>
{
    [DebuggerDisplay("'{KeyChar}' {ChildrenCount} False")]
    internal class TrieLinkNode<TValue> : IEnumerable<KeyValuePair<char, TrieLinkNode<TValue>>>
    {
        protected int ChildrenCount
        {
            get
            {
                if (Children is null)
                {
                    return 0;
                }

                if (Children is TrieLinkNode<TValue> sn)
                {
                    return 1;
                }

                return ((Map<char, TrieLinkNode<TValue>>)Children).Count;
            }
        }

        public bool Any => Children != null;

        protected object Children;
        
        public char KeyChar;

        public TrieLinkNode(char keyChar)
        {
            KeyChar = keyChar;
        }

        public TrieLinkNode<TValue> GetChildNode(char c)
        {
            if (Children == null)
            {
                return null;
            }

            if (Children is TrieLinkNode<TValue> singleNode)
            {
                if (c == singleNode?.KeyChar)
                {
                    return singleNode;
                }

                return null;
            }

            return ((Map<char, TrieLinkNode<TValue>>)Children).GetOrDefault(c);
        }

        public void AddChild(TrieLinkNode<TValue> newNode)
        {
            if (Children == null)
            {
                Children = newNode;
                return;
            }
            
            var map = Children as Map<char, TrieLinkNode<TValue>>;
            
            if(map != null)
            {
                map[newNode.KeyChar] = newNode;
                return;
            }
            
            var singleNode = (TrieLinkNode<TValue>)Children;

            Children = new Map<char, TrieLinkNode<TValue>>
            {
                { singleNode.KeyChar, singleNode },
                { newNode.KeyChar, newNode }
            };
        }

        public void Dispose()
        {
            (Children as IDisposable)?.Dispose();
            Children = null;
        }

        public IEnumerator<KeyValuePair<char, TrieLinkNode<TValue>>> GetEnumerator()
        {
            var singleNode = Children as TrieLinkNode<TValue>;
            
            if (singleNode == null)
            {
                if (Children == null)
                {
                    yield break;
                }

                var map = (Map<char, TrieLinkNode<TValue>>)Children;
                
                foreach (var child in map)
                {
                    yield return child;
                }
            }
            else
            {
                yield return new KeyValuePair<char, TrieLinkNode<TValue>>(singleNode.KeyChar, singleNode);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void MakeChildEndOfWord(TrieLinkNode<TValue> currentNode, TValue value)
        {
            if (Children is TrieLinkNode<TValue>)
            {
                Children = new TrieEndNode<TValue>(currentNode.KeyChar) {Children = currentNode.Children, Value = value};
                return;
            }
            
            var map = (Map<char, TrieLinkNode<TValue>>)Children;
            map[currentNode.KeyChar] = new TrieEndNode<TValue>(currentNode.KeyChar) {Children = currentNode.Children, Value = value};
        }

        public void MakeChildLinkNode(TrieEndNode<TValue> currentNode)
        {
            if (Children is TrieLinkNode<TValue>)
            {
                Children = new TrieLinkNode<TValue>(currentNode.KeyChar) {Children = currentNode.Children};
                return;
            }
            
            var map = (Map<char, TrieLinkNode<TValue>>)Children;
            map[currentNode.KeyChar] = new TrieLinkNode<TValue>(currentNode.KeyChar) {Children = currentNode.Children};
        }

        public void RemoveNode(TrieLinkNode<TValue> node)
        {
            if (Children is TrieLinkNode<TValue>)
            {
                Children = null;
                return;
            }
            
            var map = (Map<char, TrieLinkNode<TValue>>)Children;
            map.Remove(node.KeyChar);
        }
    }

    [DebuggerDisplay("'{KeyChar}' {ChildrenCount} True")]
    internal class TrieEndNode<TValue> : TrieLinkNode<TValue>
    {
        public TValue Value;

        public TrieEndNode(char keyChar) : base(keyChar)
        {
        }
    }
}