using System.Buffers;

namespace Konsarpoo.Collections.Allocators;

public static class ArrayPoolAllocatorSetup
{
    public static IDataAllocatorSetup<T> GetDataAllocatorSetup<T>(ArrayPool<T> pool = null, ArrayPool<Data<T>.INode> nodePool = null, int? maxDataNodeArrayLength = null) 
    {
        return new DataAllocatorSetup<T>(new ArrayAllocatorAllocator<T>(pool ?? ArrayPool<T>.Shared), new ArrayAllocatorAllocator<Data<T>.INode>(nodePool ?? ArrayPool<Data<T>.INode>.Shared), maxDataNodeArrayLength);
    }
            
    public static IMapAllocatorSetup<T, V> GetMapAllocatorSetup<T,V>(ArrayPool<int> bucketPool = null, ArrayPool<Data<int>.INode> bucketNodePool = null, ArrayPool<Map<T,V>.Entry> entryBucketPool = null, ArrayPool<Data<Map<T,V>.Entry>.INode> entryNodesPool = null, int? maxDataNodeArrayLength = null) 
    {
        return new MapAllocatorSetup<T, V>(ArrayPoolAllocatorSetup.GetDataAllocatorSetup<int>(bucketPool, bucketNodePool, maxDataNodeArrayLength), ArrayPoolAllocatorSetup.GetDataAllocatorSetup<Map<T, V>.Entry>(entryBucketPool, entryNodesPool, maxDataNodeArrayLength));
    }
            
    public static ISetAllocatorSetup<T> GetSetAllocatorSetup<T>(ArrayPool<int> bucketPool = null, ArrayPool<Data<int>.INode> bucketNodePool = null, ArrayPool<Set<T>.Slot> entryBucketPool = null, ArrayPool<Data<Set<T>.Slot>.INode> entryNodesPool = null, int? maxDataNodeArrayLength = null) 
    {
        return new SetAllocatorSetup<T>(ArrayPoolAllocatorSetup.GetDataAllocatorSetup<int>(bucketPool, bucketNodePool, maxDataNodeArrayLength), ArrayPoolAllocatorSetup.GetDataAllocatorSetup<Set<T>.Slot>(entryBucketPool, entryNodesPool, maxDataNodeArrayLength));
    }
}