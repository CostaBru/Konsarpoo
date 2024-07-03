using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

[DebuggerDisplay("Count = {Count}")]
public abstract partial class AbstractTupleTrieMap<TKey, TValue> :
    IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue>,
    IAppender<KeyValuePair<TKey, TValue>>,
    IDisposable
    where TKey : ITuple
{
    protected abstract TKey ConcatKeyWith(TKey key, object obj, int pos);
    protected abstract TKey CreateKey(object[] obj);

    [NonSerialized] private TrieLinkNode<TValue> m_root = new(null);

    [NonSerialized] private ushort m_version;

    [NonSerialized] [CanBeNull] private SerializationInfo m_sInfo;

    [NonSerialized] private static TValue s_nullRef;

    [NonSerialized] [CanBeNull] private Func<TKey, TValue> m_missingValueFactory;

    [NonSerialized] private int m_count;
    
    [NonSerialized] [CanBeNull] private Func<Type,IDictionary<object, TrieLinkNode<TValue>>> m_nodesMapFactory;

    /// <summary>
    /// Checks that both map and readonly dictionary has the same keys and values.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator ==([CanBeNull] AbstractTupleTrieMap<TKey, TValue> a,
        [CanBeNull] IReadOnlyDictionary<TKey, TValue> b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
        if (RuntimeHelpers.Equals(a, b)) return true;
        if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
        return a.EqualsDict(b);
    }

    /// <summary>
    /// Checks that both map and readonly dictionary does not have the same keys and values.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator !=(AbstractTupleTrieMap<TKey, TValue> a, IReadOnlyDictionary<TKey, TValue> b) => !(a == b);

    /// <summary>
    /// Default AbstractTupleTrieMap constructor. 
    /// </summary>
    public AbstractTupleTrieMap()
    {
    }

    /// <summary>
    /// Copying constructor.
    /// </summary>
    /// <param name="copyFromMap"></param>
    public AbstractTupleTrieMap(AbstractTupleTrieMap<TKey, TValue> copyFromMap)
    {
        if (copyFromMap == null)
        {
            throw new ArgumentNullException(nameof(copyFromMap));
        }

        m_missingValueFactory = copyFromMap.m_missingValueFactory;
        m_count = copyFromMap.m_count;

        var node = copyFromMap.m_root;

        var otherStack = new Data<TrieLinkNode<TValue>>(copyFromMap.Count);
        otherStack.Push(node);

        var currentNode = m_root;

        var thisStack = new Data<TrieLinkNode<TValue>>(copyFromMap.Count);
        thisStack.Push(currentNode);

        while (otherStack.Any)
        {
            var copyFrom = otherStack.Pop();
            var copyTo = thisStack.Pop();

            foreach (var child in copyFrom)
            {
                TrieLinkNode<TValue> newNode;

                if (child.Value is TrieTailNode<TValue> tn)
                {
                    var tailNode = new TrieTailNode<TValue>(child.Key)
                    {
                        Value = tn.Value,
                    };

                    if (tn.Suffix != null)
                    {
                        tailNode.Suffix = new Data<object>(tn.Suffix);
                    }

                    newNode = tailNode;
                }
                else if (child.Value is TrieEndLinkNode<TValue> en)
                {
                    newNode = new TrieEndLinkNode<TValue>(child.Key)
                    {
                        Value = en.Value
                    };
                }
                else
                {
                    newNode = new TrieLinkNode<TValue>(child.Key);
                }

                copyTo.AddChild(newNode, copyFromMap.m_nodesMapFactory);

                otherStack.Push(child.Value);
                thisStack.Push(newNode);
            }
        }
    }

    /// <summary>
    /// Deserialization constructor.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected AbstractTupleTrieMap(SerializationInfo info, StreamingContext context)
    {
        m_sInfo = info;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Clear();
    }

    /// <summary>
    /// SyncRoot object for thread safety.
    /// </summary>
    public object? SyncRoot => this;

    /// <summary>
    /// Current version of container.
    /// </summary>
    public int Version => m_version;

    /// <summary>
    /// Length of the collection.
    /// </summary>
    public int Length => Count;

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return item.Key != null && RemoveCore(item.Key);
    }

    /// <inheritdoc />
    public void Append(KeyValuePair<TKey, TValue> value)
    {
        Add(value.Key, value.Value);
    }

    /// <inheritdoc />
    public int Count => m_count;

    /// <inheritdoc />
    public bool IsReadOnly => false;


    /// <summary>
    /// Enumerator for the map.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var tuple in GetKeyValues())
        {
            yield return new KeyValuePair<TKey, TValue>(tuple.Key, tuple.Value);
        }
    }

    private IEnumerable<(TKey Key, TValue Value)> GetKeyValues()
    {
        var version = m_version;

        var data = new Data<(TrieLinkNode<TValue> Node, TKey Prefix, int pos)>();

        var queue = data.AsQueue();
        queue.Enqueue((m_root, default(TKey), 0));

        while (queue.Any)
        {
            var (node, prefix, pos) = queue.Dequeue();

            CheckState(ref version);

            if (node is TrieEndLinkNode<TValue> en)
            {
                yield return (prefix, en.Value);
            }

            foreach (var child in node)
            {
                queue.Enqueue((child.Value, child.Value.BuildString(prefix, pos, ConcatKeyWith), pos + 1));
            }
        }

        data.Dispose();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetKeyValues().GetEnumerator();

    /// <inheritdoc />
    public void Add([NotNull] TKey key, TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        var add = true;
        Insert(key, ref value, ref add);
    }

    /// <inheritdoc />
    public bool ContainsKey([NotNull] TKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return this.TryGetValueCore(key, out var _);
    }

    /// <inheritdoc />
    public bool Remove([NotNull] TKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return RemoveCore(key);
    }

    /// <inheritdoc />
    public bool TryGetValue([NotNull] TKey key, out TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return this.TryGetValueCore(key, out value);
    }

    /// <inheritdoc />
    public TValue this[[NotNull] TKey key]
    {
        get
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var value = ValueByRef(key, out var found);

            if (found)
            {
                return value;
            }

            if (m_missingValueFactory != null)
            {
                var newValue = m_missingValueFactory(key);
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

    /// <inheritdoc />
    public IEnumerable<TKey> Keys => GetKeys();

    ICollection<TValue> IDictionary<TKey, TValue>.Values
    {
        get
        {
            var data = new Data<TValue>(Count);

            data.AddRange(Values);

            return data;
        }
    }

    ICollection<TKey> IDictionary<TKey, TValue>.Keys
    {
        get
        {
            var data = new Data<TKey>(Count);

            data.AddRange(GetKeys());

            return data;
        }
    }

    /// <summary>
    /// Values of the map.
    /// </summary>
    public IEnumerable<TValue> Values => GetValues();


    /// <summary>
    /// Sets a missing value factory delegate up which would be called instead of throwing the KeyNotFound exception.
    /// </summary>
    /// <param name="missingValueFactory"></param>
    public void EnsureValues([CanBeNull] Func<TKey, TValue> missingValueFactory) => m_missingValueFactory = missingValueFactory;

    /// <summary>
    /// Sets up a nodes map factory delegate which will be called to creat a storage for a particular type. Delegate must be static. 
    /// </summary>
    /// <param name="staticFunc"></param>
    /// <exception cref="ArgumentException">If given factory func is not a static delegate.</exception>
    public void SetStorageFactory([CanBeNull] Func<Type, IDictionary<object, TrieLinkNode<TValue>>> staticFunc)
    {
        if (staticFunc != null)
        {
            bool isStatic = staticFunc.Method.IsStatic;

            if (isStatic == false)
            {
                throw new ArgumentException("Factory function can be static only.");
            }
        }
       
        m_nodesMapFactory = staticFunc;
    }


    /// <summary>
    /// Checks that this map and readonly dictionary has the same keys and values.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    protected bool EqualsDict([NotNull] IReadOnlyDictionary<TKey, TValue> other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (Count == other.Count)
        {
            var equalityComparer = EqualityComparer<object>.Default;

            foreach (var kv in this)
            {
                if (!(other.TryGetValue(kv.Key, out var otherValue)))
                {
                    return false;
                }

                if (!(equalityComparer.Equals(kv.Value, otherValue)))
                {
                    return false;
                }
            }

            foreach (var kv in other)
            {
                if (!(TryGetValueCore(kv.Key, out var otherValue)))
                {
                    return false;
                }

                if (!(equalityComparer.Equals(kv.Value, otherValue)))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified Map&lt;TKey,TValue&gt; instances are considered equal by comparing type, sizes and elements.
    /// </summary>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return EqualsDict((AbstractTupleTrieMap<TKey, TValue>)obj);
    }

    private IEnumerable<TKey> GetKeys()
    {
        foreach (var kv in GetKeyValues())
        {
            yield return kv.Key;
        }
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        var add = true;
        var value = item.Value;
        Insert(item.Key, ref value, ref add);
    }

    /// <inheritdoc />
    public void Clear()
    {
        var trieNode = m_root;

        m_root = new(null);
        m_count = 0;
        unchecked
        {
            ++m_version;
        }

        var stack = new Data<TrieLinkNode<TValue>>();
        stack.Push(trieNode);

        while (stack.Count > 0)
        {
            var node = stack.Pop();

            foreach (var child in node)
            {
                stack.Push(child.Value);
            }

            node.Dispose();
        }

        stack.Dispose();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        if (TryGetValueCore(item.Key, out var value))
        {
            return EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        return false;
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException(
                "The number of elements in the source collection is greater than the available space from arrayIndex to the end of the destination array.");
        }

        foreach (var pair in GetKeyValues())
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// Checks if the map contains a given value. Is not efficient.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool ContainsValue(TValue value)
    {
        foreach (var v in this.Values)
        {
            if (EqualityComparer<TValue>.Default.Equals(v, value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Copies the elements of the map to a data collection
    /// </summary>
    /// <returns></returns>
    public Data<KeyValuePair<TKey, TValue>> ToData()
    {
        var data = new Data<KeyValuePair<TKey, TValue>>(Count);

        foreach (var kv in GetKeyValues())
        {
            data.Add(new KeyValuePair<TKey, TValue>(kv.Key, kv.Value));
        }

        return data;
    }

    /// <summary>
    /// Converts the map to an array.
    /// </summary>
    /// <returns></returns>
    public KeyValuePair<TKey, TValue>[] ToArray()
    {
        var keyValuePairs = new KeyValuePair<TKey, TValue>[Count];

        int index = 0;
        foreach (var kv in GetKeyValues())
        {
            keyValuePairs[index] = new KeyValuePair<TKey, TValue>(kv.Key, kv.Value);

            index++;
        }

        return keyValuePairs;
    }

    /// <summary>
    /// Adds the specified key and value to the map.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Put(TKey key, TValue value)
    {
        var add = true;
        Insert(key, ref value, ref add);
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
    /// </summary>
    /// <returns></returns>
    public TValue GetSet([NotNull] TKey key, Func<TKey, AbstractTupleTrieMap<TKey, TValue>, TValue> missingValue)
    {
        if (TryGetValueCore(key, out var value))
        {
            return value;
        }

        return missingValue(key, this);
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
    /// </summary>
    /// <returns></returns>
    public TValue GetSet<TParam>([NotNull] TKey key, TParam p1, Func<TParam, TKey, AbstractTupleTrieMap<TKey, TValue>, TValue> missingValue)
    {
        if (TryGetValueCore(key, out var value))
        {
            return value;
        }

        return missingValue(p1, key, this);
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
    /// </summary>
    /// <returns></returns>
    public TValue GetSet<TParam1, TParam2>([NotNull] TKey key, TParam1 p1, TParam2 p2, Func<TParam1, TParam2, TKey, AbstractTupleTrieMap<TKey, TValue>, TValue> missingValue)
    {
        if (TryGetValueCore(key, out var value))
        {
            return value;
        }

        return missingValue(p1, p2, key, this);
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key in a map if value missing call missingValue delegate.
    /// </summary>
    /// <returns></returns>
    public TValue GetSet<TParam1, TParam2, TParam3>([NotNull] TKey key, TParam1 p1, TParam2 p2, TParam3 p3, Func<TParam1, TParam2, TParam3, TKey, AbstractTupleTrieMap<TKey, TValue>, TValue> missingValue)
    {
        if (TryGetValueCore(key, out var value))
        {
            return value;
        }

        return missingValue(p1, p2, p3, key, this);
    }

    /// <summary>
    /// Returns values where key starts with a given key part.
    /// </summary>
    /// <param name="key">key storage</param>
    /// <param name="count">key parts count to use</param>
    /// <returns></returns>
    public IEnumerable<TValue> WhereKeyStartsWith(TKey key, int count)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckState(ref ushort version)
    {
        if (version != m_version)
        {
            throw new InvalidOperationException($"TrieMap collection was modified during enumeration.");
        }
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

                currentNode.AddChild(newNode, m_nodesMapFactory);
                currentNodeParent = currentNode;
                currentNode = newNode;

                tailNode = newNode;
                valueAdded = true;
            }
            else
            {
                if (childNode is TrieTailNode<TValue> tn)
                {
                    childNode = currentNode.SplitTailNode(tn, m_nodesMapFactory);
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

    private bool TryGetValueCore(TKey key, out TValue value)
    {
        value = ValueByRef(key, out var success);

        return success;
    }
}