using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Default IQueue implementation based on Data structure and start queue property. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Qu<T> : IQueue<T> 
    {
        private readonly Data<T> m_list;

        private int m_startOffset;

        public Qu([NotNull] Data<T> list)
        {
            m_list = list ?? throw new ArgumentNullException(nameof(list));
        }

        /// <inheritdoc />
        public T Dequeue()
        {
            var val = m_list[m_startOffset];

            m_list[m_startOffset] = default;

            m_startOffset++;

            if (Count == 0)
            {
                Clear();
            }

            return val;
        }

        /// <inheritdoc />
        public T Peek()
        {
            return m_list[m_startOffset];
        }

        /// <inheritdoc />
        public bool Any => m_list.Count - m_startOffset > 0;

        private int Count => m_list.Count - m_startOffset;

        /// <inheritdoc />
        public void Enqueue(T item)
        {
           m_list.Add(item);
        }

        /// <inheritdoc />
        public void EnqueueRange(IEnumerable<T> items)
        {
            m_list.AddRange(items);
        }

        /// <inheritdoc />
        public void Clear()
        {
            m_list.Clear();

            m_startOffset = 0;
        }
    }
}