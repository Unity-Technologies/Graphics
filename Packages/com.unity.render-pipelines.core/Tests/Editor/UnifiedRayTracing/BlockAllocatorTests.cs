using NUnit.Framework;
using System;
using UnityEditor;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering.RadeonRays;

namespace UnityEngine.Rendering.UnifiedRayTracing.Tests
{
    internal class BlockAllocatorTests
    {
        [Test]
        public void GrowAndAllocate_NotEnoughSpace_ShouldFail()
        {
            var allocator = new BlockAllocator();
            allocator.Initialize(500);
            for (int i = 0; i < 50; i++)
            {
                var a = allocator.Allocate(2);
                Assert.IsTrue(a.valid);
            }

            int oldCapacity;
            int newCapacity;

            var alloc = allocator.GrowAndAllocate(500, 300, out oldCapacity, out newCapacity);
            Assert.IsFalse(alloc.valid);

            alloc = allocator.GrowAndAllocate(500, 600, out oldCapacity, out newCapacity);
            Assert.IsTrue(alloc.valid);

            alloc = allocator.GrowAndAllocate(2, 600, out oldCapacity, out newCapacity);
            Assert.IsFalse(alloc.valid);
        }

        [Test]
        public void GrowAndAllocate_NotEnoughSpaceMaxInt_ShouldFail()
        {
            var allocator = new BlockAllocator();
            allocator.Initialize(int.MaxValue);
            var a = allocator.Allocate(int.MaxValue / 2);

            int oldCapacity;
            int newCapacity;

            var alloc = allocator.GrowAndAllocate(3, int.MaxValue, out oldCapacity, out newCapacity);
            Assert.IsTrue(alloc.valid);

            alloc = allocator.GrowAndAllocate(int.MaxValue / 2, int.MaxValue, out oldCapacity, out newCapacity);
            Assert.IsFalse(alloc.valid);
        }
}
}
