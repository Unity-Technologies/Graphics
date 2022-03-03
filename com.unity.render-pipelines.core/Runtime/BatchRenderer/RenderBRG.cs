#define DEBUG_LOG_SCENE
//#define DEBUG_LOG_CULLING
//#define DEBUG_LOG_CULLING_SPLITS
//#define DEBUG_LOG_CULLING_RESULTS_SLOW
//#define DEBUG_LOG_CULLING_PLANES
//#define DEBUG_LOG_RECEIVER_PLANES

using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering
{

    [Flags]
    public enum RangeShadowFlags
    {
        None = 0,
        ReceiveShadows = 1,
        StaticShadowCaster = 2,
    }

    public struct RangeKey : IEquatable<RangeKey>
    {
        public byte layer;
        public uint renderingLayerMask;
        public ShadowCastingMode shadowCastingMode;
        public RangeShadowFlags shadowFlags;

        public bool Equals(RangeKey other)
        {
            return
                layer == other.layer &&
                renderingLayerMask == other.renderingLayerMask &&
                shadowCastingMode == other.shadowCastingMode &&
                shadowFlags == other.shadowFlags;
        }
    }

    public struct DrawRange
    {
        public RangeKey key;
        public int drawCount;
        public int drawOffset;
    }

    public struct DrawKey : IEquatable<DrawKey>
    {
        public BatchMeshID meshID;
        public uint submeshIndex;
        public BatchMaterialID material;
        public BatchDrawCommandFlags flags;

        public RangeKey range;

        public bool Equals(DrawKey other)
        {
            return
                meshID == other.meshID &&
                submeshIndex == other.submeshIndex &&
                material == other.material &&
                flags == other.flags &&
                range.Equals(other.range);
        }
    }

    public struct DrawBatch
    {
        public DrawKey key;
        public int instanceCount;
        public int instanceOffset;
    }

    public struct DrawInstance
    {
        public DrawKey key;
        public int instanceIndex;
    }

    struct SHProperties
    {
        public float4 SHAr;
        public float4 SHAg;
        public float4 SHAb;
        public float4 SHBr;
        public float4 SHBg;
        public float4 SHBb;
        public float4 SHC;

        public SHProperties(SphericalHarmonicsL2 sh)
        {
            SHAr = GetSHA(sh, 0);
            SHAg = GetSHA(sh, 1);
            SHAb = GetSHA(sh, 2);

            SHBr = GetSHB(sh, 0);
            SHBg = GetSHB(sh, 1);
            SHBb = GetSHB(sh, 2);

            SHC = GetSHC(sh);
        }

        static float4 GetSHA(SphericalHarmonicsL2 sh, int i)
        {
            return new float4(sh[i, 3], sh[i, 1], sh[i, 2], sh[i, 0] - sh[i, 6]);
        }

        static float4 GetSHB(SphericalHarmonicsL2 sh, int i)
        {
            return new float4(sh[i, 4], sh[i, 5], sh[i, 6] * 3f, sh[i, 7]);
        }

        static float4 GetSHC(SphericalHarmonicsL2 sh)
        {
            return new float4(sh[0, 8], sh[1, 8], sh[2, 8], 1);
        }
    }

    internal struct BRGInstanceBufferOffsets
    {
        public int localToWorld;
        public int worldToLocal;
        public int probeOffsetSHAr;
        public int probeOffsetSHAg;
        public int probeOffsetSHAb;
        public int probeOffsetSHBr;
        public int probeOffsetSHBg;
        public int probeOffsetSHBb;
        public int probeOffsetSHC;
        public int probeOffsetOcclusion;
    }

    unsafe class SceneBRG
    {
        private BatchRendererGroup m_BatchRendererGroup;
        private GraphicsBuffer m_GPUPersistentInstanceData;

        private BatchID m_batchID;
        private bool m_initialized;

        private NativeHashMap<RangeKey, int> m_rangeHash;
        private NativeList<DrawRange> m_drawRanges;

        private NativeHashMap<DrawKey, int> m_batchHash;
        private NativeList<DrawBatch> m_drawBatches;

        private NativeList<DrawInstance> m_instances;
        private NativeArray<int> m_instanceIndices;
        private NativeArray<int> m_drawIndices;
        private BRGInstanceBufferOffsets m_instanceBufferOffsets;

        private LightMaps m_Lightmaps;

        private BRGTransformUpdater m_BRGTransformUpdater = new BRGTransformUpdater();
        private DeferredMaterialBRG m_DeferredMaterialBRG = null;
        GeometryPoolBatchHandle m_DeferredMaterialBatch = GeometryPoolBatchHandle.Invalid;

        private List<MeshRenderer> m_AddedRenderers;

        UploadBufferPool m_visibleInstancesBufferPool;
        int m_frame;

        public static T* Malloc<T>(int count) where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<T>() * count,
                UnsafeUtility.AlignOf<T>(),
                Allocator.TempJob);
        }

        static MetadataValue CreateMetadataValue(int nameID, int gpuAddress, bool isOverridden)
        {
            const uint kIsOverriddenBit = 0x80000000;
            return new MetadataValue
            {
                NameID = nameID,
                Value = (uint)gpuAddress | (isOverridden ? kIsOverriddenBit : 0),
            };
        }

        [BurstCompile]
        private struct CullingJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<FrustumPlanes.PlanePacket4> planes;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<FrustumPlanes.PlanePacket4> receiverPlanes;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> splitCounts;

            [ReadOnly] public BRGDrawData brgDrawData;

            [WriteOnly] public NativeArray<ulong> rendererVisibility;

            public void Execute(int index)
            {
                // Each invocation is culling 8 renderers (8 split bits * 8 renderers = 64 bit bitfield)
                int start = index * 8;
                int end = math.min(start + 8, brgDrawData.length);

                ulong visibleBits = 0;
                for (int i = start; i < end; i++)
                {
                    AABB instanceBounds = brgDrawData.bounds[i];
#if DEBUG_LOG_CULLING_RESULTS_SLOW
                    bool receiverCulled = FrustumPlanes.Intersect2NoPartial(receiverPlanes, instanceBounds) == FrustumPlanes.IntersectResult.Out;
                    {
                        ulong splitMask = FrustumPlanes.Intersect2NoPartialMulti(planes, splitCounts, instanceBounds);
                        if (receiverCulled && splitMask != 0)
                        {
                            splitMask = 0x80UL; // Use bit 8 to mark receiver culling for profiling output (only 6 bits needed for payload)
                        }
                        visibleBits |= splitMask << (8 * (i - start));
                    }
#else
                    if (FrustumPlanes.Intersect2NoPartial(receiverPlanes, instanceBounds) != FrustumPlanes.IntersectResult.Out)
                    {
                        ulong splitMask = FrustumPlanes.Intersect2NoPartialMulti(planes, splitCounts, instanceBounds);
                        visibleBits |= splitMask << (8 * (i - start));  // 8x 8 bit masks per uint64
                    }
#endif
                }

                rendererVisibility[index] = visibleBits;
            }
        }

#if !DEBUG_LOG_CULLING_RESULTS_SLOW
        [BurstCompile]
#endif
        private struct DrawCommandOutputSingleSplitJob : IJob
        {
            public BatchID batchID;
            public int viewID;
            public int sliceID;
            public int maxDrawCounts;
            public int maxVisibleInstances;

            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ulong> rendererVisibility;

            [ReadOnly] public NativeArray<int> instanceIndices;
            [ReadOnly] public NativeList<DrawBatch> drawBatches;
            [ReadOnly] public NativeList<DrawRange> drawRanges;
            [ReadOnly] public NativeArray<int> drawIndices;

            [ReadOnly] public GraphicsBufferHandle visibleInstancesBufferHandle;
            [WriteOnly] public NativeArray<UInt32> visibleInstancesGPU;

            public NativeArray<BatchCullingOutputDrawCommands> drawCommands;

#if DEBUG
            [IgnoreWarning(1370)] //Ignore throwing exception warning.
#endif
            public void Execute()
            {
                var draws = drawCommands[0];

                int outIndex = 0;
                int outBatch = 0;
                int outRange = 0;

                int batchStartIndex = 0;
                int rangeStartIndex = 0;

                var bins = stackalloc uint[64];
                for (int i = 0; i < 64; ++i)
                {
                    bins[i] = 0;
                }

#if DEBUG_LOG_CULLING_RESULTS_SLOW
                uint receiverCulledCount = 0;
#endif
                for (int activeRange = 0; activeRange < drawRanges.Length; ++activeRange)
                {
                    var drawRangeInfo = drawRanges[activeRange];
                    var batchCount = drawRangeInfo.drawCount;
                    for (int activeBatch = 0; activeBatch < batchCount; ++activeBatch)
                    {
                        var remappedDrawIndex = drawIndices[drawRangeInfo.drawOffset + activeBatch];
                        var instanceCount = drawBatches[remappedDrawIndex].instanceCount;
                        var instanceOffset = drawBatches[remappedDrawIndex].instanceOffset;

                        // Output visible instances to the array
                        for (int i = 0; i < instanceCount; ++i)
                        {
                            var rendererIndex = instanceIndices[instanceOffset + i];
                            uint visibleMask = (uint)((rendererVisibility[rendererIndex >> 3] >> ((rendererIndex & 0x7) << 3)) & 0xfful);   // 8x 8 bit masks per uint64

                            if (visibleMask != 0)
                            {
#if DEBUG
                                if (outIndex >= maxVisibleInstances)
                                    throw new Exception("Exceeding visible instance count");
#endif
                                //draws.visibleInstances[outIndex] = rendererIndex;
                                visibleInstancesGPU[outIndex] = (UInt32)rendererIndex;
                                outIndex++;
                            }
                        }

                        // Emit a DrawCommand to the array if we have any visible instances
                        var visibleCount = outIndex - batchStartIndex;
                        if (visibleCount > 0)
                        {
#if DEBUG
                            if (outBatch >= maxDrawCounts)
                                throw new Exception("Exceeding draw count");
#endif

                            draws.drawCommands[outBatch] = new BatchDrawCommand
                            {
                                flags = drawBatches[remappedDrawIndex].key.flags,
                                visibleOffset = (uint)batchStartIndex,
                                visibleCount = (uint)visibleCount,
                                batchID = batchID,
                                materialID = drawBatches[remappedDrawIndex].key.material,
                                splitVisibilityMask = 0x1,
                                sortingPosition = 0,
                                regular = new BatchDrawCommandRegular
                                {
                                    meshID = drawBatches[remappedDrawIndex].key.meshID,
                                    submeshIndex = (ushort)drawBatches[remappedDrawIndex].key.submeshIndex,
                                },
                            };
                            outBatch++;
                        }

                        batchStartIndex = outIndex;
                    }

                    // Emit a DrawRange to the array if we have any visible DrawCommands
                    var visibleDrawCount = outBatch - rangeStartIndex;
                    if (visibleDrawCount > 0)
                    {
                        var rangeKey = drawRangeInfo.key;
                        draws.drawRanges[outRange] = new BatchDrawRange
                        {
                            drawCommandsBegin = (uint)rangeStartIndex,
                            drawCommandsCount = (uint)visibleDrawCount,
                            visibleInstancesBufferHandle = visibleInstancesBufferHandle,
                            filterSettings = new BatchFilterSettings
                            {
                                renderingLayerMask = rangeKey.renderingLayerMask,
                                layer = rangeKey.layer,
                                motionMode = MotionVectorGenerationMode.Camera,
                                shadowCastingMode = rangeKey.shadowCastingMode,
                                receiveShadows = (rangeKey.shadowFlags & RangeShadowFlags.ReceiveShadows) != 0,
                                staticShadowCaster = (rangeKey.shadowFlags & RangeShadowFlags.StaticShadowCaster) != 0,
                                allDepthSorted = false
                            }
                        };
                        outRange++;
                    }

                    rangeStartIndex = outBatch;
                }

                draws.drawCommandCount = outBatch;
                draws.visibleInstanceCount = outIndex;
                draws.drawRangeCount = outRange;
                drawCommands[0] = draws;

#if DEBUG_LOG_CULLING_RESULTS_SLOW
            Debug.Log(
                "[DrawCommandOutputSingleSplitJob] viewID=" + viewID +
                " sliceID=" + sliceID +
                " ranges=" + draws.drawRangeCount +
                " draws=" + draws.drawCommandCount +
                " instances=" + draws.visibleInstanceCount +
                " (receiver culled=" + receiverCulledCount + ")");
#endif
            }
        }

#if !DEBUG_LOG_CULLING_RESULTS_SLOW
        [BurstCompile]
#endif
        private struct DrawCommandOutputMultiSplitJob : IJob
        {
            public BatchID batchID;
            public int viewID;
            public int sliceID;
            public int maxDrawCounts;
            public int maxVisibleInstances;

            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ulong> rendererVisibility;

            [ReadOnly] public NativeArray<int> instanceIndices;
            [ReadOnly] public NativeList<DrawBatch> drawBatches;
            [ReadOnly] public NativeList<DrawRange> drawRanges;
            [ReadOnly] public NativeArray<int> drawIndices;

            [ReadOnly] public GraphicsBufferHandle visibleInstancesBufferHandle;
            [WriteOnly] public NativeArray<UInt32> visibleInstancesGPU;

            public NativeArray<BatchCullingOutputDrawCommands> drawCommands;

#if DEBUG
            [IgnoreWarning(1370)] //Ignore throwing exception warning.
#endif
            public void Execute()
            {
                var draws = drawCommands[0];

                int outIndex = 0;
                int outBatch = 0;
                int outRange = 0;

                int rangeStartIndex = 0;

                var bins = stackalloc uint[64];
                for (int i = 0; i < 64; ++i)
                {
                    bins[i] = 0;
                }

#if DEBUG_LOG_CULLING_RESULTS_SLOW
                uint receiverCulledCount = 0;
#endif
                for (int activeRange = 0; activeRange < drawRanges.Length; ++activeRange)
                {
                    var drawRangeInfo = drawRanges[activeRange];
                    var batchCount = drawRangeInfo.drawCount;
                    for (int activeBatch = 0; activeBatch < batchCount; ++activeBatch)
                    {
                        var remappedDrawIndex = drawIndices[drawRangeInfo.drawOffset + activeBatch];
                        var instanceCount = drawBatches[remappedDrawIndex].instanceCount;
                        var instanceOffset = drawBatches[remappedDrawIndex].instanceOffset;

                        // Count the number of instances with each unique visible mask (6 bits = 64 bins) to allocate contiguous storage for the DrawCommands
                        UInt64 usedBins = 0;
                        int visibleTotal = 0;
                        for (int i = 0; i < instanceCount; ++i)
                        {
                            var rendererIndex = instanceIndices[instanceOffset + i];
                            uint visibleMask = (uint)((rendererVisibility[rendererIndex >> 3] >> ((rendererIndex & 0x7) << 3)) & 0xfful);   // 8x 8 bit masks per uint64
                            if (visibleMask != 0)
                            {
                                usedBins |= 1ul << (int)visibleMask;
                                bins[visibleMask]++;
                                visibleTotal++;
                            }
                        }

                        // Prefix sum the visible mask bins
                        uint sum = 0;
                        UInt64 binsLeft = usedBins;
                        while (binsLeft != 0)
                        {
                            var bitIndex = math.tzcnt(binsLeft);
                            binsLeft ^= 1ul << bitIndex;

                            uint v = bins[bitIndex];
                            bins[bitIndex] = sum;
                            sum += v;
                        }

                        // Output visible instances to the array, starting from their visible mask bin offsets
                        for (int i = 0; i < instanceCount; ++i)
                        {
                            var rendererIndex = instanceIndices[instanceOffset + i];
                            uint visibleMask = (uint)((rendererVisibility[rendererIndex >> 3] >> ((rendererIndex & 0x7) << 3)) & 0xfful);   // 8x 8 bit masks per uint64

#if DEBUG_LOG_CULLING_RESULTS_SLOW
                            // Receiver culling statistics
                            if ((visibleMask & 0x80U) != 0) receiverCulledCount++;
                            visibleMask &= ~0x80U;
#endif
                            if (visibleMask != 0)
                            {
                                var offset = bins[visibleMask]++;
#if DEBUG
                                if ((outIndex + offset) >= maxVisibleInstances)
                                    throw new Exception("Exceeding visible instance count");
#endif
                                //draws.visibleInstances[outIndex + offset] = rendererIndex;
                                visibleInstancesGPU[outIndex + (int)offset] = (UInt32)rendererIndex;
                            }
                        }

                        // Emit a DrawCommand to the array for each visible mask bin that has visible indices
                        uint previousOffset = 0;
                        binsLeft = usedBins;
                        while (binsLeft != 0)
                        {
                            var bitIndex = math.tzcnt(binsLeft);
                            binsLeft ^= 1ul << bitIndex;

                            var currentOffset = bins[bitIndex];
                            var visibleCount = currentOffset - previousOffset;
                            bins[bitIndex] = 0;

                            if (visibleCount > 0)
                            {
#if DEBUG
                            if (outBatch >= maxDrawCounts)
                                throw new Exception("Exceeding draw count");
#endif
                                draws.drawCommands[outBatch] = new BatchDrawCommand
                                {
                                    flags = drawBatches[remappedDrawIndex].key.flags,
                                    visibleOffset = (uint)outIndex + previousOffset,
                                    visibleCount = (uint)visibleCount,
                                    batchID = batchID,
                                    materialID = drawBatches[remappedDrawIndex].key.material,
                                    splitVisibilityMask = (ushort)bitIndex,
                                    sortingPosition = 0,
                                    regular = new BatchDrawCommandRegular
                                    {
                                        meshID = drawBatches[remappedDrawIndex].key.meshID,
                                        submeshIndex = (ushort)drawBatches[remappedDrawIndex].key.submeshIndex,
                                    },
                                };
                                outBatch++;
                            }

                            previousOffset = currentOffset;
                        }

                        outIndex += visibleTotal;
                    }

                    // Emit a DrawRange to the array if we have any visible DrawCommands
                    var visibleDrawCount = outBatch - rangeStartIndex;
                    if (visibleDrawCount > 0)
                    {
                        var rangeKey = drawRangeInfo.key;
                        draws.drawRanges[outRange] = new BatchDrawRange
                        {
                            drawCommandsBegin = (uint)rangeStartIndex,
                            drawCommandsCount = (uint)visibleDrawCount,
                            visibleInstancesBufferHandle = visibleInstancesBufferHandle,
                            filterSettings = new BatchFilterSettings
                            {
                                renderingLayerMask = rangeKey.renderingLayerMask,
                                layer = rangeKey.layer,
                                motionMode = MotionVectorGenerationMode.Camera,
                                shadowCastingMode = rangeKey.shadowCastingMode,
                                receiveShadows = (rangeKey.shadowFlags & RangeShadowFlags.ReceiveShadows) != 0,
                                staticShadowCaster = (rangeKey.shadowFlags & RangeShadowFlags.StaticShadowCaster) != 0,
                                allDepthSorted = false
                            }
                        };
                        outRange++;
                    }

                    rangeStartIndex = outBatch;
                }

                draws.drawCommandCount = outBatch;
                draws.visibleInstanceCount = outIndex;
                draws.drawRangeCount = outRange;
                drawCommands[0] = draws;

#if DEBUG_LOG_CULLING_RESULTS_SLOW
            Debug.Log(
                "[DrawCommandOutputMultiSplitJob] viewID=" + viewID +
                " sliceID=" + sliceID +
                " ranges=" + draws.drawRangeCount +
                " draws=" + draws.drawCommandCount +
                " instances=" + draws.visibleInstanceCount +
                " (receiver culled=" + receiverCulledCount + ")");
#endif
            }
        }

        public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            if (!m_initialized)
            {
                return new JobHandle();
            }

            var cc = cullingContext;

            Assert.IsTrue(cc.cullingSplits.Length <= 6, "RenderBRG supports up to 6 culling splits.");

            
#if DEBUG_LOG_CULLING
        Debug.Log(
            "[OnPerformCulling] viewType=" + cc.viewType +
            " viewID=" + cc.viewID.GetInstanceID() +
            " viewID.slice=" + cc.viewID.GetSliceIndex() +
            " LOD.position=" + cc.lodParameters.cameraPosition +
            " LOD.ortho=" + cc.lodParameters.isOrthographic +
            " LOD.fov=" + cc.lodParameters.fieldOfView +
            " layerMask=" + cc.cullingLayerMask +
            " sceneMask=" + cc.sceneCullingMask +
            " cullingSplits=" + cc.cullingSplits.Length +
            " cullingPlanes=" + cc.cullingPlanes.Length +
            " receiverPlanes (count=" + cc.receiverPlaneCount +
            " offset=" + cc.receiverPlaneOffset + ")");
#endif

            var splitCounts = new NativeArray<int>(cc.cullingSplits.Length, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < splitCounts.Length; ++i)
            {
                var split = cc.cullingSplits[i];
                splitCounts[i] = split.cullingPlaneCount;

#if DEBUG_LOG_CULLING_SPLITS
                Debug.Log(
                    "[OnPerformCulling] split=" + i +
                    " sphere(x=" + split.sphereCenter.x + " y=" + split.sphereCenter.y + " z=" + split.sphereCenter.z + ")" +
                    " sphereRadius=" + split.sphereRadius +
                    " cullingPlaneOffset=" + split.cullingPlaneOffset +
                    " cullingPlaneCount=" + split.cullingPlaneCount +
                    " cascadeBlendCullingFactor=" + split.cascadeBlendCullingFactor +
                    " nearPlane=" + split.nearPlane);
#endif
            }

#if DEBUG_LOG_CULLING_PLANES
            for (int i = 0; i < cc.cullingPlanes.Length; ++i)
            {
                var plane = cc.cullingPlanes[i];
                Debug.Log(
                    "[OnPerformCulling] plane=" + i +
                    " normal(x=" + plane.normal.x + " y=" + plane.normal.y + " z=" + plane.normal.z + ")" +
                    " dist=" + plane.distance);
            }
#endif

            var planes = FrustumPlanes.BuildSOAPlanePacketsMulti(cc.cullingPlanes, splitCounts, Allocator.TempJob);

            var receiverPlanes = new NativeArray<Plane>(cc.receiverPlaneCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var receiverPlaneCount = 0;

            if (cc.viewType == BatchCullingViewType.Light)
            {
                bool isOrthographic = false;
                if (cc.cullingSplits.Length > 0)
                {
                    Matrix4x4 m = cc.cullingSplits[0].cullingMatrix;
                    isOrthographic = m[15] == 1.0f && m[11] == 0.0f && m[7] == 0.0f && m[3] == 0.0f;
                }
                if (isOrthographic)
                {
                    Vector3 direction = cc.localToWorldMatrix.GetColumn(2);
#if DEBUG_LOG_RECEIVER_PLANES
                    Debug.Log("[OnPerformCulling] light direction= (x=" + direction.x + " y=" + direction.y + " z=" + direction.z + ")");
#endif
                    for (int i = 0; i < cc.receiverPlaneCount; ++i)
                    {
                        var plane = cc.cullingPlanes[i + cc.receiverPlaneOffset];
                        var d = Vector3.Dot(plane.normal, direction);

                        if (d < 0.0)
                        {
                            receiverPlanes[receiverPlaneCount++] = plane;

#if DEBUG_LOG_RECEIVER_PLANES
                            Debug.Log(
                                "[OnPerformCulling] back facing receiver plane (direction)=" + i +
                                " normal(x=" + plane.normal.x + " y=" + plane.normal.y + " z=" + plane.normal.z + ")" +
                                " dist=" + plane.distance);
#endif
                        }
                    }
                }
                else
                {
                    var position = cc.localToWorldMatrix.GetPosition();
#if DEBUG_LOG_RECEIVER_PLANES
                    Debug.Log("[OnPerformCulling] light position= (x=" + position.x + " y=" + position.y + " z=" + position.z + ")");
#endif
                    for (int i = 0; i < cc.receiverPlaneCount; ++i)
                    {
                        var plane = cc.cullingPlanes[i + cc.receiverPlaneOffset];

                        if (plane.GetSide(position))
                        {
                            receiverPlanes[receiverPlaneCount++] = plane;

#if DEBUG_LOG_RECEIVER_PLANES
                            Debug.Log(
                                "[OnPerformCulling] back facing receiver plane (point)=" + i +
                                " normal(x=" + plane.normal.x + " y=" + plane.normal.y + " z=" + plane.normal.z + ")" +
                                " dist=" + plane.distance);
#endif
                        }
                    }
                }
            }

            var receiverPlanePackets = FrustumPlanes.BuildSOAPlanePackets(receiverPlanes, 0, receiverPlaneCount, Allocator.TempJob);
            receiverPlanes.Dispose();

            BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();
            drawCommands.drawRanges = Malloc<BatchDrawRange>(m_drawRanges.Length);
            int maxDrawCounts = m_drawBatches.Length * 10 * splitCounts.Length;
            // TODO: Assuming each object straddles the boundary of two splits. Not 100% conservative. Should not overflow in real apps, but we clamp anyways.
            drawCommands.drawCommands = Malloc<BatchDrawCommand>(m_drawBatches.Length * 2 * splitCounts.Length);

            drawCommands.visibleInstances = null;//Malloc<int>(m_instanceIndices.Length);
            var visibleInstancesUploadBuffer = m_visibleInstancesBufferPool.StartBufferWrite();

            // Zero init: Culling job sets the values!
            drawCommands.drawRangeCount = 0;
            drawCommands.drawCommandCount = 0;
            drawCommands.visibleInstanceCount = 0;

            drawCommands.instanceSortingPositions = null;
            drawCommands.instanceSortingPositionFloatCount = 0;

            cullingOutput.drawCommands[0] = drawCommands;

            BRGDrawData drawData = m_BRGTransformUpdater.drawData;
            var visibilityLength = (drawData.length + 7) / 8;
            var rendererVisibility =
                new NativeArray<ulong>(visibilityLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var cullingJob = new CullingJob
            {
                planes = planes,
                receiverPlanes = receiverPlanePackets,
                splitCounts = splitCounts,
                brgDrawData = drawData,
                rendererVisibility = rendererVisibility
            };

            var jobHandleCulling = cullingJob.Schedule(visibilityLength, 8);
            JobHandle jobHandleOutput;

            // Use optimized culling job for single split callbacks
            if (cc.cullingSplits.Length == 1)
            {
                var drawOutputJob = new DrawCommandOutputSingleSplitJob
                {
                    viewID = cc.viewID.GetInstanceID(),
                    sliceID = cc.viewID.GetInstanceID(),
                    batchID = m_batchID,
                    maxDrawCounts = maxDrawCounts,
                    maxVisibleInstances = m_instanceIndices.Length,
                    rendererVisibility = rendererVisibility,
                    instanceIndices = m_instanceIndices,
                    drawBatches = m_drawBatches,
                    drawRanges = m_drawRanges,
                    drawIndices = m_drawIndices,
                    drawCommands = cullingOutput.drawCommands,
                    visibleInstancesBufferHandle = visibleInstancesUploadBuffer.bufferHandle,
                    visibleInstancesGPU = visibleInstancesUploadBuffer.gpuData,
                };
                jobHandleOutput = drawOutputJob.Schedule(jobHandleCulling);
            }
            else
            {
                var drawOutputJob = new DrawCommandOutputMultiSplitJob
                {
                    viewID = cc.viewID.GetInstanceID(),
                    sliceID = cc.viewID.GetInstanceID(),
                    batchID = m_batchID,
                    maxDrawCounts = maxDrawCounts,
                    maxVisibleInstances = m_instanceIndices.Length,
                    rendererVisibility = rendererVisibility,
                    instanceIndices = m_instanceIndices,
                    drawBatches = m_drawBatches,
                    drawRanges = m_drawRanges,
                    drawIndices = m_drawIndices,
                    drawCommands = cullingOutput.drawCommands,
                    visibleInstancesBufferHandle = visibleInstancesUploadBuffer.bufferHandle,
                    visibleInstancesGPU = visibleInstancesUploadBuffer.gpuData,
                };
                jobHandleOutput = drawOutputJob.Schedule(jobHandleCulling);
            }

            // TODO: WAITING FOR THE JOB HERE! THIS IS SLOW! NEED THE MULTITHREADED FENCE VERSION!
            jobHandleOutput.Complete();
            m_visibleInstancesBufferPool.EndBufferWrite(visibleInstancesUploadBuffer);
            //m_visibleInstancesBufferPool.EndBufferWriteAfterJob(visibleInstancesUploadBuffer, jobHandleOutput);

            return jobHandleOutput;
        }

        private bool ProcessUsedMeshAndMaterialDataFromGameObjects(
            RenderPipelineAsset activePipelineAsset,
            RenderBRGGetMaterialRenderInfoCallback onGetMaterialInfoCb,
            int instanceIndex,
            MeshRenderer renderer,
            MeshFilter meshFilter,
            Dictionary<Tuple<Renderer, int>, Material> rendererMaterialInfos,
            int deferredMaterialBufferOffset,
            NativeArray<Vector4> deferredMaterialBuffer,
            ref Mesh outMesh,
            List<int> outSubmeshIndices,
            List<Material> outMaterials)
        {
            outSubmeshIndices.Clear();
            outMaterials.Clear();

            var sharedMaterials = new List<Material>();
            var startSubMesh = renderer.subMeshStartIndex;
            renderer.GetSharedMaterials(sharedMaterials);
            Material overrideMaterial = null;
            int overrideCounts = 0;
            for (int matIndex = 0; matIndex < sharedMaterials.Count; ++matIndex)
            {
                Material matToUse;
                if (!rendererMaterialInfos.TryGetValue(new Tuple<Renderer, int>(renderer, matIndex), out matToUse))
                    matToUse = sharedMaterials[matIndex];

                int targetSubmeshIndex = (int)(startSubMesh + matIndex);
                if (onGetMaterialInfoCb != null)
                {
                    RenderBRGMaterialRenderInfo visMaterialInfo = onGetMaterialInfoCb(new RenderBRGGetMaterialRenderInfoArgs()
                    {
                        pipelineAsset = activePipelineAsset,
                        renderer = renderer,
                        submeshIndex = targetSubmeshIndex,
                        material = matToUse
                    });

                    if (!visMaterialInfo.supportsBRGRendering)
                        return false;

                    if (visMaterialInfo.supportsVisibility && visMaterialInfo.materialOverride != null)
                    {
                        Assert.IsTrue(
                            overrideMaterial == null || overrideMaterial == visMaterialInfo.materialOverride,
                            "RenderBRG only supports one and only 1 override for an entire renderer.");
                        ++overrideCounts;
                        overrideMaterial = visMaterialInfo.materialOverride;
                    }
                }

                outSubmeshIndices.Add(targetSubmeshIndex);
                outMaterials.Add(matToUse);
            }

            outMesh = meshFilter.sharedMesh;

            //Special case, if the renderer qualifies for deferred materials, go for it!
            //TODO: for now we just handle 1 case, if the entire renderer can be deferred material.
            if (overrideMaterial != null && overrideCounts == outMaterials.Count && m_DeferredMaterialBatch.valid)
            {
                GeometryPoolEntryDesc geoPoolEntryDesc = new GeometryPoolEntryDesc()
                {
                    mesh = outMesh,
                    submeshData = outSubmeshIndices.Count != 0u ? new GeometryPoolSubmeshData[outSubmeshIndices.Count] : null
                };

                for (int i = 0; i < outSubmeshIndices.Count; ++i)
                {
                    geoPoolEntryDesc.submeshData[i] = new GeometryPoolSubmeshData()
                    {
                        submeshIndex = outSubmeshIndices[i],
                        material = outMaterials[i]
                    };
                }

                GeometryPoolHandle geoHandle = GeometryPoolHandle.Invalid;
                if (!m_DeferredMaterialBRG.RegisterInstance(m_DeferredMaterialBatch, instanceIndex, geoPoolEntryDesc, out geoHandle))
                {
                    Debug.LogError("Could not register instance into deferred material batch: ." + renderer);
                    return true;
                }

                deferredMaterialBuffer[deferredMaterialBufferOffset + instanceIndex] = new Vector4((float)geoHandle.index, m_DeferredMaterialBatch.index, 0.0f, 0.0f);

                //We succeeded! lets override the mesh / submesh index and material.
                outSubmeshIndices.Clear();
                outMaterials.Clear();
                outMaterials.Add(overrideMaterial);
                outSubmeshIndices.Add(geoHandle.index);
                outMesh = m_DeferredMaterialBRG.globalGeoMesh;
            }

            return true;
        }

        private void SanityCheckDrawInstanceCounts()
        {
#if DEBUG
            int maxVisibleInstances = 0;
            for (int rangeIdx = 0; rangeIdx < m_drawRanges.Length; ++rangeIdx)
            {
                int batchCount = m_drawRanges[rangeIdx].drawCount;
                for (int batchIdx = 0; batchIdx < batchCount; ++batchIdx)
                {
                    maxVisibleInstances += m_drawBatches[m_drawIndices[m_drawRanges[rangeIdx].drawOffset+ batchIdx]].instanceCount;
                }
            }
            Assert.IsTrue(maxVisibleInstances == m_instances.Length);

            int totalInstances = 0;
            for (int drawBatchIdx = 0; drawBatchIdx < m_drawBatches.Length; ++drawBatchIdx)
                totalInstances += m_drawBatches[drawBatchIdx].instanceCount;
            Assert.IsTrue(totalInstances == m_instances.Length);
#endif
        }

        // Start is called before the first frame update
        public void Initialize(List<MeshRenderer> renderers, DeferredMaterialBRG deferredMaterialBRG)
        {
            m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);
            m_BRGTransformUpdater.Initialize();

            m_visibleInstancesBufferPool = new UploadBufferPool(10 * 3, 4096 * 1024);   // HACKS: Max 10 callbacks/frame, 3 frame hard coded reuse. 4MB maximum buffer size (1 million visible indices).
            m_frame = 0;

            // Create a batch...
#if DEBUG_LOG_SCENE
            Debug.Log("Converting " + renderers.Count + " renderers...");
#endif

            int renderersLength = renderers.Count;
            m_batchHash = new NativeHashMap<DrawKey, int>(1024, Allocator.Persistent);
            m_rangeHash = new NativeHashMap<RangeKey, int>(1024, Allocator.Persistent);
            m_drawBatches = new NativeList<DrawBatch>(Allocator.Persistent);
            m_drawRanges = new NativeList<DrawRange>(Allocator.Persistent);
            m_AddedRenderers = new List<MeshRenderer>(renderersLength);
            m_DeferredMaterialBRG = deferredMaterialBRG;

            // Fill the GPU-persistent scene data ComputeBuffer
            int bigDataBufferVector4Count =
                4 /*zero*/
                + 1 /*probes*/
                + 1 /*speccube*/
                + 7 * renderersLength /*per renderer SH*/
                + 1 * renderersLength /*per renderer probe occlusion*/
                + 2 * renderersLength /* per renderer lightmapindex + scale/offset*/
                + renderersLength * 3 * 2 /*per renderer 4x3 matrix+inverse*/
                + 1 * renderersLength; /*per renderer, vec4 with deferredMaterialData*/

            var vectorBuffer = new NativeArray<Vector4>(bigDataBufferVector4Count, Allocator.Temp);

            // First 4xfloat4 of ComputeBuffer needed to be zero filled for default property fall back!
            vectorBuffer[0] = new Vector4(0, 0, 0, 0);
            vectorBuffer[1] = new Vector4(0, 0, 0, 0);
            vectorBuffer[2] = new Vector4(0, 0, 0, 0);
            vectorBuffer[3] = new Vector4(0, 0, 0, 0);
            var startOffset = 4;

            var specCubeOffset = startOffset;
            vectorBuffer[specCubeOffset] = ReflectionProbe.defaultTextureHDRDecodeValues;
            startOffset++;

            var SHArOffset = startOffset;
            var SHAgOffset = SHArOffset + renderersLength;
            var SHAbOffset = SHAgOffset + renderersLength;
            var SHBrOffset = SHAbOffset + renderersLength;
            var SHBgOffset = SHBrOffset + renderersLength;
            var SHBbOffset = SHBgOffset + renderersLength;
            var SHCOffset = SHBbOffset + renderersLength;

            var probeOcclusionOffset = SHCOffset + renderersLength;
            var lightMapIndexOffset = probeOcclusionOffset + renderersLength;
            var lightMapScaleOffset = lightMapIndexOffset + renderersLength;
            var localToWorldOffset = lightMapScaleOffset + renderersLength;
            var worldToLocalOffset = localToWorldOffset + renderersLength * 3;
            var deferredMaterialDataOffset = worldToLocalOffset + renderersLength * 3;

            m_instanceBufferOffsets = new BRGInstanceBufferOffsets()
            {
                localToWorld = localToWorldOffset,
                worldToLocal = worldToLocalOffset,
                probeOffsetSHAr = SHArOffset,
                probeOffsetSHAg = SHAgOffset,
                probeOffsetSHAb = SHAbOffset,
                probeOffsetSHBr = SHBrOffset,
                probeOffsetSHBg = SHBgOffset,
                probeOffsetSHBb = SHBbOffset,
                probeOffsetSHC = SHCOffset,
                probeOffsetOcclusion = probeOcclusionOffset
            };

            m_instances = new NativeList<DrawInstance>(1024, Allocator.Persistent);

            var lightmappingData = LightMaps.GenerateLightMappingData(renderers);

            m_Lightmaps = lightmappingData.lightmaps;
            var rendererMaterialInfos = lightmappingData.rendererToMaterialMap;

            LightProbesQuery lpq = new LightProbesQuery(Allocator.Temp);
            bool useFirstMeshForAll = false;    // Hack to help benchmarking different bottlenecks. TODO: Remove!
            MeshFilter firstMesh = null;
            if (m_DeferredMaterialBRG != null)
            {
                if (!m_DeferredMaterialBRG.CreateBatch(renderers.Count, out m_DeferredMaterialBatch))
                    Debug.LogError("Could not allocate batch for this scene, not enough gpu memory allocated.");
            }

            RenderBRGGetMaterialRenderInfoCallback onGetMaterialInfoCb = RenderBRG.GetActiveMaterialRenderInfoCallback(out RenderPipelineAsset activePipeline);

            for (int i = 0; i < renderers.Count; i++)
            {
                var renderer = renderers[i];

                var meshFilter = renderer.gameObject.GetComponent<MeshFilter>();
                if (!renderer || !meshFilter || !meshFilter.sharedMesh || renderer.enabled == false)
                {
                    continue;
                }

                if (useFirstMeshForAll)
                {
                    if (firstMesh != null) meshFilter = firstMesh;
                    firstMesh = meshFilter;
                }

                Mesh usedMesh = null;
                var usedSubmeshIndices = new List<int>();
                var usedMaterials = new List<Material>();
                if (!ProcessUsedMeshAndMaterialDataFromGameObjects(
                    activePipeline, onGetMaterialInfoCb, i, renderer, meshFilter, rendererMaterialInfos,
                    deferredMaterialDataOffset, vectorBuffer,
                    ref usedMesh, usedSubmeshIndices, usedMaterials))
                    continue;

                m_AddedRenderers.Add(renderer);

                // Disable the existing Unity MeshRenderer to avoid double rendering!
                renderer.forceRenderingOff = true;

                /*  mat4x3 packed like this:
                      p1.x, p1.w, p2.z, p3.y,
                      p1.y, p2.x, p2.w, p3.z,
                      p1.z, p2.y, p3.x, p3.w,
                      0.0,  0.0,  0.0,  1.0
                */

                if (!lightmappingData.lightmapIndexRemap.TryGetValue(renderer.lightmapIndex, out var newLmIndex))
                    newLmIndex = 0;

                vectorBuffer[i + lightMapIndexOffset] = new Vector4(newLmIndex, 0, 0, 0);
                vectorBuffer[i + lightMapScaleOffset] = renderer.lightmapScaleOffset;

                var rendererTransform = renderer.transform;
                var m = rendererTransform.localToWorldMatrix;
                vectorBuffer[i * 3 + 0 + localToWorldOffset] = new Vector4(m.m00, m.m10, m.m20, m.m01);
                vectorBuffer[i * 3 + 1 + localToWorldOffset] = new Vector4(m.m11, m.m21, m.m02, m.m12);
                vectorBuffer[i * 3 + 2 + localToWorldOffset] = new Vector4(m.m22, m.m03, m.m13, m.m23);

                var mi = rendererTransform.worldToLocalMatrix;
                vectorBuffer[i * 3 + 0 + worldToLocalOffset] = new Vector4(mi.m00, mi.m10, mi.m20, mi.m01);
                vectorBuffer[i * 3 + 1 + worldToLocalOffset] = new Vector4(mi.m11, mi.m21, mi.m02, mi.m12);
                vectorBuffer[i * 3 + 2 + worldToLocalOffset] = new Vector4(mi.m22, mi.m03, mi.m13, mi.m23);

                int tetrahedronIdx = -1;
                lpq.CalculateInterpolatedLightAndOcclusionProbe(rendererTransform.position, ref tetrahedronIdx, out var lp,
                    out var probeOcclusion);

                var sh = new SHProperties(lp);
                vectorBuffer[SHArOffset + i] = sh.SHAr;
                vectorBuffer[SHAgOffset + i] = sh.SHAg;
                vectorBuffer[SHAbOffset + i] = sh.SHAb;
                vectorBuffer[SHBrOffset + i] = sh.SHBr;
                vectorBuffer[SHBgOffset + i] = sh.SHBg;
                vectorBuffer[SHBbOffset + i] = sh.SHBb;
                vectorBuffer[SHCOffset + i] = sh.SHC;

                vectorBuffer[probeOcclusionOffset + i] = probeOcclusion;

                m_BRGTransformUpdater.RegisterTransformObject(i, rendererTransform, meshFilter.sharedMesh, renderer.lightProbeUsage == LightProbeUsage.BlendProbes);

                var mesh = m_BatchRendererGroup.RegisterMesh(usedMesh);

                // Different renderer settings? -> new draw range
                var rangeKey = new RangeKey
                {
                    layer = (byte)renderer.gameObject.layer,
                    renderingLayerMask = renderer.renderingLayerMask,
                    shadowCastingMode = renderer.shadowCastingMode,
                    shadowFlags = (renderer.receiveShadows ? RangeShadowFlags.ReceiveShadows : 0) |
                                  (renderer.staticShadowCaster ? RangeShadowFlags.StaticShadowCaster : 0)
                };

                var drawRange = new DrawRange { key = rangeKey, drawCount = 0, drawOffset = 0 };

                int drawRangeIndex;
                if (m_rangeHash.TryGetValue(rangeKey, out drawRangeIndex))
                {
                    drawRange = m_drawRanges[drawRangeIndex];
                }
                else
                {
                    drawRangeIndex = m_drawRanges.Length;
                    m_drawRanges.Add(drawRange);
                    m_rangeHash[rangeKey] = drawRangeIndex;
                }

                // Sub-meshes...
                for (int matIndex = 0; matIndex < usedMaterials.Count; matIndex++)
                {
                    Material matToUse = usedMaterials[matIndex];

                    var material = m_BatchRendererGroup.RegisterMaterial(matToUse);

                    var flags = BatchDrawCommandFlags.None;

                    bool flipWinding = math.determinant(renderer.transform.localToWorldMatrix) < 0.0;

                    if (flipWinding)
                        flags |= BatchDrawCommandFlags.FlipWinding;

                    var key = new DrawKey
                    {
                        material = material,
                        meshID = mesh,
                        submeshIndex = (uint)usedSubmeshIndices[matIndex],
                        flags = flags,
                        range = rangeKey
                    };

                    var drawBatch = new DrawBatch { key = key, instanceCount = 0, instanceOffset = 0 };

                    m_instances.Add(new DrawInstance { key = key, instanceIndex = i });

                    int drawBatchIndex;
                    if (m_batchHash.TryGetValue(key, out drawBatchIndex))
                    {
                        drawBatch = m_drawBatches[drawBatchIndex];
                    }
                    else
                    {
                        drawBatchIndex = m_drawBatches.Length;
                        m_drawBatches.Add(drawBatch);
                        m_batchHash[key] = drawBatchIndex;

                        drawRange.drawCount++;
                        m_drawRanges[drawRangeIndex] = drawRange;
                    }

                    drawBatch.instanceCount++;
                    m_drawBatches[drawBatchIndex] = drawBatch;
                }
            }

            m_GPUPersistentInstanceData =
                new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)bigDataBufferVector4Count * 16 / 4, 4);
            m_GPUPersistentInstanceData.SetData(vectorBuffer);


#if DEBUG_LOG_SCENE
            Debug.Log("DrawRanges: " + m_drawRanges.Length + ", DrawBatches: " + m_drawBatches.Length + ", Instances: " + m_instances.Length);
#endif

            // Prefix sum to calculate draw offsets for each DrawRange
            int prefixSum = 0;
            for (int i = 0; i < m_drawRanges.Length; i++)
            {
                var drawRange = m_drawRanges[i];
                drawRange.drawOffset = prefixSum;
                m_drawRanges[i] = drawRange;
                prefixSum += drawRange.drawCount;
            }

            // Generate draw index ranges for each DrawRange
            m_drawIndices = new NativeArray<int>(m_drawBatches.Length, Allocator.Persistent);
            var m_internalRangeIndex = new NativeArray<int>(m_drawRanges.Length, Allocator.Temp);
            for (int i = 0; i < m_drawBatches.Length; i++)
            {
                var draw = m_drawBatches[i];
                if (m_rangeHash.TryGetValue(draw.key.range, out int drawRangeIndex))
                {
                    var drawRange = m_drawRanges[drawRangeIndex];
                    m_drawIndices[drawRange.drawOffset + m_internalRangeIndex[drawRangeIndex]] = i;
                    m_internalRangeIndex[drawRangeIndex]++;
                }
            }

            m_internalRangeIndex.Dispose();

            // Prefix sum to calculate instance offsets for each DrawCommand
            prefixSum = 0;
            for (int i = 0; i < m_drawBatches.Length; i++)
            {
                // DrawIndices remap to get DrawCommands ordered by DrawRange
                var remappedIndex = m_drawIndices[i];
                var drawBatch = m_drawBatches[remappedIndex];
                drawBatch.instanceOffset = prefixSum;
                m_drawBatches[remappedIndex] = drawBatch;
                prefixSum += drawBatch.instanceCount;
            }

            // Generate instance index ranges for each DrawCommand
            m_instanceIndices = new NativeArray<int>(m_instances.Length, Allocator.Persistent);
            var m_internalDrawIndex = new NativeArray<int>(m_drawBatches.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < m_instances.Length; i++)
            {
                var instance = m_instances[i];
                if (m_batchHash.TryGetValue(instance.key, out int drawBatchIndex))
                {
                    var drawBatch = m_drawBatches[drawBatchIndex];
                    m_instanceIndices[drawBatch.instanceOffset + m_internalDrawIndex[drawBatchIndex]] =
                        instance.instanceIndex;
                    m_internalDrawIndex[drawBatchIndex]++;
                }
            }

            SanityCheckDrawInstanceCounts();

            m_internalDrawIndex.Dispose();

            // Bounds ("infinite")
            Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
            m_BatchRendererGroup.SetGlobalBounds(bounds);

            // Batch metadata buffer...

            // Per instance data
            int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
            int worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
            int colorID = Shader.PropertyToID("_BaseColor");
            int lightmapIndexID = Shader.PropertyToID("unity_LightmapIndex");
            int lightmapSTID = Shader.PropertyToID("unity_LightmapST");

            // Global data (should be moved to C++ side)
            int probesOcclusionID = Shader.PropertyToID("unity_ProbesOcclusion");
            int specCubeID = Shader.PropertyToID("unity_SpecCube0_HDR");
            int SHArID = Shader.PropertyToID("unity_SHAr");
            int SHAgID = Shader.PropertyToID("unity_SHAg");
            int SHAbID = Shader.PropertyToID("unity_SHAb");
            int SHBrID = Shader.PropertyToID("unity_SHBr");
            int SHBgID = Shader.PropertyToID("unity_SHBg");
            int SHBbID = Shader.PropertyToID("unity_SHBb");
            int SHCID = Shader.PropertyToID("unity_SHC");
            int deferredMaterialInstanceDataID = Shader.PropertyToID("_DeferredMaterialInstanceData");

            var batchMetadata = new NativeArray<MetadataValue>(14, Allocator.Temp);
            batchMetadata[0] = CreateMetadataValue(objectToWorldID, localToWorldOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[1] = CreateMetadataValue(worldToObjectID, worldToLocalOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[2] = CreateMetadataValue(lightmapSTID, lightMapScaleOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[3] = CreateMetadataValue(lightmapIndexID, lightMapIndexOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[4] = CreateMetadataValue(probesOcclusionID, probeOcclusionOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[5] = CreateMetadataValue(specCubeID, specCubeOffset * UnsafeUtility.SizeOf<Vector4>(), false);
            batchMetadata[6] = CreateMetadataValue(SHArID, SHArOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[7] = CreateMetadataValue(SHAgID, SHAgOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[8] = CreateMetadataValue(SHAbID, SHAbOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[9] = CreateMetadataValue(SHBrID, SHBrOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[10] = CreateMetadataValue(SHBgID, SHBgOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[11] = CreateMetadataValue(SHBbID, SHBbOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[12] = CreateMetadataValue(SHCID, SHCOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[13] = CreateMetadataValue(deferredMaterialInstanceDataID, deferredMaterialDataOffset * UnsafeUtility.SizeOf<Vector4>(), true);

            // Register batch
            m_batchID = m_BatchRendererGroup.AddBatch(batchMetadata, m_GPUPersistentInstanceData.bufferHandle);

            if (m_DeferredMaterialBatch.valid)
                m_DeferredMaterialBRG.SubmitBatch(m_DeferredMaterialBatch, batchMetadata, m_GPUPersistentInstanceData.bufferHandle);

            m_initialized = true;
        }

        public void Update()
        {
            m_visibleInstancesBufferPool.SetFrame(m_frame);
            m_visibleInstancesBufferPool.SetReuseFrame(m_frame - 3);    // Reuse 3 frames old buffers. TODO: Use the proper API  to know when GPU has stopped using the data!
            m_frame++;
        }

        public void StartTransformsUpdate()
        {
            m_BRGTransformUpdater.StartUpdateJobs();
        }

        public bool EndTransformsUpdate(CommandBuffer cmdBuffer)
        {
            return m_BRGTransformUpdater.EndUpdateJobs(
                cmdBuffer,
                m_instanceBufferOffsets,
                m_GPUPersistentInstanceData);
        }

        public void Destroy()
        {
            if (m_initialized)
            {
                // NOTE: Don't need to remove batch or unregister BatchRendererGroup resources. BRG.Dispose takes care of that.
                m_BatchRendererGroup.Dispose();
                m_GPUPersistentInstanceData.Dispose();
                m_BRGTransformUpdater.Dispose();

                m_visibleInstancesBufferPool.Dispose();

                m_batchHash.Dispose();
                m_rangeHash.Dispose();
                m_drawBatches.Dispose();
                m_drawRanges.Dispose();
                m_instances.Dispose();
                m_instanceIndices.Dispose();
                m_drawIndices.Dispose();
                m_Lightmaps.Destroy();

                foreach (var added in m_AddedRenderers)
                {
                    if (added != null)
                        added.forceRenderingOff = false;
                }

                if (m_DeferredMaterialBatch.valid)
                {
                    m_DeferredMaterialBRG.DestroyBatch(m_DeferredMaterialBatch);
                    m_DeferredMaterialBatch = GeometryPoolBatchHandle.Invalid;
                }
            }
        }
    }

    public struct RenderBRGMaterialRenderInfo
    {
        public bool supportsBRGRendering;
        public bool supportsVisibility;
        public Material materialOverride;
    }

    public struct RenderBRGGetMaterialRenderInfoArgs
    {
        public RenderPipelineAsset pipelineAsset;
        public Renderer renderer;
        public int submeshIndex;
        public Material material;
    }

    public delegate RenderBRGMaterialRenderInfo RenderBRGGetMaterialRenderInfoCallback(RenderBRGGetMaterialRenderInfoArgs arguments);

    public struct RenderBRGBindingData
    {
        public GeometryPool globalGeometryPool;

        public bool valid => globalGeometryPool != null;

        public static RenderBRGBindingData NewDefault()
        {
            return new RenderBRGBindingData()
            {
                globalGeometryPool = null
            };
        }
    }

    public class RenderBRG : MonoBehaviour
    {
        private static Dictionary<Type, RenderBRGGetMaterialRenderInfoCallback> s_SrpMatInfoCallbacks = new();

        public static void RegisterSRPRenderInfoCallback(RenderPipelineAsset pipelineAsset, RenderBRGGetMaterialRenderInfoCallback callbackValue)
        {
            s_SrpMatInfoCallbacks[pipelineAsset.GetType()] = callbackValue;
        }

        internal static RenderBRGGetMaterialRenderInfoCallback GetActiveMaterialRenderInfoCallback(out RenderPipelineAsset activePipeline)
        {
            activePipeline = GraphicsSettings.renderPipelineAsset;
            if (activePipeline == null)
                return null;

            if (s_SrpMatInfoCallbacks.TryGetValue(activePipeline.GetType(), out var outCallback))
                return outCallback;

            return null;
        }

        private static bool s_QueryLoadedScenes = true;
        private Dictionary<Scene, SceneBRG> m_Scenes = new();
        private CommandBuffer m_gpuCmdBuffer;

        public bool EnableDeferredMaterials = false;
        public bool EnableTransformUpdate = true;
        private GeometryPool m_GlobalGeoPool;

        private static uint s_DeferredMaterialBRGRef = 0;
        private static DeferredMaterialBRG s_DeferredMaterialBRG;

        public static RenderBRGBindingData GetRenderBRGMaterialBindingData()
        {
            return new RenderBRGBindingData()
            {
                globalGeometryPool = s_DeferredMaterialBRG == null ? null : s_DeferredMaterialBRG.geometryPool
            };
        }

        private void OnEnable()
        {
            CreateDeferredMaterialBRG();

            m_gpuCmdBuffer = new CommandBuffer();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            List<Scene> toAdd = new List<Scene>();

            //During play mode, if we reload the render pipeline, this will help during restart to reparse any previously loaded scenes.
            if (s_QueryLoadedScenes)
            {
                for (int s = 0; s < SceneManager.sceneCount; ++s)
                {
                    toAdd.Add(SceneManager.GetSceneAt(s));
                }
                s_QueryLoadedScenes = false;
            }

            foreach (var sceneBrg in m_Scenes)
            {
                if (sceneBrg.Value == null)
                    toAdd.Add(sceneBrg.Key);
            }

            foreach (var scene in toAdd)
                OnSceneLoaded(scene, LoadSceneMode.Additive);
        }

        private void OnDisable()
        {
            m_gpuCmdBuffer.Release();
            m_gpuCmdBuffer = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            List<Scene> toNull = new List<Scene>();
            foreach (var scene in m_Scenes)
            {
                scene.Value?.Destroy();
                toNull.Add(scene.Key);
            }

            foreach (var scene in toNull)
                m_Scenes[scene] = null;

            DisposeDeferredMaterialBRG();
        }

        private void CreateDeferredMaterialBRG()
        {
            if (!EnableDeferredMaterials)
                return;
            if (s_DeferredMaterialBRGRef == 0)
                s_DeferredMaterialBRG = new DeferredMaterialBRG();
            ++s_DeferredMaterialBRGRef;
        }

        private void DisposeDeferredMaterialBRG()
        {
            if (s_DeferredMaterialBRG == null)
                return;

            --s_DeferredMaterialBRGRef;

            if (s_DeferredMaterialBRGRef > 0)
                return;

            s_DeferredMaterialBRG.Dispose();
            s_DeferredMaterialBRG = null;
        }

        private static void GetValidChildRenderers(GameObject root, List<MeshRenderer> toAppend)
        {
            if (root == null
                || root.activeInHierarchy == false
                || root.GetComponent<BRGNoConvert>() != null)
                return;

            var mr = root.GetComponent<MeshRenderer>();
            if (mr != null
                && !mr.HasPropertyBlock()) //no support for MPB
                toAppend.Add(mr);

            for (var i = 0; i < root.transform.childCount; ++i)
                GetValidChildRenderers(root.transform.GetChild(i).gameObject, toAppend);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (m_Scenes.TryGetValue(scene, out var existingBRG) && existingBRG != null)
                return;

            var renderers = new List<MeshRenderer>();
            foreach (var go in scene.GetRootGameObjects())
                GetValidChildRenderers(go, renderers);

            if (renderers.Count == 0)
                return;

#if DEBUG_LOG_SCENE
            Debug.Log("Loading scene: " + scene.name);
#endif
            SceneBRG brg = new SceneBRG();
            brg.Initialize(renderers, s_DeferredMaterialBRG);
            m_Scenes[scene] = brg;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            m_Scenes.TryGetValue(scene, out var brg);
            if (brg != null)
            {
                brg.Destroy();
                m_Scenes.Remove(scene);
            }
        }

        private void Update()
        {
            if (s_DeferredMaterialBRG != null)
            {
                s_DeferredMaterialBRG.Update();
            }

            foreach (var sceneBrg in m_Scenes)
            {
                if (sceneBrg.Value == null)
                    continue;

                sceneBrg.Value.Update();
            }

            if (EnableTransformUpdate)
            {
                m_gpuCmdBuffer.Clear();

                foreach (var sceneBrg in m_Scenes)
                {
                    if (sceneBrg.Value == null)
                        continue;

                    sceneBrg.Value.StartTransformsUpdate();
                }

                int gpuCmds = 0;
                foreach (var sceneBrg in m_Scenes)
                {
                    if (sceneBrg.Value == null)
                        continue;

                    if (sceneBrg.Value.EndTransformsUpdate(m_gpuCmdBuffer))
                        ++gpuCmds;
                }

                if (gpuCmds > 0)
                    Graphics.ExecuteCommandBuffer(m_gpuCmdBuffer);
            }
        }

        private void OnDestroy()
        {
            foreach (var scene in m_Scenes)
                scene.Value?.Destroy();

            m_Scenes.Clear();

            DisposeDeferredMaterialBRG();
        }
    }
}
