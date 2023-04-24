namespace Konsarpoo.Collections.Allocators;

/// <summary>
/// Class represents default array pool allocator set up.
/// </summary>
public class DefaultArrayPoolAllocatorSetup : IDefaultAllocatorSetup
{
    private readonly int? m_maxDataArrayLen;

    /// <summary> Class constructor </summary>
    /// <param name="maxDataArrayLen"></param>
    public DefaultArrayPoolAllocatorSetup(int? maxDataArrayLen)
    {
        m_maxDataArrayLen = maxDataArrayLen;
    }

    /// <inheritdoc />
    public IDataAllocatorSetup<T> GetDataStorageAllocator<T>()
    {
        return ArrayPoolAllocatorSetup.GetDataAllocatorSetup<T>(maxDataNodeArrayLength: m_maxDataArrayLen);
    }

    /// <inheritdoc />
    public IMapAllocatorSetup<T, V> GetMapAllocator<T,V>()
    {
        return ArrayPoolAllocatorSetup.GetMapAllocatorSetup<T, V>(maxDataNodeArrayLength: m_maxDataArrayLen);
    }

    /// <inheritdoc />
    public ISetAllocatorSetup<T> GetSetAllocator<T>()
    {
        return ArrayPoolAllocatorSetup.GetSetAllocatorSetup<T>(maxDataNodeArrayLength: m_maxDataArrayLen);
    }
}