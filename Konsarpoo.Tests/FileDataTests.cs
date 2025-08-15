using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests
{
    [TestFixture]
    public class FileDataTests
    {
        private string m_testFile;

        [SetUp]
        public void SetUp()
        {
            m_testFile = Path.GetTempPath() + Guid.NewGuid() + ".tmp";
        }

        [TearDown]
        public void TearDown()
        {
           File.Delete(m_testFile);
        }

        [Test]
        public void TestFileDataBasicOperations()
        {
            var newFile = m_testFile;

            using (var fileData = FileData<int>.Create(newFile, maxSizeOfArray: 4,  arrayBufferCapacity: 2))
            {
                fileData.BeginWrite();
                
                for (int i = 0; i < 10; i++)
                {
                    fileData.Add(i);
                }
                
                fileData.EndWrite();
                
                Assert.AreEqual(10, fileData.Count);
                    
                for (int i = 0; i < 10; i++)
                {
                    var actual = fileData[i];
                    
                    Assert.AreEqual(i, actual);
                }
            }
            
            using (var fileData = FileData<int>.Open(newFile, arrayBufferCapacity: 2))
            {
                Assert.AreEqual(10, fileData.Count);
                
                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(i, fileData[i]);
                }
                
                var enumerated = fileData.ToArray();
                Assert.AreEqual(10, enumerated.Length);
                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(i, enumerated[i]);
                }
            }
        }

        [Test]
        public void TestFileDataCacheEviction()
        {
            var cacheTestFile= m_testFile;
            
            using (var fileData = FileData<int>.Create(cacheTestFile, maxSizeOfArray: 4, arrayBufferCapacity: 2))
            {
                fileData.BeginWrite();
                
                for (int i = 0; i < 20; i++)
                {
                    fileData.Add(i);
                }
                
                fileData.EndWrite();
                
                Assert.AreEqual(0, fileData[0]);  
                Assert.AreEqual(15, fileData[15]); 
                Assert.AreEqual(5, fileData[5]);   
                Assert.AreEqual(19, fileData[19]); 
                
                for (int i = 0; i < 20; i++)
                {
                    Assert.AreEqual(i, fileData[i]);
                }
            }
        }

        [Test]
        public void TestFileDataModification()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4,  arrayBufferCapacity: 2))
            {
                fileData.BeginWrite();
                
                for (int i = 0; i < 10; i++)
                {
                    fileData.Add(i);
                }
                
                fileData[0] = 100;
                fileData[5] = 105;
                fileData[9] = 109;
                
                fileData.EndWrite();
                
                Assert.AreEqual(100, fileData[0]);
                Assert.AreEqual(105, fileData[5]);
                Assert.AreEqual(109, fileData[9]);
                
                Assert.AreEqual(1, fileData[1]);
                Assert.AreEqual(4, fileData[4]);
                Assert.AreEqual(6, fileData[6]);
            }
            
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity: 2))
            {
                Assert.AreEqual(100, fileData[0]);
                Assert.AreEqual(105, fileData[5]);
                Assert.AreEqual(109, fileData[9]);
            }
            
        }

        [Test]
        public void TestFileDataBatchOperations()
        {
            var testFile  = m_testFile;
            
            using (var fileData = FileData<string>.Create(testFile, maxSizeOfArray: 8, arrayBufferCapacity: 3))
            {
                fileData.BeginWrite();
                
                for (int i = 0; i < 50; i++)
                {
                    fileData.Add($"Item_{i:D3}");
                }
                
                fileData[10] = "Modified_010";
                fileData[25] = "Modified_025";
                
                fileData.EndWrite(); 
                
                Assert.AreEqual(50, fileData.Count);
                Assert.AreEqual("Item_000", fileData[0]);
                Assert.AreEqual("Modified_010", fileData[10]);
                Assert.AreEqual("Modified_025", fileData[25]);
                Assert.AreEqual("Item_049", fileData[49]);
            }
           
        }

        [Test]
        public void TestFileDataMemoryPressure()
        {
            var testFile  = m_testFile;
            
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 10, arrayBufferCapacity: 1))
            {
                fileData.BeginWrite();
                
                for (int i = 0; i < 100; i++)
                {
                    fileData.Add((int)(Math.PI * (i + 100)));
                }
                
                fileData.EndWrite();
                
                var random = new Random(42);
                for (int i = 0; i < 200; i++)
                {
                    int index = random.Next(100);
                    double expected = (int)(Math.PI * (index + 100));
                    double actual = fileData[index];
                    
                    Assert.AreEqual(expected, actual, 1e-10);
                }
            }
        }

        [Test]
        public void TestFileDataInsertOperations()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2))
            {
                fileData.BeginWrite();
                for (int i = 0; i < 8; i++)
                {
                    fileData.Add(i); // two full chunks
                }
                // Insert in middle causing cascade and new chunk creation
                fileData.Insert(4, 100);
                // Insert at start
                fileData.Insert(0, 200);
                // Insert at end (append path)
                fileData.Insert(fileData.Count, 300);
                // Insert into partially filled last chunk
                fileData.Insert(2, 400);
                fileData.EndWrite();

                var expected = new List<int> { 200, 0, 400, 1, 2, 3, 100, 4, 5, 6, 7, 300 };
                Assert.AreEqual(expected.Count, fileData.Count);
                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.AreEqual(expected[i], fileData[i], $"Mismatch at index {i}");
                }
            }

            // Reopen and verify persistence
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity: 2))
            {
                var expected = new List<int> { 200, 0, 400, 1, 2, 3, 100, 4, 5, 6, 7, 300 };
                Assert.AreEqual(expected.Count, fileData.Count);
                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.AreEqual(expected[i], fileData[i], $"Mismatch after reopen at index {i}");
                }
            }
        }

        [Test]
        public void TestFileDataRemoveAtOperations()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2))
            {
                fileData.BeginWrite();
                for (int i = 0; i < 20; i++) fileData.Add(i);
                var mirror = new List<int>(Enumerable.Range(0,20));
                // remove first
                fileData.RemoveAt(0); mirror.RemoveAt(0);
                // remove last
                fileData.RemoveAt(fileData.Count - 1); mirror.RemoveAt(mirror.Count - 1);
                // remove middle
                fileData.RemoveAt(5); mirror.RemoveAt(5);
                // remove near chunk boundary
                fileData.RemoveAt(3); mirror.RemoveAt(3);
                // multiple removals to force propagation across several chunks
                fileData.RemoveAt(7); mirror.RemoveAt(7);
                fileData.RemoveAt(7); mirror.RemoveAt(7);
                fileData.EndWrite();
                Assert.AreEqual(mirror.Count, fileData.Count);
                for (int i = 0; i < mirror.Count; i++) Assert.AreEqual(mirror[i], fileData[i], $"Mismatch after removals at {i}");
            }
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity: 2))
            {
                var mirror = new List<int>(Enumerable.Range(0,20));
                mirror.RemoveAt(0);
                mirror.RemoveAt(mirror.Count - 1);
                mirror.RemoveAt(5);
                mirror.RemoveAt(3);
                mirror.RemoveAt(7);
                mirror.RemoveAt(7);
                Assert.AreEqual(mirror.Count, fileData.Count);
                for (int i = 0; i < mirror.Count; i++) Assert.AreEqual(mirror[i], fileData[i], $"Mismatch after reopen removals at {i}");
            }
        }

        [Test]
        public void TestFileDataRemoveAtShrinksChunks()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray:4, arrayBufferCapacity:2))
            {
                fileData.BeginWrite();
                for (int i = 0; i < 9; i++) fileData.Add(i); // 4,4,1
                fileData.RemoveAt(8); // remove sole element in last chunk -> chunk deleted
                fileData.EndWrite();
                Assert.AreEqual(8, fileData.Count);
                for (int i = 0; i < 8; i++) Assert.AreEqual(i, fileData[i]);
            }
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity:2))
            {
                Assert.AreEqual(8, fileData.Count);
                for (int i = 0; i < 8; i++) Assert.AreEqual(i, fileData[i]);
            }
        }

        [Test]
        public void TestFileDataClear()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray:4, arrayBufferCapacity:2))
            {
                for (int i = 0; i < 15; i++) fileData.Add(i);
                fileData.Clear();
                Assert.AreEqual(0, fileData.Count);
                fileData.BeginWrite();
                fileData.Add(42);
                fileData.EndWrite();
                Assert.AreEqual(1, fileData.Count);
                Assert.AreEqual(42, fileData[0]);
            }
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity:2))
            {
                Assert.AreEqual(1, fileData.Count);
                Assert.AreEqual(42, fileData[0]);
            }
        }
    }
}
