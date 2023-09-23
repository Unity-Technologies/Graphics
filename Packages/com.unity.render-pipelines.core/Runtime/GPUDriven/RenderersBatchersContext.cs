using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    internal struct RenderersBatchersContextDesc
    {
        public int maxInstances;
        public bool supportDitheringCrossFade;
        public bool enableDeferredMaterialInstanceData;
        public bool enableBoundingSpheresInstanceData;
        public bool enableDeferredVertexDeformation;
        public bool enableDeferredMaterialPartialMeshConversion;

        public static RenderersBatchersContextDesc NewDefault()
        {
            return new RenderersBatchersContextDesc()
            {
                maxInstances = 1024,
                supportDitheringCrossFade = false,
                enableBoundingSpheresInstanceData = false,
                enableDeferredMaterialInstanceData = false,
                enableDeferredVertexDeformation = false,
                enableDeferredMaterialPartialMeshConversion = false
            };
        }
    }

    internal class RenderersBatchersContext : IDisposable
    {
        public int maxInstances { get { return m_InstanceDataBuffer.maxInstances; } }
        public RenderersParameters renderersParameters { get { return m_RenderersParameters; } }
        public GraphicsBuffer gpuInstanceDataBuffer { get { return m_InstanceDataBuffer.gpuBuffer; } }
        public GraphicsBuffer gpuLodDataBuffer { get { return m_LODGroupDataPool.lodCullingDataBuffer.gpuBuffer; } }
        public int gpuLodDataBufferAddress { get { return m_LODGroupDataPool.lodDataBufferAddress; } }
        public NativeArray<MetadataValue> defaultMetadata { get { return m_InstanceDataBuffer.defaultMetadata; } }
        public NativeArray<int> aliveInstanceIndices { get { return m_InstancePool.aliveInstanceIndices; } }
        public NativeArray<int> rendererInstanceIDs { get { return m_InstancePool.instanceDataArrays.rendererIDs.AsArray(); } }
        public InstanceDrawData instanceDrawData { get { return m_InstancePool.transformUpdater.drawData; } }
        public NativeList<LODGroupCullingData> lodGroupCullingData { get { return m_LODGroupDataPool.lodGroupCullingData; } }
        public int instanceDataBufferVersion { get { return m_InstanceDataBuffer.version; } }
        public int instanceDataBufferLayoutVersion { get { return m_InstanceDataBuffer.layoutVersion; } }
        public LightmapManager lightmapManager { get { return m_LightmapManager; } }
        public int crossfadedRendererCount { get { return m_LODGroupDataPool.crossfadedRendererCount; } }
        public SphericalHarmonicsL2 cachedAmbientProbe { get { return m_CachedAmbientProbe; } }
        public NativeArray<TransformIndex> transformIndices { get { return m_InstancePool.instanceDataArrays.transformIndices.AsArray(); } }
        public ParallelBitArray movedTransformIndices { get { return m_InstancePool.transformUpdater.movedTransformIndices; } }

        public bool enableDeferredVertexShader { get { return m_EnableDeferredVertexShader; } }
        public bool enableDeferredMaterialPartialMeshConversion { get { return m_EnableDeferredMaterialPartialMeshConversion; } }
        public bool enableGPUInstanceCulling { get { return m_EnableGPUInstanceCulling; } }
        public bool enableGPUInstanceOcclusionCulling { get { return m_EnableGPUInstanceOcclusionCulling; } }

        private GPUDrivenProcessor m_GPUDrivenProcessor;

        private LODGroupDataPool m_LODGroupDataPool;

        private LightmapManager m_LightmapManager;

        private GPURendererInstancePool m_InstancePool;
        internal GPUInstanceDataBuffer m_InstanceDataBuffer;
        private RenderersParameters m_RenderersParameters;
        private GPUInstanceDataBufferUploader.GPUResources m_UploadResources;
        private GPUInstanceDataBufferGrower.GPUResources m_GrowerResources;

        internal CommandBuffer m_CmdBuffer;

        private SphericalHarmonicsL2 m_CachedAmbientProbe;

        private bool m_EnableDeferredVertexShader;
        private bool m_EnableDeferredMaterialPartialMeshConversion;
        private bool m_EnableGPUInstanceCulling;
        private bool m_EnableGPUInstanceOcclusionCulling;

        public RenderersBatchersContext(in RenderersBatchersContextDesc desc, GPUDrivenProcessor gpuDrivenProcessor, GPUResidentDrawerResources resources)
        {
            m_GPUDrivenProcessor = gpuDrivenProcessor;

            RenderersParameters.Flags rendererParametersFlags = RenderersParameters.Flags.None;
            if (desc.enableBoundingSpheresInstanceData)
                rendererParametersFlags |= RenderersParameters.Flags.UseBoundingSphereParameter;
            if (desc.enableDeferredMaterialInstanceData)
                rendererParametersFlags |= RenderersParameters.Flags.UseDeferredMaterialInstanceParameter;

            m_InstanceDataBuffer = RenderersParameters.CreateInstanceDataBuffer(desc.maxInstances, rendererParametersFlags);
            m_RenderersParameters = new RenderersParameters(m_InstanceDataBuffer);
            m_InstancePool = new GPURendererInstancePool(m_InstanceDataBuffer.maxInstances, desc.enableBoundingSpheresInstanceData, resources);
            m_LODGroupDataPool = new LODGroupDataPool(resources, desc.maxInstances, desc.supportDitheringCrossFade, desc.enableDeferredMaterialInstanceData);
            m_UploadResources = new GPUInstanceDataBufferUploader.GPUResources();
            m_UploadResources.LoadShaders(resources);

            m_GrowerResources = new GPUInstanceDataBufferGrower.GPUResources();
            m_GrowerResources.LoadShaders(resources);

            m_CmdBuffer = new CommandBuffer();
            m_CmdBuffer.name = "GPUCullingCommands";

            m_CachedAmbientProbe = RenderSettings.ambientProbe;

            m_LightmapManager = new LightmapManager();

            m_EnableDeferredVertexShader = desc.enableDeferredVertexDeformation;
            m_EnableDeferredMaterialPartialMeshConversion = desc.enableDeferredMaterialPartialMeshConversion;
            m_EnableGPUInstanceCulling = false; //TODO: Simon brown, restore settings and GPU culling / occlusion culling
            m_EnableGPUInstanceOcclusionCulling = false;
        }

        public void Dispose()
        {
            m_GPUDrivenProcessor.DisableGPUDrivenRendering(m_InstancePool.instanceDataArrays.rendererIDs.AsArray());

            m_CmdBuffer.Release();
            m_GrowerResources.Dispose();
            m_UploadResources.Dispose();
            m_InstancePool.Dispose();
            m_LODGroupDataPool.Dispose();
            m_InstanceDataBuffer.Dispose();
            m_LightmapManager.Dispose();
        }

        public void GrowInstanceBuffer(int newLength)
        {
            m_InstancePool.Resize(newLength);

            using (var grower = new GPUInstanceDataBufferGrower(m_InstanceDataBuffer, newLength))
            {
                var newInstanceDataBuffer = grower.SubmitToGpu(ref m_GrowerResources);

                if (newInstanceDataBuffer != m_InstanceDataBuffer)
                {
                    if (m_InstanceDataBuffer != null)
                        m_InstanceDataBuffer.Dispose();

                    m_InstanceDataBuffer = newInstanceDataBuffer;
                }
            }

            m_RenderersParameters = new RenderersParameters(m_InstanceDataBuffer);
        }

        public void EnsureSpaceForNewInstances(int numNewInstances)
        {
            var requiredLength = m_InstancePool.aliveInstanceIndices.Length + numNewInstances;

            if (requiredLength > m_InstancePool.maxInstanceCount)
                GrowInstanceBuffer(requiredLength + 1024);
        }

        public void DestroyLODGroups(NativeArray<int> destroyed)
        {
            m_LODGroupDataPool.FreeLODGroupData(destroyed);
        }

        public void UpdateLODGroups(NativeArray<int> changedID)
        {
            if (changedID.Length == 0)
                return;

            m_GPUDrivenProcessor.DispatchLODGroupData(changedID, (GPUDrivenLODGroupData lodGroupsData) =>
            {
                Profiler.BeginSample("ConvertLODGroupsToBRG");

                m_LODGroupDataPool.UpdateLODGroupData(lodGroupsData);

                Profiler.EndSample();
            });
        }

        public InstanceHandle GetInstanceHandle(int rendererID)
        {
            return m_InstancePool.GetInstanceHandle(rendererID);
        }

        public void AllocateOrGetInstances(NativeArray<int> renderersID, NativeArray<InstanceHandle> instances)
        {
            Assert.AreEqual(renderersID.Length, instances.Length);

            int numNewInstances = m_InstancePool.QueryInstances(renderersID, instances);

            EnsureSpaceForNewInstances(numNewInstances);

            m_InstancePool.AllocateInstances(renderersID, instances, numNewInstances);
        }

        public void UpdateInstanceData(NativeArray<InstanceHandle> instances, in GPUDrivenRendererData rendererData)
        {
            m_InstancePool.UpdateInstanceData(instances, rendererData, m_LODGroupDataPool.lodGroupDataHash);
        }

        public void FreeInstances(NativeArray<InstanceHandle> instances)
        {
            m_InstancePool.FreeInstances(instances);
        }

        public void QueryInstanceData(NativeArray<int> changedRenderers, NativeArray<int> destroyedRenderers, NativeArray<int> transformedRenderers, NativeArray<int> changedMeshesSorted, NativeArray<int> destroyedMeshesSorted,
            NativeArray<InstanceHandle> outChangedRendererInstances, NativeArray<InstanceHandle> outDestroyedRendererInstances, NativeArray<InstanceHandle> outTransformedInstances,
            NativeList<KeyValuePair<InstanceHandle, int>> outChangedMeshInstanceIndexPairs, NativeList<InstanceHandle> outDestroyedMeshInstances)
        {
            m_InstancePool.QueryInstanceData(changedRenderers, destroyedRenderers, transformedRenderers, changedMeshesSorted, destroyedMeshesSorted,
                outChangedRendererInstances, outDestroyedRendererInstances, outTransformedInstances,
                outChangedMeshInstanceIndexPairs, outDestroyedMeshInstances);
        }

        public int GetRendererInstanceID(InstanceHandle instance)
        {
            return m_InstancePool.GetRendererInstanceID(instance);
        }

        public TransformIndex GetTransformIndex(InstanceHandle instance)
        {
            return m_InstancePool.GetTransformIndex(instance);
        }

        public Matrix4x4 GetInstanceLocalToWorldMatrix(InstanceHandle instance)
        {
            return m_InstancePool.GetInstanceLocalToWorldMatrix(instance);
        }

        public void ChangeInstanceBufferVersion()
        {
            ++m_InstanceDataBuffer.version;
        }

        public GPUInstanceDataBufferUploader CreateDataBufferUploader(int capacity)
        {
            return new GPUInstanceDataBufferUploader(m_InstanceDataBuffer.descriptions, capacity);
        }

        public void SubmitToGpu(NativeArray<InstanceHandle> instances, ref GPUInstanceDataBufferUploader uploader)
        {
            uploader.SubmitToGpu(m_InstanceDataBuffer, instances, ref m_UploadResources);
        }

        public void ReinitializeInstanceTransforms(NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices, NativeArray<Matrix4x4> prevLocalToWorldMatrices)
        {
            //Order matters  - transforms should be updated before ambient probe for correct AABB
            m_InstancePool.ReinitializeInstanceTransforms(instances, localToWorldMatrices, prevLocalToWorldMatrices, m_RenderersParameters, m_InstanceDataBuffer.gpuBuffer);
            ChangeInstanceBufferVersion();
        }

        public void TransformInstances(NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices)
        {
            //Order matters  - transforms should be updated before ambient probe for correct AABB
            m_InstancePool.UpdateInstanceTransforms(instances, localToWorldMatrices, m_RenderersParameters, m_InstanceDataBuffer.gpuBuffer);
            ChangeInstanceBufferVersion();
        }

        public void UpdateAmbientProbeAndGpuBuffer(SphericalHarmonicsL2 ambientProbe, bool forceUpdate = false)
        {
            if (m_CachedAmbientProbe != ambientProbe || forceUpdate)
            {
                m_CachedAmbientProbe = ambientProbe;

                m_InstancePool.UpdateAllInstanceProbes(m_RenderersParameters, m_InstanceDataBuffer.gpuBuffer);
                ChangeInstanceBufferVersion();
            }
        }

        public void TransformLODGroups(NativeArray<int> lodGroupsID)
        {
            if (lodGroupsID.Length == 0)
                return;

            m_GPUDrivenProcessor.DispatchLODGroupData(lodGroupsID, (GPUDrivenLODGroupData lodGroupsData) =>
            {
                m_LODGroupDataPool.UpdateLODGroupTransformData(lodGroupsData);
            });
        }
    }
}
