using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Konsarpoo.Collections;

/// <summary>
/// Adapter class to use <see cref="StringTrieMap{TValue}"/> as a <see cref="IDictionary{object, TValue}"/>.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public class StringTrieKeyAsObjectMapAdapter<TValue> : IDictionary<object, TValue>
{
    private StringTrieMap<TValue> m_map = new StringTrieMap<TValue>();
    
    public TValue this[object key] 
    { 
        get => m_map[(string)key];
        set => m_map[(string)key] = value;
    }

    public ICollection<object> Keys => ((IDictionary<string, TValue>)m_map).Keys.Select(k => (object)k).ToList();

    public ICollection<TValue> Values => m_map.Values.ToData();

    public int Count => m_map.Count;

    public bool IsReadOnly => false;

    public void Add(object key, TValue value) => m_map.Add(key.ToString(), value);

    public void Add(KeyValuePair<object, TValue> item) => Add(item.Key, item.Value);

    public void Clear() => m_map.Clear();

    public bool Contains(KeyValuePair<object, TValue> item) => m_map.ContainsKey(item.Key.ToString()) && EqualityComparer<TValue>.Default.Equals(m_map[item.Key.ToString()], item.Value);

    public bool ContainsKey(object key) => m_map.ContainsKey(key.ToString());

    public void CopyTo(KeyValuePair<object, TValue>[] array, int arrayIndex)
    {
        foreach (var kvp in this)
        {
            array[arrayIndex++] = kvp;
        }
    }

    public IEnumerator<KeyValuePair<object, TValue>> GetEnumerator() =>  ((IDictionary<string, TValue>)m_map).Select(kvp => new KeyValuePair<object, TValue>(kvp.Key, kvp.Value)).GetEnumerator();

    public bool Remove(object key) => m_map.Remove(key.ToString());

    public bool Remove(KeyValuePair<object, TValue> item) => Contains(item) && Remove(item.Key);

    public bool TryGetValue(object key, out TValue value) => m_map.TryGetValue(key.ToString(), out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}