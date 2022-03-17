using System.Collections;

namespace Konsarpoo.Collections
{
    public partial class Data<T> 
    {
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        int IList.IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        void IList.Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        void IList.Remove(object value)
        {
            Remove((T)value);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        int IList.Add(object value)
        {
            Add((T)value);

            return m_count - 1;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool IList.Contains(object value)
        {
            return Contains((T)value);
        }
    }
}