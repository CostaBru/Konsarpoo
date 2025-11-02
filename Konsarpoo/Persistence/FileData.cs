using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Data.Serialization;

namespace Konsarpoo.Collections.Persistence;

/// <summary>
/// A file-backed data structure that provides IReadOnlyList&lt;T&gt; and IRandomAccessData&lt;T&gt; interface with on-demand loading and unloading of data chunks.
/// Uses ICacheStore for memory management and DataFileSerialization for persistence. Not thread safe.
/// </summary>
/// <typeparam name="T">The type of elements stored in the collection</typeparam>
[DebuggerDisplay("Count {m_count}")]
[DebuggerTypeProxy(typeof(ReadonlyListDebugView<>))]
public partial class FileData<T> : IReadOnlyList<T>, IDisposable, IAppender<T>, IRandomAccessData<T>
{
    public class ArrayChunk
    {
        public T[] Array;
        public int Size;
        public bool IsModified;
    }
    
    private readonly DataFileSerialization m_fileSerialization;
    private readonly Action<ArrayChunk> m_disposeChunk;
    private readonly Func<ArrayChunk, bool> m_isModifiedChunk;
    private readonly ICacheStore<int, ArrayChunk> m_buffer;
    private readonly IArrayAllocator<T> m_arrayAllocator;
    
    private readonly int m_stepBase;
    private readonly int m_maxSizeOfArray;
    private int m_count;
    private int m_arrayCount;
    private bool m_disposed;
    private int m_writeNestingLevel;
    private int m_version;
    
    public static FileData<T> Create(string filePath, int maxSizeOfArray, byte[] key = null, CompressionLevel compressionLevel = CompressionLevel.NoCompression, int arrayBufferCapacity = 10, IArrayAllocator<T> allocator = null, ICacheStore<T, ArrayChunk > cacheStore = null, Action<ArrayChunk> disposeChunk = null, Func<ArrayChunk, bool> modifiedChunkCheck = null)
    {
        return new FileData<T>(filePath, maxSizeOfArray, FileMode.CreateNew, compressionLevel, arrayBufferCapacity, key, allocator, disposeChunk: disposeChunk, isModifiedChunkCheck: modifiedChunkCheck);
    }
    
    public static FileData<T> Open(string filePath, byte[] key = null, CompressionLevel compressionLevel = CompressionLevel.NoCompression, int arrayBufferCapacity = 10, IArrayAllocator<T> allocator = null, ICacheStore<T, ArrayChunk > cacheStore = null, Action<ArrayChunk> disposeChunk = null, Func<ArrayChunk, bool> modifiedChunkCheck = null)
    {
        return new FileData<T>(filePath, 0, FileMode.Open, compressionLevel, arrayBufferCapacity, key, allocator, disposeChunk: disposeChunk, isModifiedChunkCheck: modifiedChunkCheck);
    }
    
    public static FileData<T> OpenOrCreate(string filePath, int maxSizeOfArray, byte[] key = null, CompressionLevel compressionLevel = CompressionLevel.NoCompression, int arrayBufferCapacity = 10, IArrayAllocator<T> allocator = null, ICacheStore<T, ArrayChunk > cacheStore = null, Action<ArrayChunk> disposeChunk = null, Func<ArrayChunk, bool> modifiedChunkCheck = null)
    {
        return new FileData<T>(filePath, maxSizeOfArray, FileMode.OpenOrCreate, compressionLevel, arrayBufferCapacity, key, allocator, disposeChunk: disposeChunk, isModifiedChunkCheck: modifiedChunkCheck);
    }
    
    private static DataFileSerialization PrepareConstructorArgs(string filePath, int maxSizeOfArray, FileMode fileMode, CompressionLevel compressionLevel, byte[] cryptoKey)
    {
        // Normalize OpenOrCreate into Open or Create based on metadata file existence
        var effectiveMode = fileMode == FileMode.OpenOrCreate
            ? (File.Exists(filePath) ? FileMode.Open : FileMode.Create)
            : fileMode;

        DataFileSerialization serialization = effectiveMode == FileMode.Open
            ? new DataFileSerialization(filePath, effectiveMode, cryptoKey, compressionLevel)
            : new DataFileSerialization(filePath, effectiveMode, cryptoKey, compressionLevel, Math.Max(1, maxSizeOfArray).PowerOfTwo());

        return serialization;
    }

    private FileData(string filePath, 
        int maxSizeOfArray,
        FileMode fileMode,
        CompressionLevel compressionLevel, 
        int arrayBufferCapacity,
        byte[] cryptoKey, 
        IArrayAllocator<T> allocator = null,
        ICacheStore<int, ArrayChunk> cacheStore = null,
        Action<ArrayChunk> disposeChunk = null,
        Func<ArrayChunk, bool> isModifiedChunkCheck = null)
        : 
        this(PrepareConstructorArgs(filePath, maxSizeOfArray, fileMode, compressionLevel, cryptoKey),
            arrayBufferCapacity, 
            allocator, 
            cacheStore,
            disposeChunk,
            isModifiedChunkCheck)
    {
    }

    public FileData([NotNull] DataFileSerialization fileSerialization,
        int arrayBufferCapacity,
        IArrayAllocator<T> allocator = null, 
        ICacheStore<int, ArrayChunk> cacheStore = null,
        Action<ArrayChunk> disposeChunk = null,
        Func<ArrayChunk, bool> isModifiedChunkCheck = null)
    {
        m_fileSerialization = fileSerialization ?? throw new ArgumentNullException(nameof(fileSerialization));
        m_disposeChunk = disposeChunk;
        m_isModifiedChunk = isModifiedChunkCheck;
        
        m_maxSizeOfArray = m_fileSerialization.MaxSizeOfArray;
        m_count = m_fileSerialization.DataCount;
        m_arrayCount = m_fileSerialization.ArrayCount;
        m_version = m_fileSerialization.Version;

        m_stepBase = GetStepBase(m_maxSizeOfArray);

        var defaultAllocatorSetup = KonsarpooAllocatorGlobalSetup.DefaultAllocatorSetup;

        m_arrayAllocator = allocator ?? defaultAllocatorSetup.GetDataStorageAllocator<T>().GetDataArrayAllocator();

        if (cacheStore != null)
        {
            m_buffer = cacheStore;
        }
        else
        {
            var buffer = new LfuCache<int, ArrayChunk>(0, 0, null, disposingStrategy: OnChunkDone);

            buffer.StartTrackingMemory(arrayBufferCapacity, (key, chunk) => 1);

            m_buffer = buffer;
        }
    }

    /// <summary>
    /// Gets the file path associated with this FileData instance.
    /// </summary>
    public string FilePath => m_fileSerialization.FilePath;
    
    /// <summary>
    /// Gets the version of the collection, which increments with collection count change.
    /// </summary>
    public int Version => m_version;

    /// <summary>
    /// Gets the maximum size of each internal array chunk.
    /// </summary>
    public int MaxSizeOfArray => m_maxSizeOfArray;
    
    private static int GetStepBase(int maxSizeOfArray)
    {
        if (maxSizeOfArray <= 1)
        {
            return 0;
        }
        // compute floor(log2(maxSizeOfArray)) using integer operations to avoid floating-point precision issues
        int step = 0;
        while ((1 << (step + 1)) <= maxSizeOfArray)
        {
            step++;
        }
        return step;
    }
    

    private void OnChunkDone(int arrayIndex, ArrayChunk chunk)
    {
        if (arrayIndex < m_fileSerialization.ArrayCount && (chunk.IsModified || (m_isModifiedChunk?.Invoke(chunk) ?? false)))
        {
            m_fileSerialization.WriteArray(arrayIndex, chunk.Array);
            chunk.IsModified = false;
        }

        m_disposeChunk?.Invoke(chunk);

        if (chunk.Size > 0)
        {
            Array.Clear(chunk.Array, 0, chunk.Size);
        }
            
        m_arrayAllocator.Return(chunk.Array);
    }

    /// <summary>
    /// Begins a write transaction. Multiple writes can be batched until EndWrite is called.
    /// </summary>
    public void BeginWrite()
    {
        m_writeNestingLevel++;
        m_fileSerialization.BeginWrite();
    }

    /// <summary>
    /// Ends a write transaction and flushes any pending changes to disk.
    /// </summary>
    public void EndWrite()
    {
        m_writeNestingLevel--;
        
        if (m_writeNestingLevel <= 0)
        {
            m_writeNestingLevel = 0;
            FlushModifiedChunks();
        }
        
        m_fileSerialization.EndWrite();
    }
    
    private void FlushModifiedChunks()
    {
        m_fileSerialization.UpdateMetadata((m_maxSizeOfArray, m_count, m_version));
        
        m_fileSerialization.WriteMetadata();
        
        foreach (var kvp in m_buffer.OrderBy(k => k.Key))
        {
            if (kvp.Value.IsModified || (m_isModifiedChunk?.Invoke(kvp.Value) ?? false))
            {
                WriteChunkToFile(kvp.Key, kvp.Value);
                kvp.Value.IsModified = false;
            }
        }
    }

    private ArrayChunk GetOrAddChunk(int arrayIndex)
    {
        var existingChunk = LoadChuck(arrayIndex);
        if (existingChunk != null)
        {
            return existingChunk;
        }

        var newArray = m_arrayAllocator.Rent(m_maxSizeOfArray);
      
        var newChunk = NewChunkInstance();
        
        newChunk.Array = newArray;
        newChunk.Size = 0;
        
        m_fileSerialization.AppendArray(newArray);
        m_buffer.AddOrUpdate(arrayIndex, newChunk, OnChunkDone);

        m_arrayCount++;
        
        return newChunk;
    }

    protected virtual ArrayChunk NewChunkInstance()
    {
        return new ArrayChunk();
    }

    private ArrayChunk LoadChuck(int arrayIndex)
    {
        if (m_buffer.TryGetValue(arrayIndex, out var cachedChunk))
        {
            return cachedChunk;
        }

        if (arrayIndex < m_arrayCount)
        {
            var loadedArray = m_fileSerialization.ReadArray<T>(arrayIndex);
            
            int size;
            if (m_arrayCount == 0)
            {
                size = 0;
            }
            else if (arrayIndex == m_arrayCount - 1)
            {
                // size of last chunk = total count - full chunks * capacity
                size = m_count - ((m_arrayCount - 1) * m_maxSizeOfArray);
                if (size == 0 && m_count > 0)
                {
                    size = m_maxSizeOfArray; // last chunk is full
                }
            }
            else
            {
                size = Math.Min(loadedArray.Length, m_maxSizeOfArray);
            }
            
            var chunk = NewChunkInstance();

            chunk.Array = loadedArray;
            chunk.Size = size;
            
            m_buffer.AddOrUpdate(arrayIndex, chunk, OnChunkDone);
            
            return chunk;
        }

        return null;
    }

    private void WriteChunkToFile(int arrayIndex, ArrayChunk chunk)
    {
        if (chunk.Size == 0)
        {
            return;
        }

        m_fileSerialization.WriteArray(arrayIndex, chunk.Array);
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= m_count)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range. Count is {m_count}.");
            }
            
            var arrayIndex = index >> m_stepBase;
            var elementIndex = index - (arrayIndex << m_stepBase);
            
            var chunk = LoadChuck(arrayIndex);
            return chunk.Array[elementIndex];
        }
        set
        {
            if (index < 0 || index >= m_count)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range. Count is {m_count}.");
            }

            var arrayIndex = index >> m_stepBase;
            var elementIndex = index - (arrayIndex << m_stepBase);

            var chunk = LoadChuck(arrayIndex);

            chunk.Array[elementIndex] = value;
            chunk.IsModified = true;
        }
    }

    /// <inheritdoc />
    public void Append(T value)
    {
        Add(value);
    }

    /// <summary>
    /// Gets the number of elements in the collection.
    /// </summary>
    public int Count => m_count;
    
    /// <summary>
    /// Adds a collection of elements to the end of the collection.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        
        BeginWrite();
        
        foreach (var item in items)
        {
            Add(item);
        }
        
        EndWrite();
    }

    /// <summary>
    /// Adds an element to the end of the collection.
    /// </summary>
    public void Add(T item)
    {
        var arrayIndex = m_count >> m_stepBase;
        
        var chunk = GetOrAddChunk(arrayIndex);

        chunk.Array[chunk.Size] = item;
        chunk.IsModified = true;
        chunk.Size++;

        m_version++;
        m_count++;  
    }
    
    /// <summary>
    /// List API. Reverses the order of the elements in the entire Data&lt;T&gt;.
    /// </summary>
    public void Reverse()
    {
        m_version++;
        
        if (m_arrayCount == 1)
        {
            var chunk = GetOrAddChunk(0);

            Array.Reverse(chunk.Array, 0, m_count);

            chunk.IsModified = true;
            
            return;
        }

        ReverseSlow();
    }

    private void ReverseSlow()
    {
        for(int i = 0; i < m_count / 2; i++)
        {
            T temp = this[i];
            var t1 = this[m_count - i - 1];
            this[i] = t1;
            this[m_count - i - 1] = temp;
        }
    }

    /// <summary>
    /// Array API. Ensures that current FileData&lt;T&gt; container has given size.
    /// </summary>
    /// <param name="size"></param>
    public void Ensure(int size)
    {
        Ensure(size, default(T));
    }

    /// <summary>
    /// Array API. Ensures that current FileData&lt;T&gt; container has given size.
    /// </summary>
    /// <param name="size"></param>
    /// <param name="defaultValue">default value</param>
    public void Ensure(int size, T defaultValue)
    {
        if(m_count >= size)
        {
            return;
        }

        var restSize = size - m_count;

        m_version++;

        var currentArrayIndex = m_arrayCount - 1;

        int i = Math.Max(0, currentArrayIndex);
        
        var shouldFill = EqualityComparer<T>.Default.Equals(defaultValue, default) == false;
        
        while (restSize > 0)
        {
            var chunk = GetOrAddChunk(i);

            var startIndex = 0;

            if (i == currentArrayIndex)
            {
                startIndex = chunk.Size;
            }

            var count = m_maxSizeOfArray - startIndex;
            
            if (startIndex + restSize <= m_maxSizeOfArray)
            {
                count = restSize;
            }

            if (count > 0)
            {
                if (shouldFill)
                {
                    Array.Fill(chunk.Array, defaultValue, startIndex, count);
                }

                chunk.Size = startIndex + count;

                chunk.IsModified = true;

                restSize -= count;
            }

            i++;
        }

        m_count = size;
    }

    /// <summary>
    /// Inserts an element at the specified index shifting subsequent elements to the right.
    /// </summary>
    /// <param name="index">Zero based insertion index. Can be equal to Count (append).</param>
    /// <param name="item">Item to insert.</param>
    public void Insert(int index, T item)
    {
        if (index < 0 || index > m_count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        // Fast append path
        if (index == m_count)
        {
            Add(item);
            return;
        }
        
        int arrayIndex = index >> m_stepBase;
        int elementIndex = index - (arrayIndex << m_stepBase);
        
        // Item to propagate when chunks are full
        T currentItem = item;
        
        for (int ai = arrayIndex; ai < m_arrayCount; ai++)
        {
            var chunk = LoadChuck(ai);
            if (chunk == null)
            {
                return;
            }
            
            // If chunk has space
            if (chunk.Size < m_maxSizeOfArray)
            {
                // shift if inserting not at end of used region
                if (elementIndex < chunk.Size)
                {
                    Array.Copy(chunk.Array, elementIndex, chunk.Array, elementIndex + 1, chunk.Size - elementIndex);
                }
                chunk.Array[elementIndex] = currentItem;
                chunk.Size++;
                chunk.IsModified = true;
                m_count++;
                
                return;
            }

            // Chunk full: push last element forward
            var last = chunk.Array[m_maxSizeOfArray - 1];
            // Shift right inside the chunk starting from elementIndex
            for (int i = m_maxSizeOfArray - 2; i >= elementIndex; i--)
            {
                chunk.Array[i + 1] = chunk.Array[i];
            }
            chunk.Array[elementIndex] = currentItem;
            chunk.IsModified = true; // size unchanged (still full)
            currentItem = last;
            elementIndex = 0; // insertion index for next chunk becomes start
        }

        // All existing chunks were full, need a new chunk
        var newChunkIndex = m_arrayCount; // next index
        var newChunk = GetOrAddChunk(newChunkIndex);
        newChunk.Array[0] = currentItem;
        newChunk.Size = 1;
        newChunk.IsModified = true;
        m_version++;
        m_count++;
    }

    /// <summary>
    /// Removes the element at the specified index shifting subsequent elements left.
    /// </summary>
    /// <param name="index">Zero based index to remove.</param>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= m_count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        // Fast path: removing last element
        if (index == m_count - 1)
        {
            int lastChunkIndex = index >> m_stepBase;
            var lastChunk = LoadChuck(lastChunkIndex);
            lastChunk.Array[lastChunk.Size - 1] = default;
            lastChunk.Size--;
            lastChunk.IsModified = true;

            if (lastChunk.Size == 0)
            {
                m_fileSerialization.RemoveLast();
                m_arrayCount--;
            }
        }
        else
        {
            // Shift elements left one by one using indexer to avoid holding multiple chunks simultaneously (prevents eviction issues)
            for (int i = index; i < m_count - 1; i++)
            {
                this[i] = this[i + 1];
            }
            this[m_count - 1] = default;
        }

        m_count--;

        if (m_count == 0)
        {
            Clear();
        }
        else
        {
            var lastChunk = LoadChuck(m_arrayCount - 1);
            
            if (lastChunk.Size == 0)
            {
                m_fileSerialization.RemoveLast();
                m_arrayCount--;
            }
            
            m_version++;
        }
    }
    
    /// <summary>
    /// Array and List API. Searches the entire sorted FileData&lt;T&gt; for an element using the comparer given and returns the zero-based index of the element.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="startIndex"></param>
    /// <param name="count"></param>
    /// <param name="comparer"></param>
    /// <returns>The zero-based index of item in the sorted FileData&lt;T&gt;, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of Count.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int BinarySearch(T value, int startIndex, int count, IComparer<T> comparer = null)
    {
        if (startIndex < 0 || startIndex >= m_count || count < 0 || count > m_count)
        {
            throw new ArgumentOutOfRangeException($"startIndex {startIndex} or count {count} is out of range. Total count is {m_count}.");
        }
        
        comparer ??= Comparer<T>.Default;

        if (SingleArray())
        {
            var arrayChunk = LoadChuck(0);

            return Array.BinarySearch(arrayChunk.Array, startIndex, m_count - startIndex, value, comparer);
        }

        return BinarySearchSlow(value, startIndex, count, comparer);
    }

    
    private int BinarySearchSlow(T item, int startIndex, int count, IComparer<T> comparer)
    {
        int lo = startIndex;
        int hi = count - 1;

        while (lo <= hi)
        {
            int index = lo + ((hi - lo) >> 1);

            int order = comparer.Compare(item, this[index]);

            if (order == 0)
            {
                return index;
            }

            if (order > 0)
            {
                lo = index + 1;
            }
            else
            {
                hi = index - 1;
            }
        }
        return ~lo;
    }

    /// <summary>
    /// Clears all data from the file and memory.
    /// </summary>
    public void Clear()
    {
        m_count = 0;
        m_arrayCount = 0;
        m_buffer.Clear();
        m_fileSerialization.Clear();
        m_version++;
    }
    
    /// <summary>
    /// Clears all arrays.
    /// </summary>
    public void ClearAllArrays()
    {
        var currentCount = m_arrayCount;

        for (int i = 0; i < currentCount; i++)
        {
            var chunk = LoadChuck(i);

            var array = chunk.Array;
            Array.Clear(array, 0, array.Length);
        }

        m_version++;
    }

    /// <summary>
    /// Forces all cached data to be written to disk.
    /// </summary>
    public void Flush()
    {
        FlushModifiedChunks();
        m_fileSerialization.Flush();
    }

    /// <summary>
    /// Gets an enumerator that iterates through the collection.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        var currentCount = m_arrayCount;
        var version = m_version;

        for (int i = 0; i < currentCount; i++)
        {
            var chunk = LoadChuck(i);
            
            for (int j = 0; j < chunk.Size; j++)
            {
                if (version != m_version)
                {
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                }
                
                yield return chunk.Array[j];
            }
        }
    }
    
    /// <summary>
    /// Gets an enumerator that iterates through the collection.
    /// </summary>
    public void ForEach<V>(V p, Func<FileData<T>, int, T, V, bool> act)
    {
        int totalIndex = 0;
        var version = m_version;
        
        for (int i = 0; i < m_arrayCount; i++)
        {
            var chunk = LoadChuck(i);
            
            for (int j = 0; j < chunk.Size; j++)
            {
                if (version != m_version)
                {
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                }
                
                if (act(this, totalIndex, chunk.Array[j], p))
                {
                    return;
                }

                totalIndex++;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Releases all resources used by the FileData and flush buffers to the disk.
    /// </summary>
    public void Dispose()
    {
        if (m_disposed)
        {
            return;
        }
        
        try
        {
            EndWrite();

            if (m_buffer is IDisposable d)
            {
                d.Dispose();
            }
            
            m_fileSerialization?.Dispose();
        }
        finally
        {
            m_disposed = true;
        }
    }

    /// <summary>
    /// Serializes the current instance to the provided <see cref="IDataSerializationInfo"/> implementation.
    /// </summary>
    /// <param name="info"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void SerializeTo(IDataSerializationInfo info)
    {
        info.UpdateMetadata((m_maxSizeOfArray, m_count, m_version));
                
        var currentCount = m_arrayCount;
        var version = m_version;

        if (m_arrayCount == 1)
        {
            info.WriteSingleArray(LoadChuck(0).Array);
        }
        else
        {
            for (int i = 0; i < currentCount; i++)
            {
                if (version != m_version)
                {
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                }
            
                var chunk = LoadChuck(i);
            
                info.AppendArray(chunk.Array);
            }
        }
                
        info.WriteMetadata();
    }
}
