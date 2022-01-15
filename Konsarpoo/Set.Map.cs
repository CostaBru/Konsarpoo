using System;
using System.Collections.Generic;

namespace Konsarpoo.Collections
{
    public partial class Set<T>
    {
        /// <summary>
        /// Map API. Gets or sets value in set.
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        public T this[T key]
        {
            get
            {
                if (Contains(key))
                {
                    return key;
                }

                throw new KeyNotFoundException($"Key '{key}' is not found in set.");
            }
            set
            {
                Add(key);
            }
        }
        
        /// <summary>
        /// Map API. Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>True in case of success.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool TryGetValue(T key, out T value)
        {
            if (Contains(key))
            {
                value = key;

                return true;
            }

            value = default;
            
            return false;
        }
        
        
        /// <summary>
        /// Map API. Determines whether the Set&lt;T&gt; contains the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public bool ContainsKey(T key)
        {
            return Contains(key);
        }

        /// <summary>
        /// Map API. Attempts to add the specified value to the set.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool TryAdd(T key, T value)
        {
            if (Contains(key))
            {
                return false;
            }

            return Add(value);
        }
    }
}