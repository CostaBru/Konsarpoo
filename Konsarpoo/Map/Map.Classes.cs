using System;
using System.Collections;
using System.Collections.Generic;
using Konsarpoo.Collections.Stackalloc;

namespace Konsarpoo.Collections
{
    public partial class  Map<TKey, TValue>
    {
        /// <inheritdoc />
        [Serializable]
        public class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly Map<TKey, TValue> m_dictionary;
            private readonly ushort m_version;
            private int m_index;
            private KeyValuePair<TKey, TValue> m_current;

            internal Enumerator(Map<TKey, TValue> dictionary)
            {
                m_dictionary = dictionary;
                m_version = dictionary.m_version;
                m_index = 0;
                m_current = new KeyValuePair<TKey, TValue>();
            }

            /// <inheritdoc />
            public KeyValuePair<TKey, TValue> Current => m_current;

            object IEnumerator.Current
            {
                get
                {
                    CheckState();

                    return new KeyValuePair<TKey, TValue>(m_current.Key, m_current.Value);
                }
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                CheckVersion();

                while (m_index < m_dictionary.m_count)
                {
                    var entry = m_dictionary.m_entries[m_index];
                    
                    if (entry.Key.HashCode >= 0)
                    {
                        m_current = new KeyValuePair<TKey, TValue>(entry.Key.Key, entry.Value);
                        m_index++;
                        return true;
                    }
                    m_index++;
                }
                m_index = m_dictionary.m_count + 1;
                m_current = new KeyValuePair<TKey, TValue>();
                return false;
            }

            void IEnumerator.Reset()
            {
                CheckVersion();

                m_index = 0;
                m_current = new KeyValuePair<TKey, TValue>();
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            /// <inheritdoc />
            public void Dispose()
            {
            }

            private void CheckVersion()
            {
                if (m_version != m_dictionary.m_version)
                {
                    throw new InvalidOperationException($"Map collection was modified during enumeration.");
                }
            }

            private void CheckState()
            {
                if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
                {
                    throw new InvalidOperationException("Map collection was modified during enumeration. ");
                }
            }
        }

        /// <inheritdoc />
        [Serializable]
        public class ArrayEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly Map<TKey, TValue> m_dictionary;
            private readonly Entry<TKey,TValue>[] m_entries;
            private readonly ushort m_version;
            private int m_index;
            private KeyValuePair<TKey, TValue> m_current;

            internal ArrayEnumerator(Map<TKey, TValue> dictionary)
            {
                m_entries = dictionary.m_entries.m_root.Storage;
                m_dictionary = dictionary;
                m_version = dictionary.m_version;
                m_index = 0;
                m_current = default;
            }

            /// <inheritdoc />
            public KeyValuePair<TKey, TValue> Current => m_current;

            object IEnumerator.Current
            {
                get
                {
                    CheckState();

                    return new KeyValuePair<TKey, TValue>(m_current.Key, m_current.Value);
                }
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                CheckVersion();

                while (m_index < m_dictionary.m_count)
                {
                    var entry = m_entries[m_index];
                    
                    if (entry.Key.HashCode >= 0)
                    {
                        m_current = new KeyValuePair<TKey, TValue>(entry.Key.Key, entry.Value);
                        m_index++;
                        return true;
                    }

                    m_index++;
                }

                m_index = m_dictionary.m_count + 1;
                m_current = default;
                return false;
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            /// <inheritdoc />
            public void Dispose()
            {
            }

            void IEnumerator.Reset()
            {
                CheckVersion();

                m_index = 0;
                m_current = default;
            }

            private void CheckVersion()
            {
                if (m_version != m_dictionary.m_version)
                {
                    throw new InvalidOperationException($"Map collection was modified during enumeration.");
                }
            }

            private void CheckState()
            {
                if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
                {
                    throw new InvalidOperationException("Collection was modified during enumeration.");
                }
            }
        }

        /// <inheritdoc />
        [Serializable]
        public sealed class KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>, IEnumerable<TKey>
        {
            private readonly Map<TKey, TValue> m_dictionary;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="dictionary"></param>
            /// <exception cref="ArgumentNullException"></exception>
            public KeyCollection(Map<TKey, TValue> dictionary)
            {
                if (ReferenceEquals(dictionary, null))
                {
                    throw new ArgumentNullException("dictionary");
                }
                m_dictionary = dictionary;
            }

            /// <inheritdoc />
            public void CopyTo(TKey[] array, int index)
            {
                if (ReferenceEquals(array , null))
                {
                    throw new ArgumentNullException("array");
                }
                if ((index < 0) || (index > array.Length))
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if ((array.Length - index) < m_dictionary.Count)
                {
                    throw new ArgumentException();
                }
                int count = m_dictionary.m_count;
                var entries = m_dictionary.m_entries;
                for (int i = 0; i < count; i++)
                {
                    var entry = entries[i];
                    
                    if (entry.Key.HashCode >= 0)
                    {
                        array[index++] = entry.Key.Key;
                    }
                }
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotSupportedException();
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            void ICollection<TKey>.Clear()
            {
                throw new NotSupportedException();
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            bool ICollection<TKey>.Contains(TKey item)
            {
                return m_dictionary.ContainsKey(item);
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException();
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                if (m_dictionary.m_entries?.m_root.Storage != null)
                {
                    return new ArrayEnumerator(m_dictionary);
                }
                
                return new Enumerator(m_dictionary);
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((ICollection<TKey>)this).GetEnumerator();
            }

            /// <inheritdoc />
            public int Count => m_dictionary.Count;

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            bool ICollection<TKey>.IsReadOnly => true;

            /// <inheritdoc />
            public struct Enumerator : IEnumerator<TKey>
            {
                private readonly Map<TKey, TValue> m_dictionary;
                private readonly ushort m_version;
                private int m_index;
                private TKey m_currentKey;

                /// <inheritdoc />
                public TKey Current => m_currentKey;

                object IEnumerator.Current
                {
                    get
                    {
                        CheckState();

                        return m_currentKey;
                    }
                }

                internal Enumerator(Map<TKey, TValue> dictionary)
                {
                    m_dictionary = dictionary;
                    m_version = dictionary.m_version;
                    m_index = 0;
                    m_currentKey = default;
                }

                /// <inheritdoc />
                public bool MoveNext()
                {
                    CheckVersion();

                    while (m_index < m_dictionary.m_count)
                    {
                        var entry = m_dictionary.m_entries[m_index];
                        if (entry.Key.HashCode >= 0)
                        {
                            m_currentKey = entry.Key.Key;
                            m_index++;
                            return true;
                        }
                        m_index++;
                    }
                    m_index = m_dictionary.m_count + 1;
                    m_currentKey = default;
                    return false;
                }

                [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
                /// <inheritdoc />
                public void Dispose()
                {
                }

                void IEnumerator.Reset()
                {
                    CheckVersion();

                    m_index = 0;
                    m_currentKey = default;
                }

                private void CheckVersion()
                {
                    if (m_version != m_dictionary.m_version)
                    {
                        throw new InvalidOperationException($"Map collection was modified during enumeration.");
                    }
                }

                private void CheckState()
                {
                    if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
                    {
                        throw new InvalidOperationException("Collection was modified during enumeration.");
                    }
                }
            }

            /// <inheritdoc />
            [Serializable]
            public struct ArrayEnumerator : IEnumerator<TKey>
            {
                private readonly Map<TKey, TValue> m_dictionary;
                private readonly Entry<TKey,TValue>[] m_entries;
                private readonly ushort m_version;
                private int m_index;
                private TKey m_currentKey;

                /// <inheritdoc />
                public TKey Current => m_currentKey;

                [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
                object IEnumerator.Current
                {
                    get
                    {
                        CheckState();

                        return m_currentKey;
                    }
                }

                internal ArrayEnumerator(Map<TKey, TValue> dictionary)
                {
                    m_entries = dictionary.m_entries.m_root.Storage;
                    m_dictionary = dictionary;
                    m_version = dictionary.m_version;
                    m_index = 0;
                    m_currentKey = default;
                }

                /// <inheritdoc />
                public bool MoveNext()
                {
                    CheckVersion();

                    while (m_index < m_dictionary.m_count)
                    {
                        if (m_entries[m_index].Key.HashCode >= 0)
                        {
                            m_currentKey = m_entries[m_index].Key.Key;
                            m_index++;
                            return true;
                        }
                        m_index++;
                    }
                    m_index = m_dictionary.m_count + 1;
                    m_currentKey = default;
                    return false;
                }

                [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
                /// <inheritdoc />
                public void Dispose()
                {
                }

                void IEnumerator.Reset()
                {
                    CheckVersion();

                    m_index = 0;
                    m_currentKey = default;
                }

                private void CheckVersion()
                {
                    if (m_version != m_dictionary.m_version)
                    {
                        throw new InvalidOperationException($"Map collection was modified during enumeration.");
                    }
                }

                private void CheckState()
                {
                    if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
                    {
                        throw new InvalidOperationException("Collection was modified during enumeration.");
                    }
                }
            }
        }

        /// <inheritdoc />
        [Serializable]
        public sealed class ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
        {
            private readonly Map<TKey, TValue> m_dictionary;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="dictionary"></param>
            /// <exception cref="ArgumentNullException"></exception>
            public ValueCollection(Map<TKey, TValue> dictionary)
            {
                m_dictionary = dictionary ?? throw new ArgumentNullException("dictionary");
            }

            /// <inheritdoc />
            public void CopyTo(TValue[] array, int index)
            {
                if (ReferenceEquals(array, null))
                {
                    throw new ArgumentNullException("array");
                }
                if ((index < 0) || (index > array.Length))
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if ((array.Length - index) < m_dictionary.Count)
                {
                    throw new ArgumentException();
                }
                int count = m_dictionary.m_count;
                var entries = m_dictionary.m_entries;
                for (int i = 0; i < count; i++)
                {
                    var entry = entries[i];
                    
                    if (entry.Key.HashCode >= 0)
                    {
                        array[index++] = entry.Value;
                    }
                }
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException();
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException();
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            bool ICollection<TValue>.Contains(TValue item)
            {
                return m_dictionary.ContainsValue(item);
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            {
                if (m_dictionary.m_entries?.m_root?.Storage != null)
                {
                    return new ArrayEnumerator(m_dictionary);
                }
                
                return new Enumerator(m_dictionary);
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((ICollection<TValue>)this).GetEnumerator();
            }

            /// <inheritdoc />
            public int Count => m_dictionary.Count;

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
            bool ICollection<TValue>.IsReadOnly => true;

            /// <inheritdoc />
            public struct Enumerator : IEnumerator<TValue>
            {
                private readonly Map<TKey, TValue> m_dictionary;
                private readonly ushort m_version;
                private int m_index;
                private TValue m_currentValue;

                /// <inheritdoc />
                public TValue Current => m_currentValue;

                object IEnumerator.Current
                {
                    get
                    {
                        CheckState();

                        return m_currentValue;
                    }
                }

                internal Enumerator(Map<TKey, TValue> dictionary)
                {
                    m_dictionary = dictionary;
                    m_version = dictionary.m_version;
                    m_index = 0;
                    m_currentValue = default;
                }

                /// <inheritdoc />
                public bool MoveNext()
                {
                    CheckVersion();

                    while (m_index < m_dictionary.m_count)
                    {
                        var dictionaryEntry = m_dictionary.m_entries[m_index];
                        
                        if (dictionaryEntry.Key.HashCode >= 0)
                        {
                            m_currentValue = dictionaryEntry.Value;
                            m_index++;
                            return true;
                        }
                        m_index++;
                    }
                    m_index = m_dictionary.m_count + 1;
                    m_currentValue = default;
                    return false;
                }

                [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
                /// <inheritdoc />
                public void Dispose()
                {
                }

                void IEnumerator.Reset()
                {
                    CheckVersion();

                    m_index = 0;
                    m_currentValue = default;
                }

                private void CheckVersion()
                {
                    if (m_version != m_dictionary.m_version)
                    {
                        throw new InvalidOperationException($"Map collection was modified during enumeration.");
                    }
                }

                private void CheckState()
                {
                    if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
                    {
                        throw new InvalidOperationException("Collection was modified during enumeration.");
                    }
                }
            }

            /// <inheritdoc />
            [Serializable]
            public struct ArrayEnumerator : IEnumerator<TValue>
            {
                private readonly Map<TKey, TValue> m_dictionary;
                private readonly Entry<TKey,TValue>[] m_entries;
                private readonly ushort m_version;
                private int m_index;
                private TValue m_currentValue;

                /// <inheritdoc />
                public TValue Current => m_currentValue;

                object IEnumerator.Current
                {
                    get
                    {
                        CheckState();

                        return m_currentValue;
                    }
                }

                internal ArrayEnumerator(Map<TKey, TValue> dictionary)
                {
                    m_entries = dictionary.m_entries.m_root.Storage;
                    m_dictionary = dictionary;
                    m_version = dictionary.m_version;
                    m_index = 0;
                    m_currentValue = default;
                }

                /// <inheritdoc />
                public bool MoveNext()
                {
                    CheckVersion();

                    while (m_index < m_dictionary.m_count)
                    {
                        var entry = m_entries[m_index];
                        if (entry.Key.HashCode >= 0)
                        {
                            m_currentValue = entry.Value;
                            m_index++;
                            return true;
                        }
                        m_index++;
                    }
                    m_index = m_dictionary.m_count + 1;
                    m_currentValue = default;
                    return false;
                }

                [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
                /// <inheritdoc />
                public void Dispose()
                {
                }

                void IEnumerator.Reset()
                {
                    CheckVersion();

                    m_index = 0;
                    m_currentValue = default;
                }

                private void CheckVersion()
                {
                    if (m_version != m_dictionary.m_version)
                    {
                        throw new InvalidOperationException($"Map collection was modified during enumeration.");
                    }
                }

                private void CheckState()
                {
                    if ((m_index == 0) || (m_index == (m_dictionary.m_count + 1)))
                    {
                        throw new InvalidOperationException("Collection was modified during enumeration.");
                    }
                }
            }
        }
    }
}