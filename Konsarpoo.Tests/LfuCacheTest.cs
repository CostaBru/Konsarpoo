﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        var i = lfuCache[1];
        i = lfuCache[1];
        var i2 = lfuCache[2];

        lfuCache.RemoveLeastUsedItems(2);
        
        Assert.AreEqual(1, lfuCache.Count);
        Assert.AreEqual(1, lfuCache[1]);
        
        lfuCache.Dispose();
        Assert.AreEqual(0, lfuCache.Count);
    }
    
    private class ConcurrentHashset<TKey> : ICollection<TKey>
    {
        private readonly ConcurrentDictionary<TKey, byte> m_dict;

        public ConcurrentHashset() : this(null)
        {
        }

        public ConcurrentHashset(IEqualityComparer<TKey> comparer)
        {
            m_dict = new ConcurrentDictionary<TKey, byte>(comparer);
        }

        public IEnumerator<TKey> GetEnumerator()
        {
            return m_dict.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TKey item)
        {
            m_dict[item] = 1;
        }

        public void Clear()
        {
            m_dict.Clear();
        }

        public bool Contains(TKey item)
        {
            return m_dict.ContainsKey(item);
        }

        public void CopyTo(TKey[] array, int arrayIndex)
        {
            m_dict.Keys.CopyTo(array, arrayIndex);
        }

        public bool Remove(TKey item)
        {
            return m_dict.TryRemove(item, out var val);
        }

        public int Count => m_dict.Count;

        public bool IsReadOnly => false;
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

    [Test, Ignore("This test is not reliable")]
    public void TestConcurrentAccess()
    {
        var lfuCache = new LfuCache<string, object>();

        lfuCache.StartTrackingObsolescence(new SystemStopwatch(), TimeSpan.FromMilliseconds(1));
        
        var updater1 = new Thread(() =>
        {
            var random = new Random(0);
            
            for (int i = 1; i < 100000; i += 2)
            {
                for (int j = 0; j < random.Next(0, 10); j++)
                {
                    lfuCache.GetSet(i.ToString(), (key, cache) => cache[key] = key);
                }
            }
        });
        
        var updater2 = new Thread(() =>
        {
            var random = new Random(0);
            
            for (int i = 0; i < 100000; i += 2)
            {
                for (int j = 0; j < random.Next(0, 10); j++)
                {
                    lfuCache.GetSet(i.ToString(), (key, cache) => cache[key] = key);
                }
            }
        });

        var deleter1CancellationToken = false;

        var deleter1 = new Thread(() =>
        {
            var random = new Random(0);

            while (deleter1CancellationToken == false)
            {
                try
                {
                    var next = random.Next(0, 100000);

                    lfuCache.RemoveKey(next.ToString());
                }
                catch (ThreadInterruptedException e)
                {
                    return;
                }
            }
        });
        
        var deleter2CancellationToken = false;
        
        var deleter2 = new Thread(() =>
        {
            var random = new Random(0);

            while (deleter2CancellationToken == false)
            {
                try
                {
                    var next = random.Next(10000, 100000);

                    lfuCache.RemoveKey(next.ToString());
                }
                catch (ThreadInterruptedException e)
                {
                    return;
                }
            }
        });
        
        var getter1CancellationToken = false;
        
        var getter1 = new Thread(() =>
        {
            var random = new Random(0);

            while (getter1CancellationToken == false)
            {
                try
                {
                    var next = random.Next(10000, 100000);

                    lfuCache.GetSet(next.ToString(), (key, cache) => cache[key] = key);
                }
                catch (ThreadInterruptedException e)
                {
                    return;
                }
            }
        });
        
        var getter2CancellationToken = false;
        
        var getter2 = new Thread(() =>
        {
            var random = new Random(0);

            while (getter2CancellationToken == false)
            {
                try
                {
                    var next = random.Next(10000, 100000);

                    lfuCache.GetSet(next.ToString(), (key, cache) => cache[key] = key);
                }
                catch (ThreadInterruptedException e)
                {
                   return;
                }
            }
        });
        
        var cleaner1CancellationToken = false;
        
        var cleaner1 = new Thread(() =>
        {
            while (cleaner1CancellationToken == false)
            {
                try
                {
                    if (lfuCache.Count > 10)
                    {
                        lfuCache.RemoveLeastUsedItems(5);
                    }
                }
                catch (ThreadInterruptedException e)
                {
                   return;
                }
            }
        });
        
        var cleaner2CancellationToken = false;
        
        var cleaner2 = new Thread(() =>
        {
            while (cleaner2CancellationToken == false)
            {
                try
                {
                    if (lfuCache.Count > 10)
                    {
                        lfuCache.ScanFrequentForObsolescence(10);
                        lfuCache.RemoveObsoleteItems();
                    }
                }
                catch (ThreadInterruptedException e)
                {
                   return;
                }
            }
        });
        
        updater1.Start();
        updater2.Start();
        cleaner1.Start();
        cleaner2.Start();
        deleter1.Start();
        deleter2.Start();
        getter1.Start();
        getter2.Start();

        updater1.Join();
        updater2.Join();

        cleaner1CancellationToken = true;
        cleaner2CancellationToken = true;
        deleter1CancellationToken = true;
        deleter2CancellationToken = true;
        getter1CancellationToken = true;
        getter2CancellationToken = true;

        cleaner1.Join();
        cleaner2.Join();
        deleter1.Join();
        deleter2.Join();
        getter1.Join();
        getter2.Join();
        
        lfuCache.ScanForObsolescence();
        lfuCache.RemoveObsoleteItems();
        
        Assert.AreEqual(0, lfuCache.Count);
    }
}