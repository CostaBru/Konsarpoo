using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// String and binary serialization helper.
    /// </summary>
    public static class SerializeHelper
    {
        /// <summary>
        /// Serializes a given instance to string with DataContract serializer.
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string SerializeWithDcs<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                using (var sr = new StreamReader(ms, Encoding.UTF8))
                {
                    var serializer = new DataContractSerializer(typeof(T));
                    serializer.WriteObject(ms, obj);
                    ms.Position = 0;
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Deserializes a given instance from string with DataContract serializer.
        /// </summary>
        /// <param name="xml"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T DeserializeWithDcs<T>(string xml)
        {
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, Encoding.UTF8))
                {
                    sw.Write(xml);
                    sw.Flush();
                    ms.Position = 0;
                    var deserializer = new DataContractSerializer(typeof(T));
                    return (T)deserializer.ReadObject(ms);
                }
            }
        }

        /// <summary>
        /// Serializes with binary formatter.
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static MemoryStream Serialize(object source)
        {
            IFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, source);
            return stream;
        }

        /// <summary>
        /// Deserializes with binary formatter.
        /// </summary>
        /// <param name="stream"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Deserialize<T>([NotNull] Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            IFormatter formatter = new BinaryFormatter();
            stream.Position = 0;
            return (T)formatter.Deserialize(stream);
        }

        /// <summary>
        /// Creates a deep copy of object using binary serialization.
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Clone<T>(object source)
        {
            using var memoryStream = Serialize(source) ?? throw new ArgumentNullException("Serialize(source)");
            return Deserialize<T>(memoryStream);
        }
    }
}