using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Konsarpoo.Collections;

/// <summary>
/// Debug view for StringTrie class.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="V"></typeparam>
public sealed class StringTrieMapDebugView<V>
{
    private readonly IReadOnlyDictionary<string, V> m_dict;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dict"></param>
    public StringTrieMapDebugView(IReadOnlyDictionary<string, V> dict)
    {
        m_dict = dict;
    }
        
    /// <summary>
    /// Items visible in debugger.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<string, V>[] Items => this.m_dict.ToArray();
}