using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Data.Serialization;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections.Persistence;

/// <summary>
/// File-based map/dictionary implementation with support of encryption and compression.
/// Stores data in 3 files: metadata file, buckets file and entries file. Not thread safe.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
[DebuggerDisplay("Count = {Count}")]
public partial class FileMap<TKey, TValue> : IDictionary<TKey, TValue>, 
    ICollection<KeyValuePair<TKey, TValue>>,
    IEnumerable<KeyValuePair<TKey, TValue>>, 
    IReadOnlyDictionary<TKey, TValue>, 
    IReadOnlyCollection<KeyValuePair<TKey, TValue>>, 
    IAppender<KeyValuePair<TKey, TValue>>,
    IDisposable
{
    internal const int HashCoef = 0x7fffffff;
    
    private static readonly bool s_valueTypeIsCollection = ValueIsCollectionType();
    
    private static bool ValueIsCollectionType()
    {
        var type = typeof(TValue);
        return type != typeof(string) && (type.IsArray || (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type)));
    }

    private IEqualityComparer<TKey> m_comparer;

    private readonly FileData<int> m_buckets;
    private readonly FileData<Entry<TKey, TValue>> m_entries;

    private int m_count;
    private int m_freeCount;
    private int m_freeList;
    private int m_version;
    
    private Func<TKey, TValue> m_missingValueFactory;
    
    private int m_edit = 0;
    private bool m_disposed;
    
    private static readonly bool s_valueIsDisposable = typeof(IDisposable).IsAssignableFrom(typeof(TValue));
    
    [NotNull]
    private readonly string m_metaDataFile;
    
    public static FileMap<TKey, TValue> Create(string filePathPrefix,
        int maxSizeOfArray, 
        byte[] key = null,
        CompressionLevel compressionLevel = CompressionLevel.NoCompression, 
        IEqualityComparer<TKey> comparer = null,
        int storageArrayBufferCapacity = 1)
    {
        return new FileMap<TKey, TValue>(filePathPrefix, maxSizeOfArray, FileMode.CreateNew, comparer, compressionLevel, storageArrayBufferCapacity, key, false);
    }
    
    public static FileMap<TKey, TValue> Open(string filePathPrefix, 
        byte[] key = null,
        CompressionLevel compressionLevel = CompressionLevel.NoCompression,
        IEqualityComparer<TKey> comparer = null,
        int storageArrayBufferCapacity = 10,
        bool rehashOnOpen = true)
    {
        return new FileMap<TKey, TValue>(filePathPrefix, 0, FileMode.Open, comparer, compressionLevel, storageArrayBufferCapacity, key, rehashOnOpen);
    }
    
    public static FileMap<TKey, TValue> OpenOrCreate(string filePathPrefix,
        int maxSizeOfArray, 
        byte[] key = null,
        CompressionLevel compressionLevel = CompressionLevel.NoCompression, 
        IEqualityComparer<TKey> comparer = null,
        int storageArrayBufferCapacity = 10,
        bool rehashOnOpen = true)
    {
        return new FileMap<TKey, TValue>(filePathPrefix, maxSizeOfArray, FileMode.OpenOrCreate, comparer, compressionLevel, storageArrayBufferCapacity, key, rehashOnOpen);
    }
    
    /// <summary>
    /// Metadata stored in the FileMap&lt;TKey,TValue&gt; metadata file.
    /// </summary>
    public class MapFileMetadata
    {
        /// <summary>
        /// File name of bucket file. Not a full path.
        /// </summary>
        public string BucketsFile { get; set; }
        /// <summary>
        /// File name of entries file. Not a full path.
        /// </summary>
        public string EntriesFile  { get; set; }
        /// <summary>
        ///  Number of items stored in FileMap&lt;TKey,TValue&gt;.
        /// </summary>
        public int Count  { get; set; }
        /// <summary>
        /// Number of free slots in FileMap&lt;TKey,TValue&gt;.
        /// </summary>
        public int FreeCount  { get; set; }
        /// <summary>
        /// Index of first free slot in the  FileMap&lt;TKey,TValue&gt;.
        /// </summary>
        public int FreeList  { get; set; }
        /// <summary>
        /// Version of the FileMap&lt;TKey,TValue&gt;.
        /// </summary>
        public int Version  { get; set; }
        /// <summary>
        /// Type of comparer used in the FileMap&lt;TKey,TValue&gt;.
        /// </summary>
        public string ComparerType  { get; set; }
        /// <summary>
        /// Type of key stored in the FileMap&lt;TKey,TValue&gt;.
        /// </summary>
        public string KeyType  { get; set; }
        /// <summary>
        /// Type of value stored in the FileMap&lt;TKey,TValue&gt;.
        /// </summary>
        public string ValueType  { get; set; }
        /// <summary>
        /// Maximum size of array stored in the FileMap&lt;TKey,TValue&gt;.
        /// </summary>
        public int MaxSizeOfArray  { get; set; }
    };
    
    private FileMap([NotNull] string filePath, int maxSizeOfArray,
        FileMode fileMode,
        IEqualityComparer<TKey> comparer,
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
    private FileMap(string filePath,
        ConstructorArgs args,
        IEqualityComparer<TKey> comparer,
        int arrayBufferCapacity,
        bool rehashOnOpen)
        : this(filePath, args.Meta, args.EntriesSerialization, args.BucketsSerialization, comparer, arrayBufferCapacity, rehashOnOpen)
    {
    }
    
    // Packs constructor preparation results to avoid duplication and keep the public constructor the single initialization point.
    private sealed class ConstructorArgs
    {
        public MapFileMetadata Meta { get; set; }
        public DataFileSerialization EntriesSerialization { get; set; }
        public DataFileSerialization BucketsSerialization { get; set; }
    }

    private static ConstructorArgs PrepareConstructorArgs(string filePath, int maxSizeOfArray, FileMode fileMode, CompressionLevel compressionLevel, byte[] cryptoKey)
    {
        // Normalize OpenOrCreate into Open or Create based on metadata file existence
        var effectiveMode = fileMode == FileMode.OpenOrCreate
            ? (System.IO.File.Exists(filePath) ? FileMode.Open : FileMode.Create)
            : fileMode;

        var entriesFile = GetEntriesFile(filePath);
        var bucketFile = GetBucketFile(filePath);

        DataFileSerialization entriesDataFileSerialization = effectiveMode == FileMode.Open
            ? new DataFileSerialization(entriesFile, effectiveMode, cryptoKey, compressionLevel)
            : new DataFileSerialization(entriesFile, effectiveMode, cryptoKey, compressionLevel, maxSizeOfArray);

        DataFileSerialization bucketDataFileSerialization = effectiveMode == FileMode.Open
            ? new DataFileSerialization(bucketFile, effectiveMode, cryptoKey, compressionLevel)
            : new DataFileSerialization(bucketFile, effectiveMode, cryptoKey, compressionLevel, maxSizeOfArray);

        MapFileMetadata metaData = null;
        if (effectiveMode == FileMode.Open)
        {
            var text = System.IO.File.ReadAllText(filePath);
            metaData = SerializeHelper.DeserializeWithDcs<MapFileMetadata>(text);
        }

        return new ConstructorArgs
        {
            Meta = metaData,
            EntriesSerialization = entriesDataFileSerialization,
            BucketsSerialization = bucketDataFileSerialization
        };
    }

    public FileMap(string filePath, 
        MapFileMetadata metaData, 
        DataFileSerialization entriesDataFileSerialization, 
        DataFileSerialization bucketDataFileSerialization,
        IEqualityComparer<TKey> comparer,
        int arrayBufferCapacity,
        bool rehashOnOpen)
    {
        m_metaDataFile = filePath ?? throw new ArgumentNullException(nameof(filePath));
      
        m_comparer = comparer ?? EqualityComparer<TKey>.Default;

        if (metaData != null)
        {
            m_count = metaData.Count;
            m_freeCount = metaData.FreeCount;
            m_freeList = metaData.FreeList;
            m_version = metaData.Version;
        }

        m_buckets = new FileData<int>(bucketDataFileSerialization, arrayBufferCapacity);
        m_entries = new HashCodedChunkFileData(entriesDataFileSerialization, arrayBufferCapacity, disposeChunk: DisposeValue, isModifiedChunkCheck: ModifiedChunkCheck);

        if (rehashOnOpen)
        {
            ReHash();
        }
    }
    
    private class HashCodedChunk : HashCodedChunkFileData.ArrayChunk
    {
        public int HashCode;
    }
    
    private class HashCodedChunkFileData :  FileData<Entry<TKey, TValue>>
    {
        public HashCodedChunkFileData(DataFileSerialization fileSerialization, int arrayBufferCapacity, IArrayAllocator<Entry<TKey, TValue>> allocator = null, ICacheStore<int, ArrayChunk> cacheStore = null, Action<ArrayChunk> disposeChunk = null, Func<ArrayChunk, bool> isModifiedChunkCheck = null) : base(fileSerialization, arrayBufferCapacity, allocator, cacheStore, disposeChunk, isModifiedChunkCheck)
        {
        }

        protected override ArrayChunk NewChunkInstance()
        {
            return new HashCodedChunk();
        }
    }

    private bool ModifiedChunkCheck(FileData<Entry<TKey, TValue>>.ArrayChunk arg)
    {
        if (s_valueTypeIsCollection)
        {
            if (arg is HashCodedChunk hs)
            {
                int hashCode = HashCoef;
                
                for (int i = 0; i < hs.Size && i < hs.Array.Length; i++)
                {
                    if (hs.Array[i].Value is IEnumerable en)
                    {
                        int count = 0;
                        
                        foreach (var val in en)
                        {
                            if (val != null)
                            {
                                var code = val.GetHashCode();

                                hashCode ^= code;
                            }

                            count++;
                        }
                        
                        hashCode ^= count;
                    }
                }

                if (hashCode != hs.HashCode)
                {
                    hs.HashCode = hashCode;

                    return true;
                }
            }
        }
        
        return false;
    }

    private void DisposeValue(FileData<Entry<TKey, TValue>>.ArrayChunk chunk)
    {
        if(chunk is HashCodedChunk hs)
        {
            hs.HashCode = 0;
        }
        
        if (s_valueIsDisposable)
        {
            for (int i = 0; i < chunk.Size && i < chunk.Array.Length; i++)
            {
                if (chunk.Array[i].Value is IDisposable d)
                {
                    d.Dispose();
                }
            }
        }
    }

    private static string GetEntriesFile(string mainFile)
    {
        return mainFile + ".etr";
    }

    private static string GetBucketFile(string mainFile)
    {
        return mainFile + ".bkt";
    }

    /// <summary>
    /// Gets an object that can be used to synchronize access to the FileMap&lt;TKey,TValue&gt; class instance.
    /// </summary>
    public object SyncRoot => this;

    public static IEnumerable<string> GetFileNames(string filePath)
    {
        yield return filePath;
        yield return GetBucketFile(filePath);
        yield return GetEntriesFile(filePath);
    }

    /// <summary>
    /// Gets file storage names;
    /// </summary>
    public IEnumerable<string> Files => new[] { m_metaDataFile, GetBucketFile(m_metaDataFile), GetEntriesFile(m_metaDataFile) };

   

    /// <summary>
    /// Begins a write transaction. Multiple writes can be batched until EndWrite is called.
    /// </summary>
    public void BeginWrite()
    {
        m_edit++;
        
        m_buckets.BeginWrite();
        m_entries.BeginWrite();
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
        
        m_entries.EndWrite();
        m_buckets.EndWrite();
    }

    /// <summary>
    /// Forces all cached data to be written to disk.
    /// </summary>
    public void Flush()
    {
        FlushMeta();

        m_buckets.Flush();
        m_entries.Flush();
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

        var mapFileMetadata = new MapFileMetadata()
        {
            BucketsFile = GetBucketFile(fn),
            EntriesFile = GetEntriesFile(fn),
            ComparerType = m_comparer.GetType().FullName,
            Count = m_count,
            KeyType = typeof(TKey).FullName,
            ValueType = typeof(TValue).FullName,
            MaxSizeOfArray = m_entries.MaxSizeOfArray,
            Version = m_version
        };

        var serializeWithDcs = SerializeHelper.SerializeWithDcs(mapFileMetadata);
        
        System.IO.File.WriteAllText(m_metaDataFile, serializeWithDcs);
    }

    /// <summary>
    /// Inefficient way to get key by its index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public TKey KeyAt(int index) => Keys.ElementAt(index);

    /// <summary>
    /// Gets a collection containing the keys in the FileMap&lt;TKey,TValue&gt;.
    /// </summary>
    public KeyCollection Keys => new KeyCollection(this);

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

    /// <summary>
    /// Gets a collection containing the values in the FileMap&lt;TKey,TValue&gt;.
    /// </summary>
    public ValueCollection Values => new ValueCollection(this);

    
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
    {
        CopyTo(array, index);
    }
    
    private void CopyTo(IList<KeyValuePair<TKey, TValue>> destination, int index)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }
        if ((index < 0) || (index > destination.Count))
        {
            throw new ArgumentOutOfRangeException("index");
        }
        if ((destination.Count - index) < Count)
        {
            throw new ArgumentException();
        }
            
        int count = m_count;
        var entries = m_entries;
            
        for (int i = 0; i < count; i++)
        {
            var entry = entries[i];
            if (entry.Key.HashCode >= 0)
            {
                destination[index++] = new KeyValuePair<TKey, TValue>(entry.Key.Key, entry.Value);
            }
        }
    }
    
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> pair)
    {
        var found = TryGetValue(pair.Key, out var value);
            
        if (found && EqualityComparer<TValue>.Default.Equals(value, pair.Value))
        {
            Remove(pair.Key);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Python List API. Adds new item to the end of the FileMap&lt;TKey,TValue&gt;.
    /// </summary>
    /// <param name="value"></param>
    public void Append(KeyValuePair<TKey, TValue> value)
    {
        var val = value.Value;
        var key = value.Key;
        var add = true;
        Insert(ref key, ref val, ref add);
    }

    /// <summary>
    /// Gets the number of key/value pairs contained in the FileMap&lt;TKey,TValue&gt;.
    /// </summary>
    public int Count => (m_count - m_freeCount);

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    /// <summary>
    /// Array API. Gets the number of key/value pairs contained in the FileMap&lt;TKey,TValue&gt;.
    /// </summary>
    public int Length => Count;

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public TValue this[TKey key]
    {
        get
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            if (m_missingValueFactory != null)
            {
                var newValue = m_missingValueFactory(key);
                var set = false;
                Insert(ref key, ref newValue, ref set);

                return newValue;
            }

            throw new KeyNotFoundException($"Key '{key}' was not found.");
        }
        set
        {
            var set = false;
            Insert(ref key, ref value, ref set);
        }
    }

    /// <summary>
    /// Gets the IEqualityComparer&lt;T&gt; that is used to determine equality of keys for the map.
    /// </summary>
    public IEqualityComparer<TKey> Comparer => m_comparer;

    /// <inheritdoc />
    [Serializable]
    public class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly FileMap<TKey, TValue> m_dictionary;
        private readonly int m_version;
        private int m_index;
        private KeyValuePair<TKey, TValue> m_current;

        internal Enumerator(FileMap<TKey, TValue> dictionary)
        {
            m_dictionary = dictionary;
            m_version = dictionary.m_version;
            m_index = 0;
            m_current = new KeyValuePair<TKey, TValue>();
        }

        /// <inheritdoc />
        public KeyValuePair<TKey, TValue> Current => m_current;

        object IEnumerator.Current
        {
            get
            {
                CheckState();

                return new KeyValuePair<TKey, TValue>(m_current.Key, m_current.Value);
            }
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            CheckVersion();

            while (m_index < m_dictionary.m_count)
            {
                var entry = m_dictionary.m_entries[m_index];

                if (entry.Key.HashCode >= 0)
                {
                    m_current = new KeyValuePair<TKey, TValue>(entry.Key.Key, entry.Value);
                    m_index++;
                    return true;
                }

                m_index++;
            }

            m_index = m_dictionary.m_count + 1;
            m_current = new KeyValuePair<TKey, TValue>();
            return false;
        }

        void IEnumerator.Reset()
        {
            CheckVersion();

            m_index = 0;
            m_current = new KeyValuePair<TKey, TValue>();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        /// <inheritdoc />
        public void Dispose()
        {
        }

        private void CheckVersion()
        {
            if (m_version != m_dictionary.m_version)
            {
                throw new InvalidOperationException($"FileMap collection was modified during enumeration.");
            }
        }

        private void CheckState()
        {
            if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
            {
                throw new InvalidOperationException("FileMap collection was modified during enumeration. ");
            }
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the FileMap&lt;TKey,TValue&gt;.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>
    /// Sets a missing value factory delegate up which would be called instead of throwing the KeyNotFound exception.
    /// </summary>
    /// <param name="missingValueFactory"></param>
    public void EnsureValues([CanBeNull] Func<TKey, TValue> missingValueFactory)
    {
        m_missingValueFactory = missingValueFactory;
    }

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
            m_entries.Dispose();
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
        m_entries.Ensure(prime);
    }

    /// <summary>
    /// Adds the specified key and value to the map.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add(TKey key, TValue value)
    {
        var add = true;
        Insert(ref key, ref value, ref add);
    }

    /// <summary>
    /// Adds the specified key value pair to the map.
    /// </summary>
    /// <param name="value"></param>
    public void Add(KeyValuePair<TKey, TValue> value)
    {
        var val = value.Value;
        var key = value.Key;
        var add = true;
        Insert(ref key, ref val, ref add);
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
        m_entries.Clear();

        m_freeList = -1;
        m_count = 0;
        m_freeCount = 0;
        unchecked { ++m_version; }
        
        TryFlush();
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> pair)
    {
        var found = TryGetValue(pair.Key, out var val);
            
        return (found && EqualityComparer<TValue>.Default.Equals(val, pair.Value));
    }

    private void Insert([System.Diagnostics.CodeAnalysis.NotNull] ref TKey key, ref TValue value, ref bool add)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (m_buckets.Count == 0)
        {
            Initialize(prime: 2);
        }

        int freeList;

        int hashCode = m_comparer.GetHashCode(key) & HashCoef;

        int index = hashCode % m_buckets.Count;

        var bucket = m_buckets[index] - 1;

        for (int i = bucket; i >= 0;)
        {
            var keyEntry = m_entries[i];

            if (keyEntry.Key.HashCode == hashCode && m_comparer.Equals(keyEntry.Key.Key, key))
            {
                if (add)
                {
                    throw new ArgumentException($"Key '{key}' is already exists.");
                }

                keyEntry.Value = value;
                return;
            }

            i = keyEntry.Key.Next;
        }

        if (m_freeCount > 0)
        {
            freeList = m_freeList;
            m_freeList = m_entries[freeList].Key.Next;
            m_freeCount--;
        }
        else
        {
            if (m_count == m_entries.Count)
            {
                int prime = Prime.GetPrime(m_count * 2);

                Resize(prime);

                index = hashCode % m_buckets.Count;

                bucket = m_buckets[index] - 1;
            }

            freeList = m_count;
            m_count++;
        }

        var copy = m_entries[freeList];

        copy.Key.HashCode = hashCode;
        copy.Key.Next = bucket;
        copy.Key.Key = key;
        copy.Value = value;

        m_entries[freeList] = copy;

        m_buckets[index] = freeList + 1;

        unchecked
        {
            ++m_version;
        }
        
        TryFlush();
    }

    /// <summary>
    /// Returns true if value for given key exists.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool TryGetValue([System.Diagnostics.CodeAnalysis.NotNull] TKey key, out TValue value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (m_buckets.Count > 0)
        {
            var hashCode = m_comparer.GetHashCode(key) & HashCoef;

            for (var i = m_buckets[hashCode % m_buckets.Count] - 1; i >= 0;)
            {
                var currentEntry = m_entries[i];
                if ((currentEntry.Key.HashCode == hashCode) && m_comparer.Equals(currentEntry.Key.Key, key))
                {
                    value = currentEntry.Value;
                    return true;
                }

                i = currentEntry.Key.Next;
            }
        }

        value = default;

        return false;
    }

    /// <summary>
    /// Removes the value with the specified key from the FileMap&lt;TKey,TValue&gt;.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool Remove([System.Diagnostics.CodeAnalysis.NotNull] TKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (m_buckets.Count > 0)
        {
            int hashCode = m_comparer.GetHashCode(key) & HashCoef;

            int index = hashCode % m_buckets.Count;
            int last = -1;


            for (int i = m_buckets[index] - 1; i >= 0;)
            {
                var keyEntry = m_entries[i];

                if ((keyEntry.Key.HashCode == hashCode) && m_comparer.Equals(keyEntry.Key.Key, key))
                {
                    if (last < 0)
                    {
                        m_buckets[index] = keyEntry.Key.Next + 1;
                    }
                    else
                    {
                        var entry = m_entries[last];

                        entry.Key.Next = keyEntry.Key.Next;

                        m_entries[last] = entry;
                    }

                    m_entries[i] = new Entry<TKey, TValue>() { Key = new KeyEntry<TKey>(-1, m_freeList, default) };
                    m_freeList = i;
                    m_freeCount++;
                    unchecked
                    {
                        ++m_version;
                    }
                    
                    TryFlush();

                    return true;
                }

                last = i;
                i = keyEntry.Key.Next;
            }
        }

        return false;
    }

    private void Initialize(int prime)
    {
        m_buckets.Ensure(prime);
        m_entries.Ensure(prime);

        m_freeList = -1;
    }

    private void Resize(int prime)
    {
        m_entries.Ensure(prime);

        m_buckets.ClearAllArrays();
        m_buckets.Ensure(prime);
        
        m_entries.ForEach(m_buckets, (list, i, v, b) =>
        {
            if (v.Key.HashCode >= 0)
            {
                RehashItem(v, list, b, i);
            }

            return false;
        });
    }
    
    private void ReHash()
    {
        m_buckets.ClearAllArrays();
        
        m_entries.ForEach(m_buckets, (list, i, v, b) =>
        {
            if (v.Key.HashCode >= 0)
            {
                RehashItem(v, list, b, i);
            }

            return false;
        });
    }

    private static void RehashItem(Entry<TKey, TValue> keyEntry, FileData<Entry<TKey, TValue>> entries, FileData<int> buckets, int entryIndex)
    {
        var bucket = keyEntry.Key.HashCode % entries.Count;

        keyEntry.Key.Next = buckets[bucket] - 1;

        buckets[bucket] = entryIndex + 1;

        entries[entryIndex] = keyEntry;
    }

    /// <summary>
    /// Determines whether the FileMap&lt;TKey,TValue&gt; contains the specified value.
    /// </summary>
    /// <param name="value"></param>
    public bool ContainsValue(TValue value)
    {
        if (m_entries == null)
        {
            return false;
        }

        if (value == null)
        {
            for (int i = 0; i < m_count; i++)
            {
                var entry = m_entries[i];
                if (entry.Key.HashCode >= 0)
                {
                    if (entry.Value == null)
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            var comparer = EqualityComparer<TValue>.Default;
            for (int j = 0; j < m_count; j++)
            {
                var entry = m_entries[j];
                if (entry.Key.HashCode >= 0)
                {
                    if (comparer.Equals(entry.Value, value))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
    
    /// <summary>
    /// Determines whether the FileMap&lt;TKey,TValue&gt; contains the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public bool ContainsKey(TKey key)
    {
        return TryGetValue(key, out var _);
    }

    /// <inheritdoc />
    [Serializable]
    public sealed class KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>, IEnumerable<TKey>
    {
        private readonly FileMap<TKey, TValue> m_dictionary;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public KeyCollection(FileMap<TKey, TValue> dictionary)
        {
            if (ReferenceEquals(dictionary, null))
            {
                throw new ArgumentNullException("dictionary");
            }

            m_dictionary = dictionary;
        }

        /// <inheritdoc />
        public void CopyTo(TKey[] array, int index)
        {
            if (ReferenceEquals(array, null))
            {
                throw new ArgumentNullException("array");
            }

            if ((index < 0) || (index > array.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if ((array.Length - index) < m_dictionary.Count)
            {
                throw new ArgumentException();
            }

            int count = m_dictionary.m_count;
            var entries = m_dictionary.m_entries;
            for (int i = 0; i < count; i++)
            {
                var entry = entries[i];

                if (entry.Key.HashCode >= 0)
                {
                    array[index++] = entry.Key.Key;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        void ICollection<TKey>.Add(TKey item)
        {
            throw new NotSupportedException();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        void ICollection<TKey>.Clear()
        {
            throw new NotSupportedException();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool ICollection<TKey>.Contains(TKey item)
        {
            return m_dictionary.ContainsKey(item);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool ICollection<TKey>.Remove(TKey item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<TKey> GetEnumerator()
        {
            return new Enumerator(m_dictionary);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<TKey>)this).GetEnumerator();
        }

        /// <inheritdoc />
        public int Count => m_dictionary.Count;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool ICollection<TKey>.IsReadOnly => true;

        /// <inheritdoc />
        public struct Enumerator : IEnumerator<TKey>
        {
            private readonly FileMap<TKey, TValue> m_dictionary;
            private readonly int m_version;
            private int m_index;
            private TKey m_currentKey;

            /// <inheritdoc />
            public TKey Current => m_currentKey;

            object IEnumerator.Current
            {
                get
                {
                    CheckState();

                    return m_currentKey;
                }
            }

            internal Enumerator(FileMap<TKey, TValue> dictionary)
            {
                m_dictionary = dictionary;
                m_version = dictionary.m_version;
                m_index = 0;
                m_currentKey = default;
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                CheckVersion();

                while (m_index < m_dictionary.m_count)
                {
                    var entry = m_dictionary.m_entries[m_index];
                    if (entry.Key.HashCode >= 0)
                    {
                        m_currentKey = entry.Key.Key;
                        m_index++;
                        return true;
                    }

                    m_index++;
                }

                m_index = m_dictionary.m_count + 1;
                m_currentKey = default;
                return false;
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            /// <inheritdoc />
            public void Dispose()
            {
            }

            void IEnumerator.Reset()
            {
                CheckVersion();

                m_index = 0;
                m_currentKey = default;
            }

            private void CheckVersion()
            {
                if (m_version != m_dictionary.m_version)
                {
                    throw new InvalidOperationException($"FileMap collection was modified during enumeration.");
                }
            }

            private void CheckState()
            {
                if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
                {
                    throw new InvalidOperationException("FileMap was modified during enumeration.");
                }
            }
        }
    }

    /// <inheritdoc />
    [Serializable]
    public sealed class ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
    {
        private readonly FileMap<TKey, TValue> m_dictionary;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ValueCollection(FileMap<TKey, TValue> dictionary)
        {
            m_dictionary = dictionary ?? throw new ArgumentNullException("dictionary");
        }

        /// <inheritdoc />
        public void CopyTo(TValue[] array, int index)
        {
            if (ReferenceEquals(array, null))
            {
                throw new ArgumentNullException("array");
            }

            if ((index < 0) || (index > array.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if ((array.Length - index) < m_dictionary.Count)
            {
                throw new ArgumentException();
            }

            int count = m_dictionary.m_count;
            var entries = m_dictionary.m_entries;
            for (int i = 0; i < count; i++)
            {
                var entry = entries[i];

                if (entry.Key.HashCode >= 0)
                {
                    array[index++] = entry.Value;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        void ICollection<TValue>.Add(TValue item)
        {
            throw new NotSupportedException();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        void ICollection<TValue>.Clear()
        {
            throw new NotSupportedException();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool ICollection<TValue>.Contains(TValue item)
        {
            return m_dictionary.ContainsValue(item);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool ICollection<TValue>.Remove(TValue item)
        {
            throw new NotSupportedException();
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return new Enumerator(m_dictionary);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<TValue>)this).GetEnumerator();
        }

        /// <inheritdoc />
        public int Count => m_dictionary.Count;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool ICollection<TValue>.IsReadOnly => true;

        /// <inheritdoc />
        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly FileMap<TKey, TValue> m_dictionary;
            private readonly int m_version;
            private int m_index;
            private TValue m_currentValue;

            /// <inheritdoc />
            public TValue Current => m_currentValue;

            object IEnumerator.Current
            {
                get
                {
                    CheckState();

                    return m_currentValue;
                }
            }

            internal Enumerator(FileMap<TKey, TValue> dictionary)
            {
                m_dictionary = dictionary;
                m_version = dictionary.m_version;
                m_index = 0;
                m_currentValue = default;
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                CheckVersion();

                while (m_index < m_dictionary.m_count)
                {
                    var dictionaryEntry = m_dictionary.m_entries[m_index];

                    if (dictionaryEntry.Key.HashCode >= 0)
                    {
                        m_currentValue = dictionaryEntry.Value;
                        m_index++;
                        return true;
                    }

                    m_index++;
                }

                m_index = m_dictionary.m_count + 1;
                m_currentValue = default;
                return false;
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            /// <inheritdoc />
            public void Dispose()
            {
            }

            void IEnumerator.Reset()
            {
                CheckVersion();

                m_index = 0;
                m_currentValue = default;
            }

            private void CheckVersion()
            {
                if (m_version != m_dictionary.m_version)
                {
                    throw new InvalidOperationException($"FileMap collection was modified during enumeration.");
                }
            }

            private void CheckState()
            {
                if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
                {
                    throw new InvalidOperationException("FileMap was modified during enumeration.");
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}