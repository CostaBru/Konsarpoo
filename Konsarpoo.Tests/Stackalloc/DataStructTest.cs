using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Konsarpoo.Collections.Stackalloc;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests.Stackalloc;

[TestFixture(16)]
[TestFixture(32)]
[TestFixture(1000)]
public class DataTest 
{
    public int N { get; set; }
        
    public DataTest(int capacity)
    {
        N = capacity;
    }
     

    [Test]
    public void TestReverse([Values( 1000, 6, 5, 4, 3, 2, 1, 0)] int count)
    {
        if (count > N)
        {
            return;
        }
            
        var list = Enumerable.Range(0, count).ToList();
        Span<int> initStore = stackalloc int[N];
        var dataList = new DataStruct<int>(ref initStore);
        dataList.AddRange(list);

        list.Reverse();
        dataList.Reverse();

        for (int i = 0; i < list.Count; i++)
        {
            var val = list[i];
            var arrVal = dataList[i];

            Assert.AreEqual(val, arrVal);
        }
    }

    [Test]
    public void TestArgs([Values(1000, 6, 5, 4, 3, 2, 1, 0)] int count)
    {
        if (count > N)
        {
            return;
        }
        
        var array = Enumerable.Range(0, count).ToArray();

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataStruct<int>(ref initStore);
        dataList.AddRange(array);

        try
        {
            var i = dataList[-1];
            Assert.True(false);
        }
        catch (IndexOutOfRangeException e)
        {
        }
        
        try
        {
            var i = dataList[count + 1];
            Assert.True(false);
        }
        catch (IndexOutOfRangeException e)
        {
        }
        
        try
        {
            var i = dataList[count * count];
            Assert.True(false);
        }
        catch (IndexOutOfRangeException e)
        {
        }
    }

    [Test]
    public void TestInsert([Values(0, 1, 2, 3, 4, 5, 6, 7, 8, 51, 1000)] int count,
        [Values(0, 1, 2, 3, 4, 5, 6, 7, 50, 1000)] int insertPosition)
    {
        if (count > N)
        {
            return;
        }
        
        if (insertPosition <= count)
        {
            var array = Enumerable.Range(1, count).ToArray();

            Span<int> initStore = stackalloc int[N];
            var dataList = new DataStruct<int>(ref initStore);
            dataList.AddRange(array);
            
            var copy = dataList.ToList();

            dataList.Insert(insertPosition, -999);
            copy.Insert(insertPosition, -999);

            Assert.GreaterOrEqual(dataList.IndexOf(-999), 0);

            for (int i = 0; i < copy.Count; i++)
            {
                Assert.AreEqual(copy[i], dataList[i]);
            }
        }
    }

    [Test]
    public void TestCopyCommon()
    {
        var array = Enumerable.Range(0, N).ToArray();

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataStruct<int>(ref initStore);
        dataList.AddRange(array);

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
        if (N - 2 < 0)
        {
            return;
        }
        
        var list = Enumerable.Range(0, N - 2).Reverse().ToList();
        
        Span<int> initStore = stackalloc int[N];
        var dataList = new DataStruct<int>(ref initStore);
        dataList.AddRange(list);

        dataList.Sort(new IntComp());
        list.Sort();

        for (var index = 0; index < list.Count; index++)
        {
            Assert.AreEqual(list[index], dataList[index]);
        }

        for (var index = 0; index < list.Count; index++)
        {
            var i = list[index];

            var binarySearch = dataList.BinarySearch(i, i, (v1, v2) => v1.CompareTo(v2));

            Assert.AreEqual(i, dataList[binarySearch]);

            var search = list.BinarySearch(i);
                
            Assert.AreEqual(i, list[search]);
        }

        list.AddRange(Enumerable.Range(100, 2));

        list.Sort();

        foreach (var val in Enumerable.Range(100, 2))
        {
            var binarySearch = dataList.BinarySearch(val, (v1, v2) => v1.CompareTo(v2));

            Assert.True(dataList.Insert(~binarySearch, val));
        }

        for (var index = 0; index < list.Count; index++)
        {
            Assert.AreEqual(list[index], dataList[index]);
        }
    }

    [Test]
    public void TestRemoveAll1()
    {
        var l1 = new Data<int>(Enumerable.Range(0, 5));
        var l2 = new Data<int>(Enumerable.Range(5, 5));

        var l3 = l1 + l2;
        
        Span<int> initStore = stackalloc int[N];
        var dataList = new DataStruct<int>(ref initStore);
        dataList.AddRange(l3);

        var cnt = dataList.RemoveAll(i => i >= 5);

        Assert.AreEqual(5, cnt);

        Assert.AreEqual(l1, dataList.ToData());
        
        Span<int> initStore1 = stackalloc int[N];
        var dataList1 = new DataStruct<int>(ref initStore1);
        dataList1.AddRange(Enumerable.Range(0, 100).ToArray());

        var list = new Data<int>(Enumerable.Range(0, 100).ToArray());

        list.RemoveAll(i => i + 1 % 2 != 0);
        dataList1.RemoveAll(i => i + 1 % 2 != 0);

        var filtered = dataList1.ToData(i => i + 1 % 2 == 0);

        Assert.AreEqual(list, filtered);
    }

    [Test]
    public void TestRemoveCommonSome()
    {
        var list = Enumerable.Range(0, N).ToList();

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataStruct<int>(ref initStore);
        dataList.AddRange(list);

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
    public void TestRemoveCommonAll()
    {
        var list = Enumerable.Range(0, N).ToList();

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataStruct<int>(ref initStore);
        dataList.AddRange(list);

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
    public void TestRandomCommonSort()
    {
        var random = new Random(100);

        var array = Enumerable.Range(0, N).Select(i => random.Next()).ToArray();

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataStruct<int>(ref initStore);
        dataList.AddRange(array);

        Assert.AreEqual(array.Length, dataList.Length);

        Array.Sort(array);

        dataList.Sort((x, y) => x.CompareTo(y));

        Assert.AreEqual(array.Length, dataList.Count);

        for (int i = 0; i < array.Length; i++)
        {
            var val = dataList[i];
            var arrVal = array[i];

            Assert.AreEqual(val, arrVal);
        }
    }

    [Test]
    public void TestCommonCopyTo()
    {
        var list = Enumerable.Range(0, N).ToList();
        Span<int> initStore = stackalloc int[N];
        var data = new DataStruct<int>(ref initStore);
        data.AddRange(list);

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
        Span<int> initStore = stackalloc int[N];
        var data = new DataStruct<int>(ref initStore);
        
        Assert.AreEqual(0, data.RemoveAll(r => r == 1));
    }
   

    [Test]
    public void TestCommonCopyToArray([Values(0, 1)] int index)
    {
        var array = Enumerable.Range(1, N).ToArray();

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataStruct<int>(ref initStore);

        dataList.AddRange(array);

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
}