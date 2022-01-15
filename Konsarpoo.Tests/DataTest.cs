﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

// ReSharper disable HeapView.BoxingAllocation


namespace Konsarpoo.Collections.Tests
{
    [TestFixture(16)]
    [TestFixture(1024)]
    [TestFixture(null)]
    public class DataTest
    {
        private readonly int m_maxSizeOfArrayBucket;

        public DataTest(int? maxSizeOfArrayBucket)
        {
            m_maxSizeOfArrayBucket = maxSizeOfArrayBucket ?? 1024 * 1024;
        }

        [SetUp]
        public void SetUp()
        {
            ArrayPoolGlobalSetup.SetMaxSizeOfArrayBucket(m_maxSizeOfArrayBucket);
        }

        private class GcArrayAllocator<T> : IArrayPool<T>
        {
            public T[] Rent(int count)
            {
                return new T[count];
            }

            public void Return(T[] array, bool clearArray = false)
            {
                if (clearArray)
                {
                    Array.Clear(array, 0, array.Length);
                }
            }
        }

        [Test]
        public void TestCustomAllocator()
        {
            var poolSetup = (new GcArrayAllocator<int>(), new GcArrayAllocator<Data<int>.INode>());

            var l1 = new Data<int>(0, 16, poolSetup);
            l1.AddRange(Enumerable.Range(0, 50));

            var l2 = new Data<int>(0, 16, poolSetup);
            l2.AddRange(Enumerable.Range(50, 50));

            var l3 = l1 + l2;

            var expected = new Data<int>(Enumerable.Range(0, 100));

            var enumerator = expected.GetEnumerator();
            var enumerator1 = l3.GetEnumerator();

            while (enumerator.MoveNext())
            {
                enumerator1.MoveNext();

                Assert.AreEqual(enumerator.Current, enumerator1.Current);
            }

            enumerator = expected.GetEnumerator();
            enumerator1 = l3.GetEnumerator();

            while (enumerator1.MoveNext())
            {
                Assert.True(enumerator.MoveNext());

                Assert.AreEqual(enumerator.Current, enumerator1.Current);
            }

            Assert.AreEqual(expected, l3);

            Assert.AreEqual(l2, l3 - l1);
            Assert.AreEqual(l1, l3 - l2);
        }

        [Test]
        public void TestStdVect([Values(100, 3, 2, 1)] int count)
        {
            {
                var vector = new std.vector<int>();

                Assert.True(vector.empty());

                for (int i = 0; i < count; i++)
                {
                    vector.emplace_back(ref i);
                }

                Assert.AreEqual(0, vector.back());
                Assert.AreEqual(count - 1, vector.front());

                Assert.False(vector.empty());

                Assert.AreEqual(count, vector.size());

                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(i, vector[i]);
                }

                var copy = new std.vector<int>(vector);

                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(i, copy[i]);
                }
                
                vector.reverse();
                
                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(i, vector[vector.size() - i - 1]);
                }
                
                vector.sort();
                
                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(i, vector[i]);
                }

                vector.Dispose();

                Assert.AreEqual(0, vector.Count);
            }

            var makeVector = std.make_vector(1, 2);
            
            Assert.AreEqual(2, makeVector.size());
            Assert.AreEqual(1, makeVector.at(0));
            Assert.AreEqual(2, makeVector.at(1));

            GC.Collect();
        }

        [Test]
        public void TestStdVect2()
        {
            var vector = new std.vector<int>(Enumerable.Range(0, 10));

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, vector.at(i));
            }

            vector.clear();

            Assert.True(vector.empty());
        }

        [Test]
        public void TestStdVector3()
        {
            var ints = new std.vector<int>();
            
            ints.resize(100, 1);

            Assert.AreEqual(100, ints.Count);
            Assert.True(ints.All(l => l == 1));
            
            ints.resize(50);
            Assert.AreEqual(50, ints.Count);
            Assert.True(ints.All(l => l == 1));
            
            ints.resize(0);
            Assert.AreEqual(0, ints.Count);
        }

        [Test]
        public void TestStdVect3()
        {
            var vector = new std.vector<int>();

            for (int i = 0; i < 10; i++)
            {
                vector.push_back(i);

                Assert.AreEqual(i, vector.at(0));

                vector.pop_back();
            }

            Assert.True(vector.empty());
        }
        
        [Test]
        public void TestStdVectErase()
        {
            var vector = new std.vector<int>();

            for (int i = 0; i < 10; i++)
            {
                vector.push_back(i);
            }

            for (int i = (int)vector.size() - 1; i >= 0; i--)
            {
                vector.erase(i);
                
                Assert.False(vector.Any(v => v == i));
            }
        }
        
        [Test]
        public void TestStdVectErase2()
        {
            var vector = new std.vector<int>();

            for (int i = 0; i < 10; i++)
            {
                vector.push_back(i);
            }

            vector.erase(2, 7);
            
            Assert.True(vector.Any(v => v == 0));
            Assert.True(vector.Any(v => v == 1));
            Assert.True(vector.Any(v => v == 8));
            Assert.True(vector.Any(v => v == 9));

            for (int i = 2; i <= 7; i++)
            {
                Assert.False(vector.Any(v => v == i));
            }
        }

        [Test]
        public void TestPoolList([Values(25000, 1000, 6, 5, 4, 3, 2, 1, 0)] int count)
        {
            {
                var poolList = new PoolList<int>(count, count);

                for (int i = 0; i < count; i++)
                {
                    poolList.Add(i);
                }

                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(i, poolList[i]);
                }

                var poolListCopy = new PoolList<int>(poolList);

                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(i, poolListCopy[i]);
                }

                poolList.Dispose();

                Assert.AreEqual(0, poolList.Count);
            }

            GC.Collect();
        }

        [Test]
        public void TestPoolList2([Values(25000, 1000, 6, 5, 4, 3, 2, 1, 0)] int count)
        {
            {
                var poolList = new PoolList<int>(count, count);

                for (int i = 0; i < count; i++)
                {
                    poolList.Add(i);
                }

                poolList.Clear();

                Assert.Null(poolList.m_items);
                Assert.AreEqual(0, poolList.m_size);
            }

            GC.Collect();
        }

        [Test]
        public void TestIList([Values(123, 6, 5, 4, 3, 2, 1, 0)] int count)
        {
            var data = (IList)new Data<int>();

            for (int i = 0; i < count; i++)
            {
                data.Add(i);

                Assert.True(data.Contains(i));
                Assert.AreEqual(i, data.IndexOf(i));
            }

            while (data.Count > 0)
            {
                data.RemoveAt(data.Count - 1);
            }

            for (int i = 0; i < count; i++)
            {
                data.Insert(0, i);

                data.Remove(i);
            }

            Assert.AreEqual(0, data.Count);
        }


        [Test]
        public void TestReverse([Values(25000, 1000, 6, 5, 4, 3, 2, 1, 0)] int count)
        {
            var list = Enumerable.Range(0, count).ToList();

            var dataList = list.ToData();

            list.Reverse();
            dataList.Reverse();

            for (int i = 0; i < list.Count; i++)
            {
                var val = list[i];
                var arrVal = dataList[i];

                Assert.AreEqual(val, arrVal);
            }

            dataList.Dispose();

            GC.Collect();
        }

        [Test]
        public void TestArgs([Values(25000, 1000, 6, 5, 4, 3, 2, 1, 0)] int count)
        {
            var array = Enumerable.Range(0, count).ToArray();

            var dataList = array.ToData();

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var i = dataList[-1];
            });
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var i = dataList[count + 1];
            });
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var i = dataList[count * count];
            });
        }

        [Test]
        public void TestInsert([Values(0, 1, 2, 3, 4, 5, 6, 7, 50, 25000)] int count,
            [Values(0, 1, 2, 3, 4, 5, 6, 7, 50, 25000)] int insertPosition)
        {
            var dataList = Enumerable.Range(0, count).Reverse().ToData();

            if (insertPosition <= dataList.Count)
            {
                var copy = dataList.ToList();
                var vector = new std.vector<int>(dataList);

                dataList.Insert(insertPosition, -999);
                copy.Insert(insertPosition, -999);
                vector.Insert(insertPosition, -999);

                Assert.GreaterOrEqual(dataList.IndexOf(-999), 0);

                for (int i = 0; i < copy.Count; i++)
                {
                    Assert.AreEqual(copy[i], dataList[i]);
                    Assert.AreEqual(copy[i], vector.at(i));
                }
            }
        }

        [Test]
        public void TestCopySmall([Values(2, 1)] int count)
        {
            var array = Enumerable.Range(0, count).ToArray();

            var dataList = array.ToData();

            Assert.False(dataList.HasList);
            Assert.True(dataList.Any);

            var list = dataList.ToData();

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = 0; i < array.Length; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];
                var copiedVal = list[i];

                Assert.AreEqual(val, arrVal);
                Assert.AreEqual(arrVal, copiedVal);

                dataList[i] = ~arrVal;
                Assert.AreEqual(~arrVal, dataList[i]);
            }
        }

        [Test]
        public void TestCopyCommon()
        {
            var array = Enumerable.Range(0, 4000).ToArray();

            var dataList = new Data<int>();

            dataList.AddRange(array);

            Assert.True(dataList.HasList);

            var list = dataList.ToData();

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = 0; i < array.Length; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];
                var copiedVal = list[i];

                Assert.AreEqual(val, arrVal);
                Assert.AreEqual(arrVal, copiedVal);

                dataList[i] = ~arrVal;
                Assert.AreEqual(~arrVal, dataList[i]);
            }
        }

        [Test]
        public void TestCopyHuge()
        {
            var array = Enumerable.Range(0, 25000).ToArray();

            var dataList = new Data<int>();

            dataList.AddRange(array);

            Assert.True(dataList.HasList);
            Assert.True(dataList.Any);
            Assert.NotNull(dataList.GetRoot());
            Assert.AreEqual(dataList.Count, _.len(dataList));

            var list = dataList.ToData();

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = 0; i < array.Length; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];
                var copiedVal = list[i];

                Assert.AreEqual(arrVal, val);
                Assert.AreEqual(arrVal, copiedVal);

                dataList[i] = ~arrVal;
                Assert.AreEqual(~arrVal, dataList[i]);
            }
        }

        [Test]
        public void TestCopyVeryHuge()
        {
            var array = Enumerable.Range(0, 80000).ToArray();

            var dataList = array.ToData();

            Assert.True(dataList.HasList);

            var list = dataList.ToData();
            var listO = dataList.ToData() as IList;

            Assert.AreEqual(list, list);
            Assert.True(list.Equals((object)listO));

            Assert.False(listO.IsFixedSize);
            Assert.False(listO.IsReadOnly);
            Assert.False(listO.IsSynchronized);
            Assert.NotNull(listO.SyncRoot);
            Assert.AreEqual(listO.GetHashCode(), list.GetHashCode());
            Assert.AreNotEqual(listO.GetHashCode(), (list + _.List(1)).GetHashCode());

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = 0; i < array.Length; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];
                var copiedVal = list[i];
                var val1 = listO[i];

                Assert.AreEqual(val, arrVal);
                Assert.AreEqual(arrVal, copiedVal);
                Assert.AreEqual(val, val1);

                listO[i] = ~arrVal;

                dataList[i] = ~arrVal;
                Assert.AreEqual(~arrVal, dataList[i]);
                Assert.AreEqual(~arrVal, listO[i]);
            }
        }

        [Test]
        public void TestStack()
        {
            var st = _.List(0).AsStack();
            var stack = new Stack<int>();
            stack.Push(0);

            int i = 0;
            foreach (var val in Enumerable.Range(0, 12))
            {
                st.Push(val);
                stack.Push(val);

                if (i % 3 == 0)
                {
                    st.Pop();
                    stack.Pop();
                }

                Assert.AreEqual(stack.Peek(), st.Peek());

                i++;
            }

            stack.Clear();
            st.Clear();

            foreach (var val in Enumerable.Range(0, 12))
            {
                stack.Push(val);
            }

            st.PushRange(Enumerable.Range(0, 12));

            foreach (var val in Enumerable.Range(0, 12))
            {
                Assert.AreEqual(stack.Pop(), st.Pop());
            }
        }

        [Test]
        public void TestQueue()
        {
            var qu = new Data<int>().AsQueue();
            var queue = new Queue<int>();

            for (int i = 0; i < 10; i++)
            {
                qu.Enqueue(i);
                queue.Enqueue(i);

                if (i + 1 % 3 == 0)
                {
                    qu.Dequeue();
                    queue.Dequeue();
                }

                Assert.AreEqual(qu.Peek(), queue.Peek());
            }

            while (qu.Any)
            {
                Assert.AreEqual(qu.Peek(), queue.Peek());

                qu.Dequeue();
                queue.Dequeue();
            }
            
            var qu1 = new Data<int>().AsQueue();

            foreach (var i in Enumerable.Range(0, 10))
            {
                qu1.Enqueue(i);
            }
            
            var qu12 = new Data<int>().AsQueue();
            
            qu12.EnqueueRange(Enumerable.Range(0, 10));
            
            while (qu.Any)
            {
                Assert.AreEqual(qu1.Peek(), qu12.Peek());

                qu1.Dequeue();
                qu12.Dequeue();
            }
        }

        private class IntComp : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return x.CompareTo(y);
            }
        }

        [Test]
        public void TestSort()
        {
            var list1 = Enumerable.Range(0, 100).Reverse().ToList();
            var list2 = Enumerable.Range(0, 100).Reverse().ToData();
            var list3 = Enumerable.Range(0, 100).Reverse().ToData();
            var list4 = Enumerable.Range(0, 100).Reverse().ToData();
            var list5 = Enumerable.Range(0, 100).Reverse().ToData();

            list1.Sort();

            list2.Sort((x, y) => x.CompareTo(y));
            var comparison = new Comparison<int>((x, y) => x.CompareTo(y));
            list3.Sort(comparison);

            list4.Sort();
            list5.Sort(new IntComp());

            for (var index = 0; index < list1.Count; index++)
            {
                Assert.AreEqual(list1[index], list2[index]);
                Assert.AreEqual(list2[index], list3[index]);
                Assert.AreEqual(list4[index], list4[index]);
            }

            for (var index = 0; index < list1.Count; index++)
            {
                var i = list1[index];

                var binarySearch = list5.BinarySearch(i, i);

                Assert.AreEqual(i, list5[binarySearch]);
            }

            for (var index = 0; index < list1.Count; index++)
            {
                var i = list1[index];

                var binarySearch = list5.BinarySearch(i);

                Assert.AreEqual(i, list5[binarySearch]);
            }

            list1.AddRange(Enumerable.Range(100, 150));

            list1.Sort();

            foreach (var val in Enumerable.Range(100, 150))
            {
                var binarySearch = list5.BinarySearch(val);

                list5.Insert(~binarySearch, val);
            }

            for (var index = 0; index < list1.Count; index++)
            {
                Assert.AreEqual(list1[index], list5[index]);
            }
        }

        [Test]
        public void TestOp()
        {
            var l1 = new Data<int>(Enumerable.Range(0, 5));
            var l2 = new Data<int>(Enumerable.Range(5, 5));

            var l3 = l1 + l2;

            Assert.AreEqual(new Data<int>(Enumerable.Range(0, 10)), l3);

            Assert.AreEqual(l2, l3 - l1);
            Assert.AreEqual(l1, l3 - l2);
        }

        [Test]
        public void TestRemoveAll1()
        {
            var l1 = new Data<int>(Enumerable.Range(0, 5));
            var l2 = new Data<int>(Enumerable.Range(5, 5));

            var l3 = l1 + l2;

            var cnt = l3.RemoveAll(i => i >= 5);

            Assert.AreEqual(5, cnt);

            Assert.AreEqual(l1, l3);

            var list = new Data<int>(Enumerable.Range(0, 100));

            list.RemoveAll(i => i + 1 % 2 != 0);

            var filtered = list.Where(i => i + 1 % 2 == 0).ToData();

            Assert.AreEqual(filtered, list);
        }

        [Test]
        public void TestRemoveSmallFirst()
        {
            var list = Enumerable.Range(0, Data<int>.SmallListCount).ToList();

            var dataList = list.ToData();

            Assert.False(dataList.HasList);

            list.Remove(0);
            dataList.Remove(0);

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var val = dataList[i];
                var arrVal = list[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestRemoveSmallSome()
        {
            var list = Enumerable.Range(0, Data<int>.SmallListCount).ToList();

            var dataList = list.ToData();

            Assert.False(dataList.HasList);

            list.Remove(5);
            dataList.Remove(5);

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var val = dataList[i];
                var arrVal = list[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestRemoveCommonSome()
        {
            var list = Enumerable.Range(0, 10000).ToList();

            var dataList = list.ToData();

            list.Remove(555);
            dataList.Remove(555);

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var val = dataList[i];
                var arrVal = list[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestRemoveHugeSome()
        {
            var list = Enumerable.Range(0, 25000).ToList();

            var dataList = list.ToData();

            list.Remove(0);
            dataList.Remove(0);

            list.Remove(555);
            dataList.Remove(555);

            list.Remove(20000);
            dataList.Remove(20000);

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var val = dataList[i];
                var arrVal = list[i];

                Assert.AreEqual(val, arrVal);
            }
        }


        [Test]
        public void TestRemoveCommonAll()
        {
            var list = Enumerable.Range(0, 1000).ToList();

            var dataList = list.ToData();

            Assert.AreEqual(list.Count, dataList.Count);

            var array = list.ToArray();

            foreach (var i in array)
            {
                dataList.Remove(i);
            }

            foreach (var i in array)
            {
                Assert.AreEqual(-1, dataList.IndexOf(i));
            }
        }

        [Test]
        public void TestRemoveHugeAll()
        {
            var list = Enumerable.Range(0, 2500).ToList();

            var dataList = list.ToData();

            Assert.AreEqual(list.Count, dataList.Count);

            var array = list.ToArray();

            foreach (var i in array)
            {
                dataList.Remove(i);
            }

            foreach (var i in array)
            {
                Assert.AreEqual(-1, dataList.IndexOf(i));
            }
        }

        [Test]
        public void TestSmallEnumeration([Values(1, 2)] int count)
        {
            var array = Enumerable.Range(0, count).ToArray();

            var dataList = array.ToData();

            Assert.AreEqual(array.Length, dataList.Count);

            Assert.False(dataList.HasList);

            int i = 0;
            foreach (var val in dataList)
            {
                Assert.AreEqual(val, array[i]);

                i++;
            }
        }

        [Test]
        public void TestHugeEnumaration()
        {
            var array = Enumerable.Range(0, 50000).ToArray();

            var dataList = array.ToData();

            Assert.AreEqual(array.Length, dataList.Count);

            Assert.True(dataList.HasList);

            int i = 0;
            foreach (var val in dataList)
            {
                Assert.AreEqual(array[i], val, i.ToString());

                i++;
            }
        }

        [Test]
        public void TestHugeIndexing()
        {
            var array = Enumerable.Range(0, 50000).ToArray();

            var dataList = array.ToData();

            Assert.AreEqual(array.Length, dataList.Count);

            Assert.True(dataList.HasList);

            for (int i = 0; i < array.Length; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];


                Assert.AreEqual(val, arrVal, "i = " + i);
            }
        }

        [Test]
        public void TestSortSmall6()
        {
            var ints = new int[Data<int>.SmallListCount] { -1, 5 };

            var dataList = ints.ToData();

            Assert.AreEqual(ints.Length, dataList.Count);

            Assert.False(dataList.HasList);

            Array.Sort(ints);

            dataList.Sort();

            for (int i = 0; i < ints.Length; i++)
            {
                var val = dataList[i];
                var arrVal = ints[i];

                Assert.AreEqual(val, arrVal);
            }
        }


        [Test]
        public void TestSortSmall5()
        {
            var ints = new int[Data<int>.SmallListCount] { -1, 1000 };

            var dataList = ints.ToData();

            Assert.AreEqual(ints.Length, dataList.Count);

            Assert.False(dataList.HasList);

            Array.Sort(ints);

            dataList.Sort();

            for (int i = 0; i < ints.Length; i++)
            {
                var val = dataList[i];
                var arrVal = ints[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestSortSmall4()
        {
            var ints = new int[Data<int>.SmallListCount] { 8, 1000 };

            var dataList = ints.ToData();

            Assert.AreEqual(ints.Length, dataList.Count);

            Assert.False(dataList.HasList);

            Array.Sort(ints);

            dataList.Sort();

            for (int i = 0; i < ints.Length; i++)
            {
                var val = dataList[i];
                var arrVal = ints[i];

                Assert.AreEqual(val, arrVal);
            }
        }


        [Test]
        public void TestSortSmall3()
        {
            var ints = new int[Data<int>.SmallListCount] { -1, 8 };

            var dataList = ints.ToData();

            Assert.AreEqual(ints.Length, dataList.Count);

            Assert.False(dataList.HasList);

            Array.Sort(ints);

            dataList.Sort();

            for (int i = 0; i < ints.Length; i++)
            {
                var val = dataList[i];
                var arrVal = ints[i];

                Assert.AreEqual(val, arrVal);
            }
        }


        [Test]
        public void TestSortSmall2([Values(1000, -1000)] int value)
        {
            var ints = new int[Data<int>.SmallListCount] { 10, value };

            var dataList = ints.ToData();

            Assert.AreEqual(ints.Length, dataList.Count);

            Assert.False(dataList.HasList);

            Array.Sort(ints);

            dataList.Sort();

            for (int i = 0; i < ints.Length; i++)
            {
                var val = dataList[i];
                var arrVal = ints[i];

                Assert.AreEqual(val, arrVal);
            }
        }


        [Test]
        public void TestSortSmall1()
        {
            var ints = new int[1] { 10 };

            var dataList = ints.ToData();

            Assert.False(dataList.HasList);

            Array.Sort(ints);

            dataList.Sort();

            Assert.AreEqual(ints.Length, dataList.Count);

            for (int i = 0; i < ints.Length; i++)
            {
                var val = dataList[i];
                var arrVal = ints[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestRandomIndexing()
        {
            var random = new Random();

            var array = Enumerable.Range(0, 50000).Select(i => random.Next()).ToArray();

            var dataList = array.ToData();

            Assert.AreEqual(array.Length, dataList.Count);

            for (int i = 0; i < array.Length; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];

                Assert.AreEqual(val, arrVal);
            }
        }


        [Test]
        public void TestRandomCommonSort()
        {
            var random = new Random();

            var array = Enumerable.Range(0, 10000).Select(i => random.Next()).ToArray();

            var dataList = array.ToData();

            Assert.AreEqual(array.Length, dataList.Count);

            Array.Sort(array);

            dataList.Sort();

            Assert.AreEqual(array.Length, dataList.Count);

            for (int i = 0; i < array.Length; i++)
            {
                var val = dataList[0];
                var arrVal = array[0];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestRandomHugeSort()
        {
            var random = new Random();

            var array = Enumerable.Range(0, 50000).Select(i => random.Next()).ToArray();

            var dataList = array.ToData();

            Array.Sort(array);

            dataList.Sort();

            for (int i = 0; i < array.Length; i++)
            {
                var val = dataList[0];
                var arrVal = array[0];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestSmallCopyToArray([Values(2, 1)] int count, [Values(0, 10)] int index)
        {
            var array = Enumerable.Range(0, count).ToArray();

            var dataList = new Data<int>();

            dataList.AddRange(array);

            Assert.False(dataList.HasList);

            var copyTo1 = new int[100];
            var copyTo2 = new int[100];

            dataList.CopyTo(copyTo1, index);

            array.CopyTo(copyTo2, index);

            for (int i = index; i < copyTo1.Length; i++)
            {
                var val = copyTo1[i];
                var arrVal = copyTo2[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestCommonCopyToArray([Values(0, 10)] int index)
        {
            var array = Enumerable.Range(0, 5000).ToArray();

            var dataList = new Data<int>();

            dataList.AddRange(array);

            Assert.True(dataList.HasList);

            var copyTo1 = new int[5500];
            var copyTo2 = new int[5500];

            dataList.CopyTo(copyTo1, index);

            array.CopyTo(copyTo2, index);

            for (int i = index; i < copyTo1.Length; i++)
            {
                var val = copyTo1[i];
                var arrVal = copyTo2[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestHugeCopyToArray([Values(0, 10)] int index)
        {
            var array = Enumerable.Range(0, 25000).ToArray();

            var dataList = new Data<int>();

            dataList.AddRange(array);

            Assert.True(dataList.HasList);

            var copyTo1 = new int[25500];
            var copyTo2 = new int[25500];

            dataList.CopyTo(copyTo1, index);

            array.CopyTo(copyTo2, index);

            for (int i = index; i < copyTo1.Length; i++)
            {
                var val = copyTo1[i];
                var arrVal = copyTo2[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestSmallInsert([Values(2, 1, 0)] int count, [Values(1, 0)] int index)
        {
            if (index < count || (count == 0 && index == 0))
            {
                var list = Enumerable.Range(0, count).ToList();

                var dataList = new Data<int>();

                dataList.AddRange(list);

                Assert.False(dataList.HasList);

                list.Insert(index, -500);
                dataList.Insert(index, -500);

                Assert.AreEqual(list.Count, dataList.Count);

                for (int i = index; i < list.Count; i++)
                {
                    var val = list[i];
                    var arrVal = dataList[i];

                    Assert.AreEqual(val, arrVal);
                }
            }
        }


        [Test]
        public void TestCommonInsert([Values(5000, 4, 3, 2, 1, 0)] int index)
        {
            var list = Enumerable.Range(0, 5000).ToList();

            var dataList = new Data<int>();

            dataList.AddRange(list);

            Assert.True(dataList.HasList);

            list.Insert(index, -500);
            dataList.Insert(index, -500);

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = index; i < list.Count; i++)
            {
                var val = list[i];
                var arrVal = dataList[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TesHugeInsert([Values(10000, 5, 20000)] int index)
        {
            var list = Enumerable.Range(0, 25000).ToList();

            var dataList = new Data<int>();

            dataList.AddRange(list);

            Assert.True(dataList.HasList);

            list.Insert(index, -500);
            dataList.Insert(index, -500);

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = index; i < list.Count; i++)
            {
                var val = list[i];
                var arrVal = dataList[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestSmallEnsure([Values(2, 1, 0)] int count, [Values(1, 0)] int size)
        {
            if (count <= size)
            {
                var list = Enumerable.Range(0, count).ToList();

                var dataList = new Data<int?>();

                dataList.AddRange(list.Select(i => new int?(i)));

                Assert.False(dataList.HasList);

                dataList.Ensure(size);

                Assert.AreEqual(dataList.Count, size);

                for (int i = 0; i < size; i++)
                {
                    if (i < count)
                    {
                        var arrVal = dataList[i];
                        var val = list[i];

                        Assert.AreEqual(val, arrVal);
                    }
                    else
                    {
                        var arrVal = dataList[i];

                        Assert.Null(arrVal);
                    }
                }
            }
        }

        [Test]
        public void TestCommonEnsure([Values(100000, 25000, 10000, 1000, 6, 0)] int size)
        {
            for (int e = 0; e < 5; e++)
            {
                var list = new Data<int?>();

                var newSize = size + size * e;

                list.Ensure(newSize);

                Assert.AreEqual(newSize, list.Count);

                for (int i = 0; i < newSize; i++)
                {
                    var arrVal = list[i];

                    Assert.Null(arrVal);
                }
            }
        }

        [Test]
        public void TestCommonEnsure1()
        {
            var list = new Data<int?>();

            for (int size = 0; size < 1000; size++)
            {
                if (size == 18)
                {
                }

                list.Ensure(size);

                Assert.AreEqual(size, list.Count);

                for (int i = 0; i < size; i++)
                {
                    var arrVal = list[i];

                    Assert.Null(arrVal);
                }
            }
        }

        [Test]
        public void TestNotEmptyCommonEnsure([Values(25000, 4000)] int count,
            [Values(30000, 10000, 5000, 4001, 4000, 1000, 0)] int size)
        {
            if (count <= size)
            {
                var list = Enumerable.Range(0, count).ToList();

                var dataList = new Data<int?>();

                dataList.AddRange(list.Select(i => new int?(i)));

                dataList.Ensure(size);

                Assert.AreEqual(dataList.Count, size);

                for (int i = 0; i < size; i++)
                {
                    if (i < count)
                    {
                        var arrVal = dataList[i];
                        var val = list[i];

                        Assert.AreEqual(val, arrVal);
                    }
                    else
                    {
                        var arrVal = dataList[i];

                        Assert.Null(arrVal);
                    }
                }
            }
        }

        [Test]
        public void TestRemoveAllSmall([Values(2, 1)] int count, [Values(999999, -1)] int item)
        {
            var array = Enumerable.Range(0, count).ToList();

            var dataList = array.ToData();

            Assert.False(dataList.HasList);

            dataList.RemoveAll(r => r == item);
            array.RemoveAll(r => r == item);

            Assert.AreEqual(dataList.Count, dataList.Count);

            for (int i = 0; i < array.Count; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestRemoveAllSmall2([Values(2, 1)] int count, [Values(3, 2)] int item)
        {
            var ints = new List<int>();

            for (int i = 0; i < count / 2; i++)
            {
                ints.Add(i);
                ints.Add(i);
            }

            var dataList = ints.ToData();

            Assert.False(dataList.HasList);

            dataList.RemoveAll(r => r == item);
            ints.RemoveAll(r => r == item);

            Assert.AreEqual(dataList.Count, dataList.Count);

            for (int i = 0; i < ints.Count; i++)
            {
                var val = dataList[i];
                var arrVal = ints[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestRemoveAllItemSmall([Values(2, 1)] int count, [Values(1, 0)] int item)
        {
            var ints = new List<int>();

            for (int i = 0; i < count / 2; i++)
            {
                ints.Add(i);
                ints.Add(i);
            }

            var dataList = ints.ToData();

            Assert.False(dataList.HasList);

            dataList.RemoveAll(item);
            ints.RemoveAll(r => r == item);

            Assert.AreEqual(dataList.Count, dataList.Count);

            for (int i = 0; i < ints.Count; i++)
            {
                var val = dataList[i];
                var arrVal = ints[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestRemoveAll([Values(1000, 6, 5, 4, 3, 2, 1)] int count, [Values(999, 5, 4, 3, 2, 1, 0)] int item,
            [Values(3, 1, 0)] int duplicates)
        {
            var array = Enumerable.Range(0, count).ToList();

            for (int i = 0; i < duplicates; i++)
            {
                array.AddRange(Enumerable.Range(0, count));
            }

            var dataList = array.ToData();

            dataList.RemoveAll(r => r == item);
            array.RemoveAll(r => r == item);

            Assert.AreEqual(dataList.Count, dataList.Count);

            for (int i = 0; i < array.Count; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void RemoveAll2()
        {
            var data = new Data<KeyValuePair<int, string>>();

            data.AddRange(Enumerable.Range(1, 100).Select(i => new KeyValuePair<int, string>(i, i.ToString())));

            foreach (var i in Enumerable.Range(1, 100))
            {
                var dataCount = data.Count;

                data.RemoveAll(i, c => c.Key);

                Assert.AreEqual(dataCount - 1, data.Count);
            }
        }

        [Test]
        public void TestRemoveAllItem([Values(1000, 6, 5, 4, 3, 2, 1)] int count,
            [Values(999, 5, 4, 3, 2, 1, 0)] int item, [Values(3, 1, 0)] int duplicates)
        {
            var array = Enumerable.Range(0, count).ToList();

            for (int i = 0; i < duplicates; i++)
            {
                array.AddRange(Enumerable.Range(0, count));
            }

            var dataList = array.ToData();

            dataList.RemoveAll(item);
            array.RemoveAll(r => r == item);

            Assert.AreEqual(dataList.Count, dataList.Count);

            for (int i = 0; i < array.Count; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestFindIndex([Values(25000, 1000, 6, 5, 4, 3, 2, 1)] int count,
            [Values(24466, 999, 5, 4, 3, 2, 1, 0)] int item, [Values(2, 1, 0)] int duplicates)
        {
            var array = Enumerable.Range(0, count).ToList();

            for (int i = 0; i < duplicates; i++)
            {
                array.AddRange(Enumerable.Range(0, count));
            }

            var dataList = array.ToData();

            Assert.True(dataList.FindIndex(v => v == item) == array.FindIndex(v => v == item));
        }

        [Test]
        public void TestRemoveAllHuge([Values(15000)] int count, [Values(14444, 999, 5, 4, 3, 2, 1, 0)] int item,
            [Values(2, 1, 0)] int duplicates)
        {
            var array = Enumerable.Range(0, count).ToList();

            for (int i = 0; i < duplicates; i++)
            {
                array.AddRange(Enumerable.Range(0, count));
            }

            var dataList = array.ToData();

            dataList.RemoveAll(r => r == item);
            array.RemoveAll(r => r == item);

            Assert.AreEqual(dataList.Count, dataList.Count);

            for (int i = 0; i < array.Count; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestRemoveAllHuge2([Values(15000)] int count, [Values(14444, 999, 5, 4, 3, 2, 1, 0)] int item,
            [Values(2, 1, 0)] int duplicates)
        {
            var array = Enumerable.Range(0, count).ToList();

            for (int i = 0; i < duplicates; i++)
            {
                array.AddRange(Enumerable.Range(0, count));
            }

            var dataList = array.ToData();

            dataList.RemoveAll(item);

            array.RemoveAll(r => r == item);

            Assert.AreEqual(dataList.Count, dataList.Count);

            for (int i = 0; i < array.Count; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];

                Assert.AreEqual(val, arrVal);
            }
        }

        [Test]
        public void TestCtr([Values(4, 1000, 100000)] int count)
        {
            var random = new Random();

            var array = Enumerable.Range(0, count).Select(i => random.Next()).ToArray();

            var list = array.ToData();

            var newList = new Data<int>(list);

            for (int i = 0; i < array.Length; i++)
            {
                var val = list[0];
                var arrVal = array[0];
                var valNew = newList[0];

                Assert.AreEqual(val, arrVal);
                Assert.AreEqual(valNew, val);
            }
        }

        [Test]
        public void TestFindIndex2([Values(25000, 1000, 4, 3, 2, 1)] int count,
            [Values(24466, 999, 4, 3, 2, 1, 0)] int item, [Values(2, 1, 0)] int duplicates)
        {
            var list = Enumerable.Range(0, count).Select(i => new KeyValuePair<int, int>(i, -i)).ToList();

            for (int index = 0; index < duplicates; index++)
            {
                list.AddRange(Enumerable.Range(0, count).Select(i => new KeyValuePair<int, int>(i, -i)));
            }

            var dataList = list.ToData();

            Assert.True(dataList.FindIndex(item, (kv) => kv.Value) == list.FindIndex(v => v.Value == item));
        }

        [Test]
        public void TestClearFill([Values(25000, 1000, 4, 3, 2, 1)] int count)
        {
            var array = Enumerable.Range(0, count).Select(i => new KeyValuePair<int, int>(i, -i)).ToArray();

            var dataList = array.ToData();

            dataList.Clear();

            dataList.AddRange(array);

            for (int i = 0; i < array.Length; i++)
            {
                var val = dataList[0];
                var arrVal = array[0];

                Assert.AreEqual(val, arrVal);
            }

            if (dataList is IDisposable d)
            {
                d.Dispose();

                Assert.AreEqual(0, dataList.Count);
            }
        }

        [Test]
        public void TestBinarySearch([Values(25000, 1000, 4, 3, 2, 1)] int count,
            [Values(24466, 999, 4, 3, 2, 1, 0)] int item)
        {
            var array = Enumerable.Range(0, count).ToArray();

            var dataList = array.ToData();

            var ab = Array.BinarySearch(array, 0, array.Length, item);
            var hb = dataList.BinarySearch(item, 0, dataList.Count);

            var readOnlyList = (IReadOnlyList<int>)dataList;
            var rb = readOnlyList.BinarySearchExact(item, 0, readOnlyList.Count, (val, item_) => val.CompareTo(item_));

            var index = dataList.BinarySearch(new IdVal(){Id = item}, 0, dataList.Count, (trId, change) => trId.Id.CompareTo(change));
            
            Assert.AreEqual(index, hb);
            Assert.AreEqual(ab, hb);
            Assert.AreEqual(ab, rb);
        }
        
        private class IdVal
        {
            public int Id { get; set; }
        }

        [Test]
        public void AddLists()
        {
            var l1 = Enumerable.Range(0, 100).ToData();
            var l2 = Enumerable.Range(100, 100).ToData();

            var l3 = l1 + l2;

            Assert.True(l3.Count == l2.Count + l1.Count);

            foreach (var item in l1)
            {
                var binarySearch = l3.BinarySearch(item);

                Assert.True(binarySearch >= 0, $"{item}");
            }

            foreach (var item in l2)
            {
                var binarySearch = l3.BinarySearch(item);

                Assert.True(binarySearch >= 0, $"{item}");
            }
        }

        [Test]
        public void TestEqOperator()
        {
            var l1 = Enumerable.Range(0, 100).ToData();
            var l2 = Enumerable.Range(0, 100).ToData();
            var l3 = Enumerable.Range(0, 101).ToData();

            Assert.True(l1 == l2);
            Assert.False(l1 == l3);
        }

        [Test]
        public void TestAddRangeArr()
        {
            var list = new Data<int>();

            var array = Enumerable.Range(0, 100).ToArray();

            list.AddRange(array);

            for (var index = 0; index < list.Count; index++)
            {
                Assert.AreEqual(array[index], list[index]);
            }
        }

        [Test]
        public void TestAddRangeHashset()
        {
            var list = new Data<int>();

            var array = Enumerable.Range(0, 100).ToArray();

            var set = Enumerable.Range(0, 100).ToHashSet();

            list.AddRange(set);

            list.Sort();

            for (var index = 0; index < list.Count; index++)
            {
                Assert.AreEqual(array[index], list[index]);
            }
        }

        [Test]
        public void TestBinarySearchLeft()
        {
            var l1 = _.List(0);
            var ll1 = (IReadOnlyList<int>)l1;

            Assert.AreEqual(l1.FindIndex(0, i => i), l1.BinarySearchLeft(0, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, l1.BinarySearchLeft(2, (a, v) => a.CompareTo(v)));

            Assert.AreEqual(ll1.FindIndex(0, i => i), ll1.BinarySearchLeft(0, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, ll1.BinarySearchLeft(2, (a, v) => a.CompareTo(v)));

            l1 = _.List(1, 2);
            ll1 = (IReadOnlyList<int>)l1;

            Assert.AreEqual(l1.FindIndex(1, i => i), l1.BinarySearchLeft(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(l1.FindIndex(2, i => i), l1.BinarySearchLeft(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, l1.BinarySearchLeft(3, (a, v) => a.CompareTo(v)));

            Assert.AreEqual(ll1.FindIndex(1, i => i), ll1.BinarySearchLeft(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(ll1.FindIndex(2, i => i), ll1.BinarySearchLeft(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, ll1.BinarySearchLeft(3, (a, v) => a.CompareTo(v)));

            l1 = _.List(1, 1, 2, 2);
            ll1 = (IReadOnlyList<int>)l1;

            Assert.AreEqual(l1.FindIndex(1, i => i), l1.BinarySearchLeft(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(l1.FindIndex(2, i => i), l1.BinarySearchLeft(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, l1.BinarySearchLeft(3, (a, v) => a.CompareTo(v)));

            Assert.AreEqual(ll1.FindIndex(1, i => i), ll1.BinarySearchLeft(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(ll1.FindIndex(2, i => i), ll1.BinarySearchLeft(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, ll1.BinarySearchLeft(3, (a, v) => a.CompareTo(v)));

            l1 = _.List(0, 1, 1, 2, 2, 4);
            ll1 = (IReadOnlyList<int>)l1;

            Assert.AreEqual(l1.FindIndex(1, i => i), l1.BinarySearchLeft(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(l1.FindIndex(2, i => i), l1.BinarySearchLeft(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, l1.BinarySearchLeft(3, (a, v) => a.CompareTo(v)));

            Assert.AreEqual(ll1.FindIndex(1, i => i), ll1.BinarySearchLeft(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(ll1.FindIndex(2, i => i), ll1.BinarySearchLeft(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, ll1.BinarySearchLeft(3, (a, v) => a.CompareTo(v)));
        }

        [Test]
        public void TestBinarySearchRight()
        {
            var l1 = _.List(0);
            var ll1 = (IReadOnlyList<int>)l1;

            Assert.AreEqual(l1.FindLastIndex(0, i => i), l1.BinarySearchRight(0, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, l1.BinarySearchRight(2, (a, v) => a.CompareTo(v)));

            Assert.AreEqual(ll1.FindLastIndex(0, i => i), ll1.BinarySearchRight(0, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, ll1.BinarySearchRight(2, (a, v) => a.CompareTo(v)));


            l1 = _.List(1, 2);
            ll1 = (IReadOnlyList<int>)l1;

            Assert.AreEqual(l1.FindLastIndex(1, i => i), l1.BinarySearchRight(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(l1.FindLastIndex(2, i => i), l1.BinarySearchRight(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, l1.BinarySearchRight(3, (a, v) => a.CompareTo(v)));

            Assert.AreEqual(ll1.FindLastIndex(1, i => i), ll1.BinarySearchRight(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(ll1.FindLastIndex(2, i => i), ll1.BinarySearchRight(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, ll1.BinarySearchRight(3, (a, v) => a.CompareTo(v)));

            l1 = _.List(1, 1, 2, 2);
            ll1 = (IReadOnlyList<int>)l1;

            Assert.AreEqual(l1.FindLastIndex(1, i => i), l1.BinarySearchRight(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(l1.FindLastIndex(2, i => i), l1.BinarySearchRight(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, l1.BinarySearchLeft(3, (a, v) => a.CompareTo(v)));

            Assert.AreEqual(ll1.FindLastIndex(1, i => i), ll1.BinarySearchRight(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(ll1.FindLastIndex(2, i => i), ll1.BinarySearchRight(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, ll1.BinarySearchLeft(3, (a, v) => a.CompareTo(v)));

            l1 = _.List(0, 1, 1, 2, 2, 4);
            ll1 = (IReadOnlyList<int>)l1;

            Assert.AreEqual(l1.FindLastIndex(1, i => i), l1.BinarySearchRight(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(l1.FindLastIndex(2, i => i), l1.BinarySearchRight(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, l1.BinarySearchRight(3, (a, v) => a.CompareTo(v)));

            Assert.AreEqual(ll1.FindLastIndex(1, i => i), ll1.BinarySearchRight(1, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(ll1.FindLastIndex(2, i => i), ll1.BinarySearchRight(2, (a, v) => a.CompareTo(v)));
            Assert.AreEqual(-1, ll1.BinarySearchRight(3, (a, v) => a.CompareTo(v)));
        }

        [Test]
        public void ToHashsetTest()
        {
            var l1 = _.List(0, 1, 1, 2, 2, 4) + _.List(0, 1, 1, 2, 2, 4);

            var hashSet = l1.ToSet();
            var set1 = ((IReadOnlyCollection<int>)hashSet).ToSet();

            var ints = new HashSet<int>(hashSet);


            Assert.AreEqual(ints.Count, hashSet.Count);
            Assert.AreEqual(ints.Count, set1.Count);

            foreach (var i in ints)
            {
                Assert.True(hashSet.Contains(i));
            }

            foreach (var i in ints)
            {
                Assert.True(set1.Contains(i));
            }
        }

        private class DisposableTest : IDisposable
        {
            public bool Disposed { get; set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        [Test]
        public void DisposableCollTest()
        {
            var disposableCollection = new DisposableCollection();

            var disposableTests = _.List(new DisposableTest(), new DisposableTest(), new DisposableTest());

            foreach (var disposableTest in disposableTests)
            {
                disposableCollection.AddDisposable(disposableTest);
            }

            disposableCollection.RemoveDisposable(disposableTests.Last());

            disposableCollection.Dispose();

            Assert.True(disposableTests.Take(2).All(d => d.Disposed));
            Assert.False(disposableTests.Last().Disposed);
        }

        [Test]
        public void DebugView()
        {
            var list = _.List(1, 2, 3);
            var debugView = new CollectionDebugView<int>(list);
            Assert.True(list.SequenceEqual(debugView.Items));
        }

        [Test]
        public void DebugDicView()
        {
            var map = _.Map((1, 1), (2, 2), (3, 3));
            var debugView = new DictionaryDebugView<int, int>(map);
            Assert.True(map.ToArray().SequenceEqual(debugView.Items));
        }

        [Test]
        public void TestSetSerialization()
        {
            var set = new Data<int>();

            set.AddRange(Enumerable.Range(0, 10000));

            var serializeWithDcs = SerializeHelper.SerializeWithDcs(set);

            var deserializeWithDcs = SerializeHelper.DeserializeWithDcs<Data<int>>(serializeWithDcs);

            Assert.AreEqual(deserializeWithDcs, set);
        }

        [Test]
        public void TestSetSerialization2()
        {
            var set = new Data<int>();

            set.AddRange(Enumerable.Range(0, 10000));

            var clone = SerializeHelper.Clone<Data<int>>(set);

            Assert.AreEqual(clone, set);
        }

        [Test]
        public void ResizeTest()
        {
            var list = _.List(0);
            
            list.Resize(5);
            
            Assert.AreEqual(5, list.Count);

            var ints = new Data<int>();
            
            ints.Resize(100, 1);

            Assert.AreEqual(100, ints.Count);
            Assert.True(ints.All(l => l == 1));
            
            ints.Resize(50);
            Assert.AreEqual(50, ints.Count);
            Assert.True(ints.All(l => l == 1));
            
            ints.Resize(0);
            Assert.AreEqual(0, ints.Count);
        }

        [Test]
        public void TestMergeAsSorted([Values(0, 2, 14, 1024)] int d1, [Values(0, 2, 14, 1024)] int d2, [Values(-4, 0)] int d2Off)
        {
            var data1 = Enumerable.Range(0, d1).ToData();
            var data2 = Enumerable.Range(Math.Abs(d1 + d2Off), d2).ToData();

            var res = new Data<int>();
            
            res.MergeAscSorted(data1, data2, (x, y) => x.CompareTo(y));
            
            Assert.True(res == (data1 + data2).OrderBy(r => r).ToData());
            
            var res1 = new Data<int>();
            
            res1.MergeAscSorted(data2, data1, (x, y) => x.CompareTo(y));
            
            Assert.True(res == (data1 + data2).OrderBy(r => r).ToData());
        }
    }
}