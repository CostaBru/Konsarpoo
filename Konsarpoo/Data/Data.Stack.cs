using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Konsarpoo.Collections
{
    public partial class Data<T> 
    {
        /// <summary>
        /// Stack API to pop an element from the data stack. Stack removes this element from after the call.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            var value = this[Count - 1];
            
            RemoveLast();

            return value;
        }
       
        /// <summary>
        /// Stack API to peek an element from the data stack without removing it from the stack.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            return this[Count - 1];
        }

        /// <summary>
        /// Stack and Qu API. Gets flags indicating whether any element in container.
        /// </summary>
        public bool Any => m_count > 0;

        /// <summary>
        /// Stack API to push an element to the data stack. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T value)
        {
            Add(value);
        }
        
        /// <summary>
        /// Stack API to push the list of elements to the data stack. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushRange(IEnumerable<T> value)
        {
            AddRange(value);
        }
    }
}