using System;

namespace Konsarpoo.Collections.Stackalloc;

/// <summary>
/// Key entry
/// </summary>
/// <typeparam name="TKey"></typeparam>
[Serializable]
public struct KeyEntry<TKey>
{
    public KeyEntry(int hashCode, int next, TKey key)
    {
        HashCode = hashCode;
        Next = next;
        Key = key;
    }

    internal int HashCode;
    internal int Next;
    internal TKey Key;
}