using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Useful collection extensions.
    /// </summary>
    public static class ReadOnlyExt
    {
        /// <summary>
        /// Null safe method to combine two readonly collections into single instance.
        /// </summary>
        [Pure]
        public static IReadOnlyCollection<T> TryCombine<T>([CanBeNull] this IReadOnlyCollection<T> collection1, [CanBeNull] IReadOnlyCollection<T> collection2)
        {
            if (collection1 == null)
            {
                return collection2;
            }

            if (collection2 == null)
            {
                return collection1;
            }

            return new CollectionWrap<T>(collection1, collection2);
        }

        private class CollectionWrap<T> : IReadOnlyCollection<T>
        {
            private IReadOnlyCollection<T> m_left;
            private IReadOnlyCollection<T> m_right;

            public CollectionWrap(IReadOnlyCollection<T> t1, IReadOnlyCollection<T> t2)
            {
                m_left = t1;
                m_right = t2;
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var v in m_left)
                {
                    yield return v;
                }

                foreach (var v in m_right)
                {
                    yield return v;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            int IReadOnlyCollection<T>.Count => m_left.Count + m_right.Count;
        }
        
        /// <summary>
        /// Union immutable readonly lists without reallocating new arrays. 
        /// </summary>
        [Pure]
        public static IReadOnlyList<T> UnionAsReadOnlyListWith<T>([NotNull] this IReadOnlyList<T> source, [NotNull] params IReadOnlyList<T>[] collections)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collections == null) throw new ArgumentNullException(nameof(collections));

            if (collections.Length == 0)
            {
                return source;
            }
            
            return new ReadOnlyListWrap<T>(source, collections);
        }

        private class ReadOnlyListWrap<T> : IReadOnlyList<T>
        {
            private readonly IReadOnlyList<T>[] m_collections;
            private readonly int[] m_startIndexes;
            private readonly int[] m_endIndexes;
            private readonly int m_count = 0;
            
            public ReadOnlyListWrap(IReadOnlyList<T> t1, IReadOnlyList<T>[] collections)
            {
                m_collections = new IReadOnlyList<T>[collections.Length + 1];
                m_startIndexes = new int[collections.Length + 1];
                m_endIndexes = new int[collections.Length + 1];

                var prevCount = 0;

                m_collections[0] = t1;
                m_startIndexes[0] = 0;
                m_endIndexes[0] = t1.Count - 1;

                prevCount = t1.Count;

                for (int i = 0; i < collections.Length; i++)
                {
                    var readOnlyList = collections[i];
                    var count = readOnlyList.Count;

                    m_collections[i + 1] = readOnlyList;
                    m_startIndexes[i + 1] = prevCount;
                    m_endIndexes[i + 1] = prevCount + count - 1;

                    prevCount += count;
                }

                m_count = prevCount;
            }

            public T this[int index]
            {
                get
                {
                    var collectionIndex = Array.BinarySearch(m_endIndexes, index);

                    if (collectionIndex < 0)
                    {
                        collectionIndex = ~collectionIndex;
                    }

                    var localIndex = index - m_startIndexes[collectionIndex];

                    return m_collections[collectionIndex][localIndex];
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var list in m_collections) foreach (var val in list) yield return val;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            int IReadOnlyCollection<T>.Count => m_count;
        }
    }
}