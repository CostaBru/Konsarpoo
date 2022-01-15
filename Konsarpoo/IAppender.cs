namespace Konsarpoo.Collections
{
    /// <summary>
    /// Python list API interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAppender<T>
    {
        void Append(T value);
        
        int Count { get; }
    }
}