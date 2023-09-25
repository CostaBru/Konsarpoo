namespace Konsarpoo.Collections.Stackalloc;

/// <summary>
/// Key entry for MapRs and SetRs.
/// </summary>
/// <typeparam name="TKey"></typeparam>
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