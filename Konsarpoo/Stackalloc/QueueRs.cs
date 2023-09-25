using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Konsarpoo.Collections.Stackalloc;

/// <summary>
/// Default Queue implementation based on stack allocation. 
/// </summary>
/// <typeparam name="T"></typeparam>
[StructLayout(LayoutKind.Auto)]
public ref struct QueueRs<T>
{
    internal int m_startOffset;

    private Span<T> m_buffer;
    private int m_count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QueueRs(ref Span<T> span)
    {
        m_buffer = span;
        m_count = 0;
    }
    
    /// <summary>
    /// Allows to enumerate contents. 
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QuRsEnumerator GetEnumerator() => new(m_buffer, m_count, m_startOffset);
    
    /// <summary>
    /// Allows to enumerate contents. 
    /// </summary>
    /// <returns></returns>
    public RsEnumerator<T, T> GetRsEnumerator() => new (new QuRsEnumerator(m_buffer, m_count, m_startOffset));
    
    /// <summary>
    ///  Queue enumerator.
    /// </summary>
    public ref struct QuRsEnumerator
    {
        private readonly Span<T> m_span;
        private readonly int m_count;
        private readonly int m_startOffset;
        private int m_index;

     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal QuRsEnumerator(Span<T> span, int count, int offset)
        {
            m_span = span;
            m_count = count;
            m_index = offset - 1;
            m_startOffset = offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int index = m_index + 1;
            if (index < m_count)
            {
                m_index = index;
                return true;
            }

            return false;
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_span[m_index];
        }

        public int Count => m_count - m_startOffset;
    }
   
    internal void ResetTail()
    {
        if (m_count == 0 || m_startOffset == 0)
        {
            return;
        }
        
        var slice = m_buffer.Slice(m_startOffset);
        
        slice.CopyTo(m_buffer);

        m_count = Count;
        
        m_startOffset = 0;
    }

    /// <summary>
    /// Returns and removes item from the queue.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Dequeue()
    {
        var val = m_buffer[m_startOffset];

        m_buffer[m_startOffset] = default;

        m_startOffset++;

        if (Count == 0)
        {
            Clear();
        }

        return val;
    }

    /// <summary>
    /// Returns but does not remove an item from the queue.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Peek() => m_buffer[m_startOffset];

    /// <summary>
    /// Gets the flag indicating whether queue has any element.
    /// </summary>
    public bool Any => m_count - m_startOffset > 0;

    /// <summary>
    /// Gets the number of items in queue.
    /// </summary>
    public int Count => m_count - m_startOffset;

    /// <summary>
    /// Adds an element to the queue.
    /// </summary>
    /// <param name="item"></param>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(T item)
    {
        if (m_count >= m_buffer.Length - m_startOffset)
        {
            ResetTail();

            if (m_count >= m_buffer.Length - m_startOffset)
            {
                throw new InsufficientMemoryException($"Cannot enqueue a new item to the QueueRs. The {m_buffer.Length} maximum reached.");
            }
        }

        m_buffer[m_count] = item;
        m_count++;
    }

    /// <summary>
    ///  Adds all elements in given collection to the queue.
    /// </summary>
    /// <param name="items"></param>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnqueueRange(IReadOnlyList<T> items)
    {
        CheckCanEnqueue(items.Count);

        foreach (var v in items)
        {
            m_buffer[m_count] = v;
            m_count++;
        }
    }
    
    /// <summary>
    ///  Adds all elements in given collection to the queue.
    /// </summary>
    /// <param name="items"></param>
    /// <exception cref="InsufficientMemoryException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnqueueRange(ref DataRs<T> value)
    {
        var count = value.m_count;

        CheckCanEnqueue(count);

        for (int i = 0; i < count; i++)
        {
            m_buffer[m_count] = value.m_buffer[i];
            m_count++;
        }
    }

    private void CheckCanEnqueue(int count)
    {
        if (m_count >= m_buffer.Length - m_startOffset)
        {
            ResetTail();

            if (m_count >= m_buffer.Length - m_startOffset)
            {
                throw new InsufficientMemoryException($"Cannot enqueue a new item to the QueueRs. The {m_buffer.Length} maximum reached.");
            }
        }

        var valueCount = count;

        if (valueCount + m_count > m_buffer.Length - m_startOffset)
        {
            ResetTail();

            if (m_count >= m_buffer.Length - m_startOffset)
            {
                throw new InsufficientMemoryException($"Cannot enqueue the {valueCount} of new items to the QueueRs. The {m_buffer.Length} is a maximum.");
            }
        }
    }

    /// <summary>
    /// Adds all elements in given collection to the queue.
    /// </summary>
    /// <param name="items"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EnqueueRange(ref SetRs<T> value)
    {
        CheckCanEnqueue(value.m_count);
        
        var index = 0;

        while (index < value.m_count)
        {
            if (value.m_entries[index].HashCode >= 0)
            {
                m_buffer[m_count] = value.m_entries[index].Key;
                m_count++;
            }

            index++;
        }

        return true;
    }

    /// <summary>
    /// Clears the queue.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        m_count = 0;

        m_startOffset = 0;
    }
}