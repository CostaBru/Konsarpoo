using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Konsarpoo.Collections.Stackalloc;

[StructLayout(LayoutKind.Auto)]
public ref struct DataStruct<T>
{
    private Span<T> m_buffer;
    private int m_count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataStruct(ref Span<T> span)
    {
        m_buffer = span;
        m_count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(ref T value)
    {
        if (m_count >= m_buffer.Length)
        {
            return false;
        }
        
        m_buffer[m_count] = value;
        m_count++;

        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddRange(IReadOnlyList<T> value)
    {
        if (m_count >= m_buffer.Length)
        {
            return false;
        }

        var valueCount = value.Count;
        
        if (valueCount + m_count >= m_buffer.Length)
        {
            return false;
        }

        foreach (var v in value)
        {
            m_buffer[m_count] = v;
            m_count++;
        }

        return true;
    }

    public void Values(Action<T> onValue)
    {
        for (int i = 0; i < m_count; i++)
        {
            onValue(m_buffer[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Ensure(int newCount, T value = default)
    {
        var currentCount = m_count;
        if (newCount == currentCount)
        {
            return true;
        }
        
        if (newCount >= m_buffer.Length)
        {
            return false;
        }

        for (int i = currentCount; i < newCount; i++)
        {
            m_buffer[i] = value;
        }
        
        return true;
    }

    public ref T this[int index] => ref m_buffer[index];

    public int Count => m_count;
    public int Length => m_count;
}