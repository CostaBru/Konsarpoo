using JetBrains.Annotations;

namespace Konsarpoo.Collections;

/// <summary>
/// Interface for Data class pool set up.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDataAllocatorSetup<T>
{
    /// <summary> Gets data allocator. </summary>
    [CanBeNull]
    public IArrayPool<T> GetDataArrayAllocator();

    /// <summary> Gets nodes allocator. </summary>
    [CanBeNull]
    public IArrayPool<Data<T>.INode> GetNodesArrayAllocator();
}