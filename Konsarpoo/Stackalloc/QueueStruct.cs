using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Konsarpoo.Collections.Stackalloc;

public ref struct QuStruct<T>
{
    private readonly Data<T> m_list;

    private int m_startOffset;

    private Span<T> m_buffer;
    private int m_count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QuStruct(ref Span<T> span)
    {
        m_buffer = span;
        m_count = 0;
    }

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

    public T Peek() => m_buffer[m_startOffset];

    public bool Any => m_count - m_startOffset > 0;

    public int Count => m_count - m_startOffset;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Enqueue(T item)
    {
        if (m_count >= m_buffer.Length)
        {
            return false;
        }

        m_buffer[m_count] = item;
        m_count++;

        return true;
    }

    public bool EnqueueRange(IReadOnlyList<T> items)
    {
        if (m_count >= m_buffer.Length)
        {
            return false;
        }

        var valueCount = items.Count;
        
        if (valueCount + m_count > m_buffer.Length)
        {
            return false;
        }

        foreach (var v in items)
        {
            m_buffer[m_count] = v;
            m_count++;
        }

        return true;
    }

    public void Clear()
    {
        m_count = 0;

        m_startOffset = 0;
    }
}