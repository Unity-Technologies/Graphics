using UnityEngine.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using System;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    internal struct PackedMatrix
    {
        public float4 packed0;
        public float4 packed1;
        public float4 packed2;

        public static PackedMatrix FromMatrix4x4(in Matrix4x4 m)
        {
            return new PackedMatrix
            {
                /*  mat4x3 packed like this:
                      p1.x, p1.w, p2.z, p3.y,
                      p1.y, p2.x, p2.w, p3.z,
                      p1.z, p2.y, p3.x, p3.w,
                      0.0,  0.0,  0.0,  1.0
                */

                packed0 = new float4(m.m00, m.m10, m.m20, m.m01),
                packed1 = new float4(m.m11, m.m21, m.m02, m.m12),
                packed2 = new float4(m.m22, m.m03, m.m13, m.m23)
            };
        }
    }

    [Flags]
    internal enum InstanceFlags : byte
    {
        None = 0,
        HasBounds = 1 << 0, // used as "is valid", set when registering
        AffectsLightmaps = 1 << 1, // either lightmapped or influence-only
        IsShadowsOff = 1 << 2, // shadow casting mode is ShadowCastingMode.Off
        IsShadowsOnly = 1 << 3, // shadow casting mode is ShadowCastingMode.ShadowsOnly
    }

    internal struct InstanceDrawData
    {
        public NativeArray<uint> lodIndicesAndMasks;
        public NativeArray<AABB> localBounds;
        public NativeArray<AABB> worldBounds;
        public NativeArray<Matrix4x4> localToWorldMatrices;
        public ParallelBitArray localToWorldIsFlipped;
        public NativeArray<InstanceFlags> flags;
        public NativeArray<int> layers;

        public int length => localBounds.Length;

        public InstanceDrawData(int initialSize)
        {
            lodIndicesAndMasks = new NativeArray<uint>(initialSize, Allocator.Persistent);
            localBounds = new NativeArray<AABB>(initialSize, Allocator.Persistent);
            worldBounds = new NativeArray<AABB>(initialSize, Allocator.Persistent);
            localToWorldMatrices = new NativeArray<Matrix4x4>(initialSize, Allocator.Persistent);
            localToWorldIsFlipped = new ParallelBitArray(initialSize, Allocator.Persistent);
            flags = new NativeArray<InstanceFlags>(initialSize, Allocator.Persistent);
            layers = new NativeArray<int>(initialSize, Allocator.Persistent);
        }

        public void GrowBuffers(int newCapacity)
        {
            var oldCapacity = length;

            if (newCapacity > oldCapacity)
            {
                lodIndicesAndMasks.ResizeArray(newCapacity);
                localBounds.ResizeArray(newCapacity);
                worldBounds.ResizeArray(newCapacity);
                localToWorldMatrices.ResizeArray(newCapacity);
                localToWorldIsFlipped.Resize(newCapacity);
                flags.ResizeArray(newCapacity);
                layers.ResizeArray(newCapacity);

                // initialize the new elements
                unsafe
                {
                    InstanceFlags* flagsPtr = (InstanceFlags*)flags.GetUnsafePtr();
                    UnsafeUtility.MemClear(flagsPtr + oldCapacity, (newCapacity - oldCapacity) * UnsafeUtility.SizeOf<InstanceFlags>());
                }
            }
        }

        public void Dispose()
        {
            if (lodIndicesAndMasks.IsCreated)
                lodIndicesAndMasks.Dispose();

            if (localBounds.IsCreated)
                localBounds.Dispose();

            if (worldBounds.IsCreated)
                worldBounds.Dispose();

            if (localToWorldMatrices.IsCreated)
                localToWorldMatrices.Dispose();

            if (localToWorldIsFlipped.IsCreated)
                localToWorldIsFlipped.Dispose();

            if (flags.IsCreated)
                flags.Dispose();

            if (layers.IsCreated)
                layers.Dispose();
        }
    }

    [Flags]
    internal enum TransformUpdaterFlags
    {
        None = 0,
        AccessBoundsInCPU = 1 << 0,
        HasLightProbeCombined = 1 << 1,
        IsPartOfStaticBatch = 1 << 2
    }

    internal struct TransformIndex : IEquatable<TransformIndex>
    {
        public int index;
        public static readonly TransformIndex Invalid = new TransformIndex() { index = -1 };
        public bool valid => index != -1;
        public bool Equals(TransformIndex other) => index == other.index;
    }

    internal struct ParallelBitArray
    {
        private NativeArray<long> m_Bits;
        private int m_Length;

        public int Length => m_Length;

        public ParallelBitArray(int length, Allocator allocator)
        {
            m_Bits = new NativeArray<long>((length + 63) / 64, allocator);
            m_Length = length;
        }

        public bool IsCreated => m_Bits.IsCreated;

        public void Dispose()
        {
            m_Bits.Dispose();
            m_Length = 0;
        }

        public void Resize(int newLength)
        {
            int oldLength = m_Length;
            if (newLength == oldLength)
                return;

            int oldBitsLength = m_Bits.Length;
            int newBitsLength = (newLength + 63) / 64;
            if (newBitsLength != oldBitsLength)
                m_Bits.ResizeArray(newBitsLength);

            // mask off bits past the length
            int validLength = Math.Min(oldLength, newLength);
            int validBitsLength = Math.Min(oldBitsLength, newBitsLength);
            for (int chunkIndex = validBitsLength; chunkIndex < m_Bits.Length; ++chunkIndex)
            {
                int validBitCount = Math.Max(validLength - 64 * chunkIndex, 0);
                if (validBitCount < 64)
                {
                    ulong validMask = (1ul << validBitCount) - 1;
                    m_Bits[chunkIndex] &= (long)validMask;
                }
            }
            m_Length = newLength;
        }

        public void Set(int index, bool value)
        {
            unsafe
            {
                Debug.Assert(0 <= index && index < m_Length);

                int entry_index = index >> 6;
                long* entries = (long*)m_Bits.GetUnsafePtr();

                ulong bit = 1ul << (index & 0x3f);
                long and_mask = (long)(~bit);
                long or_mask = value ? (long)bit : 0;

                long old_entry, new_entry;
                do
                {
                    old_entry = Interlocked.Read(ref entries[entry_index]);
                    new_entry = (old_entry & and_mask) | or_mask;
                } while (Interlocked.CompareExchange(ref entries[entry_index], new_entry, old_entry) != old_entry);
            }
        }

        public bool Get(int index)
        {
            unsafe
            {
                Debug.Assert(0 <= index && index < m_Length);

                int entry_index = index >> 6;
                long* entries = (long*)m_Bits.GetUnsafeReadOnlyPtr();

                ulong bit = 1ul << (index & 0x3f);
                long check_mask = (long)bit;
                return (entries[entry_index] & check_mask) != 0;
            }
        }

        public ulong GetChunk(int chunk_index)
        {
            return (ulong)m_Bits[chunk_index];
        }

        public void SetChunk(int chunk_index, ulong chunk_bits)
        {
            m_Bits[chunk_index] = (long)chunk_bits;
        }

        public int ChunkCount()
        {
            return m_Bits.Length;
        }
    }

    internal class TransformUpdater : IDisposable
    {
        private const int k_BlockSize = 128;
        private const int k_IntsPerCacheLine = Unity.Jobs.LowLevel.Unsafe.JobsUtility.CacheLineSize / sizeof(int);

        private int m_Capacity;
        private int m_Length;
        private NativeArray<int> m_Indices;
        private NativeArray<TransformUpdaterFlags> m_UpdaterFlags;
        private NativeArray<int> m_TetrahedronCache;
        private ParallelBitArray m_MovedInCurrentFrame;
        private ParallelBitArray m_MovedInPreviousFrame;

        private NativeArray<int> m_TransformUpdateIndexQueue;
        private NativeArray<TransformUpdatePacket> m_TransformUpdateDataQueue;
        private NativeArray<float4> m_BoundingSpheresUpdateDataQueue;
        private NativeArray<int> m_ProbeUpdateIndexQueue;
        private NativeArray<int> m_CompactTetrahedronCache;
        private NativeArray<SphericalHarmonicsL2> m_ProbeUpdateDataQueue;
        private NativeArray<Vector4> m_ProbeOcclusionUpdateDataQueue;
        private NativeArray<Vector3> m_QueryProbePosition;

        private ComputeBuffer m_TransformUpdateIndexQueueBuffer;
        private ComputeBuffer m_TransformUpdateDataQueueBuffer;
        private ComputeBuffer m_BoundingSpheresUpdateDataQueueBuffer;
        private ComputeBuffer m_ProbeUpdateIndexQueueBuffer;
        private ComputeBuffer m_ProbeUpdateDataQueueBuffer;
        private ComputeBuffer m_ProbeOcclusionUpdateDataQueueBuffer;
        private ComputeShader m_TransformUpdateCS;
        private int m_TransformInitKernel;
        private int m_TransformUpdateKernel;
        private int m_ProbeUpdateKernel;
        private int m_LodUpdateKernel;

        private bool m_EnableBoundingSpheres;

        private InstanceDrawData m_DrawData;

        public InstanceDrawData drawData => m_DrawData;
        public NativeArray<int> indices => m_Indices.GetSubArray(0, m_Length);
        public NativeArray<TransformUpdaterFlags> transformUpdaterFlags => m_UpdaterFlags.GetSubArray(0, m_Length);
        public ParallelBitArray movedTransformIndices => m_MovedInPreviousFrame;

        public int capacity => m_Capacity;
        public int length => m_Length;

        private static class TransformUpdaterIDs
        {
            //Transforms update kernel IDs
            public static readonly int _TransformUpdateQueueCount = Shader.PropertyToID("_TransformUpdateQueueCount");
            public static readonly int _TransformUpdateCombinedQueueCount = Shader.PropertyToID("_TransformUpdateCombinedQueueCount");
            public static readonly int _TransformUpdateOutputL2WVec4Offset = Shader.PropertyToID("_TransformUpdateOutputL2WVec4Offset");
            public static readonly int _TransformUpdateOutputW2LVec4Offset = Shader.PropertyToID("_TransformUpdateOutputW2LVec4Offset");
            public static readonly int _TransformUpdateOutputPrevL2WVec4Offset = Shader.PropertyToID("_TransformUpdateOutputPrevL2WVec4Offset");
            public static readonly int _TransformUpdateOutputPrevW2LVec4Offset = Shader.PropertyToID("_TransformUpdateOutputPrevW2LVec4Offset");
            public static readonly int _BoundingSphereOutputVec4Offset = Shader.PropertyToID("_BoundingSphereOutputVec4Offset");
            public static readonly int _TransformUpdateDataQueue = Shader.PropertyToID("_TransformUpdateDataQueue");
            public static readonly int _TransformUpdateIndexQueue = Shader.PropertyToID("_TransformUpdateIndexQueue");
            public static readonly int _BoundingSphereDataQueue = Shader.PropertyToID("_BoundingSphereDataQueue");
            public static readonly int _OutputTransformBuffer = Shader.PropertyToID("_OutputTransformBuffer");

            //Probe update kernel IDs
            public static readonly int _ProbeUpdateQueueCount = Shader.PropertyToID("_ProbeUpdateQueueCount");
            public static readonly int _SHUpdateVec4Offset = Shader.PropertyToID("_SHUpdateVec4Offset");
            public static readonly int _ProbeUpdateDataQueue = Shader.PropertyToID("_ProbeUpdateDataQueue");
            public static readonly int _ProbeOcclusionUpdateDataQueue = Shader.PropertyToID("_ProbeOcclusionUpdateDataQueue");
            public static readonly int _ProbeUpdateIndexQueue = Shader.PropertyToID("_ProbeUpdateIndexQueue");
            public static readonly int _OutputProbeBuffer = Shader.PropertyToID("_OutputProbeBuffer");
        }

        private void LoadShaders(GPUResidentDrawerResources resources)
        {
            m_TransformUpdateCS = resources.transformUpdaterKernels;
            if (m_EnableBoundingSpheres)
                m_TransformUpdateCS.EnableKeyword("PROCESS_BOUNDING_SPHERES");
            else
                m_TransformUpdateCS.DisableKeyword("PROCESS_BOUNDING_SPHERES");

            m_TransformInitKernel = m_TransformUpdateCS.FindKernel("ScatterInitTransformMain");
            m_TransformUpdateKernel = m_TransformUpdateCS.FindKernel("ScatterUpdateTransformMain");
            m_ProbeUpdateKernel = m_TransformUpdateCS.FindKernel("ScatterUpdateProbesMain");
        }

        private unsafe static int IncrementCounter(NativeArray<int> counter)
        {
            return Interlocked.Increment(ref UnsafeUtility.AsRef<int>((int*)counter.GetUnsafePtr())) - 1;
        }

        private unsafe static int AddCounter(NativeArray<int> counter, int value)
        {
            return Interlocked.Add(ref UnsafeUtility.AsRef<int>((int*)counter.GetUnsafePtr()), value) - value;
        }

        private void DispatchTransformUpdateCommand(
            bool reinitialize,
            int transformQueueCount,
            int motionQueueCount,
            RenderersParameters renderersParameters,
            ComputeBuffer inputIndexQueueBuffer,
            ComputeBuffer inputDataQueueBuffer,
            NativeArray<int> transformIndexQueue,
            ComputeBuffer boundingSphereDataQueueBuffer,
            NativeArray<TransformUpdatePacket> updateDataQueue,
            NativeArray<float4> boundingSphereUpdateDataQueue,
            GraphicsBuffer outputBuffer)
        {
            int kernel = reinitialize ? m_TransformInitKernel : m_TransformUpdateKernel;
            // When we reinitialize we have the current and the previous matrices per transform.
            int transformQueueDataSize = transformQueueCount * (reinitialize ? 2 : 1);
            int combinedQueueCount = transformQueueCount + motionQueueCount;
            Profiler.BeginSample("PrepareTransformUpdateDispatch");
            Profiler.BeginSample("ComputeBuffer.SetData");
            inputIndexQueueBuffer.SetData(transformIndexQueue, 0, 0, combinedQueueCount);
            inputDataQueueBuffer.SetData(updateDataQueue, 0, 0, transformQueueDataSize);
            if (m_EnableBoundingSpheres)
                boundingSphereDataQueueBuffer.SetData(boundingSphereUpdateDataQueue, 0, 0, transformQueueCount);
            Profiler.EndSample();
            m_TransformUpdateCS.SetInt(TransformUpdaterIDs._TransformUpdateQueueCount, transformQueueCount);
            m_TransformUpdateCS.SetInt(TransformUpdaterIDs._TransformUpdateCombinedQueueCount, combinedQueueCount);
            m_TransformUpdateCS.SetInt(TransformUpdaterIDs._TransformUpdateOutputL2WVec4Offset, renderersParameters.localToWorld.uintOffset);
            m_TransformUpdateCS.SetInt(TransformUpdaterIDs._TransformUpdateOutputW2LVec4Offset, renderersParameters.worldToLocal.uintOffset);
            m_TransformUpdateCS.SetInt(TransformUpdaterIDs._TransformUpdateOutputPrevL2WVec4Offset, renderersParameters.matrixPreviousM.uintOffset);
            m_TransformUpdateCS.SetInt(TransformUpdaterIDs._TransformUpdateOutputPrevW2LVec4Offset, renderersParameters.matrixPreviousMI.uintOffset);
            m_TransformUpdateCS.SetBuffer(kernel, TransformUpdaterIDs._TransformUpdateIndexQueue, inputIndexQueueBuffer);
            m_TransformUpdateCS.SetBuffer(kernel, TransformUpdaterIDs._TransformUpdateDataQueue, inputDataQueueBuffer);
            if (m_EnableBoundingSpheres)
            {
                Assert.IsTrue(renderersParameters.boundingSphere.valid);
                m_TransformUpdateCS.SetInt(TransformUpdaterIDs._BoundingSphereOutputVec4Offset, renderersParameters.boundingSphere.uintOffset);
                m_TransformUpdateCS.SetBuffer(kernel, TransformUpdaterIDs._BoundingSphereDataQueue, boundingSphereDataQueueBuffer);
            }
            m_TransformUpdateCS.SetBuffer(kernel, TransformUpdaterIDs._OutputTransformBuffer, outputBuffer);
            Profiler.EndSample();
            m_TransformUpdateCS.Dispatch(kernel, (combinedQueueCount + 63) / 64, 1, 1);
        }

        private void DispatchProbeUpdateCommand(
            int queueCount,
            RenderersParameters renderersParameters,
            ComputeBuffer inputIndexQueueBuffer,
            ComputeBuffer inputDataQueueBuffer,
            ComputeBuffer inputDataProbeOcclusionQueueBuffer,
            GraphicsBuffer outputBuffer,
            NativeArray<int> probeIndexQueue,
            NativeArray<SphericalHarmonicsL2> probeUpdateDataQueue,
            NativeArray<Vector4> probeOcclusionUpdateDataQueue)
        {
            Profiler.BeginSample("PrepareProbeUpdateDispatch");
            Profiler.BeginSample("ComputeBuffer.SetData");
            inputIndexQueueBuffer.SetData(probeIndexQueue, 0, 0, queueCount);
            inputDataQueueBuffer.SetData(probeUpdateDataQueue, 0, 0, queueCount);
            Profiler.EndSample();
            inputDataProbeOcclusionQueueBuffer.SetData(probeOcclusionUpdateDataQueue, 0, 0, queueCount);
            m_TransformUpdateCS.SetInt(TransformUpdaterIDs._ProbeUpdateQueueCount, queueCount);
            m_TransformUpdateCS.SetInt(TransformUpdaterIDs._SHUpdateVec4Offset, renderersParameters.shCoefficients.uintOffset);
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, TransformUpdaterIDs._ProbeUpdateIndexQueue, inputIndexQueueBuffer);
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, TransformUpdaterIDs._ProbeUpdateDataQueue, inputDataQueueBuffer);
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, TransformUpdaterIDs._ProbeOcclusionUpdateDataQueue, inputDataProbeOcclusionQueueBuffer);
            m_TransformUpdateCS.SetBuffer(m_ProbeUpdateKernel, TransformUpdaterIDs._OutputProbeBuffer, outputBuffer);
            Profiler.EndSample();
            m_TransformUpdateCS.Dispatch(m_ProbeUpdateKernel, (queueCount + 63) / 64, 1, 1);
        }

        [BurstCompile]
        private struct CalculateInterpolatedLightAndOcclusionProbesBatchJob : IJobParallelFor
        {
            public const int k_BatchSize = 1;
            public const int k_CalculatedProbesPerBatch = 128;

            public int probesCount;

            [ReadOnly]
            public LightProbesQuery lightProbesQuery;

            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<Vector3> queryPostitions;
            [NativeDisableParallelForRestriction] public NativeArray<int> compactTetrahedronCache;
            [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<SphericalHarmonicsL2> probesSphericalHarmonics;
            [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<Vector4> probesOcclusion;

            public void Execute(int index)
            {
                var startIndex = index * k_CalculatedProbesPerBatch;
                var endIndex = math.min(probesCount, startIndex + k_CalculatedProbesPerBatch);
                var count = endIndex - startIndex;

                var compactTetrahedronCacheSubArray = compactTetrahedronCache.GetSubArray(startIndex, count);
                var queryPostitionsSubArray = queryPostitions.GetSubArray(startIndex, count);
                var probesSphericalHarmonicsSubArray = probesSphericalHarmonics.GetSubArray(startIndex, count);
                var probesOcclusionSubArray = probesOcclusion.GetSubArray(startIndex, count);
                lightProbesQuery.CalculateInterpolatedLightAndOcclusionProbes(queryPostitionsSubArray, compactTetrahedronCacheSubArray, probesSphericalHarmonicsSubArray, probesOcclusionSubArray);
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        private struct UpdateTransformsBoundsProbesJob : IJobParallelFor
        {
            public const int k_BatchSize = 128;

            [ReadOnly]
            public bool reinitialize;

            [ReadOnly]
            public NativeArray<InstanceHandle> instances;

            [ReadOnly]
            public NativeArray<TransformIndex> transformIndices;

            [ReadOnly]
            public NativeArray<AABB> localAABB;

            [ReadOnly]
            public NativeArray<Matrix4x4> localToWorldMatrices;

            [ReadOnly]
            public NativeArray<Matrix4x4> prevLocalToWorldMatrices;

            [ReadOnly]
            public NativeArray<TransformUpdaterFlags> updaterFlags;

            [WriteOnly]
            public NativeArray<int> transformQueueCounter;

            [WriteOnly]
            public NativeArray<int> probesQueueCounter;

            public ParallelBitArray movedInCurrentFrame;

            public bool enableBoundingSpheres;

            [NativeDisableParallelForRestriction] public NativeArray<int> transformUpdateIndexStateQueue;
            [NativeDisableParallelForRestriction] public NativeArray<TransformUpdatePacket> transformUpdateDataQueue;
            [NativeDisableParallelForRestriction] public NativeArray<float4> boundingSpheresDataQueue;
            [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<AABB> boundsToUpdate;
            [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<Matrix4x4> matricesToUpdate;
            public ParallelBitArray isFlippedToUpdate;

            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<int> tetrahedronCache;
            [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<Vector3> probeQueryPosition;
            [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<int> probeUpdateIndexQueue;
            [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<int> compactTetrahedronCache;

            public void Execute(int index)
            {
                InstanceHandle instance = instances[index];

                if (!instance.valid)
                    return;

                int instanceIndex = instance.index;
                TransformIndex transformIndex = transformIndices[instanceIndex];

                if (!transformIndex.valid)
                    return;

                TransformUpdaterFlags flags = updaterFlags[transformIndex.index];
                bool movedCurrentFrame = movedInCurrentFrame.Get(transformIndex.index);
                bool isStaticObject = (flags & TransformUpdaterFlags.IsPartOfStaticBatch) != 0;

                if (!reinitialize && (isStaticObject || movedCurrentFrame))
                    return;

                movedInCurrentFrame.Set(transformIndex.index, !isStaticObject);

                int outputIndex = IncrementCounter(transformQueueCounter);
                transformUpdateIndexStateQueue[outputIndex] = instanceIndex;

                Matrix4x4 l2w = localToWorldMatrices[index];
                matricesToUpdate[instanceIndex] = l2w;

                float det = math.determinant((float3x3)(float4x4)l2w);
                isFlippedToUpdate.Set(instanceIndex, det < 0.0f);

                AABB worldAABB = AABB.Transform(l2w, localAABB[instanceIndex]);

                if ((flags & TransformUpdaterFlags.AccessBoundsInCPU) != 0)
                    boundsToUpdate[instanceIndex] = worldAABB;

                if(reinitialize)
                {
                    PackedMatrix l2wPacked = PackedMatrix.FromMatrix4x4(l2w);
                    PackedMatrix l2wPrevPacked = PackedMatrix.FromMatrix4x4(prevLocalToWorldMatrices[index]);
                    transformUpdateDataQueue[outputIndex * 2] = new TransformUpdatePacket()
                    {
                        localToWorld0 = l2wPacked.packed0,
                        localToWorld1 = l2wPacked.packed1,
                        localToWorld2 = l2wPacked.packed2,
                    };
                    transformUpdateDataQueue[outputIndex * 2 + 1] = new TransformUpdatePacket()
                    {
                        localToWorld0 = l2wPrevPacked.packed0,
                        localToWorld1 = l2wPrevPacked.packed1,
                        localToWorld2 = l2wPrevPacked.packed2,
                    };
                }
                else
                {
                    PackedMatrix l2wPacked = PackedMatrix.FromMatrix4x4(l2w);
                    transformUpdateDataQueue[outputIndex] = new TransformUpdatePacket()
                    {
                        localToWorld0 = l2wPacked.packed0,
                        localToWorld1 = l2wPacked.packed1,
                        localToWorld2 = l2wPacked.packed2,
                    };
                }

                if (enableBoundingSpheres)
                    boundingSpheresDataQueue[outputIndex] = new float4(worldAABB.center.x, worldAABB.center.y, worldAABB.center.z, math.distance(worldAABB.max, worldAABB.min) * 0.5f);

                if ((flags & TransformUpdaterFlags.HasLightProbeCombined) != 0)
                {
                    int probeOutputIndex = IncrementCounter(probesQueueCounter);

                    //@ Implement 'Anchor Override'.
                    probeQueryPosition[probeOutputIndex] = worldAABB.center;
                    probeUpdateIndexQueue[probeOutputIndex] = instanceIndex;
                    compactTetrahedronCache[probeOutputIndex] = tetrahedronCache[transformIndex.index];
                }
            }
        }

        [BurstCompile]
        private struct ScatterTetrahedronCacheIndicesJob : IJobParallelFor
        {
            public const int k_BatchSize = 128;

            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<int> probeIndices;
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<TransformIndex> transformIndices;
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<int> compactTetrahedronCache;
            [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<int> tetrahedronCache;

            public void Execute(int index)
            {
                var scatterIndex = transformIndices[probeIndices[index]];
                tetrahedronCache[scatterIndex.index] = compactTetrahedronCache[index];
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        private struct MotionUpdateJob : IJobParallelFor
        {
            public const int k_BatchSize = 16;

            public ParallelBitArray movedInCurrentFrame;
            public ParallelBitArray movedInPreviousFrame;
            public int queueWriteBase;

            [ReadOnly]
            public NativeArray<int> inputIndices;

            [WriteOnly]
            public NativeArray<int> updateQueueCounter;
            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeArray<int> transformUpdateIndexStateQueue;

            public void Execute(int chunk_index)
            {
                ulong currentChunkBits = movedInCurrentFrame.GetChunk(chunk_index);
                ulong prevChunkBits = movedInPreviousFrame.GetChunk(chunk_index);

                // update state in memory for the next frame
                movedInCurrentFrame.SetChunk(chunk_index, 0);
                movedInPreviousFrame.SetChunk(chunk_index, currentChunkBits);

                // ensure that objects that were moved last frame update their previous world matrix, if not already fully updated
                ulong remainingChunkBits = prevChunkBits & ~currentChunkBits;

                // allocate space for all the writes from this chunk
                int chunkBitCount = math.countbits(remainingChunkBits);
                int writeIndex = queueWriteBase;
                if (chunkBitCount > 0)
                    writeIndex += AddCounter(updateQueueCounter, chunkBitCount);

                // loop over set bits to do the writes
                int indexInChunk = math.tzcnt(remainingChunkBits);
                while (indexInChunk < 64)
                {
                    int transformIndex = 64 * chunk_index + indexInChunk;
                    transformUpdateIndexStateQueue[writeIndex] = inputIndices[transformIndex];

                    remainingChunkBits &= ~(1ul << indexInChunk);
                    indexInChunk = math.tzcnt(remainingChunkBits);
                    ++writeIndex;
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        private struct ProbesUpdateJob : IJobParallelFor
        {
            public const int k_BatchSize = 128;

            [ReadOnly]
            public NativeArray<int> inputIndices;

            [ReadOnly]
            public NativeArray<TransformIndex> transformIndices;

            [ReadOnly]
            public NativeArray<AABB> worldAABBs;

            [ReadOnly]
            public NativeArray<TransformUpdaterFlags> updaterFlags;

            [WriteOnly]
            public NativeArray<int> probesQueueCounter;

            [NativeDisableParallelForRestriction] public NativeArray<int> tetrahedronCache;
            [NativeDisableParallelForRestriction] public NativeArray<int> probeUpdateIndexQueue;
            [NativeDisableParallelForRestriction] public NativeArray<int> compactTetrahedronCache;
            [NativeDisableParallelForRestriction] public NativeArray<Vector3> probeQueryPosition;

            public void Execute(int index)
            {
                if ((updaterFlags[index] & TransformUpdaterFlags.HasLightProbeCombined) != 0)
                {
                    int probeOutputIndex = IncrementCounter(probesQueueCounter);
                    int instanceIndex = inputIndices[index];
                    var transformIndex = transformIndices[instanceIndex];

                    //@ Implement 'Anchor Override'.
                    probeQueryPosition[probeOutputIndex] = worldAABBs[instanceIndex].center;
                    probeUpdateIndexQueue[probeOutputIndex] = instanceIndex;
                    compactTetrahedronCache[probeOutputIndex] = tetrahedronCache[transformIndex.index];
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        internal struct AllocateTransformObjectsJob : IJob
        {
            [ReadOnly] public NativeArray<InstanceHandle> newInstances;

            [WriteOnly] public NativeArray<TransformIndex> newTransformIndices;
            [WriteOnly] public NativeArray<int> indices;

            public int length;

            public void Execute()
            {
                for (int i = 0; i < newInstances.Length; ++i)
                {
                    int newIndex = length++;
                    var newInstance = newInstances[i];
                    indices[newIndex] = newInstance.index;
                    newTransformIndices[i] = new TransformIndex() { index = newIndex };
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        internal struct UpdateTransformObjectsJob : IJob
        {
            [ReadOnly] public NativeParallelHashMap<int, InstanceHandle> lodGroupDataMap;
            [ReadOnly] public NativeArray<InstanceHandle> instances;
            [ReadOnly] public NativeArray<TransformIndex> transformIndices;
            [ReadOnly] public GPUDrivenRendererData rendererData;
            [ReadOnly] public int length;

            [WriteOnly] public NativeArray<AABB> localBounds;
            [WriteOnly] public NativeArray<TransformUpdaterFlags> updaterFlags;
            [WriteOnly] public NativeArray<int> tetrahedronCache;
            [WriteOnly] public NativeArray<uint> lodIndicesAndMasks;
            [WriteOnly] public NativeArray<InstanceFlags> flags;
            [WriteOnly] public NativeArray<int> layers;

            [BurstCompile]
            private uint GetLODGroupAndMask(int lodGroupInstanceID, byte lodMask)
            {
                uint lodGroupAndMask = 0xFFFFFFFF;

                // Renderer's LODGroup could be disabled which means that the renderer is not managed by it.
                if (lodGroupDataMap.TryGetValue(lodGroupInstanceID, out var lodGroupHandle))
                {
                    if (lodMask > 0)
                        lodGroupAndMask = (uint)lodGroupHandle.index << 8 | lodMask;
                }

                return lodGroupAndMask;
            }

            public void Execute()
            {
                for (int i = 0; i < rendererData.rendererID.Length; ++i)
                {
                    var instanceIndex = instances[i].index;
                    var transformIndex = transformIndices[instanceIndex];

                    Assert.IsTrue(transformIndex.index < length, "Use the transform index returned by the RegisterTransformObject function.");

                    var packedRendererData = rendererData.packedRendererData[i];
                    var lodGroupID = rendererData.lodGroupID[i];
                    var localAABB = rendererData.localBounds[i].ToAABB();
                    var gameObjectLayer = rendererData.gameObjectLayer[i];
                    var lightmapIndex = rendererData.lightmapIndex[i];

                    const int kLightmapIndexMask = 0xffff;
                    const int kLightmapIndexNotLightmapped = 0xffff;
                    const int kLightmapIndexInfluenceOnly = 0xfffe;

                    var instanceFlags = InstanceFlags.None;
                    var transformUpdaterFlags = TransformUpdaterFlags.AccessBoundsInCPU;
                    var lmIndexMasked = lightmapIndex & kLightmapIndexMask;

                    // Object doesn't have a valid lightmap Index, -> uses probes for lighting
                    if (lmIndexMasked >= kLightmapIndexInfluenceOnly)
                    {
                        // Only add the component when needed to store blended results (shader will use the ambient probe when not present)
                        if (packedRendererData.lightProbeUsage == LightProbeUsage.BlendProbes)
                            transformUpdaterFlags |= TransformUpdaterFlags.HasLightProbeCombined;
                    }

                    if(packedRendererData.isPartOfStaticBatch)
                        transformUpdaterFlags |= TransformUpdaterFlags.IsPartOfStaticBatch;

                    switch (packedRendererData.shadowCastingMode)
                    {
                        case ShadowCastingMode.Off:
                            instanceFlags |= InstanceFlags.IsShadowsOff;
                            break;
                        case ShadowCastingMode.ShadowsOnly:
                            instanceFlags |= InstanceFlags.IsShadowsOnly;
                            break;
                        default:
                            // visible in both cameras and shadows, no flags to set
                            break;
                    }

                    // If the object is lightmapped, or has the special influence-only value, it affects lightmaps
                    if (lmIndexMasked != kLightmapIndexNotLightmapped)
                        instanceFlags |= InstanceFlags.AffectsLightmaps;

                    if ((transformUpdaterFlags & TransformUpdaterFlags.AccessBoundsInCPU) != 0)
                        instanceFlags |= InstanceFlags.HasBounds;

                    uint lodGroupAndMask = GetLODGroupAndMask(lodGroupID, packedRendererData.lodMask);

                    updaterFlags[transformIndex.index] = transformUpdaterFlags;
                    tetrahedronCache[transformIndex.index] = -1;

                    localBounds[instanceIndex] = localAABB;
                    lodIndicesAndMasks[instanceIndex] = lodGroupAndMask;
                    flags[instanceIndex] = instanceFlags;
                    layers[instanceIndex] = gameObjectLayer;
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        internal struct DestroyTransformObjectsJob : IJob
        {
            [ReadOnly] public NativeArray<InstanceHandle> instances;

            public GPURendererInstancePool.GPURendererInstanceDataArrays instanceData;

            public NativeArray<int> length;
            public NativeArray<int> indices;
            public NativeArray<TransformUpdaterFlags> updaterFlags;
            public NativeArray<int> tetrahedronCache;
            public ParallelBitArray movedInCurrentFrame;
            public ParallelBitArray movedInPreviousFrame;

            [BurstCompile]
            private void DeleteTransformObjectSwapBack(TransformIndex transformIndex)
            {
                int currentLength = length[0];

                Assert.IsTrue(transformIndex.index < currentLength, "Use the transform index returned by the RegisterTransformObject function.");

                indices[transformIndex.index] = indices[currentLength - 1];
                updaterFlags[transformIndex.index] = updaterFlags[currentLength - 1];
                tetrahedronCache[transformIndex.index] = tetrahedronCache[currentLength - 1];
                movedInCurrentFrame.Set(transformIndex.index, movedInCurrentFrame.Get(currentLength - 1));
                movedInPreviousFrame.Set(transformIndex.index, movedInPreviousFrame.Get(currentLength - 1));

                --length[0];
            }

            public void Execute()
            {
                foreach (var instance in instances)
                {
                    if (instance.valid && instanceData.valid[instance.index])
                    {
                        int currentLength = length[0];
                        var lastTransformIndex = currentLength - 1;
                        int lastTransformIndexInstance = indices[lastTransformIndex];
                        DeleteTransformObjectSwapBack(instanceData.transformIndices[instance.index]);
                        instanceData.transformIndices[lastTransformIndexInstance] = instanceData.transformIndices[instance.index];
                    }
                }
            }
        }

        public TransformUpdater(int initialCapacity, bool enableBoundingSpheres, GPUResidentDrawerResources resources)
        {
            m_Length = 0;
            m_Capacity = Math.Max(initialCapacity, k_BlockSize);
            m_EnableBoundingSpheres = enableBoundingSpheres;
            m_Indices = new NativeArray<int>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_UpdaterFlags = new NativeArray<TransformUpdaterFlags>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_TetrahedronCache = new NativeArray<int>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_MovedInCurrentFrame = new ParallelBitArray(m_Capacity, Allocator.Persistent);
            m_MovedInPreviousFrame = new ParallelBitArray(m_Capacity, Allocator.Persistent);

            Assert.IsTrue(System.Runtime.InteropServices.Marshal.SizeOf<SHUpdatePacket>() == System.Runtime.InteropServices.Marshal.SizeOf<SphericalHarmonicsL2>());

            m_TransformUpdateIndexQueue = new NativeArray<int>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_TransformUpdateDataQueue = new NativeArray<TransformUpdatePacket>(m_Capacity * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_BoundingSpheresUpdateDataQueue = new NativeArray<float4>(m_EnableBoundingSpheres ? m_Capacity : 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_ProbeUpdateIndexQueue = new NativeArray<int>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_CompactTetrahedronCache = new NativeArray<int>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_ProbeUpdateDataQueue = new NativeArray<SphericalHarmonicsL2>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_ProbeOcclusionUpdateDataQueue = new NativeArray<Vector4>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_QueryProbePosition = new NativeArray<Vector3>(m_Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            m_TransformUpdateIndexQueueBuffer = new ComputeBuffer(m_Capacity, 4, ComputeBufferType.Raw);
            m_TransformUpdateDataQueueBuffer = new ComputeBuffer(m_Capacity * 2, System.Runtime.InteropServices.Marshal.SizeOf<TransformUpdatePacket>(), ComputeBufferType.Structured);
            m_ProbeUpdateIndexQueueBuffer = new ComputeBuffer(m_Capacity, 4, ComputeBufferType.Raw);
            m_ProbeUpdateDataQueueBuffer = new ComputeBuffer(m_Capacity, System.Runtime.InteropServices.Marshal.SizeOf<SHUpdatePacket>(), ComputeBufferType.Structured);
            m_ProbeOcclusionUpdateDataQueueBuffer = new ComputeBuffer(m_Capacity, System.Runtime.InteropServices.Marshal.SizeOf<Vector4>(), ComputeBufferType.Structured);
            if (m_EnableBoundingSpheres)
                m_BoundingSpheresUpdateDataQueueBuffer = new ComputeBuffer(m_Capacity, System.Runtime.InteropServices.Marshal.SizeOf<float4>(), ComputeBufferType.Structured);

            m_DrawData = new InstanceDrawData(initialCapacity);

            LoadShaders(resources);
        }

        private void DisposeGraphicsBuffers()
        {
            m_TransformUpdateIndexQueueBuffer?.Release();
            m_TransformUpdateDataQueueBuffer?.Release();
            m_BoundingSpheresUpdateDataQueueBuffer?.Release();
            m_ProbeUpdateIndexQueueBuffer?.Release();
            m_ProbeUpdateDataQueueBuffer?.Release();
            m_ProbeOcclusionUpdateDataQueueBuffer?.Release();
        }

        public void Dispose()
        {
            m_Indices.Dispose();
            m_UpdaterFlags.Dispose();
            m_TetrahedronCache.Dispose();
            m_MovedInCurrentFrame.Dispose();
            m_MovedInPreviousFrame.Dispose();

            m_TransformUpdateIndexQueue.Dispose();
            m_TransformUpdateDataQueue.Dispose();
            m_BoundingSpheresUpdateDataQueue.Dispose();
            m_ProbeUpdateIndexQueue.Dispose();
            m_CompactTetrahedronCache.Dispose();
            m_ProbeUpdateDataQueue.Dispose();
            m_ProbeOcclusionUpdateDataQueue.Dispose();
            m_QueryProbePosition.Dispose();

            DisposeGraphicsBuffers();

            m_DrawData.Dispose();
        }

        public void GrowBuffers(int newLength)
        {
            if (newLength > m_Capacity)
            {
                m_Capacity = Math.Max(m_Capacity, newLength) + k_BlockSize;

                m_Indices.ResizeArray(m_Capacity);
                m_UpdaterFlags.ResizeArray(m_Capacity);
                m_TetrahedronCache.ResizeArray(m_Capacity);
                m_MovedInCurrentFrame.Resize(m_Capacity);
                m_MovedInPreviousFrame.Resize(m_Capacity);

                m_TransformUpdateIndexQueue.ResizeArray(m_Capacity);
                m_TransformUpdateDataQueue.ResizeArray(m_Capacity * 2);
                if (m_EnableBoundingSpheres)
                    m_BoundingSpheresUpdateDataQueue.ResizeArray(m_Capacity);
                m_ProbeUpdateIndexQueue.ResizeArray(m_Capacity);
                m_CompactTetrahedronCache.ResizeArray(m_Capacity);
                m_ProbeUpdateDataQueue.ResizeArray(m_Capacity);
                m_ProbeOcclusionUpdateDataQueue.ResizeArray(m_Capacity);
                m_QueryProbePosition.ResizeArray(m_Capacity);

                DisposeGraphicsBuffers();

                m_TransformUpdateIndexQueueBuffer = new ComputeBuffer(m_Capacity, 4, ComputeBufferType.Raw);
                m_TransformUpdateDataQueueBuffer = new ComputeBuffer(m_Capacity * 2, System.Runtime.InteropServices.Marshal.SizeOf<TransformUpdatePacket>(), ComputeBufferType.Structured);
                m_ProbeUpdateIndexQueueBuffer = new ComputeBuffer(m_Capacity, 4, ComputeBufferType.Raw);
                m_ProbeUpdateDataQueueBuffer = new ComputeBuffer(m_Capacity, System.Runtime.InteropServices.Marshal.SizeOf<SHUpdatePacket>(), ComputeBufferType.Structured);
                m_ProbeOcclusionUpdateDataQueueBuffer = new ComputeBuffer(m_Capacity, System.Runtime.InteropServices.Marshal.SizeOf<Vector4>(), ComputeBufferType.Structured);
                if (m_EnableBoundingSpheres)
                    m_BoundingSpheresUpdateDataQueueBuffer = new ComputeBuffer(m_Capacity, System.Runtime.InteropServices.Marshal.SizeOf<float4>(), ComputeBufferType.Structured);
            }

            m_DrawData.GrowBuffers(newLength);
        }

        public void AllocateTransformObjects(NativeArray<InstanceHandle> newInstances, NativeArray<TransformIndex> newTransformIndices)
        {
            Assert.AreEqual(newInstances.Length, newTransformIndices.Length);
            Assert.IsTrue(m_Length + newInstances.Length <= m_Capacity);

            new AllocateTransformObjectsJob
            {
                newInstances = newInstances,
                newTransformIndices = newTransformIndices,
                indices = m_Indices,
                length = m_Length
            }.Run();

            m_Length += newInstances.Length;
        }

        public void UpdateTransformObjects(NativeArray<InstanceHandle> instances, NativeArray<TransformIndex> transformIndices, in GPUDrivenRendererData rendererData, NativeParallelHashMap<int, InstanceHandle> lodGroupDataMap)
        {
            new UpdateTransformObjectsJob
            {
                lodGroupDataMap = lodGroupDataMap,
                instances = instances,
                transformIndices = transformIndices,
                rendererData = rendererData,
                length = m_Length,
                updaterFlags = m_UpdaterFlags,
                tetrahedronCache = m_TetrahedronCache,
                localBounds = m_DrawData.localBounds,
                lodIndicesAndMasks = m_DrawData.lodIndicesAndMasks,
                flags = m_DrawData.flags,
                layers = m_DrawData.layers
            }.Run();
        }

        public void DestroyTransformObjects(GPURendererInstancePool.GPURendererInstanceDataArrays instanceData, NativeArray<InstanceHandle> instances)
        {
            var lengthArray = new NativeArray<int>(1, Allocator.TempJob);
            lengthArray[0] = m_Length;

            new DestroyTransformObjectsJob
            {
                instances = instances,
                instanceData = instanceData,
                length = lengthArray,
                indices = m_Indices,
                updaterFlags = m_UpdaterFlags,
                tetrahedronCache = m_TetrahedronCache,
                movedInCurrentFrame = m_MovedInCurrentFrame,
                movedInPreviousFrame = m_MovedInPreviousFrame
            }.Run();

            m_Length = lengthArray[0];
            lengthArray.Dispose();
        }

        private void UpdateTransformsInternal(bool reinitialize, NativeArray<InstanceHandle> instances, NativeArray<TransformIndex> transformIndices,
            NativeArray<Matrix4x4> localToWorldMatrices, NativeArray<Matrix4x4> prevLocalToWorldMatrices,
            in RenderersParameters renderersParameters, GraphicsBuffer outputBuffer)
        {
            Assert.AreEqual(instances.Length, localToWorldMatrices.Length);
            Assert.AreEqual(instances.Length, prevLocalToWorldMatrices.Length);

            var transformQueueCounter = new NativeArray<int>(k_IntsPerCacheLine, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var motionQueueCounter = new NativeArray<int>(k_IntsPerCacheLine, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var probesQueueCounter = new NativeArray<int>(k_IntsPerCacheLine, Allocator.TempJob, NativeArrayOptions.ClearMemory);

            if (instances.Length > 0)
            {
                var updateTransformsBoundsProbesJob = new UpdateTransformsBoundsProbesJob()
                {
                    reinitialize = reinitialize,
                    instances = instances,
                    transformIndices = transformIndices,
                    localToWorldMatrices = localToWorldMatrices,
                    prevLocalToWorldMatrices = prevLocalToWorldMatrices,
                    movedInCurrentFrame = m_MovedInCurrentFrame,
                    localAABB = m_DrawData.localBounds,
                    enableBoundingSpheres = m_EnableBoundingSpheres,
                    updaterFlags = m_UpdaterFlags,
                    tetrahedronCache = m_TetrahedronCache,
                    compactTetrahedronCache = m_CompactTetrahedronCache,
                    transformQueueCounter = transformQueueCounter,
                    probesQueueCounter = probesQueueCounter,
                    transformUpdateIndexStateQueue = m_TransformUpdateIndexQueue,
                    transformUpdateDataQueue = m_TransformUpdateDataQueue,
                    boundingSpheresDataQueue = m_BoundingSpheresUpdateDataQueue,
                    probeUpdateIndexQueue = m_ProbeUpdateIndexQueue,
                    probeQueryPosition = m_QueryProbePosition,
                    boundsToUpdate = m_DrawData.worldBounds,
                    matricesToUpdate = m_DrawData.localToWorldMatrices,
                    isFlippedToUpdate = m_DrawData.localToWorldIsFlipped,
                };

                updateTransformsBoundsProbesJob.Schedule(instances.Length, UpdateTransformsBoundsProbesJob.k_BatchSize).Complete();
            }

            int transformQueueCount = transformQueueCounter[0];
            int probesQueueCount = probesQueueCounter[0];

            JobHandle motionJobHandle = default;
            JobHandle scatterProbesUpdateHandle = default;

            if (!reinitialize)
            {
                var motionJobData = new MotionUpdateJob()
                {
                    movedInCurrentFrame = m_MovedInCurrentFrame,
                    movedInPreviousFrame = m_MovedInPreviousFrame,
                    queueWriteBase = transformQueueCounter[0],
                    inputIndices = m_Indices,
                    updateQueueCounter = motionQueueCounter,
                    transformUpdateIndexStateQueue = m_TransformUpdateIndexQueue,
                };

                motionJobHandle = motionJobData.Schedule(m_MovedInCurrentFrame.ChunkCount(), MotionUpdateJob.k_BatchSize);
            }

            if (probesQueueCount > 0)
                scatterProbesUpdateHandle = InterpolateProbesAndUpdateTetrahedronCache(probesQueueCount, transformIndices);

            motionJobHandle.Complete();

            int motionQueueCount = motionQueueCounter[0];

            if (transformQueueCount > 0 || motionQueueCount > 0)
            {
                DispatchTransformUpdateCommand(reinitialize, transformQueueCount, motionQueueCount, renderersParameters, m_TransformUpdateIndexQueueBuffer, m_TransformUpdateDataQueueBuffer,
                    m_TransformUpdateIndexQueue, m_BoundingSpheresUpdateDataQueueBuffer, m_TransformUpdateDataQueue, m_BoundingSpheresUpdateDataQueue, outputBuffer);
            }

            if (probesQueueCount > 0)
            {
                scatterProbesUpdateHandle.Complete();

                DispatchProbeUpdateCommand(probesQueueCount, renderersParameters, m_ProbeUpdateIndexQueueBuffer, m_ProbeUpdateDataQueueBuffer,
                    m_ProbeOcclusionUpdateDataQueueBuffer, outputBuffer, m_ProbeUpdateIndexQueue, m_ProbeUpdateDataQueue, m_ProbeOcclusionUpdateDataQueue);
            }

            transformQueueCounter.Dispose();
            motionQueueCounter.Dispose();
            probesQueueCounter.Dispose();
        }

        public void ReinitializeTransforms(NativeArray<InstanceHandle> instances, NativeArray<TransformIndex> transformIndices, NativeArray<Matrix4x4> localToWorldMatrices,
            NativeArray<Matrix4x4> prevLocalToWorldMatrices, in RenderersParameters renderersParameters, GraphicsBuffer outputBuffer)
        {
            if (instances.Length == 0)
                return;

            UpdateTransformsInternal(true, instances, transformIndices, localToWorldMatrices, prevLocalToWorldMatrices, renderersParameters, outputBuffer);
        }

        public void UpdateTransforms(NativeArray<InstanceHandle> instances, NativeArray<TransformIndex> transformIndices, NativeArray<Matrix4x4> localToWorldMatrices,
            in RenderersParameters renderersParameters, GraphicsBuffer outputBuffer)
        {
            UpdateTransformsInternal(false, instances, transformIndices, localToWorldMatrices, localToWorldMatrices, renderersParameters, outputBuffer);
        }

        public void UpdateAllProbes(in RenderersParameters renderersParameters, NativeArray<TransformIndex> transformIndices, GraphicsBuffer outputBuffer)
        {
            if (m_Length == 0)
                return;

            var probesQueueCounter = new NativeArray<int>(k_IntsPerCacheLine, Allocator.TempJob, NativeArrayOptions.ClearMemory);

            var jobData = new ProbesUpdateJob()
            {
                inputIndices = m_Indices,
                transformIndices = transformIndices,
                worldAABBs = m_DrawData.worldBounds,
                tetrahedronCache = m_TetrahedronCache,
                compactTetrahedronCache = m_CompactTetrahedronCache,
                updaterFlags = m_UpdaterFlags,
                probesQueueCounter = probesQueueCounter,
                probeUpdateIndexQueue = m_ProbeUpdateIndexQueue,
                probeQueryPosition = m_QueryProbePosition
            };

            jobData.Schedule(m_Length, ProbesUpdateJob.k_BatchSize).Complete();

            int probesQueueCount = probesQueueCounter[0];

            if (probesQueueCount > 0)
            {
                InterpolateProbesAndUpdateTetrahedronCache(probesQueueCount, transformIndices).Complete();

                DispatchProbeUpdateCommand(probesQueueCount, renderersParameters, m_ProbeUpdateIndexQueueBuffer, m_ProbeUpdateDataQueueBuffer,
                    m_ProbeOcclusionUpdateDataQueueBuffer, outputBuffer, m_ProbeUpdateIndexQueue, m_ProbeUpdateDataQueue, m_ProbeOcclusionUpdateDataQueue);
            }

            probesQueueCounter.Dispose();
        }

        private JobHandle InterpolateProbesAndUpdateTetrahedronCache(int queueCount, NativeArray<TransformIndex> transformIndices)
        {
            var lightProbesQuery = new LightProbesQuery(Allocator.TempJob);
            var calculateProbesJob = new CalculateInterpolatedLightAndOcclusionProbesBatchJob()
            {
                lightProbesQuery = lightProbesQuery,
                probesCount = queueCount,
                queryPostitions = m_QueryProbePosition,
                compactTetrahedronCache = m_CompactTetrahedronCache,
                probesSphericalHarmonics = m_ProbeUpdateDataQueue,
                probesOcclusion = m_ProbeOcclusionUpdateDataQueue
            };

            var totalBatchCount = 1 + (queueCount / CalculateInterpolatedLightAndOcclusionProbesBatchJob.k_CalculatedProbesPerBatch);

            var calculateProbesJobHandle = calculateProbesJob.Schedule(totalBatchCount, CalculateInterpolatedLightAndOcclusionProbesBatchJob.k_BatchSize);

            lightProbesQuery.Dispose(calculateProbesJobHandle);

            var scatterTetrahedronCacheIndicesJob = new ScatterTetrahedronCacheIndicesJob()
            {
                transformIndices = transformIndices,
                compactTetrahedronCache = m_CompactTetrahedronCache,
                probeIndices = m_ProbeUpdateIndexQueue,
                tetrahedronCache = m_TetrahedronCache
            };
            return scatterTetrahedronCacheIndicesJob.Schedule(queueCount, ScatterTetrahedronCacheIndicesJob.k_BatchSize, calculateProbesJobHandle);
        }
    }
}
