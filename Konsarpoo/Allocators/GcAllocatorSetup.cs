namespace Konsarpoo.Collections.Allocators;

public static class GcAllocatorSetup
{
    public static IDataAllocatorSetup<T> GetDataPoolSetup<T>(int? maxDataArrayLength = null)
    {
        maxDataArrayLength ??= GcAllocator<T>.SmallHeapSuitableLength;
        
        return new DataAllocatorSetup<T>(new GcAllocator<T>(), new GcAllocator<Data<T>.INode>(), maxDataArrayLength);
    }
            
    public static IMapAllocatorSetup<T, V> GetMapPoolSetup<T,V>(int? maxDataArrayLength = null) 
    {
        maxDataArrayLength ??= GcAllocator<T>.SmallHeapSuitableLength;
        
        return new MapAllocatorSetup<T, V>(GcAllocatorSetup.GetDataPoolSetup<int>(maxDataArrayLength), GcAllocatorSetup.GetDataPoolSetup<Map<T, V>.Entry>(maxDataArrayLength));
    }
            
    public static ISetAllocatorSetup<T> GetSetPoolSetup<T>(int? maxDataArrayLength = null) 
    {
        maxDataArrayLength ??= GcAllocator<T>.SmallHeapSuitableLength;
        
        return new SetAllocatorSetup<T>(GcAllocatorSetup.GetDataPoolSetup<int>(maxDataArrayLength), GcAllocatorSetup.GetDataPoolSetup<Set<T>.Slot>(maxDataArrayLength));
    }
}