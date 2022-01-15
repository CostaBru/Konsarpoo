using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Konsarpoo.Collections
{
    public sealed class DictionaryDebugView<T, V>
    {
        private readonly IDictionary<T, V> m_dict;

        public DictionaryDebugView(IDictionary<T, V> dict)
        {
            m_dict = dict;
        }


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

    public sealed class CollectionDebugView<T>
    {
        private readonly ICollection<T> m_collection;

        public CollectionDebugView(ICollection<T> collection)
        {
            m_collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get { return m_collection.ToArray(); }
        }
    }
}