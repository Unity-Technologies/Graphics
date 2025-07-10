using System;
using System.Collections;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal sealed class PersistentGpuArray<Tstruct> : IDisposable
        where Tstruct : struct
    {
        BlockAllocator m_SlotAllocator;
        ComputeBuffer m_GpuBuffer;
        NativeArray<Tstruct> m_CpuList;
        BitArray m_Updates;
        bool m_gpuBufferDirty = true;
        int m_ElementCount = 0;
        public int elementCount { get { return m_ElementCount; } }

        public PersistentGpuArray(int initialSize)
        {
            m_SlotAllocator.Initialize(initialSize);
            m_GpuBuffer = new ComputeBuffer(initialSize, Marshal.SizeOf<Tstruct>());
            m_CpuList = new NativeArray<Tstruct>(initialSize, Allocator.Persistent);
            m_Updates = new BitArray(initialSize);
            m_ElementCount = 0;
        }

        public void Dispose()
        {
            m_ElementCount = 0;
            m_SlotAllocator.Dispose();
            m_GpuBuffer.Dispose();
            m_CpuList.Dispose();
        }

        public BlockAllocator.Allocation Add(Tstruct element)
        {
            m_ElementCount++;
            var slotAllocation = m_SlotAllocator.Allocate(1);
            if (!slotAllocation.valid)
            {
                Grow();
                slotAllocation = m_SlotAllocator.Allocate(1);
                Assert.IsTrue(slotAllocation.valid);
            }
            m_CpuList[slotAllocation.block.offset] = element;
            m_Updates[slotAllocation.block.offset] = true;
            m_gpuBufferDirty = true;

            return slotAllocation;
        }

        public BlockAllocator.Allocation[] Add(int elementCount)
        {
            m_ElementCount+= elementCount;
            var slotAllocation = m_SlotAllocator.Allocate(elementCount);
            if (!slotAllocation.valid)
            {
                Grow();
                slotAllocation = m_SlotAllocator.Allocate(elementCount);
                Assert.IsTrue(slotAllocation.valid);
            }

            return m_SlotAllocator.SplitAllocation(slotAllocation, elementCount);
        }


        public void Remove(BlockAllocator.Allocation allocation)
        {
            m_ElementCount--;
            m_SlotAllocator.FreeAllocation(allocation);
        }

        public void Clear()
        {
            m_ElementCount = 0;
            var currentCapacity = m_SlotAllocator.capacity;
            m_SlotAllocator.Dispose();
            m_SlotAllocator = new BlockAllocator();
            m_SlotAllocator.Initialize(currentCapacity);
            m_Updates = new BitArray(currentCapacity);
            m_gpuBufferDirty = false;
        }

        public void Set(BlockAllocator.Allocation allocation, Tstruct element)
        {
            m_CpuList[allocation.block.offset] = element;
            m_Updates[allocation.block.offset] = true;
            m_gpuBufferDirty = true;
        }

        public Tstruct Get(BlockAllocator.Allocation allocation)
        {
            return m_CpuList[allocation.block.offset];
        }

        public void ModifyForEach(Func<Tstruct, Tstruct> lambda)
        {
            for (int i = 0; i < m_CpuList.Length; ++i)
            {
                m_CpuList[i] = lambda(m_CpuList[i]);
                m_Updates[i] = true;
            }
            m_gpuBufferDirty = true;
        }

        // Note: this should ideally be used with only one command buffer. If used with more than one cmd buffers, the order of their execution is important.
        public ComputeBuffer GetGpuBuffer(CommandBuffer cmd)
        {
            if (m_gpuBufferDirty)
            {
                int copyStartIndex = -1;
                for (int i = 0; i < m_Updates.Length; ++i)
                {
                    if (m_Updates[i])
                    {
                        if (copyStartIndex == -1)
                            copyStartIndex = i;

                        m_Updates[i] = false;
                    }
                    else if (copyStartIndex != -1)
                    {
                        int copyEndIndex = i;
                        cmd.SetBufferData(m_GpuBuffer, m_CpuList, copyStartIndex, copyStartIndex, copyEndIndex - copyStartIndex);
                        copyStartIndex = -1;
                    }
                }

                if (copyStartIndex != -1)
                {
                    int copyEndIndex = m_Updates.Length;
                    cmd.SetBufferData(m_GpuBuffer, m_CpuList, copyStartIndex, copyStartIndex, copyEndIndex - copyStartIndex);
                }

                m_gpuBufferDirty = false;
            }

            return m_GpuBuffer;
        }

        private void Grow()
        {
            var oldCapacity = m_SlotAllocator.capacity;
            m_SlotAllocator.Grow(m_SlotAllocator.capacity + 1);

            m_GpuBuffer.Dispose();
            m_GpuBuffer = new ComputeBuffer(m_SlotAllocator.capacity, Marshal.SizeOf<Tstruct>());

            var oldList = m_CpuList;
            m_CpuList = new NativeArray<Tstruct>(m_SlotAllocator.capacity, Allocator.Persistent);
            NativeArray<Tstruct>.Copy(oldList, m_CpuList, oldCapacity);
            oldList.Dispose();

            var oldUpdates = m_Updates;
            m_Updates = new BitArray(m_SlotAllocator.capacity);
            for (int i = 0; i < oldCapacity; ++i)
                m_Updates[i] = oldUpdates[i];
        }
    }
}

