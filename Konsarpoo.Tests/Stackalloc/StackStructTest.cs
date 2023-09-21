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
}