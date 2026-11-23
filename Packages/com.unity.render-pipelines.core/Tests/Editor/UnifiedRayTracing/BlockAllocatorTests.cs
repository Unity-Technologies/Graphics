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
            try
            {
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
            finally
            {
                allocator.Dispose();
            }
        }

        [Test]
        public void GrowAndAllocate_NotEnoughSpaceMaxInt_ShouldFail()
        {
            var allocator = new BlockAllocator();
            allocator.Initialize(int.MaxValue);
            try
            {
                var a = allocator.Allocate(int.MaxValue / 2);

                int oldCapacity;
                int newCapacity;

                var alloc = allocator.GrowAndAllocate(3, int.MaxValue, out oldCapacity, out newCapacity);
                Assert.IsTrue(alloc.valid);

                alloc = allocator.GrowAndAllocate(int.MaxValue / 2, int.MaxValue, out oldCapacity, out newCapacity);
                Assert.IsFalse(alloc.valid);
            }
            finally
            {
                allocator.Dispose();
            }
        }

        [Test]
        public void FreeAllocation_ShuffledSingleElementFrees_CoalescesToOneBlock()
        {
            const int count = 4096;
            var allocator = new BlockAllocator();
            allocator.Initialize(count);
            try
            {
                var allocations = new BlockAllocator.Allocation[count];
                for (int i = 0; i < count; i++)
                {
                    allocations[i] = allocator.Allocate(1);
                    Assert.IsTrue(allocations[i].valid);
                }

                foreach (int i in ShuffledIndices(count, seed: 1234))
                    allocator.FreeAllocation(allocations[i]);

                allocator.ValidateInvariants();
                Assert.AreEqual(1, allocator.freeBlocks, "Shuffled frees should coalesce back to a single free block.");
                Assert.AreEqual(count, allocator.freeElementsCount);
                Assert.IsTrue(allocator.Allocate(count).valid, "The coalesced block should satisfy a full-capacity allocation.");
            }
            finally
            {
                allocator.Dispose();
            }
        }

        [Test]
        public void FreeAllocation_EveryOtherFree_MaxFragmentationThenFullCoalesce()
        {
            const int count = 1024;
            var allocator = new BlockAllocator();
            allocator.Initialize(count);
            try
            {
                var allocations = new BlockAllocator.Allocation[count];
                for (int i = 0; i < count; i++)
                    allocations[i] = allocator.Allocate(1);

                for (int i = 0; i < count; i += 2)
                    allocator.FreeAllocation(allocations[i]);

                allocator.ValidateInvariants();
                Assert.AreEqual(count / 2, allocator.freeBlocks, "Non-adjacent frees should not coalesce.");

                for (int i = 1; i < count; i += 2)
                    allocator.FreeAllocation(allocations[i]);

                allocator.ValidateInvariants();
                Assert.AreEqual(1, allocator.freeBlocks);
                Assert.AreEqual(count, allocator.freeElementsCount);
            }
            finally
            {
                allocator.Dispose();
            }
        }

        [Test]
        public void FreeAllocation_MixedSizesShuffled_CoalescesAndReuses()
        {
            var allocator = new BlockAllocator();
            allocator.Initialize(10000);
            try
            {
                var random = new System.Random(5678);
                var allocations = new System.Collections.Generic.List<BlockAllocator.Allocation>();
                while (true)
                {
                    var allocation = allocator.Allocate(random.Next(1, 18));
                    if (!allocation.valid)
                        break;
                    allocations.Add(allocation);
                }

                allocator.ValidateInvariants();

                foreach (int i in ShuffledIndices(allocations.Count, seed: 42))
                    allocator.FreeAllocation(allocations[i]);

                allocator.ValidateInvariants();
                Assert.AreEqual(1, allocator.freeBlocks, "Mixed-size shuffled frees should coalesce back to a single free block.");
                Assert.AreEqual(10000, allocator.freeElementsCount);

                Assert.IsTrue(allocator.Allocate(10000).valid);
            }
            finally
            {
                allocator.Dispose();
            }
        }

        [Test]
        public void SplitAllocation_FreedPartsShuffled_Coalesce()
        {
            var allocator = new BlockAllocator();
            allocator.Initialize(256);
            try
            {
                var block = allocator.Allocate(64);
                var parts = allocator.SplitAllocation(block, 64);

                foreach (int i in ShuffledIndices(parts.Length, seed: 99))
                    allocator.FreeAllocation(parts[i]);

                allocator.ValidateInvariants();
                Assert.AreEqual(1, allocator.freeBlocks, "Freed split parts should coalesce with the remaining free space.");
                Assert.AreEqual(256, allocator.freeElementsCount);
            }
            finally
            {
                allocator.Dispose();
            }
        }

        [Test]
        public void GrowAndAllocate_LastFreeListEntryIsNotTailBlock_GrowsEnough()
        {
            // The required growth must be computed from the free block adjacent to the capacity end, not from
            // whichever block happens to be last in the free list.
            var allocator = new BlockAllocator();
            allocator.Initialize(100);
            try
            {
                var a = allocator.Allocate(60);
                var b = allocator.Allocate(40);
                Assert.IsTrue(a.valid && b.valid);
                allocator.FreeAllocation(a); // free list holds only the interior block [0, 60); the tail is fully allocated

                var alloc = allocator.GrowAndAllocate(200, 1000, out _, out _);
                allocator.ValidateInvariants();
                Assert.IsTrue(alloc.valid);
                Assert.AreEqual(200, alloc.block.count);
            }
            finally
            {
                allocator.Dispose();
            }
        }

        [Test]
        public void FreeAllocation_ManyShuffledFrees_CompletesQuickly()
        {
            const int count = 100000;
            var allocator = new BlockAllocator();
            allocator.Initialize(count);
            try
            {
                var allocations = new BlockAllocator.Allocation[count];
                for (int i = 0; i < count; i++)
                    allocations[i] = allocator.Allocate(1);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                foreach (int i in ShuffledIndices(count, seed: 7))
                    allocator.FreeAllocation(allocations[i]);
                stopwatch.Stop();

                allocator.ValidateInvariants();
                Assert.AreEqual(1, allocator.freeBlocks);
                // O(n^2) coalescing took minutes at this size; the bound is generous to stay robust on slow agents.
                Assert.Less(stopwatch.ElapsedMilliseconds, 5000, "Mass freeing regressed to super-linear behavior.");
            }
            finally
            {
                allocator.Dispose();
            }
        }

        [Test]
        public void Allocate_ZeroElementsFromZeroCapacityAllocator_ReturnsValidEmptyAllocation()
        {
            var allocator = new BlockAllocator();
            allocator.Initialize(0);
            try
            {
                var allocation = allocator.Allocate(0);
                Assert.IsTrue(allocation.valid, "A zero-element allocation should succeed on an empty, growable allocator.");
                Assert.AreEqual(0, allocation.block.count);

                var grown = allocator.GrowAndAllocate(0, out _, out _);
                Assert.IsTrue(grown.valid, "GrowAndAllocate of zero elements should succeed on an empty allocator.");

                Assert.IsFalse(allocator.Allocate(1).valid);

                allocator.FreeAllocation(allocation);
                allocator.FreeAllocation(grown);
                allocator.ValidateInvariants();
            }
            finally
            {
                allocator.Dispose();
            }
        }

        [Test]
        public void Allocate_ZeroElements_ReturnsValidEmptyAllocation()
        {
            var allocator = new BlockAllocator();
            allocator.Initialize(64);
            try
            {
                var allocation = allocator.Allocate(0);
                Assert.IsTrue(allocation.valid, "A zero-element allocation should succeed like any other.");
                Assert.AreEqual(0, allocation.block.count);
                Assert.AreEqual(64, allocator.freeElementsCount, "A zero-element allocation should not consume space.");
                Assert.AreEqual(1, allocator.freeBlocks);

                allocator.FreeAllocation(allocation);
                allocator.ValidateInvariants();
                Assert.AreEqual(64, allocator.freeElementsCount);
                Assert.AreEqual(1, allocator.freeBlocks);

                Assert.IsTrue(allocator.Allocate(64).valid);
            }
            finally
            {
                allocator.Dispose();
            }
        }

        static int[] ShuffledIndices(int count, int seed)
        {
            var indices = new int[count];
            for (int i = 0; i < count; i++)
                indices[i] = i;

            var random = new System.Random(seed);
            for (int i = count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            return indices;
        }
}
}
