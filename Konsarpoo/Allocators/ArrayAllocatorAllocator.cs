using System;
using System.Buffers;
using JetBrains.Annotations;

namespace Konsarpoo.Collections.Allocators;

/// <summary>
/// Default array pool allocator implementation.  
/// </summary>
/// <typeparam name="T"></typeparam>
public class ArrayAllocatorAllocator<T> : IArrayAllocator<T>
{
    private readonly ArrayPool<T> m_pool;

    /// <summary>
    /// Sets up default array pool behaviour.
    /// </summary>
    public static volatile bool ClearArrayOnRequest = KonsarpooAllocatorGlobalSetup.ClearArrayOnRequest;

    /// <summary>
    /// Class constructor.
    /// </summary>
    /// <param name="pool"></param>
    public ArrayAllocatorAllocator(ArrayPool<T> pool)
    {
        m_pool = pool;
    }
    
    /// <summary>
    /// Default constructor.
    /// </summary>
    public ArrayAllocatorAllocator()
    {
        m_pool = ArrayPool<T>.Shared;
    }

    /// <summary>
    /// Gets new array with size greater or equal to given count.
    /// </summary>
    /// <param name="count"></param>
    [NotNull]
    public T[] Rent(int count)
    {
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
        m_pool.Return(array, clearArray);
    }

    /// <inheritdoc />
    public bool CleanArrayReturn => ClearArrayOnRequest;
}