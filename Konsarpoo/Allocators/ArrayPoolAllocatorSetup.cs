using System.Buffers;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections.Allocators;

public static class ArrayPoolAllocatorSetup
{
    public static IDataAllocatorSetup<T> GetDataAllocatorSetup<T>(ArrayPool<T> pool = null, ArrayPool<Data<T>.INode> nodePool = null, int? maxDataNodeArrayLength = null) 
    {
        return new DataAllocatorSetup<T>(new ArrayAllocatorAllocator<T>(pool ?? ArrayPool<T>.Shared), new ArrayAllocatorAllocator<Data<T>.INode>(nodePool ?? ArrayPool<Data<T>.INode>.Shared), maxDataNodeArrayLength);
    }
            
    public static IMapAllocatorSetup<T, V> GetMapAllocatorSetup<T,V>(ArrayPool<int> bucketPool = null, ArrayPool<Data<int>.INode> bucketNodePool = null, ArrayPool<Entry<T,V>> entryBucketPool = null, ArrayPool<Data<Entry<T,V>>.INode> entryNodesPool = null, int? maxDataNodeArrayLength = null) 
    {
        return new MapAllocatorSetup<T, V>(ArrayPoolAllocatorSetup.GetDataAllocatorSetup<int>(bucketPool, bucketNodePool, maxDataNodeArrayLength), ArrayPoolAllocatorSetup.GetDataAllocatorSetup<Entry<T,V>>(entryBucketPool, entryNodesPool, maxDataNodeArrayLength));
    }
            
    public static ISetAllocatorSetup<T> GetSetAllocatorSetup<T>(ArrayPool<int> bucketPool = null, ArrayPool<Data<int>.INode> bucketNodePool = null, ArrayPool<KeyEntry<T>> entryBucketPool = null, ArrayPool<Data<KeyEntry<T>>.INode> entryNodesPool = null, int? maxDataNodeArrayLength = null) 
    {
        return new SetAllocatorSetup<T>(ArrayPoolAllocatorSetup.GetDataAllocatorSetup<int>(bucketPool, bucketNodePool, maxDataNodeArrayLength), ArrayPoolAllocatorSetup.GetDataAllocatorSetup<KeyEntry<T>>(entryBucketPool, entryNodesPool, maxDataNodeArrayLength));
    }
}