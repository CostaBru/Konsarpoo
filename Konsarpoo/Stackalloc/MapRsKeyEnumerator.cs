using System;
using System.Runtime.CompilerServices;

namespace Konsarpoo.Collections.Stackalloc;

/// <summary>
/// MapRs keys enumerator.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public ref struct MapRsKeyEnumerator<TKey, TValue>
{
    private readonly Span<MapRs<TKey, TValue>.Entry> m_entries;
        
    private readonly int m_count;
    private int m_index = -1;

    public MapRsKeyEnumerator(Span<MapRs<TKey,TValue>.Entry> entries, int count)
    {
        m_entries = entries;
        m_count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        var index = m_index + 1;
            
        while (index < m_count)
        {
            if (m_entries[index].Key.HashCode >= 0)
            {
                m_index = index;

                return true;
            }

            index++;
        }

        return false;
    }

    public ref TKey Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref m_entries[m_index].Key.Key;
    }

    public int Count => m_count;
}