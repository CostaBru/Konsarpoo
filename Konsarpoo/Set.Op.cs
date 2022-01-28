using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Konsarpoo.Collections
{
    public partial class Set<T>
    {
        public static Set<T> operator +(Set<T> a, IReadOnlyCollection<T> b)
        {
            if (ReferenceEquals(a, null))
            {
                return b?.ToSet();
            }
            
            if (ReferenceEquals(b, null))
            {
                return a?.ToSet();
            }

            var set = new Set<T>(a.Count + b.Count, a.m_comparer);
            
            set.AddRange(a);
            set.AddRange(b);

            return set;
        }
        
        public static Set<T> operator -(Set<T> a, IReadOnlyCollection<T> b)
        {
            if (ReferenceEquals(a, null))
            {
                return null;
            }
            
            if (ReferenceEquals(b, null))
            {
                return a.ToSet();
            }

            var list = new Set<T>(Math.Max(a.Count - b.Count, 0), a.m_comparer);
            
            foreach (var item in a)
            {
                if (!(b.Contains(item)))
                {
                    list.Add(item);
                }
            }

            return list;
        }
        
        public static bool operator ==(Set<T> a, IReadOnlyCollection<T> b)
        {
            if (RuntimeHelpers.Equals(a, b))
                return true;

            if ((object) a == null || (object) b == null)
                return false;

            if (b is Set<T> bs)
            {
                return Set<T>.HashSetEquals(a, bs, a.m_comparer);
            }

            return a.EqualsSet(b);
        }
    
        public static bool operator !=(Set<T> a, IReadOnlyCollection<T> b)
        {
            if (RuntimeHelpers.Equals(a, b))
                return false;

            if ((object)a == null || (object)b == null)
                return true;

            return !(a.EqualsSet(b));
        }
        
        protected bool EqualsSet(IReadOnlyCollection<T> other)
        {
            if (m_count == other.Count)
            {
                foreach (var item in other)
                {
                    if (!this.Contains(item))
                    {
                        return false;
                    }
                }

                foreach (var item in this)
                {
                    if (!other.Contains(item))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

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
            
            return EqualsSet((Set<T>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 3 ^ m_count.GetHashCode();

                foreach (var item in this)
                {
                    hashCode = (hashCode * 397) ^ m_comparer.GetHashCode(item);
                }
              
                return hashCode;
            }
        }
    }
}