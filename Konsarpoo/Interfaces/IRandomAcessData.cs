using System;
using System.Collections;
using System.Collections.Generic;

namespace Konsarpoo.Collections;

/// <summary>
/// Basic interface for random access data structure.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IRandomAccessData<T> : IList<T>, IAppender<T>, IReadOnlyList<T>
{
    /// <summary>
    /// Returns the count of collection.
    /// </summary>
    new int Count { get; }

    /// <summary>Gets or sets the element at the specified index.</summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1" />.</exception>
    /// <exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1" /> is read-only.</exception>
    /// <returns>The element at the specified index.</returns>
    new T this[int index] { get; set; }

    /// <summary>Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.</summary>
    /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
    /// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
    new int IndexOf(T item);

    /// <summary>Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.</summary>
    /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
    /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1" />.</exception>
    /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1" /> is read-only.</exception>
    new void Insert(int index, T item);

    /// <summary>Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.</summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1" />.</exception>
    /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1" /> is read-only.</exception>
    new void RemoveAt(int index);

    /// <summary>
    /// Array API. Ensures that current IRandomAccessData&lt;T&gt; container has given size.
    /// </summary>
    /// <param name="size"></param>
    void Ensure(int size);

    /// <summary>
    /// Array API. Ensures that current IRandomAccessData&lt;T&gt; container has given size.
    /// </summary>
    /// <param name="size"></param>
    /// <param name="defaultValue"></param>
    void Ensure(int size, T defaultValue);

    /// <summary>
    /// List API. Adds the elements of the specified collection to the end of the IRandomAccessData&lt;T&gt;.
    /// </summary>
    /// <param name="items"></param>
    /// <exception cref="ArgumentNullException"></exception>
    void AddRange(IEnumerable<T> items);

    /// <summary>
    /// List API. Clears IRandomAccessData&lt;T&gt;.
    /// </summary>
    new void Clear();

    /// <summary>
    /// Sorts the elements in the IRandomAccessData&lt;T&gt; using default IComparer&lt;T&gt; implementation. 
    /// </summary>
    void Sort();


    /// <summary>
    /// Sorts the elements in the IRandomAccessData&lt;T&gt; using the provided IComparer&lt;T&gt; delegate to compare data elements. 
    /// </summary>
    void Sort(IComparer<T> comparer);

    /// <summary>
    /// Sorts the elements in the IRandomAccessData&lt;T&gt; using the provided Comparison&lt;T&gt; delegate to compare data elements. 
    /// </summary>
    void Sort(Comparison<T> comparison);

    /// <summary>
    /// Array and List API. Searches the entire sorted IRandomAccessData&lt;T&gt; for an element using the default comparer and returns the zero-based index of the element.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="startIndex"></param>
    /// <param name="count"></param>
    /// <param name="comparer"></param>
    /// <returns>The zero-based index of item in the sorted Data&lt;T&gt;, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of Count.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    int BinarySearch(T value, int startIndex, int count, IComparer<T> comparer = null);
}