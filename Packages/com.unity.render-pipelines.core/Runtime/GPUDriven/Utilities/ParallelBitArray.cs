using System;
using System.Diagnostics;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace UnityEngine.Rendering
{
    internal struct ParallelBitArray
    {
        private Allocator m_Allocator;
        private NativeArray<long> m_Bits;
        private int m_Length;

        public int Length => m_Length;

        public bool IsCreated
        {
            get { return m_Bits.IsCreated; }
        }

        public ParallelBitArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            m_Allocator = allocator;
            m_Bits = new NativeArray<long>((length + 63) / 64, allocator, options);
            m_Length = length;
        }

        public void Dispose()
        {
            m_Bits.Dispose();
            m_Length = 0;
        }

        public void Dispose(JobHandle inputDeps)
        {
            m_Bits.Dispose(inputDeps);
            m_Length = 0;
        }

        public void Resize(int newLength)
        {
            int oldLength = m_Length;
            if (newLength == oldLength)
                return;

            int oldBitsLength = m_Bits.Length;
            int newBitsLength = (newLength + 63) / 64;
            if (newBitsLength != oldBitsLength)
            {
                var newBits = new NativeArray<long>(newBitsLength, m_Allocator, NativeArrayOptions.UninitializedMemory);
                if (m_Bits.IsCreated)
                {
                    NativeArray<long>.Copy(m_Bits, newBits, m_Bits.Length);
                    m_Bits.Dispose();
                }
                m_Bits = newBits;
            }

            // mask off bits past the length
            int validLength = Math.Min(oldLength, newLength);
            int validBitsLength = Math.Min(oldBitsLength, newBitsLength);
            for (int chunkIndex = validBitsLength; chunkIndex < m_Bits.Length; ++chunkIndex)
            {
                int validBitCount = Math.Max(validLength - 64 * chunkIndex, 0);
                if (validBitCount < 64)
                {
                    ulong validMask = (1ul << validBitCount) - 1;
                    m_Bits[chunkIndex] &= (long)validMask;
                }
            }
            m_Length = newLength;
        }

        public void Set(int index, bool value)
        {
            unsafe
            {
                Debug.Assert(0 <= index && index < m_Length);

                int entry_index = index >> 6;
                long* entries = (long*)m_Bits.GetUnsafePtr();

                ulong bit = 1ul << (index & 0x3f);
                long and_mask = (long)(~bit);
                long or_mask = value ? (long)bit : 0;

                long old_entry, new_entry;
                do
                {
                    old_entry = Interlocked.Read(ref entries[entry_index]);
                    new_entry = (old_entry & and_mask) | or_mask;
                } while (Interlocked.CompareExchange(ref entries[entry_index], new_entry, old_entry) != old_entry);
            }
        }

        public bool Get(int index)
        {
            unsafe
            {
                Debug.Assert(0 <= index && index < m_Length);

                int entry_index = index >> 6;
                long* entries = (long*)m_Bits.GetUnsafeReadOnlyPtr();

                ulong bit = 1ul << (index & 0x3f);
                long check_mask = (long)bit;
                return (entries[entry_index] & check_mask) != 0;
            }
        }

        public ulong GetChunk(int chunk_index)
        {
            return (ulong)m_Bits[chunk_index];
        }

        public void SetChunk(int chunk_index, ulong chunk_bits)
        {
            m_Bits[chunk_index] = (long)chunk_bits;
        }

        public unsafe ulong InterlockedReadChunk(int chunk_index)
        {
            long* entries = (long*)m_Bits.GetUnsafeReadOnlyPtr();
            return (ulong)Interlocked.Read(ref entries[chunk_index]);
        }

        public unsafe void InterlockedOrChunk(int chunk_index, ulong chunk_bits)
        {
            long* entries = (long*)m_Bits.GetUnsafePtr();

            long old_entry, new_entry;
            do
            {
                old_entry = Interlocked.Read(ref entries[chunk_index]);
                new_entry = old_entry | (long)chunk_bits;
            } while (Interlocked.CompareExchange(ref entries[chunk_index], new_entry, old_entry) != old_entry);
        }

        public int ChunkCount()
        {
            return m_Bits.Length;
        }

        public ParallelBitArray GetSubArray(int length)
        {
            ParallelBitArray array = new ParallelBitArray();
            array.m_Bits = m_Bits.GetSubArray(0, (length + 63) / 64);
            array.m_Length = length;
            return array;
        }

        public NativeArray<long> GetBitsArray()
        {
            return m_Bits;
        }

        public void FillZeroes(int length)
        {
            length = Math.Min(length, m_Length);
            int chunkIndex = length / 64;
            int remainder = length & 63;

            m_Bits.FillArray(0, 0, chunkIndex);

            if(remainder > 0)
            {
                long lastChunkMask = (1L << remainder) - 1;
                m_Bits[chunkIndex] &= ~lastChunkMask;
            }
        }
    }
}
