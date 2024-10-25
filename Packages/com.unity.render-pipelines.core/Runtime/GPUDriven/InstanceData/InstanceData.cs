using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

/// -----------------------------------------------------------------------
/// See the data layout and relationship diagram at the bottom of the file.
/// -----------------------------------------------------------------------

namespace UnityEngine.Rendering
{
    internal struct CPUInstanceData : IDisposable
    {
        private const int k_InvalidIndex = -1;

        private NativeArray<int> m_StructData;
        private NativeList<int> m_InstanceIndices;

        public NativeArray<InstanceHandle> instances;
        public NativeArray<SharedInstanceHandle> sharedInstances;
        public ParallelBitArray localToWorldIsFlippedBits;
        public NativeArray<AABB> worldAABBs;
        public NativeArray<int> tetrahedronCacheIndices;
        public ParallelBitArray movedInCurrentFrameBits;
        public ParallelBitArray movedInPreviousFrameBits;
        public ParallelBitArray visibleInPreviousFrameBits;
        public EditorInstanceDataArrays editorData;

        public int instancesLength { get => m_StructData[0]; set => m_StructData[0] = value; }
        public int instancesCapacity { get => m_StructData[1]; set => m_StructData[1] = value; }
        public int handlesLength => m_InstanceIndices.Length;

        public void Initialize(int initCapacity)
        {
            m_StructData = new NativeArray<int>(2, Allocator.Persistent);
            instancesCapacity = initCapacity;
            m_InstanceIndices = new NativeList<int>(Allocator.Persistent);
            instances = new NativeArray<InstanceHandle>(instancesCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            instances.FillArray(InstanceHandle.Invalid);
            sharedInstances = new NativeArray<SharedInstanceHandle>(instancesCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            sharedInstances.FillArray(SharedInstanceHandle.Invalid);
            localToWorldIsFlippedBits = new ParallelBitArray(instancesCapacity, Allocator.Persistent);
            worldAABBs = new NativeArray<AABB>(instancesCapacity, Allocator.Persistent);
            tetrahedronCacheIndices = new NativeArray<int>(instancesCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            tetrahedronCacheIndices.FillArray(k_InvalidIndex);
            movedInCurrentFrameBits = new ParallelBitArray(instancesCapacity, Allocator.Persistent);
            movedInPreviousFrameBits = new ParallelBitArray(instancesCapacity, Allocator.Persistent);
            visibleInPreviousFrameBits = new ParallelBitArray(instancesCapacity, Allocator.Persistent);
            editorData.Initialize(initCapacity);
        }

        public void Dispose()
        {
            m_StructData.Dispose();
            m_InstanceIndices.Dispose();
            instances.Dispose();
            sharedInstances.Dispose();
            localToWorldIsFlippedBits.Dispose();
            worldAABBs.Dispose();
            tetrahedronCacheIndices.Dispose();
            movedInCurrentFrameBits.Dispose();
            movedInPreviousFrameBits.Dispose();
            visibleInPreviousFrameBits.Dispose();
            editorData.Dispose();
        }

        private void Grow(int newCapacity)
        {
            Assert.IsTrue(newCapacity > instancesCapacity);

            instances.ResizeArray(newCapacity);
            instances.FillArray(InstanceHandle.Invalid, instancesCapacity);
            sharedInstances.ResizeArray(newCapacity);
            sharedInstances.FillArray(SharedInstanceHandle.Invalid, instancesCapacity);
            localToWorldIsFlippedBits.Resize(newCapacity);
            worldAABBs.ResizeArray(newCapacity);
            tetrahedronCacheIndices.ResizeArray(newCapacity);
            tetrahedronCacheIndices.FillArray(k_InvalidIndex, instancesCapacity);
            movedInCurrentFrameBits.Resize(newCapacity);
            movedInPreviousFrameBits.Resize(newCapacity);
            visibleInPreviousFrameBits.Resize(newCapacity);
            editorData.Grow(newCapacity);

            instancesCapacity = newCapacity;
        }

        private void AddUnsafe(InstanceHandle instance)
        {
            if (instance.index >= m_InstanceIndices.Length)
            {
                int prevLength = m_InstanceIndices.Length;
                m_InstanceIndices.ResizeUninitialized(instance.index + 1);

                for (int i = prevLength; i < m_InstanceIndices.Length - 1; ++i)
                    m_InstanceIndices[i] = k_InvalidIndex;
            }

            m_InstanceIndices[instance.index] = instancesLength;
            instances[instancesLength] = instance;

            ++instancesLength;
        }

        public int InstanceToIndex(InstanceHandle instance)
        {
            Assert.IsTrue(IsValidInstance(instance));
            return m_InstanceIndices[instance.index];
        }

        public InstanceHandle IndexToInstance(int index)
        {
            Assert.IsTrue(IsValidIndex(index));
            return instances[index];
        }

        public bool IsValidInstance(InstanceHandle instance)
        {
            if (instance.valid && instance.index < m_InstanceIndices.Length)
            {
                int index = m_InstanceIndices[instance.index];
                return index >= 0 && index < instancesLength && instances[index].Equals(instance);
            }
            return false;
        }

        public bool IsFreeInstanceHandle(InstanceHandle instance)
        {
            return instance.valid && (instance.index >= m_InstanceIndices.Length || m_InstanceIndices[instance.index] == k_InvalidIndex);
        }

        public bool IsValidIndex(int index)
        {
            if (index >= 0 && index < instancesLength)
            {
                InstanceHandle instance = instances[index];
                return index == m_InstanceIndices[instance.index];
            }
            return false;
        }

        public int GetFreeInstancesCount()
        {
            return instancesCapacity - instancesLength;
        }

        public void EnsureFreeInstances(int instancesCount)
        {
            int freeInstancesCount = GetFreeInstancesCount();
            int needInstances = instancesCount - freeInstancesCount;

            if (needInstances > 0)
                Grow(instancesCapacity + needInstances + 256);
        }

        public void AddNoGrow(InstanceHandle instance)
        {
            Assert.IsTrue(instance.valid);
            Assert.IsTrue(IsFreeInstanceHandle(instance));
            Assert.IsTrue(GetFreeInstancesCount() > 0);

            AddUnsafe(instance);
            SetDefault(instance);
        }

        public void Add(InstanceHandle instance)
        {
            EnsureFreeInstances(1);
            AddNoGrow(instance);
        }

        public void Remove(InstanceHandle instance)
        {
            Assert.IsTrue(IsValidInstance(instance));

            int index = InstanceToIndex(instance);
            int lastIndex = instancesLength - 1;

            instances[index] = instances[lastIndex];
            sharedInstances[index] = sharedInstances[lastIndex];
            localToWorldIsFlippedBits.Set(index, localToWorldIsFlippedBits.Get(lastIndex));
            worldAABBs[index] = worldAABBs[lastIndex];
            tetrahedronCacheIndices[index] = tetrahedronCacheIndices[lastIndex];
            movedInCurrentFrameBits.Set(index, movedInCurrentFrameBits.Get(lastIndex));
            movedInPreviousFrameBits.Set(index, movedInPreviousFrameBits.Get(lastIndex));
            visibleInPreviousFrameBits.Set(index, visibleInPreviousFrameBits.Get(lastIndex));
            editorData.Remove(index, lastIndex);

            m_InstanceIndices[instances[lastIndex].index] = index;
            m_InstanceIndices[instance.index] = k_InvalidIndex;
            instancesLength -= 1;
        }

        public void Set(InstanceHandle instance, SharedInstanceHandle sharedInstance, bool localToWorldIsFlipped, in AABB worldAABB, int tetrahedronCacheIndex,
            bool movedInCurrentFrame, bool movedInPreviousFrame, bool visibleInPreviousFrame)
        {
            int index = InstanceToIndex(instance);
            sharedInstances[index] = sharedInstance;
            localToWorldIsFlippedBits.Set(index, localToWorldIsFlipped);
            worldAABBs[index] = worldAABB;
            tetrahedronCacheIndices[index] = tetrahedronCacheIndex;
            movedInCurrentFrameBits.Set(index, movedInCurrentFrame);
            movedInPreviousFrameBits.Set(index, movedInPreviousFrame);
            visibleInPreviousFrameBits.Set(index, visibleInPreviousFrame);
            editorData.SetDefault(index);
        }

        public void SetDefault(InstanceHandle instance)
        {
            Set(instance, SharedInstanceHandle.Invalid, false, new AABB(), k_InvalidIndex, false, false, false);
        }

        // These accessors just for convenience and additional safety.
        // In general prefer converting an instance to an index and access by index.
        public SharedInstanceHandle Get_SharedInstance(InstanceHandle instance) { return sharedInstances[InstanceToIndex(instance)]; }
        public bool Get_LocalToWorldIsFlipped(InstanceHandle instance) { return localToWorldIsFlippedBits.Get(InstanceToIndex(instance)); }
        public AABB Get_WorldAABB(InstanceHandle instance) { return worldAABBs[InstanceToIndex(instance)]; }
        public int Get_TetrahedronCacheIndex(InstanceHandle instance) { return tetrahedronCacheIndices[InstanceToIndex(instance)]; }
        public unsafe ref AABB Get_WorldBounds(InstanceHandle instance) { return ref UnsafeUtility.ArrayElementAsRef<AABB>(worldAABBs.GetUnsafePtr(), InstanceToIndex(instance)); }
        public bool Get_MovedInCurrentFrame(InstanceHandle instance) { return movedInCurrentFrameBits.Get(InstanceToIndex(instance)); }
        public bool Get_MovedInPreviousFrame(InstanceHandle instance) { return movedInPreviousFrameBits.Get(InstanceToIndex(instance)); }
        public bool Get_VisibleInPreviousFrame(InstanceHandle instance) { return visibleInPreviousFrameBits.Get(InstanceToIndex(instance)); }

        public void Set_SharedInstance(InstanceHandle instance, SharedInstanceHandle sharedInstance) { sharedInstances[InstanceToIndex(instance)] = sharedInstance; }
        public void Set_LocalToWorldIsFlipped(InstanceHandle instance, bool isFlipped) { localToWorldIsFlippedBits.Set(InstanceToIndex(instance), isFlipped); }
        public void Set_WorldAABB(InstanceHandle instance, in AABB worldBounds) { worldAABBs[InstanceToIndex(instance)] = worldBounds; }
        public void Set_TetrahedronCacheIndex(InstanceHandle instance, int tetrahedronCacheIndex) { tetrahedronCacheIndices[InstanceToIndex(instance)] = tetrahedronCacheIndex; }
        public void Set_MovedInCurrentFrame(InstanceHandle instance, bool movedInCurrentFrame) { movedInCurrentFrameBits.Set(InstanceToIndex(instance), movedInCurrentFrame); }
        public void Set_MovedInPreviousFrame(InstanceHandle instance, bool movedInPreviousFrame) { movedInPreviousFrameBits.Set(InstanceToIndex(instance), movedInPreviousFrame); }
        public void Set_VisibleInPreviousFrame(InstanceHandle instance, bool visibleInPreviousFrame) { visibleInPreviousFrameBits.Set(InstanceToIndex(instance), visibleInPreviousFrame); }

        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(this);
        }

        internal readonly struct ReadOnly
        {
            public readonly NativeArray<int>.ReadOnly instanceIndices;
            public readonly NativeArray<InstanceHandle>.ReadOnly instances;
            public readonly NativeArray<SharedInstanceHandle>.ReadOnly sharedInstances;
            public readonly ParallelBitArray localToWorldIsFlippedBits;
            public readonly NativeArray<AABB>.ReadOnly worldAABBs;
            public readonly NativeArray<int>.ReadOnly tetrahedronCacheIndices;
            public readonly ParallelBitArray movedInCurrentFrameBits;
            public readonly ParallelBitArray movedInPreviousFrameBits;
            public readonly ParallelBitArray visibleInPreviousFrameBits;
            public readonly EditorInstanceDataArrays.ReadOnly editorData;
            public readonly int handlesLength => instanceIndices.Length;
            public readonly int instancesLength => instances.Length;

            public ReadOnly(in CPUInstanceData instanceData)
            {
                instanceIndices = instanceData.m_InstanceIndices.AsArray().AsReadOnly();
                instances = instanceData.instances.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                sharedInstances = instanceData.sharedInstances.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                localToWorldIsFlippedBits = instanceData.localToWorldIsFlippedBits.GetSubArray(instanceData.instancesLength);
                worldAABBs = instanceData.worldAABBs.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                tetrahedronCacheIndices = instanceData.tetrahedronCacheIndices.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                movedInCurrentFrameBits = instanceData.movedInCurrentFrameBits.GetSubArray(instanceData.instancesLength);//.AsReadOnly(); // Implement later.
                movedInPreviousFrameBits = instanceData.movedInPreviousFrameBits.GetSubArray(instanceData.instancesLength);//.AsReadOnly(); // Implement later.
                visibleInPreviousFrameBits = instanceData.visibleInPreviousFrameBits.GetSubArray(instanceData.instancesLength);//.AsReadOnly(); // Implement later.
                editorData = new EditorInstanceDataArrays.ReadOnly(instanceData);
            }

            public int InstanceToIndex(InstanceHandle instance)
            {
                Assert.IsTrue(IsValidInstance(instance));
                return instanceIndices[instance.index];
            }

            public InstanceHandle IndexToInstance(int index)
            {
                Assert.IsTrue(IsValidIndex(index));
                return instances[index];
            }

            public bool IsValidInstance(InstanceHandle instance)
            {
                if (instance.valid && instance.index < instanceIndices.Length)
                {
                    int index = instanceIndices[instance.index];
                    return index >= 0 && index < instances.Length && instances[index].Equals(instance);
                }
                return false;
            }

            public bool IsValidIndex(int index)
            {
                if (index >= 0 && index < instances.Length)
                {
                    InstanceHandle instance = instances[index];
                    return index == instanceIndices[instance.index];
                }
                return false;
            }
        }
    }

    internal struct CPUSharedInstanceData : IDisposable
    {
        private const int k_InvalidIndex = -1;
        private const uint k_InvalidLODGroupAndMask = 0xFFFFFFFF;

        private NativeArray<int> m_StructData;
        private NativeList<int> m_InstanceIndices;

        //@ Need to figure out the way to share the code with CPUInstanceData. Both structures are almost identical.
        public NativeArray<SharedInstanceHandle> instances;
        public NativeArray<int> rendererGroupIDs;

        // For now we just use nested collections since materialIDs are only parsed rarely. E.g. when an unsupported material is detected.
        public NativeArray<SmallIntegerArray> materialIDArrays;
        
        public NativeArray<int> meshIDs;
        public NativeArray<AABB> localAABBs;
        public NativeArray<CPUSharedInstanceFlags> flags;
        public NativeArray<uint> lodGroupAndMasks;
        public NativeArray<int> gameObjectLayers;
        public NativeArray<int> refCounts;

        public int instancesLength { get => m_StructData[0]; set => m_StructData[0] = value; }
        public int instancesCapacity { get => m_StructData[1]; set => m_StructData[1] = value; }
        public int handlesLength => m_InstanceIndices.Length;

        public void Initialize(int initCapacity)
        {
            m_StructData = new NativeArray<int>(2, Allocator.Persistent);
            instancesCapacity = initCapacity;
            m_InstanceIndices = new NativeList<int>(Allocator.Persistent);
            instances = new NativeArray<SharedInstanceHandle>(instancesCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            instances.FillArray(SharedInstanceHandle.Invalid);
            rendererGroupIDs = new NativeArray<int>(instancesCapacity, Allocator.Persistent);
            materialIDArrays = new NativeArray<SmallIntegerArray>(instancesCapacity, Allocator.Persistent);
            meshIDs = new NativeArray<int>(instancesCapacity, Allocator.Persistent);
            localAABBs = new NativeArray<AABB>(instancesCapacity, Allocator.Persistent);
            flags = new NativeArray<CPUSharedInstanceFlags>(instancesCapacity, Allocator.Persistent);
            lodGroupAndMasks = new NativeArray<uint>(instancesCapacity, Allocator.Persistent);
            lodGroupAndMasks.FillArray(k_InvalidLODGroupAndMask);
            gameObjectLayers = new NativeArray<int>(instancesCapacity, Allocator.Persistent);
            refCounts = new NativeArray<int>(instancesCapacity, Allocator.Persistent);
        }

        public void Dispose()
        {
            m_StructData.Dispose();
            m_InstanceIndices.Dispose();
            instances.Dispose();
            rendererGroupIDs.Dispose();

            foreach (var materialIDs in materialIDArrays)
            {
                materialIDs.Dispose();
            }
            materialIDArrays.Dispose();

            meshIDs.Dispose();
            localAABBs.Dispose();
            flags.Dispose();
            lodGroupAndMasks.Dispose();
            gameObjectLayers.Dispose();
            refCounts.Dispose();
        }

        private void Grow(int newCapacity)
        {
            Assert.IsTrue(newCapacity > instancesCapacity);

            instances.ResizeArray(newCapacity);
            instances.FillArray(SharedInstanceHandle.Invalid, instancesCapacity);
            rendererGroupIDs.ResizeArray(newCapacity);
            materialIDArrays.ResizeArray(newCapacity);
            materialIDArrays.FillArray(default, instancesCapacity);
            meshIDs.ResizeArray(newCapacity);
            localAABBs.ResizeArray(newCapacity);
            flags.ResizeArray(newCapacity);
            lodGroupAndMasks.ResizeArray(newCapacity);
            lodGroupAndMasks.FillArray(k_InvalidLODGroupAndMask, instancesCapacity);
            gameObjectLayers.ResizeArray(newCapacity);
            refCounts.ResizeArray(newCapacity);

            instancesCapacity = newCapacity;
        }

        private void AddUnsafe(SharedInstanceHandle instance)
        {
            if (instance.index >= m_InstanceIndices.Length)
            {
                int prevLength = m_InstanceIndices.Length;
                m_InstanceIndices.ResizeUninitialized(instance.index + 1);

                for (int i = prevLength; i < m_InstanceIndices.Length - 1; ++i)
                    m_InstanceIndices[i] = k_InvalidIndex;
            }

            m_InstanceIndices[instance.index] = instancesLength;
            instances[instancesLength] = instance;

            ++instancesLength;
        }

        public int SharedInstanceToIndex(SharedInstanceHandle instance)
        {
            Assert.IsTrue(IsValidInstance(instance));
            return m_InstanceIndices[instance.index];
        }

        public SharedInstanceHandle IndexToSharedInstance(int index)
        {
            Assert.IsTrue(IsValidIndex(index));
            return instances[index];
        }

        public int InstanceToIndex(in CPUInstanceData instanceData, InstanceHandle instance)
        {
            int instanceIndex = instanceData.InstanceToIndex(instance);
            SharedInstanceHandle sharedInstance = instanceData.sharedInstances[instanceIndex];
            int sharedInstanceIndex = SharedInstanceToIndex(sharedInstance);
            return sharedInstanceIndex;
        }

        public bool IsValidInstance(SharedInstanceHandle instance)
        {
            if (instance.valid && instance.index < m_InstanceIndices.Length)
            {
                int index = m_InstanceIndices[instance.index];
                return index >= 0 && index < instancesLength && instances[index].Equals(instance);
            }
            return false;
        }

        public bool IsFreeInstanceHandle(SharedInstanceHandle instance)
        {
            return instance.valid && (instance.index >= m_InstanceIndices.Length || m_InstanceIndices[instance.index] == k_InvalidIndex);
        }

        public bool IsValidIndex(int index)
        {
            if (index >= 0 && index < instancesLength)
            {
                SharedInstanceHandle instance = instances[index];
                return index == m_InstanceIndices[instance.index];
            }
            return false;
        }

        public int GetFreeInstancesCount()
        {
            return instancesCapacity - instancesLength;
        }

        public void EnsureFreeInstances(int instancesCount)
        {
            int freeInstancesCount = GetFreeInstancesCount();
            int needInstances = instancesCount - freeInstancesCount;

            if (needInstances > 0)
                Grow(instancesCapacity + needInstances + 256);
        }

        public void AddNoGrow(SharedInstanceHandle instance)
        {
            Assert.IsTrue(instance.valid);
            Assert.IsTrue(IsFreeInstanceHandle(instance));
            Assert.IsTrue(GetFreeInstancesCount() > 0);

            AddUnsafe(instance);
            SetDefault(instance);
        }

        public void Add(SharedInstanceHandle instance)
        {
            EnsureFreeInstances(1);
            AddNoGrow(instance);
        }

        public void Remove(SharedInstanceHandle instance)
        {
            Assert.IsTrue(IsValidInstance(instance));

            int index = SharedInstanceToIndex(instance);
            int lastIndex = instancesLength - 1;

            instances[index] = instances[lastIndex];
            rendererGroupIDs[index] = rendererGroupIDs[lastIndex];

            materialIDArrays[index].Dispose();
            materialIDArrays[index] = materialIDArrays[lastIndex];
            materialIDArrays[lastIndex] = default;

            meshIDs[index] = meshIDs[lastIndex];
            localAABBs[index] = localAABBs[lastIndex];
            flags[index] = flags[lastIndex];
            lodGroupAndMasks[index] = lodGroupAndMasks[lastIndex];
            gameObjectLayers[index] = gameObjectLayers[lastIndex];
            refCounts[index] = refCounts[lastIndex];

            m_InstanceIndices[instances[lastIndex].index] = index;
            m_InstanceIndices[instance.index] = k_InvalidIndex;
            instancesLength -= 1;
        }

        // These accessors just for convenience and additional safety.
        // In general prefer converting an instance to an index and access by index.
        public int Get_RendererGroupID(SharedInstanceHandle instance) { return rendererGroupIDs[SharedInstanceToIndex(instance)]; }
        public int Get_MeshID(SharedInstanceHandle instance) { return meshIDs[SharedInstanceToIndex(instance)]; }
        public unsafe ref AABB Get_LocalAABB(SharedInstanceHandle instance) { return ref UnsafeUtility.ArrayElementAsRef<AABB>(localAABBs.GetUnsafePtr(), SharedInstanceToIndex(instance)); }
        public CPUSharedInstanceFlags Get_Flags(SharedInstanceHandle instance) { return flags[SharedInstanceToIndex(instance)]; }
        public uint Get_LODGroupAndMask(SharedInstanceHandle instance) { return lodGroupAndMasks[SharedInstanceToIndex(instance)]; }
        public int Get_GameObjectLayer(SharedInstanceHandle instance) { return gameObjectLayers[SharedInstanceToIndex(instance)]; }
        public int Get_RefCount(SharedInstanceHandle instance) { return refCounts[SharedInstanceToIndex(instance)]; }
        public unsafe ref SmallIntegerArray Get_MaterialIDs(SharedInstanceHandle instance) { return ref UnsafeUtility.ArrayElementAsRef<SmallIntegerArray>(materialIDArrays.GetUnsafePtr(), SharedInstanceToIndex(instance)); }

        public void Set_RendererGroupID(SharedInstanceHandle instance, int rendererGroupID) { rendererGroupIDs[SharedInstanceToIndex(instance)] = rendererGroupID; }
        public void Set_MeshID(SharedInstanceHandle instance, int meshID) { meshIDs[SharedInstanceToIndex(instance)] = meshID; }
        public void Set_LocalAABB(SharedInstanceHandle instance, in AABB localAABB) { localAABBs[SharedInstanceToIndex(instance)] = localAABB; }
        public void Set_Flags(SharedInstanceHandle instance, CPUSharedInstanceFlags instanceFlags) { flags[SharedInstanceToIndex(instance)] = instanceFlags; }
        public void Set_LODGroupAndMask(SharedInstanceHandle instance, uint lodGroupAndMask) { lodGroupAndMasks[SharedInstanceToIndex(instance)] = lodGroupAndMask; }
        public void Set_GameObjectLayer(SharedInstanceHandle instance, int gameObjectLayer) { gameObjectLayers[SharedInstanceToIndex(instance)] = gameObjectLayer; }
        public void Set_RefCount(SharedInstanceHandle instance, int refCount) { refCounts[SharedInstanceToIndex(instance)] = refCount; }
        public void Set_MaterialIDs(SharedInstanceHandle instance, in SmallIntegerArray materialIDs)
        {
            int index = SharedInstanceToIndex(instance);
            materialIDArrays[index].Dispose();
            materialIDArrays[index] = materialIDs;
        }

        public void Set(SharedInstanceHandle instance, int rendererGroupID, in SmallIntegerArray materialIDs, int meshID, in AABB localAABB, TransformUpdateFlags transformUpdateFlags,
            InstanceFlags instanceFlags, uint lodGroupAndMask, int gameObjectLayer, int refCount)
        {
            int index = SharedInstanceToIndex(instance);

            rendererGroupIDs[index] = rendererGroupID;
            materialIDArrays[index].Dispose();
            materialIDArrays[index] = materialIDs;
            meshIDs[index] = meshID;
            localAABBs[index] = localAABB;
            flags[index] = new CPUSharedInstanceFlags { transformUpdateFlags = transformUpdateFlags, instanceFlags = instanceFlags };
            lodGroupAndMasks[index] = lodGroupAndMask;
            gameObjectLayers[index] = gameObjectLayer;
            refCounts[index] = refCount;
        }

        public void SetDefault(SharedInstanceHandle instance)
        {
            Set(instance, 0, default, 0, new AABB(), TransformUpdateFlags.None, InstanceFlags.None, k_InvalidLODGroupAndMask, 0, 0);
        }

        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(this);
        }

        internal readonly struct ReadOnly
        {
            public readonly NativeArray<int>.ReadOnly instanceIndices;
            public readonly NativeArray<SharedInstanceHandle>.ReadOnly instances;
            public readonly NativeArray<int>.ReadOnly rendererGroupIDs;
            public readonly NativeArray<SmallIntegerArray>.ReadOnly materialIDArrays;
            public readonly NativeArray<int>.ReadOnly meshIDs;
            public readonly NativeArray<AABB>.ReadOnly localAABBs;
            public readonly NativeArray<CPUSharedInstanceFlags>.ReadOnly flags;
            public readonly NativeArray<uint>.ReadOnly lodGroupAndMasks;
            public readonly NativeArray<int>.ReadOnly gameObjectLayers;
            public readonly NativeArray<int>.ReadOnly refCounts;
            public readonly int handlesLength => instanceIndices.Length;
            public readonly int instancesLength => instances.Length;

            public ReadOnly(in CPUSharedInstanceData instanceData)
            {
                instanceIndices = instanceData.m_InstanceIndices.AsArray().AsReadOnly();
                instances = instanceData.instances.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                rendererGroupIDs = instanceData.rendererGroupIDs.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                materialIDArrays = instanceData.materialIDArrays.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                meshIDs = instanceData.meshIDs.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                localAABBs = instanceData.localAABBs.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                flags = instanceData.flags.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                lodGroupAndMasks = instanceData.lodGroupAndMasks.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                gameObjectLayers = instanceData.gameObjectLayers.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                refCounts = instanceData.refCounts.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
            }

            public int SharedInstanceToIndex(SharedInstanceHandle instance)
            {
                Assert.IsTrue(IsValidSharedInstance(instance));
                return instanceIndices[instance.index];
            }

            public SharedInstanceHandle IndexToSharedInstance(int index)
            {
                Assert.IsTrue(IsValidIndex(index));
                return instances[index];
            }

            public bool IsValidSharedInstance(SharedInstanceHandle instance)
            {
                if (instance.valid && instance.index < instanceIndices.Length)
                {
                    int index = instanceIndices[instance.index];
                    return index >= 0 && index < instances.Length && instances[index].Equals(instance);
                }
                return false;
            }

            public bool IsValidIndex(int index)
            {
                if (index >= 0 && index < instances.Length)
                {
                    SharedInstanceHandle instance = instances[index];
                    return index == instanceIndices[instance.index];
                }
                return false;
            }

            public int InstanceToIndex(in CPUInstanceData.ReadOnly instanceData, InstanceHandle instance)
            {
                int instanceIndex = instanceData.InstanceToIndex(instance);
                SharedInstanceHandle sharedInstance = instanceData.sharedInstances[instanceIndex];
                int sharedInstanceIndex = SharedInstanceToIndex(sharedInstance);
                return sharedInstanceIndex;
            }
        }
    }

    internal unsafe struct SmallIntegerArray : IDisposable
    {
        private FixedList32Bytes<int> m_FixedArray;
        private UnsafeList<int> m_List;
        private readonly bool m_IsEmbedded;

        public bool Valid { get; private set; }
        public readonly int Length;

        public SmallIntegerArray(int length, Allocator allocator)
        {
            m_FixedArray = default;
            m_List = default;
            Length = length;
            Valid = true;

            if (Length <= m_FixedArray.Capacity)
            {
                m_FixedArray = new FixedList32Bytes<int>();
                m_FixedArray.Length = Length;
                m_IsEmbedded = true;
            }
            else
            {
                m_List = new UnsafeList<int>(Length, allocator, NativeArrayOptions.UninitializedMemory);
                m_List.Resize(Length);
                m_IsEmbedded = false;
            }
        }

        public int this[int index]
        {
            get
            {
                Assert.IsTrue(Valid && index < Length);

                if (m_IsEmbedded)
                    return m_FixedArray[index];
                else
                    return m_List[index];
            }
            set
            {
                Assert.IsTrue(Valid && index < Length);

                if (m_IsEmbedded)
                    m_FixedArray[index] = value;
                else
                    m_List[index] = value;
            }
        }

        public unsafe void Dispose()
        {
            if (!Valid)
                return;
            m_List.Dispose();
            Valid = false;
        }
    }

    internal interface IDataArrays
    {
        void Initialize(int initCapacity);
        void Dispose();
        void Grow(int newCapacity);
        void Remove(int index, int lastIndex);
        void SetDefault(int index);
    }

    internal struct EditorInstanceDataArrays : IDataArrays
    {
#if UNITY_EDITOR
        public NativeArray<ulong> sceneCullingMasks;
        public ParallelBitArray selectedBits;

        public void Initialize(int initCapacity)
        {
            sceneCullingMasks = new NativeArray<ulong>(initCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            sceneCullingMasks.FillArray(ulong.MaxValue);
            selectedBits = new ParallelBitArray(initCapacity, Allocator.Persistent);
        }

        public void Dispose()
        {
            sceneCullingMasks.Dispose();
            selectedBits.Dispose();
        }

        public void Grow(int newCapacity)
        {
            sceneCullingMasks.ResizeArray(newCapacity);
            selectedBits.Resize(newCapacity);
        }

        public void Remove(int index, int lastIndex)
        {
            sceneCullingMasks[index] = sceneCullingMasks[lastIndex];
            selectedBits.Set(index, selectedBits.Get(lastIndex));
        }

        public void SetDefault(int index)
        {
            sceneCullingMasks[index] = ulong.MaxValue;
            selectedBits.Set(index, false);
        }

        internal readonly struct ReadOnly
        {
            public readonly NativeArray<ulong>.ReadOnly sceneCullingMasks;
            public readonly ParallelBitArray selectedBits;

            public ReadOnly(in CPUInstanceData instanceData)
            {
                sceneCullingMasks = instanceData.editorData.sceneCullingMasks.GetSubArray(0, instanceData.instancesLength).AsReadOnly();
                selectedBits = instanceData.editorData.selectedBits.GetSubArray(instanceData.instancesLength);
            }
        }
#else
        public void Initialize(int initCapacity) { }
        public void Dispose() { }
        public void Grow(int newCapacity) { }
        public void Remove(int index, int lastIndex) { }
        public void SetDefault(int index) { }
        internal readonly struct ReadOnly { public ReadOnly(in CPUInstanceData instanceData) { } }
#endif
    }

    [Flags]
    internal enum TransformUpdateFlags : byte
    {
        None = 0,
        HasLightProbeCombined = 1 << 0,
        IsPartOfStaticBatch = 1 << 1
    }

    [Flags]
    internal enum InstanceFlags : byte
    {
        None = 0,
        AffectsLightmaps = 1 << 0, // either lightmapped or influence-only
        IsShadowsOff = 1 << 1, // shadow casting mode is ShadowCastingMode.Off
        IsShadowsOnly = 1 << 2, // shadow casting mode is ShadowCastingMode.ShadowsOnly
        HasProgressiveLod = 1 << 3,
        SmallMeshCulling = 1 << 4
    }

    internal struct CPUSharedInstanceFlags
    {
        public TransformUpdateFlags transformUpdateFlags;
        public InstanceFlags instanceFlags;
    }

    internal struct PackedMatrix
    {
        /*  mat4x3 packed like this:
                p1.x, p1.w, p2.z, p3.y,
                p1.y, p2.x, p2.w, p3.z,
                p1.z, p2.y, p3.x, p3.w,
                0.0,  0.0,  0.0,  1.0
        */

        public float4 packed0;
        public float4 packed1;
        public float4 packed2;

        public static PackedMatrix FromMatrix4x4(in Matrix4x4 m)
        {
            return new PackedMatrix
            {
                packed0 = new float4(m.m00, m.m10, m.m20, m.m01),
                packed1 = new float4(m.m11, m.m21, m.m02, m.m12),
                packed2 = new float4(m.m22, m.m03, m.m13, m.m23)
            };
        }

        public static PackedMatrix FromFloat4x4(in float4x4 m)
        {
            return new PackedMatrix
            {
                packed0 = new float4(m.c0.x, m.c0.y, m.c0.z, m.c1.x),
                packed1 = new float4(m.c1.y, m.c1.z, m.c2.x, m.c2.y),
                packed2 = new float4(m.c2.z, m.c3.x, m.c3.y, m.c3.z)
            };
        }
    }
}

//                                                                  +-------------+
//                                                                  |  Instance   |
//                                                                  |  Handle 2   |
//                                                                  +------^------+
//                                                       +-------------+   |      +-------------+
//                                                       |  Instance   |   |      |  Instance   |
//                                                       |  Handle 0   |   |      |  Handle 3   |
//                                                       +------^------+   |      +---^---------+
//                                                              |          |         /
//                                                              |          |        /
//                          +-----------------------------------------------------------------------------------------------+
//                          |                                   |          |      /                                         |
//                          |                                 +-v-- ----+--v - +---v---+----+----+----+                     |
//                          |                 InstanceIndices | 0  |free| 1  | 2  |free|free|free|free|...                  |
//                          |                                 +--^-+----+--^-+--^-+----+----+----+----+                     |
//                          |                                    |        /    /                                            |
//                          |                                    |       /    / +-------------------------                  |
//                          |                                    |      /    /  |    +-----------------------               |
//                          |                                    |     /    /   |    |                                      |
//                          |                                 +--v-+--v-+--v-+--v-+--v-+----+                               |
//                          |                       Instances |  0 |  2 |  3 |    |    |... |                               |
//                          |                                 +----+----+----+----+----+----+                               |
//         CPUInstanceData  |            LocalToWorldMatrices |    |    |    |    |    |... |                               |
//                          |                                 +----+----+----+----+----+----+                               |
//                          |                   WorldBoundses |    |    |    |    |    |... |                               |
//                          |                                 +----+----+----+----+----+----+                               |
//                          |           SharedInstanceHandles |    |    |    |    |    |... |                               |
//                          |                                 +----+----+----+----+----+----+                               |
//                          |         MovedInCurrentFrameBits |    |    |    |    |    | ...|                               |
//                          |                                 +----+----+----+----+----+----+                               |
//                          |                 SharedInstances |  1 | 1  | 1  | 2  | 3  | ...|                               |
//                          |                                 +-\--+--|-+--/-+--/-+--/-+----+                               |
//                          |                                    |    |   /    /    /                                       |
//                          +-----------------------------------------------------------------------------------------------+
//                                                                \   / |    |    |
//                                                                 \ |  /    /    /
//                          +-----------------------------------------------------------------------------------------------+
//                          |                                       \|/    /    /                                           |
//                          |                                 +----+-v--+-v--+-v--+----+----+----+----+                     |
//                          |           SharedInstanceIndices |free| 0  | 1  | 2  |free|free|free|free|...                  |
//                          |                                 +----+-|--+--|-+--|-+----+----+----+----+                     |
//                          |                                       /     /    /                                            |
//                          |                                      /     /    /                                             |
//                          |                                     /     /    /                                              |
//                          |                                     |     |    |                                              |
//                          |                                    /     /    /                                               |
//                          |                                 +-v--+--v-+--v-+----+----+----+                               |
//   CPUSharedInstanceData  |                         MeshIDs |    |    |    |... |... |... |                               |
//                          |                                 +----+----+----+----+----+----+                               |
//                          |                   LocalBoundses |    |    |    |... |... | ...|                               |
//                          |                                 +----+----+----+----+----+----+                               |
//                          |                RendererGroupIDs |    |    |    | ...| ...| ...|                               |
//                          |                                 +----+----+----+----+----+----+                               |
//                          |                 GameObjectLayer |    |    |    |... |... | ...|                               |
//                          |                                 +----+----+----+----+----+----+                               |
//                          |                           Flags |    |    |    |... |... | ...|                               |
//                          |                                 +----+----+----+----+----+----+                               |
//                          |                       RefCounts | 3  | 1  |  1 |... |... | ...|                               |
//                          |                                 +----+----+----+----+----+----+                               |
//                          +-----------------------------------------------------------------------------------------------+
