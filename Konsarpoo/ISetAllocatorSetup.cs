using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections;

/// <summary>
///  Interface for Set class pool set up.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISetAllocatorSetup<T>
{
    /// <summary> Gets bucket allocator setup. </summary>
    [CanBeNull]
    public IDataAllocatorSetup<int> GetBucketsAllocatorSetup();

    /// <summary> Gets storage allocator setup. </summary>
    /// <returns></returns>
    [CanBeNull]
    public  IDataAllocatorSetup<KeyEntry<T>> GeStorageAllocatorSetup();
}