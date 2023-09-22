using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Konsarpoo.Collections.Stackalloc;

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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QuRsEnumerator GetEnumerator() => new QuRsEnumerator(m_buffer, m_count, m_startOffset);
    
    public ref struct QuRsEnumerator
    {
        private readonly Span<T> m_span;
        private readonly int m_count;
        private int m_index;

     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal QuRsEnumerator(Span<T> span, int count, int offset)
        {
            m_span = span;
            m_count = count;
            m_index = offset - 1;
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
    }

    public void ResetTail()
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Peek() => m_buffer[m_startOffset];

    public bool Any => m_count - m_startOffset > 0;

    public int Count => m_count - m_startOffset;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Enqueue(T item)
    {
        if (m_count >= m_buffer.Length - m_startOffset)
        {
            ResetTail();

            if (m_count >= m_buffer.Length - m_startOffset)
            {
                return false;
            }
        }

        m_buffer[m_count] = item;
        m_count++;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EnqueueRange(IReadOnlyList<T> items)
    {
        if (m_count >= m_buffer.Length - m_startOffset)
        {
            ResetTail();

            if (m_count >= m_buffer.Length - m_startOffset)
            {
                return false;
            }
            
            return false;
        }

        var valueCount = items.Count;
        
        if (valueCount + m_count > m_buffer.Length - m_startOffset)
        {
            ResetTail();

            if (m_count >= m_buffer.Length - m_startOffset)
            {
                return false;
            }
            
            return false;
        }

        foreach (var v in items)
        {
            m_buffer[m_count] = v;
            m_count++;
        }

        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EnqueueRange(ref DataRs<T> value)
    {
        if (m_count >= m_buffer.Length - m_startOffset)
        {
            return false;
        }

        var valueCount = value.m_count;
        
        if (valueCount + m_count > m_buffer.Length - m_startOffset)
        {
            return false;
        }
        
        for (int i = 0; i < value.m_count; i++)
        {
            m_buffer[m_count] = value.m_buffer[i];
            m_count++;
        }

        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EnqueueRange(ref SetRs<T> value)
    {
        if (m_count >= m_buffer.Length - m_startOffset)
        {
            return false;
        }

        var valueCount = value.m_count;
        
        if (valueCount + m_count > m_buffer.Length - m_startOffset)
        {
            return false;
        }
        
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        m_count = 0;

        m_startOffset = 0;
    }
}