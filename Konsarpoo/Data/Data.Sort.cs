using System;
using System.Collections.Generic;
using System.Linq;

namespace Konsarpoo.Collections
{
    public partial class Data<T>
    {
        /// <summary>
        /// Sorts the elements in the Data&lt;T&gt; using default IComparer&lt;T&gt; implementation. 
        /// </summary>
        public void Sort()
        {
            if (m_count < 2)
            {
                return;
            }

            var rootStorage = m_root?.Storage;
            
            if (rootStorage != null)
            {
                Array.Sort(rootStorage, 0, m_count);

                unchecked { m_version += 1; }
                
                return;
            }
           
            SortSlow(this, Comparer<T>.Default.Compare);
        }

        /// <summary>
        /// Sorts the elements in the Data&lt;T&gt; using the specified IComparer&lt;T&gt; implementation. 
        /// </summary>
        public void Sort(IComparer<T> comparer)
        {
            Sort(this, comparer);
        }

        /// <summary>
        /// Sorts the elements in the Data&lt;T&gt; using the provided Comparison&lt;T&gt; delegate to compare data elements. 
        /// </summary>
        public void Sort(Comparison<T> comparison)
        {
            Sort(this, comparison);
        }

        internal static void Sort(Data<T> list)
        {
            if (list.Count == 0)
            {
                return;
            }

            Sort(list, Comparer<T>.Default.Compare);
        }

        internal static void Sort(Data<T> list, IComparer<T> comparer)
        {
            if (list.Count == 0)
            {
                return;
            }
            Sort(list, comparer.Compare);
        }

        private class Comparer : IComparer<T>
        {
            private readonly Comparison<T> m_comparison;

            public Comparer(Comparison<T> comparison)
            {
                m_comparison = comparison;
            }

            public int Compare(T x, T y)
            {
                return m_comparison(x, y);
            }
        }

        internal static void Sort(Data<T> list, Comparison<T> comparison)
        {
            if (list.Count < 2)
            {
                return;
            }

            if (list.m_root?.Storage != null)
            {
                Array.Sort(list.m_root.Storage, 0, list.m_root.Size, new Comparer(comparison));

                unchecked { list.m_version += 1; }
                
                return;
            }

            SortSlow(list, comparison);
        }

        internal static void SortSlow(Data<T> list, Comparison<T> comparison)
        {
            SortSlowCore(list, comparison);

            unchecked { ++list.m_version; }
        }

        private static void SortSlowCore(Data<T> list,  Comparison<T> comparison)
        {
            var temp = new Data<T>(list.m_count);

            temp.AddRange(list.OrderBy(x => x, new Comparer(comparison)));

            unchecked { list.m_version += 1; }

            list.m_root?.Clear(list.m_allocator);
            list.m_root = temp.m_root;
            list.m_tailStoreNode = temp.UpdateLastNode();

            temp.m_root = null;
            temp.m_tailStoreNode = null;
            temp.Dispose();
        }
    }
}