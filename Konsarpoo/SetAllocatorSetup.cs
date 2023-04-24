using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections;

public class SetAllocatorSetup<TKey> : ISetAllocatorSetup<TKey>
{
    private readonly IDataAllocatorSetup<int> m_dataAllocatorSetup;
    private readonly IDataAllocatorSetup<Set<TKey>.Slot> m_bucketsAllocatorSetup;

    public SetAllocatorSetup(IDataAllocatorSetup<int> dataAllocatorSetup, IDataAllocatorSetup<Set<TKey>.Slot> bucketsAllocatorSetup)
    {
        m_dataAllocatorSetup = dataAllocatorSetup;
        m_bucketsAllocatorSetup = bucketsAllocatorSetup;
    }

    public IDataAllocatorSetup<int> GetBucketsAllocatorSetup() => m_dataAllocatorSetup; 

    public IDataAllocatorSetup<Set<TKey>.Slot> GeStorageAllocatorSetup() => m_bucketsAllocatorSetup; 
}