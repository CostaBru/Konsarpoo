using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Debug view for ICollection classes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class CollectionDebugView<T>
    {
        private readonly ICollection<T> m_collection;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="collection"></param>
        public CollectionDebugView(ICollection<T> collection)
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
}