using System;
using System.Buffers;
using System.Diagnostics;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Basic pool based list. Cannot has size larger than max capacity. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("PoolList. Size: {m_size}")]
    internal class PoolList<T> : PoolListBase<T>, IDisposable
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="capacity"></param>
        public PoolList(int maxCapacity, int capacity) : base(new GcArrayPoolMixedAllocator<T>(ArrayPool<T>.Shared), maxCapacity, capacity)
        {
        }
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="capacity"></param>
        public PoolList(IArrayAllocator<T> allocator, int maxCapacity, int capacity) : base(allocator, maxCapacity, capacity)
        {
        }

        /// <summary>
        /// Copying constructor.
        /// </summary>
        /// <param name="poolList"></param>
        public PoolList(PoolListBase<T> poolList) : base(poolList)
        {
        }

        /// <summary>
        /// Destructor called by GC. Shouldn't be called if instance is properly disposed beforehand.
        /// </summary>
        ~PoolList()
        {
            Clear();
        }

        /// <summary>
        /// Clears container and returns all node arrays back to array allocator. Suppresses instance finalization.
        /// </summary>
        public void Dispose()
        {
            Clear();

            GC.SuppressFinalize(this);
        }
    }
}