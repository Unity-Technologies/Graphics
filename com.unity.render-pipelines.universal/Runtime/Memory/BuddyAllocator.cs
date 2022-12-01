using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.Universal
{
    struct BuddyAllocation
    {
        public int level;
        public int index;

        public BuddyAllocation(int level, int index)
        {
            this.level = level;
            this.index = index;
        }

        public uint2 index2D => SpaceFillingCurves.DecodeMorton2D((uint)index);
    }

    unsafe struct BuddyAllocator : IDisposable
    {
        struct Header
        {
            public int branchingOrder;
            public int levelCount;
            public int allocationCount;
            public int freeAllocationIdsCount;
        }

        // This data structure uses one big allocation containing a fixed header and all the arrays.
        // Some arrays are sub-divided per order, which can be identified by the presence of an X(int order) method,
        // which allows for easy access to the slice of data for the specified order.
        // The offsets for the arrays are stored together with the pointer to avoid a double look-up when
        // accessing data.
        void* m_Data;

        ref Header header => ref UnsafeUtility.AsRef<Header>(m_Data);

        (int, int) m_ActiveFreeMaskCounts;
        NativeArray<int> freeMaskCounts => GetNativeArray<int>(m_ActiveFreeMaskCounts.Item1, m_ActiveFreeMaskCounts.Item2);

        (int, int) m_FreeMasksStorage;
        NativeArray<ulong> freeMasksStorage => GetNativeArray<ulong>(m_FreeMasksStorage.Item1, m_FreeMasksStorage.Item2);
        NativeArray<ulong> FreeMasks(int level) => freeMasksStorage.GetSubArray(LevelOffset64(level, header.branchingOrder), LevelLength64(level, header.branchingOrder));

        (int, int) m_FreeMaskIndicesStorage;
        NativeArray<int> freeMaskIndicesStorage => GetNativeArray<int>(m_FreeMaskIndicesStorage.Item1, m_FreeMaskIndicesStorage.Item2);
        NativeArray<int> FreeMaskIndices(int level) => freeMaskIndicesStorage.GetSubArray(LevelOffset64(level, header.branchingOrder), LevelLength64(level, header.branchingOrder));

        Allocator m_Allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_SafetyHandle;
#endif

        public int levelCount => header.levelCount;

        public BuddyAllocator(int levelCount, int branchingOrder, Allocator allocator = Allocator.Persistent)
        {
            // Allows us to support 1D, 2D, and 3D cases.
            Assert.IsTrue(branchingOrder is >= 1 and <= 3);
            // Memory usage explodes unless capped like this.
            Assert.IsTrue((levelCount + branchingOrder - 1) / branchingOrder is >= 1 and <= 24);

            var dataSize = sizeof(Header);
            m_ActiveFreeMaskCounts = AllocateRange<int>(levelCount, ref dataSize);
            m_FreeMasksStorage = AllocateRange<ulong>(LevelOffset64(levelCount, branchingOrder), ref dataSize);
            m_FreeMaskIndicesStorage = AllocateRange<int>(LevelOffset64(levelCount, branchingOrder), ref dataSize);

            m_Data = UnsafeUtility.Malloc(dataSize, 64, allocator);
            UnsafeUtility.MemClear(m_Data, dataSize);
            m_Allocator = allocator;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_SafetyHandle = AtomicSafetyHandle.Create();
#endif

            header = new Header
            {
                branchingOrder = branchingOrder,
                levelCount = levelCount
            };

            // Initialize level-0 to have 1/1 block available.
            var freeMasks0 = FreeMasks(0);
            freeMasks0[0] = 0b1111;
            var maskCounts = freeMaskCounts;
            maskCounts[0] = 1;
        }

        public bool TryAllocate(int requestedLevel, out BuddyAllocation allocation)
        {
            allocation = default;

            // Find the highest level that has a block available.
            var level = requestedLevel;
            var maskCounts = freeMaskCounts;
            while (level >= 0)
            {
                if (maskCounts[level] > 0) break;
                level--;
            }

            // No blocks available.
            if (level < 0) return false;

            // Split a block at the level we found.
            int dataIndex;
            {
                var freeMaskIndices = FreeMaskIndices(level);
                var maskIndex = freeMaskIndices[--maskCounts[level]];
                var freeMasks = FreeMasks(level);
                var freeMask = freeMasks[maskIndex];
                Assert.AreNotEqual(freeMask, 0);
                var bitIndex = math.tzcnt(freeMask);
                freeMask ^= 1ul << bitIndex;
                freeMasks[maskIndex] = freeMask;
                if (freeMask != 0) freeMaskIndices[maskCounts[level]++] = maskIndex;
                dataIndex = maskIndex * 64 + bitIndex;
            }

            // Walk up the levels until we hit the requested level. For each level we want to mark the remaining parts
            // of the newly split blocks as free.
            while (level < requestedLevel)
            {
                level++;
                dataIndex <<= header.branchingOrder;
                var maskIndex = dataIndex >> 6;
                var bitIndex = dataIndex & 63;
                var freeMasks = FreeMasks(level);
                var freeMask = freeMasks[maskIndex];
                // We might have hit a mask that already contained free blocks. If not, add the mask index to the free list.
                if (freeMask == 0)
                {
                    var freeMaskIndices = FreeMaskIndices(level);
                    freeMaskIndices[maskCounts[level]++] = maskIndex;
                }

                // Mark other bits in the block we just broke apart as free.
                // In binary form, 2^b will give us a 1 followed by b 0s. So to get b ones, we subtract 1. Since we want
                // the least significant bit to be 0, we subtract another 1.
                // E.g. for branching order 1 we get 10b, for 2 we get 1110b, for 3 we get 11111110b.
                // Finally we shift according to the data index.
                Assert.IsTrue(bitIndex + Pow2(header.branchingOrder) - 1 < 64);
                freeMask |= ((1ul << Pow2(header.branchingOrder)) - 2ul) << bitIndex;
                freeMasks[maskIndex] = freeMask;
            }

            allocation.level = level;
            allocation.index = dataIndex;
            return true;
        }

        public void Free(BuddyAllocation allocation)
        {
            var level = allocation.level;
            var dataIndex = allocation.index;
            while (level >= 0)
            {
                var maskIndex = dataIndex >> 6;
                var bitIndex = dataIndex & 63;
                var freeMasks = FreeMasks(level);
                var freeMask = freeMasks[maskIndex];
                var wasZero = freeMask == 0;
                freeMask |= 1ul << bitIndex;

                var indices = FreeMaskIndices(level);
                var counts = freeMaskCounts;

                var superBlockMask = ((1ul << Pow2(header.branchingOrder)) - 1) << ((bitIndex >> header.branchingOrder) * Pow2(header.branchingOrder));
                // Check if the whole super-block (i.e. making up one block of the next level) is free.
                // If it is, we let the loop continue upwards.
                if (level == 0 || (~freeMask & superBlockMask) != 0)
                {
                    freeMasks[maskIndex] = freeMask;
                    if (wasZero)
                    {
                        indices[counts[level]++] = maskIndex;
                    }
                    break;
                }

                freeMask &= ~superBlockMask;
                freeMasks[maskIndex] = freeMask;

                if (!wasZero && freeMask == 0)
                {
                    for (var i = 0; i < indices.Length; i++)
                    {
                        if (indices[i] == maskIndex)
                        {
                            indices[i] = indices[--counts[level]];
                            break;
                        }
                    }
                }

                level--;
                dataIndex >>= header.branchingOrder;
            }
        }

        public void Dispose()
        {
            UnsafeUtility.Free(m_Data, m_Allocator);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_SafetyHandle);
#endif
            m_Data = default;
            m_Allocator = default;
        }

        NativeArray<T> GetNativeArray<T>(int offset, int length) where T : struct
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(PtrAdd(m_Data, offset), length, m_Allocator);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_SafetyHandle);
#endif
            return array;
        }

        // sum x^i for i=0..(n-1) = (x ^ n - 1) / (x - 1) where n is order and n is 2^branchingOrder
        static int LevelOffset(int level, int branchingOrder) => (Pow2(branchingOrder) * (Pow2(branchingOrder * (level - 1) + branchingOrder) - 1)) / (Pow2(branchingOrder) - 1);
        static int LevelLength(int level, int branchingOrder) => Pow2N(branchingOrder, level + 1);

        // These are for when orders of length <= 64 only take up 1 item, e.g. ulong bitmasks.
        static int LevelOffset64(int level, int branchingOrder) => math.min(level, 6/branchingOrder) + LevelOffset(math.max(0, level - 6/branchingOrder), branchingOrder);
        static int LevelLength64(int level, int branchingOrder) => Pow2N(branchingOrder, math.max(0, level - 6/branchingOrder + 1));

        static (int, int) AllocateRange<T>(int length, ref int dataSize) where T : struct
        {
            dataSize = AlignForward(dataSize, UnsafeUtility.AlignOf<T>());
            var range = (dataSize, length);
            dataSize += length * UnsafeUtility.SizeOf<T>();
            return range;
        }

        static int AlignForward(int offset, int alignment)
        {
            var modulo = offset % alignment;
            if (modulo != 0) offset += (alignment - modulo);
            return offset;
        }

        static void* PtrAdd(void* ptr, int bytes) => (void*) ((IntPtr) ptr + bytes);

        static int Pow2(int n) => 1 << n;

        // (2^x)^n = 2^(x*n)
        static int Pow2N(int x, int n) => 1 << (x * n);
    }
}
