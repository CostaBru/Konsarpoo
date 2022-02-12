using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Debug view for IDictionary classes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="V"></typeparam>
    public sealed class DictionaryDebugView<T, V>
    {
        private readonly IDictionary<T, V> m_dict;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dict"></param>
        public DictionaryDebugView(IDictionary<T, V> dict)
        {
            m_dict = dict;
        }

        
        /// <summary>
        /// Items visible in debugger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<T, V>[] Items
        {
            get
            {

                KeyValuePair<T, V>[] array = new KeyValuePair<T, V>[this.m_dict.Count];
                this.m_dict.CopyTo(array, 0);
                return array;
            }
        }
    }

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