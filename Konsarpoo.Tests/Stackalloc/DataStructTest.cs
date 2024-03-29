﻿using System;
using System.Collections.Generic;
using System.Linq;
using Konsarpoo.Collections.Stackalloc;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests.Stackalloc;

[TestFixture(16)]
[TestFixture(32)]
[TestFixture(1000)]
public class DataStructTest 
{
    public int N { get; set; }
        
    public DataStructTest(int capacity)
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
        var dataList = new DataRs<int>(ref initStore);
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
    public void TestEnumeration()
    {
        var list = Enumerable.Range(0, N).ToList();
        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);
        
        Assert.False(dataList.GetEnumerator().MoveNext());
        Assert.False(dataList.GetRsEnumerator().MoveNext());
        Assert.AreEqual(0, dataList.GetRsEnumerator().Count);
        
        dataList.AddRange(list);

        var le = list.GetEnumerator();
        var de = dataList.GetEnumerator();
        var dre = dataList.GetRsEnumerator();
        
        Assert.AreEqual(list.Count, dre.Count);

        while (le.MoveNext())
        {
            Assert.True(de.MoveNext());
            Assert.True(dre.MoveNext());
            
            Assert.AreEqual(le.Current, de.Current);
            Assert.AreEqual(le.Current, dre.Current);
        }
    }

    [Test]
    public void TestAddRange()
    {
        Span<int> buckets = stackalloc int[N];
        Span<KeyEntry<int>> entriesHash = stackalloc KeyEntry<int>[N];
        var set = new SetRs<int>(ref buckets, ref entriesHash);

        set.Add(1);
        set.Add(2);
        
        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);
        dataList.AddRange(ref set);
        
        Assert.True(dataList.ToData().SequenceEqual(set.ToData()));
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
        var dataList = new DataRs<int>(ref initStore);
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
    public void TestInsert([Values(0, 1, 2, 3, 4, 5, 6, 7, 8, 51, 999)] int count,
        [Values(0, 1, 2, 3, 4, 5, 6, 7, 50, 999)] int insertPosition)
    {
        if (count > N)
        {
            return;
        }
        
        if (insertPosition <= count)
        {
            var array = Enumerable.Range(1, count).ToArray();

            Span<int> initStore = stackalloc int[N];
            var dataList = new DataRs<int>(ref initStore);
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
        var dataList = new DataRs<int>(ref initStore);
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
        
        var list = Enumerable.Range(1, N - 2).Reverse().ToList();
        
        Span<int> initStore1 = stackalloc int[N];
        var dataList1 = new DataRs<int>(ref initStore1);
        dataList1.AddRange(list);
        
        Span<int> initStore2 = stackalloc int[N];
        var dataList2 = new DataRs<int>(ref initStore2);
        dataList2.AddRange(list);

        dataList2.Sort((x, y) => x.CompareTo(y));
        dataList1.Sort(new IntComp());
        list.Sort();

        for (var index = 0; index < list.Count; index++)
        {
            Assert.AreEqual(list[index], dataList1[index]);
            Assert.AreEqual(list[index], dataList2[index]);
        }

        for (var index = 0; index < list.Count; index++)
        {
            var i = list[index];

            var binarySearch = dataList1.BinarySearch(i, 0, (v1, v2) => v1.CompareTo(v2));

            Assert.AreEqual(i, dataList1[binarySearch]);

            var search = list.BinarySearch(i);
                
            Assert.AreEqual(i, list[search]);
        }

        list.AddRange(Enumerable.Range(10000, 2));

        list.Sort();

        foreach (var val in Enumerable.Range(10000, 2))
        {
            var binarySearch = dataList1.BinarySearch(val, (v1, v2) => v1.CompareTo(v2));

            dataList1.Insert(~binarySearch, val);
        }

        for (var index = 0; index < list.Count; index++)
        {
            Assert.AreEqual(list[index], dataList1[index]);
        }
    }

    [Test]
    public void TestRemoveAll1()
    {
        var l1 = new Data<int>(Enumerable.Range(0, 5));
        var l2 = new Data<int>(Enumerable.Range(5, 5));

        var l3 = l1 + l2;
        
        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);
        dataList.AddRange(l3);

        var cnt = dataList.RemoveAll(i => i >= 5);

        Assert.AreEqual(5, cnt);

        Assert.AreEqual(l1, dataList.ToData());
        
        Span<int> initStore1 = stackalloc int[N];
        var dataList1 = new DataRs<int>(ref initStore1);
        dataList1.AddRange(Enumerable.Range(0, N).ToArray());

        var list = new Data<int>(Enumerable.Range(0, N).ToArray());

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
        var dataList = new DataRs<int>(ref initStore);
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
        var dataList = new DataRs<int>(ref initStore);
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
        var dataList = new DataRs<int>(ref initStore);
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
        var data = new DataRs<int>(ref initStore);
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
        var data = new DataRs<int>(ref initStore);
        
        Assert.AreEqual(0, data.RemoveAll(r => r == 1));
    }
   

    [Test]
    public void TestCommonCopyToArray([Values(0, 1)] int index)
    {
        var array = Enumerable.Range(1, N).ToArray();

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);

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
    public void TestCommonInsert([Values(4, 3, 2, 1, 0)] int index)
    {
        var list = Enumerable.Range(0, N - 1).ToList();

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);

        dataList.AddRange(list);

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
    public void TestCommonEnsure([Values(3, 0)] int size)
    {
        for (int e = 0; e < 5; e++)
        {
            Span<int> initStore = stackalloc int[N];
            var list = new DataRs<int>(ref initStore);

            var newSize = size + size + e;

            list.Ensure(newSize, 5);

            Assert.AreEqual(newSize, list.Count);

            for (int i = 0; i < newSize; i++)
            {
                var arrVal = list[i];

                Assert.AreEqual(5, arrVal);
            }
        }
    }


    [Test]
    public void TestNotEmptyCommonEnsure([Values(1, 2)] int count,
        [Values(15, 59, 0)] int size)
    {
        if (size > N)
        {
            return;
        }
        
        if (count <= size)
        {
            var list = Enumerable.Range(0, count).ToList();

            Span<int> initStore = stackalloc int[N];
            var dataList = new DataRs<int>(ref initStore);

            dataList.AddRange(list.Select(i => i).ToData());

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

                    Assert.AreEqual(0, arrVal);
                }
            }
        }
    }

    [Test]
    public void TestRemoveAll([Values(5)] int count, [Values(4)] int item,
        [Values(3, 1, 0)] int duplicates)
    {
        if (count * (duplicates + 1) > N)
        {
            return;
        }
        
        var array = Enumerable.Range(0, count).ToList();

        for (int i = 0; i < duplicates; i++)
        {
            array.AddRange(Enumerable.Range(0, count));
        }

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);
        dataList.AddRange(array);

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
        Span<KeyValuePair<int, Guid>> initStore = stackalloc KeyValuePair<int, Guid>[N];
        var dataList = new DataRs<KeyValuePair<int, Guid>>(ref initStore);

        Span<KeyValuePair<int, Guid>> initStore2 = stackalloc KeyValuePair<int, Guid>[N];
        var dataList2 = new DataRs<KeyValuePair<int, Guid>>(ref initStore2);
        
        dataList2.AddRange(Enumerable.Range(1, N).Select(i => new KeyValuePair<int, Guid>(i, Guid.NewGuid())).ToArray());

        dataList.AddRange(ref dataList2);

        foreach (var i in Enumerable.Range(1, N))
        {
            var dataCount = dataList.Count;

            dataList.RemoveAll(i, c => c.Key);

            Assert.AreEqual(dataCount - 1, dataList.Count);
        }
    }
        
        
    [Test]
    public void RemoveAll4()
    {
        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);

        dataList.AddRange(Enumerable.Range(1, N).Select(i => i).ToArray());

        dataList.RemoveAll((x) => true);
            
        Assert.AreEqual(0, dataList.Count);
    }

    [Test]
    public void Avg()
    {
        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);

        var data = Enumerable.Range(1, N).Select(i => i).ToArray();
        
        dataList.AddRange(data);
        
        Assert.AreEqual(data.Average(), dataList.Average(d => d));
    }
    
    [Test]
    public void Max([Values(true, false)] bool reverse)
    {
        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);

        var data = Enumerable.Range(1, N).Select(i => i).ToList();

        if (reverse)
        {
            data.Reverse();
        }
        
        dataList.AddRange(data);
        
        Assert.AreEqual(data.Max(), dataList.Max());
    }
    
    [Test]
    public void Min([Values(true, false)] bool reverse)
    {
        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);

        var data = Enumerable.Range(1, N).Select(i => i).ToList();
        
        if (reverse)
        {
            data.Reverse();
        }
        
        dataList.AddRange(data);
        
        Assert.AreEqual(data.Min(), dataList.Min());
    }

    [Test]
    public void TestRemoveAllItem([Values(6, 5, 4, 3, 2, 1)] int count,
        [Values( 5, 4, 3, 2, 1, 0)] int item, [Values(3, 1, 0)] int duplicates)
    {
        if (count * (duplicates + 1) > N)
        {
            return;
        }
        
        var array = Enumerable.Range(0, count).ToList();

        for (int i = 0; i < duplicates; i++)
        {
            array.AddRange(Enumerable.Range(0, count));
        }

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);
        dataList.AddRange(array);

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
    public void TestFindIndex([Values(6, 5, 4, 3, 2, 1)] int count,
        [Values(999, 5, 4, 3, 2, 1, 0)] int item, [Values(2, 1, 0)] int duplicates)
    {
        if (count * (duplicates + 1) > N)
        {
            return;
        }
        
        var array = Enumerable.Range(0, count).ToList();

        for (int i = 0; i < duplicates; i++)
        {
            array.AddRange(Enumerable.Range(0, count));
        }

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);
        dataList.AddRange(array);

        Assert.True(dataList.IndexOf(v => v == item) == array.FindIndex(v => v == item));
    }
   

    [Test]
    public void TestFindIndex2([Values( 4, 3, 2, 1)] int count,
        [Values(999, 4, 3, 2, 1, 0)] int item, [Values(2, 1, 0)] int duplicates)
    {
        var list = Enumerable.Range(0, count).Select(i => new KeyValuePair<int, int>(i, -i)).ToList();

        for (int index = 0; index < duplicates; index++)
        {
            list.AddRange(Enumerable.Range(0, count).Select(i => new KeyValuePair<int, int>(i, -i)));
        }
        
        Span<KeyValuePair<int, int>> initStore = stackalloc KeyValuePair<int, int>[N];
        var dataList = new DataRs<KeyValuePair<int, int>>(ref initStore);

        dataList.AddRange(list);

        Assert.True(dataList.FindIndex(item, (kv) => kv.Value) == list.FindIndex(v => v.Value == item));
    }

    [Test]
    public void TestClearFill([Values(1000, 4, 3, 2, 1)] int count)
    {
        if (count > N)
        {
            return;
        }
        
        var array = Enumerable.Range(0, count).Select(i => new KeyValuePair<int, int>(i, -i)).ToArray();

        Span<KeyValuePair<int, int>> initStore = stackalloc KeyValuePair<int, int>[N];
        var dataList = new DataRs<KeyValuePair<int, int>>(ref initStore);

        dataList.AddRange(array);
        
        dataList.Clear();

        dataList.AddRange(array);

        for (int i = 0; i < array.Length; i++)
        {
            var val = dataList[0];
            var arrVal = array[0];

            Assert.AreEqual(val, arrVal);
        }
    }

    [Test]
    public void TestBinarySearch([Values(1000, 4, 3, 2, 1)] int count,
        [Values(24466, 999, 4, 3, 2, 1, 0)] int item)
    {
        if (count > N)
        {
            return;
        }
        
        var array = Enumerable.Range(0, count).ToArray();

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);
        dataList.AddRange(array);

        var ab = Array.BinarySearch(array, 0, array.Length, item);
        var hb = dataList.BinarySearch(item, 0, dataList.Count, (x, y) => x.CompareTo(y));

        var index = dataList.BinarySearch(new IdVal(){Id = item}, 0, dataList.Count, (trId, change) => trId.Id.CompareTo(change));
            
        Assert.AreEqual(index, hb);
        Assert.AreEqual(ab, hb);
    }
        
    private struct IdVal
    {
        public int Id { get; set; }
    }

    [Test]
    public void ToHashsetTest()
    {
        var l1 = _.List(0, 1, 1, 2, 2, 4) + _.List(0, 1, 1, 2, 2, 4);

        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);
        dataList.AddRange(l1);
        
        var hashSet = dataList.ToSet();

        var ints = new HashSet<int>(hashSet);

        Assert.AreEqual(ints.Count, hashSet.Count);

        foreach (var i in ints)
        {
            Assert.True(hashSet.Contains(i));
        }
    }
    
    
    [Test]
    public void TestExpressions()
    {
        var l1 = (_.List(0, 1, 1, 2, 2, 4) + _.List(0, 1, 1, 2, 2, 4))
            .Select(i => (i, i * 10))
            .ToArray();

        Span<(int key, int value)> initStore = stackalloc (int key, int value)[N];
        var dataList = new DataRs<(int key, int value)>(ref initStore);
        dataList.AddRange(l1);

        Assert.AreEqual(40, dataList.WhereFirstOrDefault(4, (val, index, dv) => val == dv.key).value);
        Assert.AreEqual(20, dataList.WhereLastOrDefault(2, (val, index, dv) => val == dv.key).value);

        var whereSelectResult = 0;
        dataList.WhereAggregate(target: 4, 
            (val, index, dv) => val == dv.key, 
            (val, index, dv) => whereSelectResult = dv.value);

        Assert.AreEqual(40, whereSelectResult);

        var tuples = dataList.ToData(i => i.key > 1);
        var tuples1 = dataList.ToData(1, (w, i) => i.key > w);
        
        Assert.AreEqual(tuples, tuples1);
        
        Assert.True(tuples.All(t => t.value > 1));
        Assert.True(dataList.All(i => i.key >= 0));
        Assert.True(dataList.All(0, (w, i) => i.key >= w));
        Assert.True(dataList.Any(i => i.key == 4));
        Assert.True(dataList.Any(4, (w, i) => i.key == w));
        
        var values = dataList.ToData((i,k) => k.key > 1, (i, k) => k.value);
        
        Assert.True(values.All(t => t > 1));

        Assert.True(dataList.ToArray().SequenceEqual(l1));
        Assert.True(dataList.ToList().SequenceEqual(l1));
        
        whereSelectResult = 0;
        dataList.WhereForEach((index, dv) => 4 == dv.key, 
            (index, dv) => whereSelectResult = dv.value);
        
        Assert.AreEqual(40, whereSelectResult);
        
        Assert.AreEqual(0, dataList.FirstOrDefault().key);
        Assert.AreEqual(4, dataList.LastOrDefault().key);
        
        Span<(int key, int value)> initStore1 = stackalloc (int key, int value)[N];
        var dataList1 = new DataRs<(int key, int value)>(ref initStore1);
        dataList1.AddRange(l1);
        
        Assert.True(dataList1.SequenceEquals(dataList));
        Assert.True(dataList1.SequenceEquals(l1));
        
        Assert.AreEqual(dataList1.Length - 2, dataList1.FindLastIndex((2, 20)));
        Assert.AreEqual(dataList1.Length - 2, dataList1.FindLastIndex(2, (p, k) => p == k.key));
        Assert.AreEqual(6, dataList1.FindLastIndex(k => k.key == 0));
        Assert.AreEqual(dataList1.Length - 1, dataList1.FindLastIndex(k => k.key == 4));
        
        Assert.AreEqual(3, dataList1.FindIndex((2, 20)));
        Assert.AreEqual(3, dataList1.FindIndex(2, (p, k) => p == k.key));
        Assert.AreEqual(0, dataList1.FindIndex(k => k.key == 0));
        Assert.AreEqual(5, dataList1.FindIndex(k => k.key == 4));
        
        var valSum = 0;
        dataList.ForEach((index, dv) => valSum += dv.value);
        
        Assert.AreEqual(l1.Sum( i=> i.Item2), valSum);
        
        var agg = new Aggregator();
        dataList.Aggregate(agg, (a, index, dv) => a.Val += dv.value);
        
        Assert.AreEqual(l1.Sum( i=> i.Item2), agg.Val);
    }
    
    private class Aggregator
    {
        public int Val { get; set; }
    }
}