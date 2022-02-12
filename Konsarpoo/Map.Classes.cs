using System;
using System.Collections;
using System.Collections.Generic;

namespace Konsarpoo.Collections
{
    public partial class  Map<TKey, TValue>
    {
        [Serializable]
        private struct Entry
        {
            public Entry(int hashCode, int next, TKey key, int value)
            {
                HashCode = hashCode;
                Next = next;
                Key = key;
                ValueRef = value;
            }

            public int HashCode;
            public int Next;
            public TKey Key;
            public int ValueRef;
        }

        /// <inheritdoc />
        [Serializable]
        public class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly Map<TKey, TValue> m_dictionary;
            private readonly int m_version;
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
                    if (m_dictionary.m_entries[m_index].HashCode >= 0)
                    {
                        m_current = new KeyValuePair<TKey, TValue>(m_dictionary.m_entries[m_index].Key, m_dictionary.m_entryValues[m_dictionary.m_entries[m_index].ValueRef]);
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

            /// <inheritdoc />
            public void Dispose()
            {
            }

            private void CheckVersion()
            {
                if (m_version != m_dictionary.m_version)
                {
                    throw new InvalidOperationException($"Map collection was modified during enumeration. {m_dictionary.m_version - m_version} time(s).");
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
            private readonly Entry[] m_entries;
            private readonly TValue[] m_values;
            private readonly int m_version;
            private int m_index;
            private KeyValuePair<TKey, TValue> m_current;

            internal ArrayEnumerator(Map<TKey, TValue> dictionary)
            {
                m_entries = ((Data<Entry>.StoreNode) dictionary.m_entries.m_root).m_items;
                m_values = ((Data<TValue>.StoreNode) dictionary.m_entryValues.m_root).m_items;
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
                    if (m_entries[m_index].HashCode >= 0)
                    {
                        m_current = new KeyValuePair<TKey, TValue>(m_entries[m_index].Key, m_values[m_entries[m_index].ValueRef]);
                        m_index++;
                        return true;
                    }

                    m_index++;
                }

                m_index = m_dictionary.m_count + 1;
                m_current = default;
                return false;
            }

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
                    throw new InvalidOperationException($"Map collection was modified during enumeration. {m_dictionary.m_version - m_version} time(s).");
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
        public sealed class KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>
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
                IList<Entry> entries = m_dictionary.m_entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].HashCode >= 0)
                    {
                        array[index++] = entries[i].Key;
                    }
                }
            }

            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TKey>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                return m_dictionary.ContainsKey(item);
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException();
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            {
                if (m_dictionary.m_entries?.m_root is Data<Entry>.StoreNode)
                {
                    return new ArrayEnumerator(m_dictionary);
                }
                
                return new Enumerator(m_dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((ICollection<TKey>)this).GetEnumerator();
            }

            /// <inheritdoc />
            public int Count => m_dictionary.Count;

            bool ICollection<TKey>.IsReadOnly => true;

            /// <inheritdoc />
            public struct Enumerator : IEnumerator<TKey>
            {
                private readonly Map<TKey, TValue> m_dictionary;
                private readonly int m_version;
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
                        if (m_dictionary.m_entries[m_index].HashCode >= 0)
                        {
                            m_currentKey = m_dictionary.m_entries[m_index].Key;
                            m_index++;
                            return true;
                        }
                        m_index++;
                    }
                    m_index = m_dictionary.m_count + 1;
                    m_currentKey = default;
                    return false;
                }

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
                        throw new InvalidOperationException($"Map collection was modified during enumeration. {m_dictionary.m_version - m_version} time(s).");
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
                private readonly Entry[] m_entries;
                private readonly int m_version;
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

                internal ArrayEnumerator(Map<TKey, TValue> dictionary)
                {
                    m_entries = ((Data<Entry>.StoreNode)dictionary.m_entries.m_root).m_items;
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
                        if (m_entries[m_index].HashCode >= 0)
                        {
                            m_currentKey = m_entries[m_index].Key;
                            m_index++;
                            return true;
                        }
                        m_index++;
                    }
                    m_index = m_dictionary.m_count + 1;
                    m_currentKey = default;
                    return false;
                }

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
                        throw new InvalidOperationException($"Map collection was modified during enumeration. {m_dictionary.m_version - m_version} time(s).");
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
                IList<Entry> entries = m_dictionary.m_entries;
                IList<TValue> entryValues = m_dictionary.m_entryValues;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].HashCode >= 0)
                    {
                        array[index++] = entryValues[entries[i].ValueRef];
                    }
                }
            }

            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                return m_dictionary.ContainsValue(item);
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            {
                if (m_dictionary.m_entries?.m_root?.Storage != null && m_dictionary.m_entryValues?.m_root?.Storage != null)
                {
                    return new ArrayEnumerator(m_dictionary);
                }
                
                return new Enumerator(m_dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((ICollection<TValue>)this).GetEnumerator();
            }

            /// <inheritdoc />
            public int Count => m_dictionary.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            /// <inheritdoc />
            public struct Enumerator : IEnumerator<TValue>
            {
                private readonly Map<TKey, TValue> m_dictionary;
                private readonly int m_version;
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
                        if (m_dictionary.m_entries[m_index].HashCode >= 0)
                        {
                            m_currentValue = m_dictionary.m_entryValues[m_dictionary.m_entries[m_index].ValueRef];
                            m_index++;
                            return true;
                        }
                        m_index++;
                    }
                    m_index = m_dictionary.m_count + 1;
                    m_currentValue = default;
                    return false;
                }

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
                        throw new InvalidOperationException($"Map collection was modified during enumeration. {m_dictionary.m_version - m_version} time(s).");
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
                private readonly Entry[] m_entries;
                private readonly int m_version;
                private int m_index;
                private TValue m_currentValue;
                private readonly TValue[] m_values;

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
                    m_entries = ((Data<Entry>.StoreNode)dictionary.m_entries.m_root).m_items;
                    m_values = ((Data<TValue>.StoreNode)dictionary.m_entryValues.m_root).m_items;
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
                        if (m_entries[m_index].HashCode >= 0)
                        {
                            m_currentValue = m_values[m_entries[m_index].ValueRef];
                            m_index++;
                            return true;
                        }
                        m_index++;
                    }
                    m_index = m_dictionary.m_count + 1;
                    m_currentValue = default;
                    return false;
                }

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
                        throw new InvalidOperationException($"Map collection was modified during enumeration. {m_dictionary.m_version - m_version} time(s).");
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