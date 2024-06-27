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
        private readonly IReadOnlyDictionary<T, V> m_dict;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dict"></param>
        public DictionaryDebugView(IReadOnlyDictionary<T, V> dict)
        {
            m_dict = dict;
        }
        
        /// <summary>
        /// Items visible in debugger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<T, V>[] Items => this.m_dict.ToArray();
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
    
}