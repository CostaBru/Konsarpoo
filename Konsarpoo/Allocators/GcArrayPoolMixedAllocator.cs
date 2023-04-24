using System;
using System.Buffers;
using JetBrains.Annotations;

namespace Konsarpoo.Collections.Allocators
{
    /// <summary>
    /// Default .net run time and array pool allocator implementation. Takes advantage of array pool for arrays with len greater than 64. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GcArrayPoolMixedAllocator<T> : IArrayAllocator<T>
    {
        private readonly ArrayPool<T> m_pool;

        /// <summary>
        /// Sets up default array pool behaviour.
        /// </summary>
        public static volatile bool ClearArrayOnRequest = KonsarpooAllocatorGlobalSetup.ClearArrayOnRequest;

        private readonly int m_gcCount;
        
        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="pool"></param>
        public GcArrayPoolMixedAllocator([NotNull] ArrayPool<T> pool, int gcCount = 64)
        {
            m_pool = pool ?? throw new ArgumentNullException(nameof(pool));
            m_gcCount = gcCount;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public GcArrayPoolMixedAllocator(int gcCount = 64)
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

        /// <inheritdoc />
        public bool CleanArrayReturn => ClearArrayOnRequest;
    }
}