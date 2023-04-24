using System.Buffers;

namespace Konsarpoo.Collections.Allocators;

public static class GcArrayPoolMixedAllocatorSetup
{
    public static IDataAllocatorSetup<T> GetDataAllocatorSetup<T>(int gcCount = 64, ArrayPool<T> pool = null, ArrayPool<Data<T>.INode> nodePool = null, int? maxSizeOfNodeArray = null) 
    {
        return new DataAllocatorSetup<T>(new GcArrayPoolMixedAllocator<T>(pool ?? ArrayPool<T>.Shared, gcCount), new GcArrayPoolMixedAllocator<Data<T>.INode>(nodePool ?? ArrayPool<Data<T>.INode>.Shared, gcCount), maxSizeOfNodeArray);
    }
            
    public static IMapAllocatorSetup<T, V> GetMapAllocatorSetup<T, V>(int gcCount = 64, ArrayPool<int> bucketPool = null, ArrayPool<Data<int>.INode> bucketNodePool = null, ArrayPool<Map<T,V>.Entry> entryBucketPool = null, ArrayPool<Data<Map<T,V>.Entry>.INode> entryNodesPool = null, int? maxSizeOfNodeArray = null)
    {
        return new MapAllocatorSetup<T, V>(GcArrayPoolMixedAllocatorSetup.GetDataAllocatorSetup<int>(gcCount, bucketPool, bucketNodePool, maxSizeOfNodeArray), GcArrayPoolMixedAllocatorSetup.GetDataAllocatorSetup<Map<T, V>.Entry>(gcCount, entryBucketPool, entryNodesPool, maxSizeOfNodeArray));
    }
            
    public static ISetAllocatorSetup<T> GetSetAllocatorSetup<T>(int gcCount = 64, ArrayPool<int> bucketPool = null, ArrayPool<Data<int>.INode> bucketNodePool = null, ArrayPool<Set<T>.Slot> entryBucketPool = null, ArrayPool<Data<Set<T>.Slot>.INode> entryNodesPool = null, int? maxSizeOfNodeArray = null)
    {
        return new SetAllocatorSetup<T>(GcArrayPoolMixedAllocatorSetup.GetDataAllocatorSetup<int>(gcCount, bucketPool, bucketNodePool, maxSizeOfNodeArray), GcArrayPoolMixedAllocatorSetup.GetDataAllocatorSetup<Set<T>.Slot>(gcCount, entryBucketPool, entryNodesPool, maxSizeOfNodeArray));
    }
}