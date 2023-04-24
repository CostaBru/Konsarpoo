using JetBrains.Annotations;

namespace Konsarpoo.Collections.Allocators;

/// <summary>
/// Interface for Data class pool set up.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDataAllocatorSetup<T>
{
    /// <summary> Gets a data allocator. </summary>
    [CanBeNull]
    public IArrayAllocator<T> GetDataArrayAllocator();

    /// <summary> Gets a nodes allocator. </summary>
    [CanBeNull]
    public IArrayAllocator<Data<T>.INode> GetNodesArrayAllocator();

    /// <summary> Gets a maximum size of data storage node array for T.</summary>
    int? MaxSizeOfArray { get; }
}