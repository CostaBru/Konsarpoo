using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Konsarpoo.Collections
{
    public partial class Map<TKey, TValue>
    {
        internal class HashHelpers
        {
            public static Dictionary<object, SerializationInfo> SerializationInfoTable = new ();
        } 
      
        // constants for serialization
        private const String VersionName = "Version";
        private const String HashSizeName = "HashSize";  // Must save buckets.Length
        private const String KeyValuePairsName = "KeyValuePairs";
        private const String ComparerName = "Comparer";
        
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected Map(SerializationInfo info, StreamingContext context) {
            //We can't do anything with the keys and values until the entire graph has been deserialized
            //and we have a resonable estimate that GetHashCode is not going to fail.  For the time being,
            //we'll just cache this.  The graph is not valid until OnDeserialization has been called.
            HashHelpers.SerializationInfoTable.Add(this, info);
        }

        /// <inheritdoc />
        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            
            if (info==null) 
            {
                throw new ArgumentNullException(nameof(info));
            }
            
            info.AddValue(VersionName, (int)m_version);
            info.AddValue(ComparerName, m_comparer, typeof(IEqualityComparer<TKey>));
            info.AddValue(HashSizeName, m_buckets.Length); //This is the length of the bucket array.
           
            if( m_buckets.m_count > 0) 
            {
                 var array = new KeyValuePair<TKey, TValue>[Count];
                 
                CopyTo(array, 0);
                
                info.AddValue(KeyValuePairsName, array, typeof(KeyValuePair<TKey, TValue>[]));
            }
        }

        /// <inheritdoc />
        public virtual void OnDeserialization(object sender) 
        {
            HashHelpers.SerializationInfoTable.TryGetValue(this, out var siInfo);
            
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
                m_buckets.Ensure(hashSize, -1);
                m_entries.Ensure(hashSize);
                
                m_freeList = -1;
 
               var array = (KeyValuePair<TKey, TValue>[])siInfo.GetValue(KeyValuePairsName, typeof(KeyValuePair<TKey, TValue>[]));
 
                if (array is null) 
                {
                    throw new SerializationException("Cannot read dict key values from serialization info.");
                }

                var add = true;
                
                for (int i = 0; i < array.Length; i++)
                {
                    var key = array[i].Key;
                    var value = array[i].Value;
                    
                    Insert(ref key, ref value, ref add);
                }
            }
            else 
            {
                m_buckets.Clear();
                m_entries.Clear();
            }
 
            m_version = (ushort)realVersion;
            HashHelpers.SerializationInfoTable.Remove(this);
        }
    }
}