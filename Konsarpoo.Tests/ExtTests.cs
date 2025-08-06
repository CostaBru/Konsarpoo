using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests
{
    [TestFixture]
    public class ExtTests
    {
        [Test]
        public void TestData2()
        {
            var data2 = new DataFlatStore<int>(2048);
            var data1 = new Data<int>(0, 2048);

            for (int i = 0; i < 10000; i++)
            {
                data2.Add(i);
                data1.Add(i);
            }

            for (int i = 0; i < data2.Count; i++)
            {
                var value = data2[i];
                
                Assert.AreEqual(i, value);
            }
        }
        
        [Test]
        public void TestSeriHelper()
        {
            var seriLookup = BuiltinSeriHelper.GetSeriLookup();

            var map = new Dictionary<Type, Action<Array, Type>>()
            {
                { typeof(DateTime), FillDateTimeArray },
                { typeof(DateTimeOffset), FillDateTimeOffsetArray },
                { typeof(Guid), FillGuidArray },
            };

            foreach (var pair in seriLookup)
            {
                var type = pair.Key;
                var seri = pair.First();

                var instance = Array.CreateInstance(type, 100);
                
                var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

                var action = map.GetOrDefault(underlyingType, FillArray);

                action(instance, underlyingType);

                var bytes = seri.write(instance);

                var result = seri.read(bytes);
                
                Assert.AreEqual(instance, result);
            }
        }

        private static void FillArray(Array instance, Type underlyingType)
        {
            for (int i = 0; i < 100; i++)
            {
                instance.SetValue(Convert.ChangeType(i, underlyingType), i);
            }
        }
        
        private static void FillDateTimeArray(Array instance, Type underlyingType)
        {
            var dateTime = DateTime.Now;

            for (int i = 0; i < 100; i++)
            {
                instance.SetValue(dateTime.AddSeconds(i), i);
            }
        }
        
        private static void FillGuidArray(Array instance, Type underlyingType)
        {
            for (int i = 0; i < 100; i++)
            {
                instance.SetValue(Guid.NewGuid(), i);
            }
        }
        
        private static void FillDateTimeOffsetArray(Array instance, Type underlyingType)
        {
            var dateTime = DateTimeOffset.Now;

            for (int i = 0; i < 100; i++)
            {
                instance.SetValue(dateTime.AddSeconds(i), i);
            }
        }

        [Test]
        public void AddAll()
        {
            var data = new Data<int>(0, 16);
            var expected = new List<int>();

            for (int i = 0; i < ((int)ushort.MaxValue * 100); i++)
            {
                data.Add(i);
                expected.Add(i);
            }

            Assert.AreEqual(expected, data);

            data.Dispose();
        }
        
        [Test]
        public void AddAll1()
        {
            var data = new Data<int>(0, 1000_000);
            var expected = new List<int>();

            for (int i = 0; i < 1000_000; i++)
            {
                data.Add(i);
                expected.Add(i);
            }

            Assert.AreEqual(expected, data);

            data.Dispose();
        }
        
        [Test]
        public void Add10000()
        {
            var testData =  new Data<int>();
            
            var stack = new Data<int>();
            
            for (int i = 0; i < 10000; i++)
            {
                testData.Add(i);
                
                stack.Push(i);

                var d = testData[i];
                
                Assert.AreEqual(i, d);
            }

            while (stack.Count > 0)
            {
                var pop = stack.Pop();
            }
        }
        
        [Test]
        public void Add1000_16()
        {
            var stack = new Data<int>(0, 16);
            
            for (int i = 0; i < 1000; i++)
            {
                stack.Push(i);
            }

            while (stack.Count > 0)
            {
                var pop = stack.Pop();
            }
        }
        

        [Test]
        public void TestCreateFromArrays()
        {
            var test = Enumerable.Range(1, 100).Select(i => Enumerable.Range(1, 16).ToArray()).ToArray();

            var expected = test.SelectMany(i => i).ToData();

            var data = new Data<int>(test, 100 * 16);

            for (int i = 0; i < expected.Count; i++)
            {
                var l = data[i];
                
                Assert.AreEqual(expected[i], l);
            }
        }

        [Test]
        public void TestAny()
        {
            var list = (IReadOnlyList<int>)_.List(0, 1, 2, 3, 4, 5);

            Func<int, int, bool> compare1 = (v1, i) => v1 == i;

            Assert.True(list.Any(compare1, 0));
            Assert.False(list.Any(compare1, -1));

            Assert.False(list.IsEmpty(compare1, 0));
            Assert.True(list.IsEmpty(compare1, -1));

            Func<int, int, int, bool> compare2 = (v1, v2, i) => v1 == i || v2 == i;

            Assert.True(list.Any(compare2, 0, 1));
            Assert.False(list.Any(compare2, -1, -2));

            Assert.False(list.IsEmpty(compare2, 0, 1));
            Assert.True(list.IsEmpty(compare2, -1, -2));

            Func<int, int, int, int, bool> compare3 = (v1, v2, v3, i) => v1 == i || v2 == i || v3 == i;

            Assert.True(list.Any(compare3, 0, 1, 2));
            Assert.False(list.Any(compare3, -1, -2, -3));

            Assert.False(list.IsEmpty(compare3, 0, 1, 2));
            Assert.True(list.IsEmpty(compare3, -1, -2, -3));

            Func<int, int, int, int, int, bool> compare4 = (v1, v2, v3, v4, i) =>
                v1 == i || v2 == i || v3 == i || v4 == i;

            Assert.True(list.Any(compare4, 0, 1, 3, 3));
            Assert.False(list.Any(compare4, -1, -2, -3, -4));

            Assert.False(list.IsEmpty(compare4, 0, 1, 3, 3));
            Assert.True(list.IsEmpty(compare4, -1, -2, -3, -4));

            Func<int, int, int, int, int, int, bool> compare5 = (v1, v2, v3, v4, v5, i) =>
                v1 == i || v2 == i || v3 == i || v4 == i || v5 == i;

            Assert.True(list.Any(compare5, 0, 1, 3, 4, 4));
            Assert.False(list.Any(compare5, -1, -2, -3, -4, -5));

            Assert.False(list.IsEmpty(compare5, 0, 1, 3, 4, 4));
            Assert.True(list.IsEmpty(compare5, -1, -2, -3, -4, -5));

            Assert.True(list.Any(5, (i, i1) => i.Equals(i1)));
            Assert.True(list.Any(5, (i, i1) => i.Equals(i1), 3));
            Assert.False(list.Any(0, (i, i1) => i.Equals(i1), 3));

            Assert.True(list.Any(5, (i, i1) => i.Equals(i1)));
            Assert.True(list.Any(5, (i, i1) => i.Equals(i1), 3));

            Assert.Throws<ArgumentNullException>(() => list.Any(5, (Func<int, int, bool>)null));
        }

        [Test]
        public void TestFirst()
        {
            var list = (IReadOnlyList<string>)_.List("0", "1", "2", "3", "4", "5");

            Func<string, string, bool> comp1 = (i, v1) => i == v1;

            Assert.AreEqual("0", list.FirstOrDefault(comp1, "0"));
            Assert.AreEqual("0", list.First(comp1, "0"));
            Assert.Null(list.FirstOrDefault(comp1, "00"));
            Assert.Throws<InvalidOperationException>(() => list.First(comp1, "00"));

            Func<string, string, string, bool> comp2 = (i, v1, v2) => i == v1 || i.Contains(v2);

            Assert.AreEqual("1", list.FirstOrDefault(comp2, "00", "1"));
            Assert.AreEqual("1", list.First(comp2, "00", "1"));
            Assert.Null(list.FirstOrDefault(comp2, "00", "11"));
            Assert.Throws<InvalidOperationException>(() => list.First(comp2, "00", "11"));

            Func<string, string, string, string, bool> comp3 = (i, v1, v2, v3) =>
                i == v1 || i.Contains(v2) || i.StartsWith(v3);

            Assert.AreEqual("2", list.FirstOrDefault(comp3, "00", "11", "2"));
            Assert.AreEqual("2", list.First(comp3, "00", "11", "2"));
            Assert.Null(list.FirstOrDefault(comp3, "00", "11", "22"));
            Assert.Throws<InvalidOperationException>(() => list.First(comp3, "00", "11", "22"));

            Func<string, string, string, string, string, bool> comp4 = (i, v1, v2, v3, v4) =>
                i == v1 || i.Contains(v2) || i.StartsWith(v3) || i.EndsWith(v4);

            Assert.AreEqual("3", list.FirstOrDefault(comp4, "00", "11", "22", "3"));
            Assert.AreEqual("3", list.First(comp4, "00", "11", "22", "3"));
            Assert.Null(list.FirstOrDefault(comp4, "00", "11", "22", "33"));
            Assert.Throws<InvalidOperationException>(() => list.First(comp4, "00", "11", "22", "33"));

            Func<string, string, string, string, string, string, bool> comp5 = (i, v1, v2, v3, v4, v5) =>
                i == v1 || i.Contains(v2) || i.StartsWith(v3) || i.EndsWith(v4) ||
                string.Equals(i, v5, StringComparison.OrdinalIgnoreCase);

            Assert.AreEqual("4", list.FirstOrDefault(comp5, "00", "11", "22", "33", "4"));
            Assert.AreEqual("4", list.First(comp5, "00", "11", "22", "33", "4"));
            Assert.Null(list.FirstOrDefault(comp5, "00", "11", "22", "33", "44"));
            Assert.Throws<InvalidOperationException>(() => list.First(comp5, "00", "11", "22", "33", "44"));
        }

        [Test]
        public void TestFindIndexExt()
        {
            var list = (IReadOnlyList<string>)_.List("0", "1", "2", "3", "4", "5");

            var index = list.FindIndex("3", (field, col) => field == col);

            Assert.AreEqual(3, index);

            var index1 = list.FindIndex("4443", (field, col) => field == col);

            Assert.AreEqual(-1, index1);
        }

        [Test]
        public void DictExtTest()
        {
            var map = _.Map((1, new List<int>()));

            Assert.False(map.ContainsKey(2));

            var val = map.GetOrAdd(2);

            val.Add(1);

            map.GetOrAdd(2).Add(2);

            map.TryGetValue(2, out var lst);

            Assert.True(ReferenceEquals(lst, val));

            var dict = ((IReadOnlyDictionary<int, List<int>>)map).ToMap();

            Assert.AreEqual(dict, map);

            var dict2 = ((IReadOnlyDictionary<int, List<int>>)map).ToDictionary(d => d.Key, d => d.Value).ToMap();

            Assert.AreEqual(dict2, map);

            var list = _.List(0, 1, 2, 3);

            Func<int, int> keySelector = i => i * 10;
            Func<int, int> elementSelector = i => i;

            var intMap = list.ToMap(keySelector);

            foreach (var i in list)
            {
                var selector = keySelector(i);

                Assert.AreEqual(i, intMap[selector]);
            }

            var intMapE = list.ToMap(keySelector, elementSelector);

            foreach (var i in list)
            {
                var selector = keySelector(i);

                Assert.AreEqual(i, intMapE[selector]);
            }

            var intMap1 = list.ToArray().ToMap(keySelector);

            foreach (var i in list)
            {
                var selector = keySelector(i);

                Assert.AreEqual(i, intMap1[selector]);
            }

            var intMapE1 = list.ToArray().ToMap(keySelector, elementSelector);

            foreach (var i in list)
            {
                var selector = keySelector(i);

                Assert.AreEqual(i, intMapE1[selector]);
            }

            var intMap2 = Enumerable.Range(0, 4).ToMap(keySelector);

            foreach (var i in list)
            {
                var selector = keySelector(i);

                Assert.AreEqual(i, intMap2[selector]);
            }

            var intMapE2 = Enumerable.Range(0, 4).ToMap(keySelector, elementSelector);

            foreach (var i in list)
            {
                var selector = keySelector(i);

                Assert.AreEqual(i, intMapE2[selector]);
            }

            Assert.AreEqual(0, new Data<int>().ToMap(keySelector).Count);
            Assert.AreEqual(0, new Data<int>().ToMap(keySelector, elementSelector).Count);
        }

        [Test]
        public void TestTryCombine()
        {
            var set = new Set<int>();

            set.Add(1);

            var ints = set.TryCombine(null);

            foreach (var i in ints)
            {
                Assert.AreEqual(1, i);
            }

            Assert.AreEqual(1, ints.Count);

            Set<int> nullSet = null;

            ints = nullSet.TryCombine(set);

            Assert.AreEqual(1, ints.Count);

            Assert.AreEqual(1, ints.First());
            
            foreach (var i in ints)
            {
                Assert.AreEqual(1, i);
            }

            foreach (var i in ints)
            {
                Assert.True(set.Contains(i));
            }

            var set2 = new Set<int>();

            set.Add(2);

            var ints2 = set.TryCombine(set2);

            Assert.AreEqual(2, ints2.Count);

            foreach (var i in ints2)
            {
                Assert.True(set.Contains(i) || set2.Contains(i));
            }
            
            var set3 = new Set<int>();

            set3.Add(1);
            
            var set4 = new Set<int>();

            set4.Add(2);

            var set5 = set3.TryCombine(set4);
            
            Assert.True(set5.Contains(1));
            Assert.True(set5.Contains(2));

            foreach (var i in set5)
            {
                Assert.True(set3.Contains(i) || set4.Contains(i));
            }
        }
    }
}