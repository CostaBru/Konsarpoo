using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

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
            if (m_root is StoreNode st)
            {
                info.WriteMetaData((m_maxSizeOfArray, m_count, m_version, 1));
                info.WriteSingleArray(st.m_items);
            }
            else
            {
                var storeNodes = GetStoreNodes(m_root).ToData();
                
                info.WriteMetaData((m_maxSizeOfArray, m_count, m_version, storeNodes.m_count));
                int i = 0;
                foreach (var storeNode in storeNodes)
                {
                    info.WriteArray(i, storeNode.m_items);
                    i++;
                }
                
                storeNodes.Dispose();
            }
        }

        public void DeserializeFrom(IDataSerializationInfo info)
        {
            var (maxSizeOfArray, dataCount, version, elementsCount) = info.ReadMetaData();
            
            m_maxSizeOfArray = maxSizeOfArray;

            if (dataCount != 0 && elementsCount != 0)
            {
                Ensure(dataCount);
                
                if (elementsCount == 1)
                {
                    T[] objArray = info.ReadSingleArray<T>();
                    if (objArray == null)
                    {
                        throw new SerializationException("Cannot read list values from serialization info.");
                    }
                    var storeNode = (StoreNode)m_root;
                    storeNode.ReturnArray();
                    storeNode.m_items = objArray;
                }
                else
                {
                    var storeNodes = GetStoreNodes(m_root);

                    int i = 0;
                    foreach (var storeNode in storeNodes)
                    {
                        T[] objArray = info.ReadArray<T>(i);
                        if (objArray == null)
                        {
                            throw new SerializationException($"Cannot read list values from serialization info for {i}th array.");
                        }
                        storeNode.ReturnArray();
                        storeNode.m_items = objArray;
                        i++;
                    }
                }
            }

            unchecked
            {
                m_version = (byte)version;
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