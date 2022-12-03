using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal.Tests
{
    class BuddyAllocatorTests
    {
        static IEnumerable<int> levelsCounts1D = Enumerable.Range(1, 24);

        [TestCaseSource(nameof(levelsCounts1D))]
        public void Empty1D(int levelCount)
        {
            var allocator = new BuddyAllocator(levelCount, 1, Allocator.Temp);
            allocator.Dispose();
        }

        static IEnumerable<int> levelsCounts2D = Enumerable.Range(1, 12);

        [TestCaseSource(nameof(levelsCounts2D))]
        public void Empty2D(int levelCount)
        {
            var allocator = new BuddyAllocator(levelCount, 2, Allocator.Temp);
            allocator.Dispose();
        }

        static IEnumerable<int> levelsCounts3D = Enumerable.Range(1, 8);

        [TestCaseSource(nameof(levelsCounts3D))]
        public void Empty3D(int levelCount)
        {
            var allocator = new BuddyAllocator(levelCount, 3, Allocator.Temp);
            allocator.Dispose();
        }

        [Test]
        public void Allocate1()
        {
            using var allocator = new BuddyAllocator(8, 2);
            Assert.IsTrue(allocator.TryAllocate(0, out var allocation));
            Assert.AreEqual(0, allocation.index);
        }

        [Test]
        public void Allocate6()
        {
            using var allocator = new BuddyAllocator(8, 2);
            Assert.IsTrue(allocator.TryAllocate(1, out var a0));
            Assert.AreEqual(math.uint2(0, 0), a0.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a1));
            Assert.AreEqual(math.uint2(1, 0), a1.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a2));
            Assert.AreEqual(math.uint2(0, 1), a2.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a3));
            Assert.AreEqual(math.uint2(1, 1), a3.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a4));
            Assert.AreEqual(math.uint2(2, 0), a4.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a5));
            Assert.AreEqual(math.uint2(3, 0), a5.index2D);
        }

        [Test]
        public void Allocate6Recycle2()
        {
            using var allocator = new BuddyAllocator(3, 2);
            Assert.IsTrue(allocator.TryAllocate(1, out var a0));
            Assert.AreEqual(math.uint2(0, 0), a0.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a1));
            Assert.AreEqual(math.uint2(1, 0), a1.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a2));
            Assert.AreEqual(math.uint2(0, 1), a2.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a3));
            Assert.AreEqual(math.uint2(1, 1), a3.index2D);

            allocator.Free(a3);
            allocator.Free(a2);

            Assert.IsTrue(allocator.TryAllocate(1, out a2));
            Assert.AreEqual(math.uint2(0, 1), a2.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out a3));
            Assert.AreEqual(math.uint2(1, 1), a3.index2D);

            Assert.IsTrue(allocator.TryAllocate(1, out var a4));
            Assert.AreEqual(math.uint2(2, 0), a4.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a5));
            Assert.AreEqual(math.uint2(3, 0), a5.index2D);
        }

        [Test]
        public void Allocate6Recycle4()
        {
            using var allocator = new BuddyAllocator(3, 2);
            Assert.IsTrue(allocator.TryAllocate(1, out var a0));
            Assert.AreEqual(math.uint2(0, 0), a0.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a1));
            Assert.AreEqual(math.uint2(1, 0), a1.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a2));
            Assert.AreEqual(math.uint2(0, 1), a2.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a3));
            Assert.AreEqual(math.uint2(1, 1), a3.index2D);

            Assert.IsTrue(allocator.TryAllocate(1, out var a4));
            Assert.AreEqual(math.uint2(2, 0), a4.index2D);
            Assert.IsTrue(allocator.TryAllocate(1, out var a5));
            Assert.AreEqual(math.uint2(3, 0), a5.index2D);

            allocator.Free(a0);
            allocator.Free(a1);
            allocator.Free(a2);
            allocator.Free(a3);

            Assert.IsTrue(allocator.TryAllocate(0, out var a6));
            Assert.AreEqual(math.uint2(0, 0), a6.index2D);
        }

        [Test]
        public void CubemapScenario1()
        {
            using var allocator = new BuddyAllocator(3, 2);

            // Allocate 6 items on level 2. This will use up (0, 0) and (1, 0) on level 1.
            Assert.IsTrue(allocator.TryAllocate(2, out var a00));
            Assert.IsTrue(allocator.TryAllocate(2, out var a01));
            Assert.IsTrue(allocator.TryAllocate(2, out var a02));
            Assert.IsTrue(allocator.TryAllocate(2, out var a03));
            Assert.IsTrue(allocator.TryAllocate(2, out var a04));
            Assert.IsTrue(allocator.TryAllocate(2, out var a05));
            Assert.AreEqual(math.uint2(0, 0), a00.index2D);
            Assert.AreEqual(math.uint2(1, 0), a01.index2D);
            Assert.AreEqual(math.uint2(0, 1), a02.index2D);
            Assert.AreEqual(math.uint2(1, 1), a03.index2D);
            Assert.AreEqual(math.uint2(2, 0), a04.index2D);
            Assert.AreEqual(math.uint2(3, 0), a05.index2D);

            Assert.IsTrue(allocator.TryAllocate(1, out var a10));
            Assert.IsTrue(allocator.TryAllocate(1, out var a11));
            Assert.IsTrue(allocator.TryAllocate(1, out var a12));
            Assert.IsTrue(allocator.TryAllocate(1, out var a13));
            Assert.IsTrue(allocator.TryAllocate(1, out var a14));
            Assert.IsTrue(allocator.TryAllocate(1, out var a15));
            Assert.AreEqual(math.uint2(0, 1), a10.index2D);
            Assert.AreEqual(math.uint2(1, 1), a11.index2D);
            Assert.AreEqual(math.uint2(2, 0), a12.index2D);
            Assert.AreEqual(math.uint2(3, 0), a13.index2D);
            Assert.AreEqual(math.uint2(2, 1), a14.index2D);
            Assert.AreEqual(math.uint2(3, 1), a15.index2D);

            // This should make (0, 0) and (1, 0) available on level 1 again.
            allocator.Free(a05);
            allocator.Free(a04);
            allocator.Free(a03);
            allocator.Free(a02);
            allocator.Free(a01);
            allocator.Free(a00);

            Assert.IsTrue(allocator.TryAllocate(1, out var a20));
            Assert.IsTrue(allocator.TryAllocate(1, out var a21));
            Assert.IsTrue(allocator.TryAllocate(1, out var a22));
            Assert.IsTrue(allocator.TryAllocate(1, out var a23));
            Assert.IsTrue(allocator.TryAllocate(1, out var a24));
            Assert.IsTrue(allocator.TryAllocate(1, out var a25));
            Assert.AreEqual(math.uint2(0, 0), a20.index2D);
            Assert.AreEqual(math.uint2(1, 0), a21.index2D);
            Assert.AreEqual(math.uint2(0, 2), a22.index2D);
            Assert.AreEqual(math.uint2(1, 2), a23.index2D);
            Assert.AreEqual(math.uint2(0, 3), a24.index2D);
            Assert.AreEqual(math.uint2(1, 3), a25.index2D);
        }
    }
}
