using System;
using System.Buffers;
using System.Collections.Generic;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections.Allocators;

/// <summary>  </summary>
public class DefaultGcArrayPoolMixedAllocatorSetup : IDefaultAllocatorSetup
{
    private readonly int m_gcCount;
    private readonly int? m_maxDataArrayLen;

    private readonly Dictionary<Type, (object dataAlloc, object nodesAlloc)> m_dataPoolDefaults = new();
    private readonly Dictionary<(Type, Type), (object bucketPool, object bucketNodePool, object entryBucketPool, object entryNodesPool)> m_mapPoolDefaults = new();
    private readonly Dictionary<Type, (object bucketPool, object bucketNodePool, object entryBucketPool, object entryNodesPool)> m_setPoolDefaults = new();

    public DefaultGcArrayPoolMixedAllocatorSetup(int gcCount = 64, int? maxDataArrayLen = null)
    {
        m_gcCount = gcCount;
        m_maxDataArrayLen = maxDataArrayLen;
    }

    /// <summary>
    /// Allows to set up array pools per type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void RegisterDataPools<T>(ArrayPool<T> pool, ArrayPool<Data<T>.INode> nodePool)
    {
        m_dataPoolDefaults[typeof(T)] = (pool, nodePool);
    }
    
    /// <summary>
    /// Allows to set up array pools per type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void RegisterMapPools<T, V>(ArrayPool<int> bucketPool = null, ArrayPool<Data<int>.INode> bucketNodePool = null, ArrayPool<Entry<T,V>> entryBucketPool = null, ArrayPool<Data<Entry<T,V>>.INode> entryNodesPool = null)
    {
        m_mapPoolDefaults[(typeof(T), typeof(V))] = (bucketPool, bucketNodePool, entryBucketPool, entryNodesPool);
    }
    
    /// <summary>
    /// Allows to set up array pools per type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void RegisterSetPools<T>(ArrayPool<int> bucketPool = null, ArrayPool<Data<int>.INode> bucketNodePool = null, ArrayPool<KeyEntry<T>> entryBucketPool = null, ArrayPool<Data<KeyEntry<T>>.INode> entryNodesPool = null)
    {
        m_setPoolDefaults[typeof(T)] = (bucketPool, bucketNodePool, entryBucketPool, entryNodesPool);
    }

    /// <inheritdoc />
    public IDataAllocatorSetup<T> GetDataStorageAllocator<T>()
    {
        if (m_dataPoolDefaults.TryGetValue(typeof(T), out var poolSetup))
        {
            return GcArrayPoolMixedAllocatorSetup.GetDataAllocatorSetup<T>(m_gcCount, (ArrayPool<T>)poolSetup.dataAlloc, (ArrayPool<Data<T>.INode>)poolSetup.nodesAlloc, m_maxDataArrayLen);
        }
        
        return GcArrayPoolMixedAllocatorSetup.GetDataAllocatorSetup<T>(m_gcCount, maxSizeOfNodeArray: m_maxDataArrayLen);
    }

    /// <inheritdoc />
    public IMapAllocatorSetup<T, V> GetMapAllocator<T,V>()
    {
        if (m_mapPoolDefaults.TryGetValue((typeof(T), typeof(V)), out var poolSetup))
        {
            return GcArrayPoolMixedAllocatorSetup.GetMapAllocatorSetup<T, V>(m_gcCount,
                (ArrayPool<int>)poolSetup.bucketPool, 
                (ArrayPool<Data<int>.INode>)poolSetup.bucketNodePool,
                (ArrayPool<Entry<T,V>>)poolSetup.entryBucketPool,
                (ArrayPool<Data<Entry<T,V>>.INode>)poolSetup.entryNodesPool, m_maxDataArrayLen);
        }
        
        return GcArrayPoolMixedAllocatorSetup.GetMapAllocatorSetup<T, V>(m_gcCount, maxSizeOfNodeArray: m_maxDataArrayLen);
    }

    /// <inheritdoc />
    public ISetAllocatorSetup<T> GetSetAllocator<T>()
    {
        if (m_setPoolDefaults.TryGetValue(typeof(T), out var poolSetup))
        {
            return GcArrayPoolMixedAllocatorSetup.GetSetAllocatorSetup<T>(
                m_gcCount,
                (ArrayPool<int>)poolSetup.bucketPool, 
                (ArrayPool<Data<int>.INode>)poolSetup.bucketNodePool,
                (ArrayPool<KeyEntry<T>>)poolSetup.entryBucketPool,
                (ArrayPool<Data<KeyEntry<T>>.INode>) poolSetup.entryNodesPool,
                m_maxDataArrayLen);
        }
        
        return GcArrayPoolMixedAllocatorSetup.GetSetAllocatorSetup<T>(m_gcCount, maxSizeOfNodeArray: m_maxDataArrayLen);
    }
}