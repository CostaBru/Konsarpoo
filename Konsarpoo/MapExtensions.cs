using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Useful map extensions.
    /// </summary>
    public static class MapExtensions
    {
        /// <summary>
        /// Gets if key exists or return given default value.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <returns></returns>
        public static TSource GetOrDefault<TSource, TKey>([NotNull] this IReadOnlyDictionary<TKey, TSource> dict, TKey key, TSource defaultVal = default(TSource))
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value;
            }

            return defaultVal;
        }
        
        /// <summary>
        /// Gets if key exists or add new key value.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static TSource GetOrAdd<TSource, TKey>([NotNull] this IDictionary<TKey, TSource> dict, TKey key) where TSource : ICollection, new()
        {
            if (dict == null)
            {
                throw new ArgumentNullException(nameof(dict));
            }
            
            if (!dict.TryGetValue(key, out var value))
            {
                TSource defaultVal;

                dict[key] = defaultVal = new TSource();
                
                return defaultVal;
            }

            return value;
        }
        
        /// <summary>
        /// Gets if key exists or add new key value.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="valueFactory"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static TSource GetOrAdd<TSource, TKey>([NotNull] this IDictionary<TKey, TSource> dict, TKey key, Func<TSource> valueFactory)
        {
            if (dict == null)
            {
                throw new ArgumentNullException(nameof(dict));
            }

            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }
            
            if (!dict.TryGetValue(key, out var value))
            {
                TSource defaultVal;

                dict[key] = defaultVal = valueFactory();
                
                return defaultVal;
            }

            return value;
        }
        
        /// <summary>
        /// Copies readonly dictionary to map.
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <returns></returns>
        public static Map<TKey, TSource> ToMap<TSource, TKey>([CanBeNull] this IReadOnlyDictionary<TKey, TSource> source)
        {
            if (ReferenceEquals(source, null)) 
            {
                return null; 
            }
             
            if (source is Map<TKey, TSource> hd) 
            {
                return new Map<TKey, TSource>(hd); 
            }
             
            return new Map<TKey, TSource>(source); 
        }
    
        /// <summary>
        /// Copies enumerable to map.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <returns></returns>
        public static Map<TKey, TSource> ToMap<TSource, TKey>([CanBeNull] this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            ToMap(source, keySelector, null);
       
        /// <summary>
        /// Copies enumerable to map.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <param name="comparer"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Map<TKey, TSource> ToMap<TSource, TKey>([CanBeNull] this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (ReferenceEquals(source, null))
            {
                return null;
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            int capacity = 0;
            if (source is ICollection<TSource> collection)
            {
                capacity = collection.Count;
                if (capacity == 0)
                {
                    return new Map<TKey, TSource>(comparer);
                }

                if (collection is TSource[] array)
                {
                    return ToMap(array, keySelector, comparer);
                }

                if (collection is IReadOnlyList<TSource> list)
                {
                    return ToMap(list, keySelector, comparer);
                }
            }

            var d = new Map<TKey, TSource>(capacity, comparer);
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), element);
            }

            return d;
        }

        /// <summary>
        /// Copies array to map.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <param name="comparer"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <returns></returns>
        private static Map<TKey, TSource> ToMap<TSource, TKey>([CanBeNull] TSource[] source, [NotNull] Func<TSource, TKey> keySelector, [NotNull] IEqualityComparer<TKey> comparer)
        {
            if (ReferenceEquals(source, null))
            {
                return null;
            }
           
            Map<TKey, TSource> d = new (source.Length, comparer);
            for (int i = 0; i < source.Length; i++)
            {
                d.Add(keySelector(source[i]), source[i]);
            }

            return d;
        }

        private static Map<TKey, TSource> ToMap<TSource, TKey>([CanBeNull] IReadOnlyList<TSource> source, [NotNull] Func<TSource, TKey> keySelector, [NotNull] IEqualityComparer<TKey> comparer)
        {
            if (ReferenceEquals(source, null))
            {
                return null;
            }
           
            Map<TKey, TSource> d = new (source.Count, comparer);
           
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), element);
            }

            return d;
        }

        /// <summary>
        /// Copies enumerable to map.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <param name="elementSelector"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TElement"></typeparam>
        /// <returns></returns>
        public static Map<TKey, TElement> ToMap<TSource, TKey, TElement>([CanBeNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, TKey> keySelector, [NotNull] Func<TSource, TElement> elementSelector) =>
            ToMap(source, keySelector, elementSelector, null);

        /// <summary>
        /// Copies enumerable to map.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <param name="elementSelector"></param>
        /// <param name="comparer"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TElement"></typeparam>
        /// <returns></returns>
        public static Map<TKey, TElement> ToMap<TSource, TKey, TElement>([CanBeNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, TKey> keySelector, [NotNull] Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (ReferenceEquals(source, null))
            {
                return null;
            }

            int capacity = 0;
            if (source is ICollection<TSource> collection)
            {
                capacity = collection.Count;
                if (capacity == 0)
                {
                    return new (comparer);
                }

                if (collection is TSource[] array)
                {
                    return ToMap(array, keySelector, elementSelector, comparer);
                }

                if (collection is IReadOnlyList<TSource> list)
                {
                    return ToMap(list, keySelector, elementSelector, comparer);
                }
            }

            Map<TKey, TElement> d = new (capacity, comparer);
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), elementSelector(element));
            }

            return d;
        }

        private static Map<TKey, TElement> ToMap<TSource, TKey, TElement>([CanBeNull] TSource[] source, [NotNull] Func<TSource, TKey> keySelector,[NotNull]  Func<TSource, TElement> elementSelector, [NotNull] IEqualityComparer<TKey> comparer)
        {
            if (ReferenceEquals(source, null))
            {
                return null;
            }
           
            Map<TKey, TElement> d = new (source.Length, comparer);
            for (int i = 0; i < source.Length; i++)
            {
                d.Add(keySelector(source[i]), elementSelector(source[i]));
            }

            return d;
        }

        private static Map<TKey, TElement> ToMap<TSource, TKey, TElement>([CanBeNull] IReadOnlyList<TSource> source, [NotNull] Func<TSource, TKey> keySelector, [NotNull] Func<TSource, TElement> elementSelector, [NotNull] IEqualityComparer<TKey> comparer)
        {
            if (ReferenceEquals(source, null))
            {
                return null;
            }
           
            Map<TKey, TElement> d = new (source.Count, comparer);
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), elementSelector(element));
            }

            return d;
        }
    }
}