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
        
        stackRs.PushRange(list);
        
        Assert.False(stackRs.Push(10000));

        list.Reverse();
        
        var le = list.GetEnumerator();
        var de = stackRs.GetEnumerator();

        while (le.MoveNext())
        {
            Assert.True(de.MoveNext());
            
            Assert.AreEqual(le.Current, de.Current);
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
        Assert.True(stackRs.Push(-1));
        Assert.True(stackRs.Push(-2));

        stackRs.Pop();
        stackRs.Pop();
        stackRs.Pop();
       
        Assert.False(stackRs.GetEnumerator().MoveNext());
    }
}