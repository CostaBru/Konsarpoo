using System;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Array allocator interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IArrayPool<T>
    {
        /// <summary>
        /// Gets new array with size greater or equal to given count.
        /// </summary>
        /// <param name="count"></param>
        [NotNull]
        T[] Rent(int count);

        /// <summary>
        /// Recycles array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="clearArray"></param>
        /// <exception cref="ArgumentNullException"></exception>
        void Return([NotNull] T[] array, bool clearArray = false);

        /// <summary>
        /// Flag indicating weather an array returned was cleared.
        /// </summary>
        bool CleanArrayReturn { get; }
    }
}