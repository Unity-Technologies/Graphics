using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    internal delegate void OnCullingCompleteCallback(JobHandle jobHandle, in BatchCullingContext cullingContext, in BatchCullingOutput cullingOutput);

    internal struct MeshInfo
    {
        public BatchMeshID meshID;
        public int meshLodCount;
        public Mesh.LodSelectionCurve meshLodSelectionCurve;
        public EmbeddedArray64<GPUDrivenSubMesh> subMeshes;

        public bool isLodSelectionActive => meshLodCount > 1;

        public bool EqualsSourceData(in GPUDrivenMeshData otherData, NativeArray<GPUDrivenSubMesh> otherSubMeshes)
        {
            if (meshLodCount != otherData.meshLodCount)
                return false;

            if (meshLodSelectionCurve.lodBias != otherData.meshLodSelectionCurve.lodBias)
                return false;

            if (meshLodSelectionCurve.lodSlope != otherData.meshLodSelectionCurve.lodSlope)
                return false;

            if (subMeshes.Length != otherSubMeshes.Length)
                return false;

            for (int i = 0; i < subMeshes.Length; i++)
            {
                GPUDrivenSubMesh oldSubMesh = subMeshes[i];
                GPUDrivenSubMesh newSubMesh = otherSubMeshes[i];

                if (!oldSubMesh.Equals(newSubMesh))
                    return false;
            }

            return true;
        }
    }

    internal class InstanceCullingBatcher : IDisposable
    {
        private GPUResidentContext m_GRDContext;
        private InstanceDataSystem m_InstanceDataSystem;
        private LODGroupDataSystem m_LODGroupDataSystem;
        private CPUDrawInstanceData m_DrawInstanceData;
        private BatchRendererGroup m_BRG;
        private OnCullingCompleteCallback m_OnCompleteCallback;
        private NativeParallelHashMap<GPUArchetypeHandle, BatchID> m_BatchIDs;
        private NativeParallelHashMap<EntityId, GPUDrivenMaterial> m_MaterialMap;
        private NativeParallelHashMap<EntityId, MeshInfo> m_MeshMap;
        private int m_CachedInstanceDataBufferLayoutVersion;
        private NativeArray<BatchMeshID> m_TempBatchMeshIDs;
        private NativeHashSet<EntityId> m_TempChangedMeshIDs;

        public NativeParallelHashMap<EntityId, GPUDrivenMaterial> materialMap => m_MaterialMap;
        public NativeParallelHashMap<EntityId, MeshInfo> meshMap => m_MeshMap;

        public void Initialize(GPUResidentContext grdContext,
            in GPUResidentDrawerSettings settings,
            BatchRendererGroup.OnFinishedCulling onFinishedCulling,
            OnCullingCompleteCallback onCompleteCallback = null)
        {
            m_GRDContext = grdContext;
            m_InstanceDataSystem = grdContext.instanceDataSystem;
            m_LODGroupDataSystem = grdContext.lodGroupDataSystem;
            m_DrawInstanceData = new CPUDrawInstanceData();
            m_DrawInstanceData.Initialize();

            m_BRG = new BatchRendererGroup(new BatchRendererGroupCreateInfo
            {
                cullingCallback = OnPerformCulling,
                finishedCullingCallback = onFinishedCulling,
                userContext = IntPtr.Zero
            });

#if UNITY_EDITOR
            if (settings.pickingShader != null)
            {
                var mat = new Material(settings.pickingShader);
                mat.hideFlags = HideFlags.HideAndDontSave;
                m_BRG.SetPickingMaterial(mat);
            }
            if (settings.loadingShader != null)
            {
                var mat = new Material(settings.loadingShader);
                mat.hideFlags = HideFlags.HideAndDontSave;
                m_BRG.SetLoadingMaterial(mat);
            }
            if (settings.errorShader != null)
            {
                var mat = new Material(settings.errorShader);
                mat.hideFlags = HideFlags.HideAndDontSave;
                m_BRG.SetErrorMaterial(mat);
            }
            m_BRG.SetEnabledViewTypes(new BatchCullingViewType[]
            {
                BatchCullingViewType.Light,
                BatchCullingViewType.Camera,
                BatchCullingViewType.Picking,
                BatchCullingViewType.SelectionOutline,
                BatchCullingViewType.Filtering
            });
#endif

            m_CachedInstanceDataBufferLayoutVersion = -1;
            m_OnCompleteCallback = onCompleteCallback;
            m_MaterialMap = new NativeParallelHashMap<EntityId, GPUDrivenMaterial>(64, Allocator.Persistent);
            m_MeshMap = new NativeParallelHashMap<EntityId, MeshInfo>(64, Allocator.Persistent);
            m_BatchIDs = new NativeParallelHashMap<GPUArchetypeHandle, BatchID>(8, Allocator.Persistent);
            m_InstanceDataSystem.onGPUBufferLayoutChanged += UpdateInstanceDataBufferLayoutVersion;
        }

        public void Dispose()
        {
            m_OnCompleteCallback = null;

            m_InstanceDataSystem.onGPUBufferLayoutChanged -= UpdateInstanceDataBufferLayoutVersion;

            foreach (var batchID in m_BatchIDs)
            {
                m_BRG.RemoveBatch(batchID.Value);
            }
            m_BatchIDs.Dispose();

            if (m_BRG != null)
                m_BRG.Dispose();

            m_DrawInstanceData.Dispose();
            m_DrawInstanceData = null;

            m_MaterialMap.Dispose();

            foreach (var kv in m_MeshMap)
                kv.Value.subMeshes.Dispose();
            m_MeshMap.Dispose();
        }

        private BatchID AddBatch(GPUArchetypeHandle archetype)
        {
            if (m_CachedInstanceDataBufferLayoutVersion != m_InstanceDataSystem.gpuBufferLayoutVersion)
                return BatchID.Null;

            var instanceDataBuffer = m_InstanceDataSystem.gpuBuffer;
            var archetypeDesc = m_InstanceDataSystem.archetypeManager.GetRef().GetArchetypeDesc(archetype);
            var components = archetypeDesc.components;

            var metadatas = new NativeArray<MetadataValue>(components.Length, Allocator.Temp);

            for (int i = 0; i < metadatas.Length; ++i)
            {
                var componentHandle = components[i];
                var compIndex = instanceDataBuffer.GetComponentIndex(componentHandle).index;
                var metadata = m_InstanceDataSystem.gpuBuffer.componentsMetadata[compIndex];
                metadatas[i] = metadata;
            }

            return m_BRG.AddBatch(metadatas, m_InstanceDataSystem.gpuBufferHandle);
        }

        private void UpdateInstanceDataBufferLayoutVersion()
        {
            if (m_CachedInstanceDataBufferLayoutVersion != m_InstanceDataSystem.gpuBufferLayoutVersion)
            {
                m_CachedInstanceDataBufferLayoutVersion = m_InstanceDataSystem.gpuBufferLayoutVersion;

                foreach (var kv in m_BatchIDs)
                {
                    var batchID = kv.Value;
                    m_BRG.RemoveBatch(batchID);
                }
                m_BatchIDs.Clear();

                var archetypeManager = m_InstanceDataSystem.archetypeManager;
                var archetypeCount = archetypeManager.GetRef().GetArchetypesCount();

                for (int i = 0; i < archetypeCount; i++)
                {
                    var archetype = GPUArchetypeHandle.Create((short)i);
                    var batchID = AddBatch(archetype);
                    m_BatchIDs.Add(archetype, batchID);
                }
            }
        }

        private void OnFetchMeshesDataForRegistration(NativeArray<EntityId> meshIDs,
            NativeArray<GPUDrivenMeshData> meshDatas,
            NativeArray<int> subMeshOffsets,
            NativeArray<GPUDrivenSubMesh> subMeshes)
        {
            Assert.IsTrue(meshIDs.Length == meshDatas.Length);
            Assert.IsTrue(m_TempBatchMeshIDs.Length == meshIDs.Length);

            new RegisterNewMeshesJob
            {
                meshMap = m_MeshMap.AsParallelWriter(),
                instanceIDs = meshIDs,
                batchMeshIDs = m_TempBatchMeshIDs,
                meshDatas = meshDatas,
                subMeshOffsets = subMeshOffsets,
                subMeshBuffer = subMeshes,
            }
            .RunParallel(meshIDs.Length, 128);
        }

        private void RegisterMeshes(JaggedSpan<EntityId> meshIDs)
        {
            Profiler.BeginSample("RegisterMeshes");
            var jobRanges = JaggedJobRange.FromSpanWithMaxBatchSize(meshIDs, FindNonRegisteredInstanceIDsJob<MeshInfo>.MaxBatchSize, Allocator.TempJob);
            var newMeshSet = new NativeParallelHashSet<EntityId>(meshIDs.totalLength, Allocator.TempJob);

            new FindNonRegisteredInstanceIDsJob<MeshInfo>
            {
                jobRanges = jobRanges.AsArray(),
                jaggedInstanceIDs = meshIDs,
                hashMap = m_MeshMap,
                outInstanceIDWriter = newMeshSet.AsParallelWriter()
            }
            .RunParallel(jobRanges);

            if (!newMeshSet.IsEmpty)
            {
                NativeArray<EntityId> newMeshIDs = newMeshSet.ToNativeArray(Allocator.TempJob);

                var batchMeshIDs = new NativeArray<BatchMeshID>(newMeshIDs.Length, Allocator.TempJob);
                GPUDrivenProcessor.RegisterMeshes(m_BRG, newMeshIDs, batchMeshIDs);

                int totalMeshesNum = m_MeshMap.Count() + newMeshIDs.Length;
                m_MeshMap.Capacity = Math.Max(m_MeshMap.Capacity, Mathf.CeilToInt(totalMeshesNum / 1023.0f) * 1024);

                m_TempBatchMeshIDs = batchMeshIDs;
                GPUDrivenProcessor.FetchMeshDatas(newMeshIDs, OnFetchMeshesDataForRegistration);
                m_TempBatchMeshIDs = default;

                newMeshIDs.Dispose();
                batchMeshIDs.Dispose();
            }

            jobRanges.Dispose();
            newMeshSet.Dispose();
            Profiler.EndSample();
        }

        private void RegisterMaterials(JaggedSpan<EntityId> materials)
        {
            Profiler.BeginSample("RegisterMaterials");
            var jobRanges = JaggedJobRange.FromSpanWithMaxBatchSize(materials, FindNonRegisteredInstanceIDsJob<GPUDrivenMaterial>.MaxBatchSize, Allocator.TempJob);
            var newMaterialIDSet = new NativeParallelHashSet<EntityId>(materials.totalLength, Allocator.TempJob);

            new FindNonRegisteredInstanceIDsJob<GPUDrivenMaterial>
            {
                jobRanges = jobRanges.AsArray(),
                jaggedInstanceIDs = materials,
                hashMap = m_MaterialMap,
                outInstanceIDWriter = newMaterialIDSet.AsParallelWriter()
            }
            .RunParallel(jobRanges);

            if (!newMaterialIDSet.IsEmpty)
            {
                NativeArray<EntityId> newMaterialIDs = newMaterialIDSet.ToNativeArray(Allocator.TempJob);

                var newMaterials = new NativeArray<GPUDrivenMaterial>(newMaterialIDs.Length, Allocator.TempJob);
                GPUDrivenProcessor.RegisterMaterials(m_BRG, newMaterialIDs, newMaterials);

                int totalMaterialsNum = m_MaterialMap.Count() + newMaterialIDs.Length;
                m_MaterialMap.Capacity = Math.Max(m_MaterialMap.Capacity, Mathf.CeilToInt(totalMaterialsNum / 1023.0f) * 1024);

                new RegisterNewMaterialsJob
                {
                    instanceIDs = newMaterialIDs,
                    materials = newMaterials,
                    materialMap = m_MaterialMap.AsParallelWriter()
                }
                .RunParallel(newMaterialIDs.Length, 128);

                newMaterialIDs.Dispose();
                newMaterials.Dispose();
            }

            jobRanges.Dispose();
            newMaterialIDSet.Dispose();
            Profiler.EndSample();
        }

        private void OnFetchMeshesDataForUpdate(NativeArray<EntityId> meshIDs,
            NativeArray<GPUDrivenMeshData> meshDatas,
            NativeArray<int> subMeshOffsets,
            NativeArray<GPUDrivenSubMesh> subMeshBuffer)
        {
            Assert.IsTrue(meshIDs.Length == meshDatas.Length);
            Assert.IsTrue(m_TempChangedMeshIDs.IsCreated);

            InstanceCullingBatcherBurst.UpdateMeshData(meshIDs, meshDatas, subMeshOffsets, subMeshBuffer, ref m_MeshMap, ref m_TempChangedMeshIDs);
        }

        public NativeHashSet<EntityId> UpdateMeshData(NativeArray<EntityId> meshIDs, Allocator allocator)
        {
            NativeHashSet<EntityId> changeMeshIDs = new NativeHashSet<EntityId>(meshIDs.Length, allocator);

            m_TempChangedMeshIDs = changeMeshIDs;
            GPUDrivenProcessor.FetchMeshDatas(meshIDs, OnFetchMeshesDataForUpdate);
            m_TempChangedMeshIDs = default;

            return changeMeshIDs;
        }

        public NativeHashSet<EntityId> UpdateMaterialData(NativeArray<EntityId> materials, NativeArray<GPUDrivenMaterialData> materialDatas, Allocator allocator)
        {
            NativeHashSet<EntityId> changeMaterialIDs = new NativeHashSet<EntityId>(materials.Length, allocator);
            InstanceCullingBatcherBurst.UpdateMaterialData(materials, materialDatas, ref m_MaterialMap, ref changeMaterialIDs);
            return changeMaterialIDs;
        }

        public CPUDrawInstanceData GetDrawInstanceData()
        {
            return m_DrawInstanceData;
        }

        public unsafe JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext context, BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            foreach (var batchID in m_BatchIDs)
            {
                if (batchID.Value == BatchID.Null)
                    return new JobHandle();
            }

            m_DrawInstanceData.RebuildDrawListsIfNeeded();

            if (m_DrawInstanceData.drawRanges.Length == 0)
                return default;

            if (m_DrawInstanceData.drawBatches.Length == 0)
                return default;

            IncludeExcludeListFilter includeExcludeListFilter = default;

#if UNITY_EDITOR

            includeExcludeListFilter = IncludeExcludeListFilter.GetFilterForCurrentCullingCallback(context, Allocator.TempJob);

            // If inclusive filtering is enabled and we know there are no included entities,
            // we can skip all the work because we know that the result will be nothing.
            if (includeExcludeListFilter.IsIncludeEnabled && includeExcludeListFilter.IsIncludeEmpty)
            {
                includeExcludeListFilter.Dispose();
                return default;
            }
#else
            includeExcludeListFilter = IncludeExcludeListFilter.GetEmptyFilter(Allocator.TempJob);
#endif

            bool allowOcclusionCulling = m_InstanceDataSystem.hasBoundingSpheres;
            JobHandle jobHandle = m_GRDContext.culler.CreateCullJobTree(
                context,
                cullingOutput,
                m_InstanceDataSystem.renderWorld,
                m_GRDContext.batcher.meshMap,
                m_InstanceDataSystem.gpuBuffer.AsReadOnly(),
                m_LODGroupDataSystem.lodGroupCullingData,
                m_DrawInstanceData,
                m_BatchIDs,
                m_GRDContext.smallMeshScreenPercentage,
                allowOcclusionCulling ? m_GRDContext.occlusionCullingCommon : null,
                includeExcludeListFilter);

            if (m_OnCompleteCallback != null)
                m_OnCompleteCallback(jobHandle, context, cullingOutput);

            includeExcludeListFilter.Dispose(jobHandle);
            return jobHandle;
        }

        public void DestroyDrawInstances(NativeArray<InstanceHandle> instances)
        {
            m_DrawInstanceData.DestroyDrawInstances(instances);
        }

        public void DestroyMaterials(NativeArray<EntityId> destroyedInstanceIDs)
        {
            if (destroyedInstanceIDs.Length == 0)
                return;

            Profiler.BeginSample("DestroyMaterials");

            var destroyedBatchMaterials = new NativeList<uint>(destroyedInstanceIDs.Length, Allocator.TempJob);

            foreach (EntityId instanceID in destroyedInstanceIDs)
            {
                if (m_MaterialMap.TryGetValue(instanceID, out GPUDrivenMaterial materialData))
                {
                    BatchMaterialID batchMaterialID = materialData.materialID;
                    destroyedBatchMaterials.Add(batchMaterialID.value);
                    m_MaterialMap.Remove(instanceID);
                    m_BRG.UnregisterMaterial(batchMaterialID);
                }
            }

            m_DrawInstanceData.DestroyMaterialDrawInstances(destroyedBatchMaterials.AsArray());

            destroyedBatchMaterials.Dispose();

            Profiler.EndSample();
        }

        public void DestroyMeshes(NativeArray<EntityId> destroyedInstanceIDs)
        {
            if (destroyedInstanceIDs.Length == 0)
                return;

            Profiler.BeginSample("DestroyMeshes");

            foreach (EntityId instanceID in destroyedInstanceIDs)
            {
                if (m_MeshMap.TryGetValue(instanceID, out var meshData))
                {
                    meshData.subMeshes.Dispose();
                    m_MeshMap.Remove(instanceID);
                    m_BRG.UnregisterMesh(meshData.meshID);
                }
            }

            Profiler.EndSample();
        }

        public void BuildBatches(NativeArray<InstanceHandle> instances)
        {
            Profiler.BeginSample("BuildBatches");

            DestroyDrawInstances(instances);

            var rangeHash = m_DrawInstanceData.rangeHash;
            var drawRanges = m_DrawInstanceData.drawRanges;
            var batchHash = m_DrawInstanceData.batchHash;
            var drawBatches = m_DrawInstanceData.drawBatches;
            var drawInstances = m_DrawInstanceData.drawInstances;

            InstanceCullingBatcherBurst.CreateDrawBatches(instances,
                ref m_InstanceDataSystem.renderWorld,
                m_MeshMap,
                m_MaterialMap,
                ref rangeHash,
                ref drawRanges,
                ref batchHash,
                ref drawBatches,
                ref drawInstances);

            m_DrawInstanceData.NeedsRebuild();

            Profiler.EndSample();
        }

        public void RegisterAndBuildBatches(NativeArray<InstanceHandle> instances, in MeshRendererUpdateBatch updateBatch)
        {
            Profiler.BeginSample("RegisterAndBuildBatches");

            if (updateBatch.HasAnyComponent(MeshRendererComponentMask.Material))
                RegisterMaterials(updateBatch.materialIDs);

            if (updateBatch.HasAnyComponent(MeshRendererComponentMask.Mesh))
                RegisterMeshes(updateBatch.meshIDs);

            BuildBatches(instances);

            Profiler.EndSample();
        }
    }
}
