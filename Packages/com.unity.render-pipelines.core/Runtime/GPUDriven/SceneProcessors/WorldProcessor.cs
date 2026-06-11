#if !UNITY_WEBGL_RENDERER_ONLY
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Profiling.LowLevel;

namespace UnityEngine.Rendering
{
    internal class WorldProcessor : IDisposable
    {
        /// <summary>
        /// Runs the full per-frame world update: fetches all pending scene changes, processes
        /// camera, LOD group, mesh renderer, material, and mesh events, then flushes all queued
        /// update batches and motion updates.
        /// </summary>
        internal static readonly ProfilerMarker k_Update =
            new ProfilerMarker(ProfilerCategory.Render, "WorldProcessor.Update");

        /// <summary>
        /// Drains all pending change records from the <c>ObjectDispatcher</c> for meshes,
        /// materials, cameras, LOD groups, mesh renderers, and their transforms into temporary
        /// native arrays for processing this frame.
        /// </summary>
        static readonly ProfilerMarker k_FetchAllChanges =
            new ProfilerMarker(ProfilerCategory.Render, "FetchAllChanges", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Registers newly added cameras with and removes destroyed cameras from the instance
        /// data system.
        /// </summary>
        static readonly ProfilerMarker k_ProcessCameraChanges =
            new ProfilerMarker(ProfilerCategory.Render, "ProcessCameraChanges", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Disables GPU-driven rendering for renderers that use unsupported materials, then
        /// destroys their GPU and CPU instances.
        /// </summary>
        static readonly ProfilerMarker k_DestroyUnsupportedRenderers =
            new ProfilerMarker(ProfilerCategory.Render, "DestroyUnsupportedRenderers", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Queries all renderer instances that reference each destroyed mesh, removes their draw
        /// instances from the culling batcher, then unregisters the meshes from the
        /// BatchRendererGroup.
        /// </summary>
        static readonly ProfilerMarker k_DestroyMeshes =
            new ProfilerMarker(ProfilerCategory.Render, "DestroyMeshes", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Fetches and processes full LOD group data (settings, bounds, and LOD buffer) for all
        /// structurally changed LOD groups, allocating new GPU instances as needed.
        /// </summary>
        static readonly ProfilerMarker k_ProcessLODGroupChanges =
            new ProfilerMarker(ProfilerCategory.Render, "ProcessLODGroupChanges", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Updates world-space reference point and size for LOD groups whose transforms changed
        /// without structural changes, skipping full reallocation.
        /// </summary>
        static readonly ProfilerMarker k_ProcessLODGroupTransformChanges =
            new ProfilerMarker(ProfilerCategory.Render, "ProcessLODGroupTransformChanges", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Fetches full renderer data and dispatches a mesh renderer update batch for all
        /// structurally changed renderers, including instance allocation and draw batch rebuild.
        /// </summary>
        static readonly ProfilerMarker k_ProcessMeshRendererChanges =
            new ProfilerMarker(ProfilerCategory.Render, "ProcessMeshRendererChanges", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Dispatches a transform-only mesh renderer update for renderers whose local-to-world
        /// matrices changed without structural changes, skipping instance reallocation.
        /// </summary>
        static readonly ProfilerMarker k_ProcessMeshRendererTransformChanges =
            new ProfilerMarker(ProfilerCategory.Render, "ProcessMeshRendererTransformChanges", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Finds all renderers affected by changed materials or meshes, then rebuilds their draw
        /// batches. Cost scales with total registered renderer count when scanning for affected
        /// renderers, not just the number of changed assets.
        /// </summary>
        static readonly ProfilerMarker k_ProcessRendererMaterialAndMeshChanges =
            new ProfilerMarker(ProfilerCategory.Render, "ProcessRendererMaterialAndMeshChanges", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Partitions changed and destroyed materials into supported, unsupported, and destroyed
        /// buckets, and extracts GPU-relevant material data for the supported subset.
        /// </summary>
        static readonly ProfilerMarker k_ClassifyMaterials =
            new ProfilerMarker(ProfilerCategory.Render, "ClassifyMaterials", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Filters the changed mesh ID list down to meshes that are actually referenced by
        /// registered renderers, discarding untracked assets early to avoid redundant processing.
        /// </summary>
        static readonly ProfilerMarker k_FindOnlyUsedMeshes =
            new ProfilerMarker(ProfilerCategory.Render, "FindOnlyUsedMeshes", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Scans all registered renderer instances to find those referencing any newly
        /// unsupported material. Cost scales with total registered renderer count, not just the
        /// number of unsupported materials.
        /// </summary>
        static readonly ProfilerMarker k_FindUnsupportedRenderers =
            new ProfilerMarker(ProfilerCategory.Render, "FindUnsupportedRenderers", MarkerFlags.VerbosityAdvanced);

        private GPUDrivenProcessor m_GPUDrivenProcessor;
        private ObjectDispatcher m_ObjectDispatcher;
        private GPUResidentContext m_GRDContext;
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
            m_GRDContext = context;
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
            using var _ = k_Update.Auto();

            TypeDispatchData meshDataSorted = default;
            TypeDispatchData materialData = default;
            TypeDispatchData cameraData = default;
            TypeDispatchData lodGroupData = default;
            TransformDispatchData lodGroupTransformData = default;
            TypeDispatchData rendererData = default;
            TransformDispatchData transformChanges = default;

            // Variables assigned outside the using block so they remain in scope below;
            // the fetch calls are native allocations and do not throw in practice, but we
            // still want the marker closed even if an unexpected exception occurs.
            using (k_FetchAllChanges.Auto())
            {
                meshDataSorted = m_ObjectDispatcher.GetTypeChangesAndClear<Mesh>(Allocator.TempJob, sortByInstanceID: true, noScriptingArray: true);
                materialData = m_ObjectDispatcher.GetTypeChangesAndClear<Material>(Allocator.TempJob, noScriptingArray: true);
                cameraData = m_ObjectDispatcher.GetTypeChangesAndClear<Camera>(Allocator.TempJob, noScriptingArray: true);
                lodGroupData = m_ObjectDispatcher.GetTypeChangesAndClear<LODGroup>(Allocator.TempJob, noScriptingArray: true);
                lodGroupTransformData = m_ObjectDispatcher.GetTransformChangesAndClear<LODGroup>(ObjectDispatcher.TransformTrackingType.GlobalTRS, Allocator.TempJob);
                rendererData = m_ObjectDispatcher.GetTypeChangesAndClear<MeshRenderer>(Allocator.TempJob, noScriptingArray: true);
                transformChanges = m_ObjectDispatcher.GetTransformChangesAndClear<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS, Allocator.TempJob);
            }

            if (cameraData.changedID.Length > 0)
            {
                using (k_ProcessCameraChanges.Auto())
                {
                    m_InstanceDataSystem.AddCameras(cameraData.changedID);
                    m_InstanceDataSystem.RemoveCameras(cameraData.destroyedID);
                }
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
                using (k_DestroyUnsupportedRenderers.Auto())
                {
                    m_GPUDrivenProcessor.DisableGPUDrivenRendering(unsupportedRenderers.AsArray());
                    m_MeshRendererProcessor.DestroyInstances(unsupportedRenderers.AsArray());
#if ENABLE_PROFILER
                    // These renderers still exist in the scene but cannot be on the GRD path due to
                    // unsupported materials. Record them as excluded so coverage stats stay accurate.
                    // ClassifyMaterials returns "unsupported" without distinguishing missing shader vs
                    // failed IsShaderSupported(). The latter (missing DOTS_INSTANCING_ON) dominates this
                    // runtime-material-change path in practice, so MissingDOTSInstancing is the catch-all
                    // label here. Rare null-shader cases are reported as MissingDOTSInstancing as well.
                    var advStats = m_GRDContext.advancedDebugStats;
                    if (advStats != null)
                    {
                        advStats.UnregisterRenderers(unsupportedRenderers.AsArray());
                        advStats.RegisterExcludedRenderers(unsupportedRenderers.AsArray(), GRDExclusionReason.MissingDOTSInstancing);
                    }
#endif
                }
            }

            m_Batcher.DestroyMaterials(destroyedMaterials.AsArray());
            m_Batcher.DestroyMaterials(unsupportedMaterials.AsArray());

            if (meshDataSorted.destroyedID.Length > 0)
            {
                using (k_DestroyMeshes.Auto())
                {
                    var destroyedMeshInstances = new NativeList<InstanceHandle>(Allocator.TempJob);
                    m_InstanceDataSystem.ScheduleQuerySortedMeshInstancesJob(meshDataSorted.destroyedID, destroyedMeshInstances).Complete();
                    m_Batcher.DestroyDrawInstances(destroyedMeshInstances.AsArray());
                    //@ Check if we need to update instance bounds and light probe sampling positions after mesh is destroyed.
                    m_Batcher.DestroyMeshes(meshDataSorted.destroyedID);
                    destroyedMeshInstances.Dispose();
                }
            }

            if (lodGroupData.changedID.Length > 0)
            {
                using (k_ProcessLODGroupChanges.Auto())
                {
                    m_LODGroupProcessor.ProcessGameObjectChanges(lodGroupData.changedID, transformOnly: false);
                }
            }

            if (lodGroupTransformData.transformedID.Length > 0)
            {
                using (k_ProcessLODGroupTransformChanges.Auto())
                {
                    m_LODGroupProcessor.ProcessGameObjectChanges(lodGroupTransformData.transformedID, transformOnly: true);
                }
            }

            if (lodGroupData.destroyedID.Length > 0)
                m_LODGroupProcessor.DestroyInstances(lodGroupData.destroyedID);

            if (rendererData.changedID.Length > 0)
            {
                using (k_ProcessMeshRendererChanges.Auto())
                {
                    m_MeshRendererProcessor.ProcessGameObjectChanges(rendererData.changedID);
                }
            }

            if (transformChanges.transformedID.Length > 0)
            {
                using (k_ProcessMeshRendererTransformChanges.Auto())
                {
                    m_MeshRendererProcessor.ProcessGameObjectTransformChanges(transformChanges);
                }
            }

            if (rendererData.destroyedID.Length > 0)
            {
                m_MeshRendererProcessor.DestroyInstances(rendererData.destroyedID);
#if ENABLE_PROFILER
                m_GRDContext.advancedDebugStats?.UnregisterRenderers(rendererData.destroyedID);
#endif
            }

            using (k_ProcessRendererMaterialAndMeshChanges.Auto())
            {
                m_MeshRendererProcessor.ProcessRendererMaterialAndMeshChanges(rendererData.changedID,
                    changedMaterials.AsArray(),
                    changedMaterialDatas.AsArray(),
                    changedMeshes.AsArray());
            }

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
            {
                m_MeshRendererProcessor.DestroyInstances(batch);
#if ENABLE_PROFILER
                m_GRDContext.advancedDebugStats?.UnregisterRenderers(batch);
#endif
            }

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
            using (k_ClassifyMaterials.Auto())
            {
                WorldProcessorBurst.ClassifyMaterials(m_Batcher.materialMap,
                    allChangedMaterials,
                    allDestroyedMaterials,
                    out changedMaterials,
                    out unsupportedMaterials,
                    out destroyedMaterials,
                    out changedMaterialDatas,
                    allocator);
            }
        }

        public NativeList<EntityId> FindOnlyUsedMeshes(NativeArray<EntityId> changedMeshes, Allocator allocator)
        {
            NativeList<EntityId> usedMeshes;

            using (k_FindOnlyUsedMeshes.Auto())
            {
                WorldProcessorBurst.FindOnlyUsedMeshes(m_Batcher.meshMap, changedMeshes, allocator, out usedMeshes);
            }

            return usedMeshes;
        }

        private NativeList<EntityId> FindUnsupportedRenderers(NativeArray<EntityId> unsupportedMaterials, Allocator allocator)
        {
            NativeList<EntityId> unsupportedRenderers;

            using (k_FindUnsupportedRenderers.Auto())
            {
                unsupportedRenderers = new NativeList<EntityId>(allocator);

                if (unsupportedMaterials.Length > 0)
                {
                    ref RenderWorld renderWorld = ref m_InstanceDataSystem.renderWorld;

                    WorldProcessorBurst.FindUnsupportedRenderers(unsupportedMaterials, renderWorld.materialIDArrays, renderWorld.instanceIDs, ref unsupportedRenderers);
                }
            }

            return unsupportedRenderers;
        }
    }
}

#endif // !UNITY_WEBGL_RENDERER_ONLY
