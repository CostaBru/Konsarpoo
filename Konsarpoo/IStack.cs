using System.Collections.Generic;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Pop only interface to get\read from stack in LIFO order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPopStack<T>
    {
        /// <summary>
        /// Gets element and removes it from stack in LIFO order.
        /// <exception cref="InvalidOperationException"></exception>
        /// </summary>
        T Pop();
        
        /// <summary>
        /// Gets element it from stack in LIFO order.
        /// <exception cref="InvalidOperationException"></exception>
        /// </summary>
        T Peek();
    }
    
    /// <summary>
    /// Push only interface to write elements to the stack in LIFO order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPushStack<T>
    {
        /// <summary>
        /// Pushes element to the stack in LIFO order.
        /// </summary>
        /// <param name="value"></param>
        void Push(T value);
        /// <summary>
        /// Pushes elements to the stack in LIFO order.
        /// </summary>
        /// <param name="value"></param>
        void PushRange(IEnumerable<T> value);
    }
    
    /// <summary>
    /// Generic Stack interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IStack<T> : IPopStack<T>, IPushStack<T>
    {
        /// <summary>
        /// Gets the flac indicating whether stack has any item.
        /// </summary>
        bool Any { get; }

        /// <summary>
        /// Clears the stack.
        /// </summary>
        void Clear();
    }
}