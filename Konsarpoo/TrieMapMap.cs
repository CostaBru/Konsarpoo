using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

[DebuggerTypeProxy(typeof(TrieMapDebugView<,>))]
[DebuggerDisplay("Count = {Count}")]
public partial class TrieMap<TKey, TValue> :
    IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue>,  
    IAppender<KeyValuePair<TKey, TValue>>,
    IDisposable
{
    private readonly Func<TKey, IEnumerable<object>> m_decompose;
    private readonly Func<TKey, object, TKey> m_concat;
    private readonly Func<IEnumerable<object>, TKey> m_compose;
    
    [NonSerialized] private TrieLinkNode<TValue> m_root = new(null);

    [NonSerialized] private ushort m_version;

    [NonSerialized] [CanBeNull] private SerializationInfo m_sInfo;

    [NonSerialized] private static TValue s_nullRef;

    [NonSerialized] [CanBeNull] private Func<TKey, TValue> m_missingValueFactory;

    [NonSerialized] private int m_count;

    /// <summary>
    /// Checks that both map and readonly dictionary has the same keys and values.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator ==([CanBeNull] TrieMap<TKey, TValue> a, [CanBeNull] IReadOnlyDictionary<TKey, TValue> b)
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
    public static bool operator !=(TrieMap<TKey, TValue> a, IReadOnlyDictionary<TKey, TValue> b) => !(a == b);

    /// <summary>
    /// Default StringTrieMap constructor. Case sensitive by default.
    /// </summary>
    public TrieMap(Func<IEnumerable<object>, TKey> compose, [NotNull] Func<TKey, object, TKey> concat, [NotNull] Func<TKey, IEnumerable<object>> decompose)
    {
        m_decompose = decompose ?? throw new ArgumentNullException(nameof(decompose));
        m_concat = concat ?? throw new ArgumentNullException(nameof(concat));
        m_compose = compose ?? throw new ArgumentNullException(nameof(compose));
    }

    /// <summary>
    /// Copying constructor.
    /// </summary>
    /// <param name="copyFromMap"></param>
    public TrieMap(TrieMap<TKey, TValue> copyFromMap) : this(copyFromMap.m_compose, copyFromMap.m_concat, copyFromMap.m_decompose)
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

                copyTo.AddChild(newNode);

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
    protected TrieMap(SerializationInfo info, StreamingContext context)
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

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return item.Key != null && RemoveCore(m_decompose(item.Key));
    }

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
        foreach (var tuple in GetKeyValuesString())
        {
            yield return new KeyValuePair<TKey, TValue>(tuple.Key, tuple.Value);
        }
    }

    private IEnumerable<(TKey Key, TValue Value)> GetKeyValuesString()
    {
        var version = m_version;

        var data = new Data<(TrieLinkNode<TValue> Node, TKey Prefix)>();
            
        var queue = data.AsQueue();
        queue.Enqueue((m_root, default(TKey)));
         
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
                queue.Enqueue((child.Value, child.Value.BuildString(prefix, m_concat)));
            }
        }
            
        data.Dispose();
    }


    IEnumerator IEnumerable.GetEnumerator() => GetKeyValuesString().GetEnumerator();

    public void Add([NotNull] TKey key, TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        var add = true;
        Insert(m_decompose(key), ref value, ref add);
    }

    public bool ContainsKey([NotNull] TKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return this.TryGetValueCore(m_decompose(key), out var _);
    }

    public bool Remove([NotNull] TKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return RemoveCore(m_decompose(key));
    }

    public bool TryGetValue([NotNull] TKey key, out TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return this.TryGetValueCore(m_decompose(key), out value);
    }

    public TValue this[[NotNull] TKey key]
    {
        get
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var value = ValueByRef(m_decompose(key), out var found);

            if (found)
            {
                return value;
            }
                
            if (m_missingValueFactory != null)
            {
                var newValue = m_missingValueFactory(key);
                var add = false;
                Insert(m_decompose(key), ref newValue, ref add);
                return newValue;
            }

            throw new KeyNotFoundException($"Key '{key}' was not found.");
        }
        set
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var add = false;
            Insert(m_decompose(key), ref value, ref add);
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
    public void EnsureValues([CanBeNull] Func<TKey, TValue> missingValueFactory) =>
        m_missingValueFactory = missingValueFactory;

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
                if (!(TryGetValueCore(m_decompose(kv.Key), out var otherValue)))
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
        return EqualsDict((TrieMap<TKey, TValue>)obj);
    }
    
    private IEnumerable<TKey> GetKeys()
    {
        foreach (var kv in GetKeyValuesString())
        {
            yield return kv.Key;
        }
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        var add = true;
        var value = item.Value;
        Insert(m_decompose(item.Key), ref value, ref add);
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

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        if (TryGetValueCore(m_decompose(item.Key), out var value))
        {
            return EqualityComparer<TValue>.Default.Equals(value ,item.Value);
        }

        return false;
    }

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

        foreach (var pair in GetKeyValuesString())
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckState(ref ushort version)
    {
        if (version != m_version)
        {
            throw new InvalidOperationException($"TrieMap collection was modified during enumeration.");
        }
    }
}