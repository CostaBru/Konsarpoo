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
    internal class PoolList<T> : PoolListBase<T>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="capacity"></param>
        public PoolList(ushort maxCapacity, ushort capacity) : base(new GcArrayPoolMixedAllocator<T>(ArrayPool<T>.Shared), maxCapacity, capacity)
        {
        }
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="capacity"></param>
        public PoolList(IArrayAllocator<T> allocator, ushort maxCapacity, ushort capacity) : base(allocator, maxCapacity, capacity)
        {
        }

        /// <summary>
        /// Copying constructor.
        /// </summary>
        /// <param name="poolList"></param>
        public PoolList(PoolListBase<T> poolList, IArrayAllocator<T> allocator) : base(poolList, allocator)
        {
        }
    }
}