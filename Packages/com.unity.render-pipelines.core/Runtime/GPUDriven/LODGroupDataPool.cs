using System;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    internal unsafe struct LODGroupData
    {
        public const int k_MaxLODLevelsCount = 8;

        public bool valid;
        public int lodCount;
        public int rendererCount;
        public fixed float screenRelativeTransitionHeights[k_MaxLODLevelsCount];
        public fixed float fadeTransitionWidth[k_MaxLODLevelsCount];
    }

    internal unsafe struct LODGroupCullingData
    {
        public float3 worldSpaceReferencePoint;
        public float usedinCullingShader;
        public fixed float sqrDistances[LODGroupData.k_MaxLODLevelsCount]; // we use square distance to get rid of a sqrt in gpu culling..
        public fixed float transitionDistances[LODGroupData.k_MaxLODLevelsCount];
    }

    [BurstCompile]
    internal struct UpdateLODGroupTransformJob : IJobParallelFor
    {
        public const int k_BatchSize = 256;

        [ReadOnly] public NativeParallelHashMap<int, InstanceHandle> lodGroupDataHash;
        [ReadOnly] public NativeArray<int> lodGroupIDs;
        [ReadOnly] public NativeArray<Vector3> worldSpaceReferencePoints;
        [ReadOnly] public NativeArray<float> worldSpaceSizes;
        [ReadOnly] public bool requiresGPUUpload;
        [ReadOnly] public bool supportDitheringCrossFade;

        [NativeDisableContainerSafetyRestriction, ReadOnly] public NativeList<LODGroupData> lodGroupData;

        [NativeDisableContainerSafetyRestriction, WriteOnly] public NativeList<LODGroupCullingData> lodGroupCullingData;

        [NativeDisableContainerSafetyRestriction, WriteOnly] public NativeArray<LODGroupCullingData> lodGroupCullingDataForUpdate;
        [NativeDisableContainerSafetyRestriction, WriteOnly] public NativeArray<uint> lodGroupIndicesForUpdate;

        [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicUpdateCount;

        public unsafe void Execute(int index)
        {
            int lodGroupID = lodGroupIDs[index];

            if (lodGroupDataHash.TryGetValue(lodGroupID, out var lodGroupInstance))
            {
                LODGroupData* lodGroup = (LODGroupData*)lodGroupData.GetUnsafePtr() + lodGroupInstance.index;
                LODGroupCullingData* lodGroupTransformResult = (LODGroupCullingData*)lodGroupCullingData.GetUnsafePtr() + lodGroupInstance.index;

                lodGroupTransformResult->worldSpaceReferencePoint = worldSpaceReferencePoints[index];

                var worldSpaceSize = worldSpaceSizes[index];

                for (int i = 0; i < lodGroup->lodCount; ++i)
                {
                    float lodHeight = lodGroup->screenRelativeTransitionHeights[i];

                    var lodDist = LODGroupRenderingUtils.CalculateLODDistance(lodHeight, worldSpaceSize);
                    lodGroupTransformResult->sqrDistances[i] = lodDist * lodDist;

                    if (supportDitheringCrossFade)
                    {
                        float prevLODHeight = i != 0 ? lodGroup->screenRelativeTransitionHeights[i - 1] : 1.0f;
                        float transitionHeight = lodHeight + lodGroup->fadeTransitionWidth[i] * (prevLODHeight - lodHeight);
                        var transitionDistance = lodDist - LODGroupRenderingUtils.CalculateLODDistance(transitionHeight, worldSpaceSize);
                        transitionDistance = Mathf.Max(0.0f, transitionDistance);
                        lodGroupTransformResult->transitionDistances[i] = transitionDistance;
                    }
                    else
                    {
                        lodGroupTransformResult->transitionDistances[i] = 0f;
                    }

                }

                if (!requiresGPUUpload)
                    return;

                var offset = atomicUpdateCount.Add(1);
                lodGroupCullingDataForUpdate[offset] = *lodGroupTransformResult;
                // To avoid useless GPU copies during scattered update, we pack in the lowest bits of the index
                // whether or not we require to copy all 8 lods, or 4 is enough
                uint requireFullCopy = (uint)lodGroup->lodCount >> 2;
                lodGroupIndicesForUpdate[offset] = (uint)lodGroupInstance.index << 1 | requireFullCopy;
            }
        }
    }

    [BurstCompile]
    internal unsafe struct AllocateOrGetLODGroupDataInstancesJob : IJob
    {
        [ReadOnly] public NativeArray<int> lodGroupsID;

        public NativeList<LODGroupData> lodGroupsData;
        public NativeList<LODGroupCullingData> lodGroupCullingData;
        public NativeParallelHashMap<int, InstanceHandle> lodGroupDataHash;
        public NativeList<InstanceHandle> freeLODGroupDataHandles;

        [WriteOnly] public NativeArray<InstanceHandle> lodGroupInstances;

        [NativeDisableUnsafePtrRestriction] public int* previousRendererCount;

        public void Execute()
        {
            int freeHandlesCount = freeLODGroupDataHandles.Length;
            int lodDataLength = lodGroupsData.Length;

            for (int i = 0; i < lodGroupsID.Length; ++i)
            {
                int lodGroupID = lodGroupsID[i];

                if (!lodGroupDataHash.TryGetValue(lodGroupID, out var lodGroupInstance))
                {
                    if (freeHandlesCount == 0)
                        lodGroupInstance = new InstanceHandle() { index = lodDataLength++ };
                    else
                        lodGroupInstance = freeLODGroupDataHandles[--freeHandlesCount];

                    lodGroupDataHash.TryAdd(lodGroupID, lodGroupInstance);
                }
                else
                {
                    *previousRendererCount += lodGroupsData.ElementAt(lodGroupInstance.index).rendererCount;
                }

                lodGroupInstances[i] = lodGroupInstance;
            }

            freeLODGroupDataHandles.ResizeUninitialized(freeHandlesCount);
            lodGroupsData.ResizeUninitialized(lodDataLength);
            lodGroupCullingData.ResizeUninitialized(lodDataLength);
        }
    }

    [BurstCompile]
    internal unsafe struct UpdateLODGroupDataJob : IJobParallelFor
    {
        public const int k_BatchSize = 256;

        [ReadOnly] public NativeArray<InstanceHandle> lodGroupInstances;
        [ReadOnly] public GPUDrivenLODGroupData inputData;
        [ReadOnly] public bool supportDitheringCrossFade;

        public NativeArray<LODGroupData> lodGroupsData;
        public NativeArray<LODGroupCullingData> lodGroupsCullingData;

        [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 rendererCount;

        public void Execute(int index)
        {
            var lodGroupInstance = lodGroupInstances[index];
            var fadeMode = inputData.fadeMode[index];
            var lodOffset = inputData.lodOffset[index];
            var lodCount = inputData.lodCount[index];
            var renderersCount = inputData.renderersCount[index];
            var worldReferencePoint = inputData.worldSpaceReferencePoint[index];
            var worldSpaceSize = inputData.worldSpaceSize[index];
            var useCrossFade = fadeMode == LODFadeMode.CrossFade && supportDitheringCrossFade;

            LODGroupData* lodGroupData = (LODGroupData*)lodGroupsData.GetUnsafePtr() + lodGroupInstance.index;
            LODGroupCullingData* lodGroupCullingData = (LODGroupCullingData*)lodGroupsCullingData.GetUnsafePtr() + lodGroupInstance.index;

            lodGroupData->valid = true;
            lodGroupData->lodCount = lodCount;
            lodGroupData->rendererCount = useCrossFade ? renderersCount : 0;
            lodGroupCullingData->worldSpaceReferencePoint = worldReferencePoint;
            lodGroupCullingData->usedinCullingShader = 0.0f;

            rendererCount.Add(lodGroupData->rendererCount);

            for (int i = 0; i < lodCount; ++i)
            {
                var lodIndex = lodOffset + i;
                var fadeTransitionWidth = inputData.lodFadeTransitionWidth[lodIndex];
                var lodHeight = inputData.lodScreenRelativeTransitionHeight[lodIndex];
                var lodDist = LODGroupRenderingUtils.CalculateLODDistance(lodHeight, worldSpaceSize);

                lodGroupCullingData->sqrDistances[i] = lodDist * lodDist;
                lodGroupCullingData->transitionDistances[i] = 0;
                lodGroupData->screenRelativeTransitionHeights[i] = lodHeight;
                lodGroupData->fadeTransitionWidth[i] = 0;

                if (useCrossFade)
                {
                    float prevLODHeight = i != 0 ? inputData.lodScreenRelativeTransitionHeight[lodIndex - 1] : 1.0f;
                    float transitionHeight = lodHeight + fadeTransitionWidth * (prevLODHeight - lodHeight);

                    var transitionDistance = lodDist - LODGroupRenderingUtils.CalculateLODDistance(transitionHeight, worldSpaceSize);
                    transitionDistance = Mathf.Max(0.0f, transitionDistance);

                    lodGroupData->fadeTransitionWidth[i] = fadeTransitionWidth;
                    lodGroupCullingData->transitionDistances[i] = transitionDistance;
                }
            }
        }
    }

    [BurstCompile]
    internal unsafe struct FreeLODGroupDataJob : IJob
    {
        [ReadOnly] public NativeArray<int> destroyedLODGroupsID;

        public NativeList<LODGroupData> lodGroupsData;
        public NativeParallelHashMap<int, InstanceHandle> lodGroupDataHash;
        public NativeList<InstanceHandle> freeLODGroupDataHandles;

        [NativeDisableUnsafePtrRestriction] public int* removedRendererCount;

        public void Execute()
        {
            foreach (int lodGroupID in destroyedLODGroupsID)
            {
                if (lodGroupDataHash.TryGetValue(lodGroupID, out var lodGroupInstance))
                {
                    Assert.IsTrue(lodGroupInstance.valid);

                    lodGroupDataHash.Remove(lodGroupID);
                    freeLODGroupDataHandles.Add(lodGroupInstance);

                    ref LODGroupData lodGroupData = ref lodGroupsData.ElementAt(lodGroupInstance.index);
                    Assert.IsTrue(lodGroupData.valid);

                    *removedRendererCount += lodGroupData.rendererCount;
                    lodGroupData.valid = false;
                }
            }
        }
    }

    internal class LODGroupDataPool : IDisposable
    {
        private NativeList<LODGroupData> m_LODGroupData;
        private NativeParallelHashMap<int, InstanceHandle> m_LODGroupDataHash;
        public NativeParallelHashMap<int, InstanceHandle> lodGroupDataHash => m_LODGroupDataHash;

        private NativeList<LODGroupCullingData> m_LODGroupCullingData;
        private NativeList<InstanceHandle> m_FreeLODGroupDataHandles;

        private int m_CrossfadedRendererCount;
        private bool m_SupportDitheringCrossFade;

        public NativeList<LODGroupCullingData> lodGroupCullingData => m_LODGroupCullingData;
        public int crossfadedRendererCount => m_CrossfadedRendererCount;

        // GPU Lod selection declarations
        private bool m_useGPUCulling;
        private GPUInstanceDataBuffer m_LodGroupCullingDataBuffer;
        public GPUInstanceDataBuffer lodCullingDataBuffer => m_LodGroupCullingDataBuffer;
        public int lodDataBufferAddress => m_LodGroupCullingDataBuffer.gpuBufferComponentAddress[0];
        private GPUInstanceDataBufferUploader.GPUResources m_UploaderGPUResources;
        private GPUInstanceDataBufferGrower.GPUResources m_GrowerGPUResources;

        // Scattered update declarations
        private CommandBuffer m_CmdBuffer;
        private int m_ScatteredUpdateBuffersSize;
        private ComputeBuffer m_ScatteredUpdateIndexQueueBuffer;
        private ComputeBuffer m_ScatteredUpdateDataQueueBuffer;
        private ComputeShader m_LodGroupUpdateCS;
        private int m_LodGroupUpdateKernel;

        private static class LodGroupShaderIDs
        {
            public static readonly int _SupportDitheringCrossFade = Shader.PropertyToID("_SupportDitheringCrossFade");
            public static readonly int _LodGroupCullingDataGPUByteSize = Shader.PropertyToID("_LodGroupCullingDataGPUByteSize");
            public static readonly int _LodGroupCullingDataStartOffset = Shader.PropertyToID("_LodGroupCullingDataStartOffset");
            public static readonly int _LodCullingDataQueueCount = Shader.PropertyToID("_LodCullingDataQueueCount");
            public static readonly int _InputLodCullingDataIndices = Shader.PropertyToID("_InputLodCullingDataIndices");
            public static readonly int _InputLodCullingDataBuffer = Shader.PropertyToID("_InputLodCullingDataBuffer");
            public static readonly int _LodGroupCullingData = Shader.PropertyToID("_LodGroupCullingData");
        }

        public LODGroupDataPool(GPUResidentDrawerResources resources, int initialInstanceCount, bool supportDitheringCrossFade, bool useGPUCulling)
        {
            m_LODGroupData = new NativeList<LODGroupData>(Allocator.Persistent);
            m_LODGroupDataHash = new NativeParallelHashMap<int, InstanceHandle>(64, Allocator.Persistent);

            m_LODGroupCullingData = new NativeList<LODGroupCullingData>(Allocator.Persistent);
            m_FreeLODGroupDataHandles = new NativeList<InstanceHandle>(Allocator.Persistent);

            m_SupportDitheringCrossFade = supportDitheringCrossFade;

            if (!useGPUCulling)
                return;

            // We currently do not support lod crossfade with GPU culling - setting it to false as an optimization to avoid update / GPU upload of unused data
            m_SupportDitheringCrossFade = false;
            m_useGPUCulling = true;
            using (var builder = new GPUInstanceDataBufferBuilder())
            {
                builder.AddComponent<LODGroupCullingData>(LodGroupShaderIDs._LodGroupCullingData, isOverriden: true, isPerInstance: true);
                m_LodGroupCullingDataBuffer = builder.Build(initialInstanceCount / 4);
            }
            m_GrowerGPUResources = new GPUInstanceDataBufferGrower.GPUResources();
            m_UploaderGPUResources = new GPUInstanceDataBufferUploader.GPUResources();

            m_CmdBuffer = new CommandBuffer();
            m_CmdBuffer.name = "LodGroupUpdaterCommands";
            LoadShaders(resources);
        }

        public void Dispose()
        {
            m_LODGroupData.Dispose();
            m_LODGroupDataHash.Dispose();

            m_LODGroupCullingData.Dispose();
            m_FreeLODGroupDataHandles.Dispose();

            if (!m_useGPUCulling)
                return;

            m_ScatteredUpdateDataQueueBuffer?.Release();
            m_ScatteredUpdateIndexQueueBuffer?.Release();

            m_GrowerGPUResources.Dispose();
            m_UploaderGPUResources.Dispose();
            m_LodGroupCullingDataBuffer?.Dispose();
        }

        public unsafe void UpdateLODGroupTransformData(in GPUDrivenLODGroupData inputData)
        {
            var lodGroupCount = inputData.lodGroupID.Length;

            // todo - profile cost of per frame tempAlloc vs. footprint of making these 2 persistent
            var lodGroupIndicesForUpdate = new NativeArray<uint>(m_useGPUCulling ? lodGroupCount : 0, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var lodGroupCullingDataForUpdate = new NativeArray<LODGroupCullingData>(m_useGPUCulling ? lodGroupCount : 0, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var updateCount = 0;

            var jobData = new UpdateLODGroupTransformJob()
            {
                lodGroupDataHash = m_LODGroupDataHash,
                lodGroupIDs = inputData.lodGroupID,
                worldSpaceReferencePoints = inputData.worldSpaceReferencePoint,
                worldSpaceSizes = inputData.worldSpaceSize,
                lodGroupData = m_LODGroupData,
                lodGroupCullingData = m_LODGroupCullingData,
                supportDitheringCrossFade = m_SupportDitheringCrossFade,
                requiresGPUUpload = m_useGPUCulling,
                atomicUpdateCount = new UnsafeAtomicCounter32(&updateCount),
                lodGroupIndicesForUpdate = lodGroupIndicesForUpdate,
                lodGroupCullingDataForUpdate = lodGroupCullingDataForUpdate
            };

            if (lodGroupCount >= UpdateLODGroupTransformJob.k_BatchSize)
                jobData.Schedule(lodGroupCount, UpdateLODGroupTransformJob.k_BatchSize).Complete();
            else
                jobData.Run(lodGroupCount);

            if (m_useGPUCulling)
                AddLODGroupUpdateCommand(updateCount, lodGroupIndicesForUpdate, lodGroupCullingDataForUpdate);

            lodGroupIndicesForUpdate.Dispose();
            lodGroupCullingDataForUpdate.Dispose();
        }

        public unsafe void UpdateLODGroupData(in GPUDrivenLODGroupData inputData)
        {
            FreeLODGroupData(inputData.invalidLODGroupID);

            var lodGroupInstances = new NativeArray<InstanceHandle>(inputData.lodGroupID.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int previousRendererCount = 0;

            new AllocateOrGetLODGroupDataInstancesJob
            {
                lodGroupsID = inputData.lodGroupID,
                lodGroupsData = m_LODGroupData,
                lodGroupCullingData = m_LODGroupCullingData,
                lodGroupDataHash = m_LODGroupDataHash,
                freeLODGroupDataHandles = m_FreeLODGroupDataHandles,
                lodGroupInstances = lodGroupInstances,
                previousRendererCount = &previousRendererCount
            }.Run();

            m_CrossfadedRendererCount -= previousRendererCount;
            Assert.IsTrue(m_CrossfadedRendererCount >= 0);

            int rendererCount = 0;

            var updateLODGroupDataJobData = new UpdateLODGroupDataJob
            {
                lodGroupInstances = lodGroupInstances,
                inputData = inputData,
                supportDitheringCrossFade = m_SupportDitheringCrossFade,
                lodGroupsData = m_LODGroupData.AsArray(),
                lodGroupsCullingData = m_LODGroupCullingData.AsArray(),
                rendererCount = new UnsafeAtomicCounter32(&rendererCount),
            };

            if (lodGroupInstances.Length >= UpdateLODGroupTransformJob.k_BatchSize)
                updateLODGroupDataJobData.Schedule(lodGroupInstances.Length, UpdateLODGroupTransformJob.k_BatchSize).Complete();
            else
                updateLODGroupDataJobData.Run(lodGroupInstances.Length);

            m_CrossfadedRendererCount += rendererCount;

            SubmitToGPU(lodGroupInstances);

            lodGroupInstances.Dispose();
        }

        public unsafe void FreeLODGroupData(NativeArray<int> destroyedLODGroupsID)
        {
            if (destroyedLODGroupsID.Length == 0)
                return;

            int removedRendererCount = 0;

            new FreeLODGroupDataJob
            {
                destroyedLODGroupsID = destroyedLODGroupsID,
                lodGroupsData = m_LODGroupData,
                lodGroupDataHash = m_LODGroupDataHash,
                freeLODGroupDataHandles = m_FreeLODGroupDataHandles,
                removedRendererCount = &removedRendererCount
            }.Run();

            m_CrossfadedRendererCount -= removedRendererCount;
            Assert.IsTrue(m_CrossfadedRendererCount >= 0);
        }

        //GPU Lod Selection below
        private void LoadShaders(GPUResidentDrawerResources resources)
        {
            m_UploaderGPUResources.LoadShaders(resources);
            m_GrowerGPUResources.LoadShaders(resources);

            m_LodGroupUpdateCS = resources.transformUpdaterKernels;
            m_LodGroupUpdateKernel = m_LodGroupUpdateCS.FindKernel("ScatterUpdateLodGroupMain");
        }

        private void SubmitToGPU(NativeArray<InstanceHandle> lodInstancesToUpload)
        {
            if (!m_useGPUCulling || lodInstancesToUpload.Length == 0)
                return;

            using var uploader = new GPUInstanceDataBufferUploader(m_LodGroupCullingDataBuffer.descriptions, lodInstancesToUpload.Length);
            uploader.AllocateInstanceHandles(lodInstancesToUpload);
            uploader.GatherInstanceData<LODGroupCullingData>(0, lodInstancesToUpload, m_LODGroupCullingData.AsArray());
            uploader.SubmitToGpu(m_LodGroupCullingDataBuffer, lodInstancesToUpload, ref m_UploaderGPUResources);
        }

        private bool GrowUpdateBuffers(int requiredSize)
        {
            if (requiredSize < m_ScatteredUpdateBuffersSize)
                return false;

            var sizeAligned = (requiredSize | 0x3F) + 1; // size aligned to 64
            m_ScatteredUpdateIndexQueueBuffer?.Release();
            m_ScatteredUpdateDataQueueBuffer?.Release();

            m_ScatteredUpdateIndexQueueBuffer = new ComputeBuffer(sizeAligned, 4, ComputeBufferType.Raw);
            m_ScatteredUpdateDataQueueBuffer = new ComputeBuffer(sizeAligned, System.Runtime.InteropServices.Marshal.SizeOf<LODGroupCullingData>(), ComputeBufferType.Raw);

            return true;
        }

        private void AddLODGroupUpdateCommand(int queueCount, NativeArray<uint> scatteredUpdateLodGroupIndices, NativeArray<LODGroupCullingData> scatteredUpdateCullingData)
        {
            if (queueCount == 0)
                return;

            GrowUpdateBuffers(queueCount);
            m_CmdBuffer.Clear();
            m_CmdBuffer.SetBufferData(m_ScatteredUpdateIndexQueueBuffer, scatteredUpdateLodGroupIndices, 0, 0, queueCount);
            m_CmdBuffer.SetBufferData(m_ScatteredUpdateDataQueueBuffer, scatteredUpdateCullingData, 0, 0, queueCount);
            m_CmdBuffer.SetComputeIntParam(m_LodGroupUpdateCS, LodGroupShaderIDs._SupportDitheringCrossFade, Convert.ToInt32(m_SupportDitheringCrossFade));
            m_CmdBuffer.SetComputeIntParam(m_LodGroupUpdateCS, LodGroupShaderIDs._LodGroupCullingDataGPUByteSize, System.Runtime.InteropServices.Marshal.SizeOf<LODGroupCullingData>());
            m_CmdBuffer.SetComputeIntParam(m_LodGroupUpdateCS, LodGroupShaderIDs._LodCullingDataQueueCount, queueCount);
            m_CmdBuffer.SetComputeIntParam(m_LodGroupUpdateCS, LodGroupShaderIDs._LodGroupCullingDataStartOffset, m_LodGroupCullingDataBuffer.gpuBufferComponentAddress[0]); // buffer has a single component - at index 0
            m_CmdBuffer.SetComputeBufferParam(m_LodGroupUpdateCS, m_LodGroupUpdateKernel, LodGroupShaderIDs._InputLodCullingDataIndices, m_ScatteredUpdateIndexQueueBuffer);
            m_CmdBuffer.SetComputeBufferParam(m_LodGroupUpdateCS, m_LodGroupUpdateKernel, LodGroupShaderIDs._InputLodCullingDataBuffer, m_ScatteredUpdateDataQueueBuffer);
            m_CmdBuffer.SetComputeBufferParam(m_LodGroupUpdateCS, m_LodGroupUpdateKernel, LodGroupShaderIDs._LodGroupCullingData, m_LodGroupCullingDataBuffer.gpuBuffer);
            m_CmdBuffer.DispatchCompute(m_LodGroupUpdateCS, m_LodGroupUpdateKernel, (queueCount + 63) / 64, 1, 1);
            Graphics.ExecuteCommandBuffer(m_CmdBuffer);
        }
    }
}
