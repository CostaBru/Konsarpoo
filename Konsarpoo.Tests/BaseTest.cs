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
    protected readonly ushort? m_maxSizeOfArrayBucket;
    protected readonly AllocatorType m_allocatorType;
    protected readonly ushort m_gcLen;

    public BaseTest(int? maxSizeOfArrayBucket, AllocatorType allocatorType, int gcLen)
    {
        m_maxSizeOfArrayBucket = (ushort?)maxSizeOfArrayBucket;
        m_allocatorType = allocatorType;
        m_gcLen = (ushort)gcLen;
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