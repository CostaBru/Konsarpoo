using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Konsarpoo.Collections.Stackalloc;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests.Stackalloc;

[TestFixture(16)]
[TestFixture(32)]
[TestFixture(1000)]
public class MapStructTest
{
    public int N { get; set; }

    public MapStructTest(int capacity)
    {
        N = capacity;
    }
    
    [Test]
    public void TestAggregate()
    {
        Span<int> buckets = stackalloc int[N];
        Span<Entry<int, int>> entriesHash = stackalloc Entry<int, int>[N];

        var map = new MapRs<int, int>(ref buckets, ref entriesHash, EqualityComparer<int>.Default);

        map.Add(1, 1);
        map.Add(2, 2);
        map.Add(3, 3);
        map.Add(4, 4);
        map.Add(5, 5);
        
        Assert.AreEqual(1, map.KeyAt(0));

        var keys = new Data<int>();
        var values = new Data<int>();

        map.ForEachKey((k) => { keys.Add(k);});
        map.ForEachValue((v) => { values.Add(v);});

        Assert.True(keys.SequenceEqual(Enumerable.Range(1, 5)));
        Assert.AreEqual(keys, values);
        
        keys.Clear();
        values.Clear();
        
        map.AggregateKeys(keys, (kk, k) => { kk.Add(k);});
        map.AggregateValues(values, (vv, v) => { vv.Add(v);});
        
        Assert.True(keys.SequenceEqual(Enumerable.Range(1, 5)));
        Assert.AreEqual(keys, values);
        
        Assert.AreEqual(new KeyValuePair<int,int>(1,1), map.FirstOrDefault());
        Assert.AreEqual(new KeyValuePair<int,int>(5,5), map.LastOrDefault());
        
        keys.Clear();
        values.Clear();
        
        map.WhereForEach((k, v) => k == v, (k, v) =>
        {
            keys.Add(k);
            values.Add(v);
        });
        
        Assert.True(keys.SequenceEqual(Enumerable.Range(1, 5)));
        Assert.AreEqual(keys, values);
        
        keys.Clear();
        
        map.WhereAggregate(keys, (kk, k, v) => k == v, (kk, k, v) =>
        {
            kk.Add(k);
        });
        
        Assert.True(keys.SequenceEqual(Enumerable.Range(1, 5)));
        Assert.AreEqual(keys, values);
    }
    

    [Test]
    public void TestRemoveIfEmpty()
    {
        Span<int> buckets = stackalloc int[N];
        Span<Entry<int, int>> entriesHash = stackalloc Entry<int, int>[N];

        var map = new MapRs<int, int>(ref buckets, ref entriesHash);

        Assert.False(map.Remove(0));
    }

    [Test]
    public void TestSmall()
    {
        Span<int> buckets = stackalloc int[N];
        Span<Entry<int, TimeSpan>> entriesHash = stackalloc Entry<int, TimeSpan>[N];
        
        var map = new MapRs<int, TimeSpan>(ref buckets, ref entriesHash);
        var dict = new Dictionary<int, TimeSpan>();

        map.Add(0, TimeSpan.FromMilliseconds(1));
        map.Add(1, TimeSpan.FromMilliseconds(2));

        Assert.True(map.ContainsValue(TimeSpan.FromMilliseconds(1)));
        Assert.True(map.ContainsValue(TimeSpan.FromMilliseconds(2)));
        Assert.False(map.ContainsValue(TimeSpan.FromMilliseconds(3)));

        var dict2 = map.ToMap();

        dict2.Add(3, TimeSpan.FromMilliseconds(3));
        dict.Add(0, TimeSpan.FromMilliseconds(1));
        dict.Add(1, TimeSpan.FromMilliseconds(2));

        Assert.AreEqual(dict.Count, map.Length);

        Assert.AreEqual(TimeSpan.FromMilliseconds(1), map[0]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(2), map[1]);

       // Assert.True(map.Equals(map.ToMap()));

        Test(dict, ref map);

        var map2 = _.Map((1, 1));

        map2.Append(new KeyValuePair<int, int>(2, 2));

        Assert.True(map2[2] == 2);
        Assert.True(map2[1] == 1);

        for (int i = 0; i < map2.Count; i++)
        {
            var keyAt = map2.KeyAt(i);

            Assert.True(map2[keyAt] == keyAt);
        }

        Assert.NotNull(map2.Comparer);
    }


    [Test]
    public void TestAddHuge()
    {
        Span<int> buckets = stackalloc int[N];
        Span<Entry<int, TimeSpan>> entriesHash = stackalloc Entry<int, TimeSpan>[N];
        var testData = new MapRs<int, TimeSpan>(ref buckets, ref entriesHash);
        
        for (int i = 0; i < N; i++)
        {
            testData.Add(i, TimeSpan.FromMilliseconds(i));

            Assert.True(testData.ContainsKey(i));
            Assert.True(testData.ContainsValue(TimeSpan.FromMilliseconds(i)));
        }
    }

    [Test]
    public void TestCommon()
    {
        Span<int> buckets = stackalloc int[N];
        Span<Entry<int, TimeSpan>> entriesHash = stackalloc Entry<int, TimeSpan>[N];
        var map = new MapRs<int, TimeSpan>(ref buckets, ref entriesHash);
        
        var dict = new Dictionary<int, TimeSpan>();

        map.Add(1, TimeSpan.FromMilliseconds(1));
        map.Add(2, TimeSpan.FromMilliseconds(2));
        map.Add(3, TimeSpan.FromMilliseconds(3));
        map.Add(4, TimeSpan.FromMilliseconds(4));
        map.Add(5, TimeSpan.FromMilliseconds(5));
        map.Add(6, TimeSpan.FromMilliseconds(6));
        map.Add(7, TimeSpan.FromMilliseconds(7));

       
        dict.Add(1, TimeSpan.FromMilliseconds(1));
        dict.Add(2, TimeSpan.FromMilliseconds(2));
        dict.Add(3, TimeSpan.FromMilliseconds(3));
        dict.Add(4, TimeSpan.FromMilliseconds(4));
        dict.Add(5, TimeSpan.FromMilliseconds(5));
        dict.Add(6, TimeSpan.FromMilliseconds(6));
        dict.Add(7, TimeSpan.FromMilliseconds(7));

        Assert.AreEqual(TimeSpan.FromMilliseconds(1), map[1]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(2), map[2]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(3), map[3]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(4), map[4]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(5), map[5]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(6), map[6]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(7), map[7]);

        Test(dict, ref map);
    }

    [Test]
    public void TestAdd2([Values(0, 1, 2, 1000)] int count)
    {
        if (count > N)
        {
            return;
        }
        
        Span<int> buckets = stackalloc int[N];
        Span<Entry<int, int>> entriesHash = stackalloc Entry<int, int>[N];
        var map = new MapRs<int, int>(ref buckets, ref entriesHash);
        
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < count; i++)
        {
            map[i] = i;
            dict[i] = i;

            if (i < 1000)
            {
                var keys1 = new int[map.Keys.Count];
                var keys2 = new int[map.Keys.Count];

                map.Keys.CopyTo(keys1, 0);
                map.Keys.CopyTo(keys2, 0);

                var mapKeys = (ICollection<int>)map.Keys;

                Assert.True(mapKeys.Contains(i), i.ToString());

                for (int j = 0; j < keys1.Length; j++)
                {
                    Assert.AreEqual(keys1[j], keys2[j]);
                }

                var enumerator = mapKeys.GetEnumerator();
                Assert.NotNull(((IEnumerable)mapKeys).GetEnumerator());

                while (enumerator.MoveNext())
                {
                    var containsKey = map.ContainsKey(enumerator.Current);

                    Assert.True(containsKey, $"Val {enumerator.Current} {map.Length}");
                }
            }

            if (i < 100)
            {
                var values1 = new int[map.Values.Count];
                var values2 = new int[map.Values.Count];

                map.Values.CopyTo(values1, 0);
                map.Values.CopyTo(values2, 0);

                for (int j = 0; j < values1.Length; j++)
                {
                    Assert.AreEqual(values1[j], values2[j]);
                }

                var mapValues = (ICollection<int>)map.Values;

                Assert.True(mapValues.Contains(i));

                var enumerator = mapValues.GetEnumerator();
                Assert.NotNull(((IEnumerable)mapValues).GetEnumerator());

                while (enumerator.MoveNext())
                {
                    Assert.True(map.ContainsValue(enumerator.Current));
                }
            }

            Assert.AreEqual(dict[i], map[i]);
        }
    }
   
   
    [Test]
    public void ValueByRefTest()
    {
        Span<int> buckets = stackalloc int[N];
        Span<Entry<int, int>> entriesHash = stackalloc Entry<int, int>[N];
        var dict = new MapRs<int, int>(ref buckets, ref entriesHash);

        dict.TryAdd(1, 10);
        dict.TryAdd(2, 20);
        dict.TryAdd(3, 30);

        ref var v = ref dict.ValueByRef(1, out var success);

        v = 100;

        Assert.AreEqual(100, dict[1]);
        Assert.AreEqual(20, dict[2]);
        Assert.AreEqual(30, dict[3]);

        dict.ValueByRef(4, out var fail);

        Assert.False(fail);
    }

    private static void Test(Dictionary<int, TimeSpan> dict, ref MapRs<int, TimeSpan> map)
    {
        var keyTest = 999;

        TimeSpan val1;
        Assert.False(map.TryGetValue(keyTest, out val1));
        Assert.AreEqual(default(TimeSpan), val1);

        try
        {
            var i = map[keyTest];
            
            Assert.False(true);
        }
        catch (KeyNotFoundException e)
        {
        }

        map[keyTest] = TimeSpan.FromMilliseconds(999);

        Assert.True(map.TryGetValue(keyTest, out val1));
        Assert.AreEqual(TimeSpan.FromMilliseconds(999), val1);

        dict[keyTest] = TimeSpan.FromMilliseconds(999);

        foreach (var keyValuePair in dict)
        {
            Assert.AreEqual(keyValuePair.Value, map[keyValuePair.Key]);
            Assert.True(map.Contains(keyValuePair));
            Assert.True(map.ContainsKey(keyValuePair.Key));
            TimeSpan val;
            Assert.True(map.TryGetValue(keyValuePair.Key, out val));
            Assert.AreEqual(keyValuePair.Value, val);
        }

        var hashSet1 = new HashSet<int>(map.Keys);

        foreach (var key in dict.Keys)
        {
            Assert.True(hashSet1.Contains(key));
        }

        var hashSet2 = new HashSet<TimeSpan>(map.Values);

        foreach (var key in dict.Values)
        {
            Assert.True(hashSet2.Contains(key));
        }

        var pairs1 = map.ToArray();
        var pairs2 = dict.ToArray();

        Assert.AreEqual(pairs1.Length, pairs2.Length);

        var hashSet3 = new HashSet<int>(pairs1.Select(p => p.Key));

        foreach (var key in pairs2)
        {
            Assert.True(hashSet3.Contains(key.Key));
        }

        map.Clear();

        Assert.AreEqual(0, map.Count);

        foreach (var keyValuePair in dict)
        {
            Assert.False(map.Contains(keyValuePair));
            Assert.False(map.ContainsKey(keyValuePair.Key));
        }

        Assert.True(map.TryAdd(5, TimeSpan.FromDays(1)));
        Assert.False(map.TryAdd(5,TimeSpan.FromDays(1)));
    }

   
    [Test]
    public void TestGetOrDefault()
    {
        Span<int> buckets = stackalloc int[N];
        Span<Entry<int, int>> entriesHash = stackalloc Entry<int, int>[N];
        var dict = new MapRs<int, int>(ref buckets, ref entriesHash);

        Assert.AreEqual(0, dict.GetOrDefault(1));
        Assert.AreEqual(1, dict.GetOrDefault(1, 1));
    }
  
    [Test]
    public void TestAddExc()
    {
        Span<int> buckets = stackalloc int[N];
        Span<Entry<int, int>> entriesHash = stackalloc Entry<int, int>[N];
        var dict = new MapRs<int, int>(ref buckets, ref entriesHash);

        dict.Add(1, 1);

        try
        {
            dict.Add(1, 1);
            Assert.False(true);
        }
        catch (ArgumentException e)
        {
        }
    }
    
    [Test]
    public void TestEnumeration()
    {
        Span<int> buckets = stackalloc int[N];
        Span<Entry<int, int>> entriesHash = stackalloc Entry<int, int>[N];
        var dict = new MapRs<int, int>(ref buckets, ref entriesHash);
        
        Assert.False(dict.GetEnumerator().MoveNext());
        Assert.False(dict.GetRsEnumerator().MoveNext());
        Assert.AreEqual(0, dict.GetRsEnumerator().Count);
        
        dict.Add(1, 10);
        dict.Add(2, 20);
        dict.Add(3, 30);

        var map = dict.ToMap();

        var le = map.GetEnumerator();
        var de = dict.GetEnumerator();
        var dre = dict.GetRsEnumerator();
        Assert.AreEqual(3, dre.Count);

        while (le.MoveNext())
        {
            Assert.True(de.MoveNext());
            Assert.True(dre.MoveNext());
            
            Assert.AreEqual(le.Current, de.Current);
            Assert.AreEqual(le.Current.Key, dre.Current);
        }
    }

    [Test]
    public void TestExc()
    {
        Span<int> buckets = stackalloc int[N];
        Span<Entry<int, int>> entriesHash = stackalloc Entry<int, int>[N];
        var map = new MapRs<int, int>(ref buckets, ref entriesHash);

        foreach (var i in Enumerable.Range(0, N))
        {
            map[i] = i;
        }

        try
        {
            map[-1] = -1;
            Assert.Fail();
        }
        catch (InsufficientMemoryException e)
        {
        }
    }
}

