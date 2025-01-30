using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    //@ Add instance version to detect dangling instance handles.
    internal struct InstanceHandle : IEquatable<InstanceHandle>, IComparable<InstanceHandle>
    {
        // Don't use this index to reference GPU data. This index is to reference CPU data only.
        // To reference GPU data convert InstanceHandle to GPUInstanceIndex.
        public int index { get; private set; }

        // This is unique instance index for each instance type.
        public int instanceIndex => index >> InstanceTypeInfo.kInstanceTypeBitCount;

        // We store type bits as lower bits because this makes max InstanceHandle index bounded by how many instances we have.
        // So you can allocate directly indexed arrays. This is fine as long as we have only 1 to 4 instance types.
        // If we put type bits in higher bits then we might want to make CPUInstanceData sparse set InstanceIndices table to be paged.
        public InstanceType type => (InstanceType)(index & InstanceTypeInfo.kInstanceTypeMask);

        public bool valid => index != -1;
        public static readonly InstanceHandle Invalid = new InstanceHandle() { index = -1 };
        public static InstanceHandle Create(int instanceIndex, InstanceType instanceType) { return new InstanceHandle() { index = instanceIndex << InstanceTypeInfo.kInstanceTypeBitCount | (int)instanceType }; }
        public static InstanceHandle FromInt(int value) { return new InstanceHandle() { index = value }; }
        public bool Equals(InstanceHandle other) => index == other.index;
        public int CompareTo(InstanceHandle other) { return index.CompareTo(other.index); }
        public override int GetHashCode() { return index; }
    }

    internal struct SharedInstanceHandle : IEquatable<SharedInstanceHandle>, IComparable<SharedInstanceHandle>
    {
        public int index { get; set; }
        public bool valid => index != -1;
        public static readonly SharedInstanceHandle Invalid = new SharedInstanceHandle() { index = -1 };
        public bool Equals(SharedInstanceHandle other) => index == other.index;
        public int CompareTo(SharedInstanceHandle other) { return index.CompareTo(other.index); }
        public override int GetHashCode() { return index; }
    }

    internal struct GPUInstanceIndex : IEquatable<GPUInstanceIndex>, IComparable<GPUInstanceIndex>
    {
        public int index { get; set; }
        public bool valid => index != -1;
        public static readonly GPUInstanceIndex Invalid = new GPUInstanceIndex() { index = -1 };
        public bool Equals(GPUInstanceIndex other) => index == other.index;
        public int CompareTo(GPUInstanceIndex other) { return index.CompareTo(other.index); }
        public override int GetHashCode() { return index; }
    }

    internal struct InstanceAllocator
    {
        private NativeArray<int> m_StructData;
        private NativeList<int> m_FreeInstances;
        private int m_BaseInstanceOffset;
        private int m_InstanceStride;

        public int length { get => m_StructData[0]; set => m_StructData[0] = value; }
        public bool valid => m_StructData.IsCreated;

        public void Initialize(int baseInstanceOffset = 0, int instanceStride = 1)
        {
            m_StructData = new NativeArray<int>(1, Allocator.Persistent);
            m_FreeInstances = new NativeList<int>(Allocator.Persistent);
            m_BaseInstanceOffset = baseInstanceOffset;
            m_InstanceStride = instanceStride;
        }

        public void Dispose()
        {
            m_StructData.Dispose();
            m_FreeInstances.Dispose();
        }

        public int AllocateInstance()
        {
            int instance;

            if (m_FreeInstances.Length > 0)
            {
                instance = m_FreeInstances[m_FreeInstances.Length - 1];
                m_FreeInstances.RemoveAtSwapBack(m_FreeInstances.Length - 1);
            }
            else
            {
                instance = length * m_InstanceStride + m_BaseInstanceOffset;
                length += 1;
            }

            return instance;
        }

        public void FreeInstance(int instance)
        {
            //@ This is a bit weak validation. Need something better but fast.
            Assert.IsTrue(instance >= 0 && instance < length * m_InstanceStride);
            m_FreeInstances.Add(instance);
        }

        public int GetNumAllocated()
        {
            return length - m_FreeInstances.Length;
        }
    }

    internal unsafe struct InstanceAllocators
    {
        private InstanceAllocator m_InstanceAlloc_MeshRenderer;
        private InstanceAllocator m_InstanceAlloc_SpeedTree;
        private InstanceAllocator m_SharedInstanceAlloc;

        public void Initialize()
        {
            //@ Will keep it as two separate allocators for two types for now. Nested native containers are not allowed in burst.
            m_InstanceAlloc_MeshRenderer = new InstanceAllocator();
            m_InstanceAlloc_SpeedTree = new InstanceAllocator();
            m_InstanceAlloc_MeshRenderer.Initialize((int)InstanceType.MeshRenderer, InstanceTypeInfo.kMaxInstanceTypesCount);
            m_InstanceAlloc_SpeedTree.Initialize((int)InstanceType.SpeedTree, InstanceTypeInfo.kMaxInstanceTypesCount);

            m_SharedInstanceAlloc = new InstanceAllocator();
            m_SharedInstanceAlloc.Initialize();
        }

        public unsafe void Dispose()
        {
            m_InstanceAlloc_MeshRenderer.Dispose();
            m_InstanceAlloc_SpeedTree.Dispose();
            m_SharedInstanceAlloc.Dispose();
        }

        private InstanceAllocator GetInstanceAllocator(InstanceType type)
        {
            switch (type)
            {
                case InstanceType.MeshRenderer:
                    return m_InstanceAlloc_MeshRenderer;
                case InstanceType.SpeedTree:
                    return m_InstanceAlloc_SpeedTree;
                default:
                    throw new ArgumentException("Allocator for this type is not created.");
            }
        }

        public int GetInstanceHandlesLength(InstanceType type)
        {
            return GetInstanceAllocator(type).length;
        }

        public int GetInstancesLength(InstanceType type)
        {
            return GetInstanceAllocator(type).GetNumAllocated();
        }

        public InstanceHandle AllocateInstance(InstanceType type)
        {
            return InstanceHandle.FromInt(GetInstanceAllocator(type).AllocateInstance());
        }

        public void FreeInstance(InstanceHandle instance)
        {
            Assert.IsTrue(instance.valid);
            GetInstanceAllocator(instance.type).FreeInstance(instance.index);
        }

        public unsafe SharedInstanceHandle AllocateSharedInstance()
        {
            return new SharedInstanceHandle { index = m_SharedInstanceAlloc.AllocateInstance() };
        }

        public void FreeSharedInstance(SharedInstanceHandle instance)
        {
            Assert.IsTrue(instance.valid);
            m_SharedInstanceAlloc.FreeInstance(instance.index);
        }
    }
}
