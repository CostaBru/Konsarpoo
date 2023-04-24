using NUnit.Framework;

namespace Konsarpoo.Collections.Tests;

public enum AllocatorType
{
    GC,
    Pool,
    Mixed
}

public class BaseTest
{
    private readonly int? m_maxSizeOfArrayBucket;
    private readonly AllocatorType m_allocatorType;
    private readonly int m_gcLen;

    public BaseTest(int? maxSizeOfArrayBucket, AllocatorType allocatorType, int gcLen)
    {
        m_maxSizeOfArrayBucket = maxSizeOfArrayBucket;
        m_allocatorType = allocatorType;
        m_gcLen = gcLen;
    }

    [SetUp]
    public void SetUp()
    {
        switch (m_allocatorType)
        {
            case AllocatorType.Mixed:
                KonsarpooAllocatorGlobalSetup.SetGcArrayPoolMixedAllocatorSetup(m_gcLen, m_maxSizeOfArrayBucket);
                break;
                
            case AllocatorType.GC:
                KonsarpooAllocatorGlobalSetup.SetGcAllocatorSetup(m_maxSizeOfArrayBucket);
                break;
                
            case AllocatorType.Pool:
                KonsarpooAllocatorGlobalSetup.SetArrayPoolAllocatorSetup(m_maxSizeOfArrayBucket);
                break;
        }
    }
}