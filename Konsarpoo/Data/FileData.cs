using System;
using System.Collections;
using System.Collections.Generic;
using Konsarpoo.Collections.Allocators;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Konsarpoo.Collections;

/// <summary>
/// A file-backed data structure that provides IReadOnlyList&lt;T&gt; interface with on-demand loading and unloading of array chunks.
/// Uses LfuCache for memory management and DataFileSerialization for persistence.
/// </summary>
/// <typeparam name="T">The type of elements stored in the collection</typeparam>
public class FileData<T> : IReadOnlyList<T>, IDisposable
{
    private class ArrayChunk
    {
        public T[] Array;
        public int Size;
        public bool IsDirty;
        
        public ArrayChunk(T[] array, int size)
        {
            Array = array;
            Size = size;
            IsDirty = false;
        }
    }
    
    private readonly DataFileSerialization m_fileSerialization;
    private readonly LfuCache<int, ArrayChunk> m_buffer;
    private readonly IArrayAllocator<T> m_arrayAllocator;
    
    private readonly int m_stepBase;
    private readonly int m_maxSizeOfArray;
    private int m_count;
    private int m_arrayCount;
    private bool m_disposed;
    private int m_writeNestingLevel;
    
    public static FileData<T> Create(string filePath, int maxSizeOfArray, byte[] key = null, int arrayBufferCapacity = 10, IArrayAllocator<T> allocator = null)
    {
        return new FileData<T>(filePath, maxSizeOfArray, FileMode.CreateNew, arrayBufferCapacity, key, allocator);
    }
    
    public static FileData<T> Open(string filePath, byte[] key = null, int arrayBufferCapacity = 10, IArrayAllocator<T> allocator = null)
    {
        return new FileData<T>(filePath, 0, FileMode.Open, arrayBufferCapacity, key, allocator);
    }
    
    public static FileData<T> CreateCrypted(string filePath, [NotNull] byte[] key, int maxSizeOfArray, int arrayBufferCapacity = 10, IArrayAllocator<T> allocator = null)
    {
        if (key == null || key.Length == 0) throw new ArgumentException(nameof(key));
        return new FileData<T>(filePath, maxSizeOfArray, FileMode.CreateNew, arrayBufferCapacity, key, allocator);
    }
    
    public static FileData<T> OpenCrypted(string filePath, [NotNull] byte[] key,  int arrayBufferCapacity = 10, IArrayAllocator<T> allocator = null)
    {
        if (key == null || key.Length == 0) throw new ArgumentException(nameof(key));
        return new FileData<T>(filePath, 0, FileMode.Open, arrayBufferCapacity, key, allocator);
    }
  
    private FileData(string filePath, int maxSizeOfArray, FileMode fileMode, int arrayBufferCapacity, byte[] cryptoKey, IArrayAllocator<T> allocator = null)
    {
        if (fileMode == FileMode.Open)
        {
            m_fileSerialization = cryptoKey != null
                ? new CryptoDataFileSerialization(filePath, fileMode, cryptoKey)
                : new DataFileSerialization(filePath, fileMode);

            var metadata = m_fileSerialization.ReadMetadata();
                 
            m_maxSizeOfArray = metadata.maxSizeOfArray;
            m_count = metadata.dataCount;
            m_arrayCount = metadata.arraysCount;
        }
        else
        {
            m_maxSizeOfArray = maxSizeOfArray; 
            m_fileSerialization = cryptoKey != null
                ? new CryptoDataFileSerialization(filePath, fileMode, maxSizeOfArray, cryptoKey, 0)
                : new DataFileSerialization(filePath, fileMode, maxSizeOfArray, 0);
        }
        
        m_stepBase = GetStepBase(m_maxSizeOfArray);
        
        var defaultAllocatorSetup = KonsarpooAllocatorGlobalSetup.DefaultAllocatorSetup;
      
        m_arrayAllocator = allocator ?? defaultAllocatorSetup.GetDataStorageAllocator<T>().GetDataArrayAllocator();
        
        m_buffer = new LfuCache<int, ArrayChunk>(0, 0, null, disposingStrategy: OnChunkDone);
        
        m_buffer.StartTrackingMemory(arrayBufferCapacity, (key, chunk) => 1);
    }

    private static int GetStepBase(int maxSizeOfArray)
    {
        if (maxSizeOfArray <= 1)
        {
            return 0;
        }
        return (int)Math.Log(maxSizeOfArray, 2);
    }

    private void OnChunkDone(int arrayIndex, ArrayChunk chunk)
    {
        m_fileSerialization.WriteArray(arrayIndex, chunk.Array);
        
        m_arrayAllocator.Return(chunk.Array);

        chunk.IsDirty = false;
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
            FlushDirtyChunks();
        }
        
        m_fileSerialization.EndWrite();
    }

    private void FlushDirtyChunks()
    {
        m_fileSerialization.SetMetadata((m_maxSizeOfArray, m_count, 1, m_arrayCount));
        
        foreach (var kvp in m_buffer.OrderBy(k => k.Key))
        {
            if (kvp.Value.IsDirty)
            {
                WriteChunkToFile(kvp.Key, kvp.Value);
                kvp.Value.IsDirty = false;
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
        var newChunk = new ArrayChunk(newArray, 0);
        
        m_fileSerialization.AppendArray(newArray);
        m_buffer.AddOrUpdate(arrayIndex, newChunk);

        m_arrayCount++;
        
        return newChunk;
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
            
            var chunk = new ArrayChunk(loadedArray, size);
            
            m_buffer.AddOrUpdate(arrayIndex, chunk);
            
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
            chunk.IsDirty = true;
        }
    }

    /// <summary>
    /// Gets the number of elements in the collection.
    /// </summary>
    public int Count => m_count;

    /// <summary>
    /// Adds an element to the end of the collection.
    /// </summary>
    public void Add(T item)
    {
        var arrayIndex = m_count >> m_stepBase;
        
        var chunk = GetOrAddChunk(arrayIndex);

        chunk.Array[chunk.Size] = item;
        chunk.IsDirty = true;
        chunk.Size++;

        m_count++;  
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
            if (chunk.Size < m_maxSizeOfArray - 1)
            {
                // shift if inserting not at end of used region
                if (elementIndex < chunk.Size)
                {
                    Array.Copy(chunk.Array, elementIndex, chunk.Array, elementIndex + 1, chunk.Size - elementIndex);
                }
                chunk.Array[elementIndex] = currentItem;
                chunk.Size++;
                chunk.IsDirty = true;
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
            chunk.IsDirty = true; // size unchanged (still full)
            currentItem = last;
            elementIndex = 0; // insertion index for next chunk becomes start
        }

        // All existing chunks were full, need a new chunk
        var newChunkIndex = m_arrayCount; // next index
        var newChunk = GetOrAddChunk(newChunkIndex);
        newChunk.Array[0] = currentItem;
        newChunk.Size = 1;
        newChunk.IsDirty = true;
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
            lastChunk.Size--;
            lastChunk.IsDirty = true;
        }
        else
        {
            // Shift elements left one by one using indexer to avoid holding multiple chunks simultaneously (prevents eviction issues)
            for (int i = index; i < m_count - 1; i++)
            {
                this[i] = this[i + 1];
            }
        }

        m_count--;

        if (m_count == 0)
        {
            Clear();
        }
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
    }

    /// <summary>
    /// Forces all cached data to be written to disk and evicts all arrays from memory.
    /// </summary>
    public void Flush()
    {
        FlushDirtyChunks();
        m_fileSerialization.Flush();
    }

    /// <summary>
    /// Gets an enumerator that iterates through the collection.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < m_count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Releases all resources used by the FileData.
    /// </summary>
    public void Dispose()
    {
        if (m_disposed)
        {
            return;
        }
        
        try
        {
            if (m_writeNestingLevel > 0)
            {
                EndWrite();
            }
            
            m_buffer?.Dispose();
            m_fileSerialization?.Dispose();
        }
        finally
        {
            m_disposed = true;
        }
    }
}
