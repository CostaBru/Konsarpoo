using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests
{
    [TestFixture(16)]
    [TestFixture(1024)]
    [TestFixture(null)]
    public class SetTest
    {
        private readonly int m_maxSizeOfArrayBucket;

        public SetTest(int? maxSizeOfArrayBucket)
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
            var hashSet = new Set<int>();

            Assert.NotNull(hashSet.SyncRoot);
            Assert.False(((ICollection<int>)hashSet).IsReadOnly);

            hashSet.Add(0);

            hashSet.Remove(0);

            hashSet.Add(2);
            hashSet.Add(7);

            Assert.False(hashSet.Remove(10));
        }

        [Test]
        public void TestRemoveIfEmpty()
        {
            var set = new Set<int>();

            Assert.False(set.Remove(0));
        }

        [Test]
        public void TestSmallDict()
        {
            var hashSet = new Map<int, int>();

            hashSet[0] = 0;

            hashSet.Remove(0);

            hashSet[2] = 2;
            hashSet[7] = 7;

            Assert.False(hashSet.Remove(10));
        }

        [Test]
        public void TestContains([Values(-1000, 0, 1)] int m, [Values(0, 1, 2, 3, 100, 1000, 500000)] int n,
            [Values(true, false)] bool ctr)
        {
            var enumerable = Enumerable.Range(m, n).ToData();

            Set<int> set;
            HashSet<int> hashSet;

            if (ctr)
            {
                hashSet = new HashSet<int>(enumerable);

                set = new Set<int>(enumerable);
            }
            else
            {
                hashSet = new HashSet<int>();

                hashSet.UnionWith(enumerable);


                set = new Set<int>();

                set.AddRange(enumerable);
            }


            foreach (var val in enumerable)
            {
                Assert.True(set.Contains(val), $"val {val} is missing.");
            }

            var ints = new HashSet<int>(set.ToArray());

            foreach (var val in enumerable)
            {
                Assert.True(ints.Contains(val), $"val {val} is missing after enumeration.");
            }

            Assert.True(set == ints);

            set.Clear();

            Assert.AreEqual(0, set.Count);

            Assert.AreEqual(0, set.ToArray().Length);

            set.AddRange(enumerable);
            set.AddRange(enumerable);
            set.AddRange(enumerable);

            Assert.AreEqual(enumerable.Count, set.Count);

            foreach (var val in enumerable)
            {
                Assert.True(set.Contains(val), $"val {val} is missing after multiple add.");
            }

            foreach (var val in enumerable)
            {
                set.Remove(val);
            }
        }

        [Test]
        public void TestAdd()
        {
            {
                var set1 = new Set<string>()
                {
                    { "test0" },
                    { "test1" },
                    { "test2" },
                    { "test3" },
                    { "test4" },
                    { "test5" },
                    { "test6" }
                };

                var set2 = new Set<string>
                {
                    { "test10" },
                    { "test11" },
                    { "test12" },
                    { "test13" },
                    { "test14" },
                    { "test15" },
                    { "test16" }
                };

                var newSet = set1 + set2;

                foreach (var kv in set1)
                {
                    Assert.True(newSet.Contains(kv));
                    Assert.True(newSet.ContainsKey(kv));
                    Assert.False(newSet.TryAdd(kv, kv));

                    Assert.True(newSet.TryGetValue(kv, out var kv1));
                    Assert.False(newSet.TryGetValue(kv + Guid.NewGuid(), out var kv2));
                }

                var s3 = new Set<string>();
                var s4 = new Set<string>();
                
                foreach (var kv in set2)
                {
                    s3[kv] = kv;
                    
                    Assert.True(s4.TryAdd(kv, kv));
                    
                    Assert.True(newSet.Contains(kv));
                    Assert.True(newSet.ContainsKey(kv));
                    Assert.False(newSet.TryAdd(kv, kv));

                    Assert.True(newSet.TryGetValue(kv, out var kv1));
                }

                newSet.Dispose();
                Assert.AreEqual(0, newSet.Count);
            }
            GC.Collect();
        }

        [Test]
        public void TestSubstract()
        {
            var set1 = new Set<string>
            {
                { "test0" },
                { "test1" },
                { "test2" },
            };

            var set2 = new Set<string>
            {
                { "test10" },
                { "test11" },
                { "test12" },
            };

            var set3 = new Set<string>
            {
                { "test3" },
                { "test4" },
                { "test5" },
                { "test6" }
            };

            var newSet1 = set1 + set3;
            var newSet2 = set2 + set3;

            var rez1 = newSet1 - newSet2;

            foreach (var kv in set1)
            {
                Assert.True(rez1.Contains(kv));
            }

            foreach (var kv in set3)
            {
                Assert.True(rez1.IsMissing(kv));
            }

            var rez2 = newSet2 - newSet1;

            foreach (var kv in set2)
            {
                Assert.True(rez2.Contains(kv));
            }

            foreach (var kv in set3)
            {
                Assert.True(rez2.IsMissing(kv));
            }
        }

        [Test]
        public void TestRemoveWhere()
        {
            var hashSet = new Set<int>();

            foreach (var v in Enumerable.Range(0, 10))
            {
                hashSet.Append(v);
            }

            hashSet.RemoveWhere(i => i >= 5);

            var set = new Set<int>(Enumerable.Range(0, 5));

            Assert.AreEqual(set, hashSet);
        }

        [Test]
        public void TestAppendCopyTo()
        {
            var hashSet = new Set<int>();

            foreach (var v in Enumerable.Range(0, 10))
            {
                hashSet.Append(v);

                Assert.True(hashSet.Contains(v));
            }

            var ints = new int[hashSet.Count];
            var ints1 = new int[hashSet.Count];

            hashSet.CopyTo(ints, 3, 2);
            hashSet.CopyTo(ints1);

            for (var index = 3; index < 5; index++)
            {
                var i = ints[index];
                Assert.True(hashSet.Contains(i));
            }

            foreach (var i in ints1)
            {
                Assert.True(hashSet.Contains(i));
            }
        }

        [Test]
        public void TestCtr()
        {
            var hugeSet = Enumerable.Range(0, 1000000).ToHashSet();
            var s = new Set<int>(hugeSet);
            var s1 = new Set<int>(s);

            Assert.Throws<ArgumentNullException>(() => new Set<int>((IReadOnlyCollection<int>)null));

            Assert.AreEqual(s, s1);
        }

        [Test]
        public void TestICollection()
        {
            var set = (ICollection<int>)new Set<int>();

            for (int i = 0; i < 10; i++)
            {
                set.Add(i);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.True(set.Contains(i));
            }
        }

        [Test]
        public void TestOp()
        {
            var l1 = new Data<int>(Enumerable.Range(0, 5)).ToSet();
            var l2 = new Data<int>(Enumerable.Range(5, 5)).ToSet();

            var lk1 = (IReadOnlyCollection<int>)new Data<int>(l1);
            var lk2 = (IReadOnlyCollection<int>)new Data<int>(l2);

            var l3 = l1 + l2;

            var set3 = new Set<int>();

            set3.UnionWith(l1);
            set3.UnionWith(l2);

            Assert.AreEqual(set3, l3);
            
            Assert.True(set3 == l3);
            
            set3.ExceptWith(l1);

            Assert.AreEqual(set3, l2);

            Assert.True(set3.Overlaps(l2 - _.List(5)));

            Assert.AreEqual(l3.Count, l3.Length);

            Assert.AreEqual(l1.GetHashCode(), l1.ToSet().GetHashCode());
            Assert.AreNotEqual(l1.GetHashCode(), l3.GetHashCode());

            Assert.AreEqual(new Data<int>(Enumerable.Range(0, 10)).ToSet(), l3);

            Assert.AreEqual(l2, l3 - l1);
            Assert.AreEqual(l1, l3 - l2);

            Assert.True(l2.Equals(l3 - l1));
            Assert.True(l1.Equals(l3 - l2));

            Assert.AreEqual(l2, l3 - lk1);
            Assert.AreEqual(l1, l3 - lk2);

            Assert.True(l3.IsSupersetOf(l1));
            Assert.True(l3.IsSupersetOf(l2));

            Assert.False(l1.IsSupersetOf(l3));
            Assert.False(l2.IsSupersetOf(l3));

            foreach (var i in l1)
            {
                Assert.True(l3[i] == i);
            }
        }

        [Test]
        public void TestSetSerialization()
        {
            var set = new Set<string>(StringComparer.OrdinalIgnoreCase);

            set.Add("qwerty");
            set.Add("test");

            var serializeWithDcs = SerializeHelper.SerializeWithDcs(set);

            var deserializeWithDcs = SerializeHelper.DeserializeWithDcs<Set<string>>(serializeWithDcs);

            Assert.AreEqual(deserializeWithDcs, set);
        }

        [Test]
        public void TestSetSerialization2()
        {
            var set = new Set<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var i in Enumerable.Range(0, 1024))
            {
                set.Add(i.ToString());
            }

            var clone = SerializeHelper.Clone<Set<string>>(set);
            
            var b = clone == set;

            Assert.AreEqual(clone, set);
        }

        [Test]
        public void TestSetExt()
        {
            var set = new Set<string>(StringComparer.OrdinalIgnoreCase);

            set.Add("qwerty");
            set.Add("test");
            
            Assert.False(set.ListNullOrItemAbsent("qwerty"));
            Assert.True(set.ListNullOrItemAbsent("121232"));

            IReadOnlyCollection<string> nullSet = null;
            
            Assert.True(nullSet.ListNullOrItemAbsent("121232"));

            var set1 = set.ToSet(set.Comparer);
            
            Assert.True(set == set1);
        }
        
        [Test]
        public void TestCapacityCtr()
        {
            var set = new Set<int>(100);

            for (int i = 0; i < 100; i++)
            {
                set[i] = i;
            }

            var set1 = set.ToSet();

            Assert.False(set1 != set);

            set1.Remove(0);
            
            Assert.True(set1 != set);
        }
        
        [Test]
        public void TestAdd2([Values(0,1,2,1000, 1_0000)] int count)
        {
            var map = new Set<int>();
            var dict = new HashSet<int>();

            for (int i = 0; i < count; i++)
            {
                map.Add(i);
                dict.Add(i);
            }
        }
        
        [Test]
        public void TestOp1()
        {
            var m1 = (Set<int>)null;
            var m2 = (Set<int>)null;
            var m3 = new Set<int>() { 1 };
            var m4 = new Set<int>() { 2 };
            var m5 = new Set<int>() { 2, 3 };
            var m6 = new Set<int>() { 1, 2, 3 };
            
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

            var set1 = new Set<string>(StringComparer.Ordinal) {"1", "2"};
            var set2 = new Set<string>(StringComparer.OrdinalIgnoreCase) {"1", "2"};
            
            Assert.True(set1 == set2);

            var set = new Set<string>();

            set.Add(null);
            
            Assert.True(set.Contains(null));
        }
    }
}