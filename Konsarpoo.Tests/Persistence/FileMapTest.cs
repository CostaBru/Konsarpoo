using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Konsarpoo.Collections.Persistence;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests.Persistence
{
    [TestFixture(true, CompressionLevel.Fastest)]
    [TestFixture(true, CompressionLevel.NoCompression)]
    [TestFixture(false, CompressionLevel.Fastest)]
    [TestFixture(false, CompressionLevel.NoCompression)]
    public class FileMapTest 
    {
        private readonly CompressionLevel m_compressionLevel;
        private readonly byte[] m_key;
        private string m_testFile;

        public FileMapTest(bool crypted, CompressionLevel compressionLevel)
        {
            m_compressionLevel = compressionLevel;
            m_key = crypted ? Encoding.Unicode.GetBytes("TestKey") : null;
        }

        [SetUp]
        public void SetUp()
        {
            
            m_testFile = Path.GetTempPath() + Guid.NewGuid() + ".tmp";
        }

        [TearDown]
        public void TearDown()
        {
            var fileNames = FileMap<int, int>.GetFileNames(m_testFile);

            foreach (var fileName in fileNames)
            {
                File.Delete(fileName);
            }
        }
        
        [Test]
        public void TestRemoveIfEmpty()
        {
            var map = new Map<int,int>();

            Assert.False(map.Remove(0));
        }

        [Test]
        public void TestSmall()
        {
            using var map = FileMap<string, string>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);
            var dict = new Dictionary<string, string>();

            map.Add("test0", "val0");
            map.Add("test1", "val1");

            Assert.True(map.ContainsValue("val0"));
            Assert.True(map.ContainsValue("val1"));
            Assert.False(map.ContainsValue("val3"));

            Assert.NotZero(map.GetHashCode());

            var dict2 = map.ToMap();

            dict2.Add("test3", "123");
            dict.Add("test0", "val0");
            dict.Add("test1", "val1");

            Assert.AreEqual(dict.Count, map.Length);

            Assert.AreEqual("val0", map["test0"]);
            Assert.AreEqual("val1", map["test1"]);

            Assert.True(map.ToMap() == dict);

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
            using var map = FileMap<string, string>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel, StringComparer.OrdinalIgnoreCase);
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
            using var map = FileMap<string, string>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel, StringComparer.OrdinalIgnoreCase);
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
        public void TestAddHuge()
        {
            {
                using var testData = FileMap<int, int>.OpenOrCreate(m_testFile, 1024 , m_key, m_compressionLevel, storageArrayBufferCapacity: 4);

                testData.BeginWrite();
                
                for (int i = 0; i < 10000; i++)
                {
                    testData.Add(i, i);
                }
                
                testData.EndWrite();
            }
            {
                using var testData = FileMap<int, int>.OpenOrCreate(m_testFile, 1024, m_key, m_compressionLevel, storageArrayBufferCapacity: 4);
                
                for (int i = 0; i < 10000; i++)
                {
                   Assert.True(testData.ContainsKey(i));
                }
            }
        }

        [Test]
        public void TestCommon()
        {
            {
                using var map = FileMap<string, string>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);
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

                Assert.True(map.ToMap() == dict);

                Test(dict, map, StringComparer.Ordinal);
            }
            {
                using var map = FileMap<string, string>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

                Assert.AreEqual("val0", map["test0"]);
                Assert.AreEqual("val1", map["test1"]);
                Assert.AreEqual("val2", map["test2"]);
                Assert.AreEqual("val3", map["test3"]);
                Assert.AreEqual("val4", map["test4"]);
                Assert.AreEqual("val5", map["test5"]);
                Assert.AreEqual("val6", map["test6"]);
            }
        }

        [Test]
        public void TestDefaultDict()
        {
            {
                using var map = FileMap<string, Data<string>>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

                map.EnsureValues((k) => new Data<string>());

                var dict = new Dictionary<string, Data<string>>();

                Assert.False(map.ContainsKey("test0"));

                map["test0"].Add("val0");

                Assert.True(map.ContainsKey("test0"));

                map["test1"].Add("val1");
                map["test2"].Add("val2");
                map["test3"].Add("val3");
                map["test4"].Add("val4");
                map["test5"].Add("val5");
                map["test6"].Add("val6");
                
                map.Flush();

                Func<Data<string>> valueFactory = () => new Data<string>();

                dict.GetOrAdd("test0", valueFactory).Add("val0");
                dict.GetOrAdd("test1", valueFactory).Add("val1");
                dict.GetOrAdd("test2", valueFactory).Add("val2");
                dict.GetOrAdd("test3", valueFactory).Add("val3");
                dict.GetOrAdd("test4", valueFactory).Add("val4");
                dict.GetOrAdd("test5", valueFactory).Add("val5");
                dict.GetOrAdd("test6", valueFactory).Add("val6");

                Assert.AreEqual("val0", map["test0"].SingleOrDefault());
                Assert.AreEqual("val1", map["test1"].SingleOrDefault());
                Assert.AreEqual("val2", map["test2"].SingleOrDefault());
                Assert.AreEqual("val3", map["test3"].SingleOrDefault());
                Assert.AreEqual("val4", map["test4"].SingleOrDefault());
                Assert.AreEqual("val5", map["test5"].SingleOrDefault());
                Assert.AreEqual("val6", map["test6"].SingleOrDefault());

                Assert.True(map.ToMap() == dict);
                
                map.Flush();
            }
            {
                using var map = FileMap<string, Data<string>>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);
                
                Assert.AreEqual("val0", map["test0"].SingleOrDefault());
                Assert.AreEqual("val1", map["test1"].SingleOrDefault());
                Assert.AreEqual("val2", map["test2"].SingleOrDefault());
                Assert.AreEqual("val3", map["test3"].SingleOrDefault());
                Assert.AreEqual("val4", map["test4"].SingleOrDefault());
                Assert.AreEqual("val5", map["test5"].SingleOrDefault());
                Assert.AreEqual("val6", map["test6"].SingleOrDefault());
            }
        }

        [Test]
        public void TestAdd2([Values(0,1,2,1000)] int count)
        {
            {
                using var map = FileMap<int, int>.OpenOrCreate(m_testFile, 512, m_key, m_compressionLevel);
                var dict = new Map<int, int>();

                map.BeginWrite();

                for (int i = 0; i < count; i++)
                {
                    map[i] = i;
                    dict[i] = i;

                    Assert.AreEqual(dict[i], map[i]);
                }

                map.EndWrite();
            }

            {
                using var map = FileMap<int, int>.OpenOrCreate(m_testFile, 512, m_key, m_compressionLevel);

                for (int i = 0; i < count; i++)
                {
                    Assert.True(map.ContainsKey(i));
                }
            }
        }
       
        private static void Test(Dictionary<string, string> dict, FileMap<string, string> map,
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
          
            Assert.True(map.TryAdd("KEY1", "VAL1"));
            Assert.False(map.TryAdd("KEY1", "VAL1"));
            Assert.NotNull(map.SyncRoot);
        }

      

        [Test]
        public void TestInt()
        {
            using var map = FileMap<string, int>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);
               
            map.Add("test1", 1);
            map.Add("test2", 2);
            map.Add("test3", 2);

            var dict = (IDictionary<string, int>)map;
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
            using var map = FileMap<string, int>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);
               
            map.Add("test1", 1);
            map.Add("test2", 2);
            map.Add("test3", 2);

            var dict = (IReadOnlyDictionary<string, int>)map;

            var keys = dict.Keys.ToData();
            var vals = dict.Values.ToData();

            Assert.AreEqual(3, keys.Count);
            Assert.AreEqual(3, vals.Count);
        }

        [Test]
        public void TestGetOrDefault()
        {
            using var map = FileMap<int, string>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

            
            Assert.Null(map.GetOrDefault(1));
            Assert.AreEqual(string.Empty, map.GetOrDefault(1, string.Empty));
        }
        
        [Test]
        public void TestGetOrCreate()
        {
            using var map = FileMap<int, Data<int>>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

            var ints = map.GetOrAdd(1, () => new Data<int>());
            
            Assert.AreEqual(0,  map.GetOrDefault(1).Count);

            var nullMap = (Map<int, Data<int>>)null;

            Assert.Throws<ArgumentNullException>(() => nullMap.GetOrAdd(1, () => new Data<int>()));
            
            Assert.AreEqual(0, ints.Count);
            
            map.GetOrAdd(1, () => new Data<int>()).Add(1);

            Assert.AreEqual(1, map[1].Count);
        }

     
        
        [Test]
        public void TestAddExc()
        {
            using var map = FileMap<string, int>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

            map.Add("1", 1);

            Assert.Throws<ArgumentException>(() => map.Add("1", 2));

            Assert.Throws<ArgumentNullException>(() => map.Add(null, 2));
            
            Assert.AreEqual(1, map["1"]);
        }

        [Test]
        public void TestExtraApi()
        {
            using var map = FileMap<int, int>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);
            
            Assert.False(map.ContainsKey(3));
        }
    }
}