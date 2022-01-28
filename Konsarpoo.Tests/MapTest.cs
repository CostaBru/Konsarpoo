using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests
{
    [TestFixture(16)]
    [TestFixture(1024)]
    [TestFixture(null)]
    public class MapTest
    {
        private readonly int m_maxSizeOfArrayBucket;

        public MapTest(int? maxSizeOfArrayBucket)
        {
            m_maxSizeOfArrayBucket = maxSizeOfArrayBucket ?? 1024 * 1024;
        }

        [SetUp]
        public void SetUp()
        {
            ArrayPoolGlobalSetup.SetMaxSizeOfArrayBucket(m_maxSizeOfArrayBucket);
        }

        [Test]
        public void TestSmall()
        {
            var map = new Map<string, string>();
            var dict = new Dictionary<string, string>();

            map.Add("test0", "val0");
            map.Add("test1", "val1");

            Assert.True(map.ContainsValue("val0"));
            Assert.True(map.ContainsValue("val1"));
            Assert.False(map.ContainsValue("val3"));

            Assert.NotZero(map.GetHashCode());

            var dict2 = map.ToMap();
            Assert.AreEqual(map.GetHashCode(), dict2.GetHashCode());

            dict2.Add("test3", "123");

            Assert.AreNotEqual(map.GetHashCode(), dict2.GetHashCode());

            dict.Add("test0", "val0");
            dict.Add("test1", "val1");

            Assert.AreEqual(dict.Count, map.Length);

            Assert.AreEqual("val0", map["test0"]);
            Assert.AreEqual("val1", map["test1"]);

            Assert.True(map == dict);
            Assert.True(map.Equals(map.ToMap()));

            Test(dict, map, StringComparer.Ordinal);

            var map2 = _.Map((1, 1));

            map2.Append(new KeyValuePair<int, int>(2, 2));

            Assert.True(map2[2] == 2);
            Assert.True(map2[1] == 1);

            for (int i = 0; i < map2.Count; i++)
            {
                var keyAt = map2.KeyAt(i);

                Assert.True(map2[keyAt] == keyAt);
            }

            Assert.False(((ICollection<KeyValuePair<int, int>>)map2).IsReadOnly);
            Assert.NotNull(map2.Comparer);
        }


        [Test]
        public void TestSmallIgnoreCase()
        {
            var map = new Map<string, string>(StringComparer.OrdinalIgnoreCase);
            var dict = new Dictionary<string, string>();

            map.Add("test0", "val0");
            map.Add("test1", "val1");

            dict.Add("TEsT0", "val0");
            dict.Add("TEsT1", "val1");

            Assert.AreEqual("val0", map["TEsT0"]);
            Assert.AreEqual("val1", map["TEsT1"]);

            Test(dict, map, StringComparer.OrdinalIgnoreCase);
        }

        [Test]
        public void TestCommonIgnoreCase()
        {
            var map = new Map<string, string>(StringComparer.OrdinalIgnoreCase);
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

            Test(dict, map, StringComparer.OrdinalIgnoreCase);
        }


        [Test]
        public void TestCommon()
        {
            var map = new Map<string, string>();
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

            Assert.True(map == dict);

            Test(dict, map, StringComparer.Ordinal);
        }

        [Test]
        public void TestAdd2([Values(0,1,2,1000, 1_0000)] int count)
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
                    
                    for (int j = 0; j < keys1.Length; j++)
                    {
                        Assert.AreEqual(keys1[j], keys2[j]);
                    }
                }

                if (i < 1000)
                {
                    var values1 = new int[map.Values.Count];
                    var values2 = new int[map.Values.Count];

                    map.Values.CopyTo(values1, 0);
                    map.Values.CopyTo(values2, 0);
                    
                    for (int j = 0; j < values1.Length; j++)
                    {
                        Assert.AreEqual(values1[j], values2[j]);
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

        private static void Test(Dictionary<string, string> dict, Map<string, string> map,
            IEqualityComparer<string> comparer)
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

            var hashSet1 = new HashSet<string>(map.Keys, comparer);

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

            map.Clear();

            Assert.AreEqual(0, map.Count);

            foreach (var keyValuePair in dict)
            {
                Assert.False(map.Contains(keyValuePair));
                Assert.False(map.ContainsKey(keyValuePair.Key));
            }

            Assert.True(map.TryAdd("KEY1", "VAL1"));
            Assert.False(map.TryAdd("KEY1", "VAL1"));
            Assert.NotNull(map.SyncRoot);
        }

        [Test]
        public void TestOp()
        {
            Func<int, int> keySelector = i => i * 10;
            Func<int, int> elementSelector = i => i;

            Map<int, int> l1 = new Data<int>(Enumerable.Range(0, 5)).ToMap(keySelector, elementSelector);
            Map<int, int> l2 = new Data<int>(Enumerable.Range(5, 5)).ToMap(keySelector, elementSelector);


            var lk1 = (IReadOnlyCollection<int>)new Data<int>(Enumerable.Range(0, 5))
                .ToMap(keySelector, elementSelector).Keys.ToData();
            var lk2 = (IReadOnlyCollection<int>)new Data<int>(Enumerable.Range(5, 5))
                .ToMap(keySelector, elementSelector).Keys.ToData();

            var l3 = l1 + l2;

            Assert.AreEqual(new Data<int>(Enumerable.Range(0, 10)).ToMap(keySelector, elementSelector), l3);

            Assert.AreEqual(l2, l3 - l1);
            Assert.AreEqual(l1, l3 - l2);

            Assert.AreEqual(l2, l3 - lk1);
            Assert.AreEqual(l1, l3 - lk2);

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

        [Test]
        public void TestSerialization2()
        {
            var map = new Map<string, int>(StringComparer.OrdinalIgnoreCase);

            map.Add("qwerty", 123);
            map.Add("test", 421);

            var clone = SerializeHelper.Clone<Map<string, int>>(map);

            Assert.AreEqual(clone, map);
        }


        [Test]
        public void TestGetPrime()
        {
            var prime = Prime.GetPrime(2946901 + 1);
            
            Assert.AreEqual(2946907, prime);

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
            
            Assert.AreEqual(0, ints.Count);
            
            map.GetOrAdd(1, () => new Data<int>()).Add(1);

            Assert.AreEqual(1, map[1].Count);
        }
    }
}