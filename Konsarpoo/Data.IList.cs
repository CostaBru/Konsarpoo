using System.Collections;

namespace Konsarpoo.Collections
{
    public partial class Data<T> 
    {
        int IList.IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        void IList.Remove(object value)
        {
            Remove((T)value);
        }
        
        int IList.Add(object value)
        {
            Add((T)value);

            return m_count - 1;
        }

        bool IList.Contains(object value)
        {
            return Contains((T)value);
        }
    }
}