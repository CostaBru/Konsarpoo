using System;
using System.Collections.Generic;
using System.Linq;

namespace Konsarpoo.Collections.Persistence;

public partial class FileData<T>
{
    /// <summary>
    /// Sorts the elements in the FileData&lt;T&gt; using default IComparer&lt;T&gt; implementation. 
    /// </summary>
    public void Sort()
    {
        if (m_count < 2)
        {
            return;
        }

        if (SingleArray())
        {
            var arrayChunk = GetOrAddChunk(0);

            Array.Sort(arrayChunk.Array, 0, m_count);

            return;
        }

        SortSlow(this, Comparer<T>.Default.Compare);
    }

    private bool SingleArray()
    {
        var arrayIndex = (m_count - 1) >> m_stepBase;

        var singleArray = arrayIndex == 0;
        return singleArray;
    }

    /// <summary>
    /// Sorts the elements in the FileData&lt;T&gt; using the specified IComparer&lt;T&gt; implementation. 
    /// </summary>
    public void Sort(IComparer<T> comparer)
    {
        Sort(this, comparer);
    }

    /// <summary>
    /// Sorts the elements in the FileData&lt;T&gt; using the provided Comparison&lt;T&gt; delegate to compare data elements. 
    /// </summary>
    public void Sort(Comparison<T> comparison)
    {
        Sort(this, comparison);
    }

    internal static void Sort(FileData<T> list)
    {
        if (list.Count == 0)
        {
            return;
        }

        Sort(list, Comparer<T>.Default.Compare);
    }

    internal static void Sort(FileData<T> list, IComparer<T> comparer)
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

    internal static void Sort(FileData<T> list, Comparison<T> comparison)
    {
        if (list.Count < 2)
        {
            return;
        }

        if (list.SingleArray())
        {
            var arrayChunk = list.GetOrAddChunk(0);

            Array.Sort(arrayChunk.Array, 0, list.m_count, new Comparer(comparison));

            return;
        }

        SortSlow(list, comparison);
    }

    internal static void SortSlow(FileData<T> list, Comparison<T> comparison)
    {
        SortSlowCore(list, comparison);
    }

    private static void SortSlowCore(FileData<T> list, Comparison<T> comparison)
    {
        var data = new Data<T>(list.m_count, list.m_maxSizeOfArray);

        data.AddRange(list.OrderBy(x => x, new Comparer(comparison)));

        int i = 0;
        foreach (var val in data)
        {
            list[i] = val;
            i++;
        }
        
        data.Dispose();
    }
}