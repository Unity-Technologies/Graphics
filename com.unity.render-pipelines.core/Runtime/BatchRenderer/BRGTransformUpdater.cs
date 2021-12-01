using UnityEngine.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Threading;
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

        public bool Equals(BRGMatrix other)
        {
            return math.all(
                (localToWorld0 == other.localToWorld0) &
                (localToWorld1 == other.localToWorld1) &
                (localToWorld2 == other.localToWorld2));
        }
    }

    internal struct BRGTransformUpdater
    {
        private const int sBlockSize = 128;
        private int m_Capacity;
        private int m_Length;
        private NativeArray<int> m_Indices;
        private TransformAccessArray m_Transforms;
        private NativeArray<BRGMatrix> m_CachedTransforms;

        public NativeArray<int> m_UpdateQueueCounter;
        public NativeArray<int> m_TransformUpdateIndexQueue;
        public NativeArray<BRGGpuTransformUpdate> m_TransformUpdateDataQueue;

        private JobHandle m_UpdateTransformsJobHandle;

        private ComputeBuffer m_IndexQueueBuffer;
        private ComputeBuffer m_DataQeueueBuffer;
        private ComputeShader m_UpdateCS;
        private int m_UpdateKernel;

        private static class BRGTransformParams
        {
            public static readonly int _TransformUpdateQueueCount = Shader.PropertyToID("_TransformUpdateQueueCount");
            public static readonly int _TransformUpdateOutputL2WVec4Offset = Shader.PropertyToID("_TransformUpdateOutputL2WVec4Offset");
            public static readonly int _TransformUpdateOutputW2LVec4Offset = Shader.PropertyToID("_TransformUpdateOutputW2LVec4Offset");
            public static readonly int _TransformUpdateDataQueue = Shader.PropertyToID("_TransformUpdateDataQueue");
            public static readonly int _TransformUpdateIndexQueue = Shader.PropertyToID("_TransformUpdateIndexQueue");
            public static readonly int _OutputTransformBuffer = Shader.PropertyToID("_OutputTransformBuffer");
        }

        private void LoadShaders()
        {
            m_UpdateCS = (ComputeShader)Resources.Load("BRGTransformUpdateCS");
            m_UpdateKernel = m_UpdateCS.FindKernel("ScatterUpdateMain");
        }

        private void AddUpdateCommand(
            CommandBuffer cmdBuffer,
            int queueCount,
            int outputByteOffsetL2W,
            int outputByteOffsetW2L,
            ComputeBuffer inputIndexQueueBuffer,
            ComputeBuffer inputDataQueueBuffer,
            GraphicsBuffer outputBuffer,
            NativeArray<int> transformIndexQueue,
            NativeArray<BRGGpuTransformUpdate> updateDataQueue)
        {
            cmdBuffer.SetBufferData(inputIndexQueueBuffer, transformIndexQueue, 0, 0, queueCount);
            cmdBuffer.SetBufferData(inputDataQueueBuffer, updateDataQueue, 0, 0, queueCount);
            cmdBuffer.SetComputeIntParam(m_UpdateCS, BRGTransformParams._TransformUpdateQueueCount, queueCount);
            cmdBuffer.SetComputeIntParam(m_UpdateCS, BRGTransformParams._TransformUpdateOutputL2WVec4Offset, outputByteOffsetL2W);
            cmdBuffer.SetComputeIntParam(m_UpdateCS, BRGTransformParams._TransformUpdateOutputW2LVec4Offset, outputByteOffsetW2L);
            cmdBuffer.SetComputeBufferParam(m_UpdateCS, m_UpdateKernel, BRGTransformParams._TransformUpdateIndexQueue, inputIndexQueueBuffer);
            cmdBuffer.SetComputeBufferParam(m_UpdateCS, m_UpdateKernel, BRGTransformParams._TransformUpdateDataQueue, inputDataQueueBuffer);
            cmdBuffer.SetComputeBufferParam(m_UpdateCS, m_UpdateKernel, BRGTransformParams._OutputTransformBuffer, outputBuffer);
            cmdBuffer.DispatchCompute(m_UpdateCS, m_UpdateKernel, (queueCount + 63) / 64, 1, 1);
        }

        [BurstCompile]
        private struct UpdateJob : IJobParallelForTransform
        {
            public float minDistance;

            [ReadOnly]
            public NativeArray<int> inputIndices;

            public NativeArray<BRGMatrix> cachedTransforms;

            [WriteOnly]
            public NativeArray<int> updateQueueCounter;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<int> transformUpdateIndexQueue;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<BRGGpuTransformUpdate> transformUpdateDataQueue;

            private int IncrementCounter()
            {
                int outputIndex = 0;
                unsafe
                {
                    int* ptr = (int*)updateQueueCounter.GetUnsafePtr<int>();
                    outputIndex = Interlocked.Increment(ref UnsafeUtility.AsRef<int>(ptr));
                }
                return outputIndex - 1;
            }

            public void Execute(int index, TransformAccess transform)
            {
                var m = BRGMatrix.FromMatrix4x4(transform.localToWorldMatrix);

                if (cachedTransforms[index].Equals(m))
                    return;

                int outputIndex = IncrementCounter();
                transformUpdateIndexQueue[outputIndex] = inputIndices[index];

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
            }
        }

        private void RecreteGpuBuffers()
        {
            if (m_IndexQueueBuffer != null)
                m_IndexQueueBuffer.Release();

            if (m_DataQeueueBuffer != null)
                m_DataQeueueBuffer.Release();

            m_IndexQueueBuffer = new ComputeBuffer(m_Capacity, 4, ComputeBufferType.Raw);
            m_DataQeueueBuffer = new ComputeBuffer(m_Capacity, System.Runtime.InteropServices.Marshal.SizeOf<BRGGpuTransformUpdate>(), ComputeBufferType.Structured);
        }

        public void Initialize()
        {
            m_IndexQueueBuffer = null;
            m_DataQeueueBuffer = null;

            LoadShaders();

            m_Length = 0;
            m_Capacity = sBlockSize;
            m_Transforms = new TransformAccessArray(m_Capacity);
            m_Indices = new NativeArray<int>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_CachedTransforms = new NativeArray<BRGMatrix>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_TransformUpdateIndexQueue = new NativeArray<int>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_TransformUpdateDataQueue = new NativeArray<BRGGpuTransformUpdate>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_UpdateQueueCounter = new NativeArray<int>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            RecreteGpuBuffers();
        }

        public void RegisterTransformObject(int instanceIndex, Transform transformObject)
        {
            int newLen = m_Length + 1;
            if (newLen == m_Capacity)
            {
                m_Capacity += sBlockSize;
                m_Transforms.ResizeArray(m_Capacity);
                m_Indices.ResizeArray(m_Capacity);
                m_CachedTransforms.ResizeArray(m_Capacity);
                m_TransformUpdateIndexQueue.ResizeArray(m_Capacity);
                m_TransformUpdateDataQueue.ResizeArray(m_Capacity);
                RecreteGpuBuffers();
            }

            m_Transforms.Add(transformObject);
            m_Indices[m_Length] = instanceIndex;
            m_CachedTransforms[m_Length] = BRGMatrix.FromMatrix4x4(transformObject.localToWorldMatrix);

            m_Length = newLen;
        }

        public void StartUpdateJobs()
        {
            if (m_Length == 0)
                return;

            m_UpdateQueueCounter[0] = 0; //reset queue to 0
            var jobData = new UpdateJob()
            {
                minDistance = System.Single.Epsilon,
                inputIndices = m_Indices,
                cachedTransforms = m_CachedTransforms,
                updateQueueCounter = m_UpdateQueueCounter,
                transformUpdateIndexQueue = m_TransformUpdateIndexQueue,
                transformUpdateDataQueue = m_TransformUpdateDataQueue
            };

            m_UpdateTransformsJobHandle = jobData.ScheduleReadOnly(m_Transforms, 64);
        }

        public bool EndUpdateJobs(CommandBuffer cmdBuffer, int outputByteOffsetL2W, int outputByteOffsetW2L, GraphicsBuffer outputBuffer)
        {
            if (m_Length == 0)
                return false;

            m_UpdateTransformsJobHandle.Complete();
            bool hasUpdates = m_UpdateQueueCounter[0] != 0;

            if (hasUpdates)
                AddUpdateCommand(
                    cmdBuffer, m_UpdateQueueCounter[0],
                    outputByteOffsetL2W, outputByteOffsetW2L, m_IndexQueueBuffer, m_DataQeueueBuffer, outputBuffer,
                    m_TransformUpdateIndexQueue, m_TransformUpdateDataQueue);

            return hasUpdates;
        }

        public void Dispose()
        {
            m_Transforms.Dispose();
            m_Indices.Dispose();
            m_CachedTransforms.Dispose();

            m_UpdateQueueCounter.Dispose();
            m_TransformUpdateIndexQueue.Dispose();
            m_TransformUpdateDataQueue.Dispose();

            m_IndexQueueBuffer.Release();
            m_DataQeueueBuffer.Release();
        }
    }

}
