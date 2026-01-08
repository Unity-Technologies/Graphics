using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    internal class WorldProcessor : IDisposable
    {
        private GPUDrivenProcessor m_GPUDrivenProcessor;
        private ObjectDispatcher m_ObjectDispatcher;
        private InstanceDataSystem m_InstanceDataSystem;
        private InstanceCullingBatcher m_Batcher;
        private MeshRendererProcessor m_MeshRendererProcessor;
        private LODGroupProcessor m_LODGroupProcessor;

        // Update batches
        private NativeList<MeshRendererUpdateBatch> m_MeshRendererUpdateBatches;
        private NativeList<LODGroupUpdateBatch> m_LODGroupUpdateBatches;
        private NativeList<NativeArray<EntityId>> m_MeshRendererDeletionBatches;
        private NativeList<NativeArray<EntityId>> m_LODGroupDeletionBatches;

        public MeshRendererProcessor meshRendererProcessor => m_MeshRendererProcessor;
        public LODGroupProcessor lodDGroupProcessor => m_LODGroupProcessor;

        public void Initialize(GPUDrivenProcessor gpuDrivenProcessor, ObjectDispatcher objectDispatcher, GPUResidentContext context)
        {
            m_GPUDrivenProcessor = gpuDrivenProcessor;
            m_ObjectDispatcher = objectDispatcher;
            m_InstanceDataSystem = context.instanceDataSystem;
            m_Batcher = context.batcher;
            m_LODGroupProcessor = new LODGroupProcessor(gpuDrivenProcessor, context);
            m_MeshRendererProcessor = new MeshRendererProcessor(gpuDrivenProcessor, context);

            m_MeshRendererUpdateBatches = new NativeList<MeshRendererUpdateBatch>(16, Allocator.Persistent);
            m_LODGroupUpdateBatches = new NativeList<LODGroupUpdateBatch>(16, Allocator.Persistent);
            m_MeshRendererDeletionBatches = new NativeList<NativeArray<EntityId>>(16, Allocator.Persistent);
            m_LODGroupDeletionBatches = new NativeList<NativeArray<EntityId>>(16, Allocator.Persistent);
        }

        public void Dispose()
        {
            m_MeshRendererUpdateBatches.Dispose();
            m_LODGroupUpdateBatches.Dispose();
            m_MeshRendererDeletionBatches.Dispose();
            m_LODGroupDeletionBatches.Dispose();
            m_MeshRendererProcessor.Dispose();

            m_MeshRendererProcessor = null;
            m_LODGroupProcessor = null;
        }

        public void Update()
        {
            Profiler.BeginSample("WorldProcessor.Update");

            Profiler.BeginSample("FetchAllChanges");
            var meshDataSorted = m_ObjectDispatcher.GetTypeChangesAndClear<Mesh>(Allocator.TempJob, sortByInstanceID: true, noScriptingArray: true);
            var materialData = m_ObjectDispatcher.GetTypeChangesAndClear<Material>(Allocator.TempJob, noScriptingArray: true);
            var cameraData = m_ObjectDispatcher.GetTypeChangesAndClear<Camera>(Allocator.TempJob, noScriptingArray: true);
            var lodGroupData = m_ObjectDispatcher.GetTypeChangesAndClear<LODGroup>(Allocator.TempJob, noScriptingArray: true);
            var lodGroupTransformData = m_ObjectDispatcher.GetTransformChangesAndClear<LODGroup>(ObjectDispatcher.TransformTrackingType.GlobalTRS, Allocator.TempJob);
            var rendererData = m_ObjectDispatcher.GetTypeChangesAndClear<MeshRenderer>(Allocator.TempJob, noScriptingArray: true);
            var transformChanges = m_ObjectDispatcher.GetTransformChangesAndClear<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS, Allocator.TempJob);
            Profiler.EndSample();

            if (cameraData.changedID.Length > 0)
            {
                Profiler.BeginSample("ProcessCameraChanges");
                m_InstanceDataSystem.AddCameras(cameraData.changedID);
                m_InstanceDataSystem.RemoveCameras(cameraData.destroyedID);
                Profiler.EndSample();
            }

            ClassifyMaterials(materialData.changedID,
                materialData.destroyedID,
                out NativeList<EntityId> unsupportedMaterials,
                out NativeList<EntityId> changedMaterials,
                out NativeList<EntityId> destroyedMaterials,
                out NativeList<GPUDrivenMaterialData> changedMaterialDatas,
                Allocator.TempJob);

            NativeList<EntityId> changedMeshes = FindOnlyUsedMeshes(meshDataSorted.changedID, Allocator.TempJob);

            NativeList<EntityId> unsupportedRenderers = FindUnsupportedRenderers(unsupportedMaterials.AsArray(), Allocator.TempJob);

            if (unsupportedRenderers.Length > 0)
            {
                Profiler.BeginSample("DestroyUnsupportedRenderers");
                m_GPUDrivenProcessor.DisableGPUDrivenRendering(unsupportedRenderers.AsArray());
                m_MeshRendererProcessor.DestroyInstances(unsupportedRenderers.AsArray());
                Profiler.EndSample();
            }

            m_Batcher.DestroyMaterials(destroyedMaterials.AsArray());
            m_Batcher.DestroyMaterials(unsupportedMaterials.AsArray());

            if (meshDataSorted.destroyedID.Length > 0)
            {
                Profiler.BeginSample("DestroyMeshes");
                var destroyedMeshInstances = new NativeList<InstanceHandle>(Allocator.TempJob);
                m_InstanceDataSystem.ScheduleQuerySortedMeshInstancesJob(meshDataSorted.destroyedID, destroyedMeshInstances).Complete();
                m_Batcher.DestroyDrawInstances(destroyedMeshInstances.AsArray());
                //@ Check if we need to update instance bounds and light probe sampling positions after mesh is destroyed.
                m_Batcher.DestroyMeshes(meshDataSorted.destroyedID);
                destroyedMeshInstances.Dispose();
                Profiler.EndSample();
            }

            if (lodGroupData.changedID.Length > 0)
            {
                Profiler.BeginSample("ProcessLODGroupChanges");
                m_LODGroupProcessor.ProcessGameObjectChanges(lodGroupData.changedID, transformOnly: false);
                Profiler.EndSample();
            }

            if (lodGroupTransformData.transformedID.Length > 0)
            {
                Profiler.BeginSample("ProcessLODGroupTransformChanges");
                m_LODGroupProcessor.ProcessGameObjectChanges(lodGroupTransformData.transformedID, transformOnly: true);
                Profiler.EndSample();
            }

            if (lodGroupData.destroyedID.Length > 0)
                m_LODGroupProcessor.DestroyInstances(lodGroupData.destroyedID);

            if (rendererData.changedID.Length > 0)
            {
                Profiler.BeginSample("ProcessMeshRendererChanges");
                m_MeshRendererProcessor.ProcessGameObjectChanges(rendererData.changedID);
                Profiler.EndSample();
            }

            if (transformChanges.transformedID.Length > 0)
            {
                Profiler.BeginSample("ProcessMeshRendererTransformChanges");
                m_MeshRendererProcessor.ProcessGameObjectTransformChanges(transformChanges);
                Profiler.EndSample();
            }

            if (rendererData.destroyedID.Length > 0)
                m_MeshRendererProcessor.DestroyInstances(rendererData.destroyedID);

            Profiler.BeginSample("ProcessRendererMaterialAndMeshChanges");
            m_MeshRendererProcessor.ProcessRendererMaterialAndMeshChanges(rendererData.changedID,
                changedMaterials.AsArray(),
                changedMaterialDatas.AsArray(),
                changedMeshes.AsArray());
            Profiler.EndSample();

            try
            {
                ProcessUpdateBatches();
            }
            finally
            {
                // Clear everything in a finally block so that GRD does not attempt to process update batches each frame after an exception was thrown.
                // Since the update batches are only valid for a frame, this is always results in error spam.
                ClearUpdateBatches();
            }

            m_InstanceDataSystem.UpdateInstanceMotions();
            m_InstanceDataSystem.ValidateTotalTreeCount();

            unsupportedRenderers.Dispose();
            changedMaterials.Dispose();
            unsupportedMaterials.Dispose();
            destroyedMaterials.Dispose();
            changedMaterialDatas.Dispose();
            changedMeshes.Dispose();
            transformChanges.Dispose();
            rendererData.Dispose();
            lodGroupTransformData.Dispose();
            lodGroupData.Dispose();
            cameraData.Dispose();
            materialData.Dispose();
            meshDataSorted.Dispose();

            Profiler.EndSample();
        }

        public void PushMeshRendererUpdateBatches(NativeArray<MeshRendererUpdateBatch> batches)
        {
            foreach (var batch in batches)
            {
                batch.Validate();
                m_MeshRendererUpdateBatches.Add(batch);
            }
        }

        public void PushLODGroupUpdateBatches(NativeArray<LODGroupUpdateBatch> batches)
        {
            foreach (var batch in batches)
            {
                batch.Validate();
                m_LODGroupUpdateBatches.Add(batch);
            }
        }

        public void PushMeshRendererDeletionBatch(NativeArray<NativeArray<EntityId>> batches)
        {
            m_MeshRendererDeletionBatches.AddRange(batches);
        }

        public void PushLODGroupDeletionBatch(NativeArray<NativeArray<EntityId>> batches)
        {
            m_LODGroupDeletionBatches.AddRange(batches);
        }

        private void ProcessUpdateBatches()
        {
            foreach (var batch in m_LODGroupDeletionBatches)
                m_LODGroupProcessor.DestroyInstances(batch);

            foreach (var batch in m_MeshRendererDeletionBatches)
                m_MeshRendererProcessor.DestroyInstances(batch);

            // Update LODs before instances otherwise some LODGroupIDs might be unknown when updating the instances
            for (int i = 0; i < m_LODGroupUpdateBatches.Length; i++)
            {
                m_LODGroupProcessor.ProcessUpdateBatch(m_LODGroupUpdateBatches.ElementAt(i));
            }

            for (int i = 0; i < m_MeshRendererUpdateBatches.Length; i++)
            {
                m_MeshRendererProcessor.ProcessUpdateBatch(ref m_MeshRendererUpdateBatches.ElementAt(i));
            }
        }

        private void ClearUpdateBatches()
        {
            foreach (var batch in m_MeshRendererDeletionBatches)
                batch.Dispose();
            m_MeshRendererDeletionBatches.Clear();

            foreach (var batch in m_LODGroupDeletionBatches)
                batch.Dispose();
            m_LODGroupDeletionBatches.Clear();

            foreach (var batch in m_MeshRendererUpdateBatches)
                batch.Dispose();
            m_MeshRendererUpdateBatches.Clear();

            foreach (var batch in m_LODGroupUpdateBatches)
                batch.Dispose();
            m_LODGroupUpdateBatches.Clear();
        }

        public void ClassifyMaterials(NativeArray<EntityId> allChangedMaterials,
            NativeArray<EntityId> allDestroyedMaterials,
            out NativeList<EntityId> unsupportedMaterials,
            out NativeList<EntityId> changedMaterials,
            out NativeList<EntityId> destroyedMaterials,
            out NativeList<GPUDrivenMaterialData> changedMaterialDatas,
            Allocator allocator)
        {
            Profiler.BeginSample("ClassifyMaterials");

            WorldProcessorBurst.ClassifyMaterials(m_Batcher.materialMap,
                allChangedMaterials,
                allDestroyedMaterials,
                out changedMaterials,
                out unsupportedMaterials,
                out destroyedMaterials,
                out changedMaterialDatas,
                allocator);

            Profiler.EndSample();
        }

        public NativeList<EntityId> FindOnlyUsedMeshes(NativeArray<EntityId> changedMeshes, Allocator allocator)
        {
            NativeList<EntityId> usedMeshes;

            Profiler.BeginSample("FindOnlyUsedMeshes");
            WorldProcessorBurst.FindOnlyUsedMeshes(m_Batcher.meshMap, changedMeshes, allocator, out usedMeshes);
            Profiler.EndSample();

            return usedMeshes;
        }

        private NativeList<EntityId> FindUnsupportedRenderers(NativeArray<EntityId> unsupportedMaterials, Allocator allocator)
        {
            Profiler.BeginSample("FindUnsupportedRenderers");

            var unsupportedRenderers = new NativeList<EntityId>(allocator);

            if (unsupportedMaterials.Length > 0)
            {
                ref RenderWorld renderWorld = ref m_InstanceDataSystem.renderWorld;

                WorldProcessorBurst.FindUnsupportedRenderers(unsupportedMaterials, renderWorld.materialIDArrays, renderWorld.instanceIDs, ref unsupportedRenderers);
            }

            Profiler.EndSample();
            return unsupportedRenderers;
        }
    }
}
