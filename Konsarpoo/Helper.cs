using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Helper class.
    /// </summary>
    public static class _
    {
        /// <summary>
        /// Python API. Returns len of collection.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static int len<T>([NotNull] IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            
            return items.Count();
        }
        
        /// <summary>
        /// Swaps references.
        /// </summary>
        /// <param name="var1"></param>
        /// <param name="var2"></param>
        /// <typeparam name="T"></typeparam>
        public static void Swap<T>(ref T var1, ref T var2)
        {
            (var1, var2) = (var2, var1);
        }
        
        /// <summary>
        /// Creates new Data.
        /// </summary>
        /// <param name="items"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Data<T> List<T>(params T[] items)
        {
            return new Data<T>(items);
        }
        
        /// <summary>
        /// Creates new Map.
        /// </summary>
        /// <param name="items"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        public static Map<T, V> Map<T, V>(params (T,V)[] items)
        {
            var dict = new Map<T, V>();

            foreach (var tuple in items)
            {
                dict[tuple.Item1] = tuple.Item2;
            }
            
            return dict;
        }
        
        /// <summary>
        /// Create new Map of objects.
        /// </summary>
        /// <param name="items"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Map<T, object> MapObj<T>(params (T,object)[] items)
        {
            var dict = new Map<T, object>();

            foreach (var tuple in items)
            {
                dict[tuple.Item1] = tuple.Item2;
            }
            
            return dict;
        }
    }
}