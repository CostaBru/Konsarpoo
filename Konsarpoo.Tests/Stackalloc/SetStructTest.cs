using System;
using System.Collections.Generic;
using System.Linq;
using Konsarpoo.Collections.Stackalloc;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests.Stackalloc;

[TestFixture(16)]
[TestFixture(32)]
[TestFixture(1000)]
public class SetStructTest
{
    public int N { get; set; }

    public SetStructTest(int capacity)
    {
        N = capacity;
    }
    
    [Test]
    public void TestAggregate()
    {
        Span<int> buckets = stackalloc int[N];
        Span<KeyEntryStruct<int>> entriesHash = stackalloc KeyEntryStruct<int>[N];
        var set = new SetRs<int>(ref buckets, ref entriesHash);

        set.Add(1);
        set.Add(2);
        set.Add(3);
        set.Add(4);
        set.Add(5);
        
        Assert.AreEqual(1, set.KeyAt(0));

        var keys = new Data<int>();

        set.ForEach((k) => { keys.Add(k);});

        Assert.True(keys.SequenceEqual(Enumerable.Range(1, 5)));
        
        keys.Clear();
        
        set.Aggregate(keys, (kk, k) => { kk.Add(k);});
        
        Assert.True(keys.SequenceEqual(Enumerable.Range(1, 5)));
        Assert.True(keys.SequenceEqual(set.ToData()));
        Assert.True(keys.SequenceEqual(set.ToArray()));
        Assert.AreEqual(set.Count, set.Length);
        
        Assert.AreEqual(1, set.FirstOrDefault());
        Assert.AreEqual(5, set.LastOrDefault());
        
        keys.Clear();
        
        set.WhereForEach((k) => k > 0, (k) =>
        {
            keys.Add(k);
        });
        
        Assert.True(keys.SequenceEqual(Enumerable.Range(1, 5)));
        
        keys.Clear();
        
        set.WhereAggregate(keys, (kk, k) => k > 0, (kk, k) =>
        {
            kk.Add(k);
        });
        
        Assert.True(keys.SequenceEqual(Enumerable.Range(1, 5)));
    }
    

    [Test]
    public void TestRemoveIfEmpty()
    {
        Span<int> buckets = stackalloc int[N];
        Span<KeyEntryStruct<int>> entriesHash = stackalloc KeyEntryStruct<int>[N];
        var set = new SetRs<int>(ref buckets, ref entriesHash, EqualityComparer<int>.Default);


        Assert.False(set.Remove(0));
    }

    [Test]
    public void TestSmall()
    {
        Span<int> buckets = stackalloc int[N];
        Span<KeyEntryStruct<int>> entriesHash = stackalloc KeyEntryStruct<int>[N];
        var set = new SetRs<int>(ref buckets, ref entriesHash, EqualityComparer<int>.Default);
        var dict = new HashSet<int>();

        set.Add(1);
        set.Add(2);
        
        Assert.True(set.Contains(1));
        Assert.True(set.ContainsKey(2));

        var dict2 = set.ToSet();

        dict2.Add(3);
        dict.Add(0);
        dict.Add(1);

        Assert.AreEqual(dict.Count, set.Length);

        Assert.AreEqual(1, set[1]);
        Assert.AreEqual(2, set[2]);

        var keyTest = 999;

        Assert.False(set.TryGetValue(keyTest, out var _));

        try
        {
            var i = set[keyTest];
            
            Assert.False(true);
        }
        catch (KeyNotFoundException e)
        {
        }

        set.Add(keyTest);

        Assert.True(set.TryGetValue(keyTest, out var _));
        Assert.True(set.Contains(keyTest));
    }


    [Test]
    public void TestAddRange()
    {
        Span<int> buckets = stackalloc int[N];
        Span<KeyEntryStruct<int>> entriesHash = stackalloc KeyEntryStruct<int>[N];
        var set = new SetRs<int>(ref buckets, ref entriesHash, EqualityComparer<int>.Default);

        var array = Enumerable.Range(1, N).ToArray();

        set.AddRange(array);

        foreach (var i in array)
        {
            Assert.True(set.Contains(i));
            Assert.True(set.ContainsKey(i));
        }
        
        Span<int> initStore = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore);
        dataList.AddRange(array);
        
        set.Clear();
        
        foreach (var i in array)
        {
            Assert.False(set.Contains(i));
            Assert.False(set.ContainsKey(i));
        }

        set.AddRange(ref dataList);
        
        foreach (var i in array)
        {
            Assert.True(set.Contains(i));
            Assert.True(set.ContainsKey(i));
        }
        
        Span<int> buckets1 = stackalloc int[N];
        Span<KeyEntryStruct<int>> entriesHash1 = stackalloc KeyEntryStruct<int>[N];
        var set1 = new SetRs<int>(ref buckets1, ref entriesHash1, EqualityComparer<int>.Default);

        set1.AddRange(array);
        
        set.Clear();

        set.AddRange(ref set1);
        
        foreach (var i in array)
        {
            Assert.True(set.Contains(i));
            Assert.True(set.ContainsKey(i));
        }
    }

    [Test]
    public void TestEnumeration()
    {
        Span<int> buckets = stackalloc int[N];
        Span<KeyEntryStruct<int>> entriesHash = stackalloc KeyEntryStruct<int>[N];
        var set = new SetRs<int>(ref buckets, ref entriesHash, EqualityComparer<int>.Default);
        
        Assert.False(set.GetEnumerator().MoveNext());
        Assert.AreEqual(0, set.GetEnumerator().Count);
        
        set.Add(1);
        set.Add(2);
        set.Add(3);

        var ss = set.ToSet();

        var le = ss.GetEnumerator();
        var de = set.GetEnumerator();
        var dre = set.GetRsEnumerator();
        Assert.AreEqual(3, set.GetEnumerator().Count);

        while (le.MoveNext())
        {
            Assert.True(de.MoveNext());
            Assert.True(dre.MoveNext());
            
            Assert.AreEqual(le.Current, de.Current);
            Assert.AreEqual(le.Current, dre.Current);
        }
    }
    
    [Test]
    public void TestEx()
    {
        Span<int> buckets = stackalloc int[N];
        Span<KeyEntryStruct<int>> entriesHash = stackalloc KeyEntryStruct<int>[N];
        var set = new SetRs<int>(ref buckets, ref entriesHash, EqualityComparer<int>.Default);

        var data = Enumerable.Range(0, N).ToData();

        set.AddRange(data);

        try
        {
            set.Add(-1);
            Assert.Fail();
        }
        catch (InsufficientMemoryException e)
        {
        }
        
        set.Clear();

        set.Add(1);
        
        Assert.True(set.Contains(1));
    }
}

