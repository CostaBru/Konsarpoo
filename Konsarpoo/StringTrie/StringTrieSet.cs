using System;
using System.Collections;
using System.Collections.Generic;

namespace Konsarpoo.Collections;

internal class StringTrieSet : ICollection<IEnumerable<char>>, ICollection<string>
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

    public void Add(string item)
    {
        Storage[item] = byte.MaxValue;
    }

    public void Clear()
    {
        m_storage.Clear();
    }

    public bool Contains(string item)
    {
        return Storage.TryGetValue(item, out var _);
    }

    public void CopyTo(string[] array, int arrayIndex)
    {
        var storage = (IDictionary<string, byte>)Storage;
        
        storage.Keys.CopyTo(array, arrayIndex);
    }

    public bool Remove(string item)
    {
        return Storage.Remove(item);
    }

    IEnumerator<string> IEnumerable<string>.GetEnumerator()
    {
        var storage = (IDictionary<string, byte>)Storage;

        return storage.Keys.GetEnumerator();
    }

    public IEnumerator<IEnumerable<char>> GetEnumerator() => Storage.Keys.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public bool Contains(IEnumerable<char> item) => Storage.ContainsKey(item);
    public void CopyTo(IEnumerable<char>[] array, int arrayIndex) => Storage.Keys.CopyTo(array, arrayIndex);
    public bool Remove(IEnumerable<char> item) => Storage.Remove(item);
    public int Count => m_storage.Count;
    public bool IsReadOnly => false;
}