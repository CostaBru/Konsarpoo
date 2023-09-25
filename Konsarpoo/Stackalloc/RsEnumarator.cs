using System;
using System.Runtime.CompilerServices;

namespace Konsarpoo.Collections.Stackalloc;

/// <summary> Generic stackalloc data structures enumerator. </summary>
/// <typeparam name="T"></typeparam>
public ref struct RsEnumerator<T, V>
{
    private MapRsKeyEnumerator<T, V> m_mapRsEnumerator;
    private SetRs<T>.SetRsEnumerator m_setRsEnumerator;
    private QueueRs<T>.QuRsEnumerator m_quRsEnumerator;
    private StackRs<T>.StackRsEnumerator m_stackRsEnumerator;
    private DataRs<T>.DataRsEnumerator m_dataRsEnumerator;
    private readonly Mode m_mode;
    
    private enum Mode
    {
        List,
        Stack,
        Qu,
        Map,
        Set
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RsEnumerator(DataRs<T>.DataRsEnumerator dataRsEnumerator)
    {
        m_dataRsEnumerator = dataRsEnumerator;
        m_mode = Mode.List;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RsEnumerator(StackRs<T>.StackRsEnumerator stackRsEnumerator)
    {
        m_stackRsEnumerator = stackRsEnumerator;
        m_mode = Mode.Stack;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RsEnumerator(QueueRs<T>.QuRsEnumerator quRsEnumerator)
    {
        m_quRsEnumerator = quRsEnumerator;
        m_mode = Mode.Qu;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RsEnumerator(SetRs<T>.SetRsEnumerator setRsEnumerator)
    {
        m_setRsEnumerator = setRsEnumerator;
        m_mode = Mode.Set;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RsEnumerator(MapRsKeyEnumerator<T, V> mapRsEnumerator)
    {
        m_mapRsEnumerator = mapRsEnumerator;
        m_mode = Mode.Map;
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        switch (m_mode)
        {
            case  Mode.List: return m_dataRsEnumerator.MoveNext(); 
            case  Mode.Set: return m_setRsEnumerator.MoveNext(); 
            case  Mode.Map: return m_mapRsEnumerator.MoveNext(); 
            case  Mode.Stack: return m_stackRsEnumerator.MoveNext(); 
            case  Mode.Qu: return m_quRsEnumerator.MoveNext();
        }

        throw new NotImplementedException();
    }

    public ref T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            switch (m_mode)
            {
                case  Mode.List: return ref m_dataRsEnumerator.Current; 
                case  Mode.Set: return ref m_setRsEnumerator.Current;
                case  Mode.Map: return ref m_mapRsEnumerator.Current;
                case  Mode.Stack: return ref m_stackRsEnumerator.Current;
                case  Mode.Qu: return ref m_quRsEnumerator.Current;
            }
            
            throw new NotImplementedException();
        }
    }
    
    public int Count
    {
        get
        {
            switch (m_mode)
            {
                case Mode.List: return m_dataRsEnumerator.Count;
                case Mode.Set: return m_setRsEnumerator.Count;
                case Mode.Map: return m_mapRsEnumerator.Count;
                case Mode.Stack: return m_stackRsEnumerator.Count;
                case Mode.Qu: return m_quRsEnumerator.Count;
            }

            throw new NotImplementedException();
        }
    }
}