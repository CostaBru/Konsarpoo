using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Konsarpoo.Collections
{
    public partial class Set<T>
    {
        private const string CapacityName = "Capacity";
        private const string ComparerName = "Comparer";
        private const string VersionName = "Version";
        private const string BucketsName = "Buckets";
        private const string EntriesName = "Entries";
        
        [NonSerialized]
        private SerializationInfo m_siInfo;
        
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected Set(SerializationInfo info, StreamingContext context)
        {
            m_siInfo = info;
        }

        /// <inheritdoc />
        [SecurityCritical]
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            
            info.AddValue(ComparerName, m_comparer, typeof(IEqualityComparer<T>));
            info.AddValue(CapacityName, m_count);
            info.AddValue(VersionName, m_version);

            if(m_count > 0) 
            {
                info.AddValue(BucketsName, new Data<int>(m_buckets), typeof(Data<int>));
                info.AddValue(EntriesName, new Data<Slot>(m_slots), typeof(Data<Slot>));
            }
        }

        /// <summary>Implements the <see cref="T:System.Runtime.Serialization.ISerializable" /> interface and raises the deserialization event when the deserialization is complete.</summary>
        /// <param name="sender">The source of the deserialization event.</param>
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> object associated with the current <see cref="T:System.Collections.Generic.HashSet`1" /> object is invalid.</exception>
        public virtual void OnDeserialization(object sender)
        {
            var siInfo = m_siInfo;
            
            if (siInfo == null)
            {
                return;
            }

            int capacity = siInfo.GetInt32(CapacityName);

            m_comparer = (IEqualityComparer<T>)siInfo.GetValue(ComparerName, typeof(IEqualityComparer<T>));
            m_freeList = -1;

            if (capacity != 0)
            {
                var buckets = (Data<int>)siInfo.GetValue(BucketsName, typeof(Data<int>));
 
                if (buckets is null) 
                {
                    throw new SerializationException("Cannot read set buckets from serialization info.");
                }

                var entries = (Data<Slot>)siInfo.GetValue(EntriesName, typeof(Data<Slot>));
 
                if (entries is null) 
                {
                    throw new SerializationException("Cannot read set slots from serialization info.");
                }
                
                buckets.OnDeserialization(this);
                entries.OnDeserialization(this);
                
                m_buckets.AddRange(buckets);
                m_slots.AddRange(entries);

                m_count = capacity;
                m_lastIndex = m_count;
            }
            else
            {
                m_buckets.Clear();
                m_slots.Clear();
            }
            m_version = (ushort)siInfo.GetInt32(VersionName);
            m_siInfo = null;
        }
    }
}