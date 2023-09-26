using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Konsarpoo.Collections.Stackalloc;

/// <summary>
/// Stack data struct implementation based on generic contiguous memory Span of T.
/// </summary>
/// <typeparam name="T"></typeparam>
[StructLayout(LayoutKind.Auto)]
public ref struct StackRs<T>
{
    private readonly Span<T> m_buffer;
    
    private int m_count;

    /// <summary>
    /// Default constructor that expect the maximum storage capacity it can contain.
    /// </summary>
    /// <param name="span"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StackRs(ref Span<T> span)
    {
        m_buffer = span;
        m_count = 0;
    }
    
    /// <summary>
    /// Constructor that fills out container with predefined data.
    /// </summary>
    /// <param name="array"></param>
    /// <param name="count"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StackRs([NotNull] T[] array, int count)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (count > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        
        m_buffer = new Span<T>(array);
        m_count = count;
    }
    
    /// <summary>
    /// Allows enumerate StackRs contents.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StackRsEnumerator GetEnumerator() => new (m_buffer, m_count);
    
    /// <summary>
    /// Allows to enumerate contents. 
    /// </summary>
    /// <returns></returns>
    public RsEnumerator<T, T> GetRsEnumerator() => new (new StackRsEnumerator(m_buffer, m_count));
   
    /// <summary>
    /// StackRs enumerator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public ref struct StackRsEnumerator
    {
        private readonly Span<T> m_span;
        private readonly int m_count;

        private int m_index;

     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal StackRsEnumerator(Span<T> span, int count)
        {
            m_span = span;
            m_index = count;
            m_count = count;
        }

        /// <summary>Advances the enumerator to the next element of the span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int index = m_index - 1;
            if (index >= 0)
            {
                m_index = index;
                return true;
            }

            return false;
        }

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_span[m_index];
        }

        public int Count => m_count;
    }

    /// <summary>
    /// Gets element and removes it from stack in LIFO order.
    /// <exception cref="IndexOutOfRangeException"></exception>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Pop()
    {
        var val = m_buffer[m_count - 1];

        m_buffer[m_count - 1] = default;

        m_count--;

        if (Count == 0)
        {
            Clear();
        }

        return val;
    }

    /// <summary>
    /// Gets element it from stack in LIFO order.
    /// <exception cref="IndexOutOfRangeException">If stack is empty.</exception>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Peek() => m_buffer[m_count - 1];

    /// <summary>
    /// Gets the flag indicating whether stack has any item.
    /// </summary>
    public bool Any => m_count > 0;

    /// <summary>
    /// Returns number of elements is stack.
    /// </summary>
    public int Count => m_count;

    /// <summary>
    /// Pushes element to the stack in LIFO order.
    /// </summary>
    /// <param name="item"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(T item)
    {
        if (m_count >= m_buffer.Length)
        {
            throw new InsufficientMemoryException($"Cannot push a new item to the StackRs. The {m_buffer.Length} maximum reached.");
        }

        m_buffer[m_count] = item;
        m_count++;
    }
    
    /// <summary>
    /// Pushes a range of elements to the stack in LIFO order.
    /// </summary>
    /// <param name="items"></param>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushRange(IReadOnlyList<T> items)
    {
        CheckCanPush(items.Count);

        foreach (var v in items)
        {
            m_buffer[m_count] = v;
            m_count++;
        }
    }

    private void CheckCanPush(int count)
    {
        if (m_count >= m_buffer.Length)
        {
            throw new InsufficientMemoryException($"Cannot push a new item to the StackRs. The {m_buffer.Length} maximum reached.");
        }


        if (count + m_count > m_buffer.Length)
        {
            throw new InsufficientMemoryException($"Cannot enqueue the {count} of new items to the StackRs. The {m_buffer.Length} is a maximum.");
        }
    }

    /// <summary>
    /// Pushes a range of elements to the stack in LIFO order.
    /// </summary>
    /// <param name="items"></param>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushRange(ref DataRs<T> items)
    {
        CheckCanPush(items.Count);
        
        for (int i = 0; i < items.m_count; i++)
        {
            m_buffer[m_count] = items.m_buffer[i];
            m_count++;
        }
    }
    
    /// <summary>
    /// Pushes a range of elements to the stack in LIFO order.
    /// </summary>
    /// <param name="items"></param>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushRange(ref SetRs<T> items)
    {
        CheckCanPush(items.Count);
        
        var index = 0;

        while (index < items.m_count)
        {
            if (items.m_entries[index].HashCode >= 0)
            {
                m_buffer[m_count] = items.m_entries[index].Key;
                m_count++;
            }

            index++;
        }
    }

    /// <summary>
    /// Clears the stack.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => m_count = 0;
}