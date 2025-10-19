using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests
{
    [TestFixture(true, CompressionLevel.Fastest)]
    [TestFixture(true, CompressionLevel.NoCompression)]
    [TestFixture(false, CompressionLevel.Fastest)]
    [TestFixture(false, CompressionLevel.NoCompression)]
    public class FileDataTests
    {
        private readonly CompressionLevel m_compressionLevel;
        private readonly byte[] m_key;
        private string m_testFile;

        public FileDataTests(bool crypted, CompressionLevel compressionLevel)
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
           File.Delete(m_testFile);
        }

        [Test]
        public void TestFileEnsure2()
        {
            var newFile = m_testFile;

            using (var fileData = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key,
                       compressionLevel: m_compressionLevel))
            {
                fileData.Ensure(2);

                fileData[0] = 1;
                fileData[1] = 1;
                
                fileData.Ensure(3, 2);
                
                Assert.AreEqual(2, fileData[2]);
                
                fileData.Ensure(5, 3);
                
                Assert.AreEqual(3, fileData[4]);
            }
        }
        
        [Test]
        public void TestFileEnsure4()
        {
            var newFile = m_testFile;

            using (var fileData = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key,
                       compressionLevel: m_compressionLevel))
            {
                fileData.Ensure(4, 1);
                fileData.Ensure(5, 2);
                
                Assert.AreEqual(2, fileData[4]);
            }
        }

        [Test]
        public void TestFileDataBasicOperations()
        {
            var newFile = m_testFile;

            using (var fileData = FileData<int>.Create(newFile, maxSizeOfArray: 4,  arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
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
            
            using (var fileData = FileData<int>.Open(newFile, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
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
        public void TestFileDataEnsure0([Values(0, 999)] int defVal)
        {
            var newFile = m_testFile;

            using (var fileData = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                if (defVal == 0)
                {
                    fileData.Ensure(4);
                }
                else
                {
                    fileData.Ensure(4, defVal);
                }
                
                Assert.AreEqual(4, fileData.Count);

                for (int i = 0; i < 4; i++)
                {
                    Assert.AreEqual(defVal, fileData[i]);
                }
            }
        }
        
        [Test]
        public void TestFileDataEnsure([Values(0, 999)] int defVal)
        {
            var newFile = m_testFile;

            using var fileData = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);
          
            if (defVal == 0)
            {
                fileData.Ensure(4);
                fileData.Ensure(16);
                fileData.Ensure(32);
            }
            else
            {
                fileData.Ensure(4, defVal);
                fileData.Ensure(16, defVal);
                fileData.Ensure(32, defVal);
            }
                
            Assert.AreEqual(32, fileData.Count);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(defVal, fileData[i]);
            }
        }
        
        [Test]
        public void TestListInsert([Values(2000, 6, 5, 4, 3, 2, 1, 0)] int count)
        {
            var newFile = m_testFile;

            {
                using var data = FileData<int>.Create(newFile, maxSizeOfArray: 512, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);

                data.BeginWrite();
                
                var list = new List<int>(count);

                for (int i = 0; i < count; i++)
                {
                    data.Insert(0, i);
                    list.Insert(0, i);

                    Assert.AreEqual(list[i], data[i]);
                }
                
                data.EndWrite();

                var enumerator = ((IEnumerable)data).GetEnumerator();
                enumerator.MoveNext();

                int ii = 0;
                foreach (var val in list)
                {
                    if (ii == 510)
                    {
                        System.Console.Write(ii);
                    }
                    
                    Assert.AreEqual(val, enumerator.Current, ii.ToString());
                    enumerator.MoveNext();
                    ii++;
                }
            }

            {
                using var poolList = FileData<int>.Open(newFile, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);

                var list = new List<int>(count);

                for (int i = 0; i < count; i++)
                {
                    list.Insert(0, i);
                }

                var enumerator = ((IEnumerable)poolList).GetEnumerator();
                enumerator.MoveNext();

                foreach (var i in list)
                {
                    Assert.AreEqual(i, enumerator.Current);
                    enumerator.MoveNext();
                }
            }
        }

        
        [Test]
        public void TestFileDataEnsure08([Values(0, 999)] int defVal)
        {
            var newFile = m_testFile;

            using (var fileData = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                var size = 8;
                
                if (defVal == 0)
                {
                    fileData.Ensure(size);
                }
                else
                {
                    fileData.Ensure(size, defVal);
                }
                
                Assert.AreEqual(size, fileData.Count);

                for (int i = 0; i < size; i++)
                {
                    Assert.AreEqual(defVal, fileData[i]);
                }
            }
        }
        
        [Test]
        public void TestFileDataEnsure18_([Values(0, 999)] int defVal)
        {
            var newFile = m_testFile;

            using (var fileData = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                fileData.Add(1);
                fileData.Add(2);
                fileData.Add(3);
                fileData.Add(4);

                var size = 8;
                
                if (defVal == 0)
                {
                    fileData.Ensure(size);
                }
                else
                {
                    fileData.Ensure(size, defVal);
                }
                
                Assert.AreEqual(size, fileData.Count);
                
                Assert.AreEqual(1, fileData[0]);
                Assert.AreEqual(2, fileData[1]);
                Assert.AreEqual(3, fileData[2]);
                Assert.AreEqual(4, fileData[3]);

                for (int i = 4; i < size; i++)
                {
                    Assert.AreEqual(defVal, fileData[i]);
                }
            }
        }
        
        [Test]
        public void TestFileDataEnsure1([Values(0, 999)] int defVal)
        {
            var newFile = m_testFile;

            using var fileData = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);
            fileData.Add(1);

            var size = 4;
                
            if (defVal == 0)
            {
                fileData.Ensure(size);
            }
            else
            {
                fileData.Ensure(size, defVal);
            }
                
            Assert.AreEqual(size, fileData.Count);
                
            Assert.AreEqual(1, fileData[0]);

            for (int i = 1; i < size; i++)
            {
                Assert.AreEqual(defVal, fileData[i]);
            }
        }
        
        [Test]
        public void TestReverse([Values(4097, 1025, 513, 5, 3, 1, 0)] int count)
        {
            var list = Enumerable.Range(0, count).ToList();

            var newFile = m_testFile;

            using var dataList = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);
            
            dataList.AddRange(list);
            
            list.Reverse();
            dataList.Reverse();

            for (int i = 0; i < list.Count; i++)
            {
                var val = list[i];
                var arrVal = dataList[i];

                Assert.AreEqual(val, arrVal);
            }
        }
        
        private class IntComp : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return x.CompareTo(y);
            }
        }
        
        [Test]
        public void TestSort1()
        {
            var list1 = Enumerable.Range(0, 100).Reverse().ToList();
            
            var newFile = m_testFile;

            using var list2 = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);
            list2.AddRange(list1);
            
            list1.Sort();

            list2.Sort((x, y) => x.CompareTo(y));

            for (var index = 0; index < list1.Count; index++)
            {
                Assert.AreEqual(list1[index], list2[index]);
            }
        }
        
        [Test]
        public void TestSort2()
        {
            var list1 = Enumerable.Range(0, 100).Reverse().ToList();
            
            var newFile = m_testFile;

            using var list2 = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);
            list2.AddRange(list1);

            var comparison = new Comparison<int>((x, y) => x.CompareTo(y));
            
            list1.Sort();
            list2.Sort(comparison);

            for (var index = 0; index < list1.Count; index++)
            {
                Assert.AreEqual(list1[index], list2[index]);
            }
        }
        
        [Test]
        public void TestSort3()
        {
            var list1 = Enumerable.Range(0, 100).Reverse().ToList();
            
            var newFile = m_testFile;

            using var list2 = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);
            list2.AddRange(list1);

            list1.Sort();
            list2.Sort(new IntComp());

            for (var index = 0; index < list1.Count; index++)
            {
                Assert.AreEqual(list1[index], list2[index]);
            }
        }
        
        [Test]
        public void TestBisect1([Values(16, 4)] int count)
        {
            var list1 = Enumerable.Range(0, count).Reverse().ToList();
            
            var newFile = m_testFile;

            using var list2 = FileData<int>.Create(newFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);
            list2.AddRange(list1);

            list1.Sort();
            list2.Sort();

            for (var index = 0; index < list1.Count; index++)
            {
                var i = list1[index];

                var search = list2.BinarySearch(i, 0, list2.Count, new IntComp());
                
                Assert.AreEqual(i, list2[search]);
            }
        }

        [Test]
        public void TestSort5()
        {
            Data<int> data = new Data<int>(0, 16);
            
            int j = 0;
            for (int i = (1024 * 100) - 1; i >= 0; i--)
            {
                data.Add(i);
                j++;
            }
            
            data.Sort();
        }

        [Test]
        public void TestFileDataCacheEviction()
        {
            var cacheTestFile= m_testFile;
            
            using (var fileData = FileData<int>.Create(cacheTestFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
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
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4,  arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
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
            
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
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
            
            using (var fileData = FileData<string>.Create(testFile, maxSizeOfArray: 8, arrayBufferCapacity: 3, key: m_key, compressionLevel: m_compressionLevel))
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
            
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 10, arrayBufferCapacity: 1, key: m_key, compressionLevel: m_compressionLevel))
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
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
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
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
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
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
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
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
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
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray:4, arrayBufferCapacity:2, key: m_key, compressionLevel: m_compressionLevel))
            {
                fileData.BeginWrite();
                for (int i = 0; i < 9; i++) fileData.Add(i); // 4,4,1
                fileData.RemoveAt(8); // remove sole element in last chunk -> chunk deleted
                fileData.EndWrite();
                Assert.AreEqual(8, fileData.Count);
                for (int i = 0; i < 8; i++) Assert.AreEqual(i, fileData[i]);
            }
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity:2, key: m_key, compressionLevel: m_compressionLevel))
            {
                Assert.AreEqual(8, fileData.Count);
                for (int i = 0; i < 8; i++) Assert.AreEqual(i, fileData[i]);
            }
        }

        [Test]
        public void TestFileDataClear()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray:4, arrayBufferCapacity:2, key: m_key, compressionLevel: m_compressionLevel))
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
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity:2, key: m_key, compressionLevel: m_compressionLevel))
            {
                Assert.AreEqual(1, fileData.Count);
                Assert.AreEqual(42, fileData[0]);
            }
        }

        [Test]
        public void TestFileDataClearAfterOperations()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                // Add some data
                for (int i = 0; i < 10; i++) fileData.Add(i);
                // Modify some
                fileData[5] = 999;
                // Insert
                fileData.Insert(0, -1);
                // Remove
                fileData.RemoveAt(10);
                // Sort
                fileData.Sort();
                // Now clear
                fileData.Clear();
                Assert.AreEqual(0, fileData.Count);
                // Add new data
                fileData.BeginWrite();
                fileData.Add(123);
                fileData.EndWrite();
                Assert.AreEqual(1, fileData.Count);
                Assert.AreEqual(123, fileData[0]);
            }
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                Assert.AreEqual(1, fileData.Count);
                Assert.AreEqual(123, fileData[0]);
            }
        }

        [Test]
        public void TestIListContainsAndIndexOf()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                for (int i = 0; i < 10; i++) fileData.Add(i);
                // duplicate value
                fileData.Add(5);

                // Contains
                Assert.IsTrue(fileData.Contains(0));
                Assert.IsTrue(fileData.Contains(5));
                Assert.IsTrue(fileData.Contains(9));
                Assert.IsFalse(fileData.Contains(-1));
                Assert.IsFalse(fileData.Contains(100));

                // IndexOf returns first occurrence
                Assert.AreEqual(0, fileData.IndexOf(0));
                Assert.AreEqual(5, fileData.IndexOf(5));
                Assert.AreEqual(9, fileData.IndexOf(9));
                Assert.AreEqual(-1, fileData.IndexOf(12345));
            }
        }

        [Test]
        public void TestIListRemoveItem_FirstOccurrenceAndReturnValue()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                for (int i = 0; i < 10; i++) fileData.Add(i);
                fileData.Add(5); // duplicate

                // Remove existing value removes the first occurrence only
                var removed = fileData.Remove(5);
                Assert.IsTrue(removed);
                Assert.AreEqual(10, fileData.Count); // 11 -> 10
                Assert.AreEqual(6, fileData[5]); // first 5 removed, 6 shifted to index 5
                Assert.IsTrue(fileData.Contains(5)); // second 5 still present

                // Remove missing value returns false and does not change
                removed = fileData.Remove(999);
                Assert.IsFalse(removed);
                Assert.AreEqual(10, fileData.Count);
            }
        }

        [Test]
        public void TestIListCopyTo_WithOffsetAndBoundsChecks()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                for (int i = 0; i < 6; i++) fileData.Add(i + 10); // 10..15

                var dest = new int[10];
                fileData.CopyTo(dest, 2);

                // Before offset should be default(0)
                Assert.AreEqual(0, dest[0]);
                Assert.AreEqual(0, dest[1]);
                // Copied range
                for (int i = 0; i < 6; i++) Assert.AreEqual(10 + i, dest[2 + i]);
                // After copied range remains default
                for (int i = 8; i < dest.Length; i++) Assert.AreEqual(0, dest[i]);

                // Exception cases
                Assert.Throws<ArgumentNullException>(() => fileData.CopyTo(null, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => fileData.CopyTo(dest, -1));
                Assert.Throws<ArgumentException>(() => fileData.CopyTo(dest, dest.Length + 1)); // arrayIndex > length

                var small = new int[7];
                // 7 - 3 = 4 < Count(6) -> not enough space
                Assert.Throws<ArgumentException>(() => fileData.CopyTo(small, 3));
                // arrayIndex == array.Length -> fails capacity check
                Assert.Throws<ArgumentException>(() => fileData.CopyTo(small, small.Length));
            }
        }

        [Test]
        public void TestIListIsReadOnly_IsFalse()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                Assert.IsFalse(fileData.IsReadOnly);
                Assert.IsFalse(((ICollection<int>)fileData).IsReadOnly);
            }
        }

        [Test]
        public void TestFileDataSort_Default_SingleChunk()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 16, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                var data = new[] { 5, 1, 9, 3, 7, 2, 8, 6, 4, 0 };
                fileData.BeginWrite();
                foreach (var x in data) fileData.Add(x);
                fileData.Sort();
                fileData.EndWrite();
                for (int i = 0; i < data.Length; i++) Assert.AreEqual(i, fileData[i]);
            }
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                for (int i = 0; i < 10; i++) Assert.AreEqual(i, fileData[i]);
            }
        }

        [Test]
        public void TestFileDataSort_Default_MultiChunk()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                // 3 chunks worth of data, intentionally jumbled
                var data = new[] { 12, 3, 7, 1, 9, 0, 5, 2, 11, 4, 8, 6 };
                var expected = data.OrderBy(x => x).ToArray();
                fileData.BeginWrite();
                foreach (var x in data) fileData.Add(x);
                fileData.Sort();
                fileData.EndWrite();
                for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], fileData[i]);
            }
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                var expected = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 12 };
                for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], fileData[i]);
            }
        }

        private sealed class DescComparer : IComparer<int>
        {
            public int Compare(int x, int y) => y.CompareTo(x);
        }

        [Test]
        public void TestFileDataSort_CustomComparer_Descending()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                var data = new[] { 5, 1, 9, 3, 7, 2, 8, 6, 4, 0 };
                fileData.BeginWrite();
                foreach (var x in data) fileData.Add(x);
                fileData.Sort(new DescComparer());
                fileData.EndWrite();
                for (int i = 0; i < data.Length; i++) Assert.AreEqual(9 - i, fileData[i]);
            }
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                for (int i = 0; i < 10; i++) Assert.AreEqual(9 - i, fileData[i]);
            }
        }

        [Test]
        public void TestFileDataSort_Comparison_ByAbsThenValue()
        {
            var testFile = m_testFile;
            using (var fileData = FileData<int>.Create(testFile, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                var data = new[] { -3, 2, -1, 0, 1, -2, 3, -4, 4 };
                fileData.BeginWrite();
                foreach (var x in data) fileData.Add(x);
                fileData.Sort((a, b) =>
                {
                    var ca = Math.Abs(a).CompareTo(Math.Abs(b));
                    return ca != 0 ? ca : a.CompareTo(b);
                });
                fileData.EndWrite();

                var expected = new[] { 0, -1, 1, -2, 2, -3, 3, -4, 4 };
                Assert.AreEqual(expected.Length, fileData.Count);
                for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], fileData[i]);
            }
            using (var fileData = FileData<int>.Open(testFile, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                var expected = new[] { 0, -1, 1, -2, 2, -3, 3, -4, 4 };
                for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], fileData[i]);
            }
        }

        [Test]
        public void TestFileDataSort_EmptyAndSingle_DoNothing1()
        {
            var testFile1 = m_testFile;
            using (var fileData = FileData<int>.Create(testFile1, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                fileData.Sort();
                fileData.Sort(new DescComparer());
                fileData.Sort((a,b) => a.CompareTo(b));
                Assert.AreEqual(0, fileData.Count);
            }
        }

        [Test]
        public void TestFileDataSort_EmptyAndSingle_DoNothing2()
        {
            var testFile2 = m_testFile;
            using (var fileData = FileData<int>.Create(testFile2, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                fileData.Add(42);
                fileData.Sort();
                Assert.AreEqual(1, fileData.Count);
                Assert.AreEqual(42, fileData[0]);

                fileData.Sort(new DescComparer());
                Assert.AreEqual(42, fileData[0]);

                fileData.Sort((a,b) => b.CompareTo(a));
                Assert.AreEqual(42, fileData[0]);
            }
        }

        [Test]
        public void TestFileDataBinarySearch_SingleChunk_FoundAndNotFound()
        {
            var file = m_testFile;
            var data = Enumerable.Range(0, 32).ToArray(); // sorted

            using (var fd = FileData<int>.Create(file, maxSizeOfArray: 64, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                fd.BeginWrite();
                foreach (var v in data) fd.Add(v);
                fd.EndWrite();

                Assert.AreEqual(data.Length, fd.Count);

                // found
                var idx = fd.BinarySearch(5, 0, fd.Count);
                var exp = Array.BinarySearch(data, 0, data.Length, 5);
                Assert.AreEqual(exp, idx);

                // not found (smaller than all)
                idx = fd.BinarySearch(-1, 0, fd.Count);
                exp = Array.BinarySearch(data, 0, data.Length, -1);
                Assert.AreEqual(exp, idx);

                // not found (greater than all)
                idx = fd.BinarySearch(100, 0, fd.Count);
                exp = Array.BinarySearch(data, 0, data.Length, 100);
                Assert.AreEqual(exp, idx);
            }
        }

        [Test]
        public void TestFileDataBinarySearch_MultiChunk_FoundAndNotFound()
        {
            var file = m_testFile;
            var data = Enumerable.Range(0, 20).ToArray(); // 5 chunks if maxSizeOfArray = 4

            using (var fd = FileData<int>.Create(file, maxSizeOfArray: 4, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                fd.BeginWrite();
                foreach (var v in data) fd.Add(v);
                fd.EndWrite();

                Assert.AreEqual(data.Length, fd.Count);

                // found in first chunk
                var idx = fd.BinarySearch(2, 0, fd.Count);
                var exp = Array.BinarySearch(data, 0, data.Length, 2);
                Assert.AreEqual(exp, idx);

                // found in middle chunk
                idx = fd.BinarySearch(9, 0, fd.Count);
                exp = Array.BinarySearch(data, 0, data.Length, 9);
                Assert.AreEqual(exp, idx);

                // not found
                idx = fd.BinarySearch(21, 0, fd.Count);
                exp = Array.BinarySearch(data, 0, data.Length, 21);
                Assert.AreEqual(exp, idx);
            }
        }

        [Test]
        public void TestFileDataBinarySearch_RangeAndEmptyRange_Semantics()
        {
            var file = m_testFile;
            var data = Enumerable.Range(0, 30).ToArray();

            using (var fd = FileData<int>.Create(file, maxSizeOfArray: 8, arrayBufferCapacity: 3, key: m_key, compressionLevel: m_compressionLevel))
            {
                fd.BeginWrite();
                foreach (var v in data) fd.Add(v);
                fd.EndWrite();

                // Search in sub-range [5, 15) => length 10
                int start = 5; int endExclusive = 15; int length = endExclusive - start;

                var idx = fd.BinarySearch(7, start, endExclusive);
                var exp = Array.BinarySearch(data, start, length, 7);
                Assert.AreEqual(exp, idx);

                // below range -> should be complement of start
                idx = fd.BinarySearch(4, start, endExclusive);
                exp = Array.BinarySearch(data, start, length, 4);
                Assert.AreEqual(exp, idx);

                // above range -> should be complement of end
                idx = fd.BinarySearch(99, start, endExclusive);
                exp = Array.BinarySearch(data, start, length, 99);
                Assert.AreEqual(exp, idx);

                // Empty range at position 5
                start = 5; endExclusive = 5; length = 0;
                idx = fd.BinarySearch(5, start, endExclusive);
                exp = Array.BinarySearch(data, start, length, 5);
                Assert.AreEqual(exp, idx);
            }
        }

        [Test]
        public void TestFileDataBinarySearch_InvalidArgs_ReturnMinusOne()
        {
            var file = m_testFile;
            using var fd = FileData<int>.Create(file, maxSizeOfArray: 8, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);
            fd.BeginWrite();
            for (int i = 0; i < 10; i++) fd.Add(i);
            fd.EndWrite();

            Assert.AreEqual(-1, fd.BinarySearch(5, -1, fd.Count)); // invalid startIndex
            Assert.AreEqual(-1, fd.BinarySearch(5, fd.Count, fd.Count)); // startIndex >= Count
            Assert.AreEqual(-1, fd.BinarySearch(5, 0, -1)); // invalid endExclusive
            Assert.AreEqual(-1, fd.BinarySearch(5, 0, fd.Count + 1)); // endExclusive > Count
        }

        [Test]
        public void TestFileDataBinarySearch_String_CustomComparer()
        {
            var file = m_testFile;
            var data = Enumerable.Range(0, 100).Select(i => i.ToString()).ToArray();

            using (var fd = FileData<string>.Create(file, maxSizeOfArray: 16, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel))
            {
                fd.BeginWrite();
                foreach (var v in data) fd.Add(v);
                fd.EndWrite();

                var cmp = StringComparer.Ordinal;
                var idx = fd.BinarySearch("42", 0, fd.Count, cmp);
                var exp = Array.BinarySearch(data, 0, data.Length, "42", cmp);
                Assert.AreEqual(exp, idx);

                idx = fd.BinarySearch("999", 0, fd.Count, cmp);
                exp = Array.BinarySearch(data, 0, data.Length, "999", cmp);
                Assert.AreEqual(exp, idx);
            }
        }
        
        [Test]
        public void TestRemoveHugeSome()
        {
            var list = Enumerable.Range(0, 25000).ToList();

            var file = m_testFile;
            using var dataList = FileData<int>.Create(file, maxSizeOfArray: 8, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);

            dataList.AddRange(list);
            
            list.Remove(0);
            dataList.Remove(0);

            list.Remove(555);
            dataList.Remove(555);

            list.Remove(20000);
            dataList.Remove(20000);

            Assert.AreEqual(list.Count, dataList.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var val = dataList[i];
                var arrVal = list[i];

                Assert.AreEqual(val, arrVal);
            }
        }


        [Test]
        public void TestRemoveCommonAll()
        {
            var list = Enumerable.Range(0, 1000).ToList();

            var file = m_testFile;
            using var dataList = FileData<int>.Create(file, maxSizeOfArray: 8, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);

            dataList.AddRange(list);

            Assert.AreEqual(list.Count, dataList.Count);

            var array = list.ToArray();

            foreach (var i in array)
            {
                dataList.Remove(i);
            }

            foreach (var i in array)
            {
                Assert.AreEqual(-1, dataList.IndexOf(i));
            }
        }
        
        [Test]
        public void TestRemoveCommonAllReverse()
        {
            var list = Enumerable.Range(0, 1000).ToList();

            var file = m_testFile;
            using var dataList = FileData<int>.Create(file, maxSizeOfArray: 8, arrayBufferCapacity: 2, key: m_key, compressionLevel: m_compressionLevel);

            dataList.AddRange(list);

            Assert.AreEqual(list.Count, dataList.Count);

            var array = list.ToArray();

            foreach (var i in array.Reverse())
            {
                dataList.Remove(i);
            }

            foreach (var i in array)
            {
                Assert.AreEqual(-1, dataList.IndexOf(i));
            }
        }
    }
}
