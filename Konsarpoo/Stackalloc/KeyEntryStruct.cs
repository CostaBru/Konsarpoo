namespace Konsarpoo.Collections.Stackalloc;

public struct KeyEntryStruct<TKey>
{
    public KeyEntryStruct(int hashCode, int next, TKey key)
    {
        HashCode = hashCode;
        Next = next;
        Key = key;
    }

    internal int HashCode;
    internal int Next;
    internal TKey Key;
}