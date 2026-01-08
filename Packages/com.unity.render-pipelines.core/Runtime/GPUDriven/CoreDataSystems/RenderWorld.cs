using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace UnityEngine.Rendering
{
    internal struct RenderWorld : IDisposable
    {
        public static readonly EntityId DefaultMesh = EntityId.None;
        public static readonly ushort DefaultSubMeshStartIndex = 0;
        public static readonly AABB DefaultLocalBounds = default;
        public static readonly InternalMeshRendererSettings DefaultRendererSettings = InternalMeshRendererSettings.Default;
        public static readonly InternalMeshLodRendererSettings DefaultMeshLodRendererSettings = InternalMeshLodRendererSettings.Default;
        public static readonly int DefaultParentLODGroupID = 0;
        public static readonly byte DefaultLODMask = 0;
        public static readonly short DefaultLightmapIndex = LightmapUtils.LightmapIndexNull;
        public static readonly int DefaultRendererPriority = 0;

#if UNITY_EDITOR
        public static readonly ulong DefaultSceneCullingMask = SceneCullingMasks.DefaultSceneCullingMask;
#endif

        private const int InvalidIndex = -1;

        // Keep these as NativeReference so that they can be modified from Burst jobs operating on the RenderWorld.
        // Also keep the length separately because the component NativeArrays length is atually the capacity.
        // Arrays growth/realloc doesn't follow a double on realloc policy, it is manually controlled.
        private NativeReference<int> m_InstancesCount;
        private NativeReference<int> m_TotalTreeCount;

        private NativeList<int> m_HandleToIndex;
        private NativeArray<InstanceHandle> m_IndexToHandle;
        private NativeArray<EntityId> m_InstanceIDs;
        private NativeArray<EmbeddedArray32<EntityId>> m_MaterialIDArrays;
        private NativeArray<EntityId> m_MeshIDs;
        private NativeArray<InternalMeshLodRendererSettings> m_MeshLodRendererSettings;
        private NativeArray<ushort> m_SubMeshStartIndices;
        private NativeArray<AABB> m_LocalAABBs;
        private NativeArray<InternalMeshRendererSettings> m_RendererSettings;
        private NativeArray<short> m_LightmapIndices;
        private NativeArray<GPUInstanceIndex> m_LODGroupIndices;
        private NativeArray<byte> m_LODMasks;
        private NativeArray<int> m_RendererPriorities;
        private NativeArray<InstanceGPUHandle> m_GPUHandles;
        private ParallelBitArray m_LocalToWorldIsFlippedBits;
        private NativeArray<AABB> m_WorldAABBs;
        private NativeArray<int> m_TetrahedronCacheIndices;
        private ParallelBitArray m_MovedInCurrentFrameBits;
        private ParallelBitArray m_MovedInPreviousFrameBits;
        private ParallelBitArray m_VisibleInPreviousFrameBits;
        private ParallelBitArray m_RenderingEnabled;
        private EditorOnly m_EditorOnly;
        private NativeParallelHashMap<EntityId, UnsafePerCameraInstanceData> m_PerCameraInstanceDataMap;

        public NativeArray<InstanceHandle> indexToHandle => m_IndexToHandle.GetSubArray(0, instanceCount);
        public NativeArray<EntityId> instanceIDs => m_InstanceIDs.GetSubArray(0, instanceCount);
        public NativeArray<EmbeddedArray32<EntityId>> materialIDArrays => m_MaterialIDArrays.GetSubArray(0, instanceCount);
        public NativeArray<EntityId> meshIDs => m_MeshIDs.GetSubArray(0, instanceCount);
        public NativeArray<InternalMeshLodRendererSettings> meshLodRendererSettings => m_MeshLodRendererSettings.GetSubArray(0, instanceCount);
        public NativeArray<ushort> subMeshStartIndices => m_SubMeshStartIndices.GetSubArray(0, instanceCount);
        public NativeArray<AABB> localAABBs => m_LocalAABBs.GetSubArray(0, instanceCount);
        public NativeArray<InternalMeshRendererSettings> rendererSettings => m_RendererSettings.GetSubArray(0, instanceCount);
        public NativeArray<short> lightmapIndices => m_LightmapIndices.GetSubArray(0, instanceCount);
        public NativeArray<GPUInstanceIndex> lodGroupIndices => m_LODGroupIndices.GetSubArray(0, instanceCount);
        public NativeArray<byte> lodMasks => m_LODMasks.GetSubArray(0, instanceCount);
        public NativeArray<int> rendererPriorities => m_RendererPriorities.GetSubArray(0, instanceCount);
        public NativeArray<InstanceGPUHandle> gpuHandles => m_GPUHandles.GetSubArray(0, instanceCount);
        public ParallelBitArray localToWorldIsFlippedBits => m_LocalToWorldIsFlippedBits.GetSubArray(instanceCount);
        public NativeArray<AABB> worldAABBs => m_WorldAABBs.GetSubArray(0, instanceCount);
        public NativeArray<int> tetrahedronCacheIndices => m_TetrahedronCacheIndices.GetSubArray(0, instanceCount);
        public ParallelBitArray movedInCurrentFrameBits => m_MovedInCurrentFrameBits.GetSubArray(instanceCount);
        public ParallelBitArray movedInPreviousFrameBits => m_MovedInPreviousFrameBits.GetSubArray(instanceCount);
        public ParallelBitArray visibleInPreviousFrameBits => m_VisibleInPreviousFrameBits.GetSubArray(instanceCount);
        public ParallelBitArray renderingEnabled => m_RenderingEnabled.GetSubArray(instanceCount);

#if UNITY_EDITOR
        public NativeArray<ulong> sceneCullingMasks => m_EditorOnly.sceneCullingMasks.GetSubArray(0, instanceCount);
#endif

        public int instanceCount { get => m_InstancesCount.Value; private set => m_InstancesCount.Value = value; }
        public int handleCount => m_HandleToIndex.Length;
        public int totalTreeCount => m_TotalTreeCount.Value;
        public int cameraCount => m_PerCameraInstanceDataMap.Count();
        public unsafe UnsafeAtomicCounter32 atomicTotalTreeCount => new UnsafeAtomicCounter32(m_TotalTreeCount.GetUnsafePtr());

        public void Initialize(int initCapacity)
        {
            m_InstancesCount = new NativeReference<int>(0, Allocator.Persistent);
            m_TotalTreeCount = new NativeReference<int>(0, Allocator.Persistent);
            m_HandleToIndex = new NativeList<int>(Allocator.Persistent);
            m_IndexToHandle = new NativeArray<InstanceHandle>(initCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_IndexToHandle.FillArray(InstanceHandle.Invalid);
            m_InstanceIDs = new NativeArray<EntityId>(initCapacity, Allocator.Persistent);
            m_MaterialIDArrays = new NativeArray<EmbeddedArray32<EntityId>>(initCapacity, Allocator.Persistent);
            m_MeshIDs = new NativeArray<EntityId>(initCapacity, Allocator.Persistent);
            m_MeshLodRendererSettings = new NativeArray<InternalMeshLodRendererSettings>(initCapacity, Allocator.Persistent);
            m_SubMeshStartIndices = new NativeArray<ushort>(initCapacity, Allocator.Persistent);
            m_LocalAABBs = new NativeArray<AABB>(initCapacity, Allocator.Persistent);
            m_RendererSettings = new NativeArray<InternalMeshRendererSettings>(initCapacity, Allocator.Persistent);
            m_LightmapIndices = new NativeArray<short>(initCapacity, Allocator.Persistent);
            m_LODGroupIndices = new NativeArray<GPUInstanceIndex>(initCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_LODGroupIndices.FillArray(GPUInstanceIndex.Invalid);
            m_LODMasks = new NativeArray<byte>(initCapacity, Allocator.Persistent);
            m_RendererPriorities = new NativeArray<int>(initCapacity, Allocator.Persistent);
            m_GPUHandles = new NativeArray<InstanceGPUHandle>(initCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_GPUHandles.FillArray(InstanceGPUHandle.Invalid);
            m_LocalToWorldIsFlippedBits = new ParallelBitArray(initCapacity, Allocator.Persistent);
            m_WorldAABBs = new NativeArray<AABB>(initCapacity, Allocator.Persistent);
            m_TetrahedronCacheIndices = new NativeArray<int>(initCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_TetrahedronCacheIndices.FillArray(InvalidIndex);
            m_MovedInCurrentFrameBits = new ParallelBitArray(initCapacity, Allocator.Persistent);
            m_MovedInPreviousFrameBits = new ParallelBitArray(initCapacity, Allocator.Persistent);
            m_VisibleInPreviousFrameBits = new ParallelBitArray(initCapacity, Allocator.Persistent);
            m_RenderingEnabled = new ParallelBitArray(initCapacity, Allocator.Persistent);
            m_EditorOnly.Initialize(initCapacity);
            m_PerCameraInstanceDataMap = new NativeParallelHashMap<EntityId, UnsafePerCameraInstanceData>(1, Allocator.Persistent);
        }

        public void Dispose()
        {
            int aliveInstanceCount = m_InstancesCount.Value;

            m_InstancesCount.Dispose();
            m_TotalTreeCount.Dispose();
            m_HandleToIndex.Dispose();
            m_IndexToHandle.Dispose();
            m_InstanceIDs.Dispose();

            for (int i = 0; i < aliveInstanceCount; i++)
                m_MaterialIDArrays[i].Dispose();
            m_MaterialIDArrays.Dispose();

            m_MeshIDs.Dispose();
            m_MeshLodRendererSettings.Dispose();
            m_SubMeshStartIndices.Dispose();
            m_LocalAABBs.Dispose();
            m_RendererSettings.Dispose();
            m_LightmapIndices.Dispose();
            m_LODGroupIndices.Dispose();
            m_LODMasks.Dispose();
            m_RendererPriorities.Dispose();
            m_GPUHandles.Dispose();
            m_LocalToWorldIsFlippedBits.Dispose();
            m_WorldAABBs.Dispose();
            m_TetrahedronCacheIndices.Dispose();
            m_MovedInCurrentFrameBits.Dispose();
            m_MovedInPreviousFrameBits.Dispose();
            m_VisibleInPreviousFrameBits.Dispose();
            m_RenderingEnabled.Dispose();
            m_EditorOnly.Dispose();

            foreach (var kv in m_PerCameraInstanceDataMap)
                kv.Value.Dispose();
            m_PerCameraInstanceDataMap.Dispose();
        }

        private void Grow(int newCapacity)
        {
            int instancesCapacity = GetInstanceCapacity();
            Assert.IsTrue(newCapacity > instancesCapacity);

            m_IndexToHandle.ResizeArray(newCapacity);
            m_IndexToHandle.FillArray(InstanceHandle.Invalid, instancesCapacity);
            m_InstanceIDs.ResizeArray(newCapacity);
            m_MaterialIDArrays.ResizeArray(newCapacity);
            m_MaterialIDArrays.FillArray(default, instancesCapacity);
            m_MeshIDs.ResizeArray(newCapacity);
            m_MeshLodRendererSettings.ResizeArray(newCapacity);
            m_SubMeshStartIndices.ResizeArray(newCapacity);
            m_LocalAABBs.ResizeArray(newCapacity);
            m_RendererSettings.ResizeArray(newCapacity);
            m_LightmapIndices.ResizeArray(newCapacity);
            m_LODGroupIndices.ResizeArray(newCapacity);
            m_LODGroupIndices.FillArray(GPUInstanceIndex.Invalid, instancesCapacity);
            m_LODMasks.ResizeArray(newCapacity);
            m_LODMasks.FillArray(DefaultLODMask, instancesCapacity);
            m_RendererPriorities.ResizeArray(newCapacity);
            m_GPUHandles.ResizeArray(newCapacity);
            m_GPUHandles.FillArray(InstanceGPUHandle.Invalid, instancesCapacity);
            m_LocalToWorldIsFlippedBits.Resize(newCapacity);
            m_WorldAABBs.ResizeArray(newCapacity);
            m_TetrahedronCacheIndices.ResizeArray(newCapacity);
            m_TetrahedronCacheIndices.FillArray(InvalidIndex, instancesCapacity);
            m_MovedInCurrentFrameBits.Resize(newCapacity);
            m_MovedInPreviousFrameBits.Resize(newCapacity);
            m_VisibleInPreviousFrameBits.Resize(newCapacity);
            m_RenderingEnabled.Resize(newCapacity);
            m_EditorOnly.Grow(newCapacity);

            foreach (var kv in m_PerCameraInstanceDataMap)
                kv.Value.Resize(newCapacity);
        }

        private int AddUninitialized(InstanceHandle instance)
        {
            if (instance.index >= m_HandleToIndex.Length)
            {
                int prevLength = m_HandleToIndex.Length;
                m_HandleToIndex.ResizeUninitialized(instance.index + 1);

                for (int i = prevLength; i < m_HandleToIndex.Length - 1; ++i)
                    m_HandleToIndex[i] = InvalidIndex;
            }

            m_HandleToIndex[instance.index] = instanceCount;
            m_IndexToHandle[instanceCount] = instance;

            return instanceCount++;
        }

        public int HandleToIndex(InstanceHandle instance)
        {
            Assert.IsTrue(IsValidInstance(instance));
            return m_HandleToIndex[instance.index];
        }

        public InstanceHandle IndexToHanle(int index)
        {
            Assert.IsTrue(IsValidIndex(index));
            return m_IndexToHandle[index];
        }

        public bool IsValidInstance(InstanceHandle instance)
        {
            if (instance.isValid && instance.index < m_HandleToIndex.Length)
            {
                int index = m_HandleToIndex[instance.index];
                return index >= 0 && index < instanceCount && m_IndexToHandle[index].Equals(instance);
            }

            return false;
        }

        public bool IsValidIndex(int index)
        {
            if (index >= 0 && index < instanceCount)
            {
                InstanceHandle instance = m_IndexToHandle[index];
                return index == m_HandleToIndex[instance.index];
            }

            return false;
        }

        public bool IsFreeInstanceHandle(InstanceHandle instance)
        {
            return instance.isValid && (instance.index >= m_HandleToIndex.Length || m_HandleToIndex[instance.index] == InvalidIndex);
        }

        public int GetInstanceCapacity()
        {
            return m_IndexToHandle.Length;
        }

        public int GetFreeCapacity()
        {
            return GetInstanceCapacity() - instanceCount;
        }

        public void EnsureFreeCapacity(int instancesCount)
        {
            if (instancesCount == 0)
                return;

            int freeInstancesCount = GetFreeCapacity();
            int needInstances = instancesCount - freeInstancesCount;

            if (needInstances > 0)
                Grow(GetInstanceCapacity() + needInstances + 256);
        }

        public int AddInstanceNoGrow(InstanceHandle instance)
        {
            Assert.IsTrue(instance.isValid);
            Assert.IsTrue(IsFreeInstanceHandle(instance));
            Assert.IsTrue(GetFreeCapacity() > 0);

            int instanceIndex = AddUninitialized(instance);
            InitializeInstance(instanceIndex);
            return instanceIndex;
        }

        public void RemoveInstance(InstanceHandle instance)
        {
            Assert.IsTrue(IsValidInstance(instance));

            int index = HandleToIndex(instance);
            int lastIndex = instanceCount - 1;

            if (m_RendererSettings[index].HasTree)
            {
                Assert.IsTrue(m_TotalTreeCount.Value > 0);
                --m_TotalTreeCount.Value;
            }

            m_IndexToHandle[index] = m_IndexToHandle[lastIndex];
            m_InstanceIDs[index] = m_InstanceIDs[lastIndex];

            m_MaterialIDArrays[index].Dispose();
            m_MaterialIDArrays[index] = m_MaterialIDArrays[lastIndex];
            m_MaterialIDArrays[lastIndex] = default;

            m_MeshIDs[index] = m_MeshIDs[lastIndex];
            m_MeshLodRendererSettings[index] = m_MeshLodRendererSettings[lastIndex];
            m_SubMeshStartIndices[index] = m_SubMeshStartIndices[lastIndex];
            m_LocalAABBs[index] = m_LocalAABBs[lastIndex];
            m_RendererSettings[index] = m_RendererSettings[lastIndex];
            m_LightmapIndices[index] = m_LightmapIndices[lastIndex];
            m_LODGroupIndices[index] = m_LODGroupIndices[lastIndex];
            m_LODMasks[index] = m_LODMasks[lastIndex];
            m_RendererPriorities[index] = m_RendererPriorities[lastIndex];
            m_GPUHandles[index] = m_GPUHandles[lastIndex];
            m_LocalToWorldIsFlippedBits.Set(index, m_LocalToWorldIsFlippedBits.Get(lastIndex));
            m_WorldAABBs[index] = m_WorldAABBs[lastIndex];
            m_TetrahedronCacheIndices[index] = m_TetrahedronCacheIndices[lastIndex];
            m_MovedInCurrentFrameBits.Set(index, m_MovedInCurrentFrameBits.Get(lastIndex));
            m_MovedInPreviousFrameBits.Set(index, m_MovedInPreviousFrameBits.Get(lastIndex));
            m_VisibleInPreviousFrameBits.Set(index, m_VisibleInPreviousFrameBits.Get(lastIndex));
            m_RenderingEnabled.Set(index, m_RenderingEnabled.Get(lastIndex));
            m_EditorOnly.Remove(index, lastIndex);

            foreach (var kv in m_PerCameraInstanceDataMap)
                kv.Value.Remove(index, lastIndex);

            m_HandleToIndex[m_IndexToHandle[lastIndex].index] = index;
            m_HandleToIndex[instance.index] = InvalidIndex;
            instanceCount -= 1;
        }

        public void ResetInstance(int instanceIndex)
        {
            m_MaterialIDArrays[instanceIndex].Dispose();
            m_MaterialIDArrays[instanceIndex] = default;
            m_MeshIDs[instanceIndex] = DefaultMesh;
            m_MeshLodRendererSettings[instanceIndex] = DefaultMeshLodRendererSettings;
            m_SubMeshStartIndices[instanceIndex] = DefaultSubMeshStartIndex;
            m_LocalAABBs[instanceIndex] = DefaultLocalBounds;
            m_RendererSettings[instanceIndex] = DefaultRendererSettings;
            m_LightmapIndices[instanceIndex] = DefaultLightmapIndex;
            m_LODGroupIndices[instanceIndex] = GPUInstanceIndex.Invalid;
            m_LODMasks[instanceIndex] = DefaultLODMask;
            m_RendererPriorities[instanceIndex] = DefaultRendererPriority;
            m_LocalToWorldIsFlippedBits.Set(instanceIndex, false);
            m_WorldAABBs[instanceIndex] = default;
            m_TetrahedronCacheIndices[instanceIndex] = InvalidIndex;
            m_RenderingEnabled.Set(instanceIndex, true);
            m_EditorOnly.SetDefault(instanceIndex);

            foreach (var kv in m_PerCameraInstanceDataMap)
            {
                kv.Value.meshLods[instanceIndex] = PerCameraInstanceData.InvalidByteData;
                kv.Value.crossFades[instanceIndex] = PerCameraInstanceData.InvalidByteData;
            }
        }

        private void InitializeInstance(int instanceIndex)
        {
            ResetInstance(instanceIndex);
            m_InstanceIDs[instanceIndex] = EntityId.None;
            m_GPUHandles[instanceIndex] = InstanceGPUHandle.Invalid;
            m_MovedInCurrentFrameBits.Set(instanceIndex, false);
            m_MovedInPreviousFrameBits.Set(instanceIndex, false);
            m_VisibleInPreviousFrameBits.Set(instanceIndex, false);
        }

        public void AddCameras(NativeArray<EntityId> cameraIDs)
        {
            foreach (var cameraID in cameraIDs)
            {
                if (m_PerCameraInstanceDataMap.ContainsKey(cameraID))
                    continue;

                m_PerCameraInstanceDataMap.Add(cameraID, new UnsafePerCameraInstanceData(GetInstanceCapacity(), Allocator.Persistent));
            }
        }

        public void RemoveCameras(NativeArray<EntityId> cameraIDs)
        {
            foreach (var cameraID in cameraIDs)
            {
                if (!m_PerCameraInstanceDataMap.ContainsKey(cameraID))
                    continue;

                m_PerCameraInstanceDataMap[cameraID].Dispose();
                m_PerCameraInstanceDataMap.Remove(cameraID);
            }
        }

        public bool TryGetPerCameraInstanceData(EntityId cameraID, out PerCameraInstanceData perCameraInstanceData)
        {
            perCameraInstanceData = default;

            if (!m_PerCameraInstanceDataMap.TryGetValue(cameraID, out UnsafePerCameraInstanceData unsafePerCameraData))
                return false;

            perCameraInstanceData = unsafePerCameraData.ToPerCameraInstanceData(instanceCount);
            return true;
        }

        public PerCameraInstanceData GetPerCameraInstanceData(EntityId cameraID)
        {
            return m_PerCameraInstanceDataMap[cameraID].ToPerCameraInstanceData(instanceCount);
        }

        public struct PerCameraInstanceData
        {
            public const int InvalidByteData = 0xff;

            public NativeArray<byte> meshLods;
            public NativeArray<byte> crossFades;

            public bool IsCreated => meshLods.IsCreated && crossFades.IsCreated;

            public PerCameraInstanceData(int length, Allocator allocator)
            {
                meshLods = new NativeArray<byte>(length, allocator);
                crossFades = new NativeArray<byte>(length, allocator);
            }

            public void Dispose(JobHandle jobHandle)
            {
                meshLods.Dispose(jobHandle);
                crossFades.Dispose(jobHandle);
            }
        }

        struct UnsafePerCameraInstanceData : IDisposable
        {
            public UnsafeList<byte> meshLods;
            public UnsafeList<byte> crossFades;

            public UnsafePerCameraInstanceData(int initCapacity, Allocator allocator)
            {
                meshLods = new UnsafeList<byte>(initCapacity, allocator, NativeArrayOptions.UninitializedMemory);
                meshLods.AddReplicate(PerCameraInstanceData.InvalidByteData, initCapacity);
                crossFades = new UnsafeList<byte>(initCapacity, allocator, NativeArrayOptions.UninitializedMemory);
                crossFades.AddReplicate(PerCameraInstanceData.InvalidByteData, initCapacity);
            }

            public void Dispose()
            {
                meshLods.Dispose();
                crossFades.Dispose();
            }

            public void Remove(int index, int lastIndex)
            {
                meshLods[index] = meshLods[lastIndex];
                crossFades[index] = crossFades[lastIndex];
            }

            public void Resize(int newCapacity)
            {
                meshLods.Resize(newCapacity);
                crossFades.Resize(newCapacity);
            }

            public PerCameraInstanceData ToPerCameraInstanceData(int instanceCount)
            {
                return new PerCameraInstanceData
                {
                    meshLods = meshLods.AsNativeArray().GetSubArray(0, instanceCount),
                    crossFades = crossFades.AsNativeArray().GetSubArray(0, instanceCount)
                };
            }
        }

        struct EditorOnly
        {
#if UNITY_EDITOR
            public NativeArray<ulong> sceneCullingMasks;

            public void Initialize(int initCapacity)
            {
                sceneCullingMasks = new NativeArray<ulong>(initCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                sceneCullingMasks.FillArray(ulong.MaxValue);
            }

            public void Dispose()
            {
                sceneCullingMasks.Dispose();
            }

            public void Grow(int newCapacity)
            {
                sceneCullingMasks.ResizeArray(newCapacity);
            }

            public void Remove(int index, int lastIndex)
            {
                sceneCullingMasks[index] = sceneCullingMasks[lastIndex];
            }

            public void SetDefault(int index)
            {
                sceneCullingMasks[index] = DefaultSceneCullingMask;
            }
#else
            public void Initialize(int initCapacity) { }
            public void Dispose() { }
            public void Grow(int newCapacity) { }
            public void Remove(int index, int lastIndex) { }
            public void SetDefault(int index) { }
#endif
        }
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

        public override string ToString()
        {
            return $"[{packed0}, {packed1}, {packed2}]";
        }
    }
}
