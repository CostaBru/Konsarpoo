using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections;

public class SetAllocatorSetup<TKey> : ISetAllocatorSetup<TKey>
{
    private readonly IDataAllocatorSetup<int> m_dataAllocatorSetup;
    private readonly IDataAllocatorSetup<KeyEntry<TKey>> m_bucketsAllocatorSetup;

    public SetAllocatorSetup(IDataAllocatorSetup<int> dataAllocatorSetup, IDataAllocatorSetup<KeyEntry<TKey>> bucketsAllocatorSetup)
    {
        m_dataAllocatorSetup = dataAllocatorSetup;
        m_bucketsAllocatorSetup = bucketsAllocatorSetup;
    }

    public IDataAllocatorSetup<int> GetBucketsAllocatorSetup() => m_dataAllocatorSetup; 

    public IDataAllocatorSetup<KeyEntry<TKey>> GeStorageAllocatorSetup() => m_bucketsAllocatorSetup; 
}