using System;
using Unity.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    [BurstCompile]
    internal static class InstanceCullingBatcherBurst
    {
        private static void RemoveDrawRange(in RangeKey key, ref NativeParallelHashMap<RangeKey, int> rangeHash, ref NativeList<DrawRange> drawRanges)
        {
            int drawRangeIndex = rangeHash[key];

            ref DrawRange lastDrawRange = ref drawRanges.ElementAt(drawRanges.Length - 1);
            rangeHash[lastDrawRange.key] = drawRangeIndex;

            rangeHash.Remove(key);
            drawRanges.RemoveAtSwapBack(drawRangeIndex);
        }

        private static void RemoveDrawBatch(in DrawKey key, ref NativeList<DrawRange> drawRanges, ref NativeParallelHashMap<RangeKey, int> rangeHash,
                                            ref NativeParallelHashMap<DrawKey, int> batchHash, ref NativeList<DrawBatch> drawBatches)
        {
            int drawBatchIndex = batchHash[key];

            int drawRangeIndex = rangeHash[key.range];
            ref DrawRange drawRange = ref drawRanges.ElementAt(drawRangeIndex);

            Assert.IsTrue(drawRange.drawCount > 0);

            if (--drawRange.drawCount == 0)
                RemoveDrawRange(drawRange.key, ref rangeHash, ref drawRanges);

            ref DrawBatch lastDrawBatch = ref drawBatches.ElementAt(drawBatches.Length - 1);
            batchHash[lastDrawBatch.key] = drawBatchIndex;

            batchHash.Remove(key);
            drawBatches.RemoveAtSwapBack(drawBatchIndex);
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static unsafe void RemoveDrawInstanceIndices(in NativeArray<int> drawInstanceIndices, ref NativeList<DrawInstance> drawInstances, ref NativeParallelHashMap<RangeKey, int> rangeHash,
            ref NativeParallelHashMap<DrawKey, int> batchHash, ref NativeList<DrawRange> drawRanges, ref NativeList<DrawBatch> drawBatches)
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
                    RemoveDrawBatch(drawBatch.key, ref drawRanges, ref rangeHash, ref batchHash, ref drawBatches);

                UnsafeUtility.MemCpy(drawInstance, drawInstancesPtr + drawInstancesNewBack--, sizeof(DrawInstance));
            }

            drawInstances.ResizeUninitialized(drawInstancesNewBack + 1);
        }

        private static ref DrawRange EditDrawRange(in RangeKey key, NativeParallelHashMap<RangeKey, int> rangeHash, NativeList<DrawRange> drawRanges)
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

        private static ref DrawBatch EditDrawBatch(in DrawKey key, in SubMeshDescriptor subMeshDescriptor, NativeParallelHashMap<DrawKey, int> batchHash, NativeList<DrawBatch> drawBatches)
        {
            var procInfo = new MeshProceduralInfo();
            procInfo.topology = subMeshDescriptor.topology;
            procInfo.baseVertex = (uint)subMeshDescriptor.baseVertex;
            procInfo.firstIndex = (uint)subMeshDescriptor.indexStart;
            procInfo.indexCount = (uint)subMeshDescriptor.indexCount;

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

        private static void ProcessRenderer(int i, bool implicitInstanceIndices, in GPUDrivenRendererGroupData rendererData,
            NativeParallelHashMap<int, BatchMeshID> batchMeshHash, NativeParallelHashMap<int, GPUDrivenPackedMaterialData> packedMaterialDataHash,
            NativeParallelHashMap<int, BatchMaterialID> batchMaterialHash, NativeArray<InstanceHandle> instances, NativeList<DrawInstance> drawInstances,
            NativeParallelHashMap<RangeKey, int> rangeHash, NativeList<DrawRange> drawRanges, NativeParallelHashMap<DrawKey, int> batchHash,
            NativeList<DrawBatch> drawBatches)
        {
            var meshIndex = rendererData.meshIndex[i];
            var meshID = rendererData.meshID[meshIndex];
            var meshLodInfo = rendererData.meshLodInfo[meshIndex];
            var submeshCount = rendererData.subMeshCount[meshIndex];
            var subMeshDescOffset = rendererData.subMeshDescOffset[meshIndex];
            var batchMeshID = batchMeshHash[meshID];
            var rendererGroupID = rendererData.rendererGroupID[i];
            var startSubMesh = rendererData.subMeshStartIndex[i];
            var gameObjectLayer = rendererData.gameObjectLayer[i];
            var renderingLayerMask = rendererData.renderingLayerMask[i];
            var materialsOffset = rendererData.materialsOffset[i];
            var materialsCount = rendererData.materialsCount[i];
            var lightmapIndex = rendererData.lightmapIndex[i];
            var packedRendererData = rendererData.packedRendererData[i];
            var rendererPriority = rendererData.rendererPriority[i];

            int instanceCount;
            int instanceOffset;

            if (implicitInstanceIndices)
            {
                instanceCount = 1;
                instanceOffset = i;
            }
            else
            {
                instanceCount = rendererData.instancesCount[i];
                instanceOffset = rendererData.instancesOffset[i];
            }

            if (instanceCount == 0)
                return;

            const int kLightmapIndexMask = 0xffff;
            const int kLightmapIndexInfluenceOnly = 0xfffe;

            var overridenComponents = InstanceComponentGroup.Default;

            // Add per-instance wind parameters
            if(packedRendererData.hasTree)
                overridenComponents |= InstanceComponentGroup.Wind;

            var lmIndexMasked = lightmapIndex & kLightmapIndexMask;

            // Object doesn't have a valid lightmap Index, -> uses probes for lighting
            if (lmIndexMasked >= kLightmapIndexInfluenceOnly)
            {
                // Only add the component when needed to store blended results (shader will use the ambient probe when not present)
                if (packedRendererData.lightProbeUsage == LightProbeUsage.BlendProbes)
                    overridenComponents |= InstanceComponentGroup.LightProbe;
            }
            else
            {
                // Add per-instance lightmap parameters
                overridenComponents |= InstanceComponentGroup.Lightmap;
            }

            // Scan all materials once to retrieve whether this renderer is indirect-compatible or not (and store it in the RangeKey).
            Span<GPUDrivenPackedMaterialData> packedMaterialDatas = stackalloc GPUDrivenPackedMaterialData[materialsCount];

            var supportsIndirect = true;
            for (int matIndex = 0; matIndex < materialsCount; ++matIndex)
            {
                if (matIndex >= submeshCount)
                {
                    Debug.LogWarning("Material count in the shared material list is higher than sub mesh count for the mesh. Object may be corrupted.");
                    continue;
                }

                var materialIndex = rendererData.materialIndex[materialsOffset + matIndex];
                GPUDrivenPackedMaterialData packedMaterialData;

                if (rendererData.packedMaterialData.Length > 0)
                {
                    packedMaterialData = rendererData.packedMaterialData[materialIndex];
                }
                else
                {
                    var materialID = rendererData.materialID[materialIndex];
                    bool isFound = packedMaterialDataHash.TryGetValue(materialID, out packedMaterialData);
                    Assert.IsTrue(isFound);
                }
                supportsIndirect &= packedMaterialData.isIndirectSupported;

                packedMaterialDatas[matIndex] = packedMaterialData;
            }

            var rangeKey = new RangeKey
            {
                layer = (byte)gameObjectLayer,
                renderingLayerMask = renderingLayerMask,
                motionMode = packedRendererData.motionVecGenMode,
                shadowCastingMode = packedRendererData.shadowCastingMode,
                staticShadowCaster = packedRendererData.staticShadowCaster,
                rendererPriority = rendererPriority,
                supportsIndirect = supportsIndirect
            };

            ref DrawRange drawRange = ref EditDrawRange(rangeKey, rangeHash, drawRanges);

            for (int matIndex = 0; matIndex < materialsCount; ++matIndex)
            {
                if (matIndex >= submeshCount)
                {
                    Debug.LogWarning("Material count in the shared material list is higher than sub mesh count for the mesh. Object may be corrupted.");
                    continue;
                }

                var materialIndex = rendererData.materialIndex[materialsOffset + matIndex];
                var materialID = rendererData.materialID[materialIndex];
                var packedMaterialData = packedMaterialDatas[matIndex];

                if (materialID == 0)
                {
                    Debug.LogWarning("Material in the shared materials list is null. Object will be partially rendered.");
                    continue;
                }

                batchMaterialHash.TryGetValue(materialID, out BatchMaterialID batchMaterialID);

                // We always provide crossfade value packed in instance index. We don't use None even if there is no LOD to not split the batch.
                var flags = BatchDrawCommandFlags.LODCrossFadeValuePacked;

                // Let the engine know if we've opted out of lightmap texture arrays
                flags |= BatchDrawCommandFlags.UseLegacyLightmapsKeyword;

                // assume that a custom motion vectors pass contains deformation motion, so should always output motion vectors
                // (otherwise this flag is set dynamically during culling only when the transform is changing)
                if (packedMaterialData.isMotionVectorsPassEnabled)
                    flags |= BatchDrawCommandFlags.HasMotion;

                if (packedMaterialData.isTransparent)
                    flags |= BatchDrawCommandFlags.HasSortingPosition;

                if (packedMaterialData.supportsCrossFade)
                    flags |= BatchDrawCommandFlags.LODCrossFadeKeyword;

                int lodLoopCount = math.max(meshLodInfo.levelCount, 1);

                for (int lodLoopIndex = 0; lodLoopIndex < lodLoopCount; ++lodLoopIndex)
                {
                    var submeshIndex = startSubMesh + matIndex;
                    var subMeshDesc = rendererData.subMeshDesc[subMeshDescOffset + submeshIndex*lodLoopCount + lodLoopIndex];
                    var drawKey = new DrawKey
                    {
                        materialID = batchMaterialID,
                        meshID = batchMeshID,
                        submeshIndex = submeshIndex,
                        activeMeshLod = meshLodInfo.lodSelectionActive ? lodLoopIndex : -1,
                        flags = flags,
                        transparentInstanceId = packedMaterialData.isTransparent ? rendererGroupID : 0,
                        range = rangeKey,
                        overridenComponents = (uint)overridenComponents,
                        // When we've opted out of lightmap texture arrays, we
                        // need to pass in a valid lightmap index. The engine
                        // uses this index for sorting and for breaking the
                        // batch when lightmaps change across draw calls, and
                        // for binding the correct light map.
                        lightmapIndex = lightmapIndex
                    };

                    ref DrawBatch drawBatch = ref EditDrawBatch(drawKey, subMeshDesc, batchHash, drawBatches);

                    if (drawBatch.instanceCount == 0)
                        ++drawRange.drawCount;

                    drawBatch.instanceCount += instanceCount;

                    for (int j = 0; j < instanceCount; ++j)
                    {
                        var instanceIndex = instanceOffset + j;
                        InstanceHandle instance = instances[instanceIndex];
                        drawInstances.Add(new DrawInstance { key = drawKey, instanceIndex = instance.index });
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void CreateDrawBatches(bool implicitInstanceIndices, in NativeArray<InstanceHandle> instances, in GPUDrivenRendererGroupData rendererData,
            in NativeParallelHashMap<int, BatchMeshID> batchMeshHash, in NativeParallelHashMap<int, BatchMaterialID> batchMaterialHash,
            in NativeParallelHashMap<int, GPUDrivenPackedMaterialData> packedMaterialDataHash,
            ref NativeParallelHashMap<RangeKey, int> rangeHash, ref NativeList<DrawRange> drawRanges, ref NativeParallelHashMap<DrawKey, int> batchHash, ref NativeList<DrawBatch> drawBatches,
            ref NativeList<DrawInstance> drawInstances)
        {
            for (int i = 0; i < rendererData.rendererGroupID.Length; ++i)
                ProcessRenderer(i, implicitInstanceIndices, rendererData, batchMeshHash, packedMaterialDataHash, batchMaterialHash, instances,
                    drawInstances, rangeHash, drawRanges, batchHash, drawBatches);
        }
    }
}
