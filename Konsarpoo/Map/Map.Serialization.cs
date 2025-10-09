using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Konsarpoo.Collections.Data.Serialization;

namespace Konsarpoo.Collections
{
    public partial class Map<TKey, TValue>
    {

        private SerializationInfo m_sInfo;
        
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected Map(SerializationInfo info, StreamingContext context) : this(0,0, null)
        {
            m_sInfo = info;
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
        private struct MapExtraInfo
        {
            public int HashSize;
            public IEqualityComparer<TKey> Comparer;
        }
        
        public void SerializeTo(IDataSerializationInfo dataSerializationInfo)
        {
            var data = new Data<KeyValuePair<TKey, TValue>>();
            data.Ensure(Count);
            CopyTo(data, 0);

            var mapExtraInfo = new MapExtraInfo()
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
            using var data = new Data<KeyValuePair<TKey, TValue>>();
            data.DeserializeFrom(info);
            using var memoryStream = new MemoryStream(info.ExtraMetadata);
            var mapExtraInfo = SerializeHelper.Deserialize<MapExtraInfo>(memoryStream);
            m_comparer = (IEqualityComparer<TKey>)mapExtraInfo.Comparer;
            if (mapExtraInfo.HashSize > 0)
            {
                m_buckets.Ensure(mapExtraInfo.HashSize, -1);
                m_entries.Ensure(mapExtraInfo.HashSize);
                
                m_freeList = -1;

                var add = true;
                
                foreach (var t in data)
                {
                    var key = t.Key;
                    var value = t.Value;
                    
                    Insert(ref key, ref value, ref add);
                }
            }
            else
            {
                m_buckets.Clear();
                m_entries.Clear();
            }

            m_version = data.m_version;
        }

        /// <inheritdoc />
        public virtual void OnDeserialization(object sender)
        {
            if (m_sInfo == null)
            {
                return;
            }

            DeserializeFrom(new DataMemorySerializationInfo(m_sInfo));

            m_sInfo = null;
        }
    }
}