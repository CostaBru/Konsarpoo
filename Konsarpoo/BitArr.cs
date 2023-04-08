using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;

namespace Konsarpoo.Collections
{
    /// <summary>Manages a compact array of bit values, which are represented as Booleans, where <see langword="true" /> indicates that the bit is on (1) and <see langword="false" /> indicates the bit is off (0).</summary>
    [DebuggerDisplay("Length {m_length}")]
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    public sealed partial class BitArr : ICollection, IEnumerable, ICloneable, IEnumerable<bool>, IReadOnlyList<bool>, IDisposable, IReadOnlyCollection<bool>
    {
        private bool EqualsList(IReadOnlyList<bool> other)
        {
            if (Length == other.Count)
            {
                for (int i = 0; i < Length ; i++)
                {
                    if (!(EqualityComparer<bool>.Default.Equals(this[i], other[i])))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
        
        public bool All(Func<bool, bool> predicate)
        {
            foreach (bool element in this)
            {
                if (!predicate(element))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }
            
            return EqualsList(((BitArr) obj));
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Length ^ 3;

                foreach (var item in m_array)
                {
                    hashCode = (hashCode * 397) ^ EqualityComparer<int>.Default.GetHashCode(item);
                }
              
                return hashCode;
            }
        }
        
        private const int BitsPerInt32 = 32;
        private const int BytesPerInt32 = 4;
        private const int BitsPerByte = 8;

        [NonSerialized]
        private Data<int> m_array;

        [NonSerialized]
        private uint m_length;
        
        [NonSerialized]
        private int m_version;

        /// <summary>Initializes a new instance of the BitArr class that can hold the specified number of bit values, which are initially set to <see langword="false" />.</summary>
        /// <param name="length">The number of bit values in the new BitArr. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="length" /> is less than zero. </exception>
        public BitArr(int length)
            : this(length, false)
        {
        }

        /// <summary>Initializes a new instance of the BitArr class that can hold the specified number of bit values, which are initially set to the specified value.</summary>
        /// <param name="length">The number of bit values in the new BitArr. </param>
        /// <param name="defaultValue">The Boolean value to assign to each bit. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="length" /> is less than zero. </exception>
        public BitArr(int length, bool defaultValue)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var capacity = (int)GetArrayLength((uint) length, BitsPerInt32);

            m_array = new Data<int>();

            int num = defaultValue ? -1 : 0;

            m_array.Ensure(capacity, num);

            m_length = (uint) length;
        }

        /// <summary>Initializes a new instance of the BitArr class that contains bit values copied from the specified array of Booleans.</summary>
        /// <param name="values">An array of Booleans to copy. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="values" /> is <see langword="null" />. </exception>
        public BitArr(IReadOnlyCollection<bool> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            CreateFromBoolArr(values);
        }

        private void CreateFromBoolArr(IReadOnlyCollection<bool> values)
        {
            var capacity = (int)GetArrayLength((uint)values.Count, BitsPerInt32);

            m_array = new Data<int>(capacity);
            m_array.Ensure(capacity);

            m_length = (uint)values.Count;

            var index = 0;

            foreach (var value in values)
            {
                if (value)
                {
                    var valueByRef = m_array.ValueByRef(index / BitsPerInt32);

                    m_array.ValueByRef(index / BitsPerInt32) = valueByRef | (1 << index % BitsPerInt32);
                }

                index++;
            }
        }

        /// <summary>Initializes a new instance of the BitArr class that contains bit values copied from the specified array of 32-bit integers.</summary>
        /// <param name="values">An array of integers containing the values to copy, where each integer represents 32 consecutive bits. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="values" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ArgumentException">The length of <paramref name="values" /> is greater than <see cref="F:System.Int32.MaxValue" /></exception>
        public BitArr(IReadOnlyList<int> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            m_array = new Data<int>(values);
            m_length = (uint) (values.Count * BitsPerInt32);
        }

        /// <summary>Initializes a new instance of the BitArr class that contains bit values copied from the specified BitArr.</summary>
        /// <param name="bits">The BitArr to copy. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="bits" /> is <see langword="null" />. </exception>
        public BitArr(BitArr bits)
        {
            if (bits == null)
            {
                throw new ArgumentNullException(nameof(bits));
            }

            m_array = new Data<int>(bits.m_array);
            m_length = bits.m_length;
        }

        /// <summary>
        /// Destructor called by GC. Shouldn't be called if instance is properly disposed beforehand.
        /// </summary>
        ~BitArr()
        {
            Clear();
        }

        /// <summary>
        /// Clears BitArr.
        /// </summary>
        public void Clear()
        {
            m_version++;
            m_length = 0;
            m_array?.Clear();
        }

        /// <summary>Gets or sets the value of the bit at a specific position in the BitArr.</summary>
        /// <param name="index">The zero-based index of the value to get or set. </param>
        /// <returns>The value of the bit at position <paramref name="index" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is less than zero.-or-
        /// <paramref name="index" /> is equal to or greater than <see cref="P:System.Collections.BitArray.Count" />. </exception>
        public bool this[int index]
        {
            get => Get(index);
            set => Set(index, value);
        }

        /// <summary>Gets the value of the bit at a specific position in the BitArr.</summary>
        /// <param name="index">The zero-based index of the value to get. </param>
        /// <returns>The value of the bit at position <paramref name="index" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is less than zero.-or-
        /// <paramref name="index" /> is greater than or equal to the number of elements in the BitArr. </exception>
        public bool Get(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Given index is negative.");
            }

            if (index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Given index {index} is larger than capacity of BitAr {Length}.");
            }
            
            if (m_array.m_root?.Storage != null)
            {
                return (uint) (m_array.m_root.Storage[index / BitsPerInt32] & 1 << index % BitsPerInt32) > 0U;
            }
            else
            {
                return (uint) (m_array.ValueByRef(index / BitsPerInt32) & 1 << index % BitsPerInt32) > 0U;
            }
        }

        /// <summary>
        /// Check that the value exists and set for given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public bool HasAndSet(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Given index is negative.");
            }

            if (m_array is null)
            {
                return false;
            }
            
            if (index >= Length)
            {
                return false;
            }
            
            if (m_array.m_root?.Storage != null)
            {
                return (uint) (m_array.m_root.Storage[index / BitsPerInt32] & 1 << index % BitsPerInt32) > 0U;
            }
            else
            {
                return (uint)(m_array.ValueByRef(index / BitsPerInt32) & 1 << index % BitsPerInt32) > 0U;
            }
        }

        /// <summary>Sets the bit at a specific position in the BitArr to the specified value.</summary>
        /// <param name="index">The zero-based index of the bit to set. </param>
        /// <param name="value">The Boolean value to assign to the bit. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is less than zero.-or-
        /// <paramref name="index" /> is greater than or equal to the number of elements in the BitArr. </exception>
        public void Set(int index, bool value)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Given index is negative.");
            }

            if (index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Given index {index} is larger than capacity of BitAr {Length}.");
            }

            EnsureStorage();

            if (m_array.m_root?.Storage != null)
            {
                int newVal = 0;

                var items = m_array.m_root.Storage;
                
                if (value)
                {
                    newVal = items[(index / BitsPerInt32)] | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = items[(index / BitsPerInt32)] & ~(1 << index % BitsPerInt32);
                }

                items[(index / BitsPerInt32)] = newVal;
            }
            else
            {
                var valueByRef = m_array.ValueByRef(index / BitsPerInt32);

                int newVal = 0;

                if (value)
                {
                    newVal = valueByRef | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = valueByRef & ~(1 << index % BitsPerInt32);
                }

                m_array.ValueByRef(index / BitsPerInt32) = newVal;
            }
        }

        private void EnsureStorage()
        {
            if (m_array == null)
            {
                m_array = new Data<int>();

                if (m_length > 0)
                {
                    m_array.Ensure((int)GetArrayLength(m_length, BitsPerInt32));
                }
            }
        }

        /// <summary>
        /// Tries to set value at given index. If size is greater skips.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public bool TrySet(int index, bool value)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Given index is negative.");
            }

            if (m_array == null)
            {
                return false;
            }

            if (index >= m_length)
            {
                return false;
            }

            if (m_array.m_root?.Storage != null)
            {
                int newVal = 0;

                var items = m_array.m_root.Storage;
                if (value)
                {
                    newVal = items[(index / BitsPerInt32)] | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = items[(index / BitsPerInt32)] & ~(1 << index % BitsPerInt32);
                }

                items[(index / BitsPerInt32)] = newVal;
            }
            else
            {
                var valueByRef = m_array.ValueByRef(index / BitsPerInt32);

                int newVal = 0;

                if (value)
                {
                    newVal = valueByRef | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = valueByRef & ~(1 << index % BitsPerInt32);
                }

                m_array.ValueByRef(index / BitsPerInt32) = newVal;
            }
          
            return true;
        }

        
        /// <summary>
        /// Set given value at given index or increase internal capacity to be able do this and set the value.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetOrAdd(int index, bool value)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Given index is negative.");
            }
            
            EnsureStorage();

            if (index >= m_length)
            {
                Length = index + 1;
            }

            if (m_array.m_root?.Storage != null)
            {
                int newVal = 0;

                var items = m_array.m_root.Storage;
                if (value)
                {
                    newVal = items[index / BitsPerInt32] | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = items[index / BitsPerInt32] & ~(1 << index % BitsPerInt32);
                }

                items[index / BitsPerInt32] = newVal;
            }
            else
            {
                var valueByRef = m_array.ValueByRef(index / BitsPerInt32);

                int newVal = 0;

                if (value)
                {
                    newVal = valueByRef | (1 << index % BitsPerInt32);
                }
                else
                {
                    newVal = valueByRef & ~(1 << index % BitsPerInt32);
                }

                m_array.ValueByRef(index / BitsPerInt32) = newVal;
            }
        }

        /// <summary>Sets all bits in the BitArr to the specified value.</summary>
        /// <param name="value">The Boolean value to assign to all bits. </param>

        public void SetAll(bool value)
        {
            int num = value ? -1 : 0;
            
            EnsureStorage();
            
            var arrayLength = GetArrayLength(m_length, BitsPerInt32);

            if (m_array.m_root?.Storage != null)
            {
                var items = m_array.m_root.Storage;
                
                for (int index = 0; index < arrayLength && index <  items.Length; ++index)
                {
                    items[index] = num;
                }
            }
            else
            {
                for (int index = 0; index < arrayLength; ++index)
                {
                    m_array.ValueByRef(index) = num;
                }
            }
        }

        /// <summary>
        /// Performs the bitwise AND operation between the elements of the current BitArr object and the corresponding elements in the specified array. The current BitArr object will be modified to store the result of the bitwise AND operation.</summary>
        /// <param name="value">The array with which to perform the bitwise AND operation. </param>
        /// <returns>An array containing the result of the bitwise AND operation, which is a reference to the current BitArr object. </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="value" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="value" /> and the current BitArr do not have the same number of elements. </exception>

        public BitArr And(BitArr value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (m_length != value.m_length)
            {
                throw new ArgumentException(nameof(value),$"Argument array length {value.m_length} is not equal to {m_length}.");
            }
            
            if (m_array == null)
            {
                return this;
            }

            var arrayLength = GetArrayLength(m_length, BitsPerInt32);
            for (int index = 0; index < arrayLength; ++index)
            {
                var valueByRef = value.m_array.ValueByRef(index);

                m_array.ValueByRef(index) &= valueByRef;
            }

            return this;
        }

        /// <summary>
        /// Performs the bitwise OR operation between the elements of the current BitArr object and the corresponding elements in the specified array. The current BitArr object will be modified to store the result of the bitwise OR operation.</summary>
        /// <param name="value">The array with which to perform the bitwise OR operation. </param>
        /// <returns>An array containing the result of the bitwise OR operation, which is a reference to the current BitArr object.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="value" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="value" /> and the current BitArr do not have the same number of elements. </exception>

        public BitArr Or(BitArr value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (m_length != value.m_length)
            {
                throw new ArgumentException(nameof(value),$"Argument array length {value.m_length} is not equal to {m_length}.");
            }
            
            if (m_array == null)
            {
                return this;
            }

            var arrayLength = GetArrayLength(m_length, BitsPerInt32);
            for (int index = 0; index < arrayLength; ++index)
            {
                var valueByRef = value.m_array.ValueByRef(index);

                m_array.ValueByRef(index) |= valueByRef;
            }

            return this;
        }

        /// <summary>
        /// Performs the bitwise exclusive OR operation between the elements of the current BitArr object against the corresponding elements in the specified array. The current BitArr object will be modified to store the result of the bitwise exclusive OR operation.</summary>
        /// <param name="value">The array with which to perform the bitwise exclusive OR operation. </param>
        /// <returns>An array containing the result of the bitwise exclusive OR operation, which is a reference to the current BitArr object. </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="value" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="value" /> and the current BitArr do not have the same number of elements. </exception>
        public BitArr Xor(BitArr value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (m_length != value.m_length)
            {
                throw new ArgumentException(nameof(value),$"Argument array length {value.m_length} is not equal to {m_length}.");
            }
            
            if (m_array == null)
            {
                return this;
            }
            
            var arrayLength = GetArrayLength(m_length, BitsPerInt32);
            for (int index = 0; index < arrayLength; ++index)
            {
                var valueByRef = value.m_array.ValueByRef(index);

                m_array.ValueByRef(index) ^= valueByRef;
            }
            return this;
        }

        /// <summary>Inverts all the bit values in the current BitArr, so that elements set to <see langword="true" /> are changed to <see langword="false" />, and elements set to <see langword="false" /> are changed to <see langword="true" />.</summary>
        /// <returns>The current instance with inverted bit values.</returns>

        public BitArr Not()
        {
            if (m_array == null)
            {
                return this;
            }
            
            var arrayLength = GetArrayLength(m_length, BitsPerInt32);
            for (int index = 0; index < arrayLength; ++index)
            {
                var valueByRef = m_array.ValueByRef(index);

                m_array.ValueByRef(index) = ~valueByRef;
            }
            return this;
        }

        /// <summary>Gets or sets the number of elements in the BitArr.</summary>
        /// <returns>The number of elements in the BitArr.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The property is set to a value that is less than zero. </exception>

        public int Length
        {
            get
            {
                return (int) m_length;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Length), "Length cannot be negative.");
                }

                var arrayLength = GetArrayLength((uint)value, BitsPerInt32);

                m_array.Ensure((int) arrayLength, 0);

                m_length = (uint)Math.Max(arrayLength, value);

                m_version++;
            }
        }

        /// <summary>
        /// Makes sure BitArray size equal to size.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Ensure(int count, bool value)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Length cannot be negative.");
            }

            m_length = (uint)count;

            var arrayLength = GetArrayLength(m_length, BitsPerInt32);

            int num = value ? -1 : 0;

            m_array.Ensure((int)arrayLength, num);

            m_version++;
        }

        /// <summary>Copies the entire BitArr to a compatible one-dimensional <see cref="T:System.Array" />, starting at the specified index of the target array.</summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from BitArr. The <see cref="T:System.Array" /> must have zero-based indexing. </param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="array" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is less than zero. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="array" /> is multidimensional.-or- The number of elements in the source BitArr is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />. </exception>
        /// <exception cref="T:System.InvalidCastException">The type of the source BitArr cannot be cast automatically to the type of the destination <paramref name="array" />. </exception>
        public void CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Given index is negative.");
            }
            
            if (m_array == null)
            {
                return;
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException("Multi dimensional array is not supported.");
            }

            if (array is int[] ia)
            {
                m_array.CopyTo(ia, index);
            }
            else if (array is byte[] numArray)
            {
                var arrayLength = GetArrayLength(m_length, BitsPerByte);
                if (array.Length - index < arrayLength)
                {
                    throw new ArgumentException($"Given index {index} and destination array length {array.Length} do not allow copying data from source array {m_length}.");
                }
                for (int i = 0; i < arrayLength; ++i)
                {
                    var m = m_array.ValueByRef(i / BytesPerInt32);

                    numArray[index + i] = (byte) (m >> i % BytesPerInt32 * 8 & byte.MaxValue);
                }
            }
            else
            {
                if (!(array is bool[]))
                {
                    throw new ArgumentException($"Not supported type {array.GetType()}.");
                }

                if (array.Length - index < m_length)
                {
                    throw new ArgumentException($"Given index {index} and destination array length {array.Length} do not allow copying data from source array {m_length}.");
                }
                bool[] flagArray = (bool[]) array;
                for (int index1 = 0; index1 < m_length; ++index1)
                {
                    var m = m_array.ValueByRef(index1 / BitsPerInt32);

                    flagArray[index + index1] = (uint) (m >> index1 % BitsPerInt32 & 1) > 0U;
                }
            }
        }

        /// <summary>Gets the number of elements contained in the BitArr.</summary>
        public int Count => (int) m_length;

        /// <summary>Creates a shallow copy of the BitArr.</summary>
        public object Clone()
        {
            return new BitArr(m_array)
            {
                m_length = m_length
            };
        }

        /// <summary>Gets an object that can be used to synchronize access to the BitArr.</summary>
        public object SyncRoot => m_array;

        /// <summary>Gets a value indicating whether the BitArr is read-only.</summary>
        /// <returns>This property is always <see langword="false" />.</returns>
        public bool IsReadOnly => false;

        /// <summary>Gets a value indicating whether access to the BitArr is synchronized (thread safe).</summary>
        /// <returns>This property is always <see langword="false" />.</returns>
        public bool IsSynchronized => false;

        private static uint GetArrayLength(uint n, uint div)
        {
            if (n <= 0)
            {
                return 0;
            }
            return (n - 1) / div + 1;
        }

        /// <summary>Returns an enumerator that iterates through the BitArr.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> for the entire BitArr.</returns>
        public IEnumerator<bool> GetEnumerator()
        {
            var version = m_version;

            if (m_array == null)
            {
                yield break;
            }

            for (int i = 0; i < m_length; i++)
            {
                if (version != m_version)
                {
                    throw new InvalidOperationException($"BitArr was modified during enumeration. {m_version - version} time(s).");
                }

                yield return Get(i);
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Clears container. Suppresses instance finalization.
        /// </summary>
        public void Dispose()
        {
            Clear();
            
            GC.SuppressFinalize(this);
        }
    }
}


