using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Konsarpoo.Collections
{
    public partial class Map<TKey, TValue>
    {
        // constants for serialization
        private const string VersionName = "Version";
        private const string HashSizeName = "HashSize";  // Must save buckets.Length
        private const string KeyValuePairsName = "KeyValuePairs";
        private const string ComparerName = "Comparer";

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
            
            info.AddValue(VersionName, (int)m_version);
            info.AddValue(ComparerName, m_comparer, typeof(IEqualityComparer<TKey>));
            info.AddValue(HashSizeName, m_buckets.Length); //This is the length of the bucket array.
           
            if( m_buckets.m_count > 0) 
            {
                 var data = new Data<KeyValuePair<TKey, TValue>>();
                 data.Ensure(Count);
                 CopyTo(data, 0);
                 info.AddValue(KeyValuePairsName, data, typeof(Data<KeyValuePair<TKey, TValue>>));
            }
        }

        /// <inheritdoc />
        public virtual void OnDeserialization(object sender)
        {
            var siInfo = m_sInfo;
            
            if (siInfo is null) 
            {
                // It might be necessary to call OnDeserialization from a container if the container object also implements
                // OnDeserialization. However, remoting will call OnDeserialization again.
                // We can return immediately if this function is called twice. 
                // Note we set remove the serialization info from the table at the end of this method.
                return;
            }            
            
            int realVersion = siInfo.GetInt32(VersionName);
            int hashSize = siInfo.GetInt32(HashSizeName);
            
            m_comparer   = (IEqualityComparer<TKey>)siInfo.GetValue(ComparerName, typeof(IEqualityComparer<TKey>));
            
            if(hashSize != 0)
            {
                var data = (Data<KeyValuePair<TKey, TValue>>)siInfo.GetValue(KeyValuePairsName, typeof(Data<KeyValuePair<TKey, TValue>>));
 
                if (data is null) 
                {
                    throw new SerializationException("Cannot read dict key values from serialization info.");
                }

                data.OnDeserialization(this);
                
                m_buckets.Ensure(hashSize, -1);
                m_entries.Ensure(hashSize);
                
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
 
            m_version = (ushort)realVersion;
            m_sInfo = null;
        }
    }
}