using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// File-backed version of Data&lt;T&gt; 
    /// Data is persisted to disk and loaded on demand.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public partial class FileData<T> : Data<T>
    {
        private readonly string m_filename;
        private readonly DataFileSerialization m_arrayAccessor;
        private int m_arraysCount = 0;

        /// <summary>
        /// Creates a new FileData instance and maps it into an existing file.
        /// </summary>
        /// <param name="filename">The path to the file where data will be stored</param>
        public FileData(string filename)
        {
            m_filename = filename ?? throw new ArgumentNullException(nameof(filename));
            m_arrayAccessor = new DataFileSerialization(filename);
            
            var metadata = m_arrayAccessor.ReadMetadata();
            
            m_arraysCount = metadata.arraysCount;
            m_count = metadata.dataCount;
            m_version = metadata.version;
            m_maxSizeOfArray = metadata.maxSizeOfArray;
            
            CreateFromFile();
        }

        /// <summary>
        /// Creates a new FileData instance mapped into new file.
        /// </summary>
        /// <param name="filename">The path to the file where data will be stored</param>
        /// <param name="capacity">Initial capacity</param>
        /// <param name="maxSizeOfArray">Maximum size of array per node</param>
        /// <param name="arraysCapacity">Arrays capacity</param>
        /// <param name="allocatorSetup">Allocator setup</param>
        public FileData(string filename, int capacity = 0, int? maxSizeOfArray = null, int arraysCapacity = 1, IDataAllocatorSetup<T> allocatorSetup = null) 
            : base(capacity, maxSizeOfArray, allocatorSetup)
        {
            m_filename = filename ?? throw new ArgumentNullException(nameof(filename));
            m_arrayAccessor = new DataFileSerialization(filename, m_maxSizeOfArray, arraysCapacity);
        }
      
        /// <summary>
        /// Gets the filename used for file storage.
        /// </summary>
        public string FileName => m_filename;

        /// <summary>
        /// Begins writing to the file. All changes will be persisted after EndWrite.
        /// </summary>
        public void BeginWrite()
        {
            m_arrayAccessor.BeginWrite();
        }

        /// <summary>
        /// Persists all changes made since BeginWrite.
        /// </summary>
        public void EndWrite()
        {
            m_arrayAccessor.SetMetadata((m_maxSizeOfArray, m_count, m_version, m_arraysCount));
            
            m_arrayAccessor.EndWrite();
        }

        protected override void CreateFromArrays(IEnumerable<T[]> arrays, int totalCount)
        {
            if (arrays == null) throw new ArgumentNullException(nameof(arrays));
            if (totalCount < 0) throw new ArgumentOutOfRangeException(nameof(totalCount));
            
            int rest = totalCount;

            int prevArrayLen = int.MaxValue;
            
            foreach (var array in arrays)
            {
                var nodeSize = Math.Min(array.Length, rest);

                var closestValidArrayLen = 1 << (int)Math.Round(Math.Log(array.Length, 2));
                
                if (closestValidArrayLen != array.Length)
                {
                    throw new ArgumentException($"Array len:{array.Length} must be power of 2, but was not.");
                }
                
                if (m_root == null)
                {
                    m_maxSizeOfArray = array.Length;
                    prevArrayLen = array.Length;
                    
                    rest -= nodeSize;

                    var id = NewFileStoreId();

                    var storeNode = new FileStoreNode(NewFileStoreId, id, m_arrayAccessor, array, nodeSize);
                    
                    m_root = storeNode;
                    m_tailStoreNode = storeNode;
                    
                    continue;
                }

                if (prevArrayLen < array.Length)
                {
                    throw new ArgumentException($"The following array len:{array.Length} must be greater than or equal to former array length: {prevArrayLen}.");
                }

                INode node1 = m_root;
                INode node2;
                if (node1.AddArray(array, nodeSize, out node2, m_allocator) == false)
                {
                    m_root = new FileLinkNode(NewFileStoreId,(ushort)(node1.Level + 1), prevArrayLen, node1, m_allocator, node2);
                }
                
                prevArrayLen = array.Length;
                rest -= nodeSize;
            }
            
            m_arrayAccessor.Flush();

            m_count = totalCount;

            UpdateLastNode();
        }

        public override void Ensure(int size, T defaultValue = default)
        {
            if (m_count >= size)
            {
                return;
            }

            unchecked { ++m_version; }
            
            var maxSizeOfArray = m_maxSizeOfArray;

            if (m_root == null)
            {
                //common case
                var arrayAllocator = m_allocator.GetDataArrayAllocator();

                var id = NewFileStoreId();

                var nodeSize = Math.Min(maxSizeOfArray, size);

                var newArray = arrayAllocator.Rent(nodeSize);

                var storeNode = new FileStoreNode(NewFileStoreId, id, m_arrayAccessor, newArray, nodeSize);
               
                arrayAllocator.Return(newArray);
                
                int startIndex = 0;

                m_count = storeNode.Size;

                m_root = storeNode;
                m_tailStoreNode = storeNode;

                var setupDefaultValueForArray = EqualityComparer<T>.Default.Equals(defaultValue, Default) == false;

                if (setupDefaultValueForArray || arrayAllocator.CleanArrayReturn == false)
                {
                    var storage = storeNode.Storage;

                    Array.Fill(storage, defaultValue, startIndex, m_count - startIndex);
                    
                    storeNode.OnStorageDone(m_allocator);
                }

                var restSize = size - m_count;

                while (restSize > 0)
                {
                    INode node1 = m_root;
                    INode node2;
                    if (node1.Ensure(ref restSize, ref defaultValue, out node2, m_allocator) == false)
                    {
                        m_root = new FileLinkNode(NewFileStoreId, (ushort)(node1.Level + 1), maxSizeOfArray, node1, m_allocator, node2);
                    }
                }

                m_count = size;
                
                UpdateLastNode();

                return;
            }

            if (m_root != null)
            {
                var restSize = size - m_count;
                
                while (restSize > 0)
                {
                    INode node1 = m_root;
                    INode node2;
                    if (node1.Ensure(ref restSize, ref defaultValue, out node2, m_allocator) == false)
                    {
                        m_root = new FileLinkNode(NewFileStoreId, (ushort)(node1.Level + 1), maxSizeOfArray, node1, m_allocator, node2);
                    }
                }
            }

            m_arrayAccessor.Flush();
            
            m_count = size;

            UpdateLastNode();
        }

        protected override INode CreateRoot(ushort rootLevel, INode root, INode newNode)
        {
            return new FileLinkNode(NewFileStoreId, rootLevel, m_maxSizeOfArray, root, m_allocator, newNode);
        }

        protected override IStoreNode CreateNewStoreNode(int capacity)
        {
            var id = NewFileStoreId();

            return new FileStoreNode(NewFileStoreId, 
                id,
                m_arrayAccessor,
                m_allocator,
                m_maxSizeOfArray, 
                capacity);
        }

        protected int NewFileStoreId()
        {
            var arraysCount = m_arraysCount;
            
            m_arraysCount++;

            return arraysCount;
        }
        
        protected void CreateFromFile()
        {
            int rest = m_count;

            var arraysCount = m_arraysCount;

            //start id generator from 0;
            m_arraysCount = 0;
            
            for (int i = 0; i < arraysCount; i++)
            {
                if (m_root == null)
                {
                    var id = NewFileStoreId();

                    var size = Math.Min(m_count, m_maxSizeOfArray);

                    var storeNode = new FileStoreNode(NewFileStoreId, id, m_arrayAccessor, size, m_maxSizeOfArray);
                    
                    rest -= size;
                    
                    m_root = storeNode;
                    m_tailStoreNode = storeNode;
                    
                    continue;
                }

                var nsize = Math.Min(rest, m_maxSizeOfArray);
                
                INode node1 = m_root;
                INode node2;
                if (node1.AddArray(Array.Empty<T>(), nsize, out node2, m_allocator) == false)
                {
                    m_root = new FileLinkNode(NewFileStoreId,(ushort)(node1.Level + 1), m_maxSizeOfArray, node1, m_allocator, node2);
                }
                
                rest -= nsize;
            }

            UpdateLastNode();
        }

        /// <summary>
        /// Disposes the FileData instance and closes the mapped file.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_arrayAccessor?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void CreateFromList(Data<T> source)
        {
            if (source.m_root != null && (source.m_root is FileStoreNode || source.m_root is FileLinkNode))
            {
                if (source.m_root is FileStoreNode simpleNode)
                {
                    var storeNode = new FileStoreNode(NewFileStoreId, simpleNode, m_allocator.GetDataArrayAllocator());
                    
                    m_root = storeNode;

                    m_tailStoreNode = storeNode;
                    
                    m_count = source.m_count;
                }
                else if (source.m_root is FileLinkNode linkNode)
                {
                    m_root = new FileLinkNode(NewFileStoreId, linkNode, m_allocator);
                    
                    m_count = source.m_count;
                    
                    UpdateLastNode();
                }
            }
            else
            {
                AddRange(source);
            }
        }
    }
}
