using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Konsarpoo.Collections.Tests
{
    internal static class SerializeHelper
    {
        internal static string SerializeWithDcs<T>(T obj)
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

        internal static T DeserializeWithDcs<T>(string xml)
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

        internal static Stream Serialize(object source)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            formatter.Serialize(stream, source);
            return stream;
        }

        internal static T Deserialize<T>(Stream stream)
        {
            IFormatter formatter = new BinaryFormatter();
            stream.Position = 0;
            return (T)formatter.Deserialize(stream);
        }

        public static T Clone<T>(object source)
        {
            return Deserialize<T>(Serialize(source));
        }
    }
}