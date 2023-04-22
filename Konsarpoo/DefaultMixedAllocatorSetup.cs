using System.Buffers;

namespace Konsarpoo.Collections;

public static class DefaultMixedAllocatorSetup
{
    public static IDataAllocatorSetup<T> GetDataPoolSetup<T>(int gcCount = 64, ArrayPool<T> pool = null, ArrayPool<Data<T>.INode> nodePool = null) 
    {
        return new DataAllocatorSetup<T>(new DefaultMixedAllocator<T>(pool, gcCount), new DefaultMixedAllocator<Data<T>.INode>(nodePool, gcCount));
    }
            
    public static IMapAllocatorSetup<T, V> GetMapPoolSetup<T, V>(int gcCount = 64, ArrayPool<int> bucketPool = null, ArrayPool<Map<T,V>.Entry> entryBucketPool = null, ArrayPool<Data<Map<T,V>.Entry>.INode> entryNodesPool = null)
    {
        return new MapAllocatorSetup<T, V>(DefaultMixedAllocatorSetup.GetDataPoolSetup<int>(gcCount, bucketPool), DefaultMixedAllocatorSetup.GetDataPoolSetup<Map<T, V>.Entry>(gcCount, entryBucketPool, entryNodesPool));
    }
            
    public static ISetAllocatorSetup<T> GetSetPoolSetup<T>(int gcCount = 64, ArrayPool<int> bucketPool = null, ArrayPool<Set<T>.Slot> entryBucketPool = null, ArrayPool<Data<Set<T>.Slot>.INode> entryNodesPool = null)
    {
        return new SetAllocatorSetup<T>(DefaultMixedAllocatorSetup.GetDataPoolSetup<int>(gcCount, bucketPool), DefaultMixedAllocatorSetup.GetDataPoolSetup<Set<T>.Slot>(gcCount, entryBucketPool, entryNodesPool));
    }
}