using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Helper extension that allows to perform basic LINQ to Object calls and allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Helps to check that item satisfied condition is present in collection. Passes the given value to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Any<T, V>(this IEnumerable<T> enumerable, V value, Func<T, V, bool> comparer, int start = 0)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return FindIndexE(enumerable, value, comparer, start) >= 0;
        }
      
        /// <summary>
        /// Gets the index of item in collection. Passes the given value to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static int FindIndexE<T, V>(this IEnumerable<T> collection, V value, Func<T, V, bool> comparer, int start = 0)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            int i = -1;

            foreach (var item in collection)
            {
                i++;

                if (i < start)
                {
                    continue;
                }

                if (comparer(item, value))
                {
                    return i;
                }
            }

            return -1;
        }
      
        /// <summary>
        /// Filter given collection.  Passes the given value to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> Where<T, V1>(this IEnumerable<T> collection, Func<T, V1, bool> compare, V1 value)
        {
            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            foreach (var item in collection)
            {
                if (compare(item, value))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Filter given collection.  Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> Where<T, V1, V2>(this IEnumerable<T> collection, Func<T, V1, V2, bool> compare, V1 value1, V2 value2)
        {
            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            foreach (var item in collection)
            {
                if (compare(item, value1, value2))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Filter given collection.  Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> Where<T, V1, V2, V3>(this IEnumerable<T> collection, Func<T, V1, V2, V3, bool> compare, V1 value1, V2 value2, V3 value3)
        {
            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            foreach (var item in collection)
            {
                if (compare(item, value1, value2, value3))
                {
                    yield return item;
                }
            }
        }

       
        /// <summary>
        /// Filter given collection.  Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> Where<T, V1, V2, V3, V4>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4)
        {
            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            foreach (var item in collection)
            {
                if (compare(item, value1, value2, value3, value4))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Filter given collection.  Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> Where<T, V1, V2, V3, V4, V5>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, V5, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4, V5 value5)
        {
            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            foreach (var item in collection)
            {
                if (compare(item, value1, value2, value3, value4, value5))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Gets first or default item matches condition. Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        public static T FirstOrDefault<T, V1>(this IEnumerable<T> collection, Func<T, V1, bool> compare, V1 value) => System.Linq.Enumerable.FirstOrDefault(Where<T, V1>(collection, compare, value));

        
        /// <summary>
        /// Gets first or default item matches condition. Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        public static T FirstOrDefault<T, V1, V2>(this IEnumerable<T> collection, Func<T, V1, V2, bool> compare, V1 value1, V2 value2) => System.Linq.Enumerable.FirstOrDefault(Where<T, V1, V2>(collection, compare, value1, value2));

        
        /// <summary>
        /// Gets first or default item matches condition. Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        public static T FirstOrDefault<T, V1, V2, V3>(this IEnumerable<T> collection, Func<T, V1, V2, V3, bool> compare, V1 value1, V2 value2, V3 value3) => System.Linq.Enumerable.FirstOrDefault(Where<T, V1, V2, V3>(collection, compare, value1, value2, value3));

        
        /// <summary>
        /// Gets first or default item matches condition. Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        public static T FirstOrDefault<T, V1, V2, V3, V4>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4) => System.Linq.Enumerable.FirstOrDefault(Where<T, V1, V2, V3, V4>(collection, compare, value1, value2, value3, value4));

        
        /// <summary>
        /// Gets first or default item matches condition. Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        public static T FirstOrDefault<T, V1, V2, V3, V4, V5>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, V5, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4, V5 value5) => System.Linq.Enumerable.FirstOrDefault(Where<T, V1, V2, V3, V4, V5>(collection, compare, value1, value2, value3, value4, value5));


        
        /// <summary>
        /// Gets first item matches condition or trows exception if no one found. Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static T First<T, V1>(this IEnumerable<T> collection, Func<T, V1, bool> compare, V1 value) => System.Linq.Enumerable.First(Where<T, V1>(collection, compare, value));

        /// <summary>
        /// Gets first item matches condition or trows exception if no one found. Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static T First<T, V1, V2>(this IEnumerable<T> collection, Func<T, V1, V2, bool> compare, V1 value1, V2 value2) => System.Linq.Enumerable.First(Where<T, V1, V2>(collection, compare, value1, value2));

        /// <summary>
        /// Gets first item matches condition or trows exception if no one found. Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static T First<T, V1, V2, V3>(this IEnumerable<T> collection, Func<T, V1, V2, V3, bool> compare, V1 value1, V2 value2, V3 value3) => System.Linq.Enumerable.First(Where<T, V1, V2, V3>(collection, compare, value1, value2, value3));

        /// <summary>
        /// Gets first item matches condition or trows exception if no one found. Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static T First<T, V1, V2, V3, V4>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4) => System.Linq.Enumerable.First(Where<T, V1, V2, V3, V4>(collection, compare, value1, value2, value3, value4));

        /// <summary>
        /// Gets first item matches condition or trows exception if no one found. Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static T First<T, V1, V2, V3, V4, V5>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, V5, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4, V5 value5) => System.Linq.Enumerable.First(Where<T, V1, V2, V3, V4, V5>(collection, compare, value1, value2, value3, value4, value5));

        /// <summary>
        /// Checks that collection has any conditions met.Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        public static bool Any<T, V1>(this IEnumerable<T> collection, Func<T, V1, bool> compare, V1 value) => System.Linq.Enumerable.Any(Where<T, V1>(collection, compare, value));

        
        /// <summary>
        /// Checks that collection has any conditions met.Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        public static bool Any<T, V1, V2>(this IEnumerable<T> collection, Func<T, V1, V2, bool> compare, V1 value1, V2 value2) => System.Linq.Enumerable.Any(Where<T, V1, V2>(collection, compare, value1, value2));

        
        /// <summary>
        /// Checks that collection has any conditions met.Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        public static bool Any<T, V1, V2, V3>(this IEnumerable<T> collection, Func<T, V1, V2, V3, bool> compare, V1 value1, V2 value2, V3 value3) => System.Linq.Enumerable.Any(Where<T, V1, V2, V3>(collection, compare, value1, value2, value3));

        
        /// <summary>
        /// Checks that collection has any conditions met.Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        public static bool Any<T, V1, V2, V3, V4>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4) => System.Linq.Enumerable.Any(Where<T, V1, V2, V3, V4>(collection, compare, value1, value2, value3, value4));

        
        /// <summary>
        /// Checks that collection has any conditions met.Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        public static bool Any<T, V1, V2, V3, V4, V5>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, V5, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4, V5 value5) => System.Linq.Enumerable.Any(Where<T, V1, V2, V3, V4, V5>(collection, compare, value1, value2, value3, value4, value5));

        
        /// <summary>
        /// Checks that collection doesn't have any conditions met.Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        public static bool IsEmpty<T, V1>(this IEnumerable<T> collection, Func<T, V1, bool> compare, V1 value) => !System.Linq.Enumerable.Any(Where<T, V1>(collection, compare, value));

        /// <summary>
        /// Checks that collection doesn't have any conditions met.Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        public static bool IsEmpty<T, V1, V2>(this IEnumerable<T> collection, Func<T, V1, V2, bool> compare, V1 value1, V2 value2) => !System.Linq.Enumerable.Any(Where<T, V1, V2>(collection, compare, value1, value2));

        /// <summary>
        /// Checks that collection doesn't have any conditions met.Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        public static bool IsEmpty<T, V1, V2, V3>(this IEnumerable<T> collection, Func<T, V1, V2, V3, bool> compare, V1 value1, V2 value2, V3 value3) => !System.Linq.Enumerable.Any(Where<T, V1, V2, V3>(collection, compare, value1, value2, value3));

        /// <summary>
        /// Checks that collection doesn't have any conditions met.Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        public static bool IsEmpty<T, V1, V2, V3, V4>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4) => !System.Linq.Enumerable.Any(Where<T, V1, V2, V3, V4>(collection, compare, value1, value2, value3, value4));

        /// <summary>
        /// Checks that collection doesn't have any conditions met.Passes the given values to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        public static bool IsEmpty<T, V1, V2, V3, V4, V5>(this IEnumerable<T> collection, Func<T, V1, V2, V3, V4, V5, bool> compare, V1 value1, V2 value2, V3 value3, V4 value4, V5 value5) => !System.Linq.Enumerable.Any(Where<T, V1, V2, V3, V4, V5>(collection, compare, value1, value2, value3, value4, value5));
       
        /// <summary>
        /// Gets the index of item in readonly list collection. Passes the given value to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static int FindIndex<T, V>(this IReadOnlyList<T> collection, V value, Func<T, V, bool> equalityComparer, int start = 0)
        {
            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            int i = 0;

            var collectionCount = collection.Count;

            if (start >= collectionCount || start < 0)
            {
                return -1;
            }

            for (var index = start; index < collectionCount; index++)
            {
                var item = collection[index];

                if (equalityComparer(item, value))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }
        
        /// <summary>
        /// Gets the index of item in readonly list collection. Passes the given value to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static int FindIndex<T, V>(this IReadOnlyList<T> collection, V value, Func<T, V> valueSelector, int start = 0)
        {
            return FindIndex<T, V>(collection, value, valueSelector, EqualityComparer<V>.Default, start);
        }

        /// <summary>
        /// Gets the index of item in readonly list collection. Passes the given value to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static int FindIndex<T, V>(this IReadOnlyList<T> collection, V value, [NotNull] Func<T, V> valueSelector, IEqualityComparer<V> equalityComparer, int start = 0)
        {
            if (valueSelector == null)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }
            
            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            if (start >= collection.Count || start < 0)
            {
                return -1;
            }

            for (int index = start; index < collection.Count; ++index)
            {
                var valueByRef = collection[index];

                var selector = valueSelector(valueByRef);

                if (equalityComparer.Equals(selector, value))
                {
                    return index;
                }
            }

            return -1;
        }
        
        /// <summary>
        /// Gets the last index of item in readonly list collection. Passes the given value to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static int FindLastIndex<T, V>(this IReadOnlyList<T> list, V value, Func<T, V> valueSelector, int end = int.MinValue)
        {
            return FindLastIndex<T, V>(list, value, valueSelector, EqualityComparer<V>.Default, end);
        }
        
        /// <summary>
        /// Gets the last index of item in readonly list collection. Passes the given value to comparer func.
        /// <remarks>It allows to pass lambda func without creating local variable closure and let .net optimize it to static function call.</remarks> 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static int FindLastIndex<T, V>(this IReadOnlyList<T> list, V value, Func<T, V> valueSelector, IEqualityComparer<V> equalityComparer, int end = int.MinValue)
        {
            if (end >= list.Count)
            {
                return -1;
            }
            
            if (end == int.MinValue)
            {
                end = list.Count - 1;
            }
            
            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            for (int index = end; index >= 0; --index)
            {
                var valueByRef = list[index];

                var selector = valueSelector(valueByRef);

                if (equalityComparer.Equals(selector, value))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
