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
        /// <summary>
        /// Returns and removes item from the queue.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">If qu is empty.</exception>
        T Dequeue();
        /// <summary>
        /// Returns but does not remove an item from the queue.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">If qu is empty.</exception>
        T Peek();
    }
    
    /// <summary>
    /// Interface to Enqueue elements if FIFO order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEnqueue<T> : IDequeue<T>
    {
        /// <summary>
        /// Adds an element to the queue.
        /// </summary>
        /// <param name="item"></param>
        void Enqueue(T item);
        
        /// <summary>
        ///  Adds all elements in given collection to the queue.
        /// </summary>
        /// <param name="items"></param>
        void EnqueueRange(IEnumerable<T> items);
    }

    /// <summary>
    /// Interface to Enqueue and Dequeue elements if FIFO order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQueue<T> : IDequeue<T>, IEnqueue<T>
    {
        /// <summary>
        /// Clears the queue.
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Gets the flag indicating whether queue has any element.
        /// </summary>
        bool Any { get; }
    }
}
