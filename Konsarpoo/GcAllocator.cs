namespace Konsarpoo.Collections;

public class GcAllocator<T> : IArrayPool<T>
{
    public T[] Rent(int count)
    {
        return new T[count];
    }

    public void Return(T[] array, bool clearArray = false)
    {
    }

    public bool CleanArrayReturn => true;
}