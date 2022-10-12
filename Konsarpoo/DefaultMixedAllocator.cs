﻿using System;
using System.Buffers;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Default array allocator implementation. Takes advantage of array pool for arrays with len greater than 64. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DefaultMixedAllocator<T> : IArrayPool<T>
    {
        private readonly ArrayPool<T> m_pool;

        /// <summary>
        /// Sets up default array pool behaviour.
        /// </summary>
        public static volatile bool ClearArrayOnRequest = ArrayPoolGlobalSetup.ClearArrayOnRequest;

        private readonly int m_gcCount;
        
        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="pool"></param>
        public DefaultMixedAllocator(ArrayPool<T> pool, int gcCount = 64)
        {
            m_pool = pool;
            m_gcCount = gcCount;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DefaultMixedAllocator(int gcCount = 64)
        {
            m_pool = ArrayPool<T>.Shared;
            m_gcCount = gcCount;
        }

        /// <summary>
        /// Gets new array with size greater or equal to given count.
        /// </summary>
        /// <param name="count"></param>
        [NotNull]
        public T[] Rent(int count)
        {
            if (count <= m_gcCount)
            {
                return new T[count];
            }

            var rent = m_pool.Rent(count);

            if (ClearArrayOnRequest)
            {
                Array.Clear(rent, 0, count);
            }
            
            return rent;
        }

        /// <summary>
        /// Recycles array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="clearArray"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Return([NotNull] T[] array, bool clearArray = false)
        {
            if (array.Length <= m_gcCount)
            {
                return;
            }
            
            m_pool.Return(array, clearArray);
        }
    }
}