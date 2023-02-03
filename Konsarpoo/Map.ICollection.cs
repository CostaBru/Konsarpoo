using System.Collections.Generic;

namespace Konsarpoo.Collections;

public partial class Map<TKey, TValue>  
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    void ICollection<TKey>.Add(TKey item)
    {
        this[item] = default;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    bool ICollection<TKey>.Contains(TKey item)
    {
        return ContainsKey(item);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    void ICollection<TKey>.CopyTo(TKey[] array, int arrayIndex)
    {
        Keys.CopyTo(array, arrayIndex);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    bool ICollection<TKey>.IsReadOnly => false;

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
    {
        return this.Keys.GetEnumerator();
    }
}