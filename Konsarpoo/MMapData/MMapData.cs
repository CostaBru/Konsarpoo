using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Konsarpoo.Collections.MMapData;

public class MMapData<T> : Data<T>
{
    private readonly string m_filename;
    private MemoryMappedFile _mmf;
    private MemoryMappedViewStream  _accessor;

    private int m_edit = 0;
    
    public MMapData(string filename)
    {
        m_filename = filename;
    }
    
    public bool IsInEdit => m_edit > 0;

    public void Load()
    {
    }

    public void BeginEdit()
    {
        m_edit++;
    }

    public void EndEdit()
    {
        m_edit--;

        if (m_edit < 0)
        {
            m_edit = 0;
        }
    }
}