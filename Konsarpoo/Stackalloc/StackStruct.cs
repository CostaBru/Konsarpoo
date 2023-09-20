using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Konsarpoo.Collections.Stackalloc;

public ref struct StackStruct<T>
{
    private Span<T> m_buffer;
    private int m_count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StackStruct(ref Span<T> span)
    {
        m_buffer = span;
        m_count = 0;
    }

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

    public T Peek() => m_buffer[m_count - 1];

    public bool Any => m_count > 0;

    public int Count => m_count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Push(T item)
    {
        if (m_count >= m_buffer.Length)
        {
            return false;
        }

        m_buffer[m_count] = item;
        m_count++;

        return true;
    }

    public bool PushRange(IReadOnlyList<T> items)
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

    public void Clear() => m_count = 0;
}