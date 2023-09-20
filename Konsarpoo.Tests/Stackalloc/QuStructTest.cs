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
        var qu = new QuStruct<int>(ref initStore);
        
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
}