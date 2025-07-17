using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal struct BlockAllocator : IDisposable
    {
        public struct Block
        {
            public int offset;
            public int count;

            public static readonly Block Invalid = new Block() { offset = 0, count = 0 };
        }

        public struct Allocation
        {
            public int handle;
            public Block block;

            public static readonly Allocation Invalid = new Allocation() { handle = -1 };
            public readonly bool valid => handle != -1;
        }

        private int m_FreeElementCount;
        private int m_MaxElementCount;
        private NativeList<Block> m_freeBlocks;
        private NativeList<Block> m_usedBlocks;
        private NativeList<int> m_freeSlots;

        public int freeElementsCount => m_FreeElementCount;
        public int freeBlocks => m_freeBlocks.Length;
        public int capacity => m_MaxElementCount;
        public int allocatedSize => m_MaxElementCount - m_FreeElementCount;

        public void Initialize(int maxElementCounts)
        {
            m_MaxElementCount = maxElementCounts;
            m_FreeElementCount = maxElementCounts;

            if (!m_freeBlocks.IsCreated)
                m_freeBlocks = new NativeList<Block>(Allocator.Persistent);
            else
                m_freeBlocks.Clear();
            m_freeBlocks.Add(new Block() { offset = 0, count = m_FreeElementCount });

            if (!m_usedBlocks.IsCreated)
                m_usedBlocks = new NativeList<Block>(Allocator.Persistent);
            else
                m_usedBlocks.Clear();

            if (!m_freeSlots.IsCreated)
                m_freeSlots = new NativeList<int>(Allocator.Persistent);
            else
                m_freeSlots.Clear();
        }

        private int CalculateGeometricGrowthCapacity(int desiredNewCapacity, int maxAllowedNewCapacity)
        {
            var oldCapacity = capacity;

            if (oldCapacity > maxAllowedNewCapacity - oldCapacity / 2)
            {
                return maxAllowedNewCapacity; // geometric growth would overflow
            }

            var geometricNewCapacity = oldCapacity + oldCapacity / 2;

            if (geometricNewCapacity < desiredNewCapacity)
                return desiredNewCapacity; // geometric growth would be insufficient

            else
                return geometricNewCapacity;
        }

        public int Grow(int newDesiredCapacity, int maxAllowedCapacity = Int32.MaxValue)
        {
            Debug.Assert(newDesiredCapacity > 0, "newDesiredCapacity must be positive");
            Debug.Assert(maxAllowedCapacity > 0, "maxAllowedCapacity must be positive");
            Debug.Assert(capacity < newDesiredCapacity, "newDesiredCapacity must be greater than curent capacity");
            Debug.Assert(maxAllowedCapacity >= newDesiredCapacity, "newDesiredCapacity must be smaller than maxAllowedCapacity");

            var newCapacity = CalculateGeometricGrowthCapacity(newDesiredCapacity, maxAllowedCapacity);
            var oldCapacity = m_MaxElementCount;
            var addedElements = newCapacity - oldCapacity;
            Debug.Assert(addedElements > 0);

            m_FreeElementCount += addedElements;
            m_MaxElementCount = newCapacity;

            int blockToMerge = m_freeBlocks.Length;
            m_freeBlocks.Add(new Block() { offset = oldCapacity, count = addedElements });

            while (blockToMerge != -1)
                blockToMerge = MergeBlockFrontBack(blockToMerge);

            return m_MaxElementCount;
        }

        public bool GetExpectedGrowthToFitAllocation(int elementCounts, int maxAllowedCapacity, out int newCapacity)
        {
            newCapacity = 0;

            var additionalRequiredElements = m_freeBlocks.IsEmpty ? elementCounts : math.max(elementCounts - m_freeBlocks[m_freeBlocks.Length - 1].count, 0);
            if (maxAllowedCapacity < capacity || (maxAllowedCapacity - capacity) < additionalRequiredElements)
                return false;

            newCapacity = additionalRequiredElements > 0 ? CalculateGeometricGrowthCapacity(capacity + additionalRequiredElements, maxAllowedCapacity) : capacity;
            return true;
        }

        public Allocation GrowAndAllocate(int elementCounts, out int oldCapacity, out int newCapacity)
        {
            return GrowAndAllocate(elementCounts, Int32.MaxValue, out oldCapacity, out newCapacity);
        }

        public Allocation GrowAndAllocate(int elementCounts, int maxAllowedCapacity, out int oldCapacity, out int newCapacity)
        {
            oldCapacity = capacity;

            var additionalRequiredElements = m_freeBlocks.IsEmpty ? elementCounts : math.max(elementCounts - m_freeBlocks[m_freeBlocks.Length - 1].count, 0);
            if (maxAllowedCapacity < capacity || (maxAllowedCapacity - capacity) < additionalRequiredElements)
            {
                newCapacity = capacity;
                return Allocation.Invalid;
            }

            newCapacity = additionalRequiredElements > 0 ? Grow(capacity + additionalRequiredElements, maxAllowedCapacity) : capacity;
            Debug.Assert(newCapacity >= oldCapacity + additionalRequiredElements);

            var alloc = Allocate(elementCounts);
            Assert.IsTrue(alloc.valid);
            return alloc;
        }

        public void Dispose()
        {
            m_MaxElementCount = 0;
            m_FreeElementCount = 0;
            if (m_freeBlocks.IsCreated)
                m_freeBlocks.Dispose();
            if (m_usedBlocks.IsCreated)
                m_usedBlocks.Dispose();
            if (m_freeSlots.IsCreated)
                m_freeSlots.Dispose();
        }

        public Allocation Allocate(int elementCounts)
        {
            if (elementCounts > m_FreeElementCount || m_freeBlocks.IsEmpty)
                return Allocation.Invalid;

            int selectedBlock = -1;
            int currentBlockCount = 0;
            for (int b = 0; b < m_freeBlocks.Length; ++b)
            {
                Block block = m_freeBlocks[b];

                //simple naive allocator, we find the smallest possible space to allocate in our blocks.
                if (elementCounts <= block.count && (selectedBlock == -1 || block.count < currentBlockCount))
                {
                    currentBlockCount = block.count;
                    selectedBlock = b;
                }
            }

            if (selectedBlock == -1)
                return Allocation.Invalid;

            Block allocationBlock = m_freeBlocks[selectedBlock];
            Block split = allocationBlock;

            split.offset += elementCounts;
            split.count -= elementCounts;
            allocationBlock.count = elementCounts;

            if (split.count > 0)
                m_freeBlocks[selectedBlock] = split;
            else
                m_freeBlocks.RemoveAtSwapBack(selectedBlock);

            int allocationHandle;
            if (m_freeSlots.IsEmpty)
            {
                allocationHandle = m_usedBlocks.Length;
                m_usedBlocks.Add(allocationBlock);
            }
            else
            {
                allocationHandle = m_freeSlots[m_freeSlots.Length - 1];
                m_freeSlots.RemoveAtSwapBack(m_freeSlots.Length - 1);
                m_usedBlocks[allocationHandle] = allocationBlock;
            }

            m_FreeElementCount -= elementCounts;
            return new Allocation() { handle = allocationHandle, block = allocationBlock };
        }

        private int MergeBlockFrontBack(int freeBlockId)
        {
            Block targetBlock = m_freeBlocks[freeBlockId];
            for (int i = 0; i < m_freeBlocks.Length; ++i)
            {
                if (i == freeBlockId)
                    continue;

                Block freeBlock = m_freeBlocks[i];
                bool mergeTargetBlock = false;
                if (targetBlock.offset == (freeBlock.offset + freeBlock.count))
                {
                    freeBlock.count += targetBlock.count;
                    mergeTargetBlock = true;
                }
                else if (freeBlock.offset == (targetBlock.offset + targetBlock.count))
                {
                    freeBlock.offset = targetBlock.offset;
                    freeBlock.count += targetBlock.count;
                    mergeTargetBlock = true;
                }

                if (mergeTargetBlock)
                {
                    m_freeBlocks[i] = freeBlock;
                    m_freeBlocks.RemoveAtSwapBack(freeBlockId);
                    return i == m_freeBlocks.Length ? freeBlockId : i;
                }
            }

            return -1;
        }

        public void FreeAllocation(in Allocation allocation)
        {
            Debug.Assert(allocation.valid, "Cannot free invalid allocation");

            m_freeSlots.Add(allocation.handle);
            m_usedBlocks[allocation.handle] = Block.Invalid;

            int blockToMerge = m_freeBlocks.Length;
            m_freeBlocks.Add(allocation.block);

            while (blockToMerge != -1)
                blockToMerge = MergeBlockFrontBack(blockToMerge);

            m_FreeElementCount += allocation.block.count;
        }

        public Allocation[] SplitAllocation(in Allocation allocation, int count)
        {
            Debug.Assert(allocation.valid, "Invalid allocation");

            var newAllocs = new Allocation[count];
            var newAllocsSize = allocation.block.count / count;

            var newBlock0 = new Block { offset = allocation.block.offset, count = newAllocsSize };
            m_usedBlocks[allocation.handle] = newBlock0;
            newAllocs[0] = new Allocation() { handle = allocation.handle, block = newBlock0 };

            for (int i = 1; i < count; ++i)
            {
                Block block = new Block { offset = allocation.block.offset + i * newAllocsSize, count = newAllocsSize };

                int allocationHandle;
                if (m_freeSlots.IsEmpty)
                {
                    allocationHandle = m_usedBlocks.Length;
                    m_usedBlocks.Add(block);
                }
                else
                {
                    allocationHandle = m_freeSlots[m_freeSlots.Length - 1];
                    m_freeSlots.RemoveAtSwapBack(m_freeSlots.Length - 1);
                    m_usedBlocks[allocationHandle] = block;
                }

                newAllocs[i] = new Allocation() { handle = allocationHandle, block = block };
            }

            return newAllocs;
        }
    }
}
