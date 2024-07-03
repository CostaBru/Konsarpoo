using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

public partial class AbstractTupleTrieMap<TKey, TValue> 
{
    [DebuggerDisplay("'{Key}' {ChildrenCount} False")]
    public class TrieLinkNode<TValue> : IEnumerable<KeyValuePair<object, TrieLinkNode<TValue>>>
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

                return ((IDictionary<object, TrieLinkNode<TValue>>)Children).Count;
            }
        }

        public bool Any => Children != null;

        protected object Children;
        
        public object Key;

        public TrieLinkNode(object key)
        {
            Key = key;
        }

        public TrieLinkNode<TValue> GetChildNode(object c)
        {
            if (Children == null)
            {
                return null;
            }

            if (Children is TrieLinkNode<TValue> singleNode)
            {
                if (EqualityComparer<object>.Default.Equals(c, singleNode.Key))
                {
                    return singleNode;
                }

                return null;
            }

            var dictionary = (IDictionary<object, TrieLinkNode<TValue>>)Children;

            if (dictionary.TryGetValue(c, out var val))
            {
                return val;
            }
            
            return null;
        }

        public void AddChild(TrieLinkNode<TValue> newNode, [CanBeNull] Func<Type, IDictionary<object,TrieLinkNode<TValue>>> mapFactory)
        {
            if (Children == null)
            {
                Children = newNode;
                return;
            }
            
            var map = Children as IDictionary<object, TrieLinkNode<TValue>>;
            
            if(map != null)
            {
                map[newNode.Key] = newNode;
                return;
            }
            
            var singleNode = (TrieLinkNode<TValue>)Children;

            IDictionary<object, TrieLinkNode<TValue>> children = null;

            if (mapFactory != null)
            {
                children = mapFactory.Invoke(newNode.Key.GetType());
            }
            
            if (children == null)
            {
                children = new Map<object, TrieLinkNode<TValue>>();
            }

            children.Add(singleNode.Key, singleNode);
            children.Add(newNode.Key, newNode);

            Children = children;
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

                var map = (IDictionary<object, TrieLinkNode<TValue>>)Children;
                
                foreach (var child in map)
                {
                    yield return child;
                }
            }
            else
            {
                yield return new KeyValuePair<object, TrieLinkNode<TValue>>(singleNode.Key, singleNode);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void MakeChildEndOfWord(TrieLinkNode<TValue> currentNode, TValue value)
        {
            if (Children is TrieLinkNode<TValue>)
            {
                Children = new TrieEndLinkNode<TValue>(currentNode.Key) {Children = currentNode.Children, Value = value};
                return;
            }
            
            var map = (IDictionary<object, TrieLinkNode<TValue>>)Children;
            map[currentNode.Key] = new TrieEndLinkNode<TValue>(currentNode.Key) {Children = currentNode.Children, Value = value};
        }

        public void MakeChildLinkNode(TrieEndLinkNode<TValue> currentNode)
        {
            if (Children is TrieLinkNode<TValue>)
            {
                Children = new TrieLinkNode<TValue>(currentNode.Key) {Children = currentNode.Children};
                return;
            }
            
            var map = (IDictionary<object, TrieLinkNode<TValue>>)Children;
            map[currentNode.Key] = new TrieLinkNode<TValue>(currentNode.Key) {Children = currentNode.Children};
        }
        
        public TrieLinkNode<TValue> SplitTailNode(TrieTailNode< TValue> currentNode,  [CanBeNull] Func<Type, IDictionary<object,TrieLinkNode<TValue>>> mapFactory)
        {
            TrieLinkNode<TValue> newLinkNode;

            if (currentNode.Suffix != null && currentNode.Suffix.Count > 0)
            {
                newLinkNode = new TrieLinkNode<TValue>(currentNode.Key);
              
                var newEndNode = new TrieTailNode<TValue>(currentNode.Suffix[0]) {Value = currentNode.Value};
                var suffixCount = currentNode.Suffix.Count;
                for (int i = 1; i < suffixCount; i++)
                {
                    newEndNode.AddSuffixChar(currentNode.Suffix[i]);
                }
                newLinkNode.AddChild(newEndNode, mapFactory);
            }
            else
            {
                newLinkNode = new TrieEndLinkNode<TValue>(currentNode.Key) {Value = currentNode.Value};
            }

            if (Children is TrieLinkNode<TValue>)
            {
                Children = newLinkNode;
                return newLinkNode;
            }
            
            var map = (IDictionary<object, TrieLinkNode<TValue>>)Children;
            map[currentNode.Key] = newLinkNode;
            return newLinkNode;
        }

        public void RemoveNode(TrieLinkNode<TValue> node)
        {
            if (Children is TrieLinkNode<TValue>)
            {
                Children = null;
                return;
            }
            
            var map = (IDictionary<object, TrieLinkNode<TValue>>)Children;
            map.Remove(node.Key);
        }

        public virtual TKey BuildString<TKey>(TKey prefix, int pos, Func<TKey, object, int, TKey> compose) 
        {
            return compose(prefix, Key, pos);
        }
        
        public virtual object[] BuildString(object[] prefix) 
        {
            var newArray = new object[prefix.Length + 1];
            Array.Copy(prefix, newArray, prefix.Length);
            newArray[prefix.Length] = Key;
            return newArray;
        }
    }

    [DebuggerDisplay("'{Key}' {ChildrenCount} True")]
    public class TrieEndLinkNode<TValue> : TrieLinkNode<TValue>
    {
        public TValue Value;

        public TrieEndLinkNode(object key) : base(key)
        {
        }
    }

    [DebuggerDisplay("'{Key}' {ChildrenCount} True")]
    public class TrieTailNode<TValue> : TrieEndLinkNode<TValue>
    {
        public Data<object> Suffix;

        public TrieTailNode(object key) : base(key)
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

        public override object[] BuildString(object[] prefix)
        {
            if (Suffix != null)
            {
                var suffixLength = Suffix.Length;
                
                var newArray = new object[prefix.Length + 1 + suffixLength];
                
                Array.Copy(prefix, newArray, prefix.Length);
                
                newArray[prefix.Length] = Key;

                for (int i = 0; i < suffixLength; i++)
                {
                    newArray[prefix.Length + i + 1] = Suffix[i];
                }
                
                return newArray;
            }

            return base.BuildString(prefix);
        }

        public override TKey BuildString<TKey>(TKey prefix, int pos, Func<TKey, object, int, TKey> compose)
        {
            if (Suffix != null)
            {
                var builder = compose(prefix, Key, pos);
                
                var i = pos + 1;

                foreach (var c in Suffix)
                {
                    builder = compose(builder, c, i);
                    i++;
                }
                
                return builder;
            }

            return compose(prefix, Key, pos);
        }
    }
}