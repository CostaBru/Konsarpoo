using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Konsarpoo.Collections
{
    public partial class Data<T>
    {
        private const string CapacityName = "Capacity";
        private const string NodeCapacityName = "NodeCapacity";
        private const string ElementsCountName = "ElementsCount";
        private const string ElementsName = "Elements";
        private const string VersionName = "Version";
        
        [NonSerialized]
        private SerializationInfo m_siInfo;
        
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected Data(SerializationInfo info, StreamingContext context)
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
            
            info.AddValue(NodeCapacityName, m_maxSizeOfArray);
            info.AddValue(CapacityName, m_count);
            info.AddValue(VersionName, m_version);

            if (m_root is StoreNode st)
            {
                info.AddValue(ElementsCountName, 1);
                
                var array = new T[m_count];
                Array.Copy(st.m_items, 0, array, 0, m_count);
                info.AddValue(ElementsName, array, typeof(T[]));
            }
            else
            {
                var storeNodes = GetStoreNodes(m_root).ToData();

                info.AddValue(ElementsCountName, storeNodes.m_count);

                int i = 0;
                
                foreach (var storeNode in storeNodes)
                {
                    var array = new T[storeNode.Size];
                    Array.Copy(storeNode.m_items, 0, array, 0, storeNode.Size);
                    
                    var elementsName = GetElementName(i);
                    info.AddValue(elementsName, array, typeof(T[]));

                    i++;
                }
                
                storeNodes.Dispose();
            }
        }

        private string GetElementName(int index)
        {
            if (index < m_predefinedElementsName.Length)
            {
                return m_predefinedElementsName[index];
            }

            return ElementsName + index;
        }

        private static readonly string[] m_predefinedElementsName = Enumerable.Range(0, 100).Select(i => ElementsName + i).ToArray();

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
            
        
        /// <summary>Implements the <see cref="T:System.Runtime.Serialization.ISerializable" /> interface and raises the deserialization event when the deserialization is complete.</summary>
        /// <param name="sender">The source of the deserialization event.</param>
        public virtual void OnDeserialization(object sender)
        {
            if (m_siInfo == null)
            {
                return;
            }

            m_maxSizeOfArray = m_siInfo.GetInt32(NodeCapacityName);
            
            int capacity = m_siInfo.GetInt32(CapacityName);
            
            int elementsCount = m_siInfo.GetInt32(ElementsCountName);

            if (capacity != 0 && elementsCount != 0)
            {
                Ensure(capacity);
                
                if (elementsCount == 1)
                {
                    T[] objArray = (T[])m_siInfo.GetValue(ElementsName, typeof(T[]));
                    if (objArray == null)
                    {
                        throw new SerializationException("Cannot read list values from serialization info.");
                    }

                    var storeNode = (StoreNode)m_root;

                    Array.Copy(objArray, 0, storeNode.m_items, 0, objArray.Length);
                }
                else
                {
                    var storeNodes = GetStoreNodes(m_root);

                    int i = 0;
                    foreach (var storeNode in storeNodes)
                    {
                        var elementName = GetElementName(i);

                        T[] objArray = (T[])m_siInfo.GetValue(elementName, typeof(T[]));
                        if (objArray == null)
                        {
                            throw new SerializationException($"Cannot read list values from serialization info for {i}th array.");
                        }

                        Array.Copy(objArray, 0, storeNode.m_items, 0, objArray.Length);
                        
                        i++;
                    }
                }
            }

            unchecked
            {
                m_version = (byte)m_siInfo.GetInt32(VersionName);
            }

            m_siInfo = null;
        }
    }
}