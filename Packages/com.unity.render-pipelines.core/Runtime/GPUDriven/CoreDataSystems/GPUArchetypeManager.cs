using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal struct GPUArchetypeManager : IDisposable
    {
        // We can make this larger if needed. If we need 128 the we just need to have 2 ulong componentMask in GPUComponentSet.
        public const int kMaxComponentsCount = 64;

        public const int kGPUArchetypeBits = 6;
        public const int kMaxGPUArchetypesCount = 1 << kGPUArchetypeBits;
        public const int kGPUArchetypeBitsMask = kMaxGPUArchetypesCount - 1;

        private NativeHandleAllocator m_ComponentHandleAllocator;
        private NativeHandleAllocator m_ArchetypeHandleAllocator;

        private NativeList<GPUComponentDesc> m_Components;
        private NativeList<GPUArchetypeDesc> m_Archetypes;

        private NativeParallelHashMap<int, GPUComponentHandle> m_ComponentHash;
        private NativeParallelHashMap<GPUComponentSet, GPUArchetypeHandle> m_ArchetypeHash;
        private NativeParallelHashMap<GPUArchetypeHandle, GPUComponentSet> m_ComponentSetHash;

        private GraphicsDeviceType m_GraphicsDeviceType;

        public void Initialize()
        {
            m_ComponentHandleAllocator = new NativeHandleAllocator();
            m_ComponentHandleAllocator.Initialize();
            m_ArchetypeHandleAllocator = new NativeHandleAllocator();
            m_ArchetypeHandleAllocator.Initialize();

            m_Components = new NativeList<GPUComponentDesc>(Allocator.Persistent);
            m_Archetypes = new NativeList<GPUArchetypeDesc>(Allocator.Persistent);

            m_ComponentHash = new NativeParallelHashMap<int, GPUComponentHandle>(16, Allocator.Persistent);
            m_ArchetypeHash = new NativeParallelHashMap<GPUComponentSet, GPUArchetypeHandle>(16, Allocator.Persistent);
            m_ComponentSetHash = new NativeParallelHashMap<GPUArchetypeHandle, GPUComponentSet>(16, Allocator.Persistent);

            m_GraphicsDeviceType = SystemInfo.graphicsDeviceType;
        }

        public void Dispose()
        {
            m_ComponentHandleAllocator.Dispose();
            m_ArchetypeHandleAllocator.Dispose();
            foreach (var component in m_Components)
                component.archetypes.Dispose();
            foreach (var archetype in m_Archetypes)
                archetype.components.Dispose();
            m_Components.Dispose();
            m_Archetypes.Dispose();
            m_ComponentHash.Dispose();
            m_ArchetypeHash.Dispose();
            m_ComponentSetHash.Dispose();
        }

        public GPUComponentHandle FindComponent(int propertyID)
        {
            if (m_ComponentHash.TryGetValue(propertyID, out var componentHandle))
                return componentHandle;

            return GPUComponentHandle.Invalid;
        }

        public GPUArchetypeHandle FindArchetype(GPUComponentSet componentSet)
        {
            if (m_ArchetypeHash.TryGetValue(componentSet, out var archetype))
                return archetype;

            return GPUArchetypeHandle.Invalid;
        }

        public GPUComponentSet FindComponentSet(GPUArchetypeHandle archetype)
        {
            Assert.IsTrue(archetype.valid);
            Assert.IsTrue(m_ComponentSetHash.ContainsKey(archetype));
            return m_ComponentSetHash[archetype];
        }

        public ref readonly GPUComponentDesc GetComponentDesc(GPUComponentHandle componentHandle)
        {
            Assert.IsTrue(componentHandle.valid);
            return ref m_Components.ElementAt(componentHandle.index);
        }

        public ref readonly GPUArchetypeDesc GetArchetypeDesc(GPUArchetypeHandle archetypeHandle)
        {
            Assert.IsTrue(archetypeHandle.valid);
            return ref m_Archetypes.ElementAt(archetypeHandle.index);
        }

        public int GetComponentsCount() => m_Components.Length;

        public int GetArchetypesCount() => m_Archetypes.Length;

        public GPUComponentHandle CreateComponent(int propertyID, int byteSize, bool isPerInstance)
        {
            //@ Should we extend this assert to all devices to address slowdowns caused by unaligned access on GPU?
            if (m_GraphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                Assert.IsTrue(byteSize % UnsafeUtility.SizeOf<Vector4>() == 0, "On OpenGLES3 component size must be aligned by float4.");
            Assert.IsTrue(m_Components.Length < kMaxComponentsCount, "Maximum GPU components count reached.");
            Assert.IsTrue(byteSize > 0, "Component size is zero.");
            Assert.IsTrue(!m_ComponentHash.ContainsKey(propertyID), $"Component with propertyID {propertyID} already exists.");
            
            var componentDesc = new GPUComponentDesc
            {
                propertyID = propertyID,
                byteSize = byteSize,
                isPerInstance = isPerInstance,
                archetypes = new UnsafeList<GPUArchetypeHandle>(4, Allocator.Persistent)
            };

            var componentHandle = GPUComponentHandle.Create((short)m_ComponentHandleAllocator.Allocate());

            if (m_Components.Length <= componentHandle.index)
                m_Components.Resize(componentHandle.index + 1, NativeArrayOptions.ClearMemory);

            m_Components[componentHandle.index] = componentDesc;
            m_ComponentHash.Add(propertyID, componentHandle);

            return componentHandle;
        }

        public GPUComponentHandle CreateComponent<T>(int propertyID, bool isPerInstance) where T : unmanaged
        {
            return CreateComponent(propertyID, UnsafeUtility.SizeOf<T>(), isPerInstance);
        }

        public unsafe GPUArchetypeHandle CreateArchetype(GPUComponentSet componentSet)
        {
            Assert.IsTrue(m_Archetypes.Length < kMaxGPUArchetypesCount, "Maximum GPU archetypes count reached.");
            Assert.IsFalse(m_ArchetypeHash.ContainsKey(componentSet), "Archetype with the same component set already exists.");

            var archetypeHandle = GPUArchetypeHandle.Create((short)m_ArchetypeHandleAllocator.Allocate());
            var components = componentSet.GetComponents(Allocator.Persistent);
            var archetypeDesc = new GPUArchetypeDesc { components = components };

            for (int i = 0; i < components.Length; ++i)
            {
                Assert.IsTrue(components[i].valid);
                ref GPUComponentDesc componentDesc = ref m_Components.ElementAt(components[i].index);
                Assert.IsFalse(componentDesc.archetypes.Contains(archetypeHandle), "Archetype already contains component.");
                componentDesc.archetypes.Add(archetypeHandle);
            }

            if (m_Archetypes.Length <= archetypeHandle.index)
                m_Archetypes.Resize(archetypeHandle.index + 1, NativeArrayOptions.ClearMemory);

            m_Archetypes[archetypeHandle.index] = archetypeDesc;
            m_ArchetypeHash.Add(componentSet, archetypeHandle);
            m_ComponentSetHash.Add(archetypeHandle, componentSet);

            return archetypeHandle;
        }

        public GPUArchetypeHandle GetOrCreateArchetype(GPUComponentSet componentSet)
        {
            if (m_ArchetypeHash.TryGetValue(componentSet, out var archetype))
                return archetype;

            return CreateArchetype(componentSet);
        }

        public GPUComponentHandle GetOrCreateComponent(int propertyID, int byteSize, bool perInstance)
        {
            var componentHandle = FindComponent(propertyID);
            if (!componentHandle.Equals(GPUComponentHandle.Invalid))
                return componentHandle;

            return CreateComponent(propertyID, byteSize, perInstance);
        }
    }

    internal struct GPUComponentDesc
    {
        public int propertyID;
        public int byteSize;
        public bool isPerInstance;
        public UnsafeList<GPUArchetypeHandle> archetypes;
    }

    internal struct GPUArchetypeDesc
    {
        public NativeList<GPUComponentHandle> components;
    }

    internal struct GPUComponentHandle : IEquatable<GPUComponentHandle>, IComparable<GPUComponentHandle>
    {
        public short index { get; private set; }
        public bool valid => index >= 0;
        public static GPUComponentHandle Create(short index) { return new GPUComponentHandle { index = index }; }
        public static readonly GPUComponentHandle Invalid = new GPUComponentHandle { index = -1 };
        public bool Equals(GPUComponentHandle other) => index == other.index;
        public int CompareTo(GPUComponentHandle other) { return index.CompareTo(other.index); }
        public override int GetHashCode() { return index; }
    }

    internal struct GPUArchetypeHandle : IEquatable<GPUArchetypeHandle>, IComparable<GPUArchetypeHandle>
    {
        public short index { get; private set; }
        public bool valid => index >= 0;
        public static GPUArchetypeHandle Create(short index) { return new GPUArchetypeHandle { index = index }; }
        public static readonly GPUArchetypeHandle Invalid = new GPUArchetypeHandle { index = -1 };
        public bool Equals(GPUArchetypeHandle other) => index == other.index;
        public int CompareTo(GPUArchetypeHandle other) { return index.CompareTo(other.index); }
        public override int GetHashCode() { return index; }
    }

    //@ We could separate shader parameters from GPUComponents and alias different shader parameters to the same GPUComponent if they have the same size.
    //@ Basically that would allow to have multiple GPUComponentSets ([default + float4 ParamName1], [default + float4 ParamName2], etc...) with the same component sizes but different propertyIDs but mapped to the same archetype.
    //@ This will not be very hard to do but it will require some additional bookkeeping. But in the end it will decrease the number of archetypes that we have to handle on GPU.
    internal struct GPUComponentSet : IEquatable<GPUComponentSet>, IComparable<GPUComponentSet>, IEnumerable
    {
        public ulong componentsMask { get; private set; }

        public bool isEmpty => componentsMask == 0;

        public bool Equals(GPUComponentSet other) => componentsMask == other.componentsMask;
        public int CompareTo(GPUComponentSet other) { return componentsMask.CompareTo(other.componentsMask); }
        public override int GetHashCode() { return componentsMask.GetHashCode(); }

        static GPUComponentSet()
        {
            Assert.AreEqual(GPUArchetypeManager.kMaxComponentsCount, UnsafeUtility.SizeOf<ulong>() * 8, "Adjust componentsMask type to correspond to kMaxComponentsCount.");
        }

        public GPUComponentSet(NativeArray<GPUComponentHandle> components)
        {
            componentsMask = 0;
            AddRange(components);
        }

        public void Add(GPUComponentHandle componentHandle)
        {
            Assert.IsTrue(componentHandle.valid);
            componentsMask |= 1ul << componentHandle.index;
        }

        public void AddSet(GPUComponentSet componentSet)
        {
            componentsMask |= componentSet.componentsMask;
        }

        public void AddRange(NativeArray<GPUComponentHandle> components)
        {
            for (int i = 0; i < components.Length; ++i)
                Add(components[i]);
        }

        public int GetComponentsCount()
        {
            return math.countbits(componentsMask);
        }

        public IEnumerator GetEnumerator()
        {
            int count = GetComponentsCount();
            for (int i = 0; i < count; i++)
                yield return GetComponentByIndex(i);
        }

        public GPUComponentHandle GetComponentByIndex(int index)
        {
            Assert.IsTrue(index >= 0 && index < GetComponentsCount(), "Index out of range.");

            ulong mask = componentsMask;
            int compIndex = 0;

            while (mask != 0)
            {
                var component = GPUComponentHandle.Create((short)math.tzcnt(mask));
                mask &= ~(1ul << component.index);
                if (compIndex++ == index)
                    return component;
            }

            throw new InvalidOperationException("Index out of range.");
        }

        public NativeList<GPUComponentHandle> GetComponents(Allocator allocator)
        {
            var components = new NativeList<GPUComponentHandle>(GetComponentsCount(), allocator);

            for (int i = 0; i < GPUArchetypeManager.kMaxComponentsCount; ++i)
            {
                if ((componentsMask & (1ul << i)) != 0)
                    components.AddNoResize(GPUComponentHandle.Create((short)i));
            }

            return components;
        }
    }
}
