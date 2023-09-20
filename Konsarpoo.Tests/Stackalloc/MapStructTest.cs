using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Konsarpoo.Collections.Allocators;
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
    public void TestRemoveIfEmpty()
    {
        Span<int> buckets = stackalloc int[N];
        Span<MapStruct<int, int>.Entry> entriesHash = stackalloc MapStruct<int, int>.Entry[N];

        var map = new MapStruct<int, int>(ref buckets, ref entriesHash);

        Assert.False(map.Remove(0));
    }

    [Test]
    public void TestSmall()
    {
        Span<int> buckets = stackalloc int[N];
        Span<MapStruct<int, TimeSpan>.Entry> entriesHash = stackalloc MapStruct<int, TimeSpan>.Entry[N];
        
        var map = new MapStruct<int, TimeSpan>(ref buckets, ref entriesHash);
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
        Span<MapStruct<int, TimeSpan>.Entry> entriesHash = stackalloc MapStruct<int, TimeSpan>.Entry[N];
        var testData = new MapStruct<int, TimeSpan>(ref buckets, ref entriesHash);
        
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
        Span<MapStruct<int, TimeSpan>.Entry> entriesHash = stackalloc MapStruct<int, TimeSpan>.Entry[N];
        var map = new MapStruct<int, TimeSpan>(ref buckets, ref entriesHash);
        
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
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), map[2]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), map[3]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), map[4]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), map[5]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), map[6]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), map[7]);

        Test(dict, ref map);
    }

    [Test]
    public void TestAdd2([Values(0, 1, 2, 1000, 1_0000)] int count)
    {
        var map = new Map<int, int>();
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

                Assert.True(mapKeys.IsReadOnly);

                Assert.Throws<NotSupportedException>(() => mapKeys.Remove(1));
                Assert.Throws<NotSupportedException>(() => mapKeys.Add(1));
                Assert.Throws<NotSupportedException>(() => mapKeys.Clear());

                Assert.True(mapKeys.Contains(i));

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

                Assert.True(mapValues.IsReadOnly);

                Assert.Throws<NotSupportedException>(() => mapValues.Remove(1));
                Assert.Throws<NotSupportedException>(() => mapValues.Add(1));
                Assert.Throws<NotSupportedException>(() => mapValues.Clear());

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
    public void TestAdd()
    {
        var map = new Map<string, string>
        {
            { "test0", "val0" },
            { "test1", "val1" },
            { "test2", "val2" },
            { "test3", "val3" },
            { "test4", "val4" },
            { "test5", "val5" },
            { "test6", "val6" }
        };

        var map2 = new Map<string, string>
        {
            { "test10", "val10" },
            { "test11", "val11" },
            { "test12", "val12" },
            { "test13", "val13" },
            { "test14", "val14" },
            { "test15", "val15" },
            { "test16", "val16" }
        };

        Assert.True(map.ContainsValue("val0"));
        Assert.True(map.ContainsValue("val1"));
        Assert.False(map2.ContainsValue("val3"));
        Assert.False(map2.ContainsValue(null));
        Assert.False(map.ContainsValue(null));

        var newDict = map + map2;

        foreach (var kv in map)
        {
            Assert.True(newDict[kv.Key] == kv.Value);
        }

        foreach (var kv in map2)
        {
            Assert.True(newDict[kv.Key] == kv.Value);
        }
    }

    [Test]
    public void TestMapObj()
    {
        {
            var mapObj = _.MapObj((1, string.Empty), (2, string.Empty));

            var map = new Map<int, object>() { { 2, string.Empty }, { 1, string.Empty } };

            Assert.True(map == mapObj);

            mapObj.Dispose();

            Assert.AreEqual(0, mapObj.Count);
        }

        GC.Collect();
    }

    [Test]
    public void TestSubstract()
    {
        var map = new Map<string, string>
        {
            { "test0", "val0" },
            { "test1", "val1" },
            { "test2", "val2" },
        };

        var map2 = new Map<string, string>
        {
            { "test10", "val10" },
            { "test11", "val11" },
            { "test12", "val12" },
        };

        var map3 = new Map<string, string>
        {
            { "test3", "val3" },
            { "test4", "val4" },
            { "test5", "val5" },
            { "test6", "val6" }
        };

        var d1 = map + map3;
        var d2 = map2 + map3;

        var rez1 = d1 - d2;

        foreach (var kv in map)
        {
            Assert.True(rez1[kv.Key] == kv.Value);
        }

        foreach (var kv in map3)
        {
            Assert.True(rez1.MissingKey(kv.Key));
        }

        var rez2 = d2 - d1;

        foreach (var kv in map2)
        {
            Assert.True(rez2[kv.Key] == kv.Value);
        }

        foreach (var kv in map3)
        {
            Assert.True(rez2.MissingKey(kv.Key));
        }

        GC.Collect();
    }

    [Test]
    public void ValueByRefTest()
    {
        var dict = new Map<string, int>
        {
            { "test1", 1 },
            { "test2", 2 },
            { "test3", 3 },
        };

        ref var v = ref dict.ValueByRef("test1", out var success);

        v = 20;

        Assert.AreEqual(20, dict["test1"]);
        Assert.AreEqual(2, dict["test2"]);
        Assert.AreEqual(3, dict["test3"]);

        dict.ValueByRef("test1123", out var fail);

        Assert.False(fail);
    }

    private static void Test(Dictionary<int, TimeSpan> dict, ref MapStruct<int, TimeSpan> map)
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

        map.TryUpdate(keyTest, TimeSpan.FromMilliseconds(999));

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
    public void TestOp()
    {
        Func<int, int> keySelector = i => i * 10;
        Func<int, int> elementSelector = i => i;

        Map<int, int> l1 = new Data<int>(Enumerable.Range(0, 5)).ToMap(keySelector, elementSelector);
        Map<int, int> l2 = new Data<int>(Enumerable.Range(5, 5)).ToMap(keySelector, elementSelector);

        var l3 = l1 + l2;

        Assert.AreEqual(new Data<int>(Enumerable.Range(0, 10)).ToMap(keySelector, elementSelector), l3);

        Assert.AreEqual(l2, l3 - l1);
        Assert.AreEqual(l1, l3 - l2);

        Assert.AreEqual(l1.ToMap(), l1 + (IReadOnlyDictionary<int, int>)null);
        Assert.Null((Map<int, int>)null + (IReadOnlyDictionary<int, int>)null);
    }

    [Test]
    public void TestOpR()
    {
        Func<int, int> keySelector = i => i * 10;
        Func<int, int> elementSelector = i => i;

        var l1 = new Data<int>(Enumerable.Range(0, 5)).ToMap(keySelector, elementSelector);
        var l2 = new Data<int>(Enumerable.Range(5, 5)).ToMap(keySelector, elementSelector);

        var l3 = l1 + (IReadOnlyDictionary<int, int>)l2;

        Assert.AreEqual(new Data<int>(Enumerable.Range(0, 10)).ToMap(keySelector, elementSelector), l3);

        Assert.AreEqual(l2, l3 - (IReadOnlyDictionary<int, int>)l1);
        Assert.AreEqual(l1, l3 - (IReadOnlyDictionary<int, int>)l2);
    }

    [Test]
    public void TestInt()
    {
        var dict = (IDictionary<string, int>)new Map<string, int>
        {
            { "test1", 1 },
            { "test2", 2 },
            { "test3", 3 },
        };

        var keys = dict.Keys.ToData();
        var vals = dict.Values.ToData();

        Assert.AreEqual(3, keys.Count);
        Assert.AreEqual(3, vals.Count);

        var keyValuePairs = (ICollection<KeyValuePair<string, int>>)dict;

        keyValuePairs.Add(new KeyValuePair<string, int>("test4", 4));

        Assert.AreEqual(dict["test4"], 4);

        keyValuePairs.Remove(new KeyValuePair<string, int>("test4", 4));

        Assert.False(dict.ContainsKey("test4"));
    }

    [Test]
    public void TestRInt()
    {
        var dict = (IReadOnlyDictionary<string, int>)new Map<string, int>
        {
            { "test1", 1 },
            { "test2", 2 },
            { "test3", 3 },
        };

        var keys = dict.Keys.ToData();
        var vals = dict.Values.ToData();

        Assert.AreEqual(3, keys.Count);
        Assert.AreEqual(3, vals.Count);
    }

    [Test]
    public void TestSerialization()
    {
        var map = new Map<string, int>(StringComparer.OrdinalIgnoreCase);

        map.Add("qwerty", 123);
        map.Add("test", 421);

        var serializeWithDcs = SerializeHelper.SerializeWithDcs(map);

        var deserializeWithDcs = SerializeHelper.DeserializeWithDcs<Map<string, int>>(serializeWithDcs);

        Assert.AreEqual(deserializeWithDcs, map);
    }

    private class GcArrayAllocator<T> : IArrayAllocator<T>
    {
        public T[] Rent(int count)
        {
            return new T[count];
        }

        public void Return(T[] array, bool clearArray = false)
        {
        }

        public bool CleanArrayReturn => true;
    }

    [Test]
    public void TestCustomAllocator()
    {
        var l1 = new Map<int, int>(0, 16, GcAllocatorSetup.GetMapPoolSetup<int, int>());
        foreach (var i in Enumerable.Range(0, 50))
        {
            l1[i] = i;
        }

        var l2 = new Map<int, int>(0, 16, GcAllocatorSetup.GetMapPoolSetup<int, int>());
        foreach (var i in Enumerable.Range(50, 50))
        {
            l2[i] = i;
        }

        var l3 = l1 + l2;

        var expected = new Map<int, int>(0, 16, GcAllocatorSetup.GetMapPoolSetup<int, int>());
        foreach (var i in Enumerable.Range(0, 100))
        {
            expected[i] = i;
        }

        Assert.AreEqual(expected, l3);

        Assert.AreEqual(l2, l3 - l1);
        Assert.AreEqual(l1, l3 - l2);
    }

    [Test]
    public void TestCustomAllocator2()
    {
        var l1 = new Map<int, int>(GcAllocatorSetup.GetMapPoolSetup<int, int>());
        foreach (var i in Enumerable.Range(0, 50))
        {
            l1[i] = i;
        }

        var l2 = new Map<int, int>(GcAllocatorSetup.GetMapPoolSetup<int, int>());
        foreach (var i in Enumerable.Range(50, 50))
        {
            l2[i] = i;
        }

        var l3 = l1 + l2;

        var expected = new Map<int, int>(GcAllocatorSetup.GetMapPoolSetup<int, int>());
        foreach (var i in Enumerable.Range(0, 100))
        {
            expected[i] = i;
        }

        Assert.AreEqual(expected, l3);

        Assert.AreEqual(l2, l3 - l1);
        Assert.AreEqual(l1, l3 - l2);
    }

    [Test]
    public void TestSerialization2()
    {
        var map = new Map<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var i in Enumerable.Range(1, 1024))
        {
            map.Add(i.ToString(), i);
        }

        Map<string, int> clone1 = SerializeHelper.Clone<Map<string, int>>(map);

        var b = clone1 == map;

        Assert.AreEqual(clone1, map);
    }


    [Test]
    public void TestGetPrime()
    {
        var prime = Prime.GetPrime(23575267 + 1);

        Assert.AreEqual(23575313, prime);

        Assert.Throws<ArgumentException>(() => Prime.GetPrime(-1));
    }

    [Test]
    public void TestCapacityCtr()
    {
        var map = new Map<int, int>(100);

        for (int i = 0; i < 100; i++)
        {
            map[i] = i;
        }

        var map1 = map.ToMap();

        Assert.False(map1 != map);

        map1.Remove(0);

        Assert.True(map1 != map);
    }

    [Test]
    public void TestGetOrDefault()
    {
        var map = new Map<int, string>();

        Assert.Null(map.GetOrDefault(1));
        Assert.AreEqual(string.Empty, map.GetOrDefault(1, string.Empty));
    }

    [Test]
    public void TestGetOrCreate()
    {
        var map = new Map<int, Data<int>>();

        var ints = map.GetOrAdd(1, () => new Data<int>());

        Assert.AreEqual(0, map.GetOrDefault(1).Count);

        var nullMap = (Map<int, Data<int>>)null;

        Assert.Throws<ArgumentNullException>(() => nullMap.GetOrAdd(1, () => new Data<int>()));

        Assert.AreEqual(0, ints.Count);

        map.GetOrAdd(1, () => new Data<int>()).Add(1);

        Assert.AreEqual(1, map[1].Count);
    }

    [Test]
    public void TestOp1()
    {
        var m1 = (Map<int, int>)null;
        var m2 = (Map<int, int>)null;
        var m3 = new Map<int, int>() { { 1, 1 } };
        var m4 = new Map<int, int>() { { 1, 2 } };
        var m5 = new Map<int, int>() { { 2, 1 } };
        var m6 = new Map<int, int>() { { 1, 1 }, { 2, 2 } };

        Assert.True(m1 == m2);
        Assert.False(m1 != m2);
        Assert.False(m3 == m1);
        Assert.False(m3 == m2);
        Assert.False(m3 == m4);
        Assert.False(m3 == m5);
        Assert.False(m3 == m6);

        Assert.True(m3.Equals(m3));
        Assert.False(m3.Equals(null));
        Assert.False(m3.Equals(m6));

        var myDict = new MyDict() { { 1, 1 } };

        Assert.False(m3.Equals(myDict));
    }

    private class MyDict : Map<int, int>
    {

    }

    [Test]
    public void TestAddExc()
    {
        var m3 = new Map<int, int>() { { 1, 1 } };

        Assert.Throws<ArgumentException>(() => m3.Add(1, 2));

        var m4 = new Map<string, int>() { { "1", 1 } };

        Assert.Throws<ArgumentNullException>(() => m4.Add(null, 2));

        m4.Add("2", 3);

        Assert.AreEqual(3, m4["2"]);
    }

    [Test]
    public void TestExtraApi()
    {
        var m3 = new Map<int, int>() { { 1, 1 } };

        m3.Put(2, 2);

        Assert.AreEqual(2, m3.GetSet(2, (v, m) => v));

        Assert.False(m3.ContainsKey(3));

        Assert.AreEqual(3, m3.GetSet(3, (v, m) => m[v] = v));
        Assert.AreEqual(3, m3[3]);
    }
}

