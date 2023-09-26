using System;

namespace Konsarpoo.Collections.Stackalloc;

[Serializable]
public struct Entry<TKey, TValue>
{
    public KeyEntry<TKey> Key;
    public TValue Value;
}