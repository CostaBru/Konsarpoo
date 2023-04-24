using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections;


/// <summary>
/// Map class pool set up container
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public interface IMapAllocatorSetup<TKey, TValue>
{
    /// <summary>
    /// Gets bucket allocator setup.
    /// </summary>
    [CanBeNull]
    public IDataAllocatorSetup<int> GetBucketAllocatorSetup();

    /// <summary>
    /// Gets storage allocator setup.
    /// </summary>
    public IDataAllocatorSetup<Map<TKey, TValue>.Entry> GetStorageAllocatorSetup();
}