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
            var encryptKey = m_crypto ? Encoding.Unicode.GetBytes("TestKey") : null;

            return new DataFileSerialization(m_testFile, FileMode.CreateNew, encryptKey, m_compressionLevel, maxSizeOfArray, 0);
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
        public void TestWriteMetaDataAndAppend()
        {
            var info = CreateInfo(2);
            try
            {
                info.SetMetadata((4, 8, 1, 200));
                
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
                info.SetMetadata((4, 0, 1, 2));
                
                var arr1 = new[] { 1, 2, 3 };
                var arr2 = new[] { 4, 5, 6, 7 };
                
                info.AppendArray(arr1);
                info.AppendArray(arr2);

                info.SetMetadata((4, 0, 1, 100));
                
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
    }
}