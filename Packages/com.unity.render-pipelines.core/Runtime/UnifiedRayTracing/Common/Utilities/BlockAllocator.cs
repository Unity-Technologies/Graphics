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
        private NativeHashMap<int, int> m_freeBlockStarts;
        private NativeHashMap<int, int> m_freeBlockEnds;

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

            if (!m_freeBlockStarts.IsCreated)
                m_freeBlockStarts = new NativeHashMap<int, int>(64, Allocator.Persistent);
            else
                m_freeBlockStarts.Clear();

            if (!m_freeBlockEnds.IsCreated)
                m_freeBlockEnds = new NativeHashMap<int, int>(64, Allocator.Persistent);
            else
                m_freeBlockEnds.Clear();

            if (m_FreeElementCount > 0)
                AddFreeBlockEntry(new Block() { offset = 0, count = m_FreeElementCount });

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

            AddAndCoalesceFreeBlock(new Block() { offset = oldCapacity, count = addedElements });

            return m_MaxElementCount;
        }

        private int TailFreeBlockCount()
        {
            return m_freeBlockEnds.TryGetValue(m_MaxElementCount, out int index) ? m_freeBlocks[index].count : 0;
        }

        public bool GetExpectedGrowthToFitAllocation(int elementCounts, int maxAllowedCapacity, out int newCapacity)
        {
            newCapacity = 0;

            var additionalRequiredElements = math.max(elementCounts - TailFreeBlockCount(), 0);
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

            var additionalRequiredElements = math.max(elementCounts - TailFreeBlockCount(), 0);
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
            if (m_freeBlockStarts.IsCreated)
                m_freeBlockStarts.Dispose();
            if (m_freeBlockEnds.IsCreated)
                m_freeBlockEnds.Dispose();
        }

        public Allocation Allocate(int elementCounts)
        {
            if (elementCounts < 0 || elementCounts > m_FreeElementCount)
                return Allocation.Invalid;

            if (elementCounts == 0)
                return new Allocation() { handle = AllocateHandle(Block.Invalid), block = Block.Invalid };

            if (m_freeBlocks.IsEmpty)
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

                    if (block.count == elementCounts)
                        break;
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
                SetFreeBlock(selectedBlock, split);
            else
                RemoveFreeBlockAt(selectedBlock);

            m_FreeElementCount -= elementCounts;
            return new Allocation() { handle = AllocateHandle(allocationBlock), block = allocationBlock };
        }

        private int AllocateHandle(in Block block)
        {
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

            return allocationHandle;
        }

        public void FreeAllocation(in Allocation allocation)
        {
            Debug.Assert(allocation.valid, "Cannot free invalid allocation");

            m_freeSlots.Add(allocation.handle);
            m_usedBlocks[allocation.handle] = Block.Invalid;

            if (allocation.block.count > 0)
                AddAndCoalesceFreeBlock(allocation.block);

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
                newAllocs[i] = new Allocation() { handle = AllocateHandle(block), block = block };
            }

            return newAllocs;
        }

        private static int BlockEnd(in Block block)
        {
            return block.offset + block.count;
        }

        private void AddAndCoalesceFreeBlock(Block block)
        {
            if (m_freeBlockEnds.TryGetValue(block.offset, out int predecessorIndex))
            {
                Block predecessor = m_freeBlocks[predecessorIndex];
                RemoveFreeBlockAt(predecessorIndex);
                block.offset = predecessor.offset;
                block.count += predecessor.count;
            }

            if (m_freeBlockStarts.TryGetValue(BlockEnd(block), out int successorIndex))
            {
                Block successor = m_freeBlocks[successorIndex];
                RemoveFreeBlockAt(successorIndex);
                block.count += successor.count;
            }

            AddFreeBlockEntry(block);
        }

        private void AddFreeBlockEntry(in Block block)
        {
            m_freeBlockStarts.Add(block.offset, m_freeBlocks.Length);
            m_freeBlockEnds.Add(BlockEnd(block), m_freeBlocks.Length);
            m_freeBlocks.Add(block);
        }

        private void SetFreeBlock(int index, in Block newBlock)
        {
            Block oldBlock = m_freeBlocks[index];
            m_freeBlockStarts.Remove(oldBlock.offset);
            m_freeBlockEnds.Remove(BlockEnd(oldBlock));
            m_freeBlockStarts.Add(newBlock.offset, index);
            m_freeBlockEnds.Add(BlockEnd(newBlock), index);
            m_freeBlocks[index] = newBlock;
        }

        private void RemoveFreeBlockAt(int index)
        {
            Block removed = m_freeBlocks[index];
            m_freeBlockStarts.Remove(removed.offset);
            m_freeBlockEnds.Remove(BlockEnd(removed));

            int last = m_freeBlocks.Length - 1;
            if (index != last)
            {
                Block moved = m_freeBlocks[last];
                m_freeBlockStarts[moved.offset] = index;
                m_freeBlockEnds[BlockEnd(moved)] = index;
            }

            m_freeBlocks.RemoveAtSwapBack(index);
        }

#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
        internal void ValidateInvariants()
        {
            if (m_freeBlockStarts.Count != m_freeBlocks.Length || m_freeBlockEnds.Count != m_freeBlocks.Length)
                throw new InvalidOperationException($"Boundary map counts ({m_freeBlockStarts.Count}, {m_freeBlockEnds.Count}) do not match free block count ({m_freeBlocks.Length}).");

            int totalFreeCount = 0;
            for (int i = 0; i < m_freeBlocks.Length; ++i)
            {
                Block block = m_freeBlocks[i];

                if (block.count <= 0)
                    throw new InvalidOperationException($"Free block {i} has non-positive count {block.count}.");
                if (block.offset < 0 || BlockEnd(block) > m_MaxElementCount)
                    throw new InvalidOperationException($"Free block {i} [{block.offset}, {BlockEnd(block)}) is outside capacity {m_MaxElementCount}.");
                if (!m_freeBlockStarts.TryGetValue(block.offset, out int startIndex) || startIndex != i)
                    throw new InvalidOperationException($"Start map entry for free block {i} is missing or stale.");
                if (!m_freeBlockEnds.TryGetValue(BlockEnd(block), out int endIndex) || endIndex != i)
                    throw new InvalidOperationException($"End map entry for free block {i} is missing or stale.");
                if (m_freeBlockStarts.ContainsKey(BlockEnd(block)))
                    throw new InvalidOperationException($"Free block {i} is adjacent to an unmerged free block.");

                totalFreeCount += block.count;
            }

            if (totalFreeCount != m_FreeElementCount)
                throw new InvalidOperationException($"Sum of free block counts ({totalFreeCount}) does not match freeElementsCount ({m_FreeElementCount}).");
        }
#endif
    }
}
