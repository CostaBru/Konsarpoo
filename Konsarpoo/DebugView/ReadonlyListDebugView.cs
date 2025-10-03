using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Konsarpoo.Collections;

/// <summary>
/// Debug view for IReadOnlyList classes.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ReadonlyListDebugView<T>
{
    private readonly IReadOnlyList<T> m_collection;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="collection"></param>
    public ReadonlyListDebugView(IReadOnlyList<T> collection)
    {
        m_collection = collection;
    }

    /// <summary>
    /// Items visible in debugger.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get { return m_collection.ToArray(); }
    }
}