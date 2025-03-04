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
        public InstanceNumInfo instanceNumInfo;
        public bool supportDitheringCrossFade;
        public bool enableBoundingSpheresInstanceData;
        public float smallMeshScreenPercentage;
        public bool enableCullerDebugStats;

        public static RenderersBatchersContextDesc NewDefault()
        {
            return new RenderersBatchersContextDesc()
            {
                instanceNumInfo = new InstanceNumInfo(meshRendererNum: 1024, speedTreeNum: 32),
            };
        }
    }

    internal class RenderersBatchersContext : IDisposable
    {
        public RenderersParameters renderersParameters { get { return m_RenderersParameters; } }
        public GraphicsBuffer gpuInstanceDataBuffer { get { return m_InstanceDataBuffer.gpuBuffer; } }
        public int activeLodGroupCount { get { return m_LODGroupDataPool.activeLodGroupCount; } }
        public NativeArray<GPUInstanceComponentDesc>.ReadOnly defaultDescriptions { get { return m_InstanceDataBuffer.descriptions.AsReadOnly(); } }
        public NativeArray<MetadataValue> defaultMetadata { get { return m_InstanceDataBuffer.defaultMetadata; } }
        public NativeList<LODGroupCullingData> lodGroupCullingData { get { return m_LODGroupDataPool.lodGroupCullingData; } }
        public int instanceDataBufferVersion { get { return m_InstanceDataBuffer.version; } }
        public int instanceDataBufferLayoutVersion { get { return m_InstanceDataBuffer.layoutVersion; } }
        public SphericalHarmonicsL2 cachedAmbientProbe { get { return m_CachedAmbientProbe; } }

        public bool hasBoundingSpheres { get { return m_InstanceDataSystem.hasBoundingSpheres; } }
        public int cameraCount { get { return m_InstanceDataSystem.cameraCount; } }
        public CPUInstanceData.ReadOnly instanceData { get { return m_InstanceDataSystem.instanceData; } }
        public CPUSharedInstanceData.ReadOnly sharedInstanceData { get { return m_InstanceDataSystem.sharedInstanceData; } }

        public CPUPerCameraInstanceData perCameraInstanceData { get { return m_InstanceDataSystem.perCameraInstanceData; } }
        public GPUInstanceDataBuffer.ReadOnly instanceDataBuffer { get { return m_InstanceDataBuffer.AsReadOnly(); } }
        public NativeArray<InstanceHandle> aliveInstances { get { return m_InstanceDataSystem.aliveInstances; } }

        public float smallMeshScreenPercentage { get { return m_SmallMeshScreenPercentage; } }

        private InstanceDataSystem m_InstanceDataSystem;

        public GPUResidentDrawerResources resources { get { return m_Resources; } }

        private GPUResidentDrawerResources m_Resources;
        private GPUDrivenProcessor m_GPUDrivenProcessor;

        private LODGroupDataPool m_LODGroupDataPool;

        internal GPUInstanceDataBuffer m_InstanceDataBuffer;
        private RenderersParameters m_RenderersParameters;
        private GPUInstanceDataBufferUploader.GPUResources m_UploadResources;
        private GPUInstanceDataBufferGrower.GPUResources m_GrowerResources;

        internal CommandBuffer m_CmdBuffer;

        private SphericalHarmonicsL2 m_CachedAmbientProbe;

        private float m_SmallMeshScreenPercentage;

        private GPUDrivenLODGroupDataCallback m_UpdateLODGroupCallback;
        private GPUDrivenLODGroupDataCallback m_TransformLODGroupCallback;

        private OcclusionCullingCommon m_OcclusionCullingCommon;
        private DebugRendererBatcherStats m_DebugStats;

        internal OcclusionCullingCommon occlusionCullingCommon { get => m_OcclusionCullingCommon; }
        internal DebugRendererBatcherStats debugStats { get => m_DebugStats; }

        public RenderersBatchersContext(in RenderersBatchersContextDesc desc, GPUDrivenProcessor gpuDrivenProcessor, GPUResidentDrawerResources resources)
        {
            m_Resources = resources;
            m_GPUDrivenProcessor = gpuDrivenProcessor;

            RenderersParameters.Flags rendererParametersFlags = RenderersParameters.Flags.None;
            if (desc.enableBoundingSpheresInstanceData)
                rendererParametersFlags |= RenderersParameters.Flags.UseBoundingSphereParameter;

            m_InstanceDataBuffer = RenderersParameters.CreateInstanceDataBuffer(rendererParametersFlags, desc.instanceNumInfo);
            m_RenderersParameters = new RenderersParameters(m_InstanceDataBuffer);
            m_LODGroupDataPool = new LODGroupDataPool(resources, desc.instanceNumInfo.GetInstanceNum(InstanceType.MeshRenderer), desc.supportDitheringCrossFade);
            m_UploadResources = new GPUInstanceDataBufferUploader.GPUResources();
            m_UploadResources.LoadShaders(resources);

            m_GrowerResources = new GPUInstanceDataBufferGrower.GPUResources();
            m_GrowerResources.LoadShaders(resources);

            m_CmdBuffer = new CommandBuffer();
            m_CmdBuffer.name = "GPUCullingCommands";

            m_CachedAmbientProbe = RenderSettings.ambientProbe;

            m_InstanceDataSystem = new InstanceDataSystem(desc.instanceNumInfo.GetTotalInstanceNum(), desc.enableBoundingSpheresInstanceData, resources);
            m_SmallMeshScreenPercentage = desc.smallMeshScreenPercentage;

            m_UpdateLODGroupCallback = UpdateLODGroupData;
            m_TransformLODGroupCallback = TransformLODGroupData;

            m_OcclusionCullingCommon = new OcclusionCullingCommon();
            m_OcclusionCullingCommon.Init(resources);
            m_DebugStats = desc.enableCullerDebugStats ? new DebugRendererBatcherStats() : null;
        }

        public void Dispose()
        {
            NativeArray<int>.ReadOnly rendererGroupIDs = m_InstanceDataSystem.sharedInstanceData.rendererGroupIDs;

            if (rendererGroupIDs.Length > 0)
                m_GPUDrivenProcessor.DisableGPUDrivenRendering(rendererGroupIDs);

            m_InstanceDataSystem.Dispose();

            m_CmdBuffer.Release();
            m_GrowerResources.Dispose();
            m_UploadResources.Dispose();
            m_LODGroupDataPool.Dispose();
            m_InstanceDataBuffer.Dispose();

            m_UpdateLODGroupCallback = null;
            m_TransformLODGroupCallback = null;
            m_DebugStats?.Dispose();
            m_DebugStats = null;
            m_OcclusionCullingCommon?.Dispose();
            m_OcclusionCullingCommon = null;
        }

        public int GetMaxInstancesOfType(InstanceType instanceType)
        {
            return m_InstanceDataSystem.GetMaxInstancesOfType(instanceType);
        }

        public int GetAliveInstancesOfType(InstanceType instanceType)
        {
            return m_InstanceDataSystem.GetAliveInstancesOfType(instanceType);
        }

        public void GrowInstanceBuffer(in InstanceNumInfo instanceNumInfo)
        {
            using (var grower = new GPUInstanceDataBufferGrower(m_InstanceDataBuffer, instanceNumInfo))
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

        private void EnsureInstanceBufferCapacity()
        {
            const int kMeshRendererGrowNum = 1024;
            const int kSpeedTreeGrowNum = 256;

            int maxCPUMeshRendererNum = m_InstanceDataSystem.GetMaxInstancesOfType(InstanceType.MeshRenderer);
            int maxCPUSpeedTreeNum = m_InstanceDataSystem.GetMaxInstancesOfType(InstanceType.SpeedTree);

            int maxGPUMeshRendererInstances = m_InstanceDataBuffer.instanceNumInfo.GetInstanceNum(InstanceType.MeshRenderer);
            int maxGPUSpeedTreeInstances = m_InstanceDataBuffer.instanceNumInfo.GetInstanceNum(InstanceType.SpeedTree);

            bool needToGrow = false;

            if(maxCPUMeshRendererNum > maxGPUMeshRendererInstances)
            {
                needToGrow = true;
                maxGPUMeshRendererInstances = maxCPUMeshRendererNum + kMeshRendererGrowNum;
            }
            if(maxCPUSpeedTreeNum > maxGPUSpeedTreeInstances)
            {
                needToGrow = true;
                maxGPUSpeedTreeInstances = maxCPUSpeedTreeNum + kSpeedTreeGrowNum;
            }

            if (needToGrow)
                GrowInstanceBuffer(new InstanceNumInfo(meshRendererNum: maxGPUMeshRendererInstances, speedTreeNum: maxGPUSpeedTreeInstances));
        }

        private void UpdateLODGroupData(in GPUDrivenLODGroupData lodGroupData)
        {
            Profiler.BeginSample("Convert LODGroups To BRG");

            m_LODGroupDataPool.UpdateLODGroupData(lodGroupData);

            Profiler.EndSample();
        }

        private void TransformLODGroupData(in GPUDrivenLODGroupData lodGroupData)
        {
            Profiler.BeginSample("Transform LODGroups");

            m_LODGroupDataPool.UpdateLODGroupTransformData(lodGroupData);

            Profiler.EndSample();
        }

        public void DestroyLODGroups(NativeArray<int> destroyed)
        {
            if (destroyed.Length == 0)
                return;

            m_LODGroupDataPool.FreeLODGroupData(destroyed);
        }

        public void UpdateLODGroups(NativeArray<int> changedID)
        {
            if (changedID.Length == 0)
                return;

            m_GPUDrivenProcessor.DispatchLODGroupData(changedID, m_UpdateLODGroupCallback);
        }

        public void ReallocateAndGetInstances(in GPUDrivenRendererGroupData rendererData, NativeArray<InstanceHandle> instances)
        {
            m_InstanceDataSystem.ReallocateAndGetInstances(rendererData, instances);

            EnsureInstanceBufferCapacity();
        }

        public JobHandle ScheduleUpdateInstanceDataJob(NativeArray<InstanceHandle> instances, in GPUDrivenRendererGroupData rendererData)
        {
            return m_InstanceDataSystem.ScheduleUpdateInstanceDataJob(instances, rendererData, m_LODGroupDataPool.lodGroupDataHash);
        }

        public void FreeRendererGroupInstances(NativeArray<int> rendererGroupsID)
        {
            m_InstanceDataSystem.FreeRendererGroupInstances(rendererGroupsID);
        }

        public void FreeInstances(NativeArray<InstanceHandle> instances)
        {
            m_InstanceDataSystem.FreeInstances(instances);
        }

        public JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeArray<InstanceHandle> instances)
        {
            return m_InstanceDataSystem.ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instances);
        }

        public JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeList<InstanceHandle> instances)
        {
            return m_InstanceDataSystem.ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instances);
        }

        public JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeArray<int> instancesOffset, NativeArray<int> instancesCount, NativeList<InstanceHandle> instances)
        {
            return m_InstanceDataSystem.ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instancesOffset, instancesCount, instances);
        }

        public JobHandle ScheduleQueryMeshInstancesJob(NativeArray<int> sortedMeshIDs, NativeList<InstanceHandle> instances)
        {
            return m_InstanceDataSystem.ScheduleQuerySortedMeshInstancesJob(sortedMeshIDs, instances);
        }

        public void ChangeInstanceBufferVersion()
        {
            ++m_InstanceDataBuffer.version;
        }

        public GPUInstanceDataBufferUploader CreateDataBufferUploader(int capacity, InstanceType instanceType)
        {
            //@ This is not quite efficient as we will allocate all the parameters/descriptions of an certain type but write only some of them later.
            //@ We should allow to preallocate space only for needed parameters/descriptions.
            return new GPUInstanceDataBufferUploader(m_InstanceDataBuffer.descriptions, capacity, instanceType);
        }

        public void SubmitToGpu(NativeArray<InstanceHandle> instances, ref GPUInstanceDataBufferUploader uploader, bool submitOnlyWrittenParams)
        {
            uploader.SubmitToGpu(m_InstanceDataBuffer, instances, ref m_UploadResources, submitOnlyWrittenParams);
        }

        public void SubmitToGpu(NativeArray<GPUInstanceIndex> gpuInstanceIndices, ref GPUInstanceDataBufferUploader uploader, bool submitOnlyWrittenParams)
        {
            uploader.SubmitToGpu(m_InstanceDataBuffer, gpuInstanceIndices, ref m_UploadResources, submitOnlyWrittenParams);
        }

        public void InitializeInstanceTransforms(NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices, NativeArray<Matrix4x4> prevLocalToWorldMatrices)
        {
            if (instances.Length == 0)
                return;

            m_InstanceDataSystem.InitializeInstanceTransforms(instances, localToWorldMatrices, prevLocalToWorldMatrices, m_RenderersParameters, m_InstanceDataBuffer);
            ChangeInstanceBufferVersion();
        }

        public void UpdateInstanceTransforms(NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices)
        {
            if(instances.Length == 0)
                return;

            m_InstanceDataSystem.UpdateInstanceTransforms(instances, localToWorldMatrices, m_RenderersParameters, m_InstanceDataBuffer);
            ChangeInstanceBufferVersion();
        }

        public void UpdateAmbientProbeAndGpuBuffer(bool forceUpdate)
        {
            if (forceUpdate || m_CachedAmbientProbe != RenderSettings.ambientProbe)
            {
                m_CachedAmbientProbe = RenderSettings.ambientProbe;
                m_InstanceDataSystem.UpdateAllInstanceProbes(m_RenderersParameters, m_InstanceDataBuffer);
                ChangeInstanceBufferVersion();
            }
        }

        public void UpdateInstanceWindDataHistory(NativeArray<GPUInstanceIndex> gpuInstanceIndices)
        {
            if (gpuInstanceIndices.Length == 0)
                return;

            m_InstanceDataSystem.UpdateInstanceWindDataHistory(gpuInstanceIndices, m_RenderersParameters, m_InstanceDataBuffer);
            ChangeInstanceBufferVersion();
        }

        // This should be called at the end of the frame loop to properly update motion vectors.
        public void UpdateInstanceMotions()
        {
            m_InstanceDataSystem.UpdateInstanceMotions(m_RenderersParameters, m_InstanceDataBuffer);
            ChangeInstanceBufferVersion();
        }

        public void TransformLODGroups(NativeArray<int> lodGroupsID)
        {
            if (lodGroupsID.Length == 0)
                return;

            m_GPUDrivenProcessor.DispatchLODGroupData(lodGroupsID, m_TransformLODGroupCallback);
        }

        public void UpdatePerFrameInstanceVisibility(in ParallelBitArray compactedVisibilityMasks)
        {
            m_InstanceDataSystem.UpdatePerFrameInstanceVisibility(compactedVisibilityMasks);
        }

        public JobHandle ScheduleCollectInstancesLODGroupAndMasksJob(NativeArray<InstanceHandle> instances, NativeArray<uint> lodGroupAndMasks)
        {
            return m_InstanceDataSystem.ScheduleCollectInstancesLODGroupAndMasksJob(instances, lodGroupAndMasks);
        }

        public InstanceHandle GetRendererInstanceHandle(int rendererID)
        {
            var rendererIDs = new NativeArray<int>(1, Allocator.TempJob);
            var instances = new NativeArray<InstanceHandle>(1, Allocator.TempJob);

            rendererIDs[0] = rendererID;

            m_InstanceDataSystem.ScheduleQueryRendererGroupInstancesJob(rendererIDs, instances).Complete();

            InstanceHandle instance = instances[0];

            rendererIDs.Dispose();
            instances.Dispose();

            return instance;
        }

        public void GetVisibleTreeInstances(in ParallelBitArray compactedVisibilityMasks, in ParallelBitArray processedBits, NativeList<int> visibeTreeRendererIDs,
            NativeList<InstanceHandle> visibeTreeInstances, bool becomeVisibleOnly, out int becomeVisibeTreeInstancesCount)
        {
            m_InstanceDataSystem.GetVisibleTreeInstances(compactedVisibilityMasks, processedBits, visibeTreeRendererIDs, visibeTreeInstances, becomeVisibleOnly, out becomeVisibeTreeInstancesCount);
        }

        public GPUInstanceDataBuffer GetInstanceDataBuffer()
        {
            return m_InstanceDataBuffer;
        }

        public void UpdateFrame()
        {
            m_OcclusionCullingCommon.UpdateFrame();
            if (m_DebugStats != null)
                m_OcclusionCullingCommon.UpdateOccluderStats(m_DebugStats);
        }

        public void FreePerCameraInstanceData(NativeArray<int> cameraIDs)
        {
            m_InstanceDataSystem.DeallocatePerCameraInstanceData(cameraIDs);
        }

        public void UpdateCameras(NativeArray<int> cameraIDs)
        {
            m_InstanceDataSystem.AllocatePerCameraInstanceData(cameraIDs);
        }

#if UNITY_EDITOR
        public void UpdateSelectedInstances(NativeArray<InstanceHandle> instances)
        {
            m_InstanceDataSystem.UpdateSelectedInstances(instances);
        }
#endif
    }
}
