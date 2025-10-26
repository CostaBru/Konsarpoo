using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using Konsarpoo.Collections.Data.Serialization;

namespace Konsarpoo.Collections.Tests.Serialization
{
    [TestFixture]
    public class DataStreamSerializationTransactionTests
    {
        private string m_testFile;

        private DataFileSerialization CreateInfo(int maxSizeOfArray)
        {
            return new DataFileSerialization(m_testFile, FileMode.Create, null, CompressionLevel.NoCompression, maxSizeOfArray);
        }
      

        private DataFileSerialization OpenInfo()
        {
            return new DataFileSerialization(m_testFile, FileMode.Open, null, CompressionLevel.NoCompression);
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

        [Test]
        public void Begin_End_Commit_HappyPath()
        {
            using (var ser = CreateInfo(256))
            {
                ser.WriteMetadata();
                ser.SetArrayCapacity(4);

                ser.AppendArray(new[] { 1, 2, 3 });
                Assert.AreEqual(1, ser.ArrayCount);

                ser.BeginTransaction(Path.GetTempPath());
                ser.AppendArray(new[] { 4, 5 });
                Assert.AreEqual(2, ser.ArrayCount);
                Assert.AreEqual(new[] { 4, 5 }, ser.ReadArray<int>(1));

                Assert.IsTrue(ser.EndTransaction());
            }

            using (var ser = OpenInfo())
            {
                Assert.AreEqual(2, ser.ArrayCount);
                CollectionAssert.AreEqual(new[] { 1, 2, 3 }, ser.ReadArray<int>(0));
                CollectionAssert.AreEqual(new[] { 4, 5 }, ser.ReadArray<int>(1));
            }
        }

        [Test]
        public void Begin_Cancel_Rollback_HappyPath()
        {
            using (var ser = CreateInfo(256))
            {
                ser.WriteMetadata();
                ser.SetArrayCapacity(4);

                ser.AppendArray(new[] { 10, 20 });
                Assert.AreEqual(1, ser.ArrayCount);

                ser.BeginTransaction(Path.GetTempPath());
                ser.AppendArray(new[] { 30 });
                Assert.AreEqual(2, ser.ArrayCount); // in staging

                Assert.IsTrue(ser.CancelTransaction());

                Assert.AreEqual(1, ser.ArrayCount);
                CollectionAssert.AreEqual(new[] { 10, 20 }, ser.ReadArray<int>(0));
            }

            using (var ser = OpenInfo())
            {
                Assert.AreEqual(1, ser.ArrayCount);
                CollectionAssert.AreEqual(new[] { 10, 20 }, ser.ReadArray<int>(0));
            }
        }

        [Test]
        public void Nested_Transactions_InnerCancel_OuterCommit()
        {
            using (var ser = CreateInfo(256))
            {
                ser.WriteMetadata();
                ser.SetArrayCapacity(4);

                ser.AppendArray(new[] { 1 }); // A

                ser.BeginTransaction(Path.GetTempPath()); // Outer
                ser.AppendArray(new[] { 2 }); // B
                Assert.AreEqual(2, ser.ArrayCount);

                ser.BeginTransaction(Path.GetTempPath()); // Inner
                ser.AppendArray(new[] { 3 }); // C
                Assert.AreEqual(3, ser.ArrayCount);

                Assert.IsTrue(ser.CancelTransaction()); // Cancel inner, drop C
                Assert.AreEqual(2, ser.ArrayCount);

                Assert.IsTrue(ser.EndTransaction()); // Commit outer, keep B
            }

            using (var ser = OpenInfo())
            {
                Assert.AreEqual(2, ser.ArrayCount);
                CollectionAssert.AreEqual(new[] { 1 }, ser.ReadArray<int>(0));
                CollectionAssert.AreEqual(new[] { 2 }, ser.ReadArray<int>(1));
            }
        }

        [Test]
        public void Nested_Transactions_InnerCommit_OuterCancel()
        {
            using (var ser = CreateInfo(256))
            {
                ser.WriteMetadata();
                ser.SetArrayCapacity(4);

                ser.AppendArray(new[] { 100 }); // A

                ser.BeginTransaction(Path.GetTempPath()); // Outer
                ser.AppendArray(new[] { 200 }); // B

                ser.BeginTransaction(Path.GetTempPath()); // Inner
                ser.AppendArray(new[] { 300 }); // C

                Assert.IsTrue(ser.EndTransaction()); // Commit inner (to outer stage)

                // At this point, staging has A,B,C
                Assert.AreEqual(3, ser.ArrayCount);

                Assert.IsTrue(ser.CancelTransaction()); // Cancel outer -> revert to only A

                Assert.AreEqual(1, ser.ArrayCount);
                CollectionAssert.AreEqual(new[] { 100 }, ser.ReadArray<int>(0));
            }

            using (var ser = OpenInfo())
            {
                Assert.AreEqual(1, ser.ArrayCount);
                CollectionAssert.AreEqual(new[] { 100 }, ser.ReadArray<int>(0));
            }
        }

        [Test]
        public void Transaction_With_BeginEndWrite_Order1()
        {
            using (var ser = CreateInfo(256))
            {
                ser.WriteMetadata();
                ser.SetArrayCapacity(4);

                ser.AppendArray(new[] { 7 });

                ser.BeginWrite();
                ser.BeginTransaction(Path.GetTempPath());
                ser.AppendArray(new[] { 8, 9 });
                ser.EndWrite();

                Assert.IsTrue(ser.EndTransaction());

                Assert.AreEqual(2, ser.ArrayCount);
                CollectionAssert.AreEqual(new[] { 7 }, ser.ReadArray<int>(0));
                CollectionAssert.AreEqual(new[] { 8, 9 }, ser.ReadArray<int>(1));
            }

            using (var ser = OpenInfo())
            {
                Assert.AreEqual(2, ser.ArrayCount);
                CollectionAssert.AreEqual(new[] { 7 }, ser.ReadArray<int>(0));
                CollectionAssert.AreEqual(new[] { 8, 9 }, ser.ReadArray<int>(1));
            }
        }

        [Test]
        public void Transaction_With_BeginEndWrite_Order2()
        {
            using (var ser = CreateInfo(256))
            {
                ser.WriteMetadata();
                ser.SetArrayCapacity(4);

                ser.AppendArray(new[] { 11 });

                ser.BeginTransaction(Path.GetTempPath());
                ser.BeginWrite();
                ser.AppendArray(new[] { 12 });
                Assert.IsTrue(ser.EndTransaction()); // commit while write in progress
                ser.EndWrite();

                Assert.AreEqual(2, ser.ArrayCount);
                CollectionAssert.AreEqual(new[] { 11 }, ser.ReadArray<int>(0));
                CollectionAssert.AreEqual(new[] { 12 }, ser.ReadArray<int>(1));
            }

            using (var ser = OpenInfo())
            {
                Assert.AreEqual(2, ser.ArrayCount);
                CollectionAssert.AreEqual(new[] { 11 }, ser.ReadArray<int>(0));
                CollectionAssert.AreEqual(new[] { 12 }, ser.ReadArray<int>(1));
            }
        }

        [Test]
        public void Dispose_Without_Explicit_CommitOrRollback_Discards_Changes()
        {
            using (var ser = CreateInfo(256))
            {
                ser.WriteMetadata();
                ser.SetArrayCapacity(4);

                ser.AppendArray(new[] { 1, 1 });
            }

            using (var ser = OpenInfo())
            {
                ser.BeginTransaction(Path.GetTempPath());
                ser.AppendArray(new[] { 2, 2 });
                // No commit/rollback here; Dispose should rollback via DiscardAllStages
            }

            using (var ser = OpenInfo())
            {
                Assert.AreEqual(1, ser.ArrayCount);
                CollectionAssert.AreEqual(new[] { 1, 1 }, ser.ReadArray<int>(0));
            }
        }
    }
}
