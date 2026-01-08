using System;
using Unity.Collections;
using UnityEngine.Profiling;
using UnityEngine.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    internal class SpeedTreeWindGPUDataUpdater : IDisposable
    {
        private InstanceDataSystem m_InstanceDataSystem;
        private InstanceCuller m_Culler;
        private ParallelBitArray m_ProcessedThisFrameTreeBits;
        private NativeArray<uint> m_CPUUploadBuffer;
        private GraphicsBuffer m_GPUUploadBuffer;

        public void Initialize(InstanceDataSystem instanceDataSystem, InstanceCuller culler)
        {
            m_InstanceDataSystem = instanceDataSystem;
            m_Culler = culler;
        }

        public void Dispose()
        {
            m_CPUUploadBuffer.Dispose();
            m_GPUUploadBuffer?.Release();

            if (m_ProcessedThisFrameTreeBits.IsCreated)
                m_ProcessedThisFrameTreeBits.Dispose();
        }

        public void OnBeginContextRendering()
        {
            if (m_ProcessedThisFrameTreeBits.IsCreated)
                m_ProcessedThisFrameTreeBits.Dispose();
        }

        public void UpdateGPUData()
        {
            if (m_InstanceDataSystem.totalTreeCount == 0)
                return;

            ParallelBitArray compactedVisibilityMasks = m_Culler.GetCompactedVisibilityMasks(syncCullingJobs: false);

            if (!compactedVisibilityMasks.IsCreated)
                return;

            Profiler.BeginSample("SpeedTreeGPUWindDataUpdater.UpdateGPUData");

            int maxInstancesCount = m_InstanceDataSystem.indexToHandle.Length;

            if (!m_ProcessedThisFrameTreeBits.IsCreated)
                m_ProcessedThisFrameTreeBits = new ParallelBitArray(maxInstancesCount, Allocator.TempJob);
            else if (m_ProcessedThisFrameTreeBits.Length < maxInstancesCount)
                m_ProcessedThisFrameTreeBits.Resize(maxInstancesCount);

            bool becomeVisibleOnly = !Application.isPlaying;
            var visibleTreeRenderers = new NativeList<EntityId>(Allocator.TempJob);
            var visibleTreeInstances = new NativeList<InstanceHandle>(Allocator.TempJob);

            m_InstanceDataSystem.GetVisibleTreeInstances(compactedVisibilityMasks, m_ProcessedThisFrameTreeBits, visibleTreeRenderers, visibleTreeInstances,
                becomeVisibleOnly, out var becomeVisibleTreeInstancesCount);

            if (visibleTreeRenderers.Length > 0)
            {
                Profiler.BeginSample("SpeedTreeGPUWindDataUpdater.UpdateSpeedTreeWindAndUploadWindParamsToGPU");

                // Become visible trees is a subset of visible trees.
                var becomeVisibleTreeRendererIDs = visibleTreeRenderers.AsArray().GetSubArray(0, becomeVisibleTreeInstancesCount);
                var becomeVisibleTreeInstances = visibleTreeInstances.AsArray().GetSubArray(0, becomeVisibleTreeInstancesCount);

                if (becomeVisibleTreeRendererIDs.Length > 0)
                    UpdateSpeedTreeWindAndUploadWindParamsToGPU(becomeVisibleTreeRendererIDs, becomeVisibleTreeInstances, history: true);

                UpdateSpeedTreeWindAndUploadWindParamsToGPU(visibleTreeRenderers.AsArray(), visibleTreeInstances.AsArray(), history: false);

                Profiler.EndSample();
            }

            visibleTreeRenderers.Dispose();
            visibleTreeInstances.Dispose();

            Profiler.EndSample();
        }

        private unsafe void UpdateSpeedTreeWindAndUploadWindParamsToGPU(NativeArray<EntityId> treeRenderers, NativeArray<InstanceHandle> treeInstances, bool history)
        {
            if (treeRenderers.Length == 0)
                return;

            ref DefaultGPUComponents defaultGPUComponents = ref m_InstanceDataSystem.defaultGPUComponents;

            Assert.AreEqual(treeRenderers.Length, treeInstances.Length);
            Assert.AreEqual(defaultGPUComponents.speedTreeWind.Length, (int)SpeedTreeWindParamIndex.MaxWindParamsCount);
            
            var gpuIndices = new NativeArray<GPUInstanceIndex>(treeInstances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_InstanceDataSystem.QueryInstanceGPUIndices(treeInstances, gpuIndices);

            if (!history)
                m_InstanceDataSystem.UpdateInstanceWindDataHistory(gpuIndices);

            GPUInstanceUploadData uploadData = m_InstanceDataSystem.CreateInstanceUploadData(defaultGPUComponents.speedTreeWind, treeInstances.Length, Allocator.TempJob);

            EnsureUploadBufferUintCount(uploadData.uploadDataUIntSize);

            NativeArray<uint> writeBuffer = m_CPUUploadBuffer.GetSubArray(0, uploadData.uploadDataUIntSize);

            var windParams = new SpeedTreeWindParamsBufferIterator();
            windParams.bufferPtr = (IntPtr)writeBuffer.GetUnsafePtr();
            for (int i = 0; i < (int)SpeedTreeWindParamIndex.MaxWindParamsCount; ++i)
                windParams.uintParamOffsets[i] = uploadData.PrepareComponentWrite<Vector4>(defaultGPUComponents.speedTreeWind[i]);
            windParams.uintStride = UnsafeUtility.SizeOf<Vector4>() / UnsafeUtility.SizeOf<uint>();
            windParams.elementOffset = 0;
            windParams.elementsCount = treeInstances.Length;
            SpeedTreeWindManager.UpdateWindAndWriteBufferWindParams(treeRenderers, windParams, history);
            
            m_GPUUploadBuffer.SetData(writeBuffer);

            m_InstanceDataSystem.UploadDataToGPU(gpuIndices, m_GPUUploadBuffer, uploadData);

            uploadData.Dispose();
            gpuIndices.Dispose();
        }

        void EnsureUploadBufferUintCount(int uintCount)
        {
            int currentCPUBufferLength = m_CPUUploadBuffer.IsCreated ? m_CPUUploadBuffer.Length : 0;
            int currentGPUBufferLength = m_GPUUploadBuffer != null ? m_GPUUploadBuffer.count : 0;

            Assert.IsTrue(currentCPUBufferLength == currentGPUBufferLength);
            int currentUintCount = currentCPUBufferLength;

            if (uintCount > currentUintCount)
            {
                // At least double on resize
                int newUintCount = math.max(currentUintCount * 2, uintCount);

                m_CPUUploadBuffer.Dispose();
                m_GPUUploadBuffer?.Release();

                m_CPUUploadBuffer = new NativeArray<uint>(newUintCount, Allocator.Persistent);
                m_GPUUploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, newUintCount, sizeof(uint));
            }

            Assert.IsTrue(m_GPUUploadBuffer.count == m_CPUUploadBuffer.Length);
        }
    }
}
