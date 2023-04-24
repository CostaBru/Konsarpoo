using JetBrains.Annotations;

namespace Konsarpoo.Collections.Allocators;

/// <inheritdoc />
public class DataAllocatorSetup<T> : IDataAllocatorSetup<T>
{
    private readonly IArrayAllocator<T> m_arrayAllocator;
    private readonly IArrayAllocator<Data<T>.INode> m_nodesAllocator;
    private readonly int? m_maxSizeOfArray;

    /// <summary>
    /// Default class constructor.
    /// </summary>
    public DataAllocatorSetup([CanBeNull] IArrayAllocator<T> arrayAllocator, [CanBeNull] IArrayAllocator<Data<T>.INode> nodesAllocator, int? maxSizeOfArray)
    {
        m_arrayAllocator = arrayAllocator;
        m_nodesAllocator = nodesAllocator;
        m_maxSizeOfArray = maxSizeOfArray;
    }

    /// <inheritdoc />
    public IArrayAllocator<T> GetDataArrayAllocator() => m_arrayAllocator;

    /// <inheritdoc />
    public IArrayAllocator<Data<T>.INode> GetNodesArrayAllocator() => m_nodesAllocator;

    /// <inheritdoc />
    public int? MaxSizeOfArray => m_maxSizeOfArray;
}