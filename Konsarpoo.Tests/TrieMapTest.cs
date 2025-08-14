using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
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
        var map = new TupleTrieMap<string, string, int, string>();

        var valueTuple = ("1", "2", 3);
        
        map[valueTuple] = "4";
        
        Assert.True(map.ContainsKey(valueTuple));
        Assert.False(map.ContainsKey(("", "", 0)));
    }
    
    [Test]
    public void TestRemoveIfEmpty()
    {
        var map = new TupleTrieMap<string, string, int, string>();

        Assert.False(map.Remove(("", "", 0)));
    }
    
    [Test]
    public void TestSmall()
    {
        var map =  new TupleTrieMap<string, string, int, string>();

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

        var map2 =  new TupleTrieMap<string, string, int, string>();

        map2.Add(("test10", "t0", 0), "0");
        map2.Append(new KeyValuePair<(string, string, int), string>(("test1", "t1", 1), "1"));

        Assert.True(map2[("test10", "t0", 0)] == "0");
        Assert.True(map2[("test1", "t1", 1)] == "1");

        Assert.False(((ICollection<KeyValuePair<(string, string, int), string>>)map2).IsReadOnly);
    }
    
    [Test]
    public void TestAddHuge()
    {
        var testData =  new TupleTrieMap<string, string, int, int>();
            
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
        var map =  new TupleTrieMap<string, string, int, Data<string>>();
            
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
    public void TestGetOrCreate()
    {
        var map = new TupleTrieMap<int, int, Data<int>>();

        var ints = map.GetOrAdd((1, 1), () => new Data<int>());
            
        Assert.AreEqual(0,  map.GetOrDefault((1, 1)).Count);

        var nullMap = (TupleTrieMap<int, int, Data<int>>)null;

        Assert.Throws<ArgumentNullException>(() => nullMap.GetOrAdd((1, 1), () => new Data<int>()));
            
        Assert.AreEqual(0, ints.Count);
            
        map.GetOrAdd((1, 1), () => new Data<int>()).Add(1);

        Assert.AreEqual(1, map[(1, 1)].Count);
    }
    
    [Test]
    public void TestRInt()
    {
        var dict = (IReadOnlyDictionary<(string, int), int>)new TupleTrieMap<string, int, int>
        {
            { ("test1", 1), 1 },
            { ("test2", 2), 2 },
            { ("test3", 3), 3 },
        };

        var keys = dict.Keys.ToData();
        var vals = dict.Values.ToData();
        
        Assert.True(dict.ContainsKey(("test1", 1)));
        Assert.False(dict.ContainsKey(("1", 1)));

        Assert.AreEqual(3, keys.Count);
        Assert.AreEqual(3, vals.Count);
    }
    
    [Test]
    public void TestCopyCtr()
    {
        var map1 = new TupleTrieMap<string, string, int, int>();
        
        map1.Add(("1", "1", 1), 1);
        map1.Add(("2", "2", 2), 2);
        map1.Add(("3", "3", 3), 3);
       
        var map2 =  new TupleTrieMap<string, string, int, int>(map1);
            
        Assert.True(map1 == map2);
    }
    
    [Test]
    public void ValueByRefTest()
    {
        var map1 = new TupleTrieMap<string, string, int, int>();
        
        map1.Add(("1", "1", 1), 1);
        map1.Add(("2", "2", 2), 2);
        map1.Add(("3", "3", 3), 3);

        ref var v = ref map1.ValueByRef(("1", "1", 1), out var success);

        v = 20;

        Assert.AreEqual(20, map1[("1", "1", 1)]);
        Assert.AreEqual(2, map1[("2", "2", 2)]);
        Assert.AreEqual(3, map1[("3", "3", 3)]);

        map1.ValueByRef(("", "", 1), out var fail);

        Assert.False(fail);
    }
    
    [Test]
    public void TestRemove()
    {
        var map = new TupleTrieMap<string, string, int, int>();
            
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
            
        map = new TupleTrieMap<string, string, int, int>();
            
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

    [Test]
    public void TestThrowsIfStorageFactoryIsNotStatic()
    {
        var map = new TupleTrieMap<string, int, int>();

        int i = 0;

        Assert.Throws<ArgumentException>(() => map.SetStorageFactory(t =>
        {
            i++;
            return null;
        }));
    }

    [Test]
    public void TestSerialization2()
    {
        var map = new TupleTrieMap<string, int, int>();
        
        map.SetStorageFactory(NodesMapFactory);

        map.Add(("1", 1), 1);
        map.Add(("1", 2), 2);
        map.Add(("1", 3), 3);

        var serializeWithDcs = SerializeHelper.SerializeWithDcs(map);

        var deserializeWithDcs = SerializeHelper.DeserializeWithDcs<TupleTrieMap<string, int, int>>(serializeWithDcs);

        Assert.True(deserializeWithDcs == map);
    }
    
    public static  IDictionary<object, AbstractTupleTrieMap<(string, int), int>.TrieLinkNode<int>> NodesMapFactory(Type type)
    {
        return null;
    }
      
    [Test]
    public void TestSerializationClone2()
    {
        var map = new TupleTrieMap<string, int, int>();
        
        map.SetStorageFactory(NodesMapFactory);

        foreach (var i in Enumerable.Range(1, 1024))
        {
            map.Add((i.ToString(), i), i);
        }

        TupleTrieMap<string, int, int> clone1 = SerializeHelper.Clone<TupleTrieMap<string, int, int>>(map);

        Assert.True(clone1 == map);
    }
    
    [Test]
    public void TestSerialization3()
    {
        var map =new TupleTrieMap<string, string, int, int>();

        map.Add(("1", "1", 1), 1);
        map.Add(("1", "2", 2), 2);
        map.Add(("1", "2", 3), 3);

        var serializeWithDcs = SerializeHelper.SerializeWithDcs(map);

        var deserializeWithDcs = SerializeHelper.DeserializeWithDcs<TupleTrieMap<string, string, int, int>>(serializeWithDcs);

        Assert.True(deserializeWithDcs == map);
    }
      
    [Test]
    public void TestSerializationClone3()
    {
        var map = new TupleTrieMap<string, string, int, int>();

        foreach (var i in Enumerable.Range(1, 1024))
        {
            map.Add((i.ToString(), i.ToString(), i), i);
        }

        TupleTrieMap<string, string, int, int> clone1 = SerializeHelper.Clone<TupleTrieMap<string, string, int, int>>(map);

        Assert.True(clone1 == map);
    }
    
    [Test]
    public void TestSerialization4()
    {
        var map =new TupleTrieMap<string, string, bool, int, int>();

        map.Add(("1", "1", true,  1), 1);
        map.Add(("1", "2", true, 2), 2);
        map.Add(("1", "2", true, 3), 3);

        var serializeWithDcs = SerializeHelper.SerializeWithDcs(map);

        var deserializeWithDcs = SerializeHelper.DeserializeWithDcs<TupleTrieMap<string, string, bool, int, int>>(serializeWithDcs);

        Assert.True(deserializeWithDcs == map);
    }
      
    [Test]
    public void TestSerializationClone4()
    {
        var map = new TupleTrieMap<string, string, bool, int, int>();

        foreach (var i in Enumerable.Range(1, 1024))
        {
            map.Add((i.ToString(), i.ToString(), true, i), i);
        }

        TupleTrieMap<string, string,  bool, int, int> clone1 = SerializeHelper.Clone<TupleTrieMap<string, string, bool, int, int>>(map);

        Assert.True(clone1 == map);
    }
    
    [Test]
    public void TestSerialization5()
    {
        var map =new TupleTrieMap<string, string, bool, TimeSpan, int, int>();

        map.Add(("1", "1", true, TimeSpan.FromSeconds(1),  1), 1);
        map.Add(("1", "2", true, TimeSpan.FromSeconds(2), 2), 2);
        map.Add(("1", "2", true, TimeSpan.FromSeconds(3), 3), 3);

        var serializeWithDcs = SerializeHelper.SerializeWithDcs(map);

        var deserializeWithDcs = SerializeHelper.DeserializeWithDcs<TupleTrieMap<string, string, bool, TimeSpan, int, int>>(serializeWithDcs);

        Assert.True(deserializeWithDcs == map);
    }
      
    [Test]
    public void TestSerializationClone5()
    {
        var map = new TupleTrieMap<string, string, bool, TimeSpan, int, int>();

        foreach (var i in Enumerable.Range(1, 1024))
        {
            map.Add((i.ToString(), i.ToString(), true, TimeSpan.FromMilliseconds(i), i), i);
        }

        TupleTrieMap<string, string,  bool, TimeSpan, int, int> clone1 = SerializeHelper.Clone<TupleTrieMap<string, string, bool, TimeSpan, int, int>>(map);

        Assert.True(clone1 == map);
    }


    [Test]
    public void TestStartWith()
    {
        var map = new TupleTrieMap<int, int, int, int>();

        map.Add((1, 1, 1), 1);
        map.Add((1, 2, 2), 2);
        map.Add((1, 2, 3), 3);
        map.Add((2, 2, 2), 4);
        map.Add((2, 3, 3), 5);
        map.Add((2, 3, 4), 6);

        var vals = map.WhereKeyStartsWith((1, 0, 0), 1).OrderBy(a => a).ToArray();
        var expected = map
            .Where(kv => kv.Key.Item1 == 1)
            .Select(v => v.Value)
            .OrderBy(r => r)
            .ToArray();

        Assert.AreEqual(expected, vals);


        vals = map.WhereKeyStartsWith((1, 1, 0), 2).OrderBy(a => a).ToArray();
        expected = map
            .Where(kv => kv.Key.Item1 == 1 && kv.Key.Item2 == 1)
            .Select(v => v.Value)
            .OrderBy(r => r)
            .ToArray();

        Assert.AreEqual(expected, vals);

        vals = map.WhereKeyStartsWith((2, 3, 4), 3).OrderBy(a => a).ToArray();
        expected = map
            .Where(kv => kv.Key.Item1 == 2 && kv.Key.Item2 == 3 && kv.Key.Item3 == 4)
            .Select(v => v.Value)
            .OrderBy(r => r)
            .ToArray();

        Assert.AreEqual(expected, vals);

        vals = map.WhereKeyStartsWith((1, 1, 10), 3).OrderBy(a => a).ToArray();
        expected = Array.Empty<int>();

        Assert.AreEqual(expected, vals);

        vals = map.WhereKeyStartsWith((1, 10, 0), 2).OrderBy(a => a).ToArray();
        expected = Array.Empty<int>();

        Assert.AreEqual(expected, vals);

        vals = map.WhereKeyStartsWith((10, 0, 0), 1).OrderBy(a => a).ToArray();
        expected = Array.Empty<int>();

        Assert.AreEqual(expected, vals);
    }

    [Test]
    public void TestStartWithArr()
    {
        var map = new TupleTrieMap<int, int, int, int>();
           
        map.Add((1, 1, 1), 1);
        map.Add((1, 2, 2), 2);
        map.Add((1, 2, 3), 3);
        map.Add((2, 2, 2), 4);
        map.Add((2, 3, 3), 5);
        map.Add((2, 3, 4), 6);
        
        var testMap = (IObjectKeyTupleTrieMap<int>)map; 
       
        var vals = testMap.WhereKeyStartsWith(new object[] {1, 0, 0}, 1).OrderBy(a => a).ToArray();
        var expected = map
            .Where(kv => kv.Key.Item1 == 1)
            .Select(v => v.Value)
            .OrderBy(r => r)
            .ToArray();
        
        Assert.AreEqual(expected, vals);

         
        vals = testMap.WhereKeyStartsWith(new object[] {1, 1, 0}, 2).OrderBy(a => a).ToArray();
        expected = map
            .Where(kv => kv.Key.Item1 == 1 && kv.Key.Item2 == 1)
            .Select(v => v.Value)
            .OrderBy(r => r)
            .ToArray();

        Assert.AreEqual(expected, vals);
        
        vals = testMap.WhereKeyStartsWith(new object[] {2, 3, 4}, 3).OrderBy(a => a).ToArray();
        expected = map
            .Where(kv => kv.Key.Item1 == 2 && kv.Key.Item2 == 3 && kv.Key.Item3 == 4)
            .Select(v => v.Value)
            .OrderBy(r => r)
            .ToArray();

        Assert.AreEqual(expected, vals);
        
        vals = testMap.WhereKeyStartsWith(new object[] {1, 1, 10}, 3).OrderBy(a => a).ToArray();
        expected = Array.Empty<int>();

        Assert.AreEqual(expected, vals);
       
        vals = testMap.WhereKeyStartsWith(new object[] {1, 10, 0}, 2).OrderBy(a => a).ToArray();
        expected = Array.Empty<int>();

        Assert.AreEqual(expected, vals);
        
        vals = testMap.WhereKeyStartsWith(new object[] { 10, 0, 0}, 1).OrderBy(a => a).ToArray();
        expected = Array.Empty<int>();

        Assert.AreEqual(expected, vals);
    }

    [Test]
    public void TestStartWithArrSingle()
    {
        var map = new TupleTrieMap<int, int, int, int, int, int>();
           
        map.Add((1, 1, 1, 1, 1), 1);
        
        var testMap = (IObjectKeyTupleTrieMap<int>)map; 

        var vals = testMap.WhereKeyStartsWith(new object[] { 1, 1 }, 2).OrderBy(a => a).First();
        
        Assert.AreEqual(1, vals);
    }

    [Test]
    public void TestExtraApi()
    {
        var m3 = new TupleTrieMap<string, string, string>() { { ("1", "1"), "1" } };
        
        m3.SetStorageFactory(MapFactory);
            
        m3.Put(("2", "2"), "2");
            
        Assert.AreEqual("2", m3.GetSet(("2", "2"), (v, m) => v.Item1));
            
        Assert.False(m3.ContainsKey(("3", "3")));
            
        Assert.AreEqual("3", m3.GetSet(("3", "3"), (v, m) => m[v] = v.Item1));
        Assert.AreEqual("3", m3[("3", "3")]);
    }

    private static IDictionary<object, AbstractTupleTrieMap<(string, string), string>.TrieLinkNode<string>> MapFactory(Type type)
    {
        return null;
    }

    [Test]
    public void TestExceptionThrown()
    {
        var m3 = new TupleTrieMap<string, string, int>() { { ("1", "1"), 1 } };
            
        Assert.Throws<ArgumentException>(() => m3.Add(("1", "1"), 2));
        Assert.Throws<ArgumentException>(() => m3.WhereKeyStartsWith(("1", "1"), 3).ToArray());
            
        var m4 = new TupleTrieMap<string, string, int>() { { ("1", "1"), 1 } };

        Assert.Throws<ArgumentNullException>(() => m4.Add((null, null), 2));
        Assert.Throws<ArgumentNullException>(() => m4[(null, null)] = 2);
    }
    
    [Test]
    public void TestArrayKeys()
    {
        var m3 = new TupleTrieMap<string, string, int>() { { ("1", "1"), 1 } };
        
        var testMap = (IObjectKeyTupleTrieMap<int>)m3; 

        var valueTuples = testMap.GetObjKeyValues().ToArray();
        
        Assert.AreEqual(new[] {(new[] {"1", "1"}, 1)}, valueTuples);
    }
    
    [Test]
    public void TestArrayAccess()
    {
        var m3 = new TupleTrieMap<string, string, int>();

        var testMap = (IObjectKeyTupleTrieMap<int>)m3; 
        
        testMap.Add(new[] { "1", "1" }, 1);
        
        Assert.AreEqual(1, testMap.Count);
        
        Assert.AreEqual(1, testMap[new[] { "1", "1" }]);
        
        Assert.True(testMap.TryGetValue(new[] { "1", "1" }, out var _));
        
        Assert.True(testMap.ContainsKey(new[] { "1", "1" }));
        
        Assert.False(testMap.ContainsKey(Enumerable.Range(0, 100).Select(i => "1").ToArray()));
    }
    
    [Test]
    public void TestArrayRemove()
    {
        var m3 = new TupleTrieMap<string, string, int>();
        
        var testMap = (IObjectKeyTupleTrieMap<int>)m3; 
        
        testMap.Add(new[] { "1", "1" }, 1);
        
        Assert.False(testMap.Remove(Enumerable.Range(0, 100).Select(i => "1").ToArray()));
        
        Assert.True(testMap.Remove(new[] { "1", "1" }));
        
        Assert.AreEqual(0, m3.Count);
    }
    
    [Test]
    public void TestExceptionThrownArray()
    {
        var m3 = new TupleTrieMap<string, string, int>() { { ("1", "1"), 1 } };
            
        var testMap = (IObjectKeyTupleTrieMap<int>)m3; 
        
        Assert.Throws<ArgumentException>(() => testMap.Add(new[] {"1", "1"}, 2));
        Assert.Throws<ArgumentException>(() => testMap.WhereKeyStartsWith(new[] {"1", "1"}, 3).ToArray());
            
        var m4 = new TupleTrieMap<string, string, int>() { { ("1", "1"), 1 } };

        Assert.Throws<ArgumentNullException>(() => testMap.Add(null, 2));
        Assert.Throws<ArgumentNullException>(() => testMap.Add(new string[] {null, null}, 2));
        Assert.Throws<ArgumentNullException>(() => testMap[new string[] {null, null}] = 2);
        Assert.Throws<ArgumentNullException>(() => testMap[null] = 2);
    }
    
    [Test]
    public void TestDefaultDictArr5()
    {
        var map =  new TupleTrieMap<string, string, string, string, string, Data<string>>();
            
        map.EnsureValues((k) => new Data<string>());

        var key = new object[] {"0", "1", "2", "3", "5"};

        var testMap = (IObjectKeyTupleTrieMap<Data<string>>)map; 
        
        testMap[key].Add("val0");
            
        Assert.True(testMap.ContainsKey(key));
        
        var keyValuePairs = new TupleTrieMapDebugView<string, string, string, string,  string, Data<string>>(map).Items;
        
        Assert.AreEqual(1, keyValuePairs.Length);
    }
    
    [Test]
    public void TestDefaultDictArr4()
    {
        var map =  new TupleTrieMap<string, string, string, string, Data<string>>();
            
        map.EnsureValues((k) => new Data<string>());

        var key = new object[] {"0", "1", "2", "3"};

        var testMap = (IObjectKeyTupleTrieMap<Data<string>>)map; 
        
        testMap[key].Add("val0");
            
        Assert.True(testMap.ContainsKey(key));
        
        var keyValuePairs = new TupleTrieMapDebugView<string, string, string, string, Data<string>>(map).Items;
        
        Assert.AreEqual(1, keyValuePairs.Length);
    }
    
    [Test]
    public void TestDefaultDictArr3()
    {
        var map =  new TupleTrieMap<string, string, string, Data<string>>();
            
        map.EnsureValues((k) => new Data<string>());

        var key = new object[] {"0", "1", "2"};

        var testMap = (IObjectKeyTupleTrieMap<Data<string>>)map; 
        
        testMap[key].Add("val0");
            
        Assert.True(testMap.ContainsKey(key));
        
        var keyValuePairs = new TupleTrieMapDebugView<string, string, string, Data<string>>(map).Items;
        
        Assert.AreEqual(1, keyValuePairs.Length);
    }
    
    [Test]
    public void TestDefaultDictArr2()
    {
        var map =  new TupleTrieMap<string, string, Data<string>>();
            
        map.EnsureValues((k) => new Data<string>());

        var key = new object[] {"0", "1"};

        var testMap = (IObjectKeyTupleTrieMap<Data<string>>)map; 
        
        testMap[key].Add("val0");

        Assert.True(testMap.ContainsKey(key));
        
        var keyValuePairs = new TupleTrieMapDebugView<string, string, Data<string>>(map).Items;
        
        Assert.AreEqual(1, keyValuePairs.Length);
    }
}