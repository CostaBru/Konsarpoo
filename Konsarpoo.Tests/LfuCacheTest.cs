using System.Linq;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests;

[TestFixture]
public class LfuCacheTest
{
    [Test]
    public void BasicTest()
    {
        var lfuCache = new LfuCache<int, int>();

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
        
        lfuCache.RemoveLfuItems();
        
        Assert.AreEqual(2, lfuCache.Count);
        
        Assert.AreEqual(3, lfuCache.GetFrequency(1));
        Assert.AreEqual(2, lfuCache.GetFrequency(2));
        Assert.False(lfuCache.ContainsKey(3));
        
        lfuCache.RemoveLfuItems();
        
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

        lfuCache.RemoveLfuItems(2);
        
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
        
        var lfuCache = new LfuCache<int, int>(mapTemplate, setTemplate);

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
        
        lfuCache.RemoveLfuItems();
        
        Assert.AreEqual(2, lfuCache.Count);
        
        Assert.AreEqual(3, lfuCache.GetFrequency(1));
        Assert.AreEqual(2, lfuCache.GetFrequency(2));
        Assert.False(lfuCache.ContainsKey(3));
        
        lfuCache.RemoveLfuItems();
        
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

        lfuCache.RemoveLfuItems(2);
        
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
}