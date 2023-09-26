using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections;

/// <inheritdoc />
public class MapAllocatorSetup<TKey, TValue> : IMapAllocatorSetup<TKey, TValue>
{
    private readonly IDataAllocatorSetup<int> m_dataAllocatorSetup;
    private readonly IDataAllocatorSetup<Entry<TKey,TValue>> m_bucketsAllocatorSetup;

    /// <summary>
    /// Default container constructor.
    /// </summary>
    public MapAllocatorSetup([CanBeNull] IDataAllocatorSetup<int> dataAllocatorSetup, [CanBeNull] IDataAllocatorSetup<Entry<TKey,TValue>> bucketsAllocatorSetup)
    {
        m_dataAllocatorSetup = dataAllocatorSetup;
        m_bucketsAllocatorSetup = bucketsAllocatorSetup;
    }

    /// <inheritdoc />
    public IDataAllocatorSetup<int> GetBucketAllocatorSetup() => m_dataAllocatorSetup;

    /// <inheritdoc />
    public IDataAllocatorSetup<Entry<TKey,TValue>> GetStorageAllocatorSetup() => m_bucketsAllocatorSetup;
}