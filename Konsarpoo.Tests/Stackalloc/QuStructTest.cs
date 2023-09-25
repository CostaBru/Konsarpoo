using System;
using System.Collections.Generic;
using System.Linq;
using Konsarpoo.Collections.Stackalloc;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests.Stackalloc;

[TestFixture(16)]
[TestFixture(32)]
[TestFixture(1000)]
public class QuStructTest
{
    public int N { get; set; }
        
    public QuStructTest(int capacity)
    {
        N = capacity;
    }
    
    [Test]
    public void TestQueue()
    {
        Span<int> initStore = stackalloc int[N];
        var qu = new QueueRs<int>(ref initStore);
        
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
    
    [Test]
    public void TestEnumeration()
    {
        var list = Enumerable.Range(0, N).ToList();
        Span<int> initStore = stackalloc int[N];
        var queueRs = new QueueRs<int>(ref initStore);
        
        Assert.False(queueRs.GetEnumerator().MoveNext());
        Assert.False(queueRs.GetRsEnumerator().MoveNext());
        Assert.AreEqual(0, queueRs.GetRsEnumerator().Count);
        
        queueRs.EnqueueRange(list);

        try
        {
            queueRs.Enqueue(10000);
            Assert.Fail();
        }
        catch(InsufficientMemoryException)
        {
        }

        var le = list.GetEnumerator();
        var de = queueRs.GetEnumerator();
        var dre = queueRs.GetRsEnumerator();
        Assert.AreEqual(list.Count, dre.Count);
        
        while (le.MoveNext())
        {
            Assert.True(de.MoveNext());
            Assert.True(dre.MoveNext());
            
            Assert.AreEqual(le.Current, de.Current);
            Assert.AreEqual(le.Current, dre.Current);
        }

        queueRs.Dequeue();
        list.Remove(0);
        
        le = list.GetEnumerator();
        de = queueRs.GetEnumerator();

        while (le.MoveNext())
        {
            Assert.True(de.MoveNext());
            
            Assert.AreEqual(le.Current, de.Current);
        }

        for (int i = 0; i < N - 2; i++)
        {
            queueRs.Dequeue();
        }
        
        Assert.True(queueRs.Any);
       queueRs.Enqueue(-1);

        queueRs.Dequeue();
        
        Assert.True(queueRs.Any);
        Assert.AreNotEqual(0, queueRs.m_startOffset);
        
        queueRs.ResetTail();
        
        Assert.True(queueRs.Any);
        Assert.AreEqual(0, queueRs.m_startOffset);
        Assert.AreEqual(-1, queueRs.Peek());
        
        Assert.AreEqual(-1, queueRs.Dequeue());
        
        Assert.False(queueRs.GetEnumerator().MoveNext());
    }

    [Test]
    public void TestAddRangeSet()
    {
        Span<int> initStore = stackalloc int[N];
        var queueRs = new QueueRs<int>(ref initStore);
        
        Span<int> buckets = stackalloc int[N];
        Span<KeyEntryStruct<int>> entriesHash = stackalloc KeyEntryStruct<int>[N];
        var set = new SetRs<int>(ref buckets, ref entriesHash, EqualityComparer<int>.Default);

        var readOnlyList = Enumerable.Range(0, N / 2).ToData();

        set.AddRange(readOnlyList);
        
        queueRs.EnqueueRange(ref set);

        var asQueue = readOnlyList.AsQueue();

        while (asQueue.Any)
        {
            var dq1 = asQueue.Dequeue();
            var dq2 = queueRs.Dequeue();
            
            Assert.AreEqual(dq1, dq2);
        }
    }
    
    [Test]
    public void TestAddRangeData()
    {
        Span<int> initStore = stackalloc int[N];
        var queueRs = new QueueRs<int>(ref initStore);
        
        Span<int> initStore1 = stackalloc int[N];
        var dataRs = new DataRs<int>(ref initStore1);

        var readOnlyList = Enumerable.Range(0, N / 2).ToData();

        dataRs.AddRange(readOnlyList);
        
        queueRs.EnqueueRange(ref dataRs);

        var asQueue = readOnlyList.AsQueue();

        while (asQueue.Any)
        {
            var dq1 = asQueue.Dequeue();
            var dq2 = queueRs.Dequeue();
            
            Assert.AreEqual(dq1, dq2);
        }
    }
}