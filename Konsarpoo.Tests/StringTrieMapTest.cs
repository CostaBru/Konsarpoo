using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Konsarpoo.Collections.Allocators;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests
{
    [TestFixture(16, AllocatorType.GC, 0)]
    [TestFixture(32, AllocatorType.Mixed, 16)]
    [TestFixture(16, AllocatorType.Pool, 0)]
    [TestFixture(1024, AllocatorType.GC, 0)]
    [TestFixture(1024, AllocatorType.Mixed, 512)]
    [TestFixture(1024, AllocatorType.Pool, 0)]
    public class StringTrieMap : BaseTest
    {
        public StringTrieMap(int? maxSizeOfArrayBucket, AllocatorType allocatorType, int gcLen) : base(maxSizeOfArrayBucket, allocatorType, gcLen)
        {
        }
        
        [Test]
        public void TestRemoveIfEmpty()
        {
            var map = new StringTrieMap<int>();

            Assert.False(map.Remove("0"));
        }

        [Test]
        public void TestSmall()
        {
            var map = new StringTrieMap<string>();

            map.Add("test0", "val0");
            map.Add("test1", "val1");
            
            Assert.AreEqual("val0", map["test0"]);
            Assert.AreEqual("val1", map["test1"]);

            Assert.True(map.ContainsValue("val0"));
            Assert.True(map.ContainsValue("val1"));
            Assert.False(map.ContainsValue("val3"));

            Assert.NotZero(map.GetHashCode());

            var dict2 = map.ToMap();

            dict2.Add("test3", "123");
            
            var dict = new Dictionary<string, string>();

            dict.Add("test0", "val0");
            dict.Add("test1", "val1");

            Assert.AreEqual(dict.Count, map.Length);

            Assert.AreEqual("val0", map["test0"]);
            Assert.AreEqual("val1", map["test1"]);

            Test(dict, map, false);

            var map2 = new StringTrieMap<int>() { { "1", 1 } };

            map2.Append(new KeyValuePair<string, int>("2", 2));

            Assert.True(map2["2"] == 2);
            Assert.True(map2["1"] == 1);

            Assert.False(((ICollection<KeyValuePair<string, int>>)map2).IsReadOnly);
            Assert.True(map2.CaseSensitive);
        }


        [Test]
        public void TestSmallIgnoreCase()
        {
            var map = new StringTrieMap<string>(false);
            var dict = new Dictionary<string, string>();

            map.Add("test0", "val0");
            map.Add("test1", "val1");

            dict.Add("TEsT0", "val0");
            dict.Add("TEsT1", "val1");

            Assert.AreEqual("val0", map["TEsT0"]);
            Assert.AreEqual("val1", map["TEsT1"]);

            Test(dict, map, true);
        }

        [Test]
        public void TestCommonIgnoreCase()
        {
            var map = new StringTrieMap<string>(false);
            var dict = new Dictionary<string, string>();

            map.Add("test0", "val0");
            map.Add("test1", "val1");
            map.Add("test2", "val2");
            map.Add("test3", "val3");
            map.Add("test4", "val4");
            map.Add("test5", "val5");
            map.Add("test6", "val6");
            map.Add("test7", "val7");

            Assert.True(map.ContainsValue("val0"));
            Assert.True(map.ContainsValue("val1"));
            Assert.False(map.ContainsValue("123123"));

            dict.Add("TEsT0", "val0");
            dict.Add("TEsT1", "val1");
            dict.Add("TEsT2", "val2");
            dict.Add("tesT3", "val3");
            dict.Add("tEst4", "val4");
            dict.Add("tEst5", "val5");
            dict.Add("tEst6", "val6");
            dict.Add("tEst7", "val7");

            Assert.AreEqual("val0", map["TEsT0"]);
            Assert.AreEqual("val1", map["TEsT1"]);
            Assert.AreEqual("val2", map["TEsT2"]);
            Assert.AreEqual("val3", map["test3"]);
            Assert.AreEqual("val4", map["tEst4"]);

            Test(dict, map, true);
        }

        [Test]
        public void TestAddHuge()
        {
            var testData = new StringTrieMap<int>();
            
            for (int i = 0; i < 1000000; i++)
            {
                testData.Add(i.ToString(), i);
            }
            
            for (int i = 0; i < 1000000; i++)
            {
                Assert.True(testData.TryGetValue(i.ToString(), out var val));
                Assert.AreEqual(i, val);
            }
            
            testData.Dispose();
        }

        [Test]
        public void TestCommon()
        {
            var map = new StringTrieMap<string>();
            var dict = new Dictionary<string, string>();

            map.Add("test0", "val0");
            map.Add("test1", "val1");
            map.Add("test2", "val2");
            map.Add("test3", "val3");
            map.Add("test4", "val4");
            map.Add("test5", "val5");
            map.Add("test6", "val6");

            dict.Add("test0", "val0");
            dict.Add("test1", "val1");
            dict.Add("test2", "val2");
            dict.Add("test3", "val3");
            dict.Add("test4", "val4");
            dict.Add("test5", "val5");
            dict.Add("test6", "val6");

            Assert.AreEqual("val0", map["test0"]);
            Assert.AreEqual("val1", map["test1"]);
            Assert.AreEqual("val2", map["test2"]);
            Assert.AreEqual("val3", map["test3"]);
            Assert.AreEqual("val4", map["test4"]);
            Assert.AreEqual("val5", map["test5"]);
            Assert.AreEqual("val6", map["test6"]);

            Test(dict, map, false);
        }
        
        [Test]
        public void TestDefaultDict()
        {
            var map = new StringTrieMap<Data<string>>();
            
            map.EnsureValues((k) => new Data<string>());
            
            var dict = new Dictionary<string, Data<string>>();

            Assert.False(map.ContainsKey("test0"));
            
            map["test0"].Add("val0");
            
            Assert.True(map.ContainsKey("test0"));
            
            map["test1"].Add( "val1");
            map["test2"].Add( "val2");
            map["test3"].Add( "val3");
            map["test4"].Add( "val4");
            map["test5"].Add( "val5");
            map["test6"].Add( "val6");

            Func<Data<string>> valueFactory = () => new Data<string>();
            
            dict.GetOrAdd("test0", valueFactory).Add("val0");
            dict.GetOrAdd("test1", valueFactory).Add( "val1");
            dict.GetOrAdd("test2", valueFactory).Add( "val2");
            dict.GetOrAdd("test3", valueFactory).Add( "val3");
            dict.GetOrAdd("test4", valueFactory).Add( "val4");
            dict.GetOrAdd("test5", valueFactory).Add( "val5");
            dict.GetOrAdd("test6", valueFactory).Add( "val6");

            Assert.AreEqual("val0", map["test0"].SingleOrDefault());
            Assert.AreEqual("val1", map["test1"].SingleOrDefault());
            Assert.AreEqual("val2", map["test2"].SingleOrDefault());
            Assert.AreEqual("val3", map["test3"].SingleOrDefault());
            Assert.AreEqual("val4", map["test4"].SingleOrDefault());
            Assert.AreEqual("val5", map["test5"].SingleOrDefault());
            Assert.AreEqual("val6", map["test6"].SingleOrDefault());

            Assert.True(map == dict);
        }

        [Test]
        public void TestAdd()
        {
            var map = new StringTrieMap<string>
            {
                { "test0", "val0" },
                { "test1", "val1" },
                { "test2", "val2" },
                { "test3", "val3" },
                { "test4", "val4" },
                { "test5", "val5" },
                { "test6", "val6" }
            };

            var map2 = new StringTrieMap<string>
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
        }
        
        [Test]
        public void TestCopyCtr()
        {
            var map1 = new StringTrieMap<string>
            {
                { "test0", "val0" },
                { "1test1", "val1" },
                { "t2est2", "val2" },
                { "te4st3", "val3" },
                { "tes5t4", "val4" },
                { "test5", "val5" },
                { "test71", "val6" }
            };

            var map2 = new StringTrieMap<string>(map1);
            
            Assert.True(map1 == map2);
        }

        [Test]
        public void ValueByRefTest()
        {
            var dict = new StringTrieMap<int>
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

        private static void Test(Dictionary<string, string> dict, StringTrieMap<string> map, bool ignoreCase)
        {
            var keyTest = "t____";

            string val1;
            Assert.False(map.TryGetValue(keyTest, out val1));
            Assert.Null(val1);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                var i = map[keyTest];
            });

            map[keyTest] = "12313";

            Assert.True(map.TryGetValue(keyTest, out val1));
            Assert.AreEqual("12313", val1);

            dict[keyTest] = "12313";

            foreach (var keyValuePair in dict)
            {
                Assert.AreEqual(keyValuePair.Value, map[keyValuePair.Key]);
                Assert.True(map.Contains(keyValuePair));
                Assert.True(map.ContainsKey(keyValuePair.Key));
                string val;
                Assert.True(map.TryGetValue(keyValuePair.Key, out val));
                Assert.AreEqual(keyValuePair.Value, val);
            }

            var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

            var data = map.ToData();

            var hashSet1 = new HashSet<string>(data.Select(s => s.Key), comparer);

            foreach (var key in dict.Keys)
            {
                Assert.True(hashSet1.Contains(key));
            }

            var hashSet2 = new HashSet<string>(map.Values);

            foreach (var key in dict.Values)
            {
                Assert.True(hashSet2.Contains(key));
            }

            var pairs1 = map.ToArray();
            var pairs2 = dict.ToArray();

            Assert.AreEqual(pairs1.Length, pairs2.Length);

            var hashSet3 = new HashSet<string>(pairs1.Select(p => p.Key), comparer);

            foreach (var key in pairs2)
            {
                Assert.True(hashSet3.Contains(key.Key));
            }
            
            map["KEY1"] = "V";

            map.Clear();

            Assert.AreEqual(0, map.Count);

            foreach (var keyValuePair in dict)
            {
                Assert.False(map.Contains(keyValuePair));
                Assert.False(map.ContainsKey(keyValuePair.Key));
            }

            Assert.True(map.TryAdd("KEY1", "VAL1"));

            map["KEY1"] = "V";

            var tryAdd = map.TryAdd("KEY1", "VAL1");
            
            Assert.False(tryAdd);
            Assert.NotNull(map.SyncRoot);
        }

        [Test]
        public void TestRemove()
        {
            var map = new StringTrieMap<int>();
            
            map.Add("test", 1);
            map.Add("te", 2);
            map.Add("t", 3);

            Assert.AreEqual(3, map.Count);
          
            Assert.True(map.Remove("test"));
            Assert.AreEqual(2, map.Count);
            
            Assert.True(map.Remove("te"));
            Assert.AreEqual(1, map.Count);

            Assert.True(map.Remove("t"));
            Assert.AreEqual(0, map.Count);
            
            Assert.False(map.Remove("t"));
            Assert.AreEqual(0, map.Count);
            
            map = new StringTrieMap<int>
            {
                { "test", 1 },
                { "te", 2 },
                { "t", 3 },
            };
            
            Assert.True(map.Remove("t"));
            Assert.AreEqual(2, map.Count);
            
            Assert.True(map.Remove("te"));
            Assert.AreEqual(1, map.Count);
            
            Assert.True(map.Remove("test"));
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void TestInt()
        {
            var dict = (IDictionary<string, int>)new StringTrieMap<int>
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
            var dict = (IReadOnlyDictionary<string, int>)new StringTrieMap<int>
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
            var map = new StringTrieMap<int>(true);

            map.Add("qwerty", 123);
            map.Add("test", 421);

            var serializeWithDcs = SerializeHelper.SerializeWithDcs(map);

            var deserializeWithDcs = SerializeHelper.DeserializeWithDcs<StringTrieMap<int>>(serializeWithDcs);

            Assert.True(deserializeWithDcs == map);
            Assert.AreEqual(true, map.CaseSensitive);
        }
      
        [Test]
        public void TestSerialization2()
        {
            var map = new StringTrieMap<int>(true);

           foreach (var i in Enumerable.Range(1, 1024))
           {
               map.Add(i.ToString(), i);
           }

            StringTrieMap<int> clone1 = SerializeHelper.Clone<StringTrieMap<int>>(map);

            Assert.True(clone1 == map);
        }
      
        [Test]
        public void TestGetOrDefault()
        {
            var map = new StringTrieMap<string>();

            Assert.Null(map.GetOrDefault("1"));
            Assert.AreEqual(string.Empty, map.GetOrDefault("1", string.Empty));
        }
        
        [Test]
        public void TestGetOrCreate()
        {
            var map = new StringTrieMap<Data<int>>();

            var ints = map.GetOrAdd("1", () => new Data<int>());
            
            Assert.AreEqual(0,  map.GetOrDefault("1").Count);

            var nullMap = (StringTrieMap<Data<int>>)null;

            Assert.Throws<ArgumentNullException>(() => nullMap.GetOrAdd("1", () => new Data<int>()));
            
            Assert.AreEqual(0, ints.Count);
            
            map.GetOrAdd("1", () => new Data<int>()).Add(1);

            Assert.AreEqual(1, map["1"].Count);
        }

      
        [Test]
        public void TestAddExc()
        {
            var m3 = new StringTrieMap<int>() { { "1", 1 } };
            
            Assert.Throws<ArgumentException>(() => m3.Add("1", 2));
            
            var m4 = new Map<string, int>() { { "1", 1 } };

            Assert.Throws<ArgumentNullException>(() => m4.Add(null, 2));
            
            m4.Add("2", 3);
            
            Assert.AreEqual(3, m4["2"]);
        }

        [Test]
        public void TestExtraApi()
        {
            var m3 = new StringTrieMap<string>() { { "1", "1" } };
            
            m3.Put("2", "2");
            
            Assert.AreEqual("2", m3.GetSet("2", (v, m) => v));
            
            Assert.False(m3.ContainsKey("3"));
            
            Assert.AreEqual("3", m3.GetSet("3", (v, m) => m[v] = v));
            Assert.AreEqual("3", m3["3"]);
        }
        
        [Test]
        public void TestStartWith()
        {
            var m3 = new StringTrieMap<string>();
           
            m3.Add("a", "a");
            m3.Add("abc", "abc");
            m3.Add("abcd", "abcd");
            m3.Add("bc", "bc");
            m3.Add("c", "c");

            var vals = m3.WhereKeyStartsWith("a").OrderBy(a => a).ToArray();
            var expected = new[]{"a", "abc", "abcd"}.OrderBy(a => a).ToArray();
            
            Assert.AreEqual(expected, vals);
            
            vals = m3.WhereKeyStartsWith("a123").OrderBy(a => a).ToArray();
            expected = Array.Empty<string>();
            
            Assert.AreEqual(expected, vals);
        }
        
        [Test]
        public void TestStartWithCaseInsensitive()
        {
            var m3 = new StringTrieMap<string>(false)
            {
                { "a", "a" },
                { "ABC", "abc" },
                { "aBcd", "abcd" },
                { "bc", "bc" },
                { "C", "c" },
            };

            var vals = m3.WhereKeyStartsWith("a").OrderBy(a => a).ToArray();
            var expected = new[]{"a", "abc", "abcd"}.OrderBy(a => a).ToArray();
            
            Assert.AreEqual(expected, vals);
            
            vals = m3.WhereKeyStartsWith("a123").OrderBy(a => a).ToArray();
            expected = Array.Empty<string>();
            
            Assert.AreEqual(expected, vals);
        }
        
        [Test]
        public void TestStartWithEmpty()
        {
            var m3 = new StringTrieMap<string>()
            {
                { "a", "a" },
            };

            var vals = m3.WhereKeyStartsWith(string.Empty).OrderBy(a => a).ToArray();
            var expected = new[]{"a"}.OrderBy(a => a).ToArray();
            
            Assert.AreEqual(expected, vals);
        }
        
        [Test]
        public void TestKeyContainsSubstring()
        {
            var m3 = new StringTrieMap<string>()
            {
                { "abc", "abc" },
                { "abcd", "abcd" },
                { "dbc", "dbc" },
            };

            var vals = m3.WhereKeyContains("abc").OrderBy(a => a).ToArray();
            var expected = new[]{"abc", "abcd"}.OrderBy(a => a).ToArray();
            
            Assert.AreEqual(expected, vals);
        }
        
        [Test]
        public void TestEndsWithSubstring()
        {
            var m3 = new StringTrieMap<string>()
            {
                { "abc", "abc" },
                { "abcd", "abcd" },
                { "dbc", "dbc" },
            };

            var vals = m3.WhereKeyEndsWith("bc").OrderBy(a => a).ToArray();
            var expected = new[]{"abc", "dbc"}.OrderBy(a => a).ToArray();
            
            Assert.AreEqual(expected, vals);
        }
        
        [Test]
        public void TestEndsWithEmpty()
        {
            var m3 = new StringTrieMap<string>()
            {
                { "a", "a" },
            };

            var vals = m3.WhereKeyEndsWith(string.Empty).OrderBy(a => a).ToArray();
            var expected = new[]{"a"}.OrderBy(a => a).ToArray();
            
            Assert.AreEqual(expected, vals);
        }
    }
}