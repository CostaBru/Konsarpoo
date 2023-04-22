using JetBrains.Annotations;

namespace Konsarpoo.Collections;

/// <inheritdoc />
public class MapAllocatorSetup<TKey, TValue> : IMapAllocatorSetup<TKey, TValue>
{
    private readonly IDataAllocatorSetup<int> m_dataAllocatorSetup;
    private readonly IDataAllocatorSetup<Map<TKey, TValue>.Entry> m_bucketsAllocatorSetup;

    /// <summary>
    /// Default container constructor.
    /// </summary>
    public MapAllocatorSetup([CanBeNull] IDataAllocatorSetup<int> dataAllocatorSetup, [CanBeNull] IDataAllocatorSetup<Map<TKey, TValue>.Entry> bucketsAllocatorSetup)
    {
        m_dataAllocatorSetup = dataAllocatorSetup;
        m_bucketsAllocatorSetup = bucketsAllocatorSetup;
    }

    /// <inheritdoc />
    public IDataAllocatorSetup<int> GetBucketAllocatorSetup() => m_dataAllocatorSetup;

    /// <inheritdoc />
    public IDataAllocatorSetup<Map<TKey, TValue>.Entry> GetStorageAllocatorSetup() => m_bucketsAllocatorSetup;
}