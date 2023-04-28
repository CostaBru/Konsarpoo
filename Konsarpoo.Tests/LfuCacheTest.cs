using System;
using System.Linq;
using System.Threading;
using Konsarpoo.Collections.Allocators;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests;

[TestFixture(16, AllocatorType.GC, 0)]
[TestFixture(32, AllocatorType.Mixed, 16)]
[TestFixture(16, AllocatorType.Pool, 0)]
[TestFixture(1024, AllocatorType.GC, 0)]
[TestFixture(1024, AllocatorType.Mixed, 512)]
[TestFixture(1024, AllocatorType.Pool, 0)]
public class LfuCacheTest : BaseTest
{
    public LfuCacheTest(int? maxSizeOfArrayBucket, AllocatorType allocatorType, int gcLen) : base(maxSizeOfArrayBucket, allocatorType, gcLen)
    {
    }
    
    [Test]
    public void BasicTest()
    {
        var lfuCache = new LfuCache<int, int>(Enumerable.Range(1, 100).ToArray());

        lfuCache[1] = 1;
        lfuCache[2] = 2;
        lfuCache[3] = 3;
        
        Assert.AreEqual(3, lfuCache.Count);
      
        Assert.AreEqual(1, lfuCache.GetFrequency(1));
        Assert.AreEqual(1, lfuCache.GetFrequency(2));
        Assert.AreEqual(1, lfuCache.GetFrequency(3));

        var i1 = lfuCache[1];
        i1 = lfuCache[1];
        
        Assert.AreEqual(3, lfuCache.GetFrequency(1));
        Assert.AreEqual(1, lfuCache.GetFrequency(2));
        Assert.AreEqual(1, lfuCache.GetFrequency(3));
        
        i1 = lfuCache[2];
        
        Assert.AreEqual(3, lfuCache.GetFrequency(1));
        Assert.AreEqual(2, lfuCache.GetFrequency(2));
        Assert.AreEqual(1, lfuCache.GetFrequency(3));
        
        lfuCache.RemoveLeastUsedItems();
        
        Assert.AreEqual(2, lfuCache.Count);
        
        Assert.AreEqual(3, lfuCache.GetFrequency(1));
        Assert.AreEqual(2, lfuCache.GetFrequency(2));
        Assert.False(lfuCache.ContainsKey(3));
        
        lfuCache.RemoveLeastUsedItems();
        
        Assert.AreEqual(1, lfuCache.Count);
        
        Assert.AreEqual(3, lfuCache.GetFrequency(1));
        Assert.False(lfuCache.ContainsKey(2));
        Assert.False(lfuCache.ContainsKey(3));
        
        lfuCache[2] = 2;
        lfuCache[3] = 3;
        
        Assert.AreEqual(3, lfuCache.Count);
        
        lfuCache.Clear();
        
        Assert.AreEqual(0, lfuCache.Count);
        
        lfuCache[1] = 1;
        lfuCache[2] = 2;
        lfuCache[3] = 3;

        var i = lfuCache[1]; i = lfuCache[1];
        var i2 = lfuCache[2];

        lfuCache.RemoveLeastUsedItems(2);
        
        Assert.AreEqual(1, lfuCache.Count);
        Assert.AreEqual(1, lfuCache[1]);
        
        lfuCache.Dispose();
        Assert.AreEqual(0, lfuCache.Count);
    }
    
    [Test]
    public void CustomAllocatorTest()
    {
        var mapTemplate = new Map<int, LfuCache<int, int>.DataVal>(0, 16, GcAllocatorSetup.GetMapPoolSetup<int, LfuCache<int, int>.DataVal>());
        var setTemplate = new Set<int>(0, 16, GcAllocatorSetup.GetSetPoolSetup<int>());
        
        var lfuCache = new LfuCache<int, int>(mapTemplate, setTemplate, Enumerable.Range(1, 100).ToArray());

        lfuCache[1] = 1;
        lfuCache[2] = 2;
        lfuCache[3] = 3;
        
        Assert.AreEqual(3, lfuCache.Count);
      
        Assert.AreEqual(1, lfuCache.GetFrequency(1));
        Assert.AreEqual(1, lfuCache.GetFrequency(2));
        Assert.AreEqual(1, lfuCache.GetFrequency(3));

        var i1 = lfuCache[1];
        i1 = lfuCache[1];
        
        Assert.AreEqual(3, lfuCache.GetFrequency(1));
        Assert.AreEqual(1, lfuCache.GetFrequency(2));
        Assert.AreEqual(1, lfuCache.GetFrequency(3));
        
        i1 = lfuCache[2];
        
        Assert.AreEqual(3, lfuCache.GetFrequency(1));
        Assert.AreEqual(2, lfuCache.GetFrequency(2));
        Assert.AreEqual(1, lfuCache.GetFrequency(3));
        
        lfuCache.RemoveLeastUsedItems();
        
        Assert.AreEqual(2, lfuCache.Count);
        
        Assert.AreEqual(3, lfuCache.GetFrequency(1));
        Assert.AreEqual(2, lfuCache.GetFrequency(2));
        Assert.False(lfuCache.ContainsKey(3));
        
        lfuCache.RemoveLeastUsedItems();
        
        Assert.AreEqual(1, lfuCache.Count);
        
        Assert.AreEqual(3, lfuCache.GetFrequency(1));
        Assert.False(lfuCache.ContainsKey(2));
        Assert.False(lfuCache.ContainsKey(3));
        
        lfuCache[2] = 2;
        lfuCache[3] = 3;
        
        Assert.AreEqual(3, lfuCache.Count);
        
        lfuCache.Clear();
        
        Assert.AreEqual(0, lfuCache.Count);
        
        lfuCache[1] = 1;
        lfuCache[2] = 2;
        lfuCache[3] = 3;

        var i = lfuCache[1]; i = lfuCache[1];
        var i2 = lfuCache[2];

        lfuCache.RemoveLeastUsedItems(2);
        
        Assert.AreEqual(1, lfuCache.Count);
        Assert.AreEqual(1, lfuCache[1]);
        
        lfuCache.Dispose();
        Assert.AreEqual(0, lfuCache.Count);
    }

    [Test]
    public void TestRemove()
    {
        var lfuCache = new LfuCache<int, int>();

        lfuCache[1] = 1;
        lfuCache[2] = 2;
        lfuCache[3] = 3;

      
        Assert.True(lfuCache.RemoveKey(1));
        Assert.False(lfuCache.RemoveKey(0));

        ref var valueByRef = ref lfuCache.ValueByRef(2, out var success);
        
        Assert.True(success);
        Assert.AreEqual(2, valueByRef);

        valueByRef = 5;
        
        Assert.AreEqual(5, lfuCache[2]);

        var val = 1;
        
        var bucketIndex = lfuCache.GetBucketIndex(ref val);
                
        Assert.True(bucketIndex >= 0);
                
        Assert.True(bucketIndex < lfuCache.BucketCount);
    }
    
    [Test]
    public void TestCacheStringSerialization()
    {
        var lfuCache = new LfuCache<int, int>();

        var range = Enumerable.Range(0, 10000);

        foreach (var i in range)
        {
            lfuCache[i] = i;
        }
        
        var serializeWithDcs = SerializeHelper.SerializeWithDcs(lfuCache);

        var deserializeWithDcs = SerializeHelper.DeserializeWithDcs<LfuCache<int, int>>(serializeWithDcs);

        var deepEquals = deserializeWithDcs.DeepEquals(lfuCache);
        
        Assert.True(deepEquals);
    }

    [Test]
    public void TestCacheBinarySerialization()
    {
        var lfuCache = new LfuCache<int, int>();

        var range = Enumerable.Range(0, 10000);

        foreach (var i in range)
        {
            lfuCache[i] = i;
        }

        for (int i = 0; i < 5; i++)
        {
            var i1 = lfuCache[50];
        }

        var deserializeWithDcs = lfuCache.Copy();

        var deepEquals = deserializeWithDcs.DeepEquals(lfuCache);
        
        Assert.True(deepEquals);
    }
    
    private class DataProvider
    {
        public int GetData()
        {
            return 1;
        }
    }
    
    [Test]
    public void TestCacheGetSet()
    {
        var lfuCache = new LfuCache<int, int>();

        var dataProvider = new DataProvider();

        Assert.AreEqual(1, lfuCache.GetSet(1, dataProvider, (p, k, cache) => cache.Put(k, p.GetData())));

        Assert.AreEqual(1, lfuCache[1]);

        lfuCache.Get(1, out var val);
        
        Assert.AreEqual(1, val);
    }
    
     
    [Test]
    public void TestObsolescence()
    {
        var lfuCache = new LfuCache<int, int>();

        var mockStopwatch = new MockStopwatch();

        lfuCache.StartTrackingObsolescence(mockStopwatch, TimeSpan.FromMilliseconds(5));

        mockStopwatch.Elapsed = TimeSpan.FromMilliseconds(1);
        
        lfuCache[1] = 1;
        lfuCache[2] = 1;
        lfuCache[3] = 1;
        
        lfuCache[1] = 2;
        lfuCache[2] = 2;
        lfuCache[3] = 2;
        
        Assert.AreEqual(0, lfuCache.ScanForObsolescence());
        
        mockStopwatch.Elapsed = TimeSpan.FromMilliseconds(100);
        
        Assert.AreEqual(3, lfuCache.ScanForObsolescence());
        
        lfuCache[4] = 1;
        lfuCache[5] = 1;
        lfuCache[6] = 1;
        lfuCache[7] = 1;

        Assert.AreEqual(0, lfuCache.ScanForObsolescence());
        
        Assert.AreEqual(3, lfuCache.ObsoleteKeysCount);

        Assert.AreEqual(3, lfuCache.RemoveObsoleteItems());
        
        Assert.AreEqual(4, lfuCache.Count);
        
        Assert.False(lfuCache.ContainsKey(1));
        Assert.False(lfuCache.ContainsKey(2));
        Assert.False(lfuCache.ContainsKey(3));
        
        Assert.True(lfuCache.ContainsKey(4));
        Assert.True(lfuCache.ContainsKey(5));
        Assert.True(lfuCache.ContainsKey(6));
        Assert.True(lfuCache.ContainsKey(7));
        
        lfuCache.StopTrackingObsolescence();
        
        mockStopwatch.Elapsed += TimeSpan.FromMilliseconds(100);
        
        Assert.AreEqual(0, lfuCache.ScanForObsolescence());
        Assert.AreEqual(0, lfuCache.RemoveObsoleteItems());
        
        Assert.True(lfuCache.ContainsKey(4));
        Assert.True(lfuCache.ContainsKey(5));
        Assert.True(lfuCache.ContainsKey(6));
        Assert.True(lfuCache.ContainsKey(7));
        
        lfuCache.ResetObsolescence();
        
        mockStopwatch = new MockStopwatch();
        
        lfuCache.StartTrackingObsolescence(mockStopwatch, TimeSpan.FromMilliseconds(10));
        
        lfuCache[4] = 1;
        lfuCache[5] = 1;
        lfuCache[6] = 1;
        lfuCache[7] = 1;
        
        lfuCache[4] = 1;
        lfuCache[5] = 1;
        
        lfuCache[5] = 1;
        
        mockStopwatch.Elapsed = TimeSpan.FromMilliseconds(100);
        
        lfuCache[7] = 1;

        var scanFrequentForObsolescence = lfuCache.ScanFrequentForObsolescence(3);
        
        Assert.AreEqual(2, scanFrequentForObsolescence);
        Assert.AreEqual(2, lfuCache.ObsoleteKeysCount);
        Assert.AreEqual(2, lfuCache.RemoveObsoleteItems());
        
        Assert.True(lfuCache.ContainsKey(6));
        Assert.True(lfuCache.ContainsKey(7));
    }

    private class SomeCacheVal : IDisposable
    {
        public SomeCacheVal(int someValue)
        {
            SomeValue = someValue;
        }

        public bool IsDisposed {get; set; }
        
        public int SomeValue { get; set; }
        
        public void Dispose()
        {
            IsDisposed = true;
        }

        public SomeCacheVal Copy()
        {
            return (SomeCacheVal)this.MemberwiseClone();
        }
    }
    
    [Test]
    public void TestDisposeAndCopyStrategy()
    {
        var cache = new LfuCache<int, SomeCacheVal>(copyStrategy: (v) => v.Copy());

        SomeCacheVal v1;
        SomeCacheVal v2;
        SomeCacheVal v3;

        cache[1] = v1 = new SomeCacheVal(1);
        cache[2] = v2 = new SomeCacheVal(2);
        cache[3] = v3 = new SomeCacheVal(3);
        
        Assert.False(ReferenceEquals(v1, cache[1]));
        Assert.False(ReferenceEquals(v2, cache[2]));
        Assert.False(ReferenceEquals(v3, cache[3]));
        
        var vv1= cache[1];
        var vv2= cache[2];
        var vv3 = cache[3];

        Assert.False(ReferenceEquals(vv1, cache[1]));
        Assert.False(ReferenceEquals(vv2, cache[2]));
        Assert.False(ReferenceEquals(vv3, cache[3]));

        ref var someCacheVal = ref cache.ValueByRef(1, out var s);

        cache.RemoveKey(1);
        
        Assert.True(someCacheVal.IsDisposed);
        
        Assert.False(v1.IsDisposed);
        Assert.False(v2.IsDisposed);
        Assert.False(v3.IsDisposed);
           
        Assert.False(vv1.IsDisposed);
        Assert.False(vv2.IsDisposed);
        Assert.False(vv3.IsDisposed);
        
        Assert.AreEqual(2, cache.Count);
        Assert.True(cache.ContainsKey(2));
        Assert.True(cache.ContainsKey(3));
    }
}