using System.Collections;
using System.Collections.Generic;

namespace Konsarpoo.Collections;

internal class StringTrieSet : ICollection<IEnumerable<char>>
{
    private StringTrieMap<byte> m_storage;
    private IDictionary<IEnumerable<char>, byte> Storage => m_storage;

    public StringTrieSet(bool caseSensitive = false)
    {
        m_storage = new StringTrieMap<byte>(caseSensitive);
    }
    
    public void Add(IEnumerable<char> item)
    {
        m_storage.TryAdd(item, byte.MaxValue);
    }
    
    public void Clear()
    {
        m_storage.Clear();
    }
    public IEnumerator<IEnumerable<char>> GetEnumerator() => Storage.Keys.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public bool Contains(IEnumerable<char> item) => Storage.ContainsKey(item);
    public void CopyTo(IEnumerable<char>[] array, int arrayIndex) => Storage.Keys.CopyTo(array, arrayIndex);
    public bool Remove(IEnumerable<char> item) => Storage.Remove(item);
    public int Count => m_storage.Count;
    public bool IsReadOnly => false;
}