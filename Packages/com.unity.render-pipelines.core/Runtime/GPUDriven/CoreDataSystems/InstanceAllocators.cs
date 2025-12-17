using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    //@ Add instance version or generation to detect dangling instance handles.

    // This is used to uniquely reference an instance on the CPU side.
    internal struct InstanceHandle : IEquatable<InstanceHandle>, IComparable<InstanceHandle>
    {
        public int index { get; private set; }
        public bool isValid => index >= 0;
        public static InstanceHandle Create(int index) { return new InstanceHandle { index = index }; }
        public static readonly InstanceHandle Invalid = new InstanceHandle { index = -1 };
        public bool Equals(InstanceHandle other) => index == other.index;
        public int CompareTo(InstanceHandle other) { return index.CompareTo(other.index); }
        public override int GetHashCode() { return index; }
    }

    // This is used to uniquely define an instance within its archetype on the GPU side. (Instance data is sorted by archetype on the GPU).
    internal struct InstanceGPUHandle : IEquatable<InstanceGPUHandle>, IComparable<InstanceGPUHandle>
    {
        private int m_Data;
        public bool isValid => m_Data >= 0;
        public GPUArchetypeHandle archetype => GPUArchetypeHandle.Create((short)(m_Data & GPUArchetypeManager.kGPUArchetypeBitsMask));
        public int archetypeInstanceIndex => m_Data >> GPUArchetypeManager.kGPUArchetypeBits;
        public static InstanceGPUHandle Create(GPUArchetypeHandle gpuArchetype, int gpuPerArchetypeIndex)
        {
            Assert.IsTrue(gpuArchetype.index < GPUArchetypeManager.kMaxGPUArchetypesCount);

            return new InstanceGPUHandle
            {
                m_Data = ((gpuPerArchetypeIndex << GPUArchetypeManager.kGPUArchetypeBits) | (int)gpuArchetype.index)
            };
        }
        public static readonly InstanceGPUHandle Invalid = new InstanceGPUHandle { m_Data = -1 };
        public bool Equals(InstanceGPUHandle other) => m_Data == other.m_Data;
        public int CompareTo(InstanceGPUHandle other) { return m_Data.CompareTo(other.m_Data); }
        public override int GetHashCode() { return m_Data; }
    }

    internal unsafe struct InstanceAllocators
    {
        private NativeHandleAllocator m_InstanceCPUHandleAllocator;
        private NativeArray<NativeHandleAllocator> m_InstanceGPUHandleAllocators;

        public void Initialize()
        {
            m_InstanceCPUHandleAllocator = new NativeHandleAllocator();
            m_InstanceCPUHandleAllocator.Initialize();

            m_InstanceGPUHandleAllocators = new NativeArray<NativeHandleAllocator>(GPUArchetypeManager.kMaxGPUArchetypesCount, Allocator.Persistent);
            for(int i = 0; i < m_InstanceGPUHandleAllocators.Length; ++i)
                UnsafeUtility.ArrayElementAsRef<NativeHandleAllocator>(m_InstanceGPUHandleAllocators.GetUnsafePtr(), i).Initialize();
        }

        public unsafe void Dispose()
        {
            m_InstanceCPUHandleAllocator.Dispose();
            for (int i = 0; i < m_InstanceGPUHandleAllocators.Length; ++i)
                UnsafeUtility.ArrayElementAsRef<NativeHandleAllocator>(m_InstanceGPUHandleAllocators.GetUnsafePtr(), i).Dispose();
            m_InstanceGPUHandleAllocators.Dispose();
        }

        private ref NativeHandleAllocator GetInstanceGPUHandleAllocator(GPUArchetypeHandle archetype)
        {
            Assert.IsTrue(archetype.valid);
            return ref UnsafeUtility.ArrayElementAsRef<NativeHandleAllocator>(m_InstanceGPUHandleAllocators.GetUnsafePtr(), archetype.index);
        }

        public int TrimGPUAllocatorLength(GPUArchetypeHandle archetype)
        {
            ref var allocator = ref GetInstanceGPUHandleAllocator(archetype);
            allocator.TrimLength();
            return allocator.length;
        }

        public int GetInstanceGPUHandlesAllocatedCount(GPUArchetypeHandle archetype)
        {
            return GetInstanceGPUHandleAllocator(archetype).allocatedCount;
        }

        public InstanceHandle AllocateInstance()
        {
            return InstanceHandle.Create(m_InstanceCPUHandleAllocator.Allocate());
        }

        public InstanceGPUHandle AllocateInstanceGPUHandle(GPUArchetypeHandle archetype)
        {
            return InstanceGPUHandle.Create(archetype, GetInstanceGPUHandleAllocator(archetype).Allocate());
        }

        public void FreeInstance(InstanceHandle instance)
        {
            Assert.IsTrue(instance.isValid);
            m_InstanceCPUHandleAllocator.Free(instance.index);
        }

        public void FreeInstanceGPUHandle(InstanceGPUHandle gpuHandle)
        {
            Assert.IsTrue(gpuHandle.isValid);
            GetInstanceGPUHandleAllocator(gpuHandle.archetype).Free(gpuHandle.archetypeInstanceIndex);
        }
    }
}
