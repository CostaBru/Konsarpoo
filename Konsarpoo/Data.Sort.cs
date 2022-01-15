using System;
using System.Collections.Generic;

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
            Sort(list, 0, list.Count - 1, comparer.Compare);
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

            Sort(list, 0, list.Count - 1, comparison);

            ++list.m_version;
        }

        private static void Sort(Data<T> list, int left, int right, Comparison<T> comparison)
        {
            do
            {
                int leftIdx = left;
                int rightIdx = right;
                int currentIdx = leftIdx + (rightIdx - leftIdx >> 1);

                if (leftIdx != currentIdx)
                {
                    var x = list.ValueByRef(leftIdx);
                    var y = list.ValueByRef(currentIdx);

                    if (comparison(x, y) > 0)
                    {
                        T obj = x;
                        list.ValueByRef(leftIdx) = y;
                        list.ValueByRef(currentIdx) = obj;
                    }
                }

                if (leftIdx != rightIdx)
                {
                    var x = list.ValueByRef(leftIdx);
                    var y = list.ValueByRef(rightIdx);

                    if (comparison(x, y) > 0)
                    {
                        T obj = x;
                        list.ValueByRef(leftIdx) = y;
                        list.ValueByRef(rightIdx) = obj;
                    }
                }

                if (currentIdx != rightIdx)
                {
                    var x = list.ValueByRef(currentIdx);
                    var y = list.ValueByRef(rightIdx);

                    if (comparison(x, y) > 0)
                    {
                        T obj = x;
                        list.ValueByRef(currentIdx) = y;
                        list.ValueByRef(rightIdx) = obj;
                    }
                }

                T value = list.ValueByRef(currentIdx);
                do
                {
                    while (comparison(list.ValueByRef(leftIdx), value) < 0)
                    {
                        ++leftIdx;
                    }
                    while (comparison(value, list.ValueByRef(rightIdx)) < 0)
                    {
                        --rightIdx;
                    }
                    if (leftIdx <= rightIdx)
                    {
                        if (leftIdx < rightIdx)
                        {
                            T obj2 = list.ValueByRef(leftIdx);
                            list.ValueByRef(leftIdx) = list.ValueByRef(rightIdx);
                            list.ValueByRef(rightIdx) = obj2;
                        }
                        ++leftIdx;
                        --rightIdx;
                    }
                    else
                    {
                        break;
                    }
                }
                while (leftIdx <= rightIdx);

                if (rightIdx - left <= right - leftIdx)
                {
                    if (left < rightIdx)
                    {
                        Sort(list, left, rightIdx, comparison);
                    }
                    left = leftIdx;
                }
                else
                {
                    if (leftIdx < right)
                    {
                        Sort(list, leftIdx, right, comparison);
                    }
                    right = rightIdx;
                }
            }
            while (left < right);
        }
    }
}