using System;
using System.Collections.Generic;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Interface to dequeue elements in FIFO order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDequeue<T>
    {
        T Dequeue();
        T Peek();
    }
    
    /// <summary>
    /// Interface to Enqueue elements if FIFO order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEnqueue<T> : IDequeue<T>
    {
        void Enqueue(T item);
        void EnqueueRange(IEnumerable<T> items);
    }

    /// <summary>
    /// Interface to Enqueue and Dequeue elements if FIFO order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQueue<T> : IDequeue<T>, IEnqueue<T>
    {
        void Clear();
        
        bool Any { get; }
    }
}
