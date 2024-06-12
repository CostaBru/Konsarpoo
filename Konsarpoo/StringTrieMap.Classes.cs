using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Konsarpoo.Collections;

public partial class StringTrieMap<TValue>
{
    [DebuggerDisplay("'{KeyChar}' {ChildrenCount} {IsEndOfWord}")]
    internal class TrieNode<TValue> : IEnumerable<KeyValuePair<char, TrieNode<TValue>>>
    {
        protected int ChildrenCount
        {
            get
            {
                if (Children is null)
                {
                    return 0;
                }
                
                if(Children is TrieNode<TValue> sn)
                {
                    return 1;
                }
                
                return ((Map<char, TrieNode<TValue>>)Children).Count;
            }
        }

        protected object Children;
        
        public char KeyChar;
        public bool IsEndOfWord;
        public TValue Value;

        public TrieNode(char keyChar)
        {
            KeyChar = keyChar;
        }

        public TrieNode<TValue> GetChildNode(char c)
        {
            if (Children == null)
            {
                return null;
            }

            if (Children is TrieNode<TValue> singleNode)
            {
                if (c == singleNode?.KeyChar)
                {
                    return singleNode;
                }

                return null;
            }

            return ((Map<char, TrieNode<TValue>>)Children).GetOrDefault(c);
        }

        public void AddChild(char c, TrieNode<TValue> newNode)
        {
            if (Children == null)
            {
                Children = newNode;
                return;
            }
            
            var map = Children as Map<char, TrieNode<TValue>>;
            
            if(map != null)
            {
                map[c] = newNode;
                return;
            }
            
            var singleNode = (TrieNode<TValue>)Children;

            Children = new Map<char, TrieNode<TValue>>
            {
                { singleNode.KeyChar, singleNode },
                { c, newNode }
            };
        }

        public void Dispose()
        {
            (Children as IDisposable)?.Dispose();
            Children = null;
        }

        public IEnumerator<KeyValuePair<char, TrieNode<TValue>>> GetEnumerator()
        {
            var singleNode = Children as TrieNode<TValue>;
            
            if (singleNode == null)
            {
                if (Children == null)
                {
                    yield break;
                }

                var map = (Map<char, TrieNode<TValue>>)Children;
                
                foreach (var child in map)
                {
                    yield return child;
                }
            }
            else
            {
                yield return new KeyValuePair<char, TrieNode<TValue>>(singleNode.KeyChar, singleNode);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}