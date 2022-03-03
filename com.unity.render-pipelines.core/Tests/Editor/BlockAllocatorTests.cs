using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Tests
{
    class BlockAllocatorTests
    {
        //[SetUp]
        //nothing to setup.

        //[TearDown]
        //nothing to do tear down.

        [Test]
        public void TestBlockAllocatorAddRemove()
        {
            BlockAllocator ba = new BlockAllocator();
            ba.Initialize(4);

            var a0 = ba.Allocate(1);
            var a1 = ba.Allocate(1);
            var a2 = ba.Allocate(1);
            var a3 = ba.Allocate(1);

            var a4 = ba.Allocate(10);
            Assert.IsTrue(!a4.valid);

            ba.FreeAllocation(a0);
            ba.FreeAllocation(a1);
            ba.FreeAllocation(a2);
            ba.FreeAllocation(a3);

            Assert.AreEqual(ba.freeElementsCount, 4);
            Assert.AreEqual(ba.freeBlocks, 1);

            ba.Dispose();
        }

        [Test]
        public void TestMergeFreeBlocks()
        {
            BlockAllocator ba = new BlockAllocator();
            ba.Initialize(8);
            var a0 = ba.Allocate(2);
            var a1 = ba.Allocate(2); //These two allocations get merged when freed.
            Assert.AreEqual(ba.freeElementsCount, 4);

            var a2 = ba.Allocate(2);
            var a3 = ba.Allocate(2);
            Assert.AreEqual(ba.freeElementsCount, 0);

            ba.FreeAllocation(a1);
            ba.FreeAllocation(a2);
            Assert.AreEqual(ba.freeElementsCount, 4);

            var a4 = ba.Allocate(4);
            Assert.IsTrue(a4.valid);
            Assert.AreEqual(ba.freeElementsCount, 0);
            ba.FreeAllocation(a4);

            //Now try to merge the inverse
            a1 = ba.Allocate(2);
            Assert.IsTrue(a1.valid);
            a2 = ba.Allocate(2);
            Assert.IsTrue(a2.valid);

            ba.FreeAllocation(a2);
            ba.FreeAllocation(a1);

            a4 = ba.Allocate(4);
            Assert.IsTrue(a4.valid);
            Assert.AreEqual(ba.freeElementsCount, 0);
            ba.FreeAllocation(a4);

            ba.FreeAllocation(a0);
            ba.FreeAllocation(a3);

            Assert.AreEqual(ba.freeElementsCount, 8);
            Assert.AreEqual(ba.freeBlocks, 1);
            ba.Dispose();
        }

        [Test]
        public void TestFragmentation()
        {
            BlockAllocator ba = new BlockAllocator();
            ba.Initialize(8);

            var a0 = ba.Allocate(1);
            var a1 = ba.Allocate(2);
            var a2 = ba.Allocate(3);
            var a3 = ba.Allocate(2);

            var a4 = ba.Allocate(4);
            Assert.IsTrue(!a4.valid);

            ba.FreeAllocation(a1);
            ba.FreeAllocation(a3);

            a4 = ba.Allocate(4);
            Assert.IsTrue(!a4.valid);

            ba.FreeAllocation(a2);
            a4 = ba.Allocate(7);
            Assert.IsTrue(a4.valid);

            ba.Dispose();
        }
    }
}
