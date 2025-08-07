using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Konsarpoo.Collections.Allocators;
using NUnit.Framework;

// ReSharper disable HeapView.BoxingAllocation


namespace Konsarpoo.Collections.Tests
{
    [TestFixture(16, AllocatorType.GC, 0)]
    [TestFixture(0, AllocatorType.GC, 0)]
    [TestFixture(32, AllocatorType.Mixed, 16)]
    [TestFixture(16, AllocatorType.Pool, 0)]
    [TestFixture(1024, AllocatorType.GC, 0)]
    [TestFixture(1024, AllocatorType.Mixed, 512)]
    [TestFixture(1024, AllocatorType.Pool, 0)]
    public class DataTest : BaseTest
    {
        public DataTest(int? maxSizeOfArrayBucket, AllocatorType allocatorType, int gcLen) : base((ushort)maxSizeOfArrayBucket, allocatorType, (ushort)gcLen)
        {
        }

        [Test]
        public void TestCustomAllocator()
        {
            var dataPoolSetup = GcAllocatorSetup.GetDataPoolSetup<int>();
            
            var l1 = new Data<int>(0, 16, dataPoolSetup);
            l1.AddRange(Enumerable.Range(0, 50));

            var l2 = new Data<int>(0, 16, dataPoolSetup);
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
            var arrayAllocatorAllocator = new ArrayAllocatorAllocator<int>();
            
            {
                var poolList = new PoolList<int>((ushort)count, 0);

                for (int i = 0; i < count; i++)
                {
                    poolList.Add(i, arrayAllocatorAllocator);
                }

                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(i, poolList[i]);
                }

                var poolListCopy = new PoolList<int>(poolList, arrayAllocatorAllocator);

                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(i, poolListCopy[i]);
                }

                poolList.Clear(arrayAllocatorAllocator);

                Assert.AreEqual(0, poolList.Count);
            }

            GC.Collect();
        }

        [Test]
        public void TestPoolList2([Values(25000, 1000, 6, 5, 4, 3, 2, 1, 0)] int count)
        {
            var arrayAllocatorAllocator = new ArrayAllocatorAllocator<int>();

            {
                var poolList = new PoolList<int>((ushort)count, 0);

                for (int i = 0; i < count; i++)
                {
                    poolList.Add(i, arrayAllocatorAllocator);
                }

                poolList.Clear(arrayAllocatorAllocator);

                Assert.AreEqual(0, poolList.m_items.Length);
                Assert.AreEqual(0, poolList.m_size);
            }

            GC.Collect();
        }

        [Test]
        public void TestPoolListInsert([Values(25000, 1000, 6, 5, 4, 3, 2, 1, 0)] int count)
        {
            var arrayAllocatorAllocator = new ArrayAllocatorAllocator<int>();
            
            {
                var poolList = new PoolList<int>((ushort)count, 0);
                var list = new List<int>(count);

                for (int i = 0; i < count; i++)
                {
                    poolList.Insert(0, i, arrayAllocatorAllocator);
                    list.Insert(0, i);

                    Assert.AreEqual(list[i], poolList[i]);
                }

                var enumerator = ((IEnumerable)poolList).GetEnumerator();
                enumerator.MoveNext();

                foreach (var i in list)
                {
                    Assert.AreEqual(i, enumerator.Current);
                    enumerator.MoveNext();
                }
            }
        }

        [Test]
        public void TestIList([Values(123, 6, 5, 4, 3, 2, 1, 0)] int count)
        {
            var data = (IList)new Data<int>();

            for (int i = 0; i < count; i++)
            {
                data.Add(i);

                Assert.True(data.Contains(i));
                Assert.AreEqual(i, data.IndexOf(i), i);
            }
            
            var data1 = (IList)new Data<int>();
            
            for (int i = 0; i < count; i++)
            {
                data1.Add(i);
                
                data1[i] = i;

                Assert.True(data1.Contains(i));
                Assert.AreEqual(i, data1.IndexOf(i));
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
        public void TestInsert([Values(0, 1, 2, 3, 4, 5, 6, 7, 8, 51, 25001)] int count,
            [Values(0, 1, 2, 3, 4, 5, 6, 7, 50, 25000)] int insertPosition)
        {
            var dataList = Enumerable.Range(0, count).Reverse().ToData();

            if (insertPosition <= dataList.Count)
            {
                var copy = dataList.ToList();
                var vector = new std.vector<int>(dataList);

                dataList.Insert(insertPosition, -999);
                copy.Insert(insertPosition, -999);
                vector.insert(insertPosition, -999);

                Assert.GreaterOrEqual(dataList.IndexOf(-999), 0);
                Assert.GreaterOrEqual(((IList<int>)dataList).IndexOf(-999), 0);
                Assert.False(((IList<int>)dataList).IsReadOnly);

                for (int i = 0; i < copy.Count; i++)
                {
                    Assert.AreEqual(copy[i], dataList[i]);
                    Assert.AreEqual(copy[i], vector.at(i));
                }
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

            for (int i = 0; i < array.Length; i++)
            {
                var storageNode = dataList.GetStorageNode(i);
                
                Assert.NotNull(storageNode);
                Assert.NotNull(storageNode.Storage);
            }

            var ints = new Data<int>();
            
            Assert.Throws<IndexOutOfRangeException>(() => ints.GetStorageNode(0));
            
            ints.Add(1);

            var node = ints.GetStorageNode(0);
            
            Assert.NotNull(node);
            
            Assert.AreEqual(1, node.Storage[0]);
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
            var vect1 = new std.vector<int>(Enumerable.Range(0, 100).Reverse());
            var vect2 = new std.vector<int>(Enumerable.Range(0, 100).Reverse());
            var vect3 = new std.vector<int>(Enumerable.Range(0, 100).Reverse());
            var vect4 = new std.vector<int>(Enumerable.Range(0, 100).Reverse());

            var comparison = new Comparison<int>((x, y) => x.CompareTo(y));

            vect1.sort();
            vect2.sort((x, y) => x.CompareTo(y));
            vect3.sort(comparison);
            vect4.sort(new IntComp());
            
            list1.Sort();

            list2.Sort((x, y) => x.CompareTo(y));
            list3.Sort(comparison);

            list4.Sort();
            list5.Sort(new IntComp());

            for (var index = 0; index < list1.Count; index++)
            {
                Assert.AreEqual(list1[index], list2[index]);
                Assert.AreEqual(list2[index], list3[index]);
                Assert.AreEqual(list4[index], list4[index]);
                Assert.AreEqual(list4[index], vect1[index]);
                Assert.AreEqual(list4[index], vect2[index]);
                Assert.AreEqual(list4[index], vect3[index]);
                Assert.AreEqual(list4[index], vect4[index]);
            }

            for (var index = 0; index < list1.Count; index++)
            {
                var i = list1[index];

                var binarySearch = list5.BinarySearch(i, i);

                Assert.AreEqual(i, list5[binarySearch]);

                var search = list5.BinarySearch(i, 0, list5.Count, new IntComp());
                
                Assert.AreEqual(i, list5[search]);
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
        public void TestSort2()
        {
            Data<int> data = new Data<int>(0, 16);
            
            int j = 0;
            for (int i = (1024 * 100) - 1; i >= 0; i--)
            {
                data.Add(i);
                j++;
            }
            
            data.Sort();
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
        public void TestRemoveCommonAllReverse()
        {
            var list = Enumerable.Range(0, 1000).ToList();

            var dataList = list.ToData();

            Assert.AreEqual(list.Count, dataList.Count);

            var array = list.ToArray();

            foreach (var i in array.Reverse())
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
        public void TestCommonCopyTo()
        {
            var list = Enumerable.Range(0, 100).ToList();
            var data = Enumerable.Range(0, 100).ToData();

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var copyTo1 = new int[150];
                    var copyTo2 = new int[150];

                    data.CopyTo(i, copyTo1, j, 5);
                    list.CopyTo(i, copyTo2, j, 5);
                    
                    for (int index = 0; index < copyTo1.Length; index++)
                    {
                        var val = copyTo1[index];
                        var arrVal = copyTo2[index];

                        Assert.AreEqual(val, arrVal);
                    }
                }
            }
        }
        
        private class Comparer<T> : IComparer<T>
        {
            private readonly Comparison<T> m_comparison;

            public Comparer(Comparison<T> comparison)
            {
                m_comparison = comparison;
            }

            public int Compare(T x, T y)
            {
                return m_comparison(x, y);
            }
        }

        [Test]
        public void TestRemoveOnEmpty()
        {
            Assert.AreEqual(0, new Data<int>().RemoveAll(1));
            Assert.AreEqual(0, new Data<int>().RemoveAll(1, new Comparer<int>((x, y) => x.CompareTo(y))));
            Assert.AreEqual(0, new Data<int>().RemoveAll(1, i => i));
            Assert.AreEqual(0, new Data<int>().RemoveAll(1, i => i, EqualityComparer<int>.Default));
        }

        [Test]
        public void TestExceptionsOnEmpty([Values(0, 1, 2, 1000)] int size)
        {
            var data = new Data<int>(Enumerable.Range(0, size));

            Assert.Throws<IndexOutOfRangeException>(() => data[size + 1] += 1);
            Assert.Throws<IndexOutOfRangeException>(() => data.ValueByRef(size + 1) += 1);
            Assert.Throws<IndexOutOfRangeException>(() => data.RemoveAt(size + 1));
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                data.Insert(size + 1, 5);
            });
            Assert.Throws<InvalidOperationException>(() => new Data<int>().RemoveLast());
            Assert.Throws<ArgumentNullException>(() => new Data<int>() {5}.RemoveAll(1, (Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => new Data<int>() {5}.RemoveAll(1, i => i, null));
            Assert.Throws<ArgumentNullException>(() => new Data<int>() {5}.FindIndex((Predicate<int>)null, 5));
            Assert.Throws<ArgumentNullException>(() => new Data<int>() {5}.FindLastIndex(1, (Func<int, int>)null, 5));
            Assert.Throws<InvalidOperationException>(() =>
            {
                var ints = new Data<int>() { 1, 2, 3 };
                foreach (var i in ints)
                {
                    ints.Add(i);
                }
            });
            
            Assert.Throws<ArgumentNullException>(() => data.AddRange((IEnumerable<int>)null));
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
        public void TestCommonCopyFromArray([Values(0, 10)] int index, [Values(100, 3000)] int copyCount)
        {
            var array = Enumerable.Range(0, 5000).ToArray();

            var dataList = new Data<int>();

            dataList.Ensure(copyCount + index);

            Assert.True(dataList.HasList);

            dataList.CopyFrom(index, array, index, copyCount);

            for (int i = index; i < copyCount; i++)
            {
                var val = dataList[i];
                var arrVal = array[i];

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
        public void TestCommonInsert([Values(5000, 4, 3, 2, 1, 0)] int index)
        {
            //var list = Enumerable.Range(0, 5001).ToList();
            var list = Enumerable.Range(0, 5001).ToList();

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
            
            var dataList1 = new Data<int>();

            foreach (var i in list)
            {
                dataList1.Add(i);
            }

            var i2 = dataList1[25000 - 2];
            var i1 = dataList1[25000 - 1];

            var dataList = new Data<int>();

            dataList.AddRange(list);

            Assert.True(dataList.HasList);

            list.Insert(index, -500);
            dataList.Insert(index, -500);
            var v = dataList[index];

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = index; i < list.Count; i++)
            {
                var val = list[i];
                var arrVal = dataList[i];

                Assert.AreEqual(val, arrVal);
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

                dataList.AddRange(list.Select(i => new int?(i)).ToData());

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
        public void RemoveAll3()
        {
            var data = new Data<int>();

            data.AddRange(Enumerable.Range(1, 100).Select(i => i));

            data.RemoveAll(1, new Comparer<int>((x, y) => 0));
            
            Assert.AreEqual(0, data.Count);
        }
        
        [Test]
        public void RemoveAll4()
        {
            var data = new Data<int>();

            data.AddRange(Enumerable.Range(1, 100).Select(i => i));

            data.RemoveAll((x) => true);
            
            Assert.AreEqual(0, data.Count);
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
                
                Assert.True(dataList.Version >= ushort.MaxValue);

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
        
        [Test]
        public void TestBinarySearchString()
        {
            var array = Enumerable.Range(0, 999).Select(i => i.ToString()).ToArray();

            var dataList = array.ToData();

            var ab = Array.BinarySearch(array, 0, array.Length, "999", StringComparer.Ordinal);
            var hb = dataList.BinarySearch("999", 0, dataList.Count, (x, y) => String.Compare(x, y, StringComparison.Ordinal));

            Assert.AreEqual(ab, hb);
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
            
            Assert.Throws<ArgumentNullException>(() => ll1.FindIndex(0, (Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ll1.FindIndex(0, (i) => i , (IEqualityComparer<int>)null));
            Assert.Throws<ArgumentNullException>(() => ll1.FindIndex(0, (Func<int, int, bool>)null));
            
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

            Assert.Throws<ArgumentNullException>(() => disposableCollection.RemoveDisposable(null));

            Assert.AreEqual(3, disposableCollection.Items.Count);

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
        public void VersionTest()
        {
            var ints = new Data<int>();
            ints.Ensure(5);
            var version = ints.Version;
            ints.Ensure(5);
            Assert.AreEqual(version, ints.Version);
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
        
        [Test]
        public void TestOp1()
        {
            var m1 = (Data<int>)null;
            var m2 = (Data<int>)null;
            var m3 = new Data<int>() {  1, 1  };
            var m4 = new Data<int>() {  1, 2  };
            var m5 = new Data<int>() {  2, 1  };
            var m6 = new Data<int>() {  1, 1 , 2, 2};
            
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
        }


        [Test]
        public void TestSet()
        {
            var ints = new Data<int>();
            var data1 = new Data<int>();
            
            ints.Ensure(10000);
            
            var arr = new int[10000];

            for (int i = 0; i < 10000; i++)
            {
                arr[i] = i;
                ints[i] = i;

                data1.Append(i);
                
                Assert.AreEqual(arr[i], ints[i]);
                Assert.AreEqual(arr[i], ints.ValueByRef(i));
                Assert.AreEqual(arr[i], data1.ValueByRef(i));
            }
        }

        [Test]
        public void TestMap()
        {
            var ints = new Data<int>();
            
            ints.Add(1);
            
            Assert.True(ints.ContainsKey(0));
            Assert.True(ints.TryGetValue(0, out var value));
            Assert.AreEqual(1, value);
            
            Assert.False(ints.ContainsKey(2));
            Assert.False(ints.TryGetValue(2, out var value2));
            Assert.AreEqual(default(int), value2);
        }
        
        [Test]
        public void TestGetOrDefaultAndFit()
        {
            var ints = new Data<int>();
            
            Assert.AreEqual(0, ints.GetOrDefault(1000));
            
            ints.PlaceAt(1000, 1000);
            
            Assert.AreEqual(1000, ints.GetOrDefault(1000));
            
            Assert.AreEqual(0, ints.GetOrDefault(10000));
        }
        
        private class DataItem
        {
            public Data<double> Items { get; set; }
        }
         
        [Test]
        public void TestDisposingHandler()
        {
            var lfuCache = new LfuCache<int, Data<DataItem>>((v) => v, disposingStrategy: (k,v ) => v.Dispose());

            var vals = new Data<DataItem>();

            var doubles = new Data<double>() { Double.Pi };
            vals.Add(new DataItem(){ Items = doubles});
            
            Data<DataItem>.SetDisposingHandler((d) =>
            {
                foreach (var item in d)
                {
                    item.Items.Dispose();
                }
            } );

            lfuCache.AddOrUpdate(0, vals);

            lfuCache.RemoveKey(0);
            
            Assert.AreEqual(0, doubles.Count);
        }

        [Test]
        public void TestReadonlyUnion()
        {
            var data1 = Enumerable.Range(0, 10).ToData();
            var data2 = Enumerable.Range(0, 10).ToData();
            var data3 = Enumerable.Range(0, 10).ToData();

            var dataExpected = new Data<int>();
            
            dataExpected.AddRange(data1);
            dataExpected.AddRange(data2);
            dataExpected.AddRange(data3);

            var unionAsReadOnlyList = data1.UnionAsReadOnlyListWith(data2, data3);
            
            Assert.AreEqual(dataExpected.Count, unionAsReadOnlyList.Count);

            for (int i = 0; i < 30; i++)
            {
                Assert.AreEqual(dataExpected[i], unionAsReadOnlyList[i]);
            }

            var enumerator = dataExpected.GetEnumerator();

            foreach (var val in unionAsReadOnlyList)
            {
                enumerator.MoveNext();
                
                Assert.AreEqual(enumerator.Current, val);
            }
        }
        
        [Test]
        public void TestReadonlyUnion1()
        {
            var data1 = Enumerable.Range(0, 10).ToData();

            var dataExpected = new Data<int>();
            
            dataExpected.AddRange(data1);

            var unionAsReadOnlyList = data1.UnionAsReadOnlyListWith();
            
            Assert.AreEqual(dataExpected.Count, unionAsReadOnlyList.Count);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(dataExpected[i], unionAsReadOnlyList[i]);
            }

            var enumerator = dataExpected.GetEnumerator();

            foreach (var val in unionAsReadOnlyList)
            {
                enumerator.MoveNext();
                
                Assert.AreEqual(enumerator.Current, val);
            }
        }
    }
}