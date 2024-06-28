using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Konsarpoo.Collections;

/// <summary>
/// Debug view for StringTrie class.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="V"></typeparam>
public sealed class TrieMapDebugView<T,V>
{
    private readonly IReadOnlyDictionary<T, V> m_dict;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dict"></param>
    public TrieMapDebugView(IReadOnlyDictionary<T, V> dict)
    {
        m_dict = dict;
    }
        
    /// <summary>
    /// Items visible in debugger.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<T, V>[] Items => this.m_dict.ToArray();
}