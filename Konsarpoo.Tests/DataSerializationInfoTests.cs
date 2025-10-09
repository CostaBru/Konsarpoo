using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Unicode;
using Konsarpoo.Collections.Data.Serialization;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests
{
    [TestFixture(true, CompressionLevel.Fastest)]
    [TestFixture(true, CompressionLevel.NoCompression)]
    [TestFixture(false, CompressionLevel.Fastest)]
    [TestFixture(false, CompressionLevel.NoCompression)]
    public class DataFileSerializationTests
    {
        private readonly bool m_crypto;
        private readonly CompressionLevel m_compressionLevel;
        private string m_testFile;

        public DataFileSerializationTests(bool crypto, CompressionLevel compressionLevel)
        {
            m_crypto = crypto;
            m_compressionLevel = compressionLevel;
        }

        [SetUp]
        public void SetUp()
        {
            m_testFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bin");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(m_testFile))
            {
                File.Delete(m_testFile);
            }
        }
        
        private DataFileSerialization CreateInfo(int maxSizeOfArray)
        {
            return new DataFileSerialization(m_testFile, FileMode.Create, GetEncryptKey(), m_compressionLevel, maxSizeOfArray);
        }

        private byte[] GetEncryptKey()
        {
            var encryptKey = m_crypto ? Encoding.Unicode.GetBytes("TestKey") : null;
            return encryptKey;
        }

        private DataFileSerialization OpenInfo()
        {
            return new DataFileSerialization(m_testFile, FileMode.Open, GetEncryptKey(), m_compressionLevel);
        }

        [Test]
        public void AppendArray_AppendsArraysCorrectly()
        {
            var info = CreateInfo(4);
            try
            {
                var arr1 = new[] { 1, 2, 3 };
                var arr2 = new[] { 4, 5, 6, 7 };

                info.AppendArray(arr1);
                var read1 = info.ReadArray<int>(0);
                
                info.AppendArray(arr2);
                var read2 = info.ReadArray<int>(1);
                
                read1 = info.ReadArray<int>(0);
                read2 = info.ReadArray<int>(1);

                Assert.AreEqual(arr1, read1);
                Assert.AreEqual(arr2, read2);
            }
            finally
            {
                info.Dispose();
            }
        }
        
        [Test]
        public void AppendExtraMetadata()
        {
            var arr1 = new[] { 1, 2, 3 };
            var arr2 = new[] { 4, 5, 6, 7 };
                
            var info = CreateInfo(4);
            try
            {
                var bytes = Encoding.UTF8.GetBytes("Test");

                info.SetExtraMetadata(bytes);

                info.AppendArray(arr1);
                var read1 = info.ReadArray<int>(0);
                
                info.AppendArray(arr2);
                var read2 = info.ReadArray<int>(1);
                
                read1 = info.ReadArray<int>(0);
                read2 = info.ReadArray<int>(1);

                Assert.AreEqual(arr1, read1);
                Assert.AreEqual(arr2, read2);
                
            }
            finally
            {
                info.Dispose();
            }

            {
                var dataFileSerialization = OpenInfo();

                try
                {
                    var extraMetadata = dataFileSerialization.ExtraMetadata;

                    var metadata = Encoding.UTF8.GetString(extraMetadata);

                    Assert.AreEqual("Test", metadata);
                    
                    var read1 = dataFileSerialization.ReadArray<int>(0);
                    var read2 = dataFileSerialization.ReadArray<int>(1);
                
                    Assert.AreEqual(arr1, read1);
                    Assert.AreEqual(arr2, read2);
                }
                finally
                {
                    dataFileSerialization.Dispose();
                }
            }
        }

        [Test]
        public void TestWriteMetaDataAndAppend()
        {
            var info = CreateInfo(2);
            try
            {
                info.UpdateMetadata((4, 8, 1));
                
                info.WriteMetadata();
                
                var arr1 = new[] { 1, 2, 3 };
                var arr2 = new[] { 4, 5, 6, 7 };

                info.AppendArray(arr1);
                info.AppendArray(arr2);
                
                var read1 = info.ReadArray<int>(0);
                var read2 = info.ReadArray<int>(1);
                
                Assert.AreEqual(arr1, read1);
                Assert.AreEqual(arr2, read2);
            }
            finally
            {
                info.Dispose();
            }
        }
        
        [Test]
        public void TestSettingArrayCapacityDoNotCorrupt()
        {
            var info = CreateInfo(2);
            try
            {
                info.UpdateMetadata((4, 0, 1));
                info.WriteMetadata();
                
                var arr1 = new[] { 1, 2, 3 };
                var arr2 = new[] { 4, 5, 6, 7 };
                
                info.AppendArray(arr1);
                info.AppendArray(arr2);

                info.SetArrayCapacity(100);
                
                var read1 = info.ReadArray<int>(0);
                var read2 = info.ReadArray<int>(1);
                
                Assert.AreEqual(arr1, read1);
                Assert.AreEqual(arr2, read2);
            }
            finally
            {
                info.Dispose();
            }
        }

        [Test]
        public void WriteArray_AtZeroPosition_OverwritesCorrectly()
        {
            var info = CreateInfo(4);
            try
            {
                var arr1 = new[] { 1, 2, 3 };
                var arr2 = new[] { 9, 8, 7, 1 };

                info.AppendArray(arr1);
                info.WriteArray(0, arr2);

                var read = info.ReadArray<int>(0);

                Assert.AreEqual(arr2, read);
            }
            finally
            {
                info.Dispose();
            }
        }
        
        [Test]
        public void WriteArray_AtZeroPosition_OverwritesCorrectly_Copy()
        {
            var info = CreateInfo(2);
            try
            {
                var arr1 = new[] { 1, 2, 3 };
                var arr2 = new[] { 4, 5, 6, 7 };
                var arr3 = new[] { 8, 9 };

                info.AppendArray(arr1);
                info.AppendArray(arr2);
                info.WriteArray(0, arr3);

                var read0 = info.ReadArray<int>(0);
                var read1 = info.ReadArray<int>(1);

                Assert.AreEqual(arr3, read0);
                Assert.AreEqual(arr2, read1);
            }
            finally
            {
                info.Dispose();
            }
        }

        [Test]
        public void TestMapSerialization()
        {
            var originalMap = new Map<string, int>();
            originalMap.Add("key1", 1);
            originalMap.Add("key2", 2);
            originalMap.Add("key3", 3);

            var info = CreateInfo(4);
            try
            {
                originalMap.SerializeTo(info);

                var deserializedMap = new Map<string, int>();
                deserializedMap.DeserializeFrom(info);

                Assert.AreEqual(originalMap.Count, deserializedMap.Count);
                foreach (var kvp in originalMap)
                {
                    Assert.IsTrue(deserializedMap.TryGetValue(kvp.Key, out var value));
                    Assert.AreEqual(kvp.Value, value);
                }
            }
            finally
            {
                info.Dispose();
            }
        }
       
        [Test]
        public void TestSetSerialization()
        {
            var originalSet = new Set<string>();
            originalSet.Add("item1");
            originalSet.Add("item2");
            originalSet.Add("item3");

            var info = CreateInfo(4);
            try
            {
                originalSet.SerializeTo(info);

                var deserializedSet = new Set<string>();
                deserializedSet.DeserializeFrom(info);

                Assert.AreEqual(originalSet.Count, deserializedSet.Count);
                foreach (var item in originalSet)
                {
                    Assert.IsTrue(deserializedSet.Contains(item));
                }
            }
            finally
            {
                info.Dispose();
            }
        }
    }
}