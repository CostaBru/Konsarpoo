namespace Konsarpoo.Collections.Allocators;

/// <summary>
/// Class represents default .net run time array allocator set up.
/// </summary>
public class DefaultGcAllocatorSetup : IDefaultAllocatorSetup
{
    private readonly int? m_maxDataArrayLength;

    /// <summary> Default constructor. </summary>
    /// <param name="maxDataArrayLength"></param>
    public DefaultGcAllocatorSetup(int? maxDataArrayLength = null)
    {
        m_maxDataArrayLength = maxDataArrayLength;
    }

    /// <inheritdoc />
    public IDataAllocatorSetup<T> GetDataStorageAllocator<T>()
    {
        return GcAllocatorSetup.GetDataPoolSetup<T>(m_maxDataArrayLength);
    }

    /// <inheritdoc />
    public IMapAllocatorSetup<T, V> GetMapAllocator<T,V>()
    {
        return GcAllocatorSetup.GetMapPoolSetup<T, V>(m_maxDataArrayLength);
    }

    /// <inheritdoc />
    public ISetAllocatorSetup<T> GetSetAllocator<T>()
    {
        return GcAllocatorSetup.GetSetPoolSetup<T>(m_maxDataArrayLength);
    }
}