using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    public static class DataExtensions
    {
        /// <summary>
        /// Merges two sorted Data container into new empty given one.
        /// </summary>
        /// <param name="result">Empty result container.</param>
        /// <param name="l1">Sorted container 1.</param>
        /// <param name="l2">Sorted container 2.</param>
        /// <param name="comparer">Comparer used to sort first and second containers.</param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void MergeAscSorted<T>([NotNull] this Data<T> result, [NotNull] Data<T> l1, [NotNull] Data<T> l2, [NotNull] Func<T, T, int> comparer)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            if (l1 == null)
            {
                throw new ArgumentNullException(nameof(l1));
            }
            if (l2 == null)
            {
                throw new ArgumentNullException(nameof(l2));
            }
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }
            
            var list1 = l1;
            var list2 = l2;
            
            var n1 = list1.Count;
            var n2 = list2.Count;

            if (n2 > n1)
            {
                _.Swap(ref list1, ref list2);
                _.Swap(ref n1, ref n2);
            }
            
            result.Ensure(n1 + n2);

            if (result.m_root is Data<T>.StoreNode r)
            {
                if (list1.m_root is Data<T>.StoreNode s1 && list2.m_root is Data<T>.StoreNode s2)
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(s1.m_items[i], s2.m_items[j]) < 0)
                        {
                            r.m_items[k++] = s1.m_items[i++];
                        }
                        else
                        {
                            r.m_items[k++] = s2.m_items[j++];
                        }
                    }

                    while (i < n1) r.m_items[k++] = s1.m_items[i++];
                    while (j < n2) r.m_items[k++] = s2.m_items[j++];
                }
                else if (list1.m_root is Data<T>.StoreNode ss1)
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(ss1.m_items[i], list2[j]) < 0)
                        {
                            r.m_items[k++] = ss1.m_items[i++];
                        }
                        else
                        {
                            r.m_items[k++] = list2[j++];
                        }
                    }

                    while (i < n1) r.m_items[k++] = ss1.m_items[i++];
                    while (j < n2) r.m_items[k++] = list2[j++];
                }
                else
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(list1[i], list2[j]) < 0)
                        {
                            r.m_items[k++] = list1[i++];
                        }
                        else
                        {
                            r.m_items[k++] = list2[j++];
                        }
                    }

                    while (i < n1) r.m_items[k++] = list1[i++];
                    while (j < n2) r.m_items[k++] = list2[j++];
                }
            }
            else
            {
                if (list1.m_root is Data<T>.StoreNode s1 && list2.m_root is Data<T>.StoreNode s2)
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(s1.m_items[i], s2.m_items[j]) < 0)
                        {
                            result[k++] = s1.m_items[i++];
                        }
                        else
                        {
                            result[k++] = s2.m_items[j++];
                        }
                    }

                    while (i < n1) result[k++] = s1.m_items[i++];
                    while (j < n2) result[k++] = s2.m_items[j++];
                }
                else if (list1.m_root is Data<T>.StoreNode ss1)
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(ss1.m_items[i], list2[j]) < 0)
                        {
                            result[k++] = ss1.m_items[i++];
                        }
                        else
                        {
                            result[k++] = list2[j++];
                        }
                    }

                    while (i < n1) result[k++] = ss1.m_items[i++];
                    while (j < n2) result[k++] = list2[j++];
                }
                else
                {
                    int i = 0, j = 0, k = 0;

                    while (i < n1 && j < n2)
                    {
                        if (comparer(list1[i], list2[j]) < 0)
                        {
                            result[k++] = list1[i++];
                        }
                        else
                        {
                            result[k++] = list2[j++];
                        }
                    }

                    while (i < n1) result[k++] = list1[i++];
                    while (j < n2) result[k++] = list2[j++];
                }
            }
        }

        public static Data<T> ToData<T>([NotNull] this IList<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            return new Data<T>(source);
        }

        public static Data<T> ToData<T>([NotNull] this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            return new Data<T>(source);
        }

        public static Data<T> ToData<T>([NotNull] this Data<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            return new Data<T>(source);
        }

        public static bool ListNullOrItemAbsent<T>([CanBeNull] this IReadOnlyCollection<T> source, T value)
        {
            return source == null || !source.Contains(value);
        }

        public static Set<T> ToSet<T>([NotNull] this IReadOnlyCollection<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            if (source is Set<T> hs)
            {
                return new Set<T>(hs);
            }
            
            return new Set<T>(source);
        }
        
        public static Set<T> ToSet<T>([NotNull] this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            return new Set<T>(source, comparer);
        }
        
        public static int BinarySearchExact<T, V>(
            [NotNull] this IReadOnlyList<T> array,
            V target, 
            int startIndex, 
            int count,
            [NotNull] Func<T, V, int> compare)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            int lo = startIndex;
            int hi = count - 1;

            while (lo <= hi)
            {
                int index = lo + (hi - lo >> 1);

                var comp = compare(array[index], target);

                if (comp == 0)
                {
                    return index;
                }

                if (comp < 0)
                {
                    lo = index + 1;
                }
                else
                {
                    hi = index - 1;
                }
            }
            return ~lo;
        }

        public static int BinarySearchLeft<T, V>(this IReadOnlyList<T> array, V target, Func<T, V, int> compare)
        {
            return BinarySearchLeft(array, target, 0, array.Count, compare);
        }

        public static int BinarySearchLeft<T, V>([NotNull] this IReadOnlyList<T> array, V target, int startIndex, int count,
            [NotNull] Func<T, V, int> compare)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            int lo = startIndex;
            int hi = count - 1;
            int res = -1;
            
            while (lo <= hi)
            {
                int index = lo + (hi - lo >> 1);

                var comp = compare(array[index], target);

                if (comp > 0)
                {
                    hi = index - 1;
                }
                else if (comp < 0)
                {
                    lo = index + 1;
                }
                else
                {
                    res = index;
                    hi = index - 1;
                }
            }

            return res;
        } 
        
        public static int BinarySearchRight<T, V>(this IReadOnlyList<T> array, V target, Func<T, V, int> compare)
        {
            return BinarySearchRight(array, target, 0, array.Count, compare);
        }
        
        public static int BinarySearchRight<T, V>([NotNull] this IReadOnlyList<T> array, V target, int startIndex, int count,
            [NotNull] Func<T, V, int> compare)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            int lo = startIndex;
            int hi = count - 1;
            int res = -1;

            while (lo <= hi)
            {
                int index = lo + (hi - lo >> 1);

                var comp = compare(array[index], target);

                if (comp > 0)
                {
                    hi = index - 1;
                }
                else if (comp < 0)
                {
                    lo = index + 1;
                }
                else
                {
                    res = index;
                    lo = index + 1;
                }
            }

            return res;
        } 
    }
}