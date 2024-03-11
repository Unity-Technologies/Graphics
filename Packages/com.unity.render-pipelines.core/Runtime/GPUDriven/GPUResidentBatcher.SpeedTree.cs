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
    internal partial class GPUResidentBatcher : IDisposable
    {
        private ParallelBitArray m_ProcessedThisFrameTreeBits;

        private void ProcessTrees()
        {
            int treeInstancesCount = m_BatchersContext.GetAliveInstancesOfType(InstanceType.SpeedTree);

            if (treeInstancesCount == 0)
                return;

            ParallelBitArray compactedVisibilityMasks = m_InstanceCullingBatcher.GetCompactedVisibilityMasks(syncCullingJobs: false);

            if (!compactedVisibilityMasks.IsCreated)
                return;

            Profiler.BeginSample("GPUResidentInstanceBatcher.ProcessTrees");

            int maxInstancesCount = m_BatchersContext.aliveInstances.Length;

            if(!m_ProcessedThisFrameTreeBits.IsCreated)
                m_ProcessedThisFrameTreeBits = new ParallelBitArray(maxInstancesCount, Allocator.TempJob);
            else if(m_ProcessedThisFrameTreeBits.Length < maxInstancesCount)
                m_ProcessedThisFrameTreeBits.Resize(maxInstancesCount);

            bool becomeVisibleOnly = !Application.isPlaying;
            var visibleTreeRendererIDs = new NativeList<int>(Allocator.TempJob);
            var visibleTreeInstances = new NativeList<InstanceHandle>(Allocator.TempJob);

            m_BatchersContext.GetVisibleTreeInstances(compactedVisibilityMasks, m_ProcessedThisFrameTreeBits, visibleTreeRendererIDs, visibleTreeInstances,
                becomeVisibleOnly, out var becomeVisibleTreeInstancesCount);

            if (visibleTreeRendererIDs.Length > 0)
            {
                Profiler.BeginSample("GPUResidentInstanceBatcher.UpdateSpeedTreeWindAndUploadWindParamsToGPU");

                // Become visible trees is a subset of visible trees.
                var becomeVisibleTreeRendererIDs = visibleTreeRendererIDs.AsArray().GetSubArray(0, becomeVisibleTreeInstancesCount);
                var becomeVisibleTreeInstances = visibleTreeInstances.AsArray().GetSubArray(0, becomeVisibleTreeInstancesCount);

                if (becomeVisibleTreeRendererIDs.Length > 0)
                    UpdateSpeedTreeWindAndUploadWindParamsToGPU(becomeVisibleTreeRendererIDs, becomeVisibleTreeInstances, history: true);

                UpdateSpeedTreeWindAndUploadWindParamsToGPU(visibleTreeRendererIDs.AsArray(), visibleTreeInstances.AsArray(), history: false);

                Profiler.EndSample();
            }

            visibleTreeRendererIDs.Dispose();
            visibleTreeInstances.Dispose();

            Profiler.EndSample();
        }

        private unsafe void UpdateSpeedTreeWindAndUploadWindParamsToGPU(NativeArray<int> treeRendererIDs, NativeArray<InstanceHandle> treeInstances, bool history)
        {
            if (treeRendererIDs.Length == 0)
                return;

            Assert.AreEqual(treeRendererIDs.Length, treeInstances.Length);
            Assert.AreEqual(m_BatchersContext.renderersParameters.windParams.Length, (int)SpeedTreeWindParamIndex.MaxWindParamsCount);

            var gpuInstanceIndices = new NativeArray<GPUInstanceIndex>(treeInstances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_BatchersContext.instanceDataBuffer.CPUInstanceArrayToGPUInstanceArray(treeInstances, gpuInstanceIndices);

            if (!history)
                m_BatchersContext.UpdateInstanceWindDataHistory(gpuInstanceIndices);

            GPUInstanceDataBufferUploader uploader = m_BatchersContext.CreateDataBufferUploader(treeInstances.Length, InstanceType.SpeedTree);
            uploader.AllocateUploadHandles(treeInstances.Length);

            var windParams = new SpeedTreeWindParamsBufferIterator();
            windParams.bufferPtr = uploader.GetUploadBufferPtr();
            for (int i = 0; i < (int)SpeedTreeWindParamIndex.MaxWindParamsCount; ++i)
                windParams.uintParamOffsets[i] = uploader.PrepareParamWrite<Vector4>(m_BatchersContext.renderersParameters.windParams[i].index);
            windParams.uintStride = uploader.GetUIntPerInstance();
            windParams.elementOffset = 0;
            windParams.elementsCount = treeInstances.Length;

            SpeedTreeWindManager.UpdateWindAndWriteBufferWindParams(treeRendererIDs, windParams, history);
            m_BatchersContext.SubmitToGpu(gpuInstanceIndices, ref uploader, submitOnlyWrittenParams: true);

            gpuInstanceIndices.Dispose();
            uploader.Dispose();
        }
    }
}
