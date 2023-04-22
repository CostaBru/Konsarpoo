using System.Buffers;

namespace Konsarpoo.Collections;

public static class ArrayPoolAllocatorSetup
{
    public static IDataAllocatorSetup<T> GetDataPoolSetup<T>(ArrayPool<T> pool = null, ArrayPool<Data<T>.INode> nodePool = null) 
    {
        return new DataAllocatorSetup<T>(new ArrayPoolAllocator<T>(pool), new ArrayPoolAllocator<Data<T>.INode>(nodePool));
    }
            
    public static IMapAllocatorSetup<T, V> GetMapPoolSetup<T,V>(ArrayPool<int> bucketPool = null, ArrayPool<Map<T,V>.Entry> entryBucketPool = null, ArrayPool<Data<Map<T,V>.Entry>.INode> entryNodesPool = null) 
    {
        return new MapAllocatorSetup<T, V>(ArrayPoolAllocatorSetup.GetDataPoolSetup<int>(bucketPool), ArrayPoolAllocatorSetup.GetDataPoolSetup<Map<T, V>.Entry>(entryBucketPool, entryNodesPool));
    }
            
    public static ISetAllocatorSetup<T> GetSetPoolSetup<T>(ArrayPool<int> bucketPool = null, ArrayPool<Set<T>.Slot> entryBucketPool = null, ArrayPool<Data<Set<T>.Slot>.INode> entryNodesPool = null) 
    {
        return new SetAllocatorSetup<T>(ArrayPoolAllocatorSetup.GetDataPoolSetup<int>(bucketPool), ArrayPoolAllocatorSetup.GetDataPoolSetup<Set<T>.Slot>(entryBucketPool, entryNodesPool));
    }
}