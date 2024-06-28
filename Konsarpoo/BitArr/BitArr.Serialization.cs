using System;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Konsarpoo.Collections
{
    [Serializable]
    [KnownType(typeof(Data<int>))]
    public partial class BitArr : ISerializable, IDeserializationCallback
    {
        private const string CapacityName = "Capacity";
        private const string ElementsName = "Elements";
        private const string VersionName = "Version";
        
        [NonSerialized]
        private SerializationInfo m_siInfo;
        
        private BitArr(SerializationInfo info, StreamingContext context)
        {
            m_siInfo = info;
        }

        /// <summary>
        /// Populates a SerializationInfo with the data needed to serialize the target object.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        [SecurityCritical]
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            
            info.AddValue(CapacityName, Count);
            info.AddValue(VersionName, m_version);
            info.AddValue(ElementsName, new Data<int>(m_array), typeof(Data<int>));
        }
        
        /// <summary>Implements the <see cref="T:System.Runtime.Serialization.ISerializable" /> interface and raises the deserialization event when the deserialization is complete.</summary>
        /// <param name="sender">The source of the deserialization event.</param>
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> object associated with the current <see cref="T:System.Collections.Generic.HashSet`1" /> object is invalid.</exception>
        public void OnDeserialization(object sender)
        {
            var siInfo = m_siInfo;
            
            if (siInfo == null)
            {
                return;
            }

            int capacity = siInfo.GetInt32(CapacityName);

            if (capacity != 0)
            {
                var data = (Data<int>)siInfo.GetValue(ElementsName, typeof(Data<int>));
                if (data == null)
                {
                    throw new SerializationException("Cannot read BitArr values from serialization info.");
                }
                
                data.OnDeserialization(this);

                m_array = data;
            }

            m_version = siInfo.GetInt32(VersionName);
            m_length = (uint)capacity;
            m_siInfo = null;
        }
    }
}