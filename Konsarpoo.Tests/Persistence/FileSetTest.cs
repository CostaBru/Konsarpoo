using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Konsarpoo.Collections.Persistence;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests.Persistence
{
    [TestFixture(true, CompressionLevel.Fastest)]
    [TestFixture(true, CompressionLevel.NoCompression)]
    [TestFixture(false, CompressionLevel.Fastest)]
    [TestFixture(false, CompressionLevel.NoCompression)]
    public class FileSetTest 
    {
        private readonly CompressionLevel m_compressionLevel;
        private readonly byte[] m_key;
        private string m_testFile;

        public FileSetTest(bool crypted, CompressionLevel compressionLevel)
        {
            m_compressionLevel = compressionLevel;
            m_key = crypted ? Encoding.Unicode.GetBytes("TestKey") : null;
        }

        [SetUp]
        public void SetUp()
        {
            
            m_testFile = Path.GetTempPath() + Guid.NewGuid() + ".tmp";
        }

        [TearDown]
        public void TearDown()
        {
            var fileNames = FileSet<int>.GetFileNames(m_testFile);

            foreach (var fileName in fileNames)
            {
                File.Delete(fileName);
            }
        }
       
        [Test]
        public void TestSmall()
        {
            {
                using var hashSet = FileSet<string>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

                Assert.NotNull(hashSet.SyncRoot);
                Assert.False(((ICollection<string>)hashSet).IsReadOnly);

                hashSet.Add("0");

                hashSet.Remove("0");

                hashSet.Add("2");
                hashSet.Add("7");

                Assert.False(hashSet.Remove("10"));

            }

            {
                using var hashSet2 = FileSet<string>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

                Assert.AreEqual(2, hashSet2.Count);

                Assert.True(hashSet2.Contains("2"));
                Assert.True(hashSet2.Contains("7"));
            }
        }

        [Test]
        public void TestRemoveIfEmpty()
        {
            using var set = FileSet<int>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

            Assert.False(set.Remove(0));
        }


        [Test]
        public void TestContains([Values(-1000, 0, 1)] int m, [Values(0, 1, 2, 3, 100, 1000)] int n)
        {
            var enumerable = Enumerable.Range(m, n).ToData();

            HashSet<int> hashSet;

            hashSet = new HashSet<int>();

            hashSet.UnionWith(enumerable);

            using var set = FileSet<int>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

            set.AddRange(enumerable);
            
            Assert.AreEqual(set.Count, hashSet.Count);

            foreach (var val in enumerable)
            {
                Assert.True(set.Contains(val), $"val {val} is missing.");
            }

            var ints = new HashSet<int>(set.ToArray());

            foreach (var val in enumerable)
            {
                Assert.True(ints.Contains(val), $"val {val} is missing after enumeration.");
            }

            set.Clear();

            Assert.AreEqual(0, set.Count);

            Assert.AreEqual(0, set.ToArray().Length);

            set.AddRange(enumerable);
            set.AddRange(enumerable);
            set.AddRange(enumerable);

            Assert.AreEqual(enumerable.Count, set.Count);

            foreach (var val in enumerable)
            {
                Assert.True(set.Contains(val), $"val {val} is missing after multiple add.");
            }

            foreach (var val in enumerable)
            {
                set.Remove(val);
            }
        }

        [Test]
        public void TestRemoveWhere()
        {
            using var hashSet = FileSet<int>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

            foreach (var v in Enumerable.Range(0, 10))
            {
                hashSet.Append(v);
            }

            hashSet.RemoveWhere(i => i >= 5);

            Assert.AreEqual(5, hashSet.Count);
        }

        [Test]
        public void TestAppendCopyTo()
        {
            using var hashSet = FileSet<int>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

            foreach (var v in Enumerable.Range(0, 10))
            {
                hashSet.Append(v);

                Assert.True(hashSet.Contains(v));
            }

            var ints = new int[hashSet.Count];
            var ints1 = new int[hashSet.Count];

            hashSet.CopyTo(ints, 3, 2);
            hashSet.CopyTo(ints1);

            for (var index = 3; index < 5; index++)
            {
                var i = ints[index];
                Assert.True(hashSet.Contains(i));
            }

            foreach (var i in ints1)
            {
                Assert.True(hashSet.Contains(i));
            }
        }

        [Test]
        public void TestICollection()
        {
            using var set =  FileSet<int>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

            var collection = (ICollection<int>)set;

            for (int i = 0; i < 10; i++)
            {
                collection.Add(i);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.True(collection.Contains(i));
            }
        }

      
        [Test]
        public void TestSetComparer()
        {
            using var set = FileSet<string>.OpenOrCreate( m_testFile, 4, m_key, m_compressionLevel, comparer: StringComparer.OrdinalIgnoreCase);
            
            set.Add("qwerty");
            set.Add("test");

            Assert.True(set.Contains("qwertY"));
            Assert.True(set.Contains("Test"));
        }
        
        [Test]
        public void TestOp1()
        {
            using var set = FileSet<string>.OpenOrCreate(m_testFile, 4, m_key, m_compressionLevel);

            set.Add(null);
            
            Assert.True(set.Contains(null));
        }

        [Test]
        public void TestMapApi()
        {
            var m3 = new Set<int>() { 1 };
            
            Assert.False(m3[1000]);
            Assert.True(m3[1]);

            m3.Discard(1);
            
            Assert.False(m3[1]);

            m3[1] = true;
            
            Assert.False(m3[1000]);
            Assert.True(m3[1]);
        }
    }
}