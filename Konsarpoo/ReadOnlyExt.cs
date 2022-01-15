using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Konsarpoo.Collections
{
    public static class ReadOnlyExt
    {
        /// <summary>
        /// Null safe method to combine two readonly collections into single instance.
        /// </summary>
        public static IReadOnlyCollection<T> TryCombine<T>(this IReadOnlyCollection<T> collection1, IReadOnlyCollection<T> collection2)
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
    }
}