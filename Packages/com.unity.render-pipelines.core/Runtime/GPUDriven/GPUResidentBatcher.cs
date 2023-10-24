using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Burst;
using UnityEngine.Profiling;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal class GPUResidentBatcher : IDisposable
    {
        private RenderersBatchersContext m_BatchersContext;
        private GPUDrivenProcessor m_GPUDrivenProcessor;
        private GPUDrivenRendererDataCallback m_UpdateRendererDataCallback;

        internal InstanceCullingBatcher instanceCullingBatcher { get => m_InstanceCullingBatcher; }

        private InstanceCullingBatcher m_InstanceCullingBatcher = null;

        public GPUResidentBatcher(
            RenderersBatchersContext batcherContext,
            InstanceCullingBatcherDesc instanceCullerBatcherDesc,
            GPUDrivenProcessor gpuDrivenProcessor)
        {
            m_BatchersContext = batcherContext;
            m_GPUDrivenProcessor = gpuDrivenProcessor;
            m_UpdateRendererDataCallback = UpdateRendererData;

            m_InstanceCullingBatcher = new InstanceCullingBatcher(batcherContext, instanceCullerBatcherDesc);
        }

        public void Dispose()
        {
            m_GPUDrivenProcessor.ClearMaterialFilters();
            m_InstanceCullingBatcher.Dispose();
        }

        public void OnBeginContextRendering()
        {
        }

        public void OnEndContextRendering()
        {
            m_InstanceCullingBatcher?.OnEndContextRendering();
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

        public void DestroyInstances(NativeArray<InstanceHandle> instances)
        {
            m_InstanceCullingBatcher.DestroyInstances(instances);
        }

        public void DestroyMeshes(NativeArray<int> destroyedMeshes)
        {
            m_InstanceCullingBatcher.DestroyMeshes(destroyedMeshes);
        }

        public void UpdateRenderers(NativeArray<int> renderersID)
        {
            if (renderersID.Length == 0)
                return;

            m_GPUDrivenProcessor.enablePartialRendering = false;
            m_GPUDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(renderersID, m_UpdateRendererDataCallback);
            m_GPUDrivenProcessor.enablePartialRendering = false;
        }

        public void PostCullBeginCameraRendering(RenderRequestBatcherContext context)
        {
            RenderSettings.ambientProbe = context.ambientProbe;
            m_BatchersContext.UpdateAmbientProbeAndGpuBuffer(context.ambientProbe);
            m_InstanceCullingBatcher.PostCullBeginCameraRendering(context);
        }

        private void UpdateRendererData(in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials)
        {
            if (rendererData.rendererGroupID.Length == 0)
                return;

            Profiler.BeginSample("GPUResidentInstanceBatcher.UpdateRendererData");
            {
                var usedMaterialIDs = new NativeList<int>(Allocator.TempJob);
                usedMaterialIDs.AddRange(rendererData.materialID);
                NativeParallelHashMap<LightmapManager.RendererSubmeshPair, int> rendererToMaterialMap = new();
                NativeArray<float4> lightMapTextureIndices = new();

                // ----------------------------------------------------------------------------------------------------------------------------------
                // Register lightmaps.
                // ----------------------------------------------------------------------------------------------------------------------------------
                Profiler.BeginSample("GenerateLightmappingData");
                {
                    rendererToMaterialMap = m_BatchersContext.lightmapManager.GenerateLightmappingData(rendererData, materials, usedMaterialIDs);
                    Profiler.BeginSample("GetLightmapTextureIndex");
                    lightMapTextureIndices = new NativeArray<float4>(rendererData.rendererGroupID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    m_BatchersContext.lightmapManager.GetLightmapTextureIndices(rendererData, lightMapTextureIndices);
                    Profiler.EndSample();
                }
                Profiler.EndSample();

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
                    writeJobHandle = uploader.WriteInstanceDataJob(m_BatchersContext.renderersParameters.lightmapIndex.index, lightMapTextureIndices, rendererData.rendererGroupIndex);
                    writeJobHandle = JobHandle.CombineDependencies(uploader.WriteInstanceDataJob(m_BatchersContext.renderersParameters.lightmapScale.index, rendererData.lightmapScaleOffset, rendererData.rendererGroupIndex), writeJobHandle);

                    writeJobHandle.Complete();

                    m_BatchersContext.SubmitToGpu(instances, ref uploader, submitOnlyWrittenParams: true);
                    m_BatchersContext.ChangeInstanceBufferVersion();
                    lightMapTextureIndices.Dispose();
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
                    m_InstanceCullingBatcher.BuildBatch(
                        instances,
                        usedMaterialIDs.AsArray(),
                        rendererData.meshID,
                        rendererToMaterialMap,
                        rendererData);

                }
                Profiler.EndSample();

                instances.Dispose();
                usedMaterialIDs.Dispose();
            }
            Profiler.EndSample();
        }
    }
}
