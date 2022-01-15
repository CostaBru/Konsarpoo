using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Konsarpoo.Collections
{
    public partial class Data<T> 
    {
          /// <summary>
        /// Returns a non distinct union of two collections
        /// </summary>
        public static Data<T> operator +(Data<T> a, IReadOnlyCollection<T> b)
        {
            if (ReferenceEquals(a, null))
            {
                return b?.ToData();
            }
            
            if (ReferenceEquals(b, null))
            {
                return a?.ToData();
            }

            var list = new Data<T>(a);
            
            list.Ensure(a.Count + b.Count);

            int i = a.Count;
            foreach (var bl in b)
            {
                list[i++] = bl;
            }

            return list;
        }
        
        /// <summary>
        /// Returns an items that are absent in second collection.
        /// </summary>
        public static Data<T> operator -(Data<T> a, IReadOnlyCollection<T> b)
        {
            if (ReferenceEquals(a, null))
            {
                return null;
            }
            
            if (ReferenceEquals(b, null))
            {
                return a.ToData();
            }

            var list = new Data<T>(Math.Max(a.Count - b.Count, 0));
            
            foreach (var item in a)
            {
                if (!(b.Contains(item)))
                {
                    list.Add(item);
                }
            }

            return list;
        }
        
        /// <summary>
        /// Compares two collections using default comparer for T.
        /// </summary>
        public static bool operator ==(Data<T> a, IReadOnlyList<T> b)
        {
            if (RuntimeHelpers.Equals(a, b))
                return true;

            if ((object) a == null || (object) b == null)
                return false;

            return a.EqualsList(b);
        }
    
        /// <summary>
        /// Compares two collections using default comparer for T.
        /// </summary>
        public static bool operator !=(Data<T> a, IReadOnlyList<T> b)
        {
            if (RuntimeHelpers.Equals(a, b))
                return false;

            if ((object)a == null || (object)b == null)
                return true;

            return !(a.EqualsList(b));
        }
        
        protected bool EqualsList(IReadOnlyList<T> other)
        {
            if (m_count == other.Count)
            {
                for (int i = 0; i < m_count ; i++)
                {
                    if (!(EqualityComparer<T>.Default.Equals(this[i], other[i])))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Determines whether the specified Data&lt;T&gt; instances are considered equal by comparing type, sizes and elements.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            
            return EqualsList((Data<T>) obj);
        }

        /// <summary>
        /// Returns a hashcode generated using default equality comparer for all items contained in Data&lt;T&gt;.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_count.GetHashCode();

                foreach (var item in this)
                {
                    hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(item);
                }
              
                return hashCode;
            }
        }

    }
}