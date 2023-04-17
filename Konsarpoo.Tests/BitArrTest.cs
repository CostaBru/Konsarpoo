using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;

namespace Konsarpoo.Collections.Tests
{
    [TestFixture(16)]
    [TestFixture(1024)]
    [TestFixture(null)]
    public class BitArrTest
    {
        private readonly int m_maxSizeOfArrayBucket;

        public BitArrTest(int? maxSizeOfArrayBucket)
        {
            m_maxSizeOfArrayBucket = maxSizeOfArrayBucket ?? 1024 * 1024;
        }

        [SetUp]
        public void SetUp()
        {
            ArrayPoolGlobalSetup.SetMaxSizeOfArrayBucket(m_maxSizeOfArrayBucket);
        }
        
        [Test]
        public void TestSet([Values(true, false)] bool defaultVal)
        {
            {
                var length = 10000;

                var bitArr = new BitArr(length, defaultVal);
                var bitArr1 = new BitArr(length, defaultVal);

                var ba = new BitArray(length, defaultVal);

                var hashCode1 = bitArr.GetHashCode();

                for (int i = 0; i < length; i++)
                {
                    bitArr.Set(i, i % 2 == 0);
                    bitArr1[i] = i % 2 == 0;
                    ba.Set(i, i % 2 == 0);
                }

                Assert.AreEqual(bitArr.Count, ba.Count);

                int ii = 0;
                foreach (bool b in ba)
                {
                    Assert.AreEqual(b, bitArr[ii]);
                    Assert.AreEqual(b, bitArr1[ii]);

                    ii++;
                }

                bitArr.Dispose();

                Assert.AreEqual(0, bitArr.Count);
            }
            GC.Collect();
        }

        [Test]
        public void TestEnsure([Values(true, false)] bool defaultVal)
        {
            var length = 100;

            var bitArr = new BitArr(length, defaultVal);
            
            bitArr.Ensure(1000, !defaultVal);
            
            Assert.AreEqual(1000, bitArr.Count);
            
            GC.Collect();
        }
        
        [Test]
        public void TestLength([Values(true, false)] bool defaultVal)
        {
            var length = 100;

            var bitArr = new BitArr(length, defaultVal);
            
            bitArr.Length = 1000;
            
            Assert.AreEqual(1000, bitArr.Length);
            
            var ba = new BitArray(length, defaultVal);
            
            ba.Length = 1000;
            
            Assert.AreEqual(ba.Length, bitArr.Length);
        }
        
        [Test]
        public void TestAnd()
        {
            var length = 100;

            var bitArr1 = new BitArr(length, true);
            var bitArr2 = new BitArr(length, true);

            var bitArr = bitArr1.And(bitArr2);

            Assert.True(bitArr.All(b => b)); 
            
            bitArr1 = new BitArr(length, false);
            bitArr2 = new BitArr(length, false);

            bitArr = bitArr1.And(bitArr2);

            Assert.True(bitArr.All(b => !b)); 
        }
        
        [Test]
        public void TestOr()
        {
            var length = 100;

            var bitArr1 = new BitArr(length, true);
            var bitArr2 = new BitArr(length, true);

            var bitArr = bitArr1.Or(bitArr2);

            Assert.True(bitArr.All(b => b)); 
            
            bitArr1 = new BitArr(length, false);
            bitArr2 = new BitArr(length, false);

            bitArr = bitArr1.Or(bitArr2);

            Assert.True(bitArr.All(b => !b)); 
               
            bitArr1 = new BitArr(length, true);
            bitArr2 = new BitArr(length, false);

            bitArr = bitArr1.Or(bitArr2);

            Assert.True(bitArr.All(b => b)); 
        }
        
        [Test]
        public void TestNot()
        {
            var length = 100;

            var bitArr1 = new BitArr(length, true);

            var bitArr = bitArr1.Not();

            Assert.True(bitArr.All(b => !b)); 
            
            bitArr1 = new BitArr(length, false);

            bitArr = bitArr1.Not();

            Assert.True(bitArr.All(b => b)); 
        }
        
        [Test]
        public void TestXor()
        {
            var length = 100;

            var bitArr1 = new BitArr(length, true);
            var bitArr2 = new BitArr(length, true);

            var bitArr = bitArr1.Xor(bitArr2);

            Assert.True(bitArr.All(b => !b)); 
            
            bitArr1 = new BitArr(length, false);
            bitArr2 = new BitArr(length, false);

            bitArr = bitArr1.Xor(bitArr2);

            Assert.True(bitArr.All(b => !b)); 
               
            bitArr1 = new BitArr(length, true);
            bitArr2 = new BitArr(length, false);

            bitArr = bitArr1.Xor(bitArr2);

            Assert.True(bitArr.All(b => b)); 
        }

        [Test]
        public void TestSetAll()
        {
            var length = 100;

            var bitArr1 = new BitArr(length, true);
            
            bitArr1.SetAll(false);
            
            Assert.NotNull(bitArr1.SyncRoot);
            Assert.False(bitArr1.IsReadOnly);
            Assert.False(bitArr1.IsSynchronized);
            
            Assert.True(bitArr1.All(b => !b)); 
        }


        [Test]
        public void TestCopyTo([Values(true, false)] bool defaultVal)
        {
            var length = 100;

            var bitArr1 = new BitArr(length, defaultVal);

            var bools = new bool[length];
            
            bitArr1.CopyTo(bools, 0);
            
            Assert.True(bools.All(b => b == defaultVal));

            var arrCopyBitArr = new BitArr(bools);
            
            Assert.True((arrCopyBitArr.Equals(bitArr1)));
            
            var ints = new int[length];
            
            bitArr1.CopyTo(ints, 0);
            
            var bytes = new byte[length];
            
            bitArr1.CopyTo(bytes, 0);
        }
        
        [Test]
        public void TestAdd([Values(true, false)] bool defaultVal)
        {
            var length = 1000;

            var ba = new BitArr(length, defaultVal);

            for (int i = length; i < length + 1000; i++)
            {
                ba.SetOrAdd(i, true);

                Assert.True(ba.HasAndSet(i));
                Assert.False(ba.HasAndSet(i + 10000));
            }

            Assert.False(ba.TrySet(100000, false));
            Assert.True(ba.TrySet(0, false));

            var clone = (BitArr)ba.Clone();
            
            Assert.True((clone.Equals(ba)));
            
            var ctrCopy = new BitArr(ba);
            
            Assert.True((clone.Equals(ba)));
            Assert.True((ctrCopy.Equals(ba)));
        }

        [Test]
        public void TestClearAndSet([Values(true, false)] bool defaultVal)
        {
            var length = 100;

            var ba = new BitArr(length);
            
            ba.SetAll(defaultVal);
            
            ba.Clear();
            
            ba.SetOrAdd(1000, true);
            
            Assert.True(ba.HasAndSet(1000));
        }

        [Test]
        public void TestEnumerator([Values(true, false)] bool defaultVal)
        {
            var length = 100;

            var ba = new BitArr(length);
            
            ba.SetAll(defaultVal);

            foreach (var val in ba)
            {
                Assert.True(val == defaultVal);
            }
        }
        
        [Test]
        public void TestSetSerialization([Values(true, false)] bool defaultVal)
        {
            var length = 100;

            var ba = new BitArr(length, defaultVal);

            var serializeWithDcs = SerializeHelper.SerializeWithDcs(ba);

            var deserializeWithDcs = SerializeHelper.DeserializeWithDcs<BitArr>(serializeWithDcs);

            Assert.AreEqual(deserializeWithDcs, ba);
        }

        [Test]
        public void TestSetSerialization2([Values(true, false)] bool defaultVal)
        {
            var length = 100;

            var ba = new BitArr(length, defaultVal);

            var clone = SerializeHelper.Clone<BitArr>(ba);

            Assert.AreEqual(clone, ba);
        }

        [Test]
        public void EqualTests()
        {
            var length = 100;

            var ba1 = new BitArr(length, true);
            var ba2 = new BitArr(length, true);
            
            Assert.False(ba1.Equals(null));
            Assert.True(ba1.Equals(ba1));
            Assert.True(ba1.Equals(ba2));
        }

       
        [Test]
        public void TestCtr()
        {
            var ba1 = new BitArr(new bool[0]);
            
            Assert.AreEqual(0, ba1.Length);

            Assert.Throws<ArgumentOutOfRangeException>(() => ba1.Get(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ba1.Get(1));
        }

        [Test]
        public void TestSetAllHuge()
        {
            var ba1 = new BitArr(100000);

            ba1.SetAll(true);
        }
    }
}