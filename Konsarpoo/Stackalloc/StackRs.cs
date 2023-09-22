using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Konsarpoo.Collections.Stackalloc;

[StructLayout(LayoutKind.Auto)]
public ref struct StackRs<T>
{
    private Span<T> m_buffer;
    private int m_count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StackRs(ref Span<T> span)
    {
        m_buffer = span;
        m_count = 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StEnumerator<T> GetEnumerator() => new StEnumerator<T>(m_buffer, m_count);
   
    public ref struct StEnumerator<T>
    {
        private readonly Span<T> m_span;
        private int m_index;

     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal StEnumerator(Span<T> span, int count)
        {
            m_span = span;
            m_index = count;
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
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool PushRange(ref DataRs<T> value)
    {
        if (m_count >= m_buffer.Length)
        {
            return false;
        }

        var valueCount = value.m_count;
        
        if (valueCount + m_count > m_buffer.Length)
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
    public bool PushRange(ref SetRs<T> value)
    {
        if (m_count >= m_buffer.Length)
        {
            return false;
        }

        var valueCount = value.m_count;
        
        if (valueCount + m_count > m_buffer.Length)
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
    public void Clear() => m_count = 0;
}