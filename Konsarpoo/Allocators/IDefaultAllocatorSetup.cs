namespace Konsarpoo.Collections.Allocators;

/// <summary>
/// Represents Konsarpoo collection allocator abstraction.
/// </summary>
public interface IDefaultAllocatorSetup
{
    /// <summary> Creates Data class allocator for T. </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IDataAllocatorSetup<T> GetDataStorageAllocator<T>();
    
    /// <summary> Creates Map class allocator for TKey, TValue. </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    IMapAllocatorSetup<TKey, TValue> GetMapAllocator<TKey, TValue>();
    
    /// <summary> Creates Set class allocator for T. </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ISetAllocatorSetup<T> GetSetAllocator<T>();
}