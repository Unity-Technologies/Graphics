using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal partial class InstanceDataSystem : IDisposable
    {
        static float3 AABBRotateExtents(float3 extents, float3 m0, float3 m1, float3 m2)
        {
            return math.abs(m0 * extents.x) + math.abs(m1 * extents.y) + math.abs(m2 * extents.z);
        }

        public static AABB AABBTransform(float4x4 transform, AABB localBounds)
        {
            AABB transformed;
            transformed.extents = AABBRotateExtents(localBounds.extents, transform.c0.xyz, transform.c1.xyz, transform.c2.xyz);
            transformed.center = math.transform(transform, localBounds.center);
            return transformed;
        }

        private unsafe static int AtomicAddLengthNoResize<T>(in NativeList<T> list, int count) where T : unmanaged
        {
            UnsafeList<T>* unsafeList = list.GetUnsafeList();
            var newLength = Interlocked.Add(ref unsafeList->m_length, count);
            Assert.IsTrue(unsafeList->Capacity >= newLength);
            return newLength - count;
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private unsafe struct QueryRendererInstancesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<JaggedJobRange> jobRanges;
            [ReadOnly] public NativeParallelHashMap<EntityId, InstanceHandle> rendererToInstanceMap;
            [NativeDisableContainerSafetyRestriction, NoAlias][ReadOnly] public JaggedSpan<EntityId> jaggedRenderers;

            [NativeDisableContainerSafetyRestriction, NoAlias][WriteOnly] public NativeArray<InstanceHandle> instances;
            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicNonFoundInstancesCount;

            public void Execute(int jobIndex)
            {
                JaggedJobRange jobRange = jobRanges[jobIndex];
                NativeArray<EntityId> renderers = jaggedRenderers[jobRange.sectionIndex];
                int newInstancesCountJob = 0;

                for (int i = 0; i < jobRange.length; ++i)
                {
                    int localIndex = jobRange.localStart + i;
                    int absoluteIndex = jobRange.absoluteStart + i;

                    if (rendererToInstanceMap.TryGetValue(renderers[localIndex], out var instance))
                    {
                        instances[absoluteIndex] = instance;
                    }
                    else
                    {
                        newInstancesCountJob += 1;
                        instances[absoluteIndex] = InstanceHandle.Invalid;
                    }
                }

                if (atomicNonFoundInstancesCount.Counter != null && newInstancesCountJob > 0)
                    atomicNonFoundInstancesCount.Add(newInstancesCountJob);
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private struct QuerySortedMeshInstancesJob : IJobParallelForBatch
        {
            public const int MaxBatchSize = 64;

            [ReadOnly] public RenderWorld renderWorld;
            [ReadOnly] public NativeArray<EntityId> sortedMeshes;

            [NativeDisableParallelForRestriction][WriteOnly] public NativeList<InstanceHandle> instances;

            public void Execute(int startIndex, int count)
            {
                Assert.IsTrue(MaxBatchSize <= 64 && count <= 64);
                ulong validBits = 0;

                // Do a first pass to count the number of valid instances so we can do only one atomic add for the whole range.
                for (int i = 0; i < count; ++i)
                {
                    int instanceIndex = startIndex + i;
                    InstanceHandle instance = renderWorld.IndexToHanle(instanceIndex);
                    EntityId mesh = renderWorld.meshIDs[instanceIndex];

                    if (sortedMeshes.BinarySearch(mesh) >= 0)
                        validBits |= 1ul << i;
                }

                int validBitCount = math.countbits(validBits);

                if (validBitCount > 0)
                {
                    int writeIndex = AtomicAddLengthNoResize(instances, validBitCount);
                    int validBitIndex = math.tzcnt(validBits);

                    while (validBits != 0)
                    {
                        int instanceIndex = startIndex + validBitIndex;
                        instances[writeIndex] = renderWorld.indexToHandle[instanceIndex];

                        writeIndex += 1;
                        validBits &= ~(1ul << validBitIndex);
                        validBitIndex = math.tzcnt(validBits);
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private struct CalculateInterpolatedLightAndOcclusionProbesBatchJob : IJobParallelFor
        {
            public const int k_CalculatedProbesPerBatch = 8;

            [ReadOnly] public int probesCount;
            [ReadOnly] public LightProbesQuery lightProbesQuery;

            [NativeDisableParallelForRestriction][ReadOnly] public NativeArray<Vector3> queryPostitions;
            [NativeDisableParallelForRestriction] public NativeArray<int> compactTetrahedronCache;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<SphericalHarmonicsL2> probesSphericalHarmonics;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Vector4> probesOcclusion;

            public void Execute(int index)
            {
                var startIndex = index * k_CalculatedProbesPerBatch;
                var endIndex = math.min(probesCount, startIndex + k_CalculatedProbesPerBatch);
                var count = endIndex - startIndex;

                var compactTetrahedronCacheSubArray = compactTetrahedronCache.GetSubArray(startIndex, count);
                var queryPostitionsSubArray = queryPostitions.GetSubArray(startIndex, count);
                var probesSphericalHarmonicsSubArray = probesSphericalHarmonics.GetSubArray(startIndex, count);
                var probesOcclusionSubArray = probesOcclusion.GetSubArray(startIndex, count);
                lightProbesQuery.CalculateInterpolatedLightAndOcclusionProbes(queryPostitionsSubArray, compactTetrahedronCacheSubArray, probesSphericalHarmonicsSubArray, probesOcclusionSubArray);
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private struct ScatterTetrahedronCacheIndicesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<InstanceHandle> probeInstances;
            [ReadOnly] public NativeArray<int> compactTetrahedronCache;

            [NativeDisableContainerSafetyRestriction, NoAlias][NativeDisableParallelForRestriction] public RenderWorld renderWorld;

            public void Execute(int index)
            {
                InstanceHandle instance = probeInstances[index];
                int instanceIndex = renderWorld.HandleToIndex(instance);
                renderWorld.tetrahedronCacheIndices.ElementAtRW(instanceIndex) = compactTetrahedronCache[index];
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private unsafe struct TransformUpdateJob : IJobParallelFor
        {
            public const int MaxBatchSize = 64;

            [ReadOnly] public NativeArray<JaggedJobRange> jobRanges;
            [ReadOnly] public NativeArray<InstanceHandle> instances;
            [ReadOnly] public JaggedSpan<float4x4> jaggedLocalToWorldMatrices;
            [ReadOnly] public JaggedSpan<float4x4> jaggedPrevLocalToWorldMatrices;
            [ReadOnly] public bool initialize;
            [ReadOnly] public bool enableBoundingSpheres;

            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicTransformQueueCount;
            [NativeDisableParallelForRestriction] public RenderWorld renderWorld;
            [NativeDisableParallelForRestriction] public NativeArray<InstanceHandle> transformUpdateInstanceQueue;
            [NativeDisableParallelForRestriction] public NativeArray<TransformUpdatePacket> transformUpdateDataQueue;
            [NativeDisableParallelForRestriction] public NativeArray<float4> boundingSpheresDataQueue;

            public void Execute(int jobIndex)
            {
                var jobRange = jobRanges[jobIndex];
                var localToWorldMatrices = jaggedLocalToWorldMatrices[jobRange.sectionIndex];
                var prevLocalToWorldMatrices = jaggedPrevLocalToWorldMatrices[jobRange.sectionIndex];

                // The bitfield can only contain up to 64 instances
                Assert.IsTrue(MaxBatchSize <= 64 && jobRange.length <= 64);
                ulong validBits = 0;

                for (int i = 0; i < jobRange.length; ++i)
                {
                    InstanceHandle instance = instances[jobRange.absoluteStart + i];
                    if (!instance.isValid)
                        continue;

                    if (!initialize)
                    {
                        int instanceIndex = renderWorld.HandleToIndex(instance);
                        bool movedCurrentFrame = renderWorld.movedInCurrentFrameBits.Get(instanceIndex);
                        bool isStaticObject = renderWorld.rendererSettings[instanceIndex].IsPartOfStaticBatch;

                        if (isStaticObject || movedCurrentFrame)
                            continue;
                    }

                    validBits |= 1ul << i;
                }

                int validBitCount = math.countbits(validBits);

                if (validBitCount > 0)
                {
                    int writeIndex = atomicTransformQueueCount.Add(validBitCount);
                    int validBitIndex = math.tzcnt(validBits);

                    while (validBits != 0)
                    {
                        int absoluteIndex = jobRange.absoluteStart + validBitIndex;
                        int localIndex = jobRange.localStart + validBitIndex;

                        InstanceHandle instance = instances[absoluteIndex];
                        int instanceIndex = renderWorld.HandleToIndex(instance);
                        bool isStaticObject = renderWorld.rendererSettings[instanceIndex].IsPartOfStaticBatch;

                        renderWorld.movedInCurrentFrameBits.Set(instanceIndex, !isStaticObject);
                        transformUpdateInstanceQueue[writeIndex] = instance;

                        ref readonly float4x4 l2w = ref localToWorldMatrices.ElementAt(localIndex);
                        ref readonly AABB localAABB = ref renderWorld.localAABBs.ElementAt(instanceIndex);
                        AABB worldAABB = AABBTransform(l2w, localAABB);
                        float det = math.determinant((float3x3)l2w);

                        renderWorld.worldAABBs.ElementAtRW(instanceIndex) = worldAABB;
                        renderWorld.localToWorldIsFlippedBits.Set(instanceIndex, det < 0.0f);

                        if (initialize)
                        {
                            PackedMatrix l2wPacked = PackedMatrix.FromFloat4x4(l2w);
                            PackedMatrix l2wPrevPacked = PackedMatrix.FromFloat4x4(prevLocalToWorldMatrices.ElementAt(localIndex));

                            transformUpdateDataQueue[writeIndex * 2] = new TransformUpdatePacket()
                            {
                                localToWorld0 = l2wPacked.packed0,
                                localToWorld1 = l2wPacked.packed1,
                                localToWorld2 = l2wPacked.packed2,
                            };
                            transformUpdateDataQueue[writeIndex * 2 + 1] = new TransformUpdatePacket()
                            {
                                localToWorld0 = l2wPrevPacked.packed0,
                                localToWorld1 = l2wPrevPacked.packed1,
                                localToWorld2 = l2wPrevPacked.packed2,
                            };
                        }
                        else
                        {
                            PackedMatrix l2wPacked = PackedMatrix.FromFloat4x4(l2w);

                            transformUpdateDataQueue[writeIndex] = new TransformUpdatePacket()
                            {
                                localToWorld0 = l2wPacked.packed0,
                                localToWorld1 = l2wPacked.packed1,
                                localToWorld2 = l2wPacked.packed2,
                            };
                        }

                        if (enableBoundingSpheres)
                            boundingSpheresDataQueue[writeIndex] = new float4(worldAABB.center.x, worldAABB.center.y, worldAABB.center.z, math.distance(worldAABB.max, worldAABB.min) * 0.5f);

                        writeIndex += 1;
                        validBits &= ~(1ul << validBitIndex);
                        validBitIndex = math.tzcnt(validBits);
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private unsafe struct ProbesUpdateJob : IJobParallelForBatch
        {
            public const int MaxBatchSize = 64;

            [ReadOnly] [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<InstanceHandle> instances;
            [ReadOnly] [NativeDisableContainerSafetyRestriction, NoAlias] public RenderWorld renderWorld;

            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicProbesQueueCount;
            [NativeDisableParallelForRestriction] public NativeArray<InstanceHandle> probeInstanceQueue;
            [NativeDisableParallelForRestriction] public NativeArray<int> compactTetrahedronCache;
            [NativeDisableParallelForRestriction] public NativeArray<Vector3> probeQueryPosition;

            public void Execute(int startIndex, int count)
            {
                Assert.IsTrue(MaxBatchSize <= 64 && count <= 64);
                ulong validBits = 0;

                for (int i = 0; i < count; ++i)
                {
                    InstanceHandle instance = instances[startIndex + i];
                    if (!instance.isValid)
                        continue;

                    int instanceIndex = renderWorld.HandleToIndex(instance);
                    InternalMeshRendererSettings rendererSettings = renderWorld.rendererSettings[instanceIndex];
                    int lightmapIndex = renderWorld.lightmapIndices[instanceIndex];
                    bool hasLightProbes = !LightmapUtils.UsesLightmaps(lightmapIndex)
                        && rendererSettings.LightProbeUsage == LightProbeUsage.BlendProbes;

                    if (!hasLightProbes)
                        continue;

                    validBits |= 1ul << i;
                }

                int validBitCount = math.countbits(validBits);

                if (validBitCount > 0)
                {
                    int writeIndex = atomicProbesQueueCount.Add(validBitCount);
                    int validBitIndex = math.tzcnt(validBits);

                    while (validBits != 0)
                    {
                        InstanceHandle instance = instances[startIndex + validBitIndex];
                        int instanceIndex = renderWorld.HandleToIndex(instance);
                        ref readonly AABB worldAABB = ref renderWorld.worldAABBs.ElementAt(instanceIndex);

                        probeInstanceQueue[writeIndex] = instance;
                        probeQueryPosition[writeIndex] = worldAABB.center;
                        compactTetrahedronCache[writeIndex] = renderWorld.tetrahedronCacheIndices[instanceIndex];

                        writeIndex += 1;
                        validBits &= ~(1ul << validBitIndex);
                        validBitIndex = math.tzcnt(validBits);
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private struct MotionUpdateJob : IJobParallelFor
        {
            [ReadOnly] public int queueWriteBase;

            [NativeDisableParallelForRestriction] public RenderWorld renderWorld;
            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicUpdateQueueCount;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<InstanceHandle> transformUpdateInstanceQueue;

            public void Execute(int chunk_index)
            {
                int maxChunkBitCount = math.min(renderWorld.instanceCount - 64 * chunk_index, 64);
                ulong chunkBitMask = ~0ul >> (64 - maxChunkBitCount);

                ulong currentChunkBits = renderWorld.movedInCurrentFrameBits.GetChunk(chunk_index) & chunkBitMask;
                ulong prevChunkBits = renderWorld.movedInPreviousFrameBits.GetChunk(chunk_index) & chunkBitMask;

                // update state in memory for the next frame
                renderWorld.movedInCurrentFrameBits.SetChunk(chunk_index, 0);
                renderWorld.movedInPreviousFrameBits.SetChunk(chunk_index, currentChunkBits);

                // ensure that objects that were moved last frame update their previous world matrix, if not already fully updated
                ulong remainingChunkBits = prevChunkBits & ~currentChunkBits;

                // allocate space for all the writes from this chunk
                int chunkBitCount = math.countbits(remainingChunkBits);
                int writeIndex = queueWriteBase;
                if (chunkBitCount > 0)
                    writeIndex += atomicUpdateQueueCount.Add(chunkBitCount);

                // loop over set bits to do the writes
                int indexInChunk = math.tzcnt(remainingChunkBits);

                while (indexInChunk < 64)
                {
                    int instanceIndex = 64 * chunk_index + indexInChunk;
                    transformUpdateInstanceQueue[writeIndex] = renderWorld.IndexToHanle(instanceIndex);

                    writeIndex += 1;
                    remainingChunkBits &= ~(1ul << indexInChunk);
                    indexInChunk = math.tzcnt(remainingChunkBits);
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private unsafe struct UpdateRendererInstancesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<JaggedJobRange> jobRanges;
            [ReadOnly] public MeshRendererUpdateBatch updateBatch;
            [ReadOnly] public NativeArray<InstanceHandle> instances;
            [ReadOnly] public NativeParallelHashMap<EntityId, GPUInstanceIndex> lodGroupDataMap;

            [NativeDisableParallelForRestriction][NativeDisableContainerSafetyRestriction] public RenderWorld renderWorld;

            public void Execute(int jobIndex)
            {
                JaggedJobRange jobRange = jobRanges[jobIndex];
                NativeArray<EntityId> rendererSection = updateBatch.instanceIDs[jobRange.sectionIndex];
                NativeArray<EntityId> meshSection = updateBatch.GetMeshSectionOrDefault(jobRange.sectionIndex);
                NativeArray<ushort> subMeshStartIndexSection = updateBatch.GetSubMeshStartIndexSectionOrDefault(jobRange.sectionIndex);
                NativeArray<EntityId> materialSection = updateBatch.GetMaterialSectionOrDefault(jobRange.sectionIndex);
                NativeArray<RangeInt> subMaterialRangeSection = updateBatch.GetSubMaterialRangeSectionOrDefault(jobRange.sectionIndex);
                NativeArray<float4x4> localToWorldSection = updateBatch.GetLocalToWorldSectionOrDefault(jobRange.sectionIndex);
                NativeArray<AABB> localBoundsSection = updateBatch.GetLocalBoundsSectionOrDefault(jobRange.sectionIndex);
                NativeArray<InternalMeshRendererSettings> rendererSettingsSection = updateBatch.GetRendererSettingsSectionOrDefault(jobRange.sectionIndex);
                NativeArray<int> rendererPrioritySection = updateBatch.GetRendererPrioritySectionOrDefault(jobRange.sectionIndex);
                NativeArray<short> lightmapIndexSection = updateBatch.GetLightmapIndexSectionOrDefault(jobRange.sectionIndex);
                NativeArray<EntityId> parentLODGroupSection = updateBatch.GetParentLODGroupIDSectionOrDefault(jobRange.sectionIndex);
                NativeArray<byte> lodMaskSection = updateBatch.GetLODMaskSectionOrDefault(jobRange.sectionIndex);
                NativeArray<InternalMeshLodRendererSettings> meshLodSettingsSection = updateBatch.GetMeshLodSettingsSectionOrDefault(jobRange.sectionIndex);
                UnsafeBitArray renderingEnabledSection = updateBatch.GetRenderingEnabledSectionOrDefault(jobRange.sectionIndex);

                NativeArray<ulong> sceneCullingMaskSection = default;
                ulong sharedSceneCullingMask = 0;
                if (updateBatch.HasAnyComponent(MeshRendererComponentMask.SceneCullingMask))
                {
                    if (updateBatch.useSharedSceneCullingMask)
                    {
                        sharedSceneCullingMask = updateBatch.sharedSceneCullingMasks[jobRange.sectionIndex];
                    }
                    else
                    {
                        sceneCullingMaskSection = updateBatch.sceneCullingMasks[jobRange.sectionIndex];
                    }
                }

                MeshRendererUpdateType updateType = updateBatch.updateType;
                bool hasOnlyKnowInstances = updateType == MeshRendererUpdateType.NoStructuralChanges
                    || updateType == MeshRendererUpdateType.RecreateOnlyKnownInstances;

                int treeCountDelta = 0;

                for (int i = 0; i < jobRange.length; i++)
                {
                    int localIndex = jobRange.localStart + i;
                    int absoluteIndex = jobRange.absoluteStart + i;
                    EntityId renderer = rendererSection[localIndex];

                    InstanceHandle instance = instances[absoluteIndex];
                    Assert.IsTrue(instance.isValid, "Invalid Instance");
                    if (!instance.isValid)
                        continue;

                    int instanceIndex = renderWorld.HandleToIndex(instance);
                    bool oldInstanceHasTree = renderWorld.rendererSettings[instanceIndex].HasTree;

                    // Rebuild instance from scratch in all cases except when we know there is no structural changes.
                    if (updateType != MeshRendererUpdateType.NoStructuralChanges)
                        renderWorld.ResetInstance(instanceIndex);

                    if (hasOnlyKnowInstances)
                    {
                        Assert.IsTrue(renderWorld.instanceIDs[instanceIndex] == renderer);
                    }
                    else
                    {
                        renderWorld.instanceIDs.ElementAtRW(instanceIndex) = renderer;
                    }

                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.Material))
                    {
                        var subMaterialRange = subMaterialRangeSection[localIndex];
                        var subMaterialIDs = new EmbeddedArray32<EntityId>(materialSection.GetSubArray(subMaterialRange.start, subMaterialRange.length),
                            Allocator.Persistent);

                        renderWorld.materialIDArrays.ElementAtRW(instanceIndex).Dispose();
                        renderWorld.materialIDArrays.ElementAtRW(instanceIndex) = subMaterialIDs;
                    }

                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.LocalBounds))
                    {
                        Assert.AreEqual(localToWorldSection.Length, rendererSection.Length);
                        ref readonly float4x4 l2w = ref localToWorldSection.ElementAt(localIndex);
                        AABB localBounds = localBoundsSection[localIndex];
                        AABB worldBounds = AABBTransform(l2w, localBounds);

                        // Note that world bounds are updated here only when local bounds change, but not when transforms change.
                        // Transform changes are handled in other dedicated jobs.
                        renderWorld.localAABBs.ElementAtRW(instanceIndex) = localBounds;
                        renderWorld.worldAABBs.ElementAtRW(instanceIndex) = worldBounds;
                    }

                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.ParentLODGroup))
                    {
                        EntityId parentLODGroup = parentLODGroupSection[localIndex];

                        // Renderer's LODGroup could be disabled which means that the renderer is not managed by it.
                        GPUInstanceIndex parentLODGroupHandle = GPUInstanceIndex.Invalid;
                        if (lodGroupDataMap.TryGetValue(parentLODGroup, out var handle))
                            parentLODGroupHandle = handle;

                        renderWorld.lodGroupIndices.ElementAtRW(instanceIndex) = parentLODGroupHandle;
                    }

                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.LODMask))
                        renderWorld.lodMasks.ElementAtRW(instanceIndex) = lodMaskSection[localIndex];

                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.MeshLodSettings))
                        renderWorld.meshLodRendererSettings.ElementAtRW(instanceIndex) = meshLodSettingsSection[localIndex];

                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.Mesh))
                        renderWorld.meshIDs.ElementAtRW(instanceIndex) = meshSection[localIndex];

                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.SubMeshStartIndex))
                        renderWorld.subMeshStartIndices.ElementAtRW(instanceIndex) = subMeshStartIndexSection[localIndex];

                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.RendererSettings))
                    {
                        InternalMeshRendererSettings newSettings = rendererSettingsSection[localIndex];

                        if (oldInstanceHasTree)
                        {
                            if (!newSettings.HasTree)
                                --treeCountDelta;
                        }
                        else
                        {
                            if (newSettings.HasTree)
                                ++treeCountDelta;
                        }

                        renderWorld.rendererSettings.ElementAtRW(instanceIndex) = newSettings;
                    }

                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.Lightmap))
                        renderWorld.lightmapIndices.ElementAtRW(instanceIndex) = lightmapIndexSection[localIndex];

                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.RendererPriority))
                        renderWorld.rendererPriorities.ElementAtRW(instanceIndex) = rendererPrioritySection[localIndex];

                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.RenderingEnabled))
                        renderWorld.renderingEnabled.Set(instanceIndex, renderingEnabledSection.IsSet(localIndex));
#if UNITY_EDITOR
                    if (updateBatch.HasAnyComponent(MeshRendererComponentMask.SceneCullingMask))
                        renderWorld.sceneCullingMasks.ElementAtRW(instanceIndex) = updateBatch.useSharedSceneCullingMask ?
                            sharedSceneCullingMask :
                            sceneCullingMaskSection[localIndex];
#endif
                }

                int oldTotalTreeCount = renderWorld.atomicTotalTreeCount.Add(treeCountDelta);
                Assert.IsTrue(oldTotalTreeCount + treeCountDelta >= 0);
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private struct GetVisibleNonProcessedTreeInstancesJob : IJobParallelForBatch
        {
            public const int MaxBatchSize = 64;

            [ReadOnly] public RenderWorld renderWorld;
            [ReadOnly][NativeDisableContainerSafetyRestriction, NoAlias] public ParallelBitArray compactedVisibilityMasks;
            [ReadOnly] public bool becomeVisible;

            [NativeDisableParallelForRestriction] public ParallelBitArray processedBits;

            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<EntityId> renderers;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<InstanceHandle> instances;

            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicTreeInstancesCount;

            public void Execute(int startIndex, int count)
            {
                Assert.IsTrue(MaxBatchSize == 64 && count <= 64);
                ulong validBits = 0;

                var chunkIndex = startIndex / 64;
                var visibleInPrevFrameChunk = renderWorld.visibleInPreviousFrameBits.GetChunk(chunkIndex);
                var processedChunk = processedBits.GetChunk(chunkIndex);

                for (int i = 0; i < count; ++i)
                {
                    int instanceIndex = startIndex + i;
                    InstanceHandle instance = renderWorld.IndexToHanle(instanceIndex);
                    bool hasTree = renderWorld.rendererSettings[instanceIndex].HasTree;

                    if (hasTree && compactedVisibilityMasks.Get(instance.index))
                    {
                        var bitMask = 1ul << i;

                        var processedInCurrentFrame = (processedChunk & bitMask) != 0;

                        if (!processedInCurrentFrame)
                        {
                            bool visibleInPrevFrame = (visibleInPrevFrameChunk & bitMask) != 0;

                            if (becomeVisible)
                            {
                                if (!visibleInPrevFrame)
                                    validBits |= bitMask;
                            }
                            else
                            {
                                if (visibleInPrevFrame)
                                    validBits |= bitMask;
                            }
                        }
                    }
                }

                int validBitsCount = math.countbits(validBits);

                if (validBitsCount > 0)
                {
                    processedBits.SetChunk(chunkIndex, processedChunk | validBits);

                    int writeIndex = atomicTreeInstancesCount.Add(validBitsCount);
                    int validBitIndex = math.tzcnt(validBits);

                    while (validBits != 0)
                    {
                        int instanceIndex = startIndex + validBitIndex;
                        EntityId renderer = renderWorld.instanceIDs[instanceIndex];
                        InstanceHandle instance = renderWorld.IndexToHanle(instanceIndex);

                        renderers[writeIndex] = renderer;
                        instances[writeIndex] = instance;

                        writeIndex += 1;
                        validBits &= ~(1ul << validBitIndex);
                        validBitIndex = math.tzcnt(validBits);
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        private struct UpdateCompactedInstanceVisibilityJob : IJobParallelForBatch
        {
            public const int MaxBatchSize = 64;

            [ReadOnly] public ParallelBitArray compactedVisibilityMasks;

            [NativeDisableContainerSafetyRestriction, NoAlias][NativeDisableParallelForRestriction] public RenderWorld renderWorld;

            public void Execute(int startIndex, int count)
            {
                Assert.IsTrue(MaxBatchSize == 64 && count <= 64);
                ulong visibleBits = 0;

                for (int i = 0; i < count; ++i)
                {
                    int instanceIndex = startIndex + i;
                    InstanceHandle instance = renderWorld.IndexToHanle(instanceIndex);
                    bool visible = compactedVisibilityMasks.Get(instance.index);

                    if (visible)
                        visibleBits |= 1ul << i;
                }

                renderWorld.visibleInPreviousFrameBits.SetChunk(startIndex / 64, visibleBits);
            }
        }

    }
}
