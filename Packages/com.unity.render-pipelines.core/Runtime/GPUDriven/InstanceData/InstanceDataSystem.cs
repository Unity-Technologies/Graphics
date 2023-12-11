using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

#if UNITY_EDITOR
using UnityEditor.Sprites;
#endif

namespace UnityEngine.Rendering
{
    internal partial class InstanceDataSystem : IDisposable
    {
        private InstanceAllocators m_InstanceAllocators;
        private CPUSharedInstanceData m_SharedInstanceData;
        private CPUInstanceData m_InstanceData;

        //@ We may want something a bit faster instead of multi hash map. Remove and search performance for multiple instances per renderer group is not great.
        private NativeParallelMultiHashMap<int, InstanceHandle> m_RendererGroupInstanceMultiHash;

        private ComputeShader m_TransformUpdateCS;
        private ComputeShader m_WindDataUpdateCS;
        private int m_TransformInitKernel;
        private int m_TransformUpdateKernel;
        private int m_MotionUpdateKernel;
        private int m_ProbeUpdateKernel;
        private int m_LODUpdateKernel;
        private int m_WindDataCopyHistoryKernel;

        private ComputeBuffer m_UpdateIndexQueueBuffer;

        private ComputeBuffer m_ProbeUpdateDataQueueBuffer;
        private ComputeBuffer m_ProbeOcclusionUpdateDataQueueBuffer;

        private ComputeBuffer m_TransformUpdateDataQueueBuffer;
        private ComputeBuffer m_BoundingSpheresUpdateDataQueueBuffer;

        private bool m_EnableBoundingSpheres;

        public bool hasBoundingSpheres { get { return m_EnableBoundingSpheres; } }

        public CPUInstanceData.ReadOnly instanceData { get { return m_InstanceData.AsReadOnly(); } }
        public CPUSharedInstanceData.ReadOnly sharedInstanceData { get { return m_SharedInstanceData.AsReadOnly(); } }
        public NativeArray<InstanceHandle> aliveInstances { get { return m_InstanceData.instances.GetSubArray(0, m_InstanceData.instancesLength); } }

        public InstanceDataSystem(int maxInstances, bool enableBoundingSpheres, GPUResidentDrawerResources resources)
        {
            m_InstanceAllocators = new InstanceAllocators();
            m_SharedInstanceData = new CPUSharedInstanceData();
            m_InstanceData = new CPUInstanceData();

            m_InstanceAllocators.Initialize();
            m_SharedInstanceData.Initialize(maxInstances);
            m_InstanceData.Initialize(maxInstances);

            m_RendererGroupInstanceMultiHash = new NativeParallelMultiHashMap<int, InstanceHandle>(maxInstances, Allocator.Persistent);

            m_TransformUpdateCS = resources.transformUpdaterKernels;
            m_WindDataUpdateCS = resources.windDataUpdaterKernels;

            m_TransformInitKernel = m_TransformUpdateCS.FindKernel("ScatterInitTransformMain");
            m_TransformUpdateKernel = m_TransformUpdateCS.FindKernel("ScatterUpdateTransformMain");
            m_MotionUpdateKernel = m_TransformUpdateCS.FindKernel("ScatterUpdateMotionMain");
            m_ProbeUpdateKernel = m_TransformUpdateCS.FindKernel("ScatterUpdateProbesMain");
            if (enableBoundingSpheres)
                m_TransformUpdateCS.EnableKeyword("PROCESS_BOUNDING_SPHERES");
            else
                m_TransformUpdateCS.DisableKeyword("PROCESS_BOUNDING_SPHERES");

            m_WindDataCopyHistoryKernel = m_WindDataUpdateCS.FindKernel("WindDataCopyHistoryMain");

            m_EnableBoundingSpheres = enableBoundingSpheres;
        }

        public void Dispose()
        {
            m_InstanceAllocators.Dispose();
            m_SharedInstanceData.Dispose();
            m_InstanceData.Dispose();

            m_RendererGroupInstanceMultiHash.Dispose();

            m_UpdateIndexQueueBuffer?.Dispose();
            m_ProbeUpdateDataQueueBuffer?.Dispose();
            m_ProbeOcclusionUpdateDataQueueBuffer?.Dispose();
            m_TransformUpdateDataQueueBuffer?.Dispose();
            m_BoundingSpheresUpdateDataQueueBuffer?.Dispose();
        }

        public int GetMaxInstancesOfType(InstanceType instanceType)
        {
            return m_InstanceAllocators.GetInstanceHandlesLength(instanceType);
        }

        public int GetAliveInstancesOfType(InstanceType instanceType)
        {
            return m_InstanceAllocators.GetInstancesLength(instanceType);
        }

        private void EnsureIndexQueueBufferCapacity(int capacity)
        {
            if(m_UpdateIndexQueueBuffer == null || m_UpdateIndexQueueBuffer.count < capacity)
            {
                m_UpdateIndexQueueBuffer?.Dispose();
                m_UpdateIndexQueueBuffer = new ComputeBuffer(capacity, 4, ComputeBufferType.Raw);
            }
        }

        private void EnsureProbeBuffersCapacity(int capacity)
        {
            EnsureIndexQueueBufferCapacity(capacity);

            if (m_ProbeUpdateDataQueueBuffer == null || m_ProbeUpdateDataQueueBuffer.count < capacity)
            {
                m_ProbeUpdateDataQueueBuffer?.Dispose();
                m_ProbeOcclusionUpdateDataQueueBuffer?.Dispose();
                m_ProbeUpdateDataQueueBuffer = new ComputeBuffer(capacity, System.Runtime.InteropServices.Marshal.SizeOf<SHUpdatePacket>(), ComputeBufferType.Structured);
                m_ProbeOcclusionUpdateDataQueueBuffer = new ComputeBuffer(capacity, System.Runtime.InteropServices.Marshal.SizeOf<Vector4>(), ComputeBufferType.Structured);
            }
        }

        private void EnsureTransformBuffersCapacity(int capacity)
        {
            EnsureIndexQueueBufferCapacity(capacity);

            // Current and the previous matrices
            int transformsCapacity = capacity * 2;

            if (m_TransformUpdateDataQueueBuffer == null || m_TransformUpdateDataQueueBuffer.count < transformsCapacity)
            {
                m_TransformUpdateDataQueueBuffer?.Dispose();
                m_BoundingSpheresUpdateDataQueueBuffer?.Dispose();
                m_TransformUpdateDataQueueBuffer = new ComputeBuffer(transformsCapacity, System.Runtime.InteropServices.Marshal.SizeOf<TransformUpdatePacket>(), ComputeBufferType.Structured);
                if (m_EnableBoundingSpheres)
                    m_BoundingSpheresUpdateDataQueueBuffer = new ComputeBuffer(capacity, System.Runtime.InteropServices.Marshal.SizeOf<float4>(), ComputeBufferType.Structured);
            }
        }

        private JobHandle ScheduleInterpolateProbesAndUpdateTetrahedronCache(int queueCount, NativeArray<InstanceHandle> probeUpdateInstanceQueue, NativeArray<int> compactTetrahedronCache,
            NativeArray<Vector3> probeQueryPosition, NativeArray<SphericalHarmonicsL2> probeUpdateDataQueue, NativeArray<Vector4> probeOcclusionUpdateDataQueue)
        {
            var lightProbesQuery = new LightProbesQuery(Allocator.TempJob);

            var calculateProbesJob = new CalculateInterpolatedLightAndOcclusionProbesBatchJob()
            {
                lightProbesQuery = lightProbesQuery,
                probesCount = queueCount,
                queryPostitions = probeQueryPosition,
                compactTetrahedronCache = compactTetrahedronCache,
                probesSphericalHarmonics = probeUpdateDataQueue,
                probesOcclusion = probeOcclusionUpdateDataQueue
            };

            var totalBatchCount = 1 + (queueCount / CalculateInterpolatedLightAndOcclusionProbesBatchJob.k_CalculatedProbesPerBatch);

            var calculateProbesJobHandle = calculateProbesJob.Schedule(totalBatchCount, CalculateInterpolatedLightAndOcclusionProbesBatchJob.k_BatchSize);

            lightProbesQuery.Dispose(calculateProbesJobHandle);

            var scatterTetrahedronCacheIndicesJob = new ScatterTetrahedronCacheIndicesJob()
            {
                compactTetrahedronCache = compactTetrahedronCache,
                probeInstances = probeUpdateInstanceQueue,
                instanceData = m_InstanceData
            };

            return scatterTetrahedronCacheIndicesJob.Schedule(queueCount, ScatterTetrahedronCacheIndicesJob.k_BatchSize, calculateProbesJobHandle);
        }

        private void DispatchProbeUpdateCommand(int queueCount, NativeArray<InstanceHandle> probeInstanceQueue, NativeArray<SphericalHarmonicsL2> probeUpdateDataQueue,
            NativeArray<Vector4> probeOcclusionUpdateDataQueue, RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            EnsureProbeBuffersCapacity(queueCount);

            var gpuInstanceIndices = new NativeArray<GPUInstanceIndex>(queueCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            outputBuffer.CPUInstanceArrayToGPUInstanceArray(probeInstanceQueue.GetSubArray(0, queueCount), gpuInstanceIndices);

            Profiler.BeginSample("PrepareProbeUpdateDispatch");
            Profiler.BeginSample("ComputeBuffer.SetData");
            m_UpdateIndexQueueBuffer.SetData(gpuInstanceIndices, 0, 0, queueCount);
            m_ProbeUpdateDataQueueBuffer.SetData(probeUpdateDataQueue, 0, 0, queueCount);
            m_ProbeOcclusionUpdateDataQueueBuffer.SetData(probeOcclusionUpdateDataQueue, 0, 0, queueCount);
            Profiler.EndSample();
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._ProbeUpdateQueueCount, queueCount);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._SHUpdateVec4Offset, renderersParameters.shCoefficients.uintOffset);
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, InstanceTransformUpdateIDs._ProbeUpdateIndexQueue, m_UpdateIndexQueueBuffer);
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, InstanceTransformUpdateIDs._ProbeUpdateDataQueue, m_ProbeUpdateDataQueueBuffer);
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, InstanceTransformUpdateIDs._ProbeOcclusionUpdateDataQueue, m_ProbeOcclusionUpdateDataQueueBuffer);
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, InstanceTransformUpdateIDs._OutputProbeBuffer, outputBuffer.gpuBuffer);
            Profiler.EndSample();
            m_TransformUpdateCS.Dispatch(m_ProbeUpdateKernel, (queueCount + 63) / 64, 1, 1);

            gpuInstanceIndices.Dispose();
        }

        private void DispatchMotionUpdateCommand(int motionQueueCount, NativeArray<InstanceHandle> transformInstanceQueue, RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            EnsureTransformBuffersCapacity(motionQueueCount);

            var gpuInstanceIndices = new NativeArray<GPUInstanceIndex>(motionQueueCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            outputBuffer.CPUInstanceArrayToGPUInstanceArray(transformInstanceQueue.GetSubArray(0, motionQueueCount), gpuInstanceIndices);

            Profiler.BeginSample("PrepareMotionUpdateDispatch");
            Profiler.BeginSample("ComputeBuffer.SetData");
            m_UpdateIndexQueueBuffer.SetData(gpuInstanceIndices, 0, 0, motionQueueCount);
            Profiler.EndSample();
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateQueueCount, motionQueueCount);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputL2WVec4Offset, renderersParameters.localToWorld.uintOffset);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputW2LVec4Offset, renderersParameters.worldToLocal.uintOffset);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputPrevL2WVec4Offset, renderersParameters.matrixPreviousM.uintOffset);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputPrevW2LVec4Offset, renderersParameters.matrixPreviousMI.uintOffset);
            m_TransformUpdateCS.SetBuffer(m_MotionUpdateKernel, InstanceTransformUpdateIDs._TransformUpdateIndexQueue, m_UpdateIndexQueueBuffer);
            m_TransformUpdateCS.SetBuffer(m_MotionUpdateKernel, InstanceTransformUpdateIDs._OutputTransformBuffer, outputBuffer.gpuBuffer);
            Profiler.EndSample();
            m_TransformUpdateCS.Dispatch(m_MotionUpdateKernel, (motionQueueCount + 63) / 64, 1, 1);

            gpuInstanceIndices.Dispose();
        }

        private void DispatchTransformUpdateCommand(bool initialize, int transformQueueCount, NativeArray<InstanceHandle> transformInstanceQueue, NativeArray<TransformUpdatePacket> updateDataQueue,
            NativeArray<float4> boundingSphereUpdateDataQueue, RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            EnsureTransformBuffersCapacity(transformQueueCount);

            int transformQueueDataSize;
            int kernel;

            if(initialize)
            {
                // When we reinitialize we have the current and the previous matrices per transform.
                transformQueueDataSize = transformQueueCount * 2;
                kernel = m_TransformInitKernel;
            }
            else
            {
                transformQueueDataSize = transformQueueCount;
                kernel = m_TransformUpdateKernel;
            }

            var gpuInstanceIndices = new NativeArray<GPUInstanceIndex>(transformQueueCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            outputBuffer.CPUInstanceArrayToGPUInstanceArray(transformInstanceQueue.GetSubArray(0, transformQueueCount), gpuInstanceIndices);

            Profiler.BeginSample("PrepareTransformUpdateDispatch");
            Profiler.BeginSample("ComputeBuffer.SetData");
            m_UpdateIndexQueueBuffer.SetData(gpuInstanceIndices, 0, 0, transformQueueCount);
            m_TransformUpdateDataQueueBuffer.SetData(updateDataQueue, 0, 0, transformQueueDataSize);
            if (m_EnableBoundingSpheres)
                m_BoundingSpheresUpdateDataQueueBuffer.SetData(boundingSphereUpdateDataQueue, 0, 0, transformQueueCount);
            Profiler.EndSample();
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateQueueCount, transformQueueCount);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputL2WVec4Offset, renderersParameters.localToWorld.uintOffset);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputW2LVec4Offset, renderersParameters.worldToLocal.uintOffset);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputPrevL2WVec4Offset, renderersParameters.matrixPreviousM.uintOffset);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputPrevW2LVec4Offset, renderersParameters.matrixPreviousMI.uintOffset);
            m_TransformUpdateCS.SetBuffer(kernel, InstanceTransformUpdateIDs._TransformUpdateIndexQueue, m_UpdateIndexQueueBuffer);
            m_TransformUpdateCS.SetBuffer(kernel, InstanceTransformUpdateIDs._TransformUpdateDataQueue, m_TransformUpdateDataQueueBuffer);
            if (m_EnableBoundingSpheres)
            {
                Assert.IsTrue(renderersParameters.boundingSphere.valid);
                m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._BoundingSphereOutputVec4Offset, renderersParameters.boundingSphere.uintOffset);
                m_TransformUpdateCS.SetBuffer(kernel, InstanceTransformUpdateIDs._BoundingSphereDataQueue, m_BoundingSpheresUpdateDataQueueBuffer);
            }
            m_TransformUpdateCS.SetBuffer(kernel, InstanceTransformUpdateIDs._OutputTransformBuffer, outputBuffer.gpuBuffer);
            Profiler.EndSample();
            m_TransformUpdateCS.Dispatch(kernel, (transformQueueCount + 63) / 64, 1, 1);

            gpuInstanceIndices.Dispose();
        }

        private void DispatchWindDataCopyHistoryCommand(NativeArray<GPUInstanceIndex> gpuInstanceIndices, RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            Profiler.BeginSample("DispatchWindDataCopyHistory");

            int kernel = m_WindDataCopyHistoryKernel;
            int instancesCount = gpuInstanceIndices.Length;

            EnsureIndexQueueBufferCapacity(instancesCount);

            m_UpdateIndexQueueBuffer.SetData(gpuInstanceIndices, 0, 0, instancesCount);

            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindDataQueueCount, instancesCount);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindVectorAddress, renderersParameters.windVector.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindGlobalAddress, renderersParameters.windGlobal.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindBranchAddress, renderersParameters.windBranch.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindBranchTwitchAddress, renderersParameters.windBranchTwitch.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindBranchWhipAddress, renderersParameters.windBranchWhip.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindBranchAnchorAddress, renderersParameters.windBranchAnchor.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindBranchAdherencesAddress, renderersParameters.windBranchAdherences.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindTurbulencesAddress, renderersParameters.windTurbulences.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf1RippleAddress, renderersParameters.windLeaf1Ripple.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf1TwitchAddress, renderersParameters.windLeaf1Twitch.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf1TumbleAddress, renderersParameters.windLeaf1Tumble.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf2RippleAddress, renderersParameters.windLeaf2Ripple.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf2TumbleAddress, renderersParameters.windLeaf2Tumble.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf2TwitchAddress, renderersParameters.windLeaf2Twitch.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindFrondRippleAddress, renderersParameters.windFrondRipple.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindAnimationAddress, renderersParameters.windAnimation.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindVectorHistoryAddress, renderersParameters.windVectorHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindGlobalHistoryAddress, renderersParameters.windGlobalHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindBranchHistoryAddress, renderersParameters.windBranchHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindBranchTwitchHistoryAddress, renderersParameters.windBranchTwitchHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindBranchWhipHistoryAddress, renderersParameters.windBranchWhipHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindBranchAnchorHistoryAddress, renderersParameters.windBranchAnchorHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindBranchAdherencesHistoryAddress, renderersParameters.windBranchAdherencesHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindTurbulencesHistoryAddress, renderersParameters.windTurbulencesHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf1RippleHistoryAddress, renderersParameters.windLeaf1RippleHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf1TwitchHistoryAddress, renderersParameters.windLeaf1TwitchHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf1TumbleHistoryAddress, renderersParameters.windLeaf1TumbleHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf2RippleHistoryAddress, renderersParameters.windLeaf2RippleHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf2TumbleHistoryAddress, renderersParameters.windLeaf2TumbleHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindLeaf2TwitchHistoryAddress, renderersParameters.windLeaf2TwitchHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindFrondRippleHistoryAddress, renderersParameters.windFrondRippleHistory.gpuAddress);
            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindAnimationHistoryAddress, renderersParameters.windAnimationHistory.gpuAddress);
            m_WindDataUpdateCS.SetBuffer(kernel, InstanceWindDataUpdateIDs._WindDataUpdateIndexQueue, m_UpdateIndexQueueBuffer);
            m_WindDataUpdateCS.SetBuffer(kernel, InstanceWindDataUpdateIDs._WindDataBuffer, outputBuffer.gpuBuffer);
            m_WindDataUpdateCS.Dispatch(kernel, (instancesCount + 63) / 64, 1, 1);

            Profiler.EndSample();
        }

        private unsafe void UpdateInstanceMotionsData(in RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            var transformUpdateInstanceQueue = new NativeArray<InstanceHandle>(m_InstanceData.instancesLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var motionQueueCount = 0;

            new MotionUpdateJob()
            {
                queueWriteBase = 0,
                instanceData = m_InstanceData,
                atomicUpdateQueueCount = new UnsafeAtomicCounter32(&motionQueueCount),
                transformUpdateInstanceQueue = transformUpdateInstanceQueue,
            }.Schedule((m_InstanceData.instancesLength + 63) / 64, MotionUpdateJob.k_BatchSize).Complete();

            if (motionQueueCount > 0)
                DispatchMotionUpdateCommand(motionQueueCount, transformUpdateInstanceQueue, renderersParameters, outputBuffer);

            transformUpdateInstanceQueue.Dispose();
        }

        private unsafe void UpdateInstanceTransformsData(bool initialize, NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices, NativeArray<Matrix4x4> prevLocalToWorldMatrices,
            in RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            Assert.AreEqual(instances.Length, localToWorldMatrices.Length);
            Assert.AreEqual(instances.Length, prevLocalToWorldMatrices.Length);

            var transformUpdateInstanceQueue = new NativeArray<InstanceHandle>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            // When we reinitialize we have the current and the previous matrices per transform.
            var transformUpdateDataQueue = new NativeArray<TransformUpdatePacket>(initialize ? instances.Length * 2 : instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var boundingSpheresUpdateDataQueue = new NativeArray<float4>(m_EnableBoundingSpheres ? instances.Length : 0, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var probeInstanceQueue = new NativeArray<InstanceHandle>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var compactTetrahedronCache = new NativeArray<int>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var probeQueryPosition = new NativeArray<Vector3>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var probeUpdateDataQueue = new NativeArray<SphericalHarmonicsL2>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var probeOcclusionUpdateDataQueue = new NativeArray<Vector4>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var transformQueueCount = 0;
            int probesQueueCount = 0;

            var transformJob = new TransformUpdateJob()
            {
                initialize = initialize,
                enableBoundingSpheres = m_EnableBoundingSpheres,
                instances = instances,
                localToWorldMatrices = localToWorldMatrices,
                prevLocalToWorldMatrices = prevLocalToWorldMatrices,
                atomicTransformQueueCount = new UnsafeAtomicCounter32(&transformQueueCount),
                sharedInstanceData = m_SharedInstanceData,
                instanceData = m_InstanceData,
                transformUpdateInstanceQueue = transformUpdateInstanceQueue,
                transformUpdateDataQueue = transformUpdateDataQueue,
                boundingSpheresDataQueue = boundingSpheresUpdateDataQueue,
            };

            var probesJob = new ProbesUpdateJob()
            {
                initialize = initialize,
                instances = instances,
                instanceData = m_InstanceData,
                sharedInstanceData = m_SharedInstanceData,
                atomicProbesQueueCount = new UnsafeAtomicCounter32(&probesQueueCount),
                probeInstanceQueue = probeInstanceQueue,
                compactTetrahedronCache = compactTetrahedronCache,
                probeQueryPosition = probeQueryPosition
            };

            JobHandle jobHandle = transformJob.ScheduleBatch(instances.Length, TransformUpdateJob.k_BatchSize);
            jobHandle = probesJob.ScheduleBatch(instances.Length, ProbesUpdateJob.k_BatchSize, jobHandle);
            jobHandle.Complete();

            if (probesQueueCount > 0)
            {
                ScheduleInterpolateProbesAndUpdateTetrahedronCache(probesQueueCount, probeInstanceQueue, compactTetrahedronCache, probeQueryPosition,
                    probeUpdateDataQueue, probeOcclusionUpdateDataQueue).Complete();

                DispatchProbeUpdateCommand(probesQueueCount, probeInstanceQueue, probeUpdateDataQueue, probeOcclusionUpdateDataQueue, renderersParameters, outputBuffer);
            }

            if (transformQueueCount > 0)
            {
                DispatchTransformUpdateCommand(initialize, transformQueueCount, transformUpdateInstanceQueue, transformUpdateDataQueue, boundingSpheresUpdateDataQueue,
                    renderersParameters, outputBuffer);
            }

            transformUpdateInstanceQueue.Dispose();
            transformUpdateDataQueue.Dispose();
            boundingSpheresUpdateDataQueue.Dispose();

            probeInstanceQueue.Dispose();
            compactTetrahedronCache.Dispose();
            probeQueryPosition.Dispose();
            probeUpdateDataQueue.Dispose();
            probeOcclusionUpdateDataQueue.Dispose();
        }

        private unsafe void UpdateInstanceProbesData(NativeArray<InstanceHandle> instances, in RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            var probeInstanceQueue = new NativeArray<InstanceHandle>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var compactTetrahedronCache = new NativeArray<int>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var probeQueryPosition = new NativeArray<Vector3>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var probeUpdateDataQueue = new NativeArray<SphericalHarmonicsL2>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var probeOcclusionUpdateDataQueue = new NativeArray<Vector4>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int probesQueueCount = 0;

            new ProbesUpdateJob()
            {
                initialize = false,
                instances = instances,
                instanceData = m_InstanceData,
                sharedInstanceData = m_SharedInstanceData,
                atomicProbesQueueCount = new UnsafeAtomicCounter32(&probesQueueCount),
                probeInstanceQueue = probeInstanceQueue,
                compactTetrahedronCache = compactTetrahedronCache,
                probeQueryPosition = probeQueryPosition
            }.ScheduleBatch(instances.Length, ProbesUpdateJob.k_BatchSize).Complete();

            if (probesQueueCount > 0)
            {
                ScheduleInterpolateProbesAndUpdateTetrahedronCache(probesQueueCount, probeInstanceQueue, compactTetrahedronCache, probeQueryPosition,
                    probeUpdateDataQueue, probeOcclusionUpdateDataQueue).Complete();

                DispatchProbeUpdateCommand(probesQueueCount, probeInstanceQueue, probeUpdateDataQueue, probeOcclusionUpdateDataQueue, renderersParameters, outputBuffer);
            }

            probeInstanceQueue.Dispose();
            compactTetrahedronCache.Dispose();
            probeQueryPosition.Dispose();
            probeUpdateDataQueue.Dispose();
            probeOcclusionUpdateDataQueue.Dispose();
        }

        public void UpdateInstanceWindDataHistory(NativeArray<GPUInstanceIndex> gpuInstanceIndices, RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            if(gpuInstanceIndices.Length == 0)
                return;

            DispatchWindDataCopyHistoryCommand(gpuInstanceIndices, renderersParameters, outputBuffer);
        }

        public unsafe void ReallocateAndGetInstances(in GPUDrivenRendererGroupData rendererData, NativeArray<InstanceHandle> instances)
        {
            Assert.AreEqual(rendererData.localToWorldMatrix.Length, instances.Length);

            int newSharedInstancesCount = 0;
            int newInstancesCount = 0;

            bool implicitInstanceIndices = rendererData.instancesCount.Length == 0;

            if (implicitInstanceIndices)
            {
                var queryJob = new QueryRendererGroupInstancesJob()
                {
                    rendererGroupInstanceMultiHash = m_RendererGroupInstanceMultiHash,
                    rendererGroupIDs = rendererData.rendererGroupID,
                    instances = instances,
                    atomicNonFoundInstancesCount = new UnsafeAtomicCounter32(&newInstancesCount)
                };

                queryJob.ScheduleBatch(rendererData.rendererGroupID.Length, QueryRendererGroupInstancesJob.k_BatchSize).Complete();

                newSharedInstancesCount = newInstancesCount;
            }
            else
            {
                var queryJob = new QueryRendererGroupInstancesMultiJob()
                {
                    rendererGroupInstanceMultiHash = m_RendererGroupInstanceMultiHash,
                    rendererGroupIDs = rendererData.rendererGroupID,
                    instancesOffsets = rendererData.instancesOffset,
                    instancesCounts = rendererData.instancesCount,
                    instances = instances,
                    atomicNonFoundSharedInstancesCount = new UnsafeAtomicCounter32(&newSharedInstancesCount),
                    atomicNonFoundInstancesCount = new UnsafeAtomicCounter32(&newInstancesCount)
                };

                queryJob.ScheduleBatch(rendererData.rendererGroupID.Length, QueryRendererGroupInstancesMultiJob.k_BatchSize).Complete();
            }

            m_InstanceData.EnsureFreeInstances(newInstancesCount);
            m_SharedInstanceData.EnsureFreeInstances(newSharedInstancesCount);

            new ReallocateInstancesJob { implicitInstanceIndices = implicitInstanceIndices, rendererGroupInstanceMultiHash = m_RendererGroupInstanceMultiHash,
                instanceAllocators = m_InstanceAllocators, sharedInstanceData = m_SharedInstanceData, instanceData = m_InstanceData,
                rendererGroupIDs = rendererData.rendererGroupID, packedRendererData = rendererData.packedRendererData, instanceOffsets = rendererData.instancesOffset,
                instanceCounts = rendererData.instancesCount, instances = instances }.Run();
        }

        public void FreeRendererGroupInstances(NativeArray<int> rendererGroupsID)
        {
            new FreeRendererGroupInstancesJob { rendererGroupInstanceMultiHash = m_RendererGroupInstanceMultiHash,
                instanceAllocators = m_InstanceAllocators, sharedInstanceData = m_SharedInstanceData, instanceData = m_InstanceData,
                rendererGroupsID = rendererGroupsID }.Run();
        }

        public void FreeInstances(NativeArray<InstanceHandle> instances)
        {
            new FreeInstancesJob { rendererGroupInstanceMultiHash = m_RendererGroupInstanceMultiHash,
                instanceAllocators = m_InstanceAllocators, sharedInstanceData = m_SharedInstanceData, instanceData = m_InstanceData,
                instances = instances }.Run();
        }

        public JobHandle ScheduleUpdateInstanceDataJob(NativeArray<InstanceHandle> instances, in GPUDrivenRendererGroupData rendererData, NativeParallelHashMap<int, GPUInstanceIndex> lodGroupDataMap)
        {
            bool implicitInstanceIndices = rendererData.instancesCount.Length == 0;

            if(implicitInstanceIndices)
            {
                Assert.AreEqual(instances.Length, rendererData.rendererGroupID.Length);
            }
            else
            {
                Assert.AreEqual(rendererData.instancesCount.Length, rendererData.rendererGroupID.Length);
                Assert.AreEqual(rendererData.instancesOffset.Length, rendererData.rendererGroupID.Length);
            }

            Assert.AreEqual(instances.Length, rendererData.localToWorldMatrix.Length);

            return new UpdateRendererInstancesJob
            {
                implicitInstanceIndices = implicitInstanceIndices,
                instances = instances,
                rendererData = rendererData,
                lodGroupDataMap = lodGroupDataMap,
                instanceData = m_InstanceData,
                sharedInstanceData = m_SharedInstanceData
            }.Schedule(rendererData.rendererGroupID.Length, UpdateRendererInstancesJob.k_BatchSize);
        }

        public void UpdateAllInstanceProbes(in RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            var instances = m_InstanceData.instances.GetSubArray(0, m_InstanceData.instancesLength);

            if (instances.Length == 0)
                return;

            UpdateInstanceProbesData(instances, renderersParameters, outputBuffer);
        }

        public void InitializeInstanceTransforms(NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices,
            NativeArray<Matrix4x4> prevLocalToWorldMatrices, in RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            if (instances.Length == 0)
                return;

            UpdateInstanceTransformsData(true, instances, localToWorldMatrices, prevLocalToWorldMatrices, renderersParameters, outputBuffer);
        }

        public void UpdateInstanceTransforms(NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices,
            in RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            if (instances.Length == 0)
                return;

            UpdateInstanceTransformsData(false, instances, localToWorldMatrices, localToWorldMatrices, renderersParameters, outputBuffer);
        }

        public void UpdateInstanceMotions(in RenderersParameters renderersParameters, GPUInstanceDataBuffer outputBuffer)
        {
            if (m_InstanceData.instancesLength == 0)
                return;

            UpdateInstanceMotionsData(renderersParameters, outputBuffer);
        }

        public JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeArray<InstanceHandle> instances)
        {
            Assert.AreEqual(rendererGroupIDs.Length, instances.Length);

            if (rendererGroupIDs.Length == 0)
                return default;

            var queryJob = new QueryRendererGroupInstancesJob()
            {
                rendererGroupInstanceMultiHash = m_RendererGroupInstanceMultiHash,
                rendererGroupIDs = rendererGroupIDs,
                instances = instances
            };

            return queryJob.ScheduleBatch(rendererGroupIDs.Length, QueryRendererGroupInstancesJob.k_BatchSize);
        }

        public JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeList<InstanceHandle> instances)
        {
            if (rendererGroupIDs.Length == 0)
                return default;

            var instancesOffset = new NativeArray<int>(rendererGroupIDs.Length, Allocator.TempJob);
            var instancesCount = new NativeArray<int>(rendererGroupIDs.Length, Allocator.TempJob);

            var jobHandle = ScheduleQueryRendererGroupInstancesJob(rendererGroupIDs, instancesOffset, instancesCount, instances);

            instancesOffset.Dispose(jobHandle);
            instancesCount.Dispose(jobHandle);

            return jobHandle;
        }

        public JobHandle ScheduleQueryRendererGroupInstancesJob(NativeArray<int> rendererGroupIDs, NativeArray<int> instancesOffset, NativeArray<int> instancesCount, NativeList<InstanceHandle> instances)
        {
            Assert.AreEqual(rendererGroupIDs.Length, instancesOffset.Length);
            Assert.AreEqual(rendererGroupIDs.Length, instancesCount.Length);

            if (rendererGroupIDs.Length == 0)
                return default;

            var queryCountJobHandle = new QueryRendererGroupInstancesCountJob
            {
                instanceData = m_InstanceData,
                sharedInstanceData = m_SharedInstanceData,
                rendererGroupInstanceMultiHash = m_RendererGroupInstanceMultiHash,
                rendererGroupIDs = rendererGroupIDs,
                instancesCount = instancesCount,
            }.ScheduleBatch(rendererGroupIDs.Length, QueryRendererGroupInstancesCountJob.k_BatchSize);

            var computeOffsetsAndResizeArrayJobHandle = new ComputeInstancesOffsetAndResizeInstancesArrayJob
            {
                instancesCount = instancesCount,
                instancesOffset = instancesOffset,
                instances = instances
            }.Schedule(queryCountJobHandle);

            return new QueryRendererGroupInstancesMultiJob()
            {
                rendererGroupInstanceMultiHash = m_RendererGroupInstanceMultiHash,
                rendererGroupIDs = rendererGroupIDs,
                instancesOffsets = instancesOffset,
                instancesCounts = instancesCount,
                instances = instances.AsDeferredJobArray()
            }.ScheduleBatch(rendererGroupIDs.Length, QueryRendererGroupInstancesMultiJob.k_BatchSize, computeOffsetsAndResizeArrayJobHandle);
        }

        public JobHandle ScheduleQuerySortedMeshInstancesJob(NativeArray<int> sortedMeshIDs, NativeList<InstanceHandle> instances)
        {
            if (sortedMeshIDs.Length == 0)
                return default;

            instances.Capacity = m_InstanceData.instancesLength;

            var queryJob = new QuerySortedMeshInstancesJob()
            {
                instanceData = m_InstanceData,
                sharedInstanceData = m_SharedInstanceData,
                sortedMeshID = sortedMeshIDs,
                instances = instances
            };

            return queryJob.ScheduleBatch(m_InstanceData.instancesLength, QuerySortedMeshInstancesJob.k_BatchSize);
        }

        public JobHandle ScheduleCollectInstancesLODGroupAndMasksJob(NativeArray<InstanceHandle> instances, NativeArray<uint> lodGroupAndMasks)
        {
            Assert.AreEqual(instances.Length, lodGroupAndMasks.Length);

            return new CollectInstancesLODGroupsAndMasksJob
            {
                instanceData = instanceData,
                sharedInstanceData = sharedInstanceData,
                instances = instances,
                lodGroupAndMasks = lodGroupAndMasks
            }.Schedule(instances.Length, CollectInstancesLODGroupsAndMasksJob.k_BatchSize);
        }

        public bool InternalSanityCheckStates()
        {
            var instanceRefCountsHash = new NativeParallelHashMap<SharedInstanceHandle, int>(64, Allocator.Temp);

            int totalValidInstances = 0;

            for(int i = 0; i < m_InstanceData.handlesLength; ++i)
            {
                var instance = InstanceHandle.FromInt(i);

                if(m_InstanceData.IsValidInstance(instance))
                {
                    var sharedInstance = m_InstanceData.Get_SharedInstance(instance);

                    if (instanceRefCountsHash.TryGetValue(sharedInstance, out var refCounts))
                        instanceRefCountsHash[sharedInstance] = refCounts + 1;
                    else
                        instanceRefCountsHash.Add(sharedInstance, 1);

                    ++totalValidInstances;
                }
            }

            if (m_InstanceData.instancesLength != totalValidInstances)
                return false;

            int totalValidSharedInstances = 0;

            for (int i = 0; i < m_SharedInstanceData.handlesLength; ++i)
            {
                var sharedInstance = new SharedInstanceHandle { index = i };

                if (m_SharedInstanceData.IsValidInstance(sharedInstance))
                {
                    var refCount = m_SharedInstanceData.Get_RefCount(sharedInstance);

                    if (instanceRefCountsHash[sharedInstance] != refCount)
                        return false;

                    ++totalValidSharedInstances;
                }
            }

            if (m_SharedInstanceData.instancesLength != totalValidSharedInstances)
                return false;

            return true;
        }

        public unsafe void GetVisibleTreeInstances(in ParallelBitArray compactedVisibilityMasks, in ParallelBitArray processedBits, NativeList<int> visibeTreeRendererIDs,
            NativeList<InstanceHandle> visibeTreeInstances, bool becomeVisibleOnly, out int becomeVisibeTreeInstancesCount)
        {
            Assert.AreEqual(visibeTreeRendererIDs.Length, 0);
            Assert.AreEqual(visibeTreeInstances.Length, 0);

            becomeVisibeTreeInstancesCount = 0;

            int maxTreeInstancesCount = GetAliveInstancesOfType(InstanceType.SpeedTree);

            if (maxTreeInstancesCount == 0)
                return;

            visibeTreeRendererIDs.ResizeUninitialized(maxTreeInstancesCount);
            visibeTreeInstances.ResizeUninitialized(maxTreeInstancesCount);

            int visibleTreeInstancesCount = 0;

            new GetVisibleNonProcessedTreeInstancesJob
            {
                becomeVisible = true,
                instanceData = m_InstanceData,
                sharedInstanceData = m_SharedInstanceData,
                compactedVisibilityMasks = compactedVisibilityMasks,
                processedBits = processedBits,
                rendererIDs = visibeTreeRendererIDs.AsArray(),
                instances = visibeTreeInstances.AsArray(),
                atomicTreeInstancesCount = new UnsafeAtomicCounter32(&visibleTreeInstancesCount)
            }.ScheduleBatch(m_InstanceData.instancesLength, GetVisibleNonProcessedTreeInstancesJob.k_BatchSize).Complete();

            becomeVisibeTreeInstancesCount = visibleTreeInstancesCount;

            if(!becomeVisibleOnly)
            {
                new GetVisibleNonProcessedTreeInstancesJob
                {
                    becomeVisible = false,
                    instanceData = m_InstanceData,
                    sharedInstanceData = m_SharedInstanceData,
                    compactedVisibilityMasks = compactedVisibilityMasks,
                    processedBits = processedBits,
                    rendererIDs = visibeTreeRendererIDs.AsArray(),
                    instances = visibeTreeInstances.AsArray(),
                    atomicTreeInstancesCount = new UnsafeAtomicCounter32(&visibleTreeInstancesCount)
                }.ScheduleBatch(m_InstanceData.instancesLength, GetVisibleNonProcessedTreeInstancesJob.k_BatchSize).Complete();
            }

            Assert.IsTrue(becomeVisibeTreeInstancesCount <= visibleTreeInstancesCount);
            Assert.IsTrue(visibleTreeInstancesCount <= maxTreeInstancesCount);

            visibeTreeRendererIDs.ResizeUninitialized(visibleTreeInstancesCount);
            visibeTreeInstances.ResizeUninitialized(visibleTreeInstancesCount);
        }

        public void UpdatePerFrameInstanceVisibility(in ParallelBitArray compactedVisibilityMasks)
        {
            Assert.AreEqual(m_InstanceData.handlesLength, compactedVisibilityMasks.Length);

            var updateCompactedInstanceVisibilityJob = new UpdateCompactedInstanceVisibilityJob
            {
                instanceData = m_InstanceData,
                compactedVisibilityMasks = compactedVisibilityMasks
            };

            updateCompactedInstanceVisibilityJob.ScheduleBatch(m_InstanceData.instancesLength, UpdateCompactedInstanceVisibilityJob.k_BatchSize).Complete();
        }

        private static class InstanceTransformUpdateIDs
        {
            // Transform update kernel IDs
            public static readonly int _TransformUpdateQueueCount = Shader.PropertyToID("_TransformUpdateQueueCount");
            public static readonly int _TransformUpdateOutputL2WVec4Offset = Shader.PropertyToID("_TransformUpdateOutputL2WVec4Offset");
            public static readonly int _TransformUpdateOutputW2LVec4Offset = Shader.PropertyToID("_TransformUpdateOutputW2LVec4Offset");
            public static readonly int _TransformUpdateOutputPrevL2WVec4Offset = Shader.PropertyToID("_TransformUpdateOutputPrevL2WVec4Offset");
            public static readonly int _TransformUpdateOutputPrevW2LVec4Offset = Shader.PropertyToID("_TransformUpdateOutputPrevW2LVec4Offset");
            public static readonly int _BoundingSphereOutputVec4Offset = Shader.PropertyToID("_BoundingSphereOutputVec4Offset");
            public static readonly int _TransformUpdateDataQueue = Shader.PropertyToID("_TransformUpdateDataQueue");
            public static readonly int _TransformUpdateIndexQueue = Shader.PropertyToID("_TransformUpdateIndexQueue");
            public static readonly int _BoundingSphereDataQueue = Shader.PropertyToID("_BoundingSphereDataQueue");
            public static readonly int _OutputTransformBuffer = Shader.PropertyToID("_OutputTransformBuffer");

            // Probe update kernel IDs
            public static readonly int _ProbeUpdateQueueCount = Shader.PropertyToID("_ProbeUpdateQueueCount");
            public static readonly int _SHUpdateVec4Offset = Shader.PropertyToID("_SHUpdateVec4Offset");
            public static readonly int _ProbeUpdateDataQueue = Shader.PropertyToID("_ProbeUpdateDataQueue");
            public static readonly int _ProbeOcclusionUpdateDataQueue = Shader.PropertyToID("_ProbeOcclusionUpdateDataQueue");
            public static readonly int _ProbeUpdateIndexQueue = Shader.PropertyToID("_ProbeUpdateIndexQueue");
            public static readonly int _OutputProbeBuffer = Shader.PropertyToID("_OutputProbeBuffer");
        }

        private static class InstanceWindDataUpdateIDs
        {
            public static readonly int _WindDataQueueCount = Shader.PropertyToID("_WindDataQueueCount");
            public static readonly int _WindVectorAddress = Shader.PropertyToID("_WindVectorAddress");
            public static readonly int _WindGlobalAddress = Shader.PropertyToID("_WindGlobalAddress");
            public static readonly int _WindBranchAddress = Shader.PropertyToID("_WindBranchAddress");
            public static readonly int _WindBranchTwitchAddress = Shader.PropertyToID("_WindBranchTwitchAddress");
            public static readonly int _WindBranchWhipAddress = Shader.PropertyToID("_WindBranchWhipAddress");
            public static readonly int _WindBranchAnchorAddress = Shader.PropertyToID("_WindBranchAnchorAddress");
            public static readonly int _WindBranchAdherencesAddress = Shader.PropertyToID("_WindBranchAdherencesAddress");
            public static readonly int _WindTurbulencesAddress = Shader.PropertyToID("_WindTurbulencesAddress");
            public static readonly int _WindLeaf1RippleAddress = Shader.PropertyToID("_WindLeaf1RippleAddress");
            public static readonly int _WindLeaf1TwitchAddress = Shader.PropertyToID("_WindLeaf1TwitchAddress");
            public static readonly int _WindLeaf1TumbleAddress = Shader.PropertyToID("_WindLeaf1TumbleAddress");
            public static readonly int _WindLeaf2RippleAddress = Shader.PropertyToID("_WindLeaf2RippleAddress");
            public static readonly int _WindLeaf2TumbleAddress = Shader.PropertyToID("_WindLeaf2TumbleAddress");
            public static readonly int _WindLeaf2TwitchAddress = Shader.PropertyToID("_WindLeaf2TwitchAddress");
            public static readonly int _WindFrondRippleAddress = Shader.PropertyToID("_WindFrondRippleAddress");
            public static readonly int _WindAnimationAddress = Shader.PropertyToID("_WindAnimationAddress");
            public static readonly int _WindVectorHistoryAddress = Shader.PropertyToID("_WindVectorHistoryAddress");
            public static readonly int _WindGlobalHistoryAddress = Shader.PropertyToID("_WindGlobalHistoryAddress");
            public static readonly int _WindBranchHistoryAddress = Shader.PropertyToID("_WindBranchHistoryAddress");
            public static readonly int _WindBranchTwitchHistoryAddress = Shader.PropertyToID("_WindBranchTwitchHistoryAddress");
            public static readonly int _WindBranchWhipHistoryAddress = Shader.PropertyToID("_WindBranchWhipHistoryAddress");
            public static readonly int _WindBranchAnchorHistoryAddress = Shader.PropertyToID("_WindBranchAnchorHistoryAddress");
            public static readonly int _WindBranchAdherencesHistoryAddress = Shader.PropertyToID("_WindBranchAdherencesHistoryAddress");
            public static readonly int _WindTurbulencesHistoryAddress = Shader.PropertyToID("_WindTurbulencesHistoryAddress");
            public static readonly int _WindLeaf1RippleHistoryAddress = Shader.PropertyToID("_WindLeaf1RippleHistoryAddress");
            public static readonly int _WindLeaf1TumbleHistoryAddress = Shader.PropertyToID("_WindLeaf1TumbleHistoryAddress");
            public static readonly int _WindLeaf1TwitchHistoryAddress = Shader.PropertyToID("_WindLeaf1TwitchHistoryAddress");
            public static readonly int _WindLeaf2RippleHistoryAddress = Shader.PropertyToID("_WindLeaf2RippleHistoryAddress");
            public static readonly int _WindLeaf2TumbleHistoryAddress = Shader.PropertyToID("_WindLeaf2TumbleHistoryAddress");
            public static readonly int _WindLeaf2TwitchHistoryAddress = Shader.PropertyToID("_WindLeaf2TwitchHistoryAddress");
            public static readonly int _WindFrondRippleHistoryAddress = Shader.PropertyToID("_WindFrondRippleHistoryAddress");
            public static readonly int _WindAnimationHistoryAddress = Shader.PropertyToID("_WindAnimationHistoryAddress");
            public static readonly int _WindDataUpdateIndexQueue = Shader.PropertyToID("_WindDataUpdateIndexQueue");
            public static readonly int _WindDataBuffer = Shader.PropertyToID("_WindDataBuffer");
        }
    }
}
