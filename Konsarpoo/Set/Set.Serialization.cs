using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Konsarpoo.Collections.Data.Serialization;

namespace Konsarpoo.Collections
{
    public partial class Set<T>
    {
        private const string CapacityName = "Capacity";
        private const string ElementsName = "Elements";
        private const string ComparerName = "Comparer";
        private const string VersionName = "Version";
        
        [NonSerialized]
        private SerializationInfo m_siInfo;
        
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected Set(SerializationInfo info, StreamingContext context) : this(0, 0, null, null)
        {
            m_siInfo = info;
        }
        
        /// <inheritdoc />
        [System.Security.SecurityCritical]  // auto-generated_required
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            
            if (info == null) 
            {
                throw new ArgumentNullException(nameof(info));
            }
            
            SerializeTo(new DataMemorySerializationInfo(info));
        }

        [Serializable]
        private struct SetExtraInfo
        {
            public int HashSize;
            public IEqualityComparer<T> Comparer;
        }
        
        public void SerializeTo(IDataSerializationInfo dataSerializationInfo)
        {
            var data = new Data<T>();
            data.Ensure(Count);
            CopyTo(data, 0, Count);

            var mapExtraInfo = new SetExtraInfo()
            {
                HashSize = m_buckets.Length,
                Comparer = m_comparer
            };
            
            using var memoryStream = (MemoryStream)SerializeHelper.Serialize(mapExtraInfo);
            dataSerializationInfo.SetExtraMetadata(memoryStream.ToArray());
            data.SerializeTo(dataSerializationInfo);
        }

        public void DeserializeFrom(IDataSerializationInfo info)
        {
            using var data = new Data<T>();
            data.DeserializeFrom(info);
            using var memoryStream = new MemoryStream(info.ExtraMetadata);
            var mapExtraInfo = SerializeHelper.Deserialize<SetExtraInfo>(memoryStream);
            m_comparer = (IEqualityComparer<T>)mapExtraInfo.Comparer;
            if (mapExtraInfo.HashSize > 0)
            {
                m_buckets.Ensure(mapExtraInfo.HashSize, -1);
                m_slots.Ensure(mapExtraInfo.HashSize);
                
                m_freeList = -1;

                var add = true;
                
                foreach (var t in data)
                {
                    Add(t);
                }
            }
            else
            {
                m_buckets.Clear();
                m_slots.Clear();
            }

            m_version = data.m_version;
        }

        /// <inheritdoc />
        public virtual void OnDeserialization(object sender)
        {
            if (m_siInfo == null)
            {
                return;
            }

            DeserializeFrom(new DataMemorySerializationInfo(m_siInfo));

            m_siInfo = null;
        }
    }
}