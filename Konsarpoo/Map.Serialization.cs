using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Konsarpoo.Collections
{
    public partial class Map<TKey, TValue>
    {
        // constants for serialization
        private const string VersionName = "Version";
        private const string CapacityName = "Capacity";
        private const string BucketsName = "Buckets";
        private const string EntriesName = "Entries";
        private const string ComparerName = "Comparer";

        private SerializationInfo m_siInfo;
        
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected Map(SerializationInfo info, StreamingContext context) {
            //We can't do anything with the keys and values until the entire graph has been deserialized
            //and we have a resonable estimate that GetHashCode is not going to fail.  For the time being,
            //we'll just cache this.  The graph is not valid until OnDeserialization has been called.
            m_siInfo = info;
        }

        /// <inheritdoc />
        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            
            if (info == null) 
            {
                throw new ArgumentNullException(nameof(info));
            }
            
            info.AddValue(VersionName, (int)m_version);
            info.AddValue(ComparerName, m_comparer, typeof(IEqualityComparer<TKey>));
            info.AddValue(CapacityName, m_count); //This is the length of the bucket array.
           
            if(m_count > 0) 
            {
                info.AddValue(BucketsName, new Data<int>(m_buckets), typeof(Data<int>));
                info.AddValue(EntriesName, new Data<Entry>(m_entries), typeof(Data<Entry>));
            }
        }

        /// <inheritdoc />
        public virtual void OnDeserialization(object sender)
        {
            var siInfo = m_siInfo;
            
            if (siInfo is null) 
            {
                // It might be necessary to call OnDeserialization from a container if the container object also implements
                // OnDeserialization. However, remoting will call OnDeserialization again.
                // We can return immediately if this function is called twice. 
                // Note we set remove the serialization info from the table at the end of this method.
                return;
            }            
            
            int realVersion = siInfo.GetInt32(VersionName);
            int count = siInfo.GetInt32(CapacityName);
            
            m_comparer   = (IEqualityComparer<TKey>)siInfo.GetValue(ComparerName, typeof(IEqualityComparer<TKey>));
            
            if(count != 0)
            {
                m_freeList = -1;
 
                var buckets = (Data<int>)siInfo.GetValue(BucketsName, typeof(Data<int>));
 
                if (buckets is null) 
                {
                    throw new SerializationException("Cannot read dict buckets from serialization info.");
                }

                var entries = (Data<Entry>)siInfo.GetValue(EntriesName, typeof(Data<Entry>));
 
                if (entries is null) 
                {
                    throw new SerializationException("Cannot read dict entries from serialization info.");
                }
                
                buckets.OnDeserialization(this);
                entries.OnDeserialization(this);
                
                m_buckets.AddRange(buckets);
                m_entries.AddRange(entries);

                m_count = count;
            }
            else 
            {
                m_buckets.Clear();
                m_entries.Clear();
            }
 
            m_version = (ushort)realVersion;
        }
    }
}