using System;
using System.IO;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests;

[TestFixture(16, AllocatorType.GC, 0)]
[TestFixture(0, AllocatorType.GC, 0)]
[TestFixture(32, AllocatorType.Mixed, 16)]
[TestFixture(16, AllocatorType.Pool, 0)]
[TestFixture(1024, AllocatorType.GC, 0)]
[TestFixture(1024, AllocatorType.Mixed, 512)]
[TestFixture(1024, AllocatorType.Pool, 0)]
public class FileDataTest : BaseTest
{
    public FileDataTest(int? maxSizeOfArrayBucket, AllocatorType allocatorType, int gcLen) : base((ushort)maxSizeOfArrayBucket, allocatorType, (ushort)gcLen)
    {
    }
    
    [Test]
    public void WriteReadTestAfterAdd()
    {
        var tempFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bintemp");

        try
        {
            CreateTestFileAdd(tempFilename, MaxSizeOfArrayBucket);
            
            using var d = new FileData<int>(tempFilename);

            for (int i = 0; i < d.Length; i++)
            {
                var value = d[i];
                
                Assert.AreEqual(i, value);
            }
        }
        finally
        {
            File.Delete(tempFilename);
        }
    }

    private int MaxSizeOfArrayBucket => m_maxSizeOfArrayBucket.Value;


    [Test]
    public void WriteReadTestAfterEsnureSet()
    {
        var tempFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bintemp");

        try
        {
            CreateTestFileSet(tempFilename, MaxSizeOfArrayBucket);
            
            using var d = new FileData<int>(tempFilename);

            for (int i = 0; i < d.Length; i++)
            {
                var value = d[i];
                
                Assert.AreEqual(i, value);
            }
        }
        finally
        {
            File.Delete(tempFilename);
        }
    }

    private static void CreateTestFileAdd(string tempFilename, int maxSizeOfArray)
    {
        using var d = new FileData<int>(tempFilename, 0, maxSizeOfArray, 1);

        d.BeginWrite();
            
        for (int i = 0; i < 1000; i++)
        {
            d.Add(i);
        }
            
        d.EndWrite();
    }
    
    private static void CreateTestFileSet(string tempFilename, int maxSizeOfArray)
    {
        using var d = new FileData<int>(tempFilename, 0, maxSizeOfArray, 1);

        d.BeginWrite();
        
        d.Ensure(1000);
            
        for (int i = 0; i < 1000; i++)
        {
            d[i] = i;
        }
            
        d.EndWrite();
    }
}