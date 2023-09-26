using System;
using System.Collections.Generic;
using System.Linq;
using Konsarpoo.Collections.Stackalloc;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests.Stackalloc;

[TestFixture(16)]
[TestFixture(32)]
[TestFixture(1000)]
public class StackStructTest
{
    public int N { get; set; }
        
    public StackStructTest(int capacity)
    {
        N = capacity;
    }
    
    [Test]
    public void TestStack()
    {
        Span<int> initStore = stackalloc int[N];
        var st = new StackRs<int>(ref initStore);
        st.Push(0);
        
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

        foreach (var val in Enumerable.Range(0, 5))
        {
            stack.Push(val);
        }

        st.PushRange(Enumerable.Range(0, 5).ToArray());

        foreach (var val in Enumerable.Range(0, 5))
        {
            Assert.AreEqual(stack.Pop(), st.Pop());
        }
    }
    
    [Test]
    public void TestEnumeration()
    {
        var list = Enumerable.Range(0, N).ToData();
        Span<int> initStore = stackalloc int[N];
        var stackRs = new StackRs<int>(ref initStore);
        
        Assert.False(stackRs.GetEnumerator().MoveNext());
        Assert.False(stackRs.GetRsEnumerator().MoveNext());
        Assert.AreEqual(0, stackRs.GetRsEnumerator().Count);
        
        stackRs.PushRange(list);

        try
        {
            stackRs.Push(10000);
            
            Assert.Fail();
        }
        catch (InsufficientMemoryException e)
        {
        }

        list.Reverse();
        
        var le = list.GetEnumerator();
        var de = stackRs.GetEnumerator();
        var dre = stackRs.GetRsEnumerator();
        Assert.AreEqual(list.Count, dre.Count);

        while (le.MoveNext())
        {
            Assert.True(de.MoveNext());
            Assert.True(dre.MoveNext());
            
            Assert.AreEqual(le.Current, de.Current);
            Assert.AreEqual(le.Current, dre.Current);
        }

        stackRs.Pop();
        list.RemoveAt(0);
        
        le = list.GetEnumerator();
        de = stackRs.GetEnumerator();

        while (le.MoveNext())
        {
            Assert.True(de.MoveNext());
            
            Assert.AreEqual(le.Current, de.Current);
        }

        for (int i = 0; i < N - 2; i++)
        {
            stackRs.Pop();
        }
        
        Assert.True(stackRs.Any);
        stackRs.Push(-1);
        stackRs.Push(-2);

        stackRs.Pop();
        stackRs.Pop();
        stackRs.Pop();
       
        Assert.False(stackRs.GetEnumerator().MoveNext());
    }

    [Test]
    public void TestAddRangeSet()
    {
        Span<int> initStore = stackalloc int[N];
        var stackRs = new StackRs<int>(ref initStore);
        
        Span<int> buckets = stackalloc int[N];
        Span<KeyEntry<int>> entriesHash = stackalloc KeyEntry<int>[N];
        var set = new SetRs<int>(ref buckets, ref entriesHash, EqualityComparer<int>.Default);

        var data = Enumerable.Range(0, N).ToData();
        
        set.AddRange(data);
        
        stackRs.PushRange(ref set);

        var asStack = data.AsStack();

        while (asStack.Any)
        {
            var p1 = asStack.Pop();
            
            var p2 = stackRs.Pop();
            
            Assert.AreEqual(p1, p2);
        }

        try
        {
            stackRs.Pop();
            Assert.Fail();
        }
        catch (IndexOutOfRangeException e)
        {
        }
        
        try
        {
            asStack.Pop();
            Assert.Fail();
        }
        catch (IndexOutOfRangeException e)
        {
        }
    }
    
    [Test]
    public void TestAddRangeDataRs()
    {
        Span<int> initStore = stackalloc int[N];
        var stackRs = new StackRs<int>(ref initStore);
        
        Span<int> initStore1 = stackalloc int[N];
        var dataList = new DataRs<int>(ref initStore1);
        
        var data = Enumerable.Range(0, N).ToData();
        
        dataList.AddRange(data);
        
        stackRs.PushRange(ref dataList);

        var asStack = data.AsStack();

        while (asStack.Any)
        {
            var p1 = asStack.Pop();
            
            var p2 = stackRs.Pop();
            
            Assert.AreEqual(p1, p2);
        }
    }

    [Test]
    public void TestCopyCtr()
    {
        var data = Enumerable.Range(1, N).ToArray();
        var stackRs = new StackRs<int>(data, N);
        
        Assert.AreEqual(N, stackRs.Count);
        
        Assert.AreEqual(N, stackRs.Pop());
    }
}