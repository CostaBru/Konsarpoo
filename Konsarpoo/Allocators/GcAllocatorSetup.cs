﻿using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections.Allocators;

public static class GcAllocatorSetup
{
    public static IDataAllocatorSetup<T> GetDataPoolSetup<T>(int? maxDataArrayLength = null)
    {
        maxDataArrayLength = maxDataArrayLength == null || maxDataArrayLength <= 0 ? GcAllocator<T>.SmallHeapSuitableLength : maxDataArrayLength;
        
        return new DataAllocatorSetup<T>(new GcAllocator<T>(), new GcAllocator<Data<T>.INode>(), maxDataArrayLength);
    }
            
    public static IMapAllocatorSetup<T, V> GetMapPoolSetup<T,V>(int? maxDataArrayLength = null) 
    {
        maxDataArrayLength ??= GcAllocator<T>.SmallHeapSuitableLength;
        
        return new MapAllocatorSetup<T, V>(GcAllocatorSetup.GetDataPoolSetup<int>(maxDataArrayLength), GcAllocatorSetup.GetDataPoolSetup<Entry<T,V>>(maxDataArrayLength));
    }
            
    public static ISetAllocatorSetup<T> GetSetPoolSetup<T>(int? maxDataArrayLength = null) 
    {
        maxDataArrayLength ??= GcAllocator<T>.SmallHeapSuitableLength;
        
        return new SetAllocatorSetup<T>(GcAllocatorSetup.GetDataPoolSetup<int>(maxDataArrayLength), GcAllocatorSetup.GetDataPoolSetup<KeyEntry<T>>(maxDataArrayLength));
    }
}