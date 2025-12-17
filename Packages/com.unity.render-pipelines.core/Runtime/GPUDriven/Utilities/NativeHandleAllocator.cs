using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal struct NativeHandleAllocator
    {
        private const int InvalidChunkIndex = -1;
        private const int BitChunkSize = 32;

        // Store length/freeCount inside an UnsafeList<int> of size 2 so that they can be modified from Burst jobs while the struct is passed by value.
        private UnsafeList<int> m_StructData;

        private UnsafeList<FreeBitsChunk> m_FreeBitChunksDense;
        private UnsafeList<int> m_FreeChunkIndicesSparse;

        public int length { get => m_StructData[0]; private set => m_StructData[0] = value; }

        public int freeCount { get => m_StructData[1]; private set => m_StructData[1] = value; }

        public int allocatedCount => isValid ? length - freeCount : 0;

        public bool isValid => m_StructData.IsCreated;

        public void Initialize(int initialCapacity = 128)
        {
            Assert.IsTrue(math.ispow2(BitChunkSize));

            int bitChunkCount = CoreUtils.DivRoundUp(initialCapacity, BitChunkSize);

            m_StructData = new UnsafeList<int>(2, Allocator.Persistent) { 0, 0 };
            m_FreeBitChunksDense = new UnsafeList<FreeBitsChunk>(bitChunkCount, Allocator.Persistent);
            m_FreeChunkIndicesSparse = new UnsafeList<int>(bitChunkCount, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (isValid)
            {
                m_StructData.Dispose();
                m_FreeBitChunksDense.Dispose();
                m_FreeChunkIndicesSparse.Dispose();
            }
        }

        public int Allocate()
        {
            Assert.IsTrue(isValid, "Allocator not initialized.");

            if (freeCount == 0)
            {
                Assert.IsTrue(m_FreeBitChunksDense.Length == 0, "Found non empty handles chunks while freeHandles is zero.");
                return length++;
            }

            Assert.IsTrue(m_FreeBitChunksDense.Length > 0, "No free chunks available.");

            int lastChunkIndex = m_FreeBitChunksDense.Length - 1;
            FreeBitsChunk freeBitsChunk = m_FreeBitChunksDense[lastChunkIndex];
            Assert.IsTrue(freeBitsChunk.freeBits != 0, "No free bits in the chunk.");

            int freeBitIndex = math.tzcnt(freeBitsChunk.freeBits);
            freeBitsChunk.freeBits &= ~(1u << freeBitIndex);
            m_FreeBitChunksDense[lastChunkIndex] = freeBitsChunk;

            int handle = freeBitsChunk.chunk * BitChunkSize + freeBitIndex;
            Assert.IsTrue(handle < length, "Handle exceeds length.");

            if (freeBitsChunk.freeBits == 0)
            {
                m_FreeChunkIndicesSparse[freeBitsChunk.chunk] = InvalidChunkIndex;
                m_FreeBitChunksDense.Resize(m_FreeBitChunksDense.Length - 1);
            }

            freeCount -= 1;

            return handle;
        }

        public void Free(int handle)
        {
            Assert.IsTrue(math.ispow2(BitChunkSize));
            Assert.IsTrue(isValid, "Allocator not initialized.");
            Assert.IsTrue(handle >= 0 && handle < length, "Handle is out of allocated range.");

            int chunk = handle / BitChunkSize;
            int bitIndex = handle & (BitChunkSize - 1);
            uint bitMask = 1u << bitIndex;

            int chunkIndex;

            if (chunk >= m_FreeChunkIndicesSparse.Length)
            {
                m_FreeChunkIndicesSparse.AddReplicate(InvalidChunkIndex, chunk - m_FreeChunkIndicesSparse.Length + 1);
                chunkIndex = InvalidChunkIndex;
            }
            else
            {
                chunkIndex = m_FreeChunkIndicesSparse[chunk];
            }

            if (chunkIndex != InvalidChunkIndex)
            {
                FreeBitsChunk freeBitsChunk = m_FreeBitChunksDense[chunkIndex];
                Assert.IsTrue((freeBitsChunk.freeBits & bitMask) == 0, "Handle is freed already.");
                Assert.IsTrue(freeBitsChunk.chunk == chunk, "Chunk index mismatch.");
                freeBitsChunk.freeBits |= bitMask;
                m_FreeBitChunksDense[chunkIndex] = freeBitsChunk;
            }
            else
            {
                m_FreeChunkIndicesSparse[chunk] = m_FreeBitChunksDense.Length;
                m_FreeBitChunksDense.Add(new FreeBitsChunk(chunk, bitMask));
            }

            freeCount += 1;
        }

        internal void TrimLengthImpl()
        {
            if (!isValid || length == 0)
                return;

            int lastChunk = (length - 1) / BitChunkSize;

            if (lastChunk != m_FreeChunkIndicesSparse.Length - 1)
                return;

            int usedBitsInChunk = Math.Max(length - BitChunkSize * lastChunk, 0);
            int nonUsedBitsInChunk = BitChunkSize - usedBitsInChunk;
            uint usedBitsMask = 0xFFFFFFFF >> nonUsedBitsInChunk;

            while (lastChunk >= 0)
            {
               int lastChunkIndex = m_FreeChunkIndicesSparse[lastChunk]; 

                if (lastChunkIndex == InvalidChunkIndex)
                    return;

                FreeBitsChunk lastBitsChunk = m_FreeBitChunksDense[lastChunkIndex];
                Assert.IsTrue(lastBitsChunk.chunk == lastChunk, "Chunk index mismatch.");

                int freeLeadBits = math.lzcnt(~lastBitsChunk.freeBits & usedBitsMask) - nonUsedBitsInChunk; 
                lastBitsChunk.freeBits &= (uint)((ulong)usedBitsMask >> freeLeadBits);

                if (lastBitsChunk.freeBits == 0)
                {
                    int lastDenseChunk = m_FreeBitChunksDense[m_FreeBitChunksDense.Length - 1].chunk;
                    m_FreeBitChunksDense.RemoveAtSwapBack(lastChunkIndex);
                    m_FreeChunkIndicesSparse[lastDenseChunk] = lastChunkIndex;
                    m_FreeChunkIndicesSparse.Resize(m_FreeChunkIndicesSparse.Length - 1);
                }
                else
                {
                    m_FreeBitChunksDense[lastChunkIndex] = lastBitsChunk;
                }

                length -= freeLeadBits;
                freeCount -= freeLeadBits;
                Assert.IsTrue(length >= 0 && freeCount >= 0, "Length or freeCount went negative.");

                if (freeLeadBits < usedBitsInChunk)
                    return;

                if (usedBitsInChunk < BitChunkSize)
                {
                    usedBitsInChunk = BitChunkSize;
                    nonUsedBitsInChunk = 0;
                    usedBitsMask = 0xFFFFFFFF;
                }

                lastChunk -= 1;
            }
        }

        public unsafe void TrimLength()
        {
            fixed (NativeHandleAllocator* thisPtr = &this)
            {
                NativeHandleAllocatorBurst.TrimLength(thisPtr);
            }
        }

        struct FreeBitsChunk
        {
            public readonly int chunk;
            public uint freeBits;

            public FreeBitsChunk(int chunk, uint freeBits)
            {
                this.chunk = chunk;
                this.freeBits = freeBits;
            }
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal static unsafe class NativeHandleAllocatorBurst
    {
        public static void TrimLength(NativeHandleAllocator* allocator) => allocator->TrimLengthImpl();
    }
}
