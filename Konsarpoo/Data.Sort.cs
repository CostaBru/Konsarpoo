using System;
using System.Collections.Generic;
using System.Linq;

namespace Konsarpoo.Collections
{
    public partial class Data<T>
    {
        /// <summary>
        /// Sorts the elements or a portion of the elements in the Data&lt;T&gt; using default IComparer&lt;T&gt; implementation. 
        /// </summary>
        public void Sort()
        {
            Sort(this);
        }

        /// <summary>
        /// Sorts the elements or a portion of the elements in the Data&lt;T&gt; using the specified IComparer&lt;T&gt; implementation. 
        /// </summary>
        public void Sort(IComparer<T> comparer)
        {
            Sort(this, comparer);
        }

        /// <summary>
        /// Sorts the elements or a portion of the elements in the Data&lt;T&gt; using the provided Comparison&lt;T&gt; delegate to compare data elements. 
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

                ++list.m_version;
                return;
            }

            SortSlow(list, comparison);
        }

        private static void SortSlow(Data<T> list, Comparison<T> comparison)
        {
            if (list.m_root == null && list.m_count <= SmallListCount)
            {
                switch (list.m_count)
                {
                    case SmallListCount:
                    {
                        if (comparison(list.m_val0, list.m_val1) > 0)
                        {
                            _.Swap(ref list.m_val0, ref list.m_val1);
                        }

                        ++list.m_version;
                        return;
                    }
                }
            }

            SortSlowCore(list, comparison);

            ++list.m_version;
        }

        private static void SortSlowCore(Data<T> list,  Comparison<T> comparison)
        {
            var temp = new Data<T>(list.m_count, list.m_maxSizeOfArray, (list.m_pool, list.m_nodesPool));

            temp.AddRange(list.OrderBy(x => x, new Comparer(comparison)));

            list.m_version += 1;

            list.m_root?.Clear();
            list.m_root = temp.m_root;

            temp.m_root = null;
            temp.Dispose();
        }
    }
}