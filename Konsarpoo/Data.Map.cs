namespace Konsarpoo.Collections;

public partial class Data<T>
{
    /// <summary>
    /// Determines whether the Data contains the specified index.
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public bool ContainsKey(int index)
    {
        if (index < 0 || index >= m_count)
        {
            return false;
        }

        return true;
    }
        
    /// <summary>
    /// Attempts to get the value associated with the specified index.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="value"></param>
    /// <returns>True in case of success.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool TryGetValue(int index, out T value)
    {
        value = default;
            
        if (index < 0 || index >= m_count)
        {
            return false;
        }

        value = ValueByRef(index);

        return true;
    }
}