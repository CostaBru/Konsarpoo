using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests;

[TestFixture(16, AllocatorType.GC, 0)]
[TestFixture(32, AllocatorType.Mixed, 16)]
[TestFixture(16, AllocatorType.Pool, 0)]
[TestFixture(1024, AllocatorType.GC, 0)]
[TestFixture(1024, AllocatorType.Mixed, 512)]
[TestFixture(1024, AllocatorType.Pool, 0)]
public class TrieMapTest : BaseTest
{
    public TrieMapTest(int? maxSizeOfArrayBucket, AllocatorType allocatorType, int gcLen) : base(maxSizeOfArrayBucket, allocatorType, gcLen)
    {
    }

    [Test]
    public void TestStringStringInt()
    {
        var map = CreateMap<string>();

        var valueTuple = ("1", "2", 3);
        
        map[valueTuple] = "4";
        
        Assert.True(map.ContainsKey(valueTuple));
        Assert.False(map.ContainsKey(("", "", 0)));
    }
    
    [Test]
    public void TestRemoveIfEmpty()
    {
        var map = CreateMap<string>();

        Assert.False(map.Remove(("", "", 0)));
    }
    
    [Test]
    public void TestSmall()
    {
        var map = CreateMap<string>();

        map.Add(("test0", "t0", 0), "val0");
        map.Add(("test1", "t1", 1), "val1");
            
        Assert.AreEqual("val0", map[("test0", "t0", 0)]);
        Assert.AreEqual("val1", map[("test1", "t1", 1)]);

        Assert.True(map.ContainsValue("val0"));
        Assert.True(map.ContainsValue("val1"));
        Assert.False(map.ContainsValue("val3"));

        Assert.NotZero(map.GetHashCode());

        var dict2 = map.ToMap();

        dict2.Add(("test3", "t3", 3), "123");
        
        var dict = new Dictionary<(string, string, int), string>();
        dict.Add(("test0", "t0", 0), "val0");
        dict.Add(("test1", "t1", 1), "val1");

        Assert.AreEqual(dict.Count, map.Length);

        Assert.AreEqual("val0", map[("test0", "t0", 0)]);
        Assert.AreEqual("val1", map[("test1", "t1", 1)]);

        var map2 = CreateMap<string>();

        map2.Add(("test10", "t0", 0), "0");
        map2.Append(new KeyValuePair<(string, string, int), string>(("test1", "t1", 1), "1"));

        Assert.True(map2[("test10", "t0", 0)] == "0");
        Assert.True(map2[("test1", "t1", 1)] == "1");

        Assert.False(((ICollection<KeyValuePair<(string, string, int), string>>)map2).IsReadOnly);
    }
    
    [Test]
    public void TestAddHuge()
    {
        var testData = CreateMap<int>();
            
        for (int i = 0; i < 1000000; i++)
        {
            testData.Add((i.ToString(), i.ToString(), i), i);
        }
            
        for (int i = 0; i < 1000000; i++)
        {
            Assert.True(testData.TryGetValue((i.ToString(), i.ToString(), i), out var val));
            Assert.AreEqual(i, val);
        }
            
        testData.Dispose();
    }
    
    [Test]
    public void TestDefaultDict()
    {
        var map = CreateMap<Data<string>>();
            
        map.EnsureValues((k) => new Data<string>());
            
        var dict = new Dictionary<(string, string, int), Data<string>>();

        Assert.False(map.ContainsKey(("test0", "0", 0)));
            
        map[("test0", "0", 0)].Add("val0");
            
        Assert.True(map.ContainsKey(("test0", "0", 0)));
            
        Func<Data<string>> valueFactory = () => new Data<string>();
            
        dict.GetOrAdd(("test0", "0", 0), valueFactory).Add("val0");

        Assert.AreEqual("val0", map[("test0", "0", 0)].SingleOrDefault());

        Assert.True(map == dict);
    }
    
    [Test]
    public void TestCopyCtr()
    {
        var map1 = CreateMap<int>();
        
        map1.Add(("1", "1", 1), 1);
        map1.Add(("2", "2", 2), 2);
        map1.Add(("3", "3", 3), 3);
       
        var map2 = new TrieMap<(string key, string type, int id), int>(map1);
            
        Assert.True(map1 == map2);
    }
    
    [Test]
    public void ValueByRefTest()
    {
        var map1 = CreateMap<int>();
        
        map1.Add(("1", "1", 1), 1);
        map1.Add(("2", "2", 2), 2);
        map1.Add(("3", "3", 3), 3);

        ref var v = ref map1.ValueByRef(new object[] {"1", "1", 1}, out var success);

        v = 20;

        Assert.AreEqual(20, map1[("1", "1", 1)]);
        Assert.AreEqual(2, map1[("2", "2", 2)]);
        Assert.AreEqual(3, map1[("3", "3", 3)]);

        map1.ValueByRef(new object[] {"", "", 1}, out var fail);

        Assert.False(fail);
    }
    
    [Test]
    public void TestRemove()
    {
        var map = CreateMap<int>();
            
        map.Add(("1", "1", 1), 1);
        map.Add(("1", "2", 2), 2);
        map.Add(("1", "2", 3), 3);

        Assert.AreEqual(3, map.Count);
          
        Assert.True(map.Remove(("1", "1", 1)));
        Assert.AreEqual(2, map.Count);
            
        Assert.True(map.Remove(("1", "2", 2)));
        Assert.AreEqual(1, map.Count);

        Assert.True(map.Remove(("1", "2", 3)));
        Assert.AreEqual(0, map.Count);
            
        Assert.False(map.Remove(("1", "2", 3)));
        Assert.AreEqual(0, map.Count);
            
        map = CreateMap<int>();
            
        map.Add(("1", "1", 1), 1);
        map.Add(("1", "2", 2), 2);
        map.Add(("1", "2", 3), 3);
            
        Assert.True(map.Remove(("1", "2", 3)));
        Assert.AreEqual(2, map.Count);
            
        Assert.True(map.Remove(("1", "2", 2)));
        Assert.AreEqual(1, map.Count);
            
        Assert.True(map.Remove(("1", "1", 1)));
        Assert.AreEqual(0, map.Count);
    }

    private static TrieMap<(string key, string type, int id), T> CreateMap<T>()
    {
        Func<IEnumerable<object>, (string key, string type, int id)> comp;
        comp = (coll) => ((string)coll.ElementAt(0), (string)coll.ElementAt(1), (int)coll.ElementAt(2));

        Func<(string key, string type, int id), object, (string key, string type, int id)> concat;
        concat = (key, obj) =>
        {
            if (obj is string str)
            {
                if (key.key == null)
                {
                    return (str, (string)null, 0);
                }

                return (key.key, str, 0);
            }

            return (key.key, key.type, (int)obj);
        };

        Func<(string key, string type, int id), IEnumerable<object>> decompose;
        decompose = (key) => new object[] { key.key, key.type, key.id };

        return new TrieMap<(string key, string type, int id), T>(comp, concat, decompose);
    }
}