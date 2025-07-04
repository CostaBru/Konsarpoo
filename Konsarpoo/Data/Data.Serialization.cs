using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    public partial class Data<T>
    {
        [NonSerialized]
        private SerializationInfo m_siInfo;
        
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected Data(SerializationInfo info, StreamingContext context) : this()
        {
            m_siInfo = info;
        }

        /// <summary>
        /// Populates a SerializationInfo with the data needed to serialize the target object.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        [SecurityCritical]
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            SerializeTo(new DataMemorySerializationInfo(info));
        }
        
        /// <summary>Implements the <see cref="T:System.Runtime.Serialization.ISerializable" /> interface and raises the deserialization event when the deserialization is complete.</summary>
        /// <param name="sender">The source of the deserialization event.</param>
        public virtual void OnDeserialization(object sender)
        {
            if (m_siInfo == null)
            {
                return;
            }

            DeserializeFrom(new DataMemorySerializationInfo(m_siInfo));

            m_siInfo = null;
        }
        
        public void SerializeTo(IDataSerializationInfo info)
        {
            if (m_root is null)
            {
                info.WriteMetaData((m_maxSizeOfArray, m_count, m_version, 1));
                info.WriteSingleArray(Array.Empty<T>());
                return;
            }
            
            if (m_root is StoreNode st)
            {
                info.WriteMetaData((m_maxSizeOfArray, m_count, m_version, 1));
                info.WriteSingleArray(st.m_items);
            }
            else
            {
                var storeNodes = GetStoreNodes(m_root).ToData();
                
                info.WriteMetaData((m_maxSizeOfArray, m_count, m_version, storeNodes.Count));
                foreach (var storeNode in storeNodes)
                {
                    info.AppendArray(storeNode.m_items);
                }
                
                storeNodes.Dispose();
            }
        }

        private void CreateFromArrays([NotNull] IEnumerable<T[]> arrays, int totalCount)
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

                    m_root = new StoreNode(null, m_arrayAllocator, array, nodeSize);
                    continue;
                }

                if (prevArrayLen < array.Length)
                {
                    throw new ArgumentException($"The following array len:{array.Length} must be greater than or equal to former array length: {prevArrayLen}.");
                }

                INode node1 = m_root;
                INode node2;
                if (node1.AddArray(array, nodeSize, out node2) == false)
                {
                    m_root = new LinkNode(null, node1.Level + 1, array.Length, node1, m_nodesAllocator, node2);

                    node1.Parent = m_root;
                    node2.Parent = m_root;
                }
                
                prevArrayLen = array.Length;
                rest -= nodeSize;
            }

            m_count = totalCount;
        }

        public void DeserializeFrom(IDataSerializationInfo info)
        {
            var (maxSizeOfArray, dataCount, version, elementsCount) = info.ReadMetaData();
            
            m_maxSizeOfArray = maxSizeOfArray;

            if (dataCount != 0 && elementsCount != 0)
            {
                IEnumerable<T[]> arrays;
                
                if (elementsCount == 1)
                {
                    T[] objArray = info.ReadSingleArray<T>();
                    if (objArray == null)
                    {
                        throw new SerializationException("Cannot read list values from serialization info.");
                    }

                    arrays = new[] { objArray };
                }
                else
                {
                    arrays = ReadArrays(info, elementsCount);
                }
                
                CreateFromArrays(arrays, dataCount);
            }

            unchecked
            {
                m_version = (byte)version;
            }
        }

        private IEnumerable<T[]> ReadArrays(IDataSerializationInfo info, int count)
        {
            for (int j = 0; j < count; j++)
            {
                var array = info.ReadArray<T>(j);
                
                if (array == null)
                {
                    throw new SerializationException("Cannot read list values from serialization info.");
                }
                
                yield return array;
            }
        }

        private IEnumerable<StoreNode> GetStoreNodes(INode node)
        {
            foreach (var nodeNode in node.Nodes)
            {
                if (nodeNode is StoreNode sn)
                {
                    yield return sn;
                }
                else
                {
                    foreach (var storeNode in GetStoreNodes(nodeNode))
                    {
                        yield return storeNode;
                    }
                }
            }
        } 
    }
}