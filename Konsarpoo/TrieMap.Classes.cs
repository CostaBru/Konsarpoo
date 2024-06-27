using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Konsarpoo.Collections;

public partial class TrieMap<TKey, TValue> 
{
    [DebuggerDisplay("'{KeyChar}' {ChildrenCount} False")]
    internal class TrieLinkNode<TValue> : IEnumerable<KeyValuePair<object, TrieLinkNode<TValue>>>
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

                return ((Map<object, TrieLinkNode<TValue>>)Children).Count;
            }
        }

        public bool Any => Children != null;

        protected object Children;
        
        public object KeyChar;

        public TrieLinkNode(object keyChar)
        {
            KeyChar = keyChar;
        }

        public TrieLinkNode<TValue> GetChildNode(object c)
        {
            if (Children == null)
            {
                return null;
            }

            if (Children is TrieLinkNode<TValue> singleNode)
            {
                if (singleNode == null)
                {
                    return null;
                }
                
                if (EqualityComparer<object>.Default.Equals(c, singleNode.KeyChar))
                {
                    return singleNode;
                }

                return null;
            }

            return ((Map<object, TrieLinkNode<TValue>>)Children).GetOrDefault(c);
        }

        public void AddChild(TrieLinkNode<TValue> newNode)
        {
            if (Children == null)
            {
                Children = newNode;
                return;
            }
            
            var map = Children as Map<object, TrieLinkNode<TValue>>;
            
            if(map != null)
            {
                map[newNode.KeyChar] = newNode;
                return;
            }
            
            var singleNode = (TrieLinkNode<TValue>)Children;

            Children = new Map<object, TrieLinkNode<TValue>>
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

        public IEnumerator<KeyValuePair<object, TrieLinkNode<TValue>>> GetEnumerator()
        {
            var singleNode = Children as TrieLinkNode<TValue>;
            
            if (singleNode == null)
            {
                if (Children == null)
                {
                    yield break;
                }

                var map = (Map<object, TrieLinkNode<TValue>>)Children;
                
                foreach (var child in map)
                {
                    yield return child;
                }
            }
            else
            {
                yield return new KeyValuePair<object, TrieLinkNode<TValue>>(singleNode.KeyChar, singleNode);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void MakeChildEndOfWord(TrieLinkNode<TValue> currentNode, TValue value)
        {
            if (Children is TrieLinkNode<TValue>)
            {
                Children = new TrieEndLinkNode<TValue>(currentNode.KeyChar) {Children = currentNode.Children, Value = value};
                return;
            }
            
            var map = (Map<object, TrieLinkNode<TValue>>)Children;
            map[currentNode.KeyChar] = new TrieEndLinkNode<TValue>(currentNode.KeyChar) {Children = currentNode.Children, Value = value};
        }

        public void MakeChildLinkNode(TrieEndLinkNode<TValue> currentNode)
        {
            if (Children is TrieLinkNode<TValue>)
            {
                Children = new TrieLinkNode<TValue>(currentNode.KeyChar) {Children = currentNode.Children};
                return;
            }
            
            var map = (Map<object, TrieLinkNode< TValue>>)Children;
            map[currentNode.KeyChar] = new TrieLinkNode<TValue>(currentNode.KeyChar) {Children = currentNode.Children};
        }
        
        public TrieLinkNode<TValue> SplitTailNode(TrieTailNode< TValue> currentNode)
        {
            TrieLinkNode<TValue> newLinkNode;

            if (currentNode.Suffix != null && currentNode.Suffix.Count > 0)
            {
                newLinkNode = new TrieLinkNode<TValue>(currentNode.KeyChar);
              
                var newEndNode = new TrieTailNode<TValue>(currentNode.Suffix[0]) {Value = currentNode.Value};
                var suffixCount = currentNode.Suffix.Count;
                for (int i = 1; i < suffixCount; i++)
                {
                    newEndNode.AddSuffixChar(currentNode.Suffix[i]);
                }
                newLinkNode.AddChild(newEndNode);
            }
            else
            {
                newLinkNode = new TrieEndLinkNode<TValue>(currentNode.KeyChar) {Value = currentNode.Value};
            }

            if (Children is TrieLinkNode<TValue>)
            {
                Children = newLinkNode;
                return newLinkNode;
            }
            
            var map = (Map<object, TrieLinkNode<TValue>>)Children;
            map[currentNode.KeyChar] = newLinkNode;
            return newLinkNode;
        }

        public void RemoveNode(TrieLinkNode<TValue> node)
        {
            if (Children is TrieLinkNode<TValue>)
            {
                Children = null;
                return;
            }
            
            var map = (Map<object, TrieLinkNode<TValue>>)Children;
            map.Remove(node.KeyChar);
        }

        public virtual TKey BuildString<TKey>(TKey prefix, Func<TKey, object, TKey> compose)
        {
            return compose(prefix, KeyChar);
        }
    }

    [DebuggerDisplay("'{KeyChar}' {ChildrenCount} True")]
    internal class TrieEndLinkNode<TValue> : TrieLinkNode<TValue>
    {
        public TValue Value;

        public TrieEndLinkNode(object keyChar) : base(keyChar)
        {
        }
    }

    [DebuggerDisplay("'{KeyChar}' {ChildrenCount} True")]
    internal class TrieTailNode<TValue> : TrieEndLinkNode<TValue>
    {
        public Data<object> Suffix;

        public TrieTailNode(object keyChar) : base(keyChar)
        {
        }

        public void AddSuffixChar(object c)
        {
            if (Suffix == null)
            {
                Suffix = new Data<object>();
            }

            Suffix.Add(c);
        }

        public bool SuffixMatchAtPos(object c, int position)
        {
            if (Suffix == null || Suffix.Count <= position)
            {
                return false;
            }

            return EqualityComparer<object>.Default.Equals(Suffix[position], c);
        }
        
        public override TKey BuildString<TKey>(TKey prefix, Func<TKey, object, TKey> compose)
        {
            if (Suffix != null)
            {
                var builder = compose(prefix, KeyChar);
                
                foreach (var c in Suffix)
                {
                    builder = compose(builder, c);
                }
                
                return builder;
            }

            return compose(prefix, KeyChar);
        }
    }
}