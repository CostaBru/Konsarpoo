using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using JetBrains.Annotations;
using Konsarpoo.Collections.Data.Serialization;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections.Persistence;

/// <summary>
/// File-based set implementation with support of encryption and compression.
/// Stores data in 3 files: metadata file, buckets file and slots file. Not thread safe.
/// </summary>
/// <typeparam name="T"></typeparam>
[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
public partial class FileSet<T> : ICollection<T>, IReadOnlyCollection<T>, IAppender<T>, IDisposable
{
    private static readonly bool IsReferenceType = !typeof(T).IsValueType;

    private IEqualityComparer<T> m_comparer;

    private readonly FileData<int> m_buckets;
    private readonly FileData<KeyEntry<T>> m_slots;

    private int m_count;
    private int m_freeCount;
    private int m_freeList;
    private int m_version;
    
    private int m_edit = 0;
    private bool m_disposed;
    private int m_lastIndex;
    
    [NotNull]
    private readonly string m_metaDataFile;
    
    public static FileSet<T> Create(string filePathPrefix,
        int maxSizeOfArray, 
        byte[] key = null,
        CompressionLevel compressionLevel = CompressionLevel.NoCompression, 
        IEqualityComparer<T> comparer = null,
        int storageArrayBufferCapacity = 1)
    {
        return new FileSet<T>(filePathPrefix, maxSizeOfArray, FileMode.CreateNew, comparer, compressionLevel, storageArrayBufferCapacity, key, false);
    }
    
    public static FileSet<T> Open(string filePathPrefix, 
        byte[] key = null,
        CompressionLevel compressionLevel = CompressionLevel.NoCompression,
        IEqualityComparer<T> comparer = null,
        int storageArrayBufferCapacity = 10,
        bool rehashOnOpen = true)
    {
        return new FileSet<T>(filePathPrefix, 0, FileMode.Open, comparer, compressionLevel, storageArrayBufferCapacity, key, rehashOnOpen);
    }
    
    public static FileSet<T> OpenOrCreate(string filePathPrefix,
        int maxSizeOfArray, 
        byte[] key = null,
        CompressionLevel compressionLevel = CompressionLevel.NoCompression, 
        IEqualityComparer<T> comparer = null,
        int storageArrayBufferCapacity = 10,
        bool rehashOnOpen = true)
    {
        return new FileSet<T>(filePathPrefix, maxSizeOfArray, FileMode.OpenOrCreate, comparer, compressionLevel, storageArrayBufferCapacity, key, rehashOnOpen);
    }
    
    /// <summary>
    /// Metadata stored in the FileSet&lt;T&gt; metadata file.
    /// </summary>
    public class FileSetMetadata
    {
        /// <summary>
        /// File name of bucket file. Not a full path.
        /// </summary>
        public string BucketsFile { get; set; }
        /// <summary>
        /// File name of entries file. Not a full path.
        /// </summary>
        public string SlotsFile  { get; set; }
        /// <summary>
        ///  Number of items stored in the FileSet&lt;T&gt;.
        /// </summary>
        public int Count  { get; set; }
        /// <summary>
        /// Number of free slots in the FileSet&lt;T&gt;.
        /// </summary>
        public int FreeCount  { get; set; }
        /// <summary>
        /// Index of first free slot in the FileSet&lt;T&gt;.
        /// </summary>
        public int FreeList  { get; set; }
        /// <summary>
        /// Version of the FileSet&lt;T&gt;.
        /// </summary>
        public int Version  { get; set; }
        /// <summary>
        /// Type of comparer used in the FileSet&lt;T&gt;.
        /// </summary>
        public string ComparerType  { get; set; }
        /// <summary>
        /// Type of key stored in the FileSet&lt;T&gt;.
        /// </summary>
        public string KeyType  { get; set; }
        /// <summary>
        /// Maximum size of array stored in the FileSet&lt;T&gt;.
        /// </summary>
        public int MaxSizeOfArray  { get; set; }
        /// <summary>
        /// Last used index in the FileSet&lt;T&gt;.
        /// </summary>
        public int LastIndex { get; set; }
    };
    
    private FileSet([NotNull] string filePath, int maxSizeOfArray,
        FileMode fileMode,
        IEqualityComparer<T> comparer,
        CompressionLevel compressionLevel,
        int arrayBufferCapacity,
        byte[] cryptoKey, 
        bool rehashOnOpen)
        : this(
            filePath,
            PrepareConstructorArgs(filePath, maxSizeOfArray, fileMode, compressionLevel, cryptoKey),
            comparer,
            arrayBufferCapacity,
            rehashOnOpen)
    {
    }
    
    // Bridge to reuse the public ctor with prepared args in a single call
    private FileSet(string filePath,
        ConstructorArgs args,
        IEqualityComparer<T> comparer,
        int arrayBufferCapacity,
        bool rehashOnOpen)
        : this(filePath, args.Meta, args.EntriesSerialization, args.BucketsSerialization, comparer, arrayBufferCapacity, rehashOnOpen)
    {
    }
    
    // Packs constructor preparation results to avoid duplication and keep the public constructor the single initialization point.
    private sealed class ConstructorArgs
    {
        public FileSetMetadata Meta { get; set; }
        public DataFileSerialization EntriesSerialization { get; set; }
        public DataFileSerialization BucketsSerialization { get; set; }
    }

    private static ConstructorArgs PrepareConstructorArgs(string filePath, int maxSizeOfArray, FileMode fileMode, CompressionLevel compressionLevel, byte[] cryptoKey)
    {
        // Normalize OpenOrCreate into Open or Create based on metadata file existence
        var effectiveMode = fileMode == FileMode.OpenOrCreate
            ? (System.IO.File.Exists(filePath) ? FileMode.Open : FileMode.Create)
            : fileMode;

        var entriesFile = GetSlotsFile(filePath);
        var bucketFile = GetBucketFile(filePath);

        DataFileSerialization entriesDataFileSerialization = effectiveMode == FileMode.Open
            ? new DataFileSerialization(entriesFile, effectiveMode, cryptoKey, compressionLevel)
            : new DataFileSerialization(entriesFile, effectiveMode, cryptoKey, compressionLevel, maxSizeOfArray);

        DataFileSerialization bucketDataFileSerialization = effectiveMode == FileMode.Open
            ? new DataFileSerialization(bucketFile, effectiveMode, cryptoKey, compressionLevel)
            : new DataFileSerialization(bucketFile, effectiveMode, cryptoKey, compressionLevel, maxSizeOfArray);

        FileSetMetadata metaData = null;
        if (effectiveMode == FileMode.Open)
        {
            var text = System.IO.File.ReadAllText(filePath);
            metaData = SerializeHelper.DeserializeWithDcs<FileSetMetadata>(text);
        }

        return new ConstructorArgs
        {
            Meta = metaData,
            EntriesSerialization = entriesDataFileSerialization,
            BucketsSerialization = bucketDataFileSerialization
        };
    }

    public FileSet(string filePath, 
        FileSetMetadata metaData, 
        DataFileSerialization entriesDataFileSerialization, 
        DataFileSerialization bucketDataFileSerialization,
        IEqualityComparer<T> comparer,
        int arrayBufferCapacity,
        bool rehashOnOpen)
    {
        m_metaDataFile = filePath ?? throw new ArgumentNullException(nameof(filePath));
      
        m_comparer = comparer ?? EqualityComparer<T>.Default;

        if (metaData != null)
        {
            m_count = metaData.Count;
            m_freeCount = metaData.FreeCount;
            m_freeList = metaData.FreeList;
            m_version = metaData.Version;
            m_lastIndex = metaData.LastIndex;
        }

        m_buckets = new FileData<int>(bucketDataFileSerialization, arrayBufferCapacity);
        m_slots = new FileData<KeyEntry<T>>(entriesDataFileSerialization, arrayBufferCapacity);

        if (rehashOnOpen)
        {
            ReHash();
        }
    }
  
    private static string GetSlotsFile(string mainFile)
    {
        return mainFile + ".slt";
    }

    private static string GetBucketFile(string mainFile)
    {
        return mainFile + ".sbkt";
    }

    /// <summary>
    /// Gets an object that can be used to synchronize access to the FileSet&lt;T&gt; class instance.
    /// </summary>
    public object SyncRoot => this;

    public static IEnumerable<string> GetFileNames(string filePath)
    {
        yield return filePath;
        yield return GetBucketFile(filePath);
        yield return GetSlotsFile(filePath);
    }

    /// <summary>
    /// Gets file storage names;
    /// </summary>
    public IEnumerable<string> Files => new[] { m_metaDataFile, GetBucketFile(m_metaDataFile), GetSlotsFile(m_metaDataFile) };

    /// <summary>
    /// Begins a write transaction. Multiple writes can be batched until EndWrite is called.
    /// </summary>
    public void BeginWrite()
    {
        m_edit++;
        
        m_buckets.BeginWrite();
        m_slots.BeginWrite();
    }

    /// <summary>
    /// Ends a write transaction and flushes any pending changes to disk.
    /// </summary>
    public void EndWrite()
    {
        m_edit--;

        if (m_edit <= 0)
        {
            m_edit = 0;
            
            FlushMeta();
        }
        
        m_slots.EndWrite();
        m_buckets.EndWrite();
    }

    /// <summary>
    /// Forces all cached data to be written to disk.
    /// </summary>
    public void Flush()
    {
        FlushMeta();

        m_buckets.Flush();
        m_slots.Flush();
    }

    protected void TryFlush()
    {
        if (m_edit > 0)
        {
            return;
        }

        Flush();
    }

    private void FlushMeta()
    {
        var fn = Path.GetFileName(m_metaDataFile);

        var mapFileMetadata = new FileSetMetadata()
        {
            BucketsFile = GetBucketFile(fn),
            SlotsFile = GetSlotsFile(fn),
            ComparerType = m_comparer.GetType().FullName,
            Count = m_count,
            KeyType = typeof(T).FullName,
            MaxSizeOfArray = m_slots.MaxSizeOfArray,
            Version = m_version,
            LastIndex = m_lastIndex
        };

        var serializeWithDcs = SerializeHelper.SerializeWithDcs(mapFileMetadata);
        
        System.IO.File.WriteAllText(m_metaDataFile, serializeWithDcs);
    }
    
    /// <summary>
    /// Python List API. Adds new item to the end of the FileSet&lt;T&gt;.
    /// </summary>
    /// <param name="value"></param>
    public void Append(T value)
    {
        var key = value;
        Insert(ref key);
    }

    /// <summary>
    /// Gets the number of key/value pairs contained in the FileSet&lt;T&gt;.
    /// </summary>
    public int Count => (m_count - m_freeCount);

    bool ICollection<T>.IsReadOnly => false;

    /// <summary>
    /// Array API. Gets the number of key/value pairs contained in the FileSet&lt;T&gt;.
    /// </summary>
    public int Length => Count;

    /// <summary>
    /// Gets the IEqualityComparer&lt;T&gt; that is used to determine equality of keys for the map.
    /// </summary>
    public IEqualityComparer<T> Comparer => m_comparer;

  
    /// <summary>
    /// Clears container. Suppresses instance finalization.
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
            
            m_buckets.Dispose();
            m_slots.Dispose();
        }
        finally
        {
            m_disposed = true;
        }
    }

    /// <summary>
    /// Allocates the internal storage to fit a given number of items beforehand.
    /// </summary>
    /// <param name="capacity"></param>
    public void EnsureCapacity(int capacity)
    {
        var prime = Prime.GetPrime(capacity);

        m_buckets.Ensure(prime);
        m_slots.Ensure(prime);
    }

    /// <summary>
    /// Adds the specified key and value to the map.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public bool Add(T key)
    {
        return Insert(ref key);
    }

    void ICollection<T>.Add(T key)
    {
        Insert(ref key);
    }

    /// <summary>
    /// Removes all contents.
    /// </summary>
    public void Clear()
    {
        if (m_count <= 0)
        {
            return;
        }

        m_buckets.Clear();
        m_slots.Clear();

        m_freeList = -1;
        m_count = 0;
        m_freeCount = 0;
        m_lastIndex = 0;
        unchecked { ++m_version; }
        
        TryFlush();
    }

    /// <summary>
    /// Determines whether the Set&lt;T&gt; has missing the item.
    /// </summary>
    /// <param name="item"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public bool IsMissing(T item)
    {
        return !Contains(item);
    }
        
    /// <summary>
    /// Determines whether the Set&lt;T&gt; contains the item.
    /// </summary>
    /// <param name="item"></param>
    public bool Contains(T item)
    {
        if (m_buckets.Count > 0)
        {
            var hashCode = IsReferenceType && item == null ? 0 : m_comparer.GetHashCode(item) & int.MaxValue;

            var storageIndex = hashCode % m_buckets.Count;

            var start = m_buckets[storageIndex];

            for (int index = start - 1; index >= 0; index = m_slots[index].Next)
            {
                var slot = m_slots[index];

                if (slot.HashCode == hashCode && m_comparer.Equals(slot.Key, item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        CopyTo(array, 0, m_count);
    }
    
    /// <summary>
    /// Copies the Set&lt;T&gt;to an array.
    /// </summary>
    /// <param name="array"></param>
    public void CopyTo(IList<T> array)
    {
        CopyTo(array, 0, m_count);
    }


    /// <summary>
    /// Copies the Set&lt;T&gt; or a portion of it to an array.
    /// </summary>
    /// <param name="array"></param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    /// <param name="count"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void CopyTo([NotNull] IList<T> array, int arrayIndex, int count)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "An array index is negative.");
        }

        if (arrayIndex >= array.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex),
                $"An array index '{arrayIndex}' is greater or equal than array length ({array.Count}).");
        }

        if (count > m_count)
        {
            throw new ArgumentOutOfRangeException(nameof(count),
                $"Copy count is greater than the number of elements from start to the end of collection.");
        }

        if (count > array.Count - arrayIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(count),
                $"Copy count is greater than the number of elements from arrayIndex to the end of destinationArray");
        }

        int num = 0;
        for (int index = 0; index < m_lastIndex && num < count; ++index)
        {
            var valueByRef = m_slots[index];
            if (valueByRef.HashCode >= 0)
            {
                array[arrayIndex + num] = valueByRef.Key;
                ++num;
            }
        }
    }

    private bool Insert([System.Diagnostics.CodeAnalysis.NotNull] ref T value)
    {
        if (m_buckets.Count == 0)
        {
            Initialize(prime: 2);
        }

        var hashCode = m_comparer.GetHashCode(value) & int.MaxValue;

        int storageIndex = hashCode % m_buckets.Count;

    
        var start = m_buckets[storageIndex];

        for (int? i = start - 1; i >= 0; i = m_slots[i.Value].Next)
        {
            var s = m_slots[i.Value];

            if (s.HashCode == hashCode && m_comparer.Equals(s.Key, value))
            {
                return false;
            }
        }

        int index;
        if (m_freeList >= 0)
        {
            index = m_freeList;
            m_freeList = m_slots[index].Next;
        }
        else
        {
            if (m_lastIndex == m_slots.Count)
            {
                IncreaseCapacity();
                storageIndex = hashCode % m_buckets.Count;
            }

            index = m_lastIndex;
            ++m_lastIndex;
        }
       
        var bucket = m_buckets[storageIndex];

         var slot =  m_slots[index];

        slot.HashCode = hashCode;
        slot.Key = value;
        slot.Next = bucket - 1;

        m_slots[index] = slot;
        
        m_buckets[storageIndex] = index + 1;

        ++m_count;
        unchecked
        {
            ++m_version;
        }

        return true;
    }
    
    private static int ExpandPrime(int oldSize)
    {
        int min = 2 * oldSize;
        if ((uint) min > 2146435069U && 2146435069 > oldSize)
        {
            return 2146435069;
        }
        return Prime.GetPrime(min);
    }
    
    private void IncreaseCapacity()
    {
        int newSize = ExpandPrime(m_count);
        if (newSize <= m_count)
        {
            throw new ArgumentException($"Set capacity overflow happened. {newSize} is less than {m_count}.");
        }

        m_buckets.ClearAllArrays();
            
        m_buckets.Ensure(newSize);
        m_slots.Ensure(newSize);
       
        for (int slotIndex = 0; slotIndex < m_lastIndex; ++slotIndex)
        {
            var slot = m_slots[slotIndex];
                
            int bucketIndex = slot.HashCode % newSize;

            slot.Next = m_buckets[bucketIndex] - 1;

            m_buckets[bucketIndex] = slotIndex + 1;
        }
    }

    /// <summary>
    /// Returns true if value for given key exists.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool TryGetValue([System.Diagnostics.CodeAnalysis.NotNull] T key, out T value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (m_buckets.Count > 0)
        {
            var hashCode = m_comparer.GetHashCode(key) & int.MaxValue;

            for (var i = m_buckets[hashCode % m_buckets.Count] - 1; i >= 0;)
            {
                var currentEntry = m_slots[i];
                if ((currentEntry.HashCode == hashCode) && m_comparer.Equals(currentEntry.Key, key))
                {
                    value = key;
                    return true;
                }

                i = currentEntry.Next;
            }
        }

        value = default;

        return false;
    }

    /// <summary>
    /// Removes the value with the specified key from the FileSet&lt;T&gt;.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool Remove(T item)
    {
        if (m_buckets.Count > 0)
        {
            int hashCode = 0;
            if (IsReferenceType)
            {
                if (item == null)
                {
                    hashCode = 0;
                }
                else
                {
                    hashCode = m_comparer.GetHashCode(item) & int.MaxValue;
                }
            }
            else
            {
                hashCode = m_comparer.GetHashCode(item) & int.MaxValue;
            }

            int index1 = hashCode % m_buckets.Count;
            int last = -1;

            var start = m_buckets[index1];

            for (int index = start - 1; index >= 0; index = m_slots[index].Next)
            {
                var currentEntry =  m_slots[index];

                if (currentEntry.HashCode == hashCode && m_comparer.Equals(currentEntry.Key, item))
                {
                    if (last < 0)
                    {
                        m_buckets[index1] = currentEntry.Next + 1;
                    }
                    else
                    {
                        var entry = m_slots[last];
                        
                        entry.Next = currentEntry.Next;

                        m_slots[last] = entry;
                    }

                    currentEntry.HashCode = -1;
                    currentEntry.Key = default(T);
                    currentEntry.Next = m_freeList;

                    m_slots[index] = currentEntry;

                    --m_count;

                    if (m_count == 0)
                    {
                        m_lastIndex = 0;
                        m_freeList = -1;
                    }
                    else
                    {
                        m_freeList = index;
                    }

                    return true;
                }

                last = index;
            }
        }

        return false;
    }

    private void Initialize(int prime)
    {
        m_buckets.Ensure(prime);
        m_slots.Ensure(prime);

        m_freeList = -1;
    }

    private void Resize(int prime)
    {
        m_slots.Ensure(prime);

        m_buckets.ClearAllArrays();
        m_buckets.Ensure(prime);
        
        m_slots.ForEach(m_buckets, (list, i, v, b) =>
        {
            if (v.HashCode >= 0)
            {
                RehashItem(v, list, b, i);
            }

            return false;
        });
    }
    
    private void ReHash()
    {
        m_buckets.ClearAllArrays();
        
        m_slots.ForEach(m_buckets, (list, i, v, b) =>
        {
            if (v.HashCode >= 0)
            {
                RehashItem(v, list, b, i);
            }

            return false;
        });
    }

    private static void RehashItem(KeyEntry<T> keyEntry, FileData<KeyEntry<T>> entries, FileData<int> buckets, int entryIndex)
    {
        var bucket = keyEntry.HashCode % entries.Count;

        keyEntry.Next = buckets[bucket] - 1;

        buckets[bucket] = entryIndex + 1;

        entries[entryIndex] = keyEntry;
    }
    
    /// <summary>
    /// Determines whether the FileSet&lt;T&gt; contains the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public bool ContainsKey(T key)
    {
        return TryGetValue(key, out var _);
    }
    
    /// <summary>
    /// Returns values contained in Set.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IEnumerable<T> Values()
    {
        var version = m_version;
            
        for (int i = 0; i < m_lastIndex; ++i)
        {
            if (m_slots[i].HashCode >= 0)
            {
                if (version != m_version)
                {
                    throw new InvalidOperationException($"FileSet collection was modified during enumeration.");
                }
                        
                yield return m_slots[i].Key;
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    public IEnumerator<T> GetEnumerator()
    {
        return Values().GetEnumerator();
    }
       
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return Values().GetEnumerator();
    }

    
    /// <summary>Adds the specified enumerable to a set.</summary>
    /// <param name="enumerable">The element to add to the set.</param>
    public void AddRange(IEnumerable<T> enumerable)
    {
        foreach (var item in enumerable)
        {
            var val = item;
            Insert(ref val);
        }
    }
    
     
    /// <summary>
    /// Modifies the current FileSet&lt;T&gt; object to contain all elements that are present in itself, the specified collection, or both.
    /// </summary>
    /// <param name="other"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void UnionWith(IEnumerable<T> other)
    {
        foreach (var item in other)
        {
            var val = item;
            Insert(ref val);
        }
    }
    
    /// <summary>
    /// Removes all elements that match the conditions defined by the specified predicate from a FileSet&lt;T&gt; collection.
    /// </summary>
    /// <param name="match"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public int RemoveWhere([NotNull] Predicate<T> match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }
        
        int num = 0;
        for (int index = 0; index < m_lastIndex; ++index)
        {
            var byRef =  m_slots[index];
            if (byRef.HashCode >= 0)
            {
                ref T obj = ref byRef.Key;

                if (match(obj) && Remove(obj))
                {
                    ++num;
                }
            }
        }
        return num;
    }
}