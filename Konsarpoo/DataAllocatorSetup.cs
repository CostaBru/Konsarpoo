using JetBrains.Annotations;

namespace Konsarpoo.Collections;

/// <inheritdoc />
public class DataAllocatorSetup<T> : IDataAllocatorSetup<T>
{
    private readonly IArrayPool<T> m_arrayPool;
    private readonly IArrayPool<Data<T>.INode> m_nodesPool;

    /// <summary>
    /// Default class constructor.
    /// </summary>
    public DataAllocatorSetup([CanBeNull] IArrayPool<T> arrayPool, [CanBeNull] IArrayPool<Data<T>.INode> nodesPool)
    {
        m_arrayPool = arrayPool;
        m_nodesPool = nodesPool;
    }

    /// <inheritdoc />
    public IArrayPool<T> GetDataArrayAllocator() => m_arrayPool;


    /// <inheritdoc />
    public IArrayPool<Data<T>.INode> GetNodesArrayAllocator() => m_nodesPool;
}