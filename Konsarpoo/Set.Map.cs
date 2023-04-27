using System;
using System.Collections.Generic;

namespace Konsarpoo.Collections
{
    public partial class Set<T>
    {
        /// <summary>
        /// Set Map API &lt;TKey, bool&gt;. Gets or sets value in set.
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        public bool this[T key]
        {
            get
            {
                return Contains(key);
            }
            set
            {
                if (value)
                {
                    Add(key);
                }
                else
                {
                    Remove(key);
                }
            }
        }
        
        /// <summary>
        /// Set Map API &lt;TKey, bool&gt; Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>True in case of success.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool TryGetValue(T key, out bool value)
        {
            if (Contains(key))
            {
                value = true;

                return true;
            }

            value = default;
            
            return false;
        }
        
        
        /// <summary>
        /// Set Map API &lt;TKey, bool&gt;  Determines whether the Set&lt;T&gt; contains the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public bool ContainsKey(T key)
        {
            return Contains(key);
        }

        /// <summary>
        /// Set Map API &lt;TKey, bool&gt;  Attempts to add the specified value to the set.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool TryAdd(T key, bool value)
        {
            if (value)
            {
                if (Contains(key))
                {
                    return false;
                }

                return Add(key);
            }

            return false;
        }
    }
}