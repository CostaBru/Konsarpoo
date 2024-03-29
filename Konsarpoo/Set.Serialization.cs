﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

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
        [SecurityCritical]
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            
            info.AddValue(ComparerName, m_comparer, typeof(IEqualityComparer<T>));
            info.AddValue(CapacityName, m_buckets?.Count ?? 0);
            info.AddValue(VersionName, m_version);

            if (m_count == 0)
            {
                return;
            }

            var data = new Data<T>();
            data.Ensure(Count);
            CopyTo(data);
            info.AddValue(ElementsName, data, typeof(Data<T>));
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

            m_comparer = (IEqualityComparer<T>)m_siInfo.GetValue(ComparerName, typeof(IEqualityComparer<T>));
            m_freeList = -1;

            if (capacity != 0)
            {
                var data = (Data<T>)m_siInfo.GetValue(ElementsName, typeof(Data<T>));
                if (data == null)
                {
                    throw new SerializationException("Cannot read set values from serialization info.");
                }
                
                m_buckets.Ensure(capacity);
                m_slots.Ensure(capacity);
            
                data.OnDeserialization(this);
                AddRange(data);
            }
            else
            {
                m_buckets.Clear();
                m_slots.Clear();
            }
            m_version = (ushort)m_siInfo.GetInt32(VersionName);
            m_siInfo = null;
        }
    }
}