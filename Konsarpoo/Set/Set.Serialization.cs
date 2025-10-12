using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;
using Konsarpoo.Collections.Data.Serialization;

namespace Konsarpoo.Collections
{
    public partial class Set<T> : IDataSerializable
    {
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

        private static readonly IDataAllocatorSetup<T> m_serializationAllocatorSetup =
            GcAllocatorSetup.GetDataPoolSetup<T>();
        
        /// <summary>
        /// Serializes the current instance to the provided <see cref="IDataSerializationInfo"/> implementation.
        /// </summary>
        /// <param name="info"></param>
        public virtual void SerializeTo([NotNull] IDataSerializationInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            
            var data = new Data<T>(m_serializationAllocatorSetup);
            data.Ensure(Count);
            CopyTo(data, 0, Count);

            var mapExtraInfo = new SetExtraInfo()
            {
                HashSize = m_buckets.Length,
                Comparer = m_comparer
            };
            
            using var memoryStream = (MemoryStream)SerializeHelper.Serialize(mapExtraInfo);
            
            info.SetExtraMetadata(memoryStream.ToArray());
            data.SerializeTo(info);
        }

        /// <summary>
        /// Deserializes the current instance from the provided <see cref="IDataSerializationInfo"/> implementation.
        /// </summary>
        /// <param name="info"></param>
        /// <exception cref="SerializationException"></exception>
        public void DeserializeFrom([NotNull] IDataSerializationInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            
            Clear();
            
            using var data = new Data<T>(m_serializationAllocatorSetup);
            data.DeserializeFrom(info);
            
            using var memoryStream = new MemoryStream(info.ExtraMetadata);
            var mapExtraInfo = SerializeHelper.Deserialize<SetExtraInfo>(memoryStream);
            
            m_comparer = mapExtraInfo.Comparer;
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