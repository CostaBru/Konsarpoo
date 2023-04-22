using System;
using System.Buffers;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

/// <summary>
/// Default array allocator implementation. Takes advantage of array pool for arrays with len greater than 64. 
/// </summary>
/// <typeparam name="T"></typeparam>
public class ArrayPoolAllocator<T> : IArrayPool<T>
{
    private readonly ArrayPool<T> m_pool;

    /// <summary>
    /// Sets up default array pool behaviour.
    /// </summary>
    public static volatile bool ClearArrayOnRequest = ArrayPoolGlobalSetup.ClearArrayOnRequest;

    /// <summary>
    /// Class constructor.
    /// </summary>
    /// <param name="pool"></param>
    public ArrayPoolAllocator(ArrayPool<T> pool)
    {
        m_pool = pool;
    }
    
    /// <summary>
    /// Default constructor.
    /// </summary>
    public ArrayPoolAllocator()
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