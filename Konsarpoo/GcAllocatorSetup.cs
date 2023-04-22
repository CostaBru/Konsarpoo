namespace Konsarpoo.Collections;

public static class GcAllocatorSetup
{
    public static IDataAllocatorSetup<T> GetDataPoolSetup<T>() 
    {
        return new DataAllocatorSetup<T>(new GcAllocator<T>(), new GcAllocator<Data<T>.INode>());
    }
            
    public static IMapAllocatorSetup<T, V> GetMapPoolSetup<T,V>() 
    {
        return new MapAllocatorSetup<T, V>(GcAllocatorSetup.GetDataPoolSetup<int>(), GcAllocatorSetup.GetDataPoolSetup<Map<T, V>.Entry>());
    }
            
    public static ISetAllocatorSetup<T> GetSetPoolSetup<T>() 
    {
        return new SetAllocatorSetup<T>(GcAllocatorSetup.GetDataPoolSetup<int>(), GcAllocatorSetup.GetDataPoolSetup<Set<T>.Slot>());
    }
}