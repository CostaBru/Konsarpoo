namespace Konsarpoo.Collections
{
    /// <summary>
    /// Python list API interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAppender<T>
    {
        /// <summary>
        /// Adds an element to the collection.
        /// </summary>
        /// <param name="value"></param>
        void Append(T value);
        
        /// <summary>
        /// Returns the count of collection.
        /// </summary>
        int Count { get; }
    }
}