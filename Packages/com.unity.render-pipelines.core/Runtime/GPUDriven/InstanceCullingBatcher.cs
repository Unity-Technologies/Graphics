using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using UnityEngine.Profiling;

[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.RegisterNewInstancesJob<UnityEngine.Rendering.BatchMeshID>))]
[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.RegisterNewInstancesJob<UnityEngine.Rendering.BatchMaterialID>))]
[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.FindNonRegisteredInstancesJob<UnityEngine.Rendering.BatchMeshID>))]
[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.FindNonRegisteredInstancesJob<UnityEngine.Rendering.BatchMaterialID>))]

namespace UnityEngine.Rendering
{
    internal delegate void OnCullingCompleteCallback(JobHandle jobHandle, in BatchCullingContext cullingContext, in BatchCullingOutput cullingOutput);

    internal struct InstanceCullingBatcherDesc
    {
        public OnCullingCompleteCallback onCompleteCallback;

        public static InstanceCullingBatcherDesc NewDefault()
        {
            return new InstanceCullingBatcherDesc()
            {
                onCompleteCallback = null
            };
        }
    }

    internal struct MeshProceduralKey : IEquatable<MeshProceduralKey>
    {
        public BatchMeshID meshID;
        public int submeshIndex;

        public bool Equals(MeshProceduralKey other)
        {
            return meshID == other.meshID && submeshIndex == other.submeshIndex;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 23) + (int)meshID.value;
            hash = (hash * 23) + (int)submeshIndex;
            return hash;
        }
    }

    internal struct MeshProceduralInfo
    {
        public MeshTopology topology;
        public uint baseVertex;
        public uint firstIndex;
        public uint indexCount;
    }

    [BurstCompile(DisableSafetyChecks = true)]
    internal struct PrefixSumDrawInstancesJob : IJob
    {
        [ReadOnly] public NativeParallelHashMap<RangeKey, int> rangeHash;

        public NativeList<DrawRange> drawRanges;
        public NativeList<DrawBatch> drawBatches;
        public NativeArray<int> drawBatchIndices;

        public void Execute()
        {
            Assert.AreEqual(rangeHash.Count(), drawRanges.Length);
            Assert.AreEqual(drawBatchIndices.Length, drawBatches.Length);

            // Prefix sum to calculate draw offsets for each DrawRange
            int drawPrefixSum = 0;

            for (int i = 0; i < drawRanges.Length; ++i)
            {
                ref DrawRange drawRange = ref drawRanges.ElementAt(i);
                drawRange.drawOffset = drawPrefixSum;
                drawPrefixSum += drawRange.drawCount;
            }

            // Generate DrawBatch index ranges for each DrawRange
            var internalRangeIndex = new NativeArray<int>(drawRanges.Length, Allocator.Temp);

            for (int i = 0; i < drawBatches.Length; ++i)
            {
                ref DrawBatch drawBatch = ref drawBatches.ElementAt(i);
                Assert.IsTrue(drawBatch.instanceCount > 0);

                if (rangeHash.TryGetValue(drawBatch.key.range, out int drawRangeIndex))
                {
                    ref DrawRange drawRange = ref drawRanges.ElementAt(drawRangeIndex);
                    drawBatchIndices[drawRange.drawOffset + internalRangeIndex[drawRangeIndex]] = i;
                    internalRangeIndex[drawRangeIndex]++;
                }
            }

            // Prefix sum to calculate instance offsets for each DrawCommand
            int drawInstancesPrefixSum = 0;

            for (int i = 0; i < drawBatchIndices.Length; ++i)
            {
                // DrawIndices remap to get DrawCommands ordered by DrawRange
                var drawBatchIndex = drawBatchIndices[i];
                ref DrawBatch drawBatch = ref drawBatches.ElementAt(drawBatchIndex);
                drawBatch.instanceOffset = drawInstancesPrefixSum;
                drawInstancesPrefixSum += drawBatch.instanceCount;
            }

            internalRangeIndex.Dispose();
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    internal unsafe struct BuildDrawListsJob : IJobParallelFor
    {
        public const int k_BatchSize = 128;
        public const int k_IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);

        [ReadOnly] public NativeParallelHashMap<DrawKey, int> batchHash;
        [NativeDisableContainerSafetyRestriction] [ReadOnly] public NativeList<DrawInstance> drawInstances;
        [NativeDisableContainerSafetyRestriction] [ReadOnly] public NativeList<DrawBatch> drawBatches;

        [NativeDisableContainerSafetyRestriction] [WriteOnly] public NativeArray<int> internalDrawIndex;
        [NativeDisableContainerSafetyRestriction] [WriteOnly] public NativeArray<int> drawInstanceIndices;

        private unsafe static int IncrementCounter(int* counter)
        {
            return Interlocked.Increment(ref UnsafeUtility.AsRef<int>(counter)) - 1;
        }

        public void Execute(int index)
        {
            // Generate instance index ranges for each DrawCommand
            ref DrawInstance drawInstance = ref drawInstances.ElementAt(index);
            int drawBatchIndex = batchHash[drawInstance.key];

            ref DrawBatch drawBatch = ref drawBatches.ElementAt(drawBatchIndex);
            var offset = IncrementCounter((int*)internalDrawIndex.GetUnsafePtr() + drawBatchIndex * k_IntsPerCacheLine);
            var writeIndex = drawBatch.instanceOffset + offset;
            drawInstanceIndices[writeIndex] = drawInstance.instanceIndex;
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    internal unsafe struct FindDrawInstancesJob : IJobParallelFor
    {
        public const int k_BatchSize = 128;

        [ReadOnly] public NativeArray<InstanceHandle> instancesSorted;
        [NativeDisableContainerSafetyRestriction] [ReadOnly] public NativeList<DrawInstance> drawInstances;

        [WriteOnly] public NativeList<int>.ParallelWriter outDrawInstanceIndicesWriter;

        public void Execute(int index)
        {
            int* instancesToRemove = stackalloc int[k_BatchSize];
            int length = 0;

            int start = index * k_BatchSize;
            int end = math.min(start + k_BatchSize, drawInstances.Length);

            for (int i = start; i < end; ++i)
            {
                ref DrawInstance drawInstance = ref drawInstances.ElementAt(i);

                if (instancesSorted.BinarySearch(new InstanceHandle { index = drawInstance.instanceIndex }) >= 0)
                    instancesToRemove[length++] = i;
            }

            outDrawInstanceIndicesWriter.AddRangeNoResize(instancesToRemove, length);
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    internal unsafe struct FindMaterialDrawInstancesJob : IJobParallelFor
    {
        public const int k_BatchSize = 128;

        [ReadOnly] public NativeArray<uint> materialsSorted;
        [NativeDisableContainerSafetyRestriction] [ReadOnly] public NativeList<DrawInstance> drawInstances;

        [WriteOnly] public NativeList<int>.ParallelWriter outDrawInstanceIndicesWriter;

        public void Execute(int index)
        {
            int* instancesToRemove = stackalloc int[k_BatchSize];
            int length = 0;

            int start = index * k_BatchSize;
            int end = math.min(start + k_BatchSize, drawInstances.Length);

            for (int i = start; i < end; ++i)
            {
                ref DrawInstance drawInstance = ref drawInstances.ElementAt(i);

                if (materialsSorted.BinarySearch(drawInstance.key.materialID.value) >= 0)
                    instancesToRemove[length++] = i;
            }

            outDrawInstanceIndicesWriter.AddRangeNoResize(instancesToRemove, length);
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    internal struct FindNonRegisteredInstancesJob<T> : IJobParallelFor where T : unmanaged
    {
        public const int k_BatchSize = 128;

        [ReadOnly] public NativeArray<int> instanceIDs;
        [ReadOnly] public NativeParallelHashMap<int, T> hashMap;

        [WriteOnly] public NativeList<int>.ParallelWriter outInstancesWriter;

        public unsafe void Execute(int index)
        {
            int* notFoundinstanceIDs = stackalloc int[k_BatchSize];
            int length = 0;

            int start = index * k_BatchSize;
            int end = math.min(start + k_BatchSize, instanceIDs.Length);

            for (int i = start; i < end; ++i)
            {
                int instanceID = instanceIDs[i];

                if (!hashMap.ContainsKey(instanceID))
                    notFoundinstanceIDs[length++] = instanceID;
            }

            outInstancesWriter.AddRangeNoResize(notFoundinstanceIDs, length);
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    internal struct RegisterNewInstancesJob<T> : IJobParallelFor where T : unmanaged
    {
        public const int k_BatchSize = 128;

        [ReadOnly] public NativeArray<int> instanceIDs;
        [ReadOnly] public NativeArray<T> batchIDs;

        [WriteOnly] public NativeParallelHashMap<int, T>.ParallelWriter hashMap;

        public unsafe void Execute(int index)
        {
            hashMap.TryAdd(instanceIDs[index], batchIDs[index]);
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    internal struct RemoveDrawInstanceIndicesJob : IJob
    {
        [NativeDisableContainerSafetyRestriction] [ReadOnly] public NativeArray<int> drawInstanceIndices;

        public NativeList<DrawInstance> drawInstances;
        public NativeParallelHashMap<RangeKey, int> rangeHash;
        public NativeParallelHashMap<DrawKey, int> batchHash;
        public NativeList<DrawRange> drawRanges;
        public NativeList<DrawBatch> drawBatches;

        [BurstCompile]
        public void RemoveDrawRange(in RangeKey key)
        {
            int drawRangeIndex = rangeHash[key];

            ref DrawRange lastDrawRange = ref drawRanges.ElementAt(drawRanges.Length - 1);
            rangeHash[lastDrawRange.key] = drawRangeIndex;

            rangeHash.Remove(key);
            drawRanges.RemoveAtSwapBack(drawRangeIndex);
        }

        [BurstCompile]
        public void RemoveDrawBatch(in DrawKey key)
        {
            int drawBatchIndex = batchHash[key];

            ref DrawBatch drawBatch = ref drawBatches.ElementAt(drawBatchIndex);

            int drawRangeIndex = rangeHash[key.range];
            ref DrawRange drawRange = ref drawRanges.ElementAt(drawRangeIndex);

            Assert.IsTrue(drawRange.drawCount > 0);

            if (--drawRange.drawCount == 0)
                RemoveDrawRange(drawRange.key);

            ref DrawBatch lastDrawBatch = ref drawBatches.ElementAt(drawBatches.Length - 1);
            batchHash[lastDrawBatch.key] = drawBatchIndex;

            batchHash.Remove(key);
            drawBatches.RemoveAtSwapBack(drawBatchIndex);
        }

        public unsafe void Execute()
        {
            var drawInstancesPtr = (DrawInstance*)drawInstances.GetUnsafePtr();
            var drawInstancesNewBack = drawInstances.Length - 1;

            for (int indexRev = drawInstanceIndices.Length - 1; indexRev >= 0; --indexRev)
            {
                int indexToRemove = drawInstanceIndices[indexRev];
                DrawInstance* drawInstance = drawInstancesPtr + indexToRemove;

                int drawBatchIndex = batchHash[drawInstance->key];
                ref DrawBatch drawBatch = ref drawBatches.ElementAt(drawBatchIndex);

                Assert.IsTrue(drawBatch.instanceCount > 0);

                if (--drawBatch.instanceCount == 0)
                    RemoveDrawBatch(drawBatch.key);

                UnsafeUtility.MemCpy(drawInstance, drawInstancesPtr + drawInstancesNewBack--, sizeof(DrawInstance));
            }

            drawInstances.ResizeUninitialized(drawInstancesNewBack + 1);
        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    internal struct CreateDrawBatchesJob : IJob
    {
        [ReadOnly] public NativeArray<InstanceHandle> instances;
        [ReadOnly] public GPUDrivenRendererData rendererData;
        [ReadOnly] public NativeParallelHashMap<LightmapManager.RendererSubmeshPair, int> rendererToMaterialMap;
        [ReadOnly] public NativeParallelHashMap<int, BatchMeshID> batchMeshHash;
        [ReadOnly] public NativeParallelHashMap<int, BatchMaterialID> batchMaterialHash;

        public NativeParallelHashMap<RangeKey, int> rangeHash;
        public NativeList<DrawRange> drawRanges;
        public NativeParallelHashMap<DrawKey, int> batchHash;
        public NativeList<DrawBatch> drawBatches;
        public NativeParallelHashMap<MeshProceduralKey, MeshProceduralInfo> meshProceduralInfoHash;

        [WriteOnly] public NativeList<DrawInstance> drawInstances;

        [BurstCompile]
        private ref DrawRange EditDrawRange(in RangeKey key)
        {
            int drawRangeIndex;

            if (!rangeHash.TryGetValue(key, out drawRangeIndex))
            {
                var drawRange = new DrawRange { key = key, drawCount = 0, drawOffset = 0 };
                drawRangeIndex = drawRanges.Length;
                rangeHash.Add(key, drawRangeIndex);
                drawRanges.Add(drawRange);
            }

            ref DrawRange data = ref drawRanges.ElementAt(drawRangeIndex);
            Assert.IsTrue(data.key.Equals(key));

            return ref data;
        }

        [BurstCompile]
        private ref DrawBatch EditDrawBatch(in DrawKey key, in SubMeshDescriptor subMeshDescriptor)
        {
            var procKey = new MeshProceduralKey()
            {
                meshID = key.meshID,
                submeshIndex = key.submeshIndex,
            };

            var procInfo = new MeshProceduralInfo();

            if (!meshProceduralInfoHash.TryGetValue(procKey, out procInfo))
            {
                procInfo.topology = subMeshDescriptor.topology;
                procInfo.baseVertex = (uint)subMeshDescriptor.baseVertex;
                procInfo.firstIndex = (uint)subMeshDescriptor.indexStart;
                procInfo.indexCount = (uint)subMeshDescriptor.indexCount;
                meshProceduralInfoHash.Add(procKey, procInfo);
            }

            int drawBatchIndex;

            if (!batchHash.TryGetValue(key, out drawBatchIndex))
            {
                var drawBatch = new DrawBatch() { key = key, instanceCount = 0, instanceOffset = 0, procInfo = procInfo };
                drawBatchIndex = drawBatches.Length;
                batchHash.Add(key, drawBatchIndex);
                drawBatches.Add(drawBatch);
            }

            ref DrawBatch data = ref drawBatches.ElementAt(drawBatchIndex);
            Assert.IsTrue(data.key.Equals(key));

            return ref data;
        }

        public void Execute()
        {
            for (int i = 0; i < rendererData.rendererID.Length; ++i)
            {
                var instance = instances[i];
                var meshIndex = rendererData.meshIndex[i];
                var meshID = rendererData.meshID[meshIndex];
                var submeshCount = rendererData.subMeshCount[meshIndex];
                var subMeshDescOffset = rendererData.subMeshDescOffset[meshIndex];
                var batchMeshID = batchMeshHash[meshID];
                var rendererID = rendererData.rendererID[i];
                var startSubMesh = rendererData.subMeshStartIndex[i];
                var gameObjectLayer = rendererData.gameObjectLayer[i];
                var renderingLayerMask = rendererData.renderingLayerMask[i];
                var materialsOffset = rendererData.materialsOffset[i];
                var materialsCount = rendererData.materialsCount[i];
                var lightmapIndex = rendererData.lightmapIndex[i];
                var packedRendererData = rendererData.packedRendererData[i];
                var rendererPriority = rendererData.rendererPriority[i];

                const int kLightmapIndexMask = 0xffff;
                const int kLightmapIndexInfluenceOnly = 0xfffe;

                var overridenComponents = RenderersParameters.DefaultComponents;
                var lmIndexMasked = lightmapIndex & kLightmapIndexMask;

                // Object doesn't have a valid lightmap Index, -> uses probes for lighting
                if (lmIndexMasked >= kLightmapIndexInfluenceOnly)
                {
                    // Only add the component when needed to store blended results (shader will use the ambient probe when not present)
                    if (packedRendererData.lightProbeUsage == LightProbeUsage.BlendProbes)
                        overridenComponents = RenderersParameters.DefaultAndProbesComponents;
                }
                else
                {
                    // Add per-instance lightmap parameters
                    overridenComponents = RenderersParameters.DefaultAndLightmapComponents;
                }

                var rangeKey = new RangeKey
                {
                    layer = (byte)gameObjectLayer,
                    renderingLayerMask = renderingLayerMask,
                    motionMode = packedRendererData.motionVecGenMode,
                    shadowCastingMode = packedRendererData.shadowCastingMode,
                    staticShadowCaster = packedRendererData.staticShadowCaster,
                    rendererPriority = rendererPriority,
                };

                ref DrawRange drawRange = ref EditDrawRange(rangeKey);

                for (int matIndex = 0; matIndex < materialsCount; ++matIndex)
                {
                    if (matIndex >= submeshCount)
                    {
                        Debug.LogWarning("Material count in the shared material list is higher than sub mesh count for the mesh. Object may be corrupted.");
                        continue;
                    }

                    var submeshIndex = startSubMesh + matIndex;
                    var materialIndex = rendererData.materialIndex[materialsOffset + matIndex];
                    var materialID = rendererData.materialID[materialIndex];
                    var isTransparent = rendererData.isTransparent[materialIndex];
                    var isMotionVectorsPassEnabled = rendererData.isMotionVectorsPassEnabled[materialIndex];
                    var subMeshDesc = rendererData.subMeshDesc[subMeshDescOffset + submeshIndex];

                    if (rendererToMaterialMap.TryGetValue(new LightmapManager.RendererSubmeshPair(rendererID, matIndex), out var cachedMaterialID))
                        materialID = cachedMaterialID;

                    if (materialID == 0)
                    {
                        Debug.LogWarning("Material in the shared materials list is null. Object will be partially rendered.");
                        continue;
                    }

                    batchMaterialHash.TryGetValue(materialID, out BatchMaterialID batchMaterialID);

                    var flags = BatchDrawCommandFlags.None;

                    // assume that a custom motion vectors pass contains deformation motion, so should always output motion vectors
                    // (otherwise this flag is set dynamically during culling only when the transform is changing)
                    if (isMotionVectorsPassEnabled)
                        flags |= BatchDrawCommandFlags.HasMotion;

                    if (isTransparent)
                        flags |= BatchDrawCommandFlags.HasSortingPosition;

                    var drawKey = new DrawKey
                    {
                        materialID = batchMaterialID,
                        meshID = batchMeshID,
                        submeshIndex = submeshIndex,
                        flags = flags,
                        transparentInstanceId = isTransparent ? rendererID : 0,
                        range = rangeKey,
                        overridenComponents = (int)overridenComponents
                    };

                    ref DrawBatch drawBatch = ref EditDrawBatch(drawKey, subMeshDesc);

                    if (drawBatch.instanceCount == 0)
                        ++drawRange.drawCount;

                    ++drawBatch.instanceCount;

                    drawInstances.Add(new DrawInstance { key = drawKey, instanceIndex = instance.index });
                }
            }
        }
    }

    [BurstCompile]
    internal class CPUInstanceData
    {
        public NativeList<DrawInstance> drawInstances => m_DrawInstances;
        public NativeParallelHashMap<DrawKey, int> batchHash => m_BatchHash;
        public NativeList<DrawBatch> drawBatches => m_DrawBatches;
        public NativeParallelHashMap<RangeKey, int> rangeHash => m_RangeHash;
        public NativeList<DrawRange> drawRanges => m_DrawRanges;
        public NativeArray<int> drawBatchIndices => m_DrawBatchIndices.AsArray();
        public NativeArray<int> drawInstanceIndices => m_DrawInstanceIndices.AsArray();
        public NativeParallelHashMap<MeshProceduralKey, MeshProceduralInfo> meshProceduralInfoHash => m_MeshProceduralInfoHash;

        private NativeParallelHashMap<RangeKey, int> m_RangeHash;       // index in m_DrawRanges, hashes by range state
        private NativeList<DrawRange> m_DrawRanges;
        private NativeParallelHashMap<DrawKey, int> m_BatchHash;        // index in m_DrawBatches, hashed by draw state
        private NativeList<DrawBatch> m_DrawBatches;
        private NativeList<DrawInstance> m_DrawInstances;
        private NativeList<int> m_DrawInstanceIndices;          // DOTS instance index, arranged in contiguous blocks in m_DrawBatches order (see DrawBatch.instanceOffset, DrawBatch.instanceCount)
        private NativeList<int> m_DrawBatchIndices;             // index in m_DrawBatches, arranged in contiguous blocks in m_DrawRanges order (see DrawRange.drawOffset, DrawRange.drawCount)
        private NativeParallelHashMap<MeshProceduralKey, MeshProceduralInfo> m_MeshProceduralInfoHash;

        private bool m_NeedsRebuild;

        public bool valid => m_DrawInstances.IsCreated;

        public void Initialize()
        {
            Assert.IsTrue(!valid);
            m_RangeHash = new NativeParallelHashMap<RangeKey, int>(1024, Allocator.Persistent);
            m_DrawRanges = new NativeList<DrawRange>(Allocator.Persistent);
            m_BatchHash = new NativeParallelHashMap<DrawKey, int>(1024, Allocator.Persistent);
            m_DrawBatches = new NativeList<DrawBatch>(Allocator.Persistent);
            m_DrawInstances = new NativeList<DrawInstance>(1024, Allocator.Persistent);
            m_DrawInstanceIndices = new NativeList<int>(1024, Allocator.Persistent);
            m_DrawBatchIndices = new NativeList<int>(1024, Allocator.Persistent);
            m_MeshProceduralInfoHash = new NativeParallelHashMap<MeshProceduralKey, MeshProceduralInfo>(64, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (m_MeshProceduralInfoHash.IsCreated)
                m_MeshProceduralInfoHash.Dispose();

            if (m_DrawBatchIndices.IsCreated)
                m_DrawBatchIndices.Dispose();

            if (m_DrawInstanceIndices.IsCreated)
                m_DrawInstanceIndices.Dispose();

            if (m_DrawInstances.IsCreated)
                m_DrawInstances.Dispose();

            if (m_DrawBatches.IsCreated)
                m_DrawBatches.Dispose();

            if (m_BatchHash.IsCreated)
                m_BatchHash.Dispose();

            if (m_DrawRanges.IsCreated)
                m_DrawRanges.Dispose();

            if (m_RangeHash.IsCreated)
                m_RangeHash.Dispose();
        }

        public void RebuildDrawListsIfNeeded()
        {
            if (!m_NeedsRebuild)
                return;

            m_NeedsRebuild = false;

            Assert.IsTrue(m_RangeHash.Count() == m_DrawRanges.Length);
            Assert.IsTrue(m_BatchHash.Count() == m_DrawBatches.Length);

            m_DrawInstanceIndices.ResizeUninitialized(m_DrawInstances.Length);
            m_DrawBatchIndices.ResizeUninitialized(m_DrawBatches.Length);

            var internalDrawIndex = new NativeArray<int>(drawBatches.Length * BuildDrawListsJob.k_IntsPerCacheLine, Allocator.TempJob, NativeArrayOptions.ClearMemory);

            var prefixSumDrawInstancesJob = new PrefixSumDrawInstancesJob()
            {
                rangeHash = m_RangeHash,
                drawRanges = m_DrawRanges,
                drawBatches = m_DrawBatches,
                drawBatchIndices = m_DrawBatchIndices.AsArray()
            };

            var prefixSumJobHandle = prefixSumDrawInstancesJob.Schedule();

            var buildDrawListsJob = new BuildDrawListsJob()
            {
                drawInstances = m_DrawInstances,
                batchHash = m_BatchHash,
                drawBatches = m_DrawBatches,
                internalDrawIndex = internalDrawIndex,
                drawInstanceIndices = m_DrawInstanceIndices.AsArray(),
            };

            buildDrawListsJob.Schedule(m_DrawInstances.Length, BuildDrawListsJob.k_BatchSize, prefixSumJobHandle).Complete();

            internalDrawIndex.Dispose();
        }

        public unsafe void DestroyDrawInstanceIndices(NativeArray<int> drawInstanceIndicesToDestroy)
        {
            Profiler.BeginSample("DestroyDrawInstanceIndices.ParallelSort");
            drawInstanceIndicesToDestroy.ParallelSort().Complete();
            Profiler.EndSample();

            var removeDrawInstanceIndicesJob = new RemoveDrawInstanceIndicesJob
            {
                drawInstanceIndices = drawInstanceIndicesToDestroy,
                drawInstances = m_DrawInstances,
                drawBatches = m_DrawBatches,
                drawRanges = m_DrawRanges,
                batchHash = m_BatchHash,
                rangeHash = m_RangeHash
            };

            removeDrawInstanceIndicesJob.Run();
        }

        public unsafe void DestroyDrawInstances(NativeArray<InstanceHandle> destroyedInstances)
        {
            if (m_DrawInstances.IsEmpty || destroyedInstances.Length == 0)
                return;

            NeedsRebuild();

            var destroyedInstancesSorted = new NativeArray<InstanceHandle>(destroyedInstances, Allocator.TempJob);
            Assert.AreEqual(UnsafeUtility.SizeOf<InstanceHandle>(), UnsafeUtility.SizeOf<int>());

            Profiler.BeginSample("DestroyDrawInstances.ParallelSort");
            destroyedInstancesSorted.Reinterpret<int>().ParallelSort().Complete();
            Profiler.EndSample();

            var drawInstanceIndicesToDestroy = new NativeList<int>(m_DrawInstances.Length, Allocator.TempJob);

            var findDrawInstancesJobHandle = new FindDrawInstancesJob()
            {
                instancesSorted = destroyedInstancesSorted,
                drawInstances = m_DrawInstances,
                outDrawInstanceIndicesWriter = drawInstanceIndicesToDestroy.AsParallelWriter()
            };

            findDrawInstancesJobHandle.Schedule(Mathf.CeilToInt((float)m_DrawInstances.Length / FindDrawInstancesJob.k_BatchSize), 1).Complete();

            DestroyDrawInstanceIndices(drawInstanceIndicesToDestroy.AsArray());

            destroyedInstancesSorted.Dispose();
            drawInstanceIndicesToDestroy.Dispose();
        }

        public unsafe void DestroyMaterialDrawInstances(NativeArray<uint> destroyedBatchMaterials)
        {
            if (m_DrawInstances.IsEmpty || destroyedBatchMaterials.Length == 0)
                return;

            NeedsRebuild();

            var destroyedBatchMaterialsSorted = new NativeArray<uint>(destroyedBatchMaterials, Allocator.TempJob);

            Profiler.BeginSample("DestroyedBatchMaterials.ParallelSort");
            destroyedBatchMaterialsSorted.Reinterpret<int>().ParallelSort().Complete();
            Profiler.EndSample();

            var drawInstanceIndicesToDestroy = new NativeList<int>(m_DrawInstances.Length, Allocator.TempJob);

            var findDrawInstancesJobHandle = new FindMaterialDrawInstancesJob()
            {
                materialsSorted = destroyedBatchMaterialsSorted,
                drawInstances = m_DrawInstances,
                outDrawInstanceIndicesWriter = drawInstanceIndicesToDestroy.AsParallelWriter()
            };

            findDrawInstancesJobHandle.Schedule(Mathf.CeilToInt((float)m_DrawInstances.Length / FindMaterialDrawInstancesJob.k_BatchSize), 1).Complete();

            DestroyDrawInstanceIndices(drawInstanceIndicesToDestroy.AsArray());

            destroyedBatchMaterialsSorted.Dispose();
            drawInstanceIndicesToDestroy.Dispose();
        }

        public void NeedsRebuild()
        {
            m_NeedsRebuild = true;
        }
    }

    internal class InstanceCullingBatcher : BaseRendererBatcher
    {
        private RenderersBatchersContext m_BatchersContext;
        private GPUDrivenProcessor m_GPUDrivenProcessor;
        private CPUInstanceData m_InstanceData;
        private BatchRendererGroup m_BRG;
        private NativeParallelHashMap<int, BatchID> m_GlobalBatchIDs;
        private InstanceCuller m_Culler;
        private NativeParallelHashMap<int, BatchMaterialID> m_BatchMaterialHash;
        private NativeParallelHashMap<int, BatchMeshID> m_BatchMeshHash;

        private int m_CachedInstanceDataBufferLayoutVersion;

        private OnCullingCompleteCallback m_OnCompleteCallback;

        private List<CPUInstanceData> m_ListCacheNoGC;

        public InstanceCullingBatcher(RenderersBatchersContext batcherContext, InstanceCullingBatcherDesc desc, GPUDrivenProcessor gpuDrivenProcessor, Shader brgPicking, Shader brgLoading, Shader brgError)
        {
            m_BatchersContext = batcherContext;
            m_GPUDrivenProcessor = gpuDrivenProcessor;
            m_InstanceData = new CPUInstanceData();
            m_InstanceData.Initialize();
            m_BRG = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
#if UNITY_EDITOR
            if (brgPicking != null)
            {
                var mat = new Material(brgPicking);
                mat.hideFlags = HideFlags.HideAndDontSave;
                m_BRG.SetPickingMaterial(mat);
            }
            if (brgLoading != null)
            {
                var mat = new Material(brgLoading);
                mat.hideFlags = HideFlags.HideAndDontSave;
                m_BRG.SetLoadingMaterial(mat);
            }
            if (brgError != null)
            {
                var mat = new Material(brgError);
                mat.hideFlags = HideFlags.HideAndDontSave;
                m_BRG.SetErrorMaterial(mat);
            }
            var viewTypes = new BatchCullingViewType[] {
                BatchCullingViewType.Light,
                BatchCullingViewType.Camera,
                BatchCullingViewType.Picking,
                BatchCullingViewType.SelectionOutline,
                BatchCullingViewType.Filtering
            };
            m_BRG.SetEnabledViewTypes(viewTypes);
#endif
            m_Culler = new InstanceCuller();
            m_CachedInstanceDataBufferLayoutVersion = -1;
            m_OnCompleteCallback = desc.onCompleteCallback;
            m_ListCacheNoGC = new List<CPUInstanceData>(1);
            m_BatchMaterialHash = new NativeParallelHashMap<int, BatchMaterialID>(64, Allocator.Persistent);
            m_BatchMeshHash = new NativeParallelHashMap<int, BatchMeshID>(64, Allocator.Persistent);

            m_GlobalBatchIDs = new NativeParallelHashMap<int, BatchID>(4, Allocator.Persistent);
            m_GlobalBatchIDs.Add((int)RenderersParameters.DefaultComponents, GetBatchID(RenderersParameters.DefaultComponents));
            m_GlobalBatchIDs.Add((int)RenderersParameters.DefaultAndProbesComponents, GetBatchID(RenderersParameters.DefaultAndProbesComponents));
            m_GlobalBatchIDs.Add((int)RenderersParameters.DefaultAndLightmapComponents, GetBatchID(RenderersParameters.DefaultAndLightmapComponents));

            BaseRendererBatcherPipeline.Initialize(this);
        }

        public override void Dispose()
        {
            m_OnCompleteCallback = null;
            m_Culler.Dispose();

            foreach (var batchID in m_GlobalBatchIDs)
            {
                if (!batchID.Value.Equals(BatchID.Null))
                    m_BRG.RemoveBatch(batchID.Value);
            }
            m_GlobalBatchIDs.Dispose();

            if (m_BRG != null)
                m_BRG.Dispose();

            m_InstanceData.Dispose();
            m_InstanceData = null;

            m_ListCacheNoGC.Clear();
            m_ListCacheNoGC = null;

            m_BatchMaterialHash.Dispose();
            m_BatchMeshHash.Dispose();

            BaseRendererBatcherPipeline.Shutdown();
        }

        private BatchID GetBatchID(RenderersParameters.InstanceComponents componentsOverriden)
        {
            if (m_CachedInstanceDataBufferLayoutVersion != m_BatchersContext.instanceDataBufferLayoutVersion)
                return BatchID.Null;

            const uint kClearIsOverriddenBit = 0x4FFFFFFF;
            var tempMetadata = new NativeList<MetadataValue>(m_BatchersContext.defaultMetadata.Length, Allocator.Temp);
            var componentOffset = 0;
            foreach (var metadata in m_BatchersContext.defaultMetadata)
            {
                var value = metadata.Value;
                // if instances in this batch do not override the component, clear the override bit
                if (((int)componentsOverriden & (1 << componentOffset++)) == 0)
                    value &= kClearIsOverriddenBit;
                tempMetadata.Add(new MetadataValue
                {
                    NameID = metadata.NameID,
                    Value = value
                });
            }

            return m_BRG.AddBatch(tempMetadata.AsArray(), m_BatchersContext.gpuInstanceDataBuffer.bufferHandle);
        }

        private void UpdateInstanceDataBufferLayoutVersion()
        {
            if (m_CachedInstanceDataBufferLayoutVersion != m_BatchersContext.instanceDataBufferLayoutVersion)
            {
                m_CachedInstanceDataBufferLayoutVersion = m_BatchersContext.instanceDataBufferLayoutVersion;

                foreach (var componentsToBatchID in m_GlobalBatchIDs)
                {
                    var batchID = componentsToBatchID.Value;
                    if (!batchID.Equals(BatchID.Null))
                        m_BRG.RemoveBatch(batchID);

                    var componentsOverriden = (RenderersParameters.InstanceComponents)componentsToBatchID.Key;
                    componentsToBatchID.Value = GetBatchID(componentsOverriden);
                }
            }
        }

        public CPUInstanceData GetInstanceData()
        {
            return m_InstanceData;
        }

        public unsafe JobHandle OnPerformCulling(
            BatchRendererGroup rendererGroup,
            BatchCullingContext cc,
            BatchCullingOutput cullingOutput,
            IntPtr userContext)
        {
            foreach (var batchID in m_GlobalBatchIDs)
            {
                if (batchID.Value.Equals(BatchID.Null))
                    return new JobHandle();
            }

            m_InstanceData.RebuildDrawListsIfNeeded();

            JobHandle jobHandle = m_Culler.CreateCullJobTree(
                cc,
                cullingOutput,
                m_BatchersContext.instanceDrawData,
                m_BatchersContext.lodGroupCullingData,
                m_InstanceData,
                m_GlobalBatchIDs,
                m_BatchersContext.crossfadedRendererCount,
                m_BatchersContext.rendererInstanceIDs,
                m_BatchersContext.transformIndices,
                m_BatchersContext.movedTransformIndices);

            if (m_OnCompleteCallback != null)
                m_OnCompleteCallback(jobHandle, cc, cullingOutput);

            return jobHandle;
        }

        public override void DestroyInstances(NativeArray<InstanceHandle> destroyedInstances)
        {
            if (destroyedInstances.Length == 0)
                return;

            Profiler.BeginSample("DestroyInstances");

            m_InstanceData.DestroyDrawInstances(destroyedInstances);

            Profiler.EndSample();
        }

        public override void DestroyMaterials(NativeArray<int> destroyedMaterials)
        {
            if (destroyedMaterials.Length == 0)
                return;

            Profiler.BeginSample("DestroyMaterials");

            var destroyedBatchMaterials = new NativeList<uint>(destroyedMaterials.Length, Allocator.TempJob);

            foreach (int destroyedMaterial in destroyedMaterials)
            {
                if (m_BatchMaterialHash.TryGetValue(destroyedMaterial, out var destroyedBatchMaterial))
                {
                    destroyedBatchMaterials.Add(destroyedBatchMaterial.value);
                    m_BatchMaterialHash.Remove(destroyedMaterial);
                    m_BRG.UnregisterMaterial(destroyedBatchMaterial);
                }
            }

            m_InstanceData.DestroyMaterialDrawInstances(destroyedBatchMaterials.AsArray());

            destroyedBatchMaterials.Dispose();

            Profiler.EndSample();
        }

        public override void DestroyMeshes(NativeArray<int> destroyedMeshes)
        {
            if (destroyedMeshes.Length == 0)
                return;

            Profiler.BeginSample("DestroyMeshes");

            foreach (int destroyedMesh in destroyedMeshes)
            {
                if (m_BatchMeshHash.TryGetValue(destroyedMesh, out var destroyedBatchMesh))
                {
                    m_BatchMeshHash.Remove(destroyedMesh);
                    m_BRG.UnregisterMesh(destroyedBatchMesh);
                }
            }

            Profiler.EndSample();
        }

        /*
        public override void UpdateMaterials(Material[] changedMaterials, NativeArray<int> changedMaterialsID, NativeArray<int> destroyedMaterialsID)
        {
        }
        */

        public override void PostCullBeginCameraRendering(RenderRequestBatcherContext context)
        {
            // We must update the ambient probe manually here to make sure probes are interpolated using the correct value.
            // Other renderer doesn't have this issue as probes are interpolated every frame. For us, a wrong interpolation would be stored persistently in our GPUInstanceDataBuffer.
            RenderSettings.ambientProbe = context.ambientProbe;
            m_BatchersContext.UpdateAmbientProbeAndGpuBuffer(context.ambientProbe);
        }

        public override void UpdateRenderers(NativeArray<int> renderersID)
        {
            if (renderersID.Length == 0)
                return;

            m_GPUDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(renderersID, (GPUDrivenRendererData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                if (rendererData.rendererID.Length == 0)
                    return;

                Profiler.BeginSample("ConvertRenderersToBRG");

                var usedMaterials = new NativeList<int>(Allocator.TempJob);
                usedMaterials.AddRange(rendererData.materialID);

                // Register lightmaps.
                // ----------------------------------------------------------------------------------------------------------------------------------

                Profiler.BeginSample("GenerateLightmappingData");

                var rendererToMaterialMap = m_BatchersContext.lightmapManager.GenerateLightmappingData(rendererData, materials, usedMaterials);

                Profiler.BeginSample("GetLightmapTextureIndex");

                var lightMapTextureIndices = new NativeArray<float4>(rendererData.rendererID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                m_BatchersContext.lightmapManager.GetLightmapTextureIndices(rendererData, lightMapTextureIndices);

                Profiler.EndSample();

                Profiler.EndSample();

                // ----------------------------------------------------------------------------------------------------------------------------------

                // Register Meshes
                // --------------------------------------------------------------------------------------------------------------------------------------

                var newMeshIDs = new NativeList<int>(rendererData.meshID.Length, Allocator.TempJob);

                new FindNonRegisteredInstancesJob<BatchMeshID>
                {
                    instanceIDs = rendererData.meshID,
                    hashMap = m_BatchMeshHash,
                    outInstancesWriter = newMeshIDs.AsParallelWriter()
                }
                .Schedule(Mathf.CeilToInt((float)rendererData.meshID.Length / FindNonRegisteredInstancesJob<BatchMeshID>.k_BatchSize), 1).Complete();

                var newBatchMeshIDs = new NativeArray<BatchMeshID>(newMeshIDs.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                m_BRG.RegisterMeshes(newMeshIDs.AsArray(), newBatchMeshIDs);

                int totalMeshesNum = m_BatchMeshHash.Count() + newBatchMeshIDs.Length;
                m_BatchMeshHash.Capacity = Math.Max(m_BatchMeshHash.Capacity, Mathf.CeilToInt(totalMeshesNum / 1023.0f) * 1024);

                new RegisterNewInstancesJob<BatchMeshID>
                {
                    instanceIDs = newMeshIDs.AsArray(),
                    batchIDs = newBatchMeshIDs,
                    hashMap = m_BatchMeshHash.AsParallelWriter()
                }
                .Schedule(newMeshIDs.Length, RegisterNewInstancesJob<BatchMeshID>.k_BatchSize).Complete();

                newMeshIDs.Dispose();
                newBatchMeshIDs.Dispose();

                // --------------------------------------------------------------------------------------------------------------------------------------

                // Register Materials
                // --------------------------------------------------------------------------------------------------------------------------------------

                var newMaterialIDs = new NativeList<int>(usedMaterials.Length, Allocator.TempJob);

                new FindNonRegisteredInstancesJob<BatchMaterialID>
                {
                    instanceIDs = usedMaterials.AsArray(),
                    hashMap = m_BatchMaterialHash,
                    outInstancesWriter = newMaterialIDs.AsParallelWriter()
                }
                .Schedule(Mathf.CeilToInt((float)usedMaterials.Length / FindNonRegisteredInstancesJob<BatchMaterialID>.k_BatchSize), 1).Complete();

                var newBatchMaterialIDs = new NativeArray<BatchMaterialID>(newMaterialIDs.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                m_BRG.RegisterMaterials(newMaterialIDs.AsArray(), newBatchMaterialIDs);

                int totalMaterialsNum = m_BatchMaterialHash.Count() + newMaterialIDs.Length;
                m_BatchMaterialHash.Capacity = Math.Max(m_BatchMaterialHash.Capacity, Mathf.CeilToInt(totalMaterialsNum / 1023.0f) * 1024);

                new RegisterNewInstancesJob<BatchMaterialID>
                {
                    instanceIDs = newMaterialIDs.AsArray(),
                    batchIDs = newBatchMaterialIDs,
                    hashMap = m_BatchMaterialHash.AsParallelWriter()
                }
                .Schedule(newMaterialIDs.Length, RegisterNewInstancesJob<BatchMaterialID>.k_BatchSize).Complete();

                newMaterialIDs.Dispose();
                newBatchMaterialIDs.Dispose();
                usedMaterials.Dispose();

                // --------------------------------------------------------------------------------------------------------------------------------------

                // Allocate and Update CPU instance data
                // --------------------------------------------------------------------------------------------------------------------------------------

                var instances = new NativeArray<InstanceHandle>(rendererData.rendererID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                m_BatchersContext.AllocateOrGetInstances(rendererData.rendererID, instances);
                m_BatchersContext.UpdateInstanceData(instances, rendererData);

                // --------------------------------------------------------------------------------------------------------------------------------------

                // Upload CPU Instance data to GPU
                // --------------------------------------------------------------------------------------------------------------------------------------

                GPUInstanceDataBufferUploader instanceUploader = m_BatchersContext.CreateDataBufferUploader(instances.Length);
                instanceUploader.AllocateInstanceHandles(instances);
                instanceUploader.WriteInstanceData(m_BatchersContext.renderersParameters.lightmapIndex.index, lightMapTextureIndices);
                // If an object is statically batched then rendererData.lightmapScaleOffset will contain identity scale offset.
                instanceUploader.WriteInstanceData(m_BatchersContext.renderersParameters.lightmapScale.index, rendererData.lightmapScaleOffset);
                m_BatchersContext.SubmitToGpu(instances, ref instanceUploader);
                m_BatchersContext.ChangeInstanceBufferVersion();
                lightMapTextureIndices.Dispose();
                instanceUploader.Dispose();

                // --------------------------------------------------------------------------------------------------------------------------------------

                m_BatchersContext.ReinitializeInstanceTransforms(instances, rendererData.localToWorldMatrix, rendererData.prevLocalToWorldMatrix);

                new CreateDrawBatchesJob
                {
                    instances = instances,
                    rendererData = rendererData,
                    rendererToMaterialMap = rendererToMaterialMap,
                    batchMeshHash = m_BatchMeshHash,
                    batchMaterialHash = m_BatchMaterialHash,
                    rangeHash = m_InstanceData.rangeHash,
                    drawRanges = m_InstanceData.drawRanges,
                    batchHash = m_InstanceData.batchHash,
                    drawBatches = m_InstanceData.drawBatches,
                    meshProceduralInfoHash = m_InstanceData.meshProceduralInfoHash,
                    drawInstances = m_InstanceData.drawInstances
                }.Run();

                m_InstanceData.NeedsRebuild();

                UpdateInstanceDataBufferLayoutVersion();

                instances.Dispose();

                Profiler.EndSample();
            });
        }

        public override void UpdateFrame()
        {

        }
    }
}
