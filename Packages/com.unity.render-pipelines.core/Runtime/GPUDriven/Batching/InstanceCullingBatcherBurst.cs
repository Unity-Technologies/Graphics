using Unity.Collections;
using Unity.Burst;
using UnityEngine.Assertions;
using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    [BurstCompile]
    internal static class InstanceCullingBatcherBurst
    {
        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void UpdateMaterialData(in NativeArray<EntityId> materialIDs,
            in NativeArray<GPUDrivenMaterialData> materialDatas,
            ref NativeParallelHashMap<EntityId, GPUDrivenMaterial> materialMap,
            ref NativeHashSet<EntityId> changedMaterialIDs)
        {
            for (int index = 0; index < materialIDs.Length; index++)
            {
                EntityId materialID = materialIDs[index];
                GPUDrivenMaterialData newMaterialData = materialDatas[index];

                if (!materialMap.TryGetValue(materialID, out var material) || material.data.Equals(newMaterialData))
                    continue;

                // Update the material data
                material.data = newMaterialData;
                materialMap[materialID] = material;
                changedMaterialIDs.Add(materialID);
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void UpdateMeshData(in NativeArray<EntityId> meshIDs,
            in NativeArray<GPUDrivenMeshData> meshDatas,
            in NativeArray<int> subMeshOffsets,
            in NativeArray<GPUDrivenSubMesh> subMeshBuffer,
            ref NativeParallelHashMap<EntityId, MeshInfo> meshMap,
            ref NativeHashSet<EntityId> changedMeshIDs)
        {
            for (int index = 0; index < meshIDs.Length; index++)
            {
                EntityId meshID = meshIDs[index];

                GPUDrivenMeshData newMeshData = meshDatas[index];
                int subMeshOffset = subMeshOffsets[index];
                int totalSubMeshCount = newMeshData.subMeshCount * math.max(newMeshData.meshLodCount, 1);
                NativeArray<GPUDrivenSubMesh> newSubMeshes = subMeshBuffer.GetSubArray(subMeshOffset, totalSubMeshCount);

                if (!meshMap.TryGetValue(meshID, out var meshInfo) || meshInfo.EqualsSourceData(newMeshData, newSubMeshes))
                    continue;

                MeshInfo newMeshInfo = meshInfo;
                newMeshInfo.meshLodCount = newMeshData.meshLodCount;
                newMeshInfo.meshLodSelectionCurve = newMeshData.meshLodSelectionCurve;
                newMeshInfo.subMeshes = new EmbeddedArray64<GPUDrivenSubMesh>(newSubMeshes, Allocator.Persistent);

                // Update the mesh data
                meshMap[meshID] = newMeshInfo;
                changedMeshIDs.Add(meshID);

                meshInfo.subMeshes.Dispose();
            }
        }

        private static ref DrawRange EditDrawRange(in RangeKey key, ref NativeParallelHashMap<RangeKey, int> rangeHash, ref NativeList<DrawRange> drawRanges)
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

        private static ref DrawBatch EditDrawBatch(in DrawKey key,
            in GPUDrivenSubMesh subMesh,
            ref NativeParallelHashMap<DrawKey, int> batchHash,
            ref NativeList<DrawBatch> drawBatches)
        {
            int drawBatchIndex;

            if (!batchHash.TryGetValue(key, out drawBatchIndex))
            {
                var drawBatch = new DrawBatch
                {
                    key = key,
                    instanceCount = 0,
                    instanceOffset = 0,
                    baseVertex = subMesh.baseVertex,
                    firstIndex = subMesh.indexStart,
                    indexCount = subMesh.indexCount,
                    topology = subMesh.topology,
                };

                drawBatchIndex = drawBatches.Length;
                batchHash.Add(key, drawBatchIndex);
                drawBatches.Add(drawBatch);
            }

            ref DrawBatch data = ref drawBatches.ElementAt(drawBatchIndex);
            Assert.IsTrue(data.key.Equals(key));

            return ref data;
        }

        private static void ProcessRenderer(InstanceHandle instance,
            ref RenderWorld renderWorld,
            in NativeParallelHashMap<EntityId, MeshInfo> meshMap,
            in NativeParallelHashMap<EntityId, GPUDrivenMaterial> materialMap,
            ref NativeParallelHashMap<RangeKey, int> rangeHash,
            ref NativeList<DrawRange> drawRanges,
            ref NativeParallelHashMap<DrawKey, int> batchHash,
            ref NativeList<DrawBatch> drawBatches,
            ref NativeList<DrawInstance> drawInstances)
        {
            Assert.IsTrue(instance.isValid, "Invalid Instance");
            if (!instance.isValid)
                return;

            int instanceIndex = renderWorld.HandleToIndex(instance);
            GPUArchetypeHandle archetype = renderWorld.gpuHandles[instanceIndex].archetype;
            EntityId meshID = renderWorld.meshIDs[instanceIndex];
            EntityId rendererID = renderWorld.instanceIDs[instanceIndex];
            short lightmapIndex = renderWorld.lightmapIndices[instanceIndex];
            InternalMeshRendererSettings rendererSettings = renderWorld.rendererSettings[instanceIndex];
            int rendererPriority = renderWorld.rendererPriorities[instanceIndex];
            ushort subMeshStartIndex = renderWorld.subMeshStartIndices[instanceIndex];
            EmbeddedArray32<EntityId> subMaterialIDs = renderWorld.materialIDArrays[instanceIndex];

            if (!meshMap.TryGetValue(meshID, out MeshInfo mesh))
                return;

            // Scan all materials once to retrieve whether this renderer is indirect-compatible or not (and store it in the RangeKey).
            // Also cache hash map lookups since we need them right after.
            bool supportsIndirect = true;
            NativeArray<GPUDrivenMaterial> subMaterials = new NativeArray<GPUDrivenMaterial>(subMaterialIDs.Length, Allocator.Temp);
            for (int i = 0; i < subMaterialIDs.Length; i++)
            {
                EntityId subMaterialID = subMaterialIDs[i];
                if (!materialMap.TryGetValue(subMaterialID, out GPUDrivenMaterial subMaterial))
                    continue;

                supportsIndirect &= subMaterial.isIndirectSupported;
                subMaterials[i] = subMaterial;
            }

            var rangeKey = new RangeKey
            {
                layer = rendererSettings.ObjectLayer,
                renderingLayerMask = rendererSettings.RenderingLayerMask,
                motionMode = rendererSettings.MotionVectorGenerationMode,
                shadowCastingMode = rendererSettings.ShadowCastingMode,
                staticShadowCaster = rendererSettings.StaticShadowCaster,
                rendererPriority = rendererPriority,
                supportsIndirect = supportsIndirect
            };

            ref DrawRange drawRange = ref EditDrawRange(rangeKey, ref rangeHash, ref drawRanges);

            for (int i = 0; i < subMaterials.Length; i++)
            {
                GPUDrivenMaterial subMaterial = subMaterials[i];
                if (subMaterial.materialID == BatchMaterialID.Null)
                    continue;

                // We always provide crossfade value packed in instance index. We don't use None even if there is no LOD to not split the batch.
                var flags = BatchDrawCommandFlags.LODCrossFadeValuePacked;

                if (LightmapUtils.UsesLightmaps(lightmapIndex))
                    flags |= BatchDrawCommandFlags.UseLegacyLightmapsKeyword;

                // assume that a custom motion vectors pass contains deformation motion, so should always output motion vectors
                // (otherwise this flag is set dynamically during culling only when the transform is changing)
                if (subMaterial.isMotionVectorsPassEnabled)
                    flags |= BatchDrawCommandFlags.HasMotion;

                if (subMaterial.isTransparent)
                    flags |= BatchDrawCommandFlags.HasSortingPosition;

                if (subMaterial.supportsCrossFade)
                    flags |= BatchDrawCommandFlags.LODCrossFadeKeyword;

                // Static batching uses per MeshRenderer sub-mesh offset
                int subMeshIndex = subMeshStartIndex + i;
                int lodLoopCount = math.max(mesh.meshLodCount, 1);

                for (int lodLoopIndex = 0; lodLoopIndex < lodLoopCount; lodLoopIndex++)
                {
                    GPUDrivenSubMesh subMesh = mesh.subMeshes[subMeshIndex * lodLoopCount + lodLoopIndex];

                    var drawKey = new DrawKey
                    {
                        materialID = subMaterial.materialID,
                        meshID = mesh.meshID,
                        submeshIndex = subMeshIndex,
                        activeMeshLod = mesh.isLodSelectionActive ? lodLoopIndex : -1,
                        flags = flags,
                        transparentInstanceID = subMaterial.isTransparent ? rendererID : EntityId.None,
                        range = rangeKey,
                        archetype = archetype,
                        // When we've opted out of lightmap texture arrays, we
                        // need to pass in a valid lightmap index. The engine
                        // uses this index for sorting and for breaking the
                        // batch when lightmaps change across draw calls, and
                        // for binding the correct light map.
                        lightmapIndex = lightmapIndex
                    };

                    ref DrawBatch drawBatch = ref EditDrawBatch(drawKey, subMesh, ref batchHash, ref drawBatches);

                    if (drawBatch.instanceCount == 0)
                        drawRange.drawCount += 1;

                    drawBatch.instanceCount += 1;

                    drawInstances.Add(new DrawInstance
                    {
                        key = drawKey,
                        instanceIndex = instance.index
                    });
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void CreateDrawBatches(in NativeArray<InstanceHandle> instances,
            ref RenderWorld renderWorld,
            in NativeParallelHashMap<EntityId, MeshInfo> meshMap,
            in NativeParallelHashMap<EntityId, GPUDrivenMaterial> materialMap,
            ref NativeParallelHashMap<RangeKey, int> rangeHash,
            ref NativeList<DrawRange> drawRanges,
            ref NativeParallelHashMap<DrawKey, int> batchHash,
            ref NativeList<DrawBatch> drawBatches,
            ref NativeList<DrawInstance> drawInstances)
        {
            for (int i = 0; i < instances.Length; i++)
                ProcessRenderer(instances[i], ref renderWorld, meshMap, materialMap, ref rangeHash, ref drawRanges, ref batchHash, ref drawBatches, ref drawInstances);
        }
    }
}
