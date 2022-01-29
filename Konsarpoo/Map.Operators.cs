using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    public partial class  Map<TKey, TValue>
    {
        public static Map<TKey, TValue> operator +([CanBeNull] Map<TKey, TValue> a, [CanBeNull] IReadOnlyDictionary<TKey, TValue> b)
        {
            if (ReferenceEquals(a, null))
            {
                return b?.ToMap();
            }
            
            if (ReferenceEquals(b, null))
            {
                return a?.ToMap();
            }

            var dict = new Map<TKey, TValue>(a.m_comparer);
            
            dict.Union(a);
            dict.Union(b);

            return dict;
        }

        public void Union([NotNull] IReadOnlyDictionary<TKey, TValue> dict)
        {
            if (dict == null) throw new ArgumentNullException(nameof(dict));

            foreach (var kv in dict)
            {
                this[kv.Key] = kv.Value;
            }
        }

        public static Map<TKey, TValue> operator -([CanBeNull] Map<TKey, TValue> a,  [CanBeNull] IReadOnlyDictionary<TKey, TValue> b)
        {
            if (ReferenceEquals(a, null))
            {
                return null;
            }
            
            if (ReferenceEquals(b, null))
            {
                return a.ToMap();
            }

            var list = new Map<TKey, TValue>(a.m_comparer);
            
            foreach (var item in a)
            {
                if (!(b.TryGetValue(item.Key, out var bValue)))
                {
                    list.Add(item);
                }                
                else if (!(EqualityComparer<TValue>.Default.Equals(item.Value, bValue)))
                {
                    list.Add(item);
                }
            }

            return list;
        }
        
        public static Map<TKey, TValue> operator -([CanBeNull] Map<TKey, TValue> a,  [CanBeNull] IReadOnlyCollection<TKey> b)
        {
            if (ReferenceEquals(a, null))
            {
                return null;
            }
            
            if (ReferenceEquals(b, null))
            {
                return a.ToMap();
            }

            var list = new Map<TKey, TValue>(a.m_comparer);
            
            foreach (var item in a)
            {
                if (!(b.Contains(item.Key)))
                {
                    list.Add(item);
                }  
            }

            return list;
        }

        public static bool operator ==([CanBeNull] Map<TKey, TValue> a, [CanBeNull] IReadOnlyDictionary<TKey, TValue> b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            {
                return true;
            }
            
            if (RuntimeHelpers.Equals(a, b))
            {
                return true;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }

            return a.EqualsDict(b);
        }

        public static bool operator !=([CanBeNull] Map<TKey, TValue> a, [CanBeNull] IReadOnlyDictionary<TKey, TValue> b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            {
                return false;
            }
            
            if (RuntimeHelpers.Equals(a, b))
            {
                return false;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return true;
            }

            return !(a.EqualsDict(b));
        }

        protected bool EqualsDict([NotNull] IReadOnlyDictionary<TKey, TValue> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            
            if (m_count == other.Count)
            {
                foreach (var kv in this)
                {
                    if(!(other.TryGetValue(kv.Key, out var otherValue)))
                    {
                        return false; 
                    }
                    
                    if (!(EqualityComparer<TValue>.Default.Equals(kv.Value, otherValue)))
                    {
                        return false;
                    }
                }
                
                foreach (var kv in other)
                {
                    if(!(this.TryGetValue(kv.Key, out var otherValue)))
                    {
                        return false; 
                    }
                }

                return true;
            }

            return false;
        }
    }
}