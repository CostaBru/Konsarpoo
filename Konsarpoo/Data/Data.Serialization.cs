using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using Konsarpoo.Collections.Data.Serialization;

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
                info.WriteMetadata((m_maxSizeOfArray, m_count, m_version, 1));
                info.WriteSingleArray(Array.Empty<T>());
                return;
            }

            if (m_root.Storage != null)
            {
                info.WriteMetadata((m_maxSizeOfArray, m_count, m_version, 1));
                info.WriteSingleArray(m_root.Storage);
            }
            else
            {
                var storeNodes = GetArrays(m_root).ToArray();
                
                info.WriteMetadata((m_maxSizeOfArray, m_count, m_version, storeNodes.Length));
                foreach (var storeNode in storeNodes)
                {
                    info.AppendArray(storeNode);
                }
            }
        }


        public void DeserializeFrom(IDataSerializationInfo info)
        {
            var (maxSizeOfArray, dataCount, version, elementsCount) = info.ReadMetadata();
            
            m_maxSizeOfArray = (ushort)maxSizeOfArray;

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

        private IEnumerable<T[]> GetArrays(INode node)
        {
            foreach (var nodeNode in node.Nodes)
            {
                var storage = nodeNode.Storage;
                
                if (storage != null)
                {
                    yield return storage;
                }
                else
                {
                    foreach (var storeNode in GetArrays(nodeNode))
                    {
                        yield return storeNode;
                    }
                }
            }
        } 
    }
}