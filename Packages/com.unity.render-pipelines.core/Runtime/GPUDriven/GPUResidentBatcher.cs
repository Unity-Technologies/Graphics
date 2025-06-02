using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Profiling;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    internal partial class GPUResidentBatcher : IDisposable
    {
        private RenderersBatchersContext m_BatchersContext;
        private GPUDrivenProcessor m_GPUDrivenProcessor;
        private GPUDrivenRendererDataCallback m_UpdateRendererInstancesAndBatchesCallback;
        private GPUDrivenRendererDataCallback m_UpdateRendererBatchesCallback;

        internal RenderersBatchersContext batchersContext { get => m_BatchersContext; }
        internal OcclusionCullingCommon occlusionCullingCommon { get => m_BatchersContext.occlusionCullingCommon; }
        internal InstanceCullingBatcher instanceCullingBatcher { get => m_InstanceCullingBatcher; }

        private InstanceCullingBatcher m_InstanceCullingBatcher = null;

        public GPUResidentBatcher(
            RenderersBatchersContext batcherContext,
            InstanceCullingBatcherDesc instanceCullerBatcherDesc,
            GPUDrivenProcessor gpuDrivenProcessor)
        {
            m_BatchersContext = batcherContext;
            m_GPUDrivenProcessor = gpuDrivenProcessor;
            m_UpdateRendererInstancesAndBatchesCallback = UpdateRendererInstancesAndBatches;
            m_UpdateRendererBatchesCallback = UpdateRendererBatches;

            m_InstanceCullingBatcher = new InstanceCullingBatcher(batcherContext, instanceCullerBatcherDesc, OnFinishedCulling);
        }

        public void Dispose()
        {
            m_GPUDrivenProcessor.ClearMaterialFilters();
            m_InstanceCullingBatcher.Dispose();

            if (m_ProcessedThisFrameTreeBits.IsCreated)
                m_ProcessedThisFrameTreeBits.Dispose();
        }

        public void OnBeginContextRendering()
        {
            if (m_ProcessedThisFrameTreeBits.IsCreated)
                m_ProcessedThisFrameTreeBits.Dispose();
        }

        public void OnEndContextRendering()
        {
            m_InstanceCullingBatcher?.OnEndContextRendering();
        }

        public void OnBeginCameraRendering(Camera camera)
        {
            m_InstanceCullingBatcher?.OnBeginCameraRendering(camera);
        }

        public void OnEndCameraRendering(Camera camera)
        {
            m_InstanceCullingBatcher?.OnEndCameraRendering(camera);
        }

        public void UpdateFrame()
        {
            m_InstanceCullingBatcher.UpdateFrame();
            m_BatchersContext.UpdateFrame();
        }

        public void DestroyMaterials(NativeArray<int> destroyedMaterials)
        {
            m_InstanceCullingBatcher.DestroyMaterials(destroyedMaterials);
        }

        public void DestroyDrawInstances(NativeArray<InstanceHandle> instances)
        {
            m_InstanceCullingBatcher.DestroyDrawInstances(instances);
        }

        public void DestroyMeshes(NativeArray<int> destroyedMeshes)
        {
            m_InstanceCullingBatcher.DestroyMeshes(destroyedMeshes);
        }

        internal void FreeRendererGroupInstances(NativeArray<int> rendererGroupIDs)
        {
            if (rendererGroupIDs.Length == 0)
                return;

            var instances = new NativeList<InstanceHandle>(rendererGroupIDs.Length, Allocator.TempJob);
            m_BatchersContext.ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instances).Complete();
            DestroyDrawInstances(instances.AsArray());
            instances.Dispose();

            m_BatchersContext.FreeRendererGroupInstances(rendererGroupIDs);
        }

        public void InstanceOcclusionTest(RenderGraph renderGraph, in OcclusionCullingSettings settings, ReadOnlySpan<SubviewOcclusionTest> subviewOcclusionTests)
        {
            if (!m_BatchersContext.hasBoundingSpheres)
                return;

            m_InstanceCullingBatcher.culler.InstanceOcclusionTest(renderGraph, settings, subviewOcclusionTests, m_BatchersContext);
        }

        public void UpdateInstanceOccluders(RenderGraph renderGraph, in OccluderParameters occluderParams, ReadOnlySpan<OccluderSubviewUpdate> occluderSubviewUpdates)
        {
            if (!m_BatchersContext.hasBoundingSpheres)
                return;

            m_BatchersContext.occlusionCullingCommon.UpdateInstanceOccluders(renderGraph, occluderParams, occluderSubviewUpdates);
        }

        public void UpdateRenderers(NativeArray<int> renderersID, bool materialUpdateOnly = false)
        {
            if (renderersID.Length == 0)
                return;

            m_GPUDrivenProcessor.enablePartialRendering = false;
            m_GPUDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(renderersID,
                materialUpdateOnly ? m_UpdateRendererBatchesCallback : m_UpdateRendererInstancesAndBatchesCallback, materialUpdateOnly);
            m_GPUDrivenProcessor.enablePartialRendering = false;
        }

#if UNITY_EDITOR
        public void UpdateSelectedRenderers(NativeArray<int> renderersID)
        {
            var instances = new NativeArray<InstanceHandle>(renderersID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_BatchersContext.ScheduleQueryRendererGroupInstancesJob(renderersID, instances).Complete();
            m_BatchersContext.UpdateSelectedInstances(instances);
            instances.Dispose();
        }
#endif

        public JobHandle SchedulePackedMaterialCacheUpdate(NativeArray<int> materialIDs,
            NativeArray<GPUDrivenPackedMaterialData> packedMaterialDatas)
        {
            return m_InstanceCullingBatcher.SchedulePackedMaterialCacheUpdate(materialIDs, packedMaterialDatas);
        }

        public void PostCullBeginCameraRendering(RenderRequestBatcherContext context)
        {
            m_InstanceCullingBatcher.PostCullBeginCameraRendering(context);
        }

        public void OnSetupAmbientProbe()
        {
            m_BatchersContext.UpdateAmbientProbeAndGpuBuffer(forceUpdate: false);
        }

        private void UpdateRendererInstancesAndBatches(in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials)
        {
            FreeRendererGroupInstances(rendererData.invalidRendererGroupID);

            if (rendererData.rendererGroupID.Length == 0)
                return;

            Profiler.BeginSample("GPUResidentInstanceBatcher.UpdateRendererInstancesAndBatches");
            {
                // --------------------------------------------------------------------------------------------------------------------------------------
                // Allocate and Update CPU instance data
                // --------------------------------------------------------------------------------------------------------------------------------------
                var instances = new NativeArray<InstanceHandle>(rendererData.localToWorldMatrix.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                Profiler.BeginSample("AllocateInstanceData");
                {
                    m_BatchersContext.ReallocateAndGetInstances(rendererData, instances);
                    var updateInstanceDataJob = m_BatchersContext.ScheduleUpdateInstanceDataJob(instances, rendererData);

                    GPUInstanceDataBufferUploader uploader = m_BatchersContext.CreateDataBufferUploader(instances.Length, InstanceType.MeshRenderer);
                    uploader.AllocateUploadHandles(instances.Length);
                    JobHandle writeJobHandle = default;
                    writeJobHandle = uploader.WriteInstanceDataJob(m_BatchersContext.renderersParameters.lightmapScale.index, rendererData.lightmapScaleOffset, rendererData.rendererGroupIndex);
                    writeJobHandle.Complete();

                    m_BatchersContext.SubmitToGpu(instances, ref uploader, submitOnlyWrittenParams: true);
                    m_BatchersContext.ChangeInstanceBufferVersion();
                    uploader.Dispose();

                    // --------------------------------------------------------------------------------------------------------------------------------------
                    // Update and Upload Transform data to GPU
                    // ----------------------------------------------------------------------------------------------------------------------------------
                    updateInstanceDataJob.Complete();
                    m_BatchersContext.InitializeInstanceTransforms(instances, rendererData.localToWorldMatrix, rendererData.prevLocalToWorldMatrix);

                }
                Profiler.EndSample();

                // --------------------------------------------------------------------------------------------------------------------------------------
                // Instance culling batcher
                // --------------------------------------------------------------------------------------------------------------------------------------

                Profiler.BeginSample("InstanceCullingBatcher.BuildBatch");
                {
                    m_InstanceCullingBatcher.BuildBatch(instances, rendererData, true);
                }
                Profiler.EndSample();

                instances.Dispose();
            }
            Profiler.EndSample();
        }

        private void UpdateRendererBatches(in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials)
        {
            if (rendererData.rendererGroupID.Length == 0)
                return;

            Profiler.BeginSample("GPUResidentInstanceBatcher.UpdateRendererBatches");
            {
                // --------------------------------------------------------------------------------------------------------------------------------------
                // Get Instances
                // --------------------------------------------------------------------------------------------------------------------------------------
                var instances = new NativeList<InstanceHandle>(rendererData.localToWorldMatrix.Length, Allocator.TempJob);

                Profiler.BeginSample("QueryInstances");
                {
                    m_BatchersContext.ScheduleQueryRendererGroupInstancesJob(rendererData.rendererGroupID, instances).Complete();
                }
                Profiler.EndSample();

                // --------------------------------------------------------------------------------------------------------------------------------------
                // Instance culling batcher
                // --------------------------------------------------------------------------------------------------------------------------------------

                Profiler.BeginSample("InstanceCullingBatcher.BuildBatch");
                {
                    m_InstanceCullingBatcher.BuildBatch(instances.AsArray(), rendererData, false);
                }
                Profiler.EndSample();

                instances.Dispose();
            }
            Profiler.EndSample();
        }

        private void OnFinishedCulling(IntPtr customCullingResult)
        {
            ProcessTrees();
            m_InstanceCullingBatcher.OnFinishedCulling(customCullingResult);
        }
    }
}
