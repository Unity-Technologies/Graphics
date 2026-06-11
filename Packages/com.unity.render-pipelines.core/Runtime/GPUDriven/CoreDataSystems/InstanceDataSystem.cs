#if !UNITY_WEBGL_RENDERER_ONLY
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal partial class InstanceDataSystem : IDisposable
    {
        // NOTE: Hard-coded, separate declaration to avoid pulling Terrain module dependency because of a single value.
        //       Kept in sync with SpeedTreeWindParamIndex.MaxWindParamsCount through GPUDrivenRenderingTests.
        internal const int k_STMaxWindParamsCount = 16;

        private NativeReference<GPUArchetypeManager> m_ArchetypeManager;
        private DefaultGPUComponents m_DefaultGPUComponents;
        private GPUInstanceDataBuffer m_InstanceDataBuffer;
        private InstanceAllocators m_InstanceAllocators;
        private RenderWorld m_RenderWorld;
        private NativeParallelHashMap<EntityId, InstanceHandle> m_RendererToInstanceMap;
        private GPUCapacityResizingPolicy m_GPUResizingPolicy;
        private CommandBuffer m_CmdBuffer;

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

        private readonly int[] m_ScratchWindParamAddressArray = new int[k_STMaxWindParamsCount * 4];

        /// <summary>
        /// Runs light probe interpolation and tetrahedron cache update jobs for all instances in
        /// the probe update queue. Computes SH coefficients and occlusion values per instance.
        /// Cost scales with the number of instances that use blended light probes.
        /// </summary>
        static readonly ProfilerMarker k_InterpolateProbesAndUpdateTetrahedronCache =
            new ProfilerMarker(ProfilerCategory.Render, "InterpolateProbesAndUpdateTetrahedronCache", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Uploads SH and occlusion probe data to the GPU instance buffer via a compute shader
        /// scatter-write. Includes a <c>ComputeBuffer.SetData</c> call and an immediate dispatch.
        /// </summary>
        internal static readonly ProfilerMarker k_DispatchProbeUpdateCommand =
            new ProfilerMarker(ProfilerCategory.Render, "DispatchProbeUpdateCommand", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Uploads previous-frame transform matrices to the GPU instance buffer via a compute
        /// shader scatter-write to enable per-object motion vectors. Includes a
        /// <c>ComputeBuffer.SetData</c> call and an immediate dispatch.
        /// </summary>
        internal static readonly ProfilerMarker k_DispatchMotionUpdateCommand =
            new ProfilerMarker(ProfilerCategory.Render, "DispatchMotionUpdateCommand", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Uploads object-to-world and world-to-object matrices to the GPU instance buffer via
        /// a compute shader scatter-write. Optionally uploads bounding spheres when enabled.
        /// Includes <c>ComputeBuffer.SetData</c> calls and an immediate dispatch.
        /// </summary>
        internal static readonly ProfilerMarker k_DispatchTransformUpdateCommand =
            new ProfilerMarker(ProfilerCategory.Render, "DispatchTransformUpdateCommand", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Copies current-frame SpeedTree wind parameters into the history slots in the GPU
        /// instance buffer via a compute shader scatter-write, enabling wind continuity across
        /// frames.
        /// </summary>
        static readonly ProfilerMarker k_DispatchWindDataCopyHistory =
            new ProfilerMarker(ProfilerCategory.Render, "DispatchWindDataCopyHistory", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Transfers CPU staging buffers to <c>ComputeBuffer</c> slots on the GPU synchronously.
        /// Appears inside each dispatch marker; cost scales with the size of the data being
        /// uploaded and stalls the CPU until the transfer completes.
        /// </summary>
        static readonly ProfilerMarker k_ComputeBufferSetData =
            new ProfilerMarker(ProfilerCategory.Render, "ComputeBuffer.SetData", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Allocates temporary <c>NativeArray</c> staging buffers for the transform and probe
        /// update queues. Allocates additional probe arrays only when any instance uses blended
        /// light probes.
        /// </summary>
        static readonly ProfilerMarker k_AllocateBuffers =
            new ProfilerMarker(ProfilerCategory.Render, "AllocateBuffers", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Runs the <c>TransformUpdateJob</c> and optionally the <c>ProbesUpdateJob</c> in
        /// parallel, writing transform packets and SH data into the staging queues consumed by
        /// the subsequent GPU dispatch commands.
        /// </summary>
        static readonly ProfilerMarker k_UpdateTransformsAndProbes =
            new ProfilerMarker(ProfilerCategory.Render, "UpdateTransformsAndProbes", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Runs <c>UpdateRendererInstancesJob</c> in parallel to write bounds, LOD group,
        /// render settings, and other per-instance renderer data into the render world for the
        /// given update batch.
        /// </summary>
        static readonly ProfilerMarker k_UpdateInstanceData =
            new ProfilerMarker(ProfilerCategory.Render, "UpdateInstanceData", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Looks up the <c>InstanceHandle</c> for each renderer entity ID in parallel using the
        /// renderer-to-instance map, writing results into the caller-provided instances array.
        /// </summary>
        static readonly ProfilerMarker k_QueryRendererInstances =
            new ProfilerMarker(ProfilerCategory.Render, "QueryRendererInstances", MarkerFlags.VerbosityAdvanced);

        /// <summary>
        /// Recomputes the ground-truth total tree count from renderer settings and asserts it
        /// matches the cached value. Only runs when deep validation is enabled.
        /// </summary>
        static readonly ProfilerMarker k_DeepValidateTotalTreeCount =
            new ProfilerMarker(ProfilerCategory.Render, "DeepValidate.TotalTreeCount", MarkerFlags.VerbosityAdvanced);

        public NativeReference<GPUArchetypeManager> archetypeManager => m_ArchetypeManager;
        public ref DefaultGPUComponents defaultGPUComponents => ref m_DefaultGPUComponents;
        public bool hasBoundingSpheres => m_EnableBoundingSpheres;
        public int totalTreeCount => m_RenderWorld.totalTreeCount;
        public GPUInstanceDataBuffer gpuBuffer => m_InstanceDataBuffer;
        public GraphicsBufferHandle gpuBufferHandle => m_InstanceDataBuffer.nativeBuffer.bufferHandle;
        public int gpuBufferLayoutVersion => m_InstanceDataBuffer.layoutVersion;
        public ref RenderWorld renderWorld => ref m_RenderWorld;
        public NativeArray<InstanceHandle> indexToHandle => m_RenderWorld.indexToHandle;
        public NativeArray<MetadataValue>.ReadOnly componentsMetadata => m_InstanceDataBuffer.componentsMetadata;

        public event Action onGPUBufferLayoutChanged;

        public InstanceDataSystem(int instancesCPUCapacity,
            bool enableBoundingSpheres,
            GPUResidentDrawerResources resources,
            GPUCapacityResizingPolicy gpuResizingPolicy = GPUCapacityResizingPolicy.DoubleOnGrow_HalveOnQuarterShrink)
        {
            m_ArchetypeManager = new NativeReference<GPUArchetypeManager>(Allocator.Persistent);
            m_ArchetypeManager.GetRef().Initialize();

            m_DefaultGPUComponents = new DefaultGPUComponents(ref m_ArchetypeManager.GetRef(), enableBoundingSpheres);

            m_InstanceDataBuffer = new GPUInstanceDataBuffer(ref m_ArchetypeManager.GetRef(), new GPUInstanceDataBufferLayout(1, Allocator.Temp)
            {
                { m_DefaultGPUComponents.defaultGOArchetype, 0 }
            },
            resources);

            m_GPUResizingPolicy = gpuResizingPolicy;

            m_InstanceAllocators = new InstanceAllocators();

            m_InstanceAllocators.Initialize();
            m_RenderWorld.Initialize(instancesCPUCapacity);

            m_CmdBuffer = new CommandBuffer { name = "InstanceDataSystemBuffer" };

            m_RendererToInstanceMap = new NativeParallelHashMap<EntityId, InstanceHandle>(instancesCPUCapacity, Allocator.Persistent);

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
            m_CmdBuffer.Release();

            m_InstanceAllocators.Dispose();
            m_RenderWorld.Dispose();

            m_RendererToInstanceMap.Dispose();

            m_UpdateIndexQueueBuffer?.Dispose();
            m_ProbeUpdateDataQueueBuffer?.Dispose();
            m_ProbeOcclusionUpdateDataQueueBuffer?.Dispose();
            m_TransformUpdateDataQueueBuffer?.Dispose();
            m_BoundingSpheresUpdateDataQueueBuffer?.Dispose();

            m_InstanceDataBuffer.Dispose();

            m_DefaultGPUComponents.Dispose();
            m_ArchetypeManager.GetRef().Dispose();
            m_ArchetypeManager.Dispose();
        }

        private void EnsureIndexQueueBufferCapacity(int capacity)
        {
            if(m_UpdateIndexQueueBuffer == null || m_UpdateIndexQueueBuffer.count < capacity)
            {
                m_UpdateIndexQueueBuffer?.Dispose();
                m_UpdateIndexQueueBuffer = new ComputeBuffer(capacity, sizeof(int), ComputeBufferType.Raw);
            }
        }

        private void EnsureProbeBuffersCapacity(int capacity)
        {
            EnsureIndexQueueBufferCapacity(capacity);

            if (m_ProbeUpdateDataQueueBuffer == null || m_ProbeUpdateDataQueueBuffer.count < capacity)
            {
                m_ProbeUpdateDataQueueBuffer?.Dispose();
                m_ProbeOcclusionUpdateDataQueueBuffer?.Dispose();
                m_ProbeUpdateDataQueueBuffer = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<SHUpdatePacket>(), ComputeBufferType.Structured);
                m_ProbeOcclusionUpdateDataQueueBuffer = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<Vector4>(), ComputeBufferType.Structured);
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
                m_TransformUpdateDataQueueBuffer = new ComputeBuffer(transformsCapacity, UnsafeUtility.SizeOf<TransformUpdatePacket>(), ComputeBufferType.Structured);
                if (m_EnableBoundingSpheres)
                    m_BoundingSpheresUpdateDataQueueBuffer = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4>(), ComputeBufferType.Structured);
            }
        }

        private JobHandle ScheduleInterpolateProbesAndUpdateTetrahedronCache(int queueCount, NativeArray<InstanceHandle> probeUpdateInstanceQueue,
            NativeArray<int> compactTetrahedronCache,
            NativeArray<Vector3> probeQueryPosition,
            NativeArray<SphericalHarmonicsL2> probeUpdateDataQueue,
            NativeArray<Vector4> probeOcclusionUpdateDataQueue)
        {
            var lightProbesQuery = new LightProbesQuery(Allocator.TempJob);

            var calculateProbesJob = new CalculateInterpolatedLightAndOcclusionProbesBatchJob
            {
                lightProbesQuery = lightProbesQuery,
                probesCount = queueCount,
                queryPostitions = probeQueryPosition,
                compactTetrahedronCache = compactTetrahedronCache,
                probesSphericalHarmonics = probeUpdateDataQueue,
                probesOcclusion = probeOcclusionUpdateDataQueue
            };

            var totalBatchCount = 1 + (queueCount / CalculateInterpolatedLightAndOcclusionProbesBatchJob.k_CalculatedProbesPerBatch);

            var calculateProbesJobHandle = calculateProbesJob.ScheduleByRef(totalBatchCount, 1);

            lightProbesQuery.Dispose(calculateProbesJobHandle);

            var scatterTetrahedronCacheIndicesJob = new ScatterTetrahedronCacheIndicesJob
            {
                compactTetrahedronCache = compactTetrahedronCache,
                probeInstances = probeUpdateInstanceQueue,
                renderWorld = m_RenderWorld
            };

            return scatterTetrahedronCacheIndicesJob.ScheduleByRef(queueCount, 128, calculateProbesJobHandle);
        }

        private void InterpolateProbesAndUpdateTetrahedronCache(int queueCount, NativeArray<InstanceHandle> probeUpdateInstanceQueue,
            NativeArray<int> compactTetrahedronCache,
            NativeArray<Vector3> probeQueryPosition,
            NativeArray<SphericalHarmonicsL2> probeUpdateDataQueue,
            NativeArray<Vector4> probeOcclusionUpdateDataQueue)
        {
            using var _ = k_InterpolateProbesAndUpdateTetrahedronCache.Auto();

            ScheduleInterpolateProbesAndUpdateTetrahedronCache(queueCount,
                probeUpdateInstanceQueue,
                compactTetrahedronCache,
                probeQueryPosition,
                probeUpdateDataQueue,
                probeOcclusionUpdateDataQueue)
                .Complete();
        }

        private void DispatchProbeUpdateCommand(int queueCount,
            NativeArray<InstanceHandle> probeInstanceQueue,
            NativeArray<SphericalHarmonicsL2> probeUpdateDataQueue,
            NativeArray<Vector4> probeOcclusionUpdateDataQueue)
        {
            using var _ = k_DispatchProbeUpdateCommand.Auto();

            EnsureProbeBuffersCapacity(queueCount);

            var gpuIndices = new NativeArray<GPUInstanceIndex>(queueCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_InstanceDataBuffer.QueryInstanceGPUIndices(m_RenderWorld, probeInstanceQueue.GetSubArray(0, queueCount), gpuIndices);

            using (k_ComputeBufferSetData.Auto())
            {
                m_UpdateIndexQueueBuffer.SetData(gpuIndices, 0, 0, queueCount);
                m_ProbeUpdateDataQueueBuffer.SetData(probeUpdateDataQueue, 0, 0, queueCount);
                m_ProbeOcclusionUpdateDataQueueBuffer.SetData(probeOcclusionUpdateDataQueue, 0, 0, queueCount);
            }

            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._ProbeUpdateQueueCount, queueCount);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._SHUpdateVec4Offset, m_InstanceDataBuffer.GetComponentGPUUIntOffset(m_DefaultGPUComponents.shCoefficients));
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, InstanceTransformUpdateIDs._ProbeUpdateIndexQueue, m_UpdateIndexQueueBuffer);
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, InstanceTransformUpdateIDs._ProbeUpdateDataQueue, m_ProbeUpdateDataQueueBuffer);
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, InstanceTransformUpdateIDs._ProbeOcclusionUpdateDataQueue, m_ProbeOcclusionUpdateDataQueueBuffer);
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, InstanceTransformUpdateIDs._OutputProbeBuffer, m_InstanceDataBuffer.nativeBuffer);
            m_TransformUpdateCS.Dispatch(m_ProbeUpdateKernel, (queueCount + 63) / 64, 1, 1);

            gpuIndices.Dispose();
        }

        private void DispatchMotionUpdateCommand(int motionQueueCount, NativeArray<InstanceHandle> transformInstanceQueue)
        {
            using var _ = k_DispatchMotionUpdateCommand.Auto();

            EnsureTransformBuffersCapacity(motionQueueCount);

            var gpuIndices = new NativeArray<GPUInstanceIndex>(motionQueueCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_InstanceDataBuffer.QueryInstanceGPUIndices(m_RenderWorld, transformInstanceQueue.GetSubArray(0, motionQueueCount), gpuIndices);

            using (k_ComputeBufferSetData.Auto())
            {
                m_UpdateIndexQueueBuffer.SetData(gpuIndices, 0, 0, motionQueueCount);
            }

            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateQueueCount, motionQueueCount);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputL2WVec4Offset, m_InstanceDataBuffer.GetComponentGPUUIntOffset(m_DefaultGPUComponents.objectToWorld));
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputW2LVec4Offset, m_InstanceDataBuffer.GetComponentGPUUIntOffset(m_DefaultGPUComponents.worldToObject));
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputPrevL2WVec4Offset, m_InstanceDataBuffer.GetComponentGPUUIntOffset(m_DefaultGPUComponents.matrixPreviousM));
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputPrevW2LVec4Offset, m_InstanceDataBuffer.GetComponentGPUUIntOffset(m_DefaultGPUComponents.matrixPreviousMI));
            m_TransformUpdateCS.SetBuffer(m_MotionUpdateKernel, InstanceTransformUpdateIDs._TransformUpdateIndexQueue, m_UpdateIndexQueueBuffer);
            m_TransformUpdateCS.SetBuffer(m_MotionUpdateKernel, InstanceTransformUpdateIDs._OutputTransformBuffer, m_InstanceDataBuffer.nativeBuffer);
            m_TransformUpdateCS.Dispatch(m_MotionUpdateKernel, (motionQueueCount + 63) / 64, 1, 1);

            gpuIndices.Dispose();
        }

        private void DispatchTransformUpdateCommand(bool initialize,
            int transformQueueCount,
            NativeArray<InstanceHandle> transformInstanceQueue,
            NativeArray<TransformUpdatePacket> updateDataQueue,
            NativeArray<float4> boundingSphereUpdateDataQueue)
        {
            using var _ = k_DispatchTransformUpdateCommand.Auto();

            EnsureTransformBuffersCapacity(transformQueueCount);

            int transformQueueDataSize;
            int kernel;

            if (initialize)
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

            var gpuIndices = new NativeArray<GPUInstanceIndex>(transformQueueCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_InstanceDataBuffer.QueryInstanceGPUIndices(m_RenderWorld, transformInstanceQueue.GetSubArray(0, transformQueueCount), gpuIndices);

            using (k_ComputeBufferSetData.Auto())
            {
                m_UpdateIndexQueueBuffer.SetData(gpuIndices, 0, 0, transformQueueCount);
                m_TransformUpdateDataQueueBuffer.SetData(updateDataQueue, 0, 0, transformQueueDataSize);
                if (m_EnableBoundingSpheres)
                    m_BoundingSpheresUpdateDataQueueBuffer.SetData(boundingSphereUpdateDataQueue, 0, 0, transformQueueCount);
            }

            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateQueueCount, transformQueueCount);
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputL2WVec4Offset, m_InstanceDataBuffer.GetComponentGPUUIntOffset(m_DefaultGPUComponents.objectToWorld));
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputW2LVec4Offset, m_InstanceDataBuffer.GetComponentGPUUIntOffset(m_DefaultGPUComponents.worldToObject));
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputPrevL2WVec4Offset, m_InstanceDataBuffer.GetComponentGPUUIntOffset(m_DefaultGPUComponents.matrixPreviousM));
            m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._TransformUpdateOutputPrevW2LVec4Offset, m_InstanceDataBuffer.GetComponentGPUUIntOffset(m_DefaultGPUComponents.matrixPreviousMI));
            m_TransformUpdateCS.SetBuffer(kernel, InstanceTransformUpdateIDs._TransformUpdateIndexQueue, m_UpdateIndexQueueBuffer);
            m_TransformUpdateCS.SetBuffer(kernel, InstanceTransformUpdateIDs._TransformUpdateDataQueue, m_TransformUpdateDataQueueBuffer);
            if (m_EnableBoundingSpheres)
            {
                Assert.IsTrue(m_DefaultGPUComponents.boundingSphere.valid);
                m_TransformUpdateCS.SetInt(InstanceTransformUpdateIDs._BoundingSphereOutputVec4Offset, m_InstanceDataBuffer.GetComponentGPUUIntOffset(m_DefaultGPUComponents.boundingSphere));
                m_TransformUpdateCS.SetBuffer(kernel, InstanceTransformUpdateIDs._BoundingSphereDataQueue, m_BoundingSpheresUpdateDataQueueBuffer);
            }
            m_TransformUpdateCS.SetBuffer(kernel, InstanceTransformUpdateIDs._OutputTransformBuffer, m_InstanceDataBuffer.nativeBuffer);
            m_TransformUpdateCS.Dispatch(kernel, (transformQueueCount + 63) / 64, 1, 1);

            gpuIndices.Dispose();
        }

        private void DispatchWindDataCopyHistoryCommand(NativeArray<GPUInstanceIndex> gpuIndices)
        {
            using var _ = k_DispatchWindDataCopyHistory.Auto();

            int kernel = m_WindDataCopyHistoryKernel;
            int instancesCount = gpuIndices.Length;

            EnsureIndexQueueBufferCapacity(instancesCount);

            using (k_ComputeBufferSetData.Auto())
            {
                m_UpdateIndexQueueBuffer.SetData(gpuIndices, 0, 0, instancesCount);
            }

            m_WindDataUpdateCS.SetInt(InstanceWindDataUpdateIDs._WindDataQueueCount, instancesCount);
            for (int i = 0; i < k_STMaxWindParamsCount; ++i)
                m_ScratchWindParamAddressArray[i * 4] = m_InstanceDataBuffer.GetComponentGPUAddress(m_DefaultGPUComponents.speedTreeWind[i]);
            m_WindDataUpdateCS.SetInts(InstanceWindDataUpdateIDs._WindParamAddressArray, m_ScratchWindParamAddressArray);
            for (int i = 0; i < k_STMaxWindParamsCount; ++i)
                m_ScratchWindParamAddressArray[i * 4] = m_InstanceDataBuffer.GetComponentGPUAddress(m_DefaultGPUComponents.speedTreeWindHistory[i]);
            m_WindDataUpdateCS.SetInts(InstanceWindDataUpdateIDs._WindHistoryParamAddressArray, m_ScratchWindParamAddressArray);

            m_WindDataUpdateCS.SetBuffer(kernel, InstanceWindDataUpdateIDs._WindDataUpdateIndexQueue, m_UpdateIndexQueueBuffer);
            m_WindDataUpdateCS.SetBuffer(kernel, InstanceWindDataUpdateIDs._WindDataBuffer, m_InstanceDataBuffer.nativeBuffer);
            m_WindDataUpdateCS.Dispatch(kernel, (instancesCount + 63) / 64, 1, 1);
        }

        private unsafe void UpdateInstanceMotionsDataInternal()
        {
            var transformUpdateInstanceQueue = new NativeArray<InstanceHandle>(m_RenderWorld.instanceCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var motionQueueCount = 0;

            new MotionUpdateJob
            {
                queueWriteBase = 0,
                renderWorld = m_RenderWorld,
                atomicUpdateQueueCount = new UnsafeAtomicCounter32(&motionQueueCount),
                transformUpdateInstanceQueue = transformUpdateInstanceQueue,
            }
            .RunParallel((m_RenderWorld.instanceCount + 63) / 64, 16);

            if (motionQueueCount > 0)
                DispatchMotionUpdateCommand(motionQueueCount, transformUpdateInstanceQueue);

            transformUpdateInstanceQueue.Dispose();
        }

        private unsafe void UpdateInstanceTransformsData(bool initialize,
            NativeArray<InstanceHandle> instances,
            JaggedSpan<float4x4> jaggedLocalToWorldMatrices,
            JaggedSpan<float4x4> jaggedPrevLocalToWorldMatrices,
            bool anyInstanceUseBlendProbes)
        {
            Assert.AreEqual(instances.Length, jaggedLocalToWorldMatrices.totalLength);
            Assert.AreEqual(instances.Length, jaggedPrevLocalToWorldMatrices.totalLength);
            Assert.IsTrue(jaggedLocalToWorldMatrices.HasSameLayout(jaggedPrevLocalToWorldMatrices));
            if (instances.Length == 0)
                return;

            k_AllocateBuffers.Begin();
            var transformUpdateInstanceQueue = new NativeArray<InstanceHandle>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            // When we reinitialize we have the current and the previous matrices per transform.
            var transformUpdateDataQueue = new NativeArray<TransformUpdatePacket>(initialize ? instances.Length * 2 : instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var boundingSpheresUpdateDataQueue = new NativeArray<float4>(m_EnableBoundingSpheres ? instances.Length : 0, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            bool updateProbes = anyInstanceUseBlendProbes;
            NativeArray<InstanceHandle> probeInstanceQueue = default;
            NativeArray<int> compactTetrahedronCache = default;
            NativeArray<Vector3> probeQueryPosition = default;
            NativeArray<SphericalHarmonicsL2> probeUpdateDataQueue = default;
            NativeArray<Vector4> probeOcclusionUpdateDataQueue = default;

            if (updateProbes)
            {
                probeInstanceQueue = new NativeArray<InstanceHandle>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                compactTetrahedronCache = new NativeArray<int>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                probeQueryPosition = new NativeArray<Vector3>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                probeUpdateDataQueue = new NativeArray<SphericalHarmonicsL2>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                probeOcclusionUpdateDataQueue = new NativeArray<Vector4>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            }

            k_AllocateBuffers.End();

            var transformJobRanges = JaggedJobRange.FromSpanWithMaxBatchSize(jaggedLocalToWorldMatrices, TransformUpdateJob.MaxBatchSize, Allocator.TempJob);

            k_UpdateTransformsAndProbes.Begin();

            var transformQueueCount = 0;
            int probesQueueCount = 0;

            JobHandle transformJobHandle = new TransformUpdateJob
            {
                jobRanges = transformJobRanges.AsArray(),
                initialize = initialize,
                enableBoundingSpheres = m_EnableBoundingSpheres,
                instances = instances,
                jaggedLocalToWorldMatrices = jaggedLocalToWorldMatrices,
                jaggedPrevLocalToWorldMatrices = jaggedPrevLocalToWorldMatrices,
                atomicTransformQueueCount = new UnsafeAtomicCounter32(&transformQueueCount),
                renderWorld = m_RenderWorld,
                transformUpdateInstanceQueue = transformUpdateInstanceQueue,
                transformUpdateDataQueue = transformUpdateDataQueue,
                boundingSpheresDataQueue = boundingSpheresUpdateDataQueue,
            }
            .Schedule(transformJobRanges);

            JobHandle probesJobHandle = default;
            if (updateProbes)
            {
                probesJobHandle = new ProbesUpdateJob
                {
                    instances = instances,
                    renderWorld = m_RenderWorld,
                    atomicProbesQueueCount = new UnsafeAtomicCounter32(&probesQueueCount),
                    probeInstanceQueue = probeInstanceQueue,
                    compactTetrahedronCache = compactTetrahedronCache,
                    probeQueryPosition = probeQueryPosition
                }
                .ScheduleParallel(instances.Length, ProbesUpdateJob.MaxBatchSize, transformJobHandle);
            }

            JobHandle.CombineDependencies(transformJobHandle, probesJobHandle).Complete();

            k_UpdateTransformsAndProbes.End();

            if (probesQueueCount > 0)
            {
                InterpolateProbesAndUpdateTetrahedronCache(probesQueueCount, probeInstanceQueue, compactTetrahedronCache,
                    probeQueryPosition, probeUpdateDataQueue, probeOcclusionUpdateDataQueue);

                DispatchProbeUpdateCommand(probesQueueCount, probeInstanceQueue, probeUpdateDataQueue, probeOcclusionUpdateDataQueue);
            }

            if (transformQueueCount > 0)
                DispatchTransformUpdateCommand(initialize, transformQueueCount, transformUpdateInstanceQueue, transformUpdateDataQueue, boundingSpheresUpdateDataQueue);

            transformJobRanges.Dispose();
            transformUpdateInstanceQueue.Dispose();
            transformUpdateDataQueue.Dispose();
            boundingSpheresUpdateDataQueue.Dispose();

            probeInstanceQueue.Dispose();
            compactTetrahedronCache.Dispose();
            probeQueryPosition.Dispose();
            probeUpdateDataQueue.Dispose();
            probeOcclusionUpdateDataQueue.Dispose();
        }

        private unsafe void UpdateInstanceProbesData(NativeArray<InstanceHandle> instances)
        {
            var probeInstanceQueue = new NativeArray<InstanceHandle>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var compactTetrahedronCache = new NativeArray<int>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var probeQueryPosition = new NativeArray<Vector3>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var probeUpdateDataQueue = new NativeArray<SphericalHarmonicsL2>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var probeOcclusionUpdateDataQueue = new NativeArray<Vector4>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int probesQueueCount = 0;

            new ProbesUpdateJob
            {
                instances = instances,
                renderWorld = m_RenderWorld,
                atomicProbesQueueCount = new UnsafeAtomicCounter32(&probesQueueCount),
                probeInstanceQueue = probeInstanceQueue,
                compactTetrahedronCache = compactTetrahedronCache,
                probeQueryPosition = probeQueryPosition
            }
            .RunBatchParallel(instances.Length, ProbesUpdateJob.MaxBatchSize);

            if (probesQueueCount > 0)
            {
                InterpolateProbesAndUpdateTetrahedronCache(probesQueueCount, probeInstanceQueue, compactTetrahedronCache,
                    probeQueryPosition, probeUpdateDataQueue, probeOcclusionUpdateDataQueue);

                DispatchProbeUpdateCommand(probesQueueCount, probeInstanceQueue, probeUpdateDataQueue, probeOcclusionUpdateDataQueue);
            }

            probeInstanceQueue.Dispose();
            compactTetrahedronCache.Dispose();
            probeQueryPosition.Dispose();
            probeUpdateDataQueue.Dispose();
            probeOcclusionUpdateDataQueue.Dispose();
        }

        // This represents the upper limit of all allocated instances.
        // It is used to define the capacity of the instance data for this archetype in the GPU instance data buffer.
        public int TrimGPUAllocatorLength(GPUArchetypeHandle archetype)
        {
            return m_InstanceAllocators.TrimGPUAllocatorLength(archetype);
        }

        // This represents the actual number of instances that are currently allocated and active.
        public int GetGPUArchetypeAliveInstancesCount(GPUArchetypeHandle archetype)
        {
            return m_InstanceAllocators.GetInstanceGPUHandlesAllocatedCount(archetype);
        }

        public void EnsureGPUInstanceDataBufferLayout()
        {
            Assert.IsTrue(m_GPUResizingPolicy == GPUCapacityResizingPolicy.DoubleOnGrow || m_GPUResizingPolicy == GPUCapacityResizingPolicy.DoubleOnGrow_HalveOnQuarterShrink,
                "GPU capacity resizing policy is not supported yet.");

            const float kGrowMult = 2.0f;
            const float kShrinkThreshold = 0.25f;

            var archetypeCount = m_ArchetypeManager.GetRef().GetArchetypesCount();
            var bufferLayoutParams = new NativeArray<(GPUArchetypeHandle, int)>(archetypeCount, Allocator.Temp);
            var needLayoutUpdate = false;

            for (int i = 0; i < archetypeCount; i++)
            {
                var archetypeHandle = GPUArchetypeHandle.Create((short)i);
                if (m_InstanceDataBuffer.IsArchetypeAllocated(archetypeHandle))
                {
                    var archetypeIndex = m_InstanceDataBuffer.GetArchetypeIndex(archetypeHandle);
                    var maxCPUCount = TrimGPUAllocatorLength(archetypeHandle);
                    int maxGPUCount = m_InstanceDataBuffer.GetInstancesCount(archetypeIndex);
                    int newGPUCount = maxGPUCount;

                    if (maxCPUCount > maxGPUCount)
                        newGPUCount = Mathf.CeilToInt(maxCPUCount * kGrowMult);

                    if (m_GPUResizingPolicy == GPUCapacityResizingPolicy.DoubleOnGrow_HalveOnQuarterShrink)
                    {
                        if (maxCPUCount <= maxGPUCount * kShrinkThreshold)
                            newGPUCount = Mathf.CeilToInt(maxCPUCount * kGrowMult);
                    }
                    else
                    {
                        // Reset GPU capacity if no instances at all.
                        int aliveCPUDefaultCount = GetGPUArchetypeAliveInstancesCount(archetypeHandle);
                        if (aliveCPUDefaultCount == 0)
                            newGPUCount = 0;
                    }

                    if (newGPUCount != maxGPUCount)
                        needLayoutUpdate = true;

                    bufferLayoutParams[i] = (archetypeHandle, newGPUCount);
                }
                else
                {
                    needLayoutUpdate = true;

                    var maxCPUCount = TrimGPUAllocatorLength(archetypeHandle);
                    var newGPUCount = Mathf.CeilToInt(maxCPUCount * kGrowMult);
                    bufferLayoutParams[i] = (archetypeHandle, newGPUCount);
                }
            }

            if (needLayoutUpdate)
            {
                //@ Optimize this with a ctor taking param arrays
                var newLayout = new GPUInstanceDataBufferLayout(bufferLayoutParams.Length, Allocator.Temp);
                foreach (var param in bufferLayoutParams)
                {
                    newLayout.Add(param.Item1, param.Item2);
                }

                m_InstanceDataBuffer.SetGPULayout(m_CmdBuffer, ref m_ArchetypeManager.GetRef(), newLayout, submitCmdBuffer: true);
                onGPUBufferLayoutChanged?.Invoke();
            }
        }

        public void UpdateInstanceWindDataHistory(NativeArray<GPUInstanceIndex> gpuIndices)
        {
            if (gpuIndices.Length == 0)
                return;

            DispatchWindDataCopyHistoryCommand(gpuIndices);
        }

        public void AddCameras(NativeArray<EntityId> cameraIDs)
        {
            m_RenderWorld.AddCameras(cameraIDs);
        }

        public void RemoveCameras(NativeArray<EntityId> cameraIDs)
        {
            m_RenderWorld.RemoveCameras(cameraIDs);
        }

        public void AllocateNewInstances(JaggedSpan<EntityId> jaggedInstanceIDs,
            NativeArray<InstanceHandle> instances,
            NativeArray<GPUArchetypeHandle> archetypes,
            int newInstancesCount)
        {
            using (new ProfilerMarker("AllocateNewInstances").Auto())
            {
                HandleInstancesAllocations(InstanceAllocatorVariant.AllocOnly, jaggedInstanceIDs, instances, archetypes, newInstancesCount);
            }
        }

        public void ReallocateExistingGPUInstances(NativeArray<InstanceHandle> instances,
            NativeArray<GPUArchetypeHandle> archetypes)
        {
            using (new ProfilerMarker("ReallocateExistingGPUInstances").Auto())
            {
                var dummyRenderers = new JaggedSpan<EntityId>(0, Allocator.TempJob);
                HandleInstancesAllocations(InstanceAllocatorVariant.GPUReallocOnly, dummyRenderers, instances, archetypes, 0);
                dummyRenderers.Dispose();
            }
        }

        public void AllocOrGPUReallocInstances(JaggedSpan<EntityId> jaggedInstanceIDs,
            NativeArray<InstanceHandle> instances,
            NativeArray<GPUArchetypeHandle> archetypes,
            int newInstancesCount)
        {
            using (new ProfilerMarker("AllocOrGPUReallocInstances").Auto())
            {
                HandleInstancesAllocations(InstanceAllocatorVariant.AllocOrGPURealloc, jaggedInstanceIDs, instances, archetypes, newInstancesCount);
            }
        }

        private unsafe void HandleInstancesAllocations(InstanceAllocatorVariant allocVariant,
            JaggedSpan<EntityId> jaggedInstanceIDs,
            NativeArray<InstanceHandle> instances,
            NativeArray<GPUArchetypeHandle> archetypes,
            int newInstancesCount)
        {
            Assert.IsTrue(allocVariant != InstanceAllocatorVariant.Null);
            Assert.IsTrue(jaggedInstanceIDs.totalLength == 0 || jaggedInstanceIDs.totalLength == instances.Length);
            Assert.IsTrue(archetypes.Length == 1 || archetypes.Length == instances.Length);

            m_RenderWorld.EnsureFreeCapacity(newInstancesCount);

            fixed (InstanceAllocators* instanceAllocators = &m_InstanceAllocators)
            {
                InstanceDataSystemBurst.AllocateInstances(allocVariant,
                    jaggedInstanceIDs,
                    archetypes,
                    instanceAllocators,
                    ref m_RenderWorld,
                    ref instances,
                    ref m_RendererToInstanceMap);
            }

            EnsureGPUInstanceDataBufferLayout();
        }

        public unsafe void FreeInstances(NativeArray<InstanceHandle> instances)
        {
            fixed (InstanceAllocators* instanceAllocators = &m_InstanceAllocators)
            {
                InstanceDataSystemBurst.FreeInstances(instances,
                    instanceAllocators,
                    ref m_RenderWorld,
                    ref m_RendererToInstanceMap);
            }

            EnsureGPUInstanceDataBufferLayout();
        }

        public void UpdateInstanceData(NativeArray<InstanceHandle> instances, in MeshRendererUpdateBatch updateBatch, NativeParallelHashMap<EntityId, GPUInstanceIndex> lodGroupDataMap)
        {
            using var _ = k_UpdateInstanceData.Auto();

            Assert.IsTrue(instances.Length == updateBatch.TotalLength);

            var jobRanges = JaggedJobRange.FromSpanWithRelaxedBatchSize(updateBatch.instanceIDs, 128, Allocator.TempJob);

            new UpdateRendererInstancesJob
            {
                jobRanges = jobRanges.AsArray(),
                updateBatch = updateBatch,
                instances = instances,
                lodGroupDataMap = lodGroupDataMap,
                renderWorld = m_RenderWorld,
            }
            .RunParallel(jobRanges);

            jobRanges.Dispose();
        }

        public GPUInstanceUploadData CreateInstanceUploadData(GPUComponentHandle component, int capacity, Allocator allocator)
        {
            var components = new NativeArray<GPUComponentHandle>(1, Allocator.Temp);
            components[0] = component;
            var upload = new GPUInstanceUploadData(ref m_ArchetypeManager.GetRef(), components, capacity, allocator);
            components.Dispose();
            return upload;
        }

        public GPUInstanceUploadData CreateInstanceUploadData(NativeArray<GPUComponentHandle> components, int capacity, Allocator allocator)
        {
            return new GPUInstanceUploadData(ref m_ArchetypeManager.GetRef(), components, capacity, allocator);
        }

        public GPUInstanceUploadData CreateInstanceUploadData(GPUComponentSet componentSet, int capacity, Allocator allocator)
        {
            return CreateInstanceUploadData(componentSet.GetComponents(Allocator.Temp).AsArray(), capacity, allocator);
        }

        public void UploadDataToGPU(NativeArray<InstanceHandle> instances, GraphicsBuffer uploadBuffer, in GPUInstanceUploadData uploadData)
        {
            var gpuIndices = new NativeArray<GPUInstanceIndex>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_InstanceDataBuffer.QueryInstanceGPUIndices(m_RenderWorld, instances, gpuIndices);
            UploadDataToGPU(gpuIndices, uploadBuffer, uploadData);
            gpuIndices.Dispose();
        }

        public void UploadDataToGPU(NativeArray<GPUInstanceIndex> gpuIndices, GraphicsBuffer uploadBuffer, in GPUInstanceUploadData uploadData)
        {
            m_InstanceDataBuffer.UploadDataToGPU(m_CmdBuffer, uploadBuffer, uploadData, gpuIndices);
        }

        public GPUInstanceDataBufferReadback<T> ReadbackInstanceDataBuffer<T>() where T : unmanaged
        {
            var readback = new GPUInstanceDataBufferReadback<T>();
            bool success = readback.Load(m_CmdBuffer, m_InstanceDataBuffer);
            Assert.IsTrue(success, "Failed to readback instance data buffer.");
            return readback;
        }

        public ref readonly GPUComponentDesc GetGPUComponentDesc(GPUComponentHandle component)
        {
            return ref m_ArchetypeManager.GetRef().GetComponentDesc(component);
        }

        public void UpdateAllInstanceProbes()
        {
            var instances = m_RenderWorld.indexToHandle;
            if (instances.Length == 0)
                return;

            UpdateInstanceProbesData(instances);
        }

        public void InitializeInstanceTransforms(NativeArray<InstanceHandle> instances,
            JaggedSpan<float4x4> jaggedLocalToWorldMatrices,
            JaggedSpan<float4x4> jaggedPrevLocalToWorldMatrices,
            bool anyInstanceUseBlendProbes)
        {
            UpdateInstanceTransformsData(true, instances, jaggedLocalToWorldMatrices, jaggedPrevLocalToWorldMatrices, anyInstanceUseBlendProbes);
        }

        public void UpdateInstanceTransforms(NativeArray<InstanceHandle> instances, JaggedSpan<float4x4> jaggedLocalToWorldMatrices, bool anyInstanceUseBlendProbes)
        {
            UpdateInstanceTransformsData(false, instances, jaggedLocalToWorldMatrices, jaggedLocalToWorldMatrices, anyInstanceUseBlendProbes);
        }

        public void UpdateInstanceMotions()
        {
            if (m_RenderWorld.instanceCount == 0)
                return;

            UpdateInstanceMotionsDataInternal();
        }

        public void QueryInstanceGPUIndices(NativeArray<InstanceHandle> instances, NativeArray<GPUInstanceIndex> gpuIndices)
        {
            m_InstanceDataBuffer.QueryInstanceGPUIndices(m_RenderWorld, instances, gpuIndices);
        }

        public JobHandle ScheduleQueryRendererInstancesJob(JaggedSpan<EntityId> jaggedRenderers,
            NativeArray<InstanceHandle> instances,
            UnsafeAtomicCounter32 notFoundInstancesCount = default)
        {
            Assert.AreEqual(jaggedRenderers.totalLength, instances.Length);

            if (jaggedRenderers.totalLength == 0)
                return default;

            var jobRanges = JaggedJobRange.FromSpanWithRelaxedBatchSize(jaggedRenderers, 128, Allocator.TempJob);

            var jobHandle = new QueryRendererInstancesJob()
            {
                jobRanges = jobRanges.AsArray(),
                rendererToInstanceMap = m_RendererToInstanceMap,
                jaggedRenderers = jaggedRenderers,
                instances = instances,
                atomicNonFoundInstancesCount = notFoundInstancesCount
            }
            .Schedule(jobRanges);

            return jobRanges.Dispose(jobHandle);
        }

        public void QueryRendererInstances(NativeArray<EntityId> renderers,
            NativeArray<InstanceHandle> instances,
            UnsafeAtomicCounter32 notFoundInstancesCount = default)
        {
            var jaggedRenderers = renderers.ToJaggedSpan(Allocator.TempJob);
            QueryRendererInstances(jaggedRenderers, instances, notFoundInstancesCount);
            jaggedRenderers.Dispose();
        }

        public void QueryRendererInstances(JaggedSpan<EntityId> jaggedRenderers,
            NativeArray<InstanceHandle> instances,
            UnsafeAtomicCounter32 notFoundInstancesCount = default)
        {
            using (k_QueryRendererInstances.Auto())
            {
                ScheduleQueryRendererInstancesJob(jaggedRenderers, instances, notFoundInstancesCount).Complete();
            }
        }

        public JobHandle ScheduleQueryRendererInstancesJob(NativeArray<EntityId> renderers, NativeArray<InstanceHandle> instances)
        {
            var jaggedRenderers = renderers.ToJaggedSpan(Allocator.TempJob);
            var jobHandle = ScheduleQueryRendererInstancesJob(jaggedRenderers, instances);
            jaggedRenderers.Dispose(jobHandle);
            return jobHandle;
        }

        public JobHandle ScheduleQuerySortedMeshInstancesJob(NativeArray<EntityId> sortedMeshes, NativeList<InstanceHandle> instances)
        {
            if (sortedMeshes.Length == 0)
                return default;

            instances.Capacity = m_RenderWorld.instanceCount;

            return new QuerySortedMeshInstancesJob()
            {
                renderWorld = m_RenderWorld,
                sortedMeshes = sortedMeshes,
                instances = instances
            }
            .ScheduleBatch(m_RenderWorld.instanceCount, QuerySortedMeshInstancesJob.MaxBatchSize);
        }

        public bool AreAllAllocatedInstancesValid()
        {
            for (int i = 0; i < m_RenderWorld.instanceCount; ++i)
            {
                if (!m_RenderWorld.IsValidInstance(m_RenderWorld.indexToHandle[i]))
                    return false;
            }

            return true;
        }

        public unsafe void GetVisibleTreeInstances(in ParallelBitArray compactedVisibilityMasks, in ParallelBitArray processedBits, NativeList<EntityId> visibeTreeRenderers,
            NativeList<InstanceHandle> visibeTreeInstances, bool becomeVisibleOnly, out int becomeVisibeTreeInstancesCount)
        {
            Assert.AreEqual(visibeTreeRenderers.Length, 0);
            Assert.AreEqual(visibeTreeInstances.Length, 0);

            becomeVisibeTreeInstancesCount = 0;

            if (totalTreeCount == 0)
                return;

            visibeTreeRenderers.ResizeUninitialized(totalTreeCount);
            visibeTreeInstances.ResizeUninitialized(totalTreeCount);

            int visibleTreeInstancesCount = 0;

            new GetVisibleNonProcessedTreeInstancesJob
            {
                becomeVisible = true,
                renderWorld = m_RenderWorld,
                compactedVisibilityMasks = compactedVisibilityMasks,
                processedBits = processedBits,
                renderers = visibeTreeRenderers.AsArray(),
                instances = visibeTreeInstances.AsArray(),
                atomicTreeInstancesCount = new UnsafeAtomicCounter32(&visibleTreeInstancesCount)
            }
            .RunBatchParallel(m_RenderWorld.instanceCount, GetVisibleNonProcessedTreeInstancesJob.MaxBatchSize);

            becomeVisibeTreeInstancesCount = visibleTreeInstancesCount;

            if (!becomeVisibleOnly)
            {
                new GetVisibleNonProcessedTreeInstancesJob
                {
                    becomeVisible = false,
                    renderWorld = m_RenderWorld,
                    compactedVisibilityMasks = compactedVisibilityMasks,
                    processedBits = processedBits,
                    renderers = visibeTreeRenderers.AsArray(),
                    instances = visibeTreeInstances.AsArray(),
                    atomicTreeInstancesCount = new UnsafeAtomicCounter32(&visibleTreeInstancesCount)
                }
                .RunBatchParallel(m_RenderWorld.instanceCount, GetVisibleNonProcessedTreeInstancesJob.MaxBatchSize);
            }

            Assert.IsTrue(becomeVisibeTreeInstancesCount <= visibleTreeInstancesCount);
            Assert.IsTrue(visibleTreeInstancesCount <= totalTreeCount);

            visibeTreeRenderers.ResizeUninitialized(visibleTreeInstancesCount);
            visibeTreeInstances.ResizeUninitialized(visibleTreeInstancesCount);
        }

        public void UpdatePerFrameInstanceVisibility(in ParallelBitArray compactedVisibilityMasks)
        {
            Assert.AreEqual(m_RenderWorld.handleCount, compactedVisibilityMasks.Length);

            new UpdateCompactedInstanceVisibilityJob
            {
                renderWorld = m_RenderWorld,
                compactedVisibilityMasks = compactedVisibilityMasks
            }
            .RunBatchParallel(m_RenderWorld.instanceCount, UpdateCompactedInstanceVisibilityJob.MaxBatchSize);
        }

        public void ValidateTotalTreeCount()
        {
            // Disable "Unreachable code detected" warning
#pragma warning disable CS0162
            if (!GPUResidentDrawer.EnableDeepValidation)
                return;

            using (k_DeepValidateTotalTreeCount.Auto())
            {
                int totalTreeCountTruth = InstanceDataSystemBurst.ComputeTotalTreeCount(m_RenderWorld.rendererSettings);

                if (totalTreeCount != totalTreeCountTruth)
                    Debug.LogError($"Cached total tree count ({totalTreeCount}) does not match total tree count truth ({totalTreeCountTruth}).");
            }

#pragma warning restore CS0162
        }

        private static class InstanceTransformUpdateIDs
        {
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
            public static readonly int _WindDataUpdateIndexQueue = Shader.PropertyToID("_WindDataUpdateIndexQueue");
            public static readonly int _WindDataBuffer = Shader.PropertyToID("_WindDataBuffer");
            public static readonly int _WindParamAddressArray = Shader.PropertyToID("_WindParamAddressArray");
            public static readonly int _WindHistoryParamAddressArray = Shader.PropertyToID("_WindHistoryParamAddressArray");
        }

        public enum GPUCapacityResizingPolicy
        {
            DoubleOnGrow,
            DoubleOnGrow_HalveOnQuarterShrink
        }
    }
}

#endif // !UNITY_WEBGL_RENDERER_ONLY
