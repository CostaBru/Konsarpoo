using System;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Konsarpoo.Collections
{
    public partial class Data<T>
    {
        private const string CapacityName = "Capacity";
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
            
            info.AddValue(CapacityName, Count);
            info.AddValue(VersionName, m_version);
          
            var array = new T[m_count];
            CopyTo(array, 0);
            info.AddValue(ElementsName, array, typeof(T[]));
        }
        
        /// <summary>Implements the <see cref="T:System.Runtime.Serialization.ISerializable" /> interface and raises the deserialization event when the deserialization is complete.</summary>
        /// <param name="sender">The source of the deserialization event.</param>
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> object associated with the current <see cref="T:System.Collections.Generic.HashSet`1" /> object is invalid.</exception>
        public virtual void OnDeserialization(object sender)
        {
            if (m_siInfo == null)
            {
                return;
            }

            int capacity = m_siInfo.GetInt32(CapacityName);

            if (capacity != 0)
            {
                T[] objArray = (T[])m_siInfo.GetValue(ElementsName, typeof(T[]));
                if (objArray == null)
                {
                    throw new SerializationException("Cannot read list values from serialization info.");
                }
                
                Ensure(capacity);
            
                for (int index = 0; index < objArray.Length && index < capacity; ++index)
                {
                    ValueByRef(index) = objArray[index];
                }
            }
         
            m_version = m_siInfo.GetInt32(VersionName);
            m_siInfo = null;
        }
    }
}