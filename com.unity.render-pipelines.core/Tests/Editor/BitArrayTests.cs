using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Tests
{
    class BitArrayTests
    {
        BitArray8[] ba8;
        BitArray16[] ba16;
        BitArray32[] ba32;
        BitArray64[] ba64;
        BitArray128[] ba128;
        BitArray256[] ba256;

        static readonly uint[] aIndexes = new uint[] { 300, 200, 198, 100, 98, 60, 58, 30, 28, 10, 8, 4, 2, 0, 0 }; //double 0 entry to test double entry
        static readonly uint[] bIndexes = new uint[] { 300, 200, 199, 100, 99, 60, 59, 30, 29, 10, 9, 8, 5, 1, 0 };
        static readonly uint[] getSetTestedIndexes = new uint[] { 201, 200, 101, 100, 61, 60, 31, 30, 11, 10, 1, 0 };   // on a, odd value are false, even true
        const string aHumanized =       "00000000.00000000.00000000.00000000.00000000.00000000.00000001.01000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00010100.00000000.00000000.00000000.00000000.00010100.00000000.00000000.00000000.01010000.00000000.00000101.00010101";
        const string bHumanized =       "00000000.00000000.00000000.00000000.00000000.00000000.00000001.10000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00011000.00000000.00000000.00000000.00000000.00011000.00000000.00000000.00000000.01100000.00000000.00000111.00100011";
        const string aAndBHumanized =   "00000000.00000000.00000000.00000000.00000000.00000000.00000001.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00010000.00000000.00000000.00000000.00000000.00010000.00000000.00000000.00000000.01000000.00000000.00000101.00000001";
        const string aOrBHumanized =    "00000000.00000000.00000000.00000000.00000000.00000000.00000001.11000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00011100.00000000.00000000.00000000.00000000.00011100.00000000.00000000.00000000.01110000.00000000.00000111.00110111";
        const string notAHumanized =    "11111111.11111111.11111111.11111111.11111111.11111111.11111110.10111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11101011.11111111.11111111.11111111.11111111.11101011.11111111.11111111.11111111.10101111.11111111.11111010.11101010";
        const string zeroHumanized =    "00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000.00000000";
        const string maxHumanized =     "11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111.11111111";

        [SetUp]
        public void SetUpBitArray()
        {
            ba8 = new[] { new BitArray8(), new BitArray8(aIndexes), new BitArray8(bIndexes), new BitArray8(byte.MaxValue) };
            ba16 = new[] { new BitArray16(), new BitArray16(aIndexes), new BitArray16(bIndexes), new BitArray16(ushort.MaxValue) };
            ba32 = new[] { new BitArray32(), new BitArray32(aIndexes), new BitArray32(bIndexes), new BitArray32(uint.MaxValue) };
            ba64 = new[] { new BitArray64(), new BitArray64(aIndexes), new BitArray64(bIndexes), new BitArray64(ulong.MaxValue) };
            ba128 = new[] { new BitArray128(), new BitArray128(aIndexes), new BitArray128(bIndexes), new BitArray128(ulong.MaxValue, ulong.MaxValue) };
            ba256 = new[] { new BitArray256(), new BitArray256(aIndexes), new BitArray256(bIndexes), new BitArray256(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue) };
        }

        //[TearDown]
        //nothing to do as they are non static struct

        string GetLastHumanizedBits(string a, uint bitNumber) => a.Substring(a.Length - ((int)bitNumber + ((int)bitNumber - 1) / 8));  //handle '.' separators

        void TestBitArrayMethods<T>(T[] ba, uint capacity)
            where T : IBitArray
        {
            Assert.AreEqual(capacity, ba[0].capacity);
            Assert.AreEqual(true, ba[0].allFalse);
            Assert.AreEqual(false, ba[0].allTrue);
            var trimmedZeroHumanized = GetLastHumanizedBits(zeroHumanized, capacity);
            var humanized = ba[0].humanizedData;
            Assert.AreEqual(trimmedZeroHumanized, humanized);

            Assert.AreEqual(capacity, ba[1].capacity);
            Assert.AreEqual(false, ba[1].allFalse);
            Assert.AreEqual(false, ba[1].allTrue);
            var trimmedAHumanized = GetLastHumanizedBits(aHumanized, capacity);
            humanized = ba[1].humanizedData;
            Assert.AreEqual(trimmedAHumanized, humanized);

            Assert.AreEqual(capacity, ba[2].capacity);
            Assert.AreEqual(false, ba[2].allFalse);
            Assert.AreEqual(false, ba[2].allTrue);
            var trimmedBHumanized = GetLastHumanizedBits(bHumanized, capacity);
            humanized = ba[2].humanizedData;
            Assert.AreEqual(trimmedBHumanized, humanized);

            Assert.AreEqual(capacity, ba[3].capacity);
            Assert.AreEqual(false, ba[3].allFalse);
            Assert.AreEqual(true, ba[3].allTrue);
            var trimmedMaxHumnized = GetLastHumanizedBits(maxHumanized, capacity);
            humanized = ba[3].humanizedData;
            Assert.AreEqual(trimmedMaxHumnized, humanized);
        }

        void TestBitArrayOperator<T>(T[] ba)
            where T : IBitArray
        {
            //ensure we keep value type when refactoring
            var ba_4 = ba[1];
            Assert.AreEqual(ba_4, ba[1]);
            Assert.AreNotSame(ba_4, ba[1]);
            ba_4 = ba[2];
            Assert.AreNotEqual(ba_4, ba[1]);

            //test and
            var bAndA = ba[2].BitAnd(ba[1]);
            var aAndB = ba[1].BitAnd(ba[2]);
            Assert.AreEqual(bAndA, aAndB);
            Assert.AreEqual(bAndA.humanizedData, GetLastHumanizedBits(aAndBHumanized, ba[0].capacity));

            //test or
            var bOrA = ba[2].BitOr(ba[1]);
            var aOrB = ba[1].BitOr(ba[2]);
            Assert.AreEqual(bOrA, aOrB);
            Assert.AreEqual(bOrA.humanizedData, GetLastHumanizedBits(aOrBHumanized, ba[0].capacity));

            //test not
            var notA = ba[1].BitNot();
            Assert.AreEqual(notA.humanizedData, GetLastHumanizedBits(notAHumanized, ba[0].capacity));

            //test indexer
            foreach (uint index in getSetTestedIndexes.Where(i => i < ba[0].capacity))
            {
                //test get
                Assert.AreEqual(ba[1][index], (index & 1) == 0); //on a, odd value are false and even true

                //test set
                var bai = ba[1];
                bai[index] = ba[1][index];
                Assert.AreEqual(ba[1], bai);
                bai[index] = !ba[1][index];
                Assert.AreNotEqual(ba[1], bai);
                Assert.AreEqual(bai[index], !ba[1][index]);
            }
        }

        [Test]
        public void TestBitArray8()
        {
            TestBitArrayMethods(ba8, 8u);
            TestBitArrayOperator(ba8);
        }

        [Test]
        public void TestBitArray16()
        {
            TestBitArrayMethods(ba16, 16u);
            TestBitArrayOperator(ba16);
        }

        [Test]
        public void TestBitArray32()
        {
            TestBitArrayMethods(ba32, 32u);
            TestBitArrayOperator(ba32);
        }

        [Test]
        public void TestBitArray64()
        {
            TestBitArrayMethods(ba64, 64u);
            TestBitArrayOperator(ba64);
        }

        [Test]
        public void TestBitArray128()
        {
            TestBitArrayMethods(ba128, 128u);
            TestBitArrayOperator(ba128);
        }

        [Test]
        public void TestBitArray256()
        {
            TestBitArrayMethods(ba256, 256u);
            TestBitArrayOperator(ba256);
        }
    }
}
