using UnityEngine.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using System;

namespace UnityEngine.Rendering
{
    internal struct BRGMatrix : IEquatable<BRGMatrix>
    {
        public float4 localToWorld0;
        public float4 localToWorld1;
        public float4 localToWorld2;

        public static BRGMatrix FromMatrix4x4(Matrix4x4 m)
        {
            return new BRGMatrix
            {
                /*  mat4x3 packed like this:
                      p1.x, p1.w, p2.z, p3.y,
                      p1.y, p2.x, p2.w, p3.z,
                      p1.z, p2.y, p3.x, p3.w,
                      0.0,  0.0,  0.0,  1.0
                */

                localToWorld0 = new float4(m.m00, m.m10, m.m20, m.m01),
                localToWorld1 = new float4(m.m11, m.m21, m.m02, m.m12),
                localToWorld2 = new float4(m.m22, m.m03, m.m13, m.m23)
            };
        }

        public void SetTranslation(float3 translation)
        {
            localToWorld2.y = translation.x;
            localToWorld2.z = translation.y;
            localToWorld2.w = translation.z;
        }

        public bool Equals(BRGMatrix other)
        {
            return math.all(
                (localToWorld0 == other.localToWorld0) &
                (localToWorld1 == other.localToWorld1) &
                (localToWorld2 == other.localToWorld2));
        }
    }

    internal struct BRGDrawData
    {
        public int length;
        public NativeArray<int> sourceAABBIndex;
        public NativeArray<AABB> bounds;

        public void Resize(int capacity)
        {
            sourceAABBIndex.ResizeArray(capacity);
            bounds.ResizeArray(capacity);
        }

        public void Dispose()
        {
            if (sourceAABBIndex.IsCreated)
                sourceAABBIndex.Dispose();
            if (bounds.IsCreated)
                bounds.Dispose();
        }
    }

    internal struct BRGTransformUpdater
    {
        private const int sBlockSize = 128;
        private int m_Capacity;
        private int m_Length;
        private NativeArray<int> m_Indices;
        private NativeArray<bool> m_HasProbes;
        private NativeArray<int> m_TetrahedronCache;
        private TransformAccessArray m_Transforms;
        private NativeArray<BRGMatrix> m_CachedTransforms;

        private NativeHashMap<int, int> m_MeshToBoundIndexMap;
        private NativeArray<AABB> m_MeshBounds;
        private int m_MeshBoundsCount;

        private NativeArray<int> m_UpdateQueueCounter;
        private NativeArray<int> m_TransformUpdateIndexQueue;
        private NativeArray<BRGGpuTransformUpdate> m_TransformUpdateDataQueue;
        private NativeArray<int> m_ProbeUpdateIndexQueue;
        private NativeArray<SphericalHarmonicsL2> m_ProbeUpdateDataQueue;
        private NativeArray<Vector4> m_ProbeOcclusionUpdateDataQueue;
        private NativeArray<Vector3> m_QueryProbePosition;

        private JobHandle m_UpdateTransformsJobHandle;
        private LightProbesQuery m_LightProbesQuery;

        private ComputeBuffer m_TransformUpdateIndexQueueBuffer;
        private ComputeBuffer m_TransformUpdateDataQueueBuffer;
        private ComputeBuffer m_ProbeUpdateIndexQueueBuffer;
        private ComputeBuffer m_ProbeUpdateDataQueueBuffer;
        private ComputeBuffer m_ProbeOcclusionUpdateDataQueueBuffer;
        private ComputeShader m_TransformUpdateCS;
        private int m_TransformUpdateKernel;
        private int m_ProbeUpdateKernel;

        private BRGDrawData m_DrawData;
        public BRGDrawData drawData => m_DrawData;

        private enum QueueType
        {
            Transform,
            Probe,
            Count
        }

        private static class BRGTransformParams
        {
            public static readonly int _TransformUpdateQueueCount = Shader.PropertyToID("_TransformUpdateQueueCount");
            public static readonly int _TransformUpdateOutputL2WVec4Offset = Shader.PropertyToID("_TransformUpdateOutputL2WVec4Offset");
            public static readonly int _TransformUpdateOutputW2LVec4Offset = Shader.PropertyToID("_TransformUpdateOutputW2LVec4Offset");
            public static readonly int _TransformUpdateDataQueue = Shader.PropertyToID("_TransformUpdateDataQueue");
            public static readonly int _TransformUpdateIndexQueue = Shader.PropertyToID("_TransformUpdateIndexQueue");
            public static readonly int _OutputTransformBuffer = Shader.PropertyToID("_OutputTransformBuffer");
        }


        private static class BRGProbeUpdateParams
        {
            public static readonly int _ProbeUpdateQueueCount = Shader.PropertyToID("_ProbeUpdateQueueCount");
            public static readonly int _ProbeUpdateVec4OffsetSHAr = Shader.PropertyToID("_ProbeUpdateVec4OffsetSHAr");
            public static readonly int _ProbeUpdateVec4OffsetSHAg = Shader.PropertyToID("_ProbeUpdateVec4OffsetSHAg");
            public static readonly int _ProbeUpdateVec4OffsetSHAb = Shader.PropertyToID("_ProbeUpdateVec4OffsetSHAb");
            public static readonly int _ProbeUpdateVec4OffsetSHBr = Shader.PropertyToID("_ProbeUpdateVec4OffsetSHBr");
            public static readonly int _ProbeUpdateVec4OffsetSHBg = Shader.PropertyToID("_ProbeUpdateVec4OffsetSHBg");
            public static readonly int _ProbeUpdateVec4OffsetSHBb = Shader.PropertyToID("_ProbeUpdateVec4OffsetSHBb");
            public static readonly int _ProbeUpdateVec4OffsetSHC = Shader.PropertyToID("_ProbeUpdateVec4OffsetSHC");
            public static readonly int _ProbeUpdateVec4OffsetSOcclusion = Shader.PropertyToID("_ProbeUpdateVec4OffsetSOcclusion");
            public static readonly int _ProbeUpdateDataQueue = Shader.PropertyToID("_ProbeUpdateDataQueue");
            public static readonly int _ProbeOcclusionUpdateDataQueue = Shader.PropertyToID("_ProbeOcclusionUpdateDataQueue");
            public static readonly int _ProbeUpdateIndexQueue = Shader.PropertyToID("_ProbeUpdateIndexQueue");
            public static readonly int _OutputProbeBuffer = Shader.PropertyToID("_OutputProbeBuffer");
        }

        private void LoadShaders()
        {
            m_TransformUpdateCS = (ComputeShader)Resources.Load("BRGTransformUpdateCS");
            m_TransformUpdateKernel = m_TransformUpdateCS.FindKernel("ScatterUpdateTransformMain");
            m_ProbeUpdateKernel = m_TransformUpdateCS.FindKernel("ScatterUpdateProbesMain");
        }

        private void AddTransformUpdateCommand(
            CommandBuffer cmdBuffer,
            int queueCount,
            BRGInstanceBufferOffsets instanceBufferOffsets,
            ComputeBuffer inputIndexQueueBuffer,
            ComputeBuffer inputDataQueueBuffer,
            GraphicsBuffer outputBuffer,
            NativeArray<int> transformIndexQueue,
            NativeArray<BRGGpuTransformUpdate> updateDataQueue)
        {
            cmdBuffer.SetBufferData(inputIndexQueueBuffer, transformIndexQueue, 0, 0, queueCount);
            cmdBuffer.SetBufferData(inputDataQueueBuffer, updateDataQueue, 0, 0, queueCount);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGTransformParams._TransformUpdateQueueCount, queueCount);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGTransformParams._TransformUpdateOutputL2WVec4Offset, instanceBufferOffsets.localToWorld);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGTransformParams._TransformUpdateOutputW2LVec4Offset, instanceBufferOffsets.worldToLocal);
            cmdBuffer.SetComputeBufferParam(m_TransformUpdateCS, m_TransformUpdateKernel, BRGTransformParams._TransformUpdateIndexQueue, inputIndexQueueBuffer);
            cmdBuffer.SetComputeBufferParam(m_TransformUpdateCS, m_TransformUpdateKernel, BRGTransformParams._TransformUpdateDataQueue, inputDataQueueBuffer);
            cmdBuffer.SetComputeBufferParam(m_TransformUpdateCS, m_TransformUpdateKernel, BRGTransformParams._OutputTransformBuffer, outputBuffer);
            cmdBuffer.DispatchCompute(m_TransformUpdateCS, m_TransformUpdateKernel, (queueCount + 63) / 64, 1, 1);
        }

        private void AddProbeUpdateCommand(
            CommandBuffer cmdBuffer,
            int queueCount,
            BRGInstanceBufferOffsets instanceBufferOffsets,
            ComputeBuffer inputIndexQueueBuffer,
            ComputeBuffer inputDataQueueBuffer,
            ComputeBuffer inputDataProbeOcclusionQueueBuffer,
            GraphicsBuffer outputBuffer,
            NativeArray<int> probeIndexQueue,
            NativeArray<SphericalHarmonicsL2> probeUpdateDataQueue,
            NativeArray<Vector4> probeOcclusionUpdateDataQueue)
        {
            cmdBuffer.SetBufferData(inputIndexQueueBuffer, probeIndexQueue, 0, 0, queueCount);
            cmdBuffer.SetBufferData(inputDataQueueBuffer, probeUpdateDataQueue, 0, 0, queueCount);
            cmdBuffer.SetBufferData(inputDataProbeOcclusionQueueBuffer, probeOcclusionUpdateDataQueue, 0, 0, queueCount);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGProbeUpdateParams._ProbeUpdateQueueCount, queueCount);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGProbeUpdateParams._ProbeUpdateVec4OffsetSHAr, instanceBufferOffsets.probeOffsetSHAr);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGProbeUpdateParams._ProbeUpdateVec4OffsetSHAg, instanceBufferOffsets.probeOffsetSHAg);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGProbeUpdateParams._ProbeUpdateVec4OffsetSHAb, instanceBufferOffsets.probeOffsetSHAb);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGProbeUpdateParams._ProbeUpdateVec4OffsetSHBr, instanceBufferOffsets.probeOffsetSHBr);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGProbeUpdateParams._ProbeUpdateVec4OffsetSHBg, instanceBufferOffsets.probeOffsetSHBg);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGProbeUpdateParams._ProbeUpdateVec4OffsetSHBb, instanceBufferOffsets.probeOffsetSHBb);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGProbeUpdateParams._ProbeUpdateVec4OffsetSHC, instanceBufferOffsets.probeOffsetSHC);
            cmdBuffer.SetComputeIntParam(m_TransformUpdateCS, BRGProbeUpdateParams._ProbeUpdateVec4OffsetSOcclusion, instanceBufferOffsets.probeOffsetOcclusion);

            cmdBuffer.SetComputeBufferParam(m_TransformUpdateCS, m_ProbeUpdateKernel, BRGProbeUpdateParams._ProbeUpdateIndexQueue, inputIndexQueueBuffer);
            cmdBuffer.SetComputeBufferParam(m_TransformUpdateCS, m_ProbeUpdateKernel, BRGProbeUpdateParams._ProbeUpdateDataQueue, inputDataQueueBuffer);
            cmdBuffer.SetComputeBufferParam(m_TransformUpdateCS, m_ProbeUpdateKernel, BRGProbeUpdateParams._ProbeOcclusionUpdateDataQueue, inputDataProbeOcclusionQueueBuffer);
            cmdBuffer.SetComputeBufferParam(m_TransformUpdateCS, m_ProbeUpdateKernel, BRGProbeUpdateParams._OutputProbeBuffer, outputBuffer);
            cmdBuffer.DispatchCompute(m_TransformUpdateCS, m_ProbeUpdateKernel, (queueCount + 63) / 64, 1, 1);
        }

        [BurstCompile]
        private struct UpdateJob : IJobParallelForTransform
        {
            public float minDistance;

            [ReadOnly]
            public NativeArray<int> inputIndices;

            [ReadOnly]
            public NativeArray<int> sourceAABBIndex;

            [ReadOnly]
            public NativeArray<AABB> meshAABB;

            [ReadOnly]
            public NativeArray<bool> hasProbes;

            [ReadOnly]
            public LightProbesQuery lightProbesQuery;

            public NativeArray<BRGMatrix> cachedTransforms;

            [WriteOnly]
            public NativeArray<int> updateQueueCounter;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<int> tetrahedronCache;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<int> transformUpdateIndexQueue;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<BRGGpuTransformUpdate> transformUpdateDataQueue;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<int> probeUpdateIndexQueue;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<SphericalHarmonicsL2> probeUpdateDataQueue;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<Vector4> probeOcclusionUpdateDataQueue;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<Vector3> probeQueryPosition;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<AABB> boundsToUpdate;

            private int IncrementCounter(QueueType queueType)
            {
                int outputIndex = 0;
                unsafe
                {
                    int* ptr = (int*)updateQueueCounter.GetUnsafePtr<int>() + (int)queueType;
                    outputIndex = Interlocked.Increment(ref UnsafeUtility.AsRef<int>(ptr));
                }
                return outputIndex - 1;
            }

            public void Execute(int index, TransformAccess transform)
            {
                if (!transform.isValid)
                    return;

                var m = BRGMatrix.FromMatrix4x4(transform.localToWorldMatrix);

                if (cachedTransforms[index].Equals(m))
                    return;

                cachedTransforms[index] = m;

                int instanceIndex = inputIndices[index];
                AABB srcAABB = meshAABB[sourceAABBIndex[instanceIndex]];
                boundsToUpdate[instanceIndex] = AABB.Transform(transform.localToWorldMatrix, srcAABB);

                int outputIndex = IncrementCounter(QueueType.Transform);
                transformUpdateIndexQueue[outputIndex] = instanceIndex;

                /*  mat4x3 packed like this:
                      p1.x, p1.w, p2.z, p3.y,
                      p1.y, p2.x, p2.w, p3.z,
                      p1.z, p2.y, p3.x, p3.w,
                      0.0,  0.0,  0.0,  1.0
                */

                var mi = transform.worldToLocalMatrix;
                transformUpdateDataQueue[outputIndex] = new BRGGpuTransformUpdate()
                {
                    localToWorld0 = m.localToWorld0,
                    localToWorld1 = m.localToWorld1,
                    localToWorld2 = m.localToWorld2,
                    worldToLocal0 = new float4(mi.m00, mi.m10, mi.m20, mi.m01),
                    worldToLocal1 = new float4(mi.m11, mi.m21, mi.m02, mi.m12),
                    worldToLocal2 = new float4(mi.m22, mi.m03, mi.m13, mi.m23),
                };

                if (hasProbes[index])
                {
                    int probeOutputIndex = IncrementCounter(QueueType.Probe);
                    probeQueryPosition[probeOutputIndex] = transform.position;
                    probeUpdateIndexQueue[probeOutputIndex] = instanceIndex;

                    var positionView = probeQueryPosition.GetSubArray(probeOutputIndex, 1);
                    var tetrahedronCacheIndexView = tetrahedronCache.GetSubArray(probeOutputIndex, 1);
                    var shLpView = probeUpdateDataQueue.GetSubArray(probeOutputIndex, 1);
                    var occlusionProbeView = probeOcclusionUpdateDataQueue.GetSubArray(probeOutputIndex, 1);
                    lightProbesQuery.CalculateInterpolatedLightAndOcclusionProbes(positionView, tetrahedronCacheIndexView, shLpView, occlusionProbeView);
                }
            }
        }

        private void RecreteGpuBuffers()
        {
            if (m_TransformUpdateIndexQueueBuffer != null)
                m_TransformUpdateIndexQueueBuffer.Release();

            if (m_TransformUpdateDataQueueBuffer != null)
                m_TransformUpdateDataQueueBuffer.Release();

            if (m_ProbeUpdateIndexQueueBuffer != null)
                m_ProbeUpdateIndexQueueBuffer.Release();

            if (m_ProbeUpdateDataQueueBuffer != null)
                m_ProbeUpdateDataQueueBuffer.Release();

            if (m_ProbeOcclusionUpdateDataQueueBuffer != null)
                m_ProbeOcclusionUpdateDataQueueBuffer.Release();

            Assert.IsTrue(System.Runtime.InteropServices.Marshal.SizeOf<BRGSHUpdate>() == System.Runtime.InteropServices.Marshal.SizeOf<SphericalHarmonicsL2>());
            m_TransformUpdateIndexQueueBuffer = new ComputeBuffer(m_Capacity, 4, ComputeBufferType.Raw);
            m_TransformUpdateDataQueueBuffer = new ComputeBuffer(m_Capacity, System.Runtime.InteropServices.Marshal.SizeOf<BRGGpuTransformUpdate>(), ComputeBufferType.Structured);
            m_ProbeUpdateIndexQueueBuffer = new ComputeBuffer(m_Capacity, 4, ComputeBufferType.Raw);
            m_ProbeUpdateDataQueueBuffer = new ComputeBuffer(m_Capacity, System.Runtime.InteropServices.Marshal.SizeOf<BRGSHUpdate>(), ComputeBufferType.Structured);
            m_ProbeOcclusionUpdateDataQueueBuffer = new ComputeBuffer(m_Capacity, System.Runtime.InteropServices.Marshal.SizeOf<Vector4>(), ComputeBufferType.Structured);
        }

        public void Initialize()
        {
            m_TransformUpdateIndexQueueBuffer = null;
            m_TransformUpdateDataQueueBuffer = null;

            LoadShaders();

            m_Length = 0;
            m_Capacity = sBlockSize;
            m_Transforms = new TransformAccessArray(m_Capacity);
            m_Indices = new NativeArray<int>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_HasProbes = new NativeArray<bool>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_TetrahedronCache = new NativeArray<int>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_CachedTransforms = new NativeArray<BRGMatrix>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_TransformUpdateIndexQueue = new NativeArray<int>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_TransformUpdateDataQueue = new NativeArray<BRGGpuTransformUpdate>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_ProbeUpdateIndexQueue = new NativeArray<int>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_ProbeUpdateDataQueue = new NativeArray<SphericalHarmonicsL2>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_ProbeOcclusionUpdateDataQueue = new NativeArray<Vector4>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_QueryProbePosition = new NativeArray<Vector3>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_UpdateQueueCounter = new NativeArray<int>((int)QueueType.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            m_MeshToBoundIndexMap = new NativeHashMap<int, int>(1024, Allocator.Persistent);
            m_MeshBounds = new NativeArray<AABB>(1024, Allocator.Persistent);
            m_MeshBoundsCount = 0;

            m_DrawData = new BRGDrawData();
            m_DrawData.Resize(m_Capacity);

            RecreteGpuBuffers();
        }

        int RegisterMesh(Mesh mesh)
        {
            if (m_MeshToBoundIndexMap.TryGetValue(mesh.GetHashCode(), out var foundIndex))
                return foundIndex;

            int nextIndex = m_MeshBoundsCount++;
            if (nextIndex == m_MeshBounds.Length)
            {
                m_MeshBounds.ResizeArray(m_MeshBounds.Length * 2);
            }

            m_MeshToBoundIndexMap.Add(mesh.GetHashCode(), nextIndex);
            return nextIndex;
        }

        public void RegisterTransformObject(int instanceIndex, Transform transformObject, Mesh mesh, bool hasLightProbe)
        {
            int newLen = m_Length + 1;
            int instanceLen = instanceIndex + 1;
            int newCapacity = Math.Max(instanceLen, newLen);
            if (newCapacity >= m_Capacity)
            {
                m_Capacity = Math.Max(m_Capacity, newCapacity) + sBlockSize;
                m_Transforms.ResizeArray(m_Capacity);
                m_Indices.ResizeArray(m_Capacity);
                m_HasProbes.ResizeArray(m_Capacity);
                m_TetrahedronCache.ResizeArray(m_Capacity);
                m_CachedTransforms.ResizeArray(m_Capacity);
                m_TransformUpdateIndexQueue.ResizeArray(m_Capacity);
                m_TransformUpdateDataQueue.ResizeArray(m_Capacity);
                m_ProbeUpdateIndexQueue.ResizeArray(m_Capacity);
                m_ProbeUpdateDataQueue.ResizeArray(m_Capacity);
                m_ProbeOcclusionUpdateDataQueue.ResizeArray(m_Capacity);
                m_DrawData.Resize(m_Capacity);
                m_QueryProbePosition.ResizeArray(m_Capacity);
                RecreteGpuBuffers();
            }

            m_Transforms.Add(transformObject);
            m_Indices[m_Length] = instanceIndex;
            m_HasProbes[m_Length] = hasLightProbe;
            m_TetrahedronCache[m_Length] = -1;
            var cachedTransform = BRGMatrix.FromMatrix4x4(transformObject.localToWorldMatrix);
            //Dirty the instances with light probe transform always.
            if (hasLightProbe)
                cachedTransform.SetTranslation(new float3(10000.0f, 10000.0f, 10000.0f));
            m_CachedTransforms[m_Length] = cachedTransform;

            int boundsIndex = RegisterMesh(mesh);
            var localAABB = mesh.bounds.ToAABB();
            m_MeshBounds[boundsIndex] = localAABB;
            m_DrawData.sourceAABBIndex[instanceIndex] = boundsIndex;
            m_DrawData.bounds[instanceIndex] = AABB.Transform(transformObject.localToWorldMatrix, mesh.bounds.ToAABB());
            m_Length = newLen;
            m_DrawData.length = Math.Max(instanceLen, m_DrawData.length);
        }

        public void StartUpdateJobs()
        {
            if (m_Length == 0)
                return;

            for (int i = 0; i < (int)QueueType.Count; ++i)
                m_UpdateQueueCounter[i] = 0; //reset queues to 0

            m_LightProbesQuery = new LightProbesQuery(Allocator.TempJob);
            var jobData = new UpdateJob()
            {
                minDistance = System.Single.Epsilon,
                inputIndices = m_Indices,
                sourceAABBIndex = m_DrawData.sourceAABBIndex,
                meshAABB = m_MeshBounds,
                hasProbes = m_HasProbes,
                tetrahedronCache = m_TetrahedronCache,
                lightProbesQuery = m_LightProbesQuery,
                cachedTransforms = m_CachedTransforms,
                updateQueueCounter = m_UpdateQueueCounter,
                transformUpdateIndexQueue = m_TransformUpdateIndexQueue,
                transformUpdateDataQueue = m_TransformUpdateDataQueue,
                probeUpdateIndexQueue = m_ProbeUpdateIndexQueue,
                probeUpdateDataQueue = m_ProbeUpdateDataQueue,
                probeOcclusionUpdateDataQueue = m_ProbeOcclusionUpdateDataQueue,
                probeQueryPosition = m_QueryProbePosition,
                boundsToUpdate = m_DrawData.bounds
            };

            m_UpdateTransformsJobHandle = jobData.ScheduleReadOnly(m_Transforms, 64);
        }

        public bool EndUpdateJobs(
            CommandBuffer cmdBuffer,
            BRGInstanceBufferOffsets instanceBufferOffsets,
            GraphicsBuffer outputBuffer)
        {
            if (m_Length == 0)
                return false;

            m_UpdateTransformsJobHandle.Complete();

            if (m_LightProbesQuery.IsCreated)
                m_LightProbesQuery.Dispose();

            int transformQueueCount = m_UpdateQueueCounter[(int)QueueType.Transform];
            bool hasTransformUpdates = transformQueueCount != 0;
            if (hasTransformUpdates)
                AddTransformUpdateCommand(
                    cmdBuffer, transformQueueCount,
                    instanceBufferOffsets,
                    m_TransformUpdateIndexQueueBuffer, m_TransformUpdateDataQueueBuffer, outputBuffer,
                    m_TransformUpdateIndexQueue, m_TransformUpdateDataQueue);

            int probeQueueCount = m_UpdateQueueCounter[(int)QueueType.Probe];
            bool probesHasUpdates = probeQueueCount != 0;
            if (probesHasUpdates)
                AddProbeUpdateCommand(
                    cmdBuffer, probeQueueCount,
                    instanceBufferOffsets,
                    m_ProbeUpdateIndexQueueBuffer, m_ProbeUpdateDataQueueBuffer, m_ProbeOcclusionUpdateDataQueueBuffer, outputBuffer,
                    m_ProbeUpdateIndexQueue, m_ProbeUpdateDataQueue, m_ProbeOcclusionUpdateDataQueue);

            return hasTransformUpdates || probesHasUpdates;
        }

        public void Dispose()
        {
            m_Transforms.Dispose();
            m_Indices.Dispose();
            m_HasProbes.Dispose();
            m_TetrahedronCache.Dispose();
            m_CachedTransforms.Dispose();

            m_UpdateQueueCounter.Dispose();
            m_TransformUpdateIndexQueue.Dispose();
            m_TransformUpdateDataQueue.Dispose();
            m_ProbeUpdateIndexQueue.Dispose();
            m_ProbeUpdateDataQueue.Dispose();
            m_ProbeOcclusionUpdateDataQueue.Dispose();
            m_QueryProbePosition.Dispose();

            if (m_LightProbesQuery.IsCreated)
                m_LightProbesQuery.Dispose();

            m_TransformUpdateIndexQueueBuffer.Release();
            m_TransformUpdateDataQueueBuffer.Release();
            m_ProbeUpdateIndexQueueBuffer.Release();
            m_ProbeUpdateDataQueueBuffer.Release();
            m_ProbeOcclusionUpdateDataQueueBuffer.Release();

            m_MeshToBoundIndexMap.Dispose();
            m_MeshBounds.Dispose();
            m_DrawData.Dispose();
        }
    }

}
