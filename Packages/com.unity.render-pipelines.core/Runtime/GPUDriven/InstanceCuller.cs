using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
#if UNITY_EDITOR
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering
{
    internal struct RangeKey : IEquatable<RangeKey>
    {
        public byte layer;
        public uint renderingLayerMask;
        public MotionVectorGenerationMode motionMode;
        public ShadowCastingMode shadowCastingMode;
        public bool staticShadowCaster;
        public int rendererPriority;

        public bool Equals(RangeKey other)
        {
            return
                layer == other.layer &&
                renderingLayerMask == other.renderingLayerMask &&
                motionMode == other.motionMode &&
                shadowCastingMode == other.shadowCastingMode &&
                staticShadowCaster == other.staticShadowCaster &&
                rendererPriority == other.rendererPriority;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 23) + layer;
            hash = (hash * 23) + (int)renderingLayerMask;
            hash = (hash * 23) + (int)motionMode;
            hash = (hash * 23) + (int)shadowCastingMode;
            hash = (hash * 23) + (staticShadowCaster ? 1 : 0);
            hash = (hash * 23) + rendererPriority;
            return hash;
        }
    }

    internal struct DrawRange
    {
        public RangeKey key;
        public int drawCount;
        public int drawOffset;
    }

    internal struct DrawKey : IEquatable<DrawKey>
    {
        public BatchMeshID meshID;
        public int submeshIndex;
        public BatchMaterialID materialID;
        public BatchDrawCommandFlags flags;
        public int transparentInstanceId; // non-zero for transparent instances, to ensure each instance has its own draw command (for sorting)
        public int overridenComponents;
        public RangeKey range;

        public bool Equals(DrawKey other)
        {
            return
                meshID == other.meshID &&
                submeshIndex == other.submeshIndex &&
                materialID == other.materialID &&
                flags == other.flags &&
                transparentInstanceId == other.transparentInstanceId &&
                overridenComponents == other.overridenComponents &&
                range.Equals(other.range);
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 23) + (int)meshID.value;
            hash = (hash * 23) + (int)submeshIndex;
            hash = (hash * 23) + (int)materialID.value;
            hash = (hash * 23) + (int)flags;
            hash = (hash * 23) + transparentInstanceId;
            hash = (hash * 23) + range.GetHashCode();
            hash = (hash * 23) + (int)overridenComponents;
            return hash;
        }
    }

    internal struct DrawBatch
    {
        public DrawKey key;
        public int instanceCount;
        public int instanceOffset;
        public MeshProceduralInfo procInfo;
    }

    internal struct DrawInstance
    {
        public DrawKey key;
        public int instanceIndex;
    }

    internal struct BinningConfig
    {
        public int viewCount;
        public bool supportsCrossFade;
        public bool supportsMotionCheck;

        public int visibilityConfigCount
        {
            get
            {
                // always bin based on flip winding state (the initial 1 bit)
                int bitCount = 1 + viewCount + (supportsCrossFade ? 1 : 0) + (supportsMotionCheck ? 1 : 0);
                return 1 << bitCount;
            }
        }
    }

    [BurstCompile]
    internal struct CullingJob : IJobParallelFor
    {
        public const int k_BatchSize = 32;

        enum CrossFadeType
        {
            kDisabled,
            kCrossFadeOut, // 1 == instance is visible in current lod, and not next - could be fading out
            kCrossFadeIn, // 2 == instance is visivle in next lod level, but not current - could be fading in
            kVisible // 3 == instance is visible in both current and next lod level - could not be impacted by fade
        }

        [ReadOnly] public BinningConfig binningConfig;
        [ReadOnly] public NativeArray<TransformIndex> transformIndices;
        [ReadOnly] public ParallelBitArray movedTransformIndices;

        [ReadOnly] public BatchCullingViewType viewType;
        [ReadOnly] public float3 cameraPosition;
        [ReadOnly] public float sqrScreenRelativeMetric;
        [ReadOnly] public bool isOrtho;
        [ReadOnly] public bool cullLightmappedShadowCasters;
        [ReadOnly] public int maxLOD;
        [ReadOnly] public uint cullingLayerMask;

        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<FrustumPlaneCuller.PlanePacket4> frustumPlanePackets;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<FrustumPlaneCuller.SplitInfo> frustumSplitInfos;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Plane> lightFacingFrustumPlanes;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ReceiverSphereCuller.SplitInfo> receiverSplitInfos;
        public float3x3 worldToLightSpaceRotation;

        [ReadOnly] public InstanceDrawData instanceDrawData;
        [NativeDisableContainerSafetyRestriction] [ReadOnly] public NativeList<LODGroupCullingData> lodGroupCullingData;
        [NativeDisableUnsafePtrRestriction] [ReadOnly] public IntPtr occlusionBuffer;

        [WriteOnly] public NativeArray<byte> rendererVisibilityMasks;
        [WriteOnly] public NativeArray<byte> rendererCrossFadeValues;


        // float [-1.0f... 1.0f] -> uint [0...254]
        static uint PackFloatToUint8(float percent)
        {
            uint packed = (uint)((1.0f + percent) * 127.0f + 0.5f);
            // ensure negative values remain negative
            if (percent < 0.0f)
                packed = math.min(packed, 126);
            return packed;
        }

        unsafe float CalculateLODVisibility(int i)
        {
            var lodDataIndexAndMask = instanceDrawData.lodIndicesAndMasks[i];
            var lodPercent = 1.0f;
            if (lodDataIndexAndMask != 0xFFFFFFFF)
            {
                var lodIndex = lodDataIndexAndMask >> 8;
                var lodMask = lodDataIndexAndMask & 0xFF;

                Assert.IsTrue(lodMask > 0);

                var maxLodMask = 1 << maxLOD;

                ref var lodGroup = ref lodGroupCullingData.ElementAt((int)lodIndex);
                float sqrDistanceToLODCenter = isOrtho ? sqrScreenRelativeMetric : LODGroupRenderingUtils.CalculateSqrPerspectiveDistance(lodGroup.worldSpaceReferencePoint, cameraPosition, sqrScreenRelativeMetric);

                // Max lod exceeded
                if(lodMask < maxLodMask)
                    return 0.0f;

                bool isMaxLOD = lodMask == maxLodMask;

                lodPercent = 0.0f;

                // Offset to the lod preceding the first for proper cross fade calculation.
                int m = math.max(math.tzcnt(lodMask) - 1, 0);
                lodMask >>= m;

                while (lodMask > 0)
                {
                    var type = (CrossFadeType)(lodMask & 3);
                    var sqrMaxDist = lodGroup.sqrDistances[m];
                    // if current instance is either not present in this current lod, or that the distance is further away, check next level
                    if (type == CrossFadeType.kDisabled || sqrDistanceToLODCenter >= sqrMaxDist)
                    {
                        ++m;
                        lodMask >>= 1;
                        continue;
                    }

                    var minDist = (m == 0 || isMaxLOD) ? 0.0f : lodGroup.sqrDistances[m - 1];

                    // we're testing lod ranges further than current distance. stop.
                    if (sqrDistanceToLODCenter < minDist)
                    {
                        ++m;
                        lodMask >>= 1;
                        continue;
                    }

                    if (type == CrossFadeType.kVisible) // Between min and max
                    {
                        lodPercent = 1.0f;
                        break;
                    }

                    var transitionDist = lodGroup.transitionDistances[m];
                    var distanceToLodCenter = math.sqrt(sqrDistanceToLODCenter);
                    var maxDist = Mathf.Sqrt(sqrMaxDist);
                    var dif = maxDist - distanceToLodCenter;
                    if (dif < transitionDist)
                    {
                        lodPercent = dif / transitionDist;
                        if (type == CrossFadeType.kCrossFadeIn)
                        {
                            lodPercent = -lodPercent;
                        }
                    }
                    else if (type == CrossFadeType.kCrossFadeOut) // not at transition distance, yet - fully visible
                    {
                        lodPercent = 1.0f;
                        break;
                    }

                    ++m;
                    lodMask >>= 1;
                }
            }
            return lodPercent;
        }

        private uint CalculateVisibilityMask(int i)
        {
            var instanceFlags = instanceDrawData.flags[i];
            if ((instanceFlags & InstanceFlags.HasBounds) == 0)
                return 0;

            if (cullingLayerMask == 0)
                return 0;

            if ((cullingLayerMask & (1 << (int)instanceDrawData.layers[i])) == 0)
                return 0;

            if (cullLightmappedShadowCasters && (instanceFlags & InstanceFlags.AffectsLightmaps) != 0)
                return 0;

            // cull early for camera and shadow views based on the shadow culling mode
            if (viewType == BatchCullingViewType.Camera && (instanceFlags & InstanceFlags.IsShadowsOnly) != 0)
                return 0;
            if (viewType == BatchCullingViewType.Light && (instanceFlags & InstanceFlags.IsShadowsOff) != 0)
                return 0;

            AABB instanceBounds = instanceDrawData.worldBounds[i];
            uint visibilityMask = FrustumPlaneCuller.ComputeSplitVisibilityMask(frustumPlanePackets, frustumSplitInfos, instanceBounds);

            if (visibilityMask != 0 && receiverSplitInfos.Length > 0)
                visibilityMask &= ReceiverSphereCuller.ComputeSplitVisibilityMask(lightFacingFrustumPlanes, receiverSplitInfos, worldToLightSpaceRotation, instanceBounds);

            // Perform an occlusion test on the instance bounds if we have an occlusion buffer available and the instance is still visible
            if (visibilityMask != 0 && occlusionBuffer != IntPtr.Zero)
                visibilityMask = BatchRendererGroup.OcclusionTestAABB(occlusionBuffer, instanceBounds.ToBounds()) ? visibilityMask : 0;

            return visibilityMask;
        }


        public void Execute(int index)
        {
            var visibilityMask = CalculateVisibilityMask(index);

            float lodPercent = 0.0f;
            if (visibilityMask != 0)
            {
                lodPercent = CalculateLODVisibility(index);
                if (lodPercent == 0.0f)
                    visibilityMask = 0;
            }

            var crossFadeValue = 0U;
            bool hasMotion = false;
            bool hasCrossFade = false;
            if (visibilityMask != 0)
            {
                if (binningConfig.supportsMotionCheck)
                {
                    var transformIndex = transformIndices[index];
                    hasMotion = movedTransformIndices.Get(transformIndex.index);

                    visibilityMask = (visibilityMask << 1) | (hasMotion ? 1U : 0);
                }

                if (binningConfig.supportsCrossFade)
                {
                    crossFadeValue = PackFloatToUint8(lodPercent);
                    hasCrossFade = (crossFadeValue < 254);

                    visibilityMask = (visibilityMask << 1) | (hasCrossFade ? 1U : 0);
                }
            }

            rendererVisibilityMasks[index] = (byte)visibilityMask;
            if (hasCrossFade)
                rendererCrossFadeValues[index] = (byte)crossFadeValue;
        }
    }

    [BurstCompile]
    internal unsafe struct AllocateBinsPerBatch : IJobParallelFor
    {
        [ReadOnly] public BinningConfig binningConfig;

        [ReadOnly] public NativeList<DrawBatch> drawBatches;
        [ReadOnly] public NativeArray<int> drawInstanceIndices;
        [ReadOnly] public InstanceDrawData instanceDrawData;

        [ReadOnly] public NativeArray<byte> rendererVisibilityMasks;

        [NativeDisableContainerSafetyRestriction] [WriteOnly] public NativeArray<int> batchBinAllocOffsets;
        [NativeDisableContainerSafetyRestriction] [WriteOnly] public NativeArray<int> batchBinCounts;

        [NativeDisableContainerSafetyRestriction] [DeallocateOnJobCompletion] public NativeArray<int> binAllocCounter;
        [NativeDisableContainerSafetyRestriction] [WriteOnly] public NativeArray<short> binConfigIndices;
        [NativeDisableContainerSafetyRestriction] [WriteOnly] public NativeArray<int> binVisibleInstanceCounts;

        unsafe public void Execute(int batchIndex)
        {
            // figure out how many combinations of views/features we need to partition by
            int configCount = binningConfig.visibilityConfigCount;

            // allocate space to keep track of the number of instances per config
            var visibleCountPerConfig = stackalloc int[configCount];
            for (int i = 0; i < configCount; ++i)
                visibleCountPerConfig[i] = 0;

            // and space to keep track of which configs have any instances
            int configMaskCount = (configCount + 63)/64;
            var configUsedMasks = stackalloc UInt64[configMaskCount];
            for (int i = 0; i < configMaskCount; ++i)
                configUsedMasks[i] = 0;

            // loop over all instances within this batch
            var drawBatch = drawBatches[batchIndex];
            var instanceCount = drawBatch.instanceCount;
            var instanceOffset = drawBatch.instanceOffset;
            for (int i = 0; i < instanceCount; ++i)
            {
                var rendererIndex = drawInstanceIndices[instanceOffset + i];

                bool isFlipped = instanceDrawData.localToWorldIsFlipped.Get(rendererIndex);
                int visiblityMask = (int)rendererVisibilityMasks[rendererIndex];
                if (visiblityMask == 0)
                    continue;

                int configIndex = (int)(visiblityMask << 1) | (isFlipped ? 1 : 0);
                Assert.IsTrue(configIndex < configCount);
                visibleCountPerConfig[configIndex]++;
                configUsedMasks[configIndex >> 6] |= 1ul << (configIndex & 0x3f);
            }

            // allocate and store the non-empty configs as bins
            int binCount = 0;
            for (int i = 0; i < configMaskCount; ++i)
                binCount += math.countbits(configUsedMasks[i]);

            int allocOffsetStart = 0;
            if (binCount > 0)
            {
                int *allocCounter = (int *)binAllocCounter.GetUnsafePtr<int>();
                int allocOffsetEnd = Interlocked.Add(ref UnsafeUtility.AsRef<int>(allocCounter), binCount);
                allocOffsetStart = allocOffsetEnd - binCount;

                int allocOffset = allocOffsetStart;
                for (int i = 0; i < configMaskCount; ++i)
                {
                    UInt64 configRemainMask = configUsedMasks[i];
                    while (configRemainMask != 0)
                    {
                        var bitPos = math.tzcnt(configRemainMask);
                        configRemainMask ^= 1ul << bitPos;

                        int configIndex = 64*i + bitPos;
                        int visibleCount = visibleCountPerConfig[configIndex];
                        Assert.IsTrue(visibleCount > 0);

                        binConfigIndices[allocOffset] = (short)configIndex;
                        binVisibleInstanceCounts[allocOffset] = visibleCount;
                        allocOffset++;
                    }
                }
                Assert.IsTrue(allocOffset == allocOffsetEnd);
            }
            batchBinAllocOffsets[batchIndex] = allocOffsetStart;
            batchBinCounts[batchIndex] = binCount;
        }
    }

    [BurstCompile]
    internal unsafe struct PrefixSumDrawsAndInstances : IJob
    {
        [ReadOnly] public NativeList<DrawRange> drawRanges;
        [ReadOnly] public NativeArray<int> drawBatchIndices;

        [ReadOnly] public NativeArray<int> batchBinAllocOffsets;
        [ReadOnly] public NativeArray<int> batchBinCounts;
        [ReadOnly] public NativeArray<int> binVisibleInstanceCounts;

        [NativeDisableContainerSafetyRestriction] [WriteOnly] public NativeArray<int> batchDrawCommandOffsets;
        [NativeDisableContainerSafetyRestriction] [WriteOnly] public NativeArray<int> binVisibleInstanceOffsets;

        [NativeDisableUnsafePtrRestriction] public NativeArray<BatchCullingOutputDrawCommands> cullingOutput;

        unsafe public void Execute()
        {
            BatchCullingOutputDrawCommands output = cullingOutput[0];

            int outRangeIndex = 0;
            int outCommandIndex = 0;
            int outVisibleInstanceIndex = 0;

            for (int rangeIndex = 0; rangeIndex < drawRanges.Length; ++rangeIndex)
            {
                int rangeDrawCommandCount = 0;
                int rangeDrawCommandOffset = outCommandIndex;

                var drawRangeInfo = drawRanges[rangeIndex];
                for (int drawIndexInRange = 0; drawIndexInRange < drawRangeInfo.drawCount; ++drawIndexInRange)
                {
                    var batchIndex = drawBatchIndices[drawRangeInfo.drawOffset + drawIndexInRange];
                    batchDrawCommandOffsets[batchIndex] = outCommandIndex;

                    var binAllocOffset = batchBinAllocOffsets[batchIndex];
                    var binCount = batchBinCounts[batchIndex];

                    for (int binIndexInBatch = 0; binIndexInBatch < binCount; ++binIndexInBatch)
                    {
                        var binIndex = binAllocOffset + binIndexInBatch;
                        binVisibleInstanceOffsets[binIndex] = outVisibleInstanceIndex;

                        outVisibleInstanceIndex += binVisibleInstanceCounts[binIndex];
                    }

                    outCommandIndex += binCount;
                    rangeDrawCommandCount += binCount;
                }

                if (rangeDrawCommandCount != 0)
                {
#if DEBUG
                    if (outRangeIndex >= output.drawRangeCount)
                        throw new Exception("Exceeding draw range count");
#endif

                    var rangeKey = drawRangeInfo.key;
                    output.drawRanges[outRangeIndex] = new BatchDrawRange
                    {
                        drawCommandsBegin = (uint)rangeDrawCommandOffset,
                        drawCommandsCount = (uint)rangeDrawCommandCount,
                        filterSettings = new BatchFilterSettings
                        {
                            renderingLayerMask = rangeKey.renderingLayerMask,
                            rendererPriority = rangeKey.rendererPriority,
                            layer = rangeKey.layer,
                            motionMode = rangeKey.motionMode,
                            shadowCastingMode = rangeKey.shadowCastingMode,
                            receiveShadows = true,
                            staticShadowCaster = rangeKey.staticShadowCaster,
                            allDepthSorted = false
                        }
                    };
                    outRangeIndex++;
                }
            }

            output.drawRangeCount = outRangeIndex; // trim to the number of written ranges
            output.drawCommandCount = outCommandIndex;
            output.drawCommands = MemoryUtilities.Malloc<BatchDrawCommand>(outCommandIndex, Allocator.TempJob);
            output.instanceSortingPositions = MemoryUtilities.Malloc<float>(3 * outCommandIndex, Allocator.TempJob);
            output.visibleInstanceCount = outVisibleInstanceIndex;
            output.visibleInstances = MemoryUtilities.Malloc<int>(outVisibleInstanceIndex, Allocator.TempJob);
            cullingOutput[0] = output;
        }
    }

    [BurstCompile]
    internal unsafe struct DrawCommandOutputPerBatch : IJobParallelFor
    {
        [ReadOnly] public BinningConfig binningConfig;
        [ReadOnly] public NativeParallelHashMap<int, BatchID> batchIDs;

        [ReadOnly] public NativeList<DrawBatch> drawBatches;
        [ReadOnly] public NativeArray<int> drawInstanceIndices;
        [ReadOnly] public InstanceDrawData instanceDrawData;

        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<byte> rendererVisibilityMasks;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<byte> rendererCrossFadeValues;

        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> batchBinAllocOffsets;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> batchBinCounts;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> batchDrawCommandOffsets;

        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<short> binConfigIndices;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> binVisibleInstanceOffsets;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> binVisibleInstanceCounts;

        [ReadOnly] public NativeArray<BatchCullingOutputDrawCommands> cullingOutput;

        unsafe int EncodeCrossFadeIntoRendererIndex(int rendererIndex, int visibilityMask)
        {
            if (binningConfig.supportsCrossFade)
            {
                // always in the low bit
                bool hasCrossFade = ((visibilityMask & 1) != 0);
                if (hasCrossFade)
                {
                    int crossFadeValue = (int)rendererCrossFadeValues[rendererIndex];
                    rendererIndex |= (crossFadeValue - 127) << 24;
                }
            }
            return rendererIndex;
        }

        unsafe public void Execute(int batchIndex)
        {
            DrawBatch drawBatch = drawBatches[batchIndex];

            var binCount = batchBinCounts[batchIndex];
            if (binCount == 0)
                return;

            BatchCullingOutputDrawCommands output = cullingOutput[0];

            // figure out how many combinations of views/features we need to partition by
            int configCount = binningConfig.visibilityConfigCount;

            // allocate storage for the instance offsets, set to zero
            var instanceOffsetPerConfig = stackalloc int[configCount];
            for (int i = 0; i < configCount; ++i)
                instanceOffsetPerConfig[i] = 0;

            // write the draw commands, scatter the allocated offsets to our storage
            // TODO: fast path when binCount == 1
            var batchBinAllocOffset = batchBinAllocOffsets[batchIndex];
            var batchDrawCommandOffset = batchDrawCommandOffsets[batchIndex];
            var lastBinInstanceOffset = 0;
            bool rangeSupportsMotion = (drawBatch.key.range.motionMode == MotionVectorGenerationMode.Object ||
                drawBatch.key.range.motionMode == MotionVectorGenerationMode.ForceNoMotion);
            for (int binIndexInBatch = 0; binIndexInBatch < binCount; ++binIndexInBatch)
            {
                var binIndex = batchBinAllocOffset + binIndexInBatch;
                var visibleInstanceOffset = binVisibleInstanceOffsets[binIndex];
                var visibleInstanceCount = binVisibleInstanceCounts[binIndex];
                lastBinInstanceOffset = visibleInstanceOffset;

                // scatter to local storage for the per-instance loop below
                var configIndex = binConfigIndices[binIndex];
                instanceOffsetPerConfig[configIndex] = visibleInstanceOffset;

                // get the write index for the draw command
                var drawCommandOffset = batchDrawCommandOffset + binIndexInBatch;

                var drawFlags = drawBatch.key.flags;
                bool isFlipped = ((configIndex & 1) != 0);
                if (isFlipped)
                    drawFlags |= BatchDrawCommandFlags.FlipWinding;

                int visibilityMask = configIndex >> 1;
                if (binningConfig.supportsCrossFade)
                {
                    if ((visibilityMask & 1) != 0)
                        drawFlags |= BatchDrawCommandFlags.LODCrossFade;
                    visibilityMask >>= 1;
                }
                if (binningConfig.supportsMotionCheck)
                {
                    if ((visibilityMask & 1) != 0 && rangeSupportsMotion)
                        drawFlags |= BatchDrawCommandFlags.HasMotion;
                    visibilityMask >>= 1;
                }
                Assert.IsTrue(visibilityMask != 0);

                var sortingPosition = 0;
                if ((drawFlags & BatchDrawCommandFlags.HasSortingPosition) != 0)
                    sortingPosition = 3 * drawCommandOffset;

#if DEBUG
                if (drawCommandOffset >= output.drawCommandCount)
                    throw new Exception("Exceeding draw command count");

                if (!batchIDs.ContainsKey(drawBatch.key.overridenComponents))
                    throw new Exception("Draw command created with an invalid BatchID");
#endif
                output.drawCommands[drawCommandOffset] = new BatchDrawCommand
                {
                    flags = drawFlags,
                    visibleOffset = (uint)visibleInstanceOffset,
                    visibleCount = (uint)visibleInstanceCount,
                    batchID = batchIDs[(int)drawBatch.key.overridenComponents],
                    materialID = drawBatch.key.materialID,
                    splitVisibilityMask = (ushort)visibilityMask,
                    sortingPosition = sortingPosition,
                    meshID = drawBatch.key.meshID,
                    submeshIndex = (ushort)drawBatch.key.submeshIndex,
                };
            }

            // write the visible instances
            var instanceOffset = drawBatch.instanceOffset;
            var instanceCount = drawBatch.instanceCount;
            var lastRendererIndex = 0;
            if (binCount > 1)
            {
                for (int i = 0; i < instanceCount; ++i)
                {
                    var rendererIndex = drawInstanceIndices[instanceOffset + i];

                    bool isFlipped = instanceDrawData.localToWorldIsFlipped.Get(rendererIndex);
                    int visibilityMask = (int)rendererVisibilityMasks[rendererIndex];
                    if (visibilityMask == 0)
                        continue;

                    rendererIndex = EncodeCrossFadeIntoRendererIndex(rendererIndex, visibilityMask);
                    lastRendererIndex = rendererIndex;

                    // add to the instance list for this bin
                    int configIndex = (int)(visibilityMask << 1) | (isFlipped ? 1 : 0);
                    Assert.IsTrue(configIndex < binningConfig.visibilityConfigCount);
                    var visibleInstanceOffset = instanceOffsetPerConfig[configIndex];
                    instanceOffsetPerConfig[configIndex]++;

#if DEBUG
                    if (visibleInstanceOffset >= output.visibleInstanceCount)
                        throw new Exception("Exceeding visible instance count");
#endif
                    output.visibleInstances[visibleInstanceOffset] = rendererIndex;
                }
            }
            else
            {
                int visibleInstanceOffset = lastBinInstanceOffset;
                for (int i = 0; i < instanceCount; ++i)
                {
                    var rendererIndex = drawInstanceIndices[instanceOffset + i];
                    int visibilityMask = (int)rendererVisibilityMasks[rendererIndex];
                    if (visibilityMask == 0)
                        continue;

                    rendererIndex = EncodeCrossFadeIntoRendererIndex(rendererIndex, visibilityMask);
                    lastRendererIndex = rendererIndex;

                    // only one bin for this batch
                    output.visibleInstances[visibleInstanceOffset] = rendererIndex;
                    visibleInstanceOffset++;
                }
            }

            // use the first instance position of each batch as the sorting position if necessary
            if ((drawBatch.key.flags & BatchDrawCommandFlags.HasSortingPosition) != 0)
            {
                AABB bounds = instanceDrawData.worldBounds[lastRendererIndex & 0xffffff];
                float3 position = bounds.center;

                int sortingPosition = 3 * batchDrawCommandOffset;
                output.instanceSortingPositions[sortingPosition + 0] = position.x;
                output.instanceSortingPositions[sortingPosition + 1] = position.y;
                output.instanceSortingPositions[sortingPosition + 2] = position.z;
            }
        }
    }

#if UNITY_EDITOR
    internal enum FilteringJobMode
    {
        Filtering,
        PickingSelection
    }

    [BurstCompile]
    internal unsafe struct DrawCommandOutputFiltering : IJob
    {
        [ReadOnly] public NativeParallelHashMap<int, BatchID> batchIDs;
        [ReadOnly] public int viewID;

        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<byte> rendererVisibilityMasks;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<byte> rendererCrossFadeValues;

        [ReadOnly] public NativeArray<int> drawInstanceIndices;
        [ReadOnly] public NativeList<DrawBatch> drawBatches;
        [ReadOnly] public NativeList<DrawRange> drawRanges;
        [ReadOnly] public NativeArray<int> drawBatchIndices;
        [ReadOnly] public InstanceDrawData instanceDrawData;

        [ReadOnly] public NativeArray<bool> filteringResults;
        [ReadOnly] public NativeArray<int> excludedRenderers;
        [ReadOnly] public NativeArray<int> rendererInstanceIDs;

        [ReadOnly]
        public FilteringJobMode mode;

        [NativeDisableUnsafePtrRestriction] public NativeArray<BatchCullingOutputDrawCommands> cullingOutput;

#if DEBUG
        [IgnoreWarning(1370)] //Ignore throwing exception warning.
#endif
        public void Execute()
        {
            BatchCullingOutputDrawCommands output = cullingOutput[0];

            int maxVisibleInstanceCount = 0;
            for (int i = 0; i < drawInstanceIndices.Length; ++i)
            {
                var rendererIndex = drawInstanceIndices[i];
                if (rendererVisibilityMasks[rendererIndex] != 0)
                    ++maxVisibleInstanceCount;
            }
            output.visibleInstanceCount = maxVisibleInstanceCount;
            output.visibleInstances = MemoryUtilities.Malloc<int>(output.visibleInstanceCount, Allocator.TempJob);

            output.drawCommandCount = output.visibleInstanceCount; // for picking/filtering, 1 draw command per instance!
            output.drawCommands = MemoryUtilities.Malloc<BatchDrawCommand>(output.drawCommandCount, Allocator.TempJob);
            if(mode == FilteringJobMode.PickingSelection)
                output.drawCommandPickingInstanceIDs = MemoryUtilities.Malloc<int>(output.drawCommandCount, Allocator.TempJob);

            int outRangeIndex = 0;
            int outCommandIndex = 0;
            int outVisibleInstanceIndex = 0;

            for (int rangeIndex = 0; rangeIndex < drawRanges.Length; ++rangeIndex)
            {
                int rangeDrawCommandOffset = outCommandIndex;

                var drawRangeInfo = drawRanges[rangeIndex];
                for (int drawIndexInRange = 0; drawIndexInRange < drawRangeInfo.drawCount; ++drawIndexInRange)
                {
                    var batchIndex = drawBatchIndices[drawRangeInfo.drawOffset + drawIndexInRange];
                    DrawBatch drawBatch = drawBatches[batchIndex];
                    var instanceOffset = drawBatch.instanceOffset;
                    var instanceCount = drawBatch.instanceCount;

                    // Output visible instances to the array
                    for (int i = 0; i < instanceCount; ++i)
                    {
                        var rendererIndex = drawInstanceIndices[instanceOffset + i];
                        int visibilityMask = (int)rendererVisibilityMasks[rendererIndex];
                        if (visibilityMask == 0)
                            continue;

                        if (mode == FilteringJobMode.Filtering && filteringResults.IsCreated && !filteringResults[rendererIndex])
                            continue;

                        var rendererID = rendererInstanceIDs[rendererIndex];
                        if (mode == FilteringJobMode.PickingSelection && excludedRenderers.IsCreated && excludedRenderers.Contains(rendererID))
                            continue;

#if DEBUG
                        if (outVisibleInstanceIndex >= output.visibleInstanceCount)
                            throw new Exception("Exceeding visible instance count");

                        if (outCommandIndex >= output.drawCommandCount)
                            throw new Exception("Exceeding draw command count");

                        if (!batchIDs.ContainsKey(drawBatch.key.overridenComponents))
                            throw new Exception("Draw command created with an invalid BatchID");
#endif
                        output.visibleInstances[outVisibleInstanceIndex] = rendererIndex;
                        output.drawCommands[outCommandIndex] = new BatchDrawCommand
                        {
                            flags = BatchDrawCommandFlags.None,
                            visibleOffset = (uint)outVisibleInstanceIndex,
                            visibleCount = (uint)1,
                            batchID = batchIDs[drawBatch.key.overridenComponents],
                            materialID = drawBatch.key.materialID,
                            splitVisibilityMask = 0x1,
                            sortingPosition = 0,
                            meshID = drawBatch.key.meshID,
                            submeshIndex = (ushort)drawBatch.key.submeshIndex,
                        };
                        if(mode == FilteringJobMode.PickingSelection)
                            output.drawCommandPickingInstanceIDs[outCommandIndex] = rendererID;

                        outVisibleInstanceIndex++;
                        outCommandIndex++;
                    }
                }

                // Emit a DrawRange to the array if we have any visible DrawCommands
                var rangeDrawCommandCount = outCommandIndex - rangeDrawCommandOffset;
                if (rangeDrawCommandCount > 0)
                {
#if DEBUG
                    if (outRangeIndex >= output.drawRangeCount)
                        throw new Exception("Exceeding draw range count");
#endif

                    var rangeKey = drawRangeInfo.key;
                    output.drawRanges[outRangeIndex] = new BatchDrawRange
                    {
                        drawCommandsBegin = (uint)rangeDrawCommandOffset,
                        drawCommandsCount = (uint)rangeDrawCommandCount,
                        filterSettings = new BatchFilterSettings
                        {
                            renderingLayerMask = rangeKey.renderingLayerMask,
                            rendererPriority = rangeKey.rendererPriority,
                            layer = rangeKey.layer,
                            motionMode = rangeKey.motionMode,
                            shadowCastingMode = rangeKey.shadowCastingMode,
                            receiveShadows = true,
                            staticShadowCaster = rangeKey.staticShadowCaster,
                            allDepthSorted = false
                        }
                    };
                    outRangeIndex++;
                }
            }

            // trim to the number of written ranges/commands/instances
            output.drawRangeCount = outRangeIndex;
            output.drawCommandCount = outCommandIndex;
            output.visibleInstanceCount = outVisibleInstanceIndex;
            cullingOutput[0] = output;
        }
    }
#endif

    [BurstCompile]
    internal struct InstanceCuller : IDisposable
    {
        private JobHandle CreateFrustumCullingJob(
            BatchCullingContext cc,
            in InstanceDrawData instanceDrawData,
            NativeList<LODGroupCullingData> lodGroupCullingData,
            in BinningConfig binningConfig,
            in NativeArray<TransformIndex> transformIndices,
            in ParallelBitArray movedTransformIndices,
            out NativeArray<byte> rendererVisibilityMasks,
            out NativeArray<byte> rendererCrossFadeValues)
        {
            Assert.IsTrue(cc.cullingSplits.Length <= 6, "InstanceCullingBatcher supports up to 6 culling splits.");

            var receiverPlanes = ReceiverPlanes.Create(cc, Allocator.Temp);
            var receiverSphereCuller = ReceiverSphereCuller.Create(cc, Allocator.TempJob);
            var frustumPlaneCuller = FrustumPlaneCuller.Create(cc, receiverPlanes.planes.AsArray(), receiverSphereCuller, Allocator.TempJob);
            var lightFacingFrustumPlanes = receiverPlanes.CopyLightFacingFrustumPlanes(Allocator.TempJob);
            receiverPlanes.planes.Dispose();

            var visibilityLength = instanceDrawData.length;
            rendererVisibilityMasks = new NativeArray<byte>(visibilityLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            rendererCrossFadeValues = new NativeArray<byte>(visibilityLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            float screenRelativeMetric = LODGroupRenderingUtils.CalculateScreenRelativeMetric(cc.lodParameters);

            var cullingJob = new CullingJob
            {
                binningConfig = binningConfig,
                transformIndices = transformIndices,
                movedTransformIndices = movedTransformIndices,
                viewType = cc.viewType,
                frustumPlanePackets = frustumPlaneCuller.planePackets,
                frustumSplitInfos = frustumPlaneCuller.splitInfos,
                lightFacingFrustumPlanes = lightFacingFrustumPlanes,
                receiverSplitInfos = receiverSphereCuller.splitInfos,
                worldToLightSpaceRotation = receiverSphereCuller.worldToLightSpaceRotation,
                cullLightmappedShadowCasters = (cc.cullingFlags & BatchCullingFlags.CullLightmappedShadowCasters) != 0,
                cameraPosition = cc.lodParameters.cameraPosition,
                sqrScreenRelativeMetric = screenRelativeMetric * screenRelativeMetric,
                isOrtho = cc.lodParameters.isOrthographic,
                instanceDrawData = instanceDrawData,
                lodGroupCullingData = lodGroupCullingData,
                occlusionBuffer = cc.occlusionBuffer,
                rendererVisibilityMasks = rendererVisibilityMasks,
                rendererCrossFadeValues = rendererCrossFadeValues,
                maxLOD = QualitySettings.maximumLODLevel,
                cullingLayerMask = cc.cullingLayerMask,
            };

            return cullingJob.Schedule(visibilityLength, CullingJob.k_BatchSize);
        }

        private int ComputeWorstCaseDrawCommandCount(
            BatchCullingContext cc,
            BinningConfig binningConfig,
            CPUInstanceData instanceData,
            int crossFadedRendererCount)
        {
            int visibleInstancesCount = instanceData.drawInstances.Length;
            int drawCommandCount = instanceData.drawBatches.Length;

            // add the number of batches split due to actively cross-fading
            drawCommandCount += math.min(crossFadedRendererCount, drawCommandCount);

            // batches can be split due to flip winding
            drawCommandCount *= 2;

            // and actively moving
            if (binningConfig.supportsMotionCheck)
                drawCommandCount *= 2;

            if (cc.cullingSplits.Length > 1)
            {
                // visible instances are only written once, grouped by visibility mask bit pattern
                // draw calls are split for each unique visibility mask bit pattern
                // handle the worst case where each draw has an instance for every possible mask
                drawCommandCount <<= (cc.cullingSplits.Length - 1);
            }

            // empty draw commands are skipped, so there cannot be more draw commands than visible instances
            drawCommandCount = math.min(drawCommandCount, visibleInstancesCount);

            return drawCommandCount;
        }

        public JobHandle CreateCullJobTree(
            BatchCullingContext cc,
            BatchCullingOutput cullingOutput,
            in InstanceDrawData instanceDrawData,
            NativeList<LODGroupCullingData> lodGroupCullingData,
            CPUInstanceData instanceData,
            NativeParallelHashMap<int, BatchID> batchIDs,
            int crossFadedRendererCount,
            NativeArray<int> rendererInstanceIDs,
            in NativeArray<TransformIndex> transformIndices,
            in ParallelBitArray movedTransformIndices)
        {
            var binningConfig = new BinningConfig
            {
                viewCount = cc.cullingSplits.Length,
                supportsCrossFade = (crossFadedRendererCount > 0),
                supportsMotionCheck = (cc.viewType == BatchCullingViewType.Camera), // TODO: could disable here if RP never needs object motion vectors, for now always batch on it
            };

            var cullingJobHandle = CreateFrustumCullingJob(
                cc,
                instanceDrawData,
                lodGroupCullingData,
                binningConfig,
                transformIndices,
                movedTransformIndices,
                out var rendererVisibilityMasks,
                out var rendererCrossFadeValues);

            // allocate for worst case number of draw ranges (all other arrays allocated after size is known)
            var drawCommands = new BatchCullingOutputDrawCommands();
            drawCommands.drawRangeCount = instanceData.drawRanges.Length;
            unsafe
            {
                drawCommands.drawRanges = MemoryUtilities.Malloc<BatchDrawRange>(drawCommands.drawRangeCount, Allocator.TempJob);
            }
            cullingOutput.drawCommands[0] = drawCommands;

#if UNITY_EDITOR
            if (cc.viewType == BatchCullingViewType.Picking || cc.viewType == BatchCullingViewType.SelectionOutline)
            {
                var pickingIDs = UnityEditor.HandleUtility.GetPickingIncludeExcludeList(Allocator.TempJob);
                var excludedRenderers = pickingIDs.ExcludeRenderers.IsCreated ? pickingIDs.ExcludeRenderers : new NativeArray<int>(0, Allocator.TempJob);
                var dummyFilteringResults = new NativeArray<bool>(0, Allocator.TempJob);
                var drawOutputJob = new DrawCommandOutputFiltering
                {
                    viewID = cc.viewID.GetInstanceID(),
                    batchIDs = batchIDs,
                    rendererVisibilityMasks = rendererVisibilityMasks,
                    rendererCrossFadeValues = rendererCrossFadeValues,
                    drawInstanceIndices = instanceData.drawInstanceIndices,
                    drawBatches = instanceData.drawBatches,
                    drawRanges = instanceData.drawRanges,
                    drawBatchIndices = instanceData.drawBatchIndices,
                    instanceDrawData = instanceDrawData,
                    rendererInstanceIDs = rendererInstanceIDs,
                    filteringResults = dummyFilteringResults,
                    excludedRenderers = excludedRenderers,
                    cullingOutput = cullingOutput.drawCommands,
                    mode = FilteringJobMode.PickingSelection

                };
                var drawOutputHandle = drawOutputJob.Schedule(cullingJobHandle);
                drawOutputHandle.Complete();
                dummyFilteringResults.Dispose();
                if (!pickingIDs.ExcludeRenderers.IsCreated)
                    excludedRenderers.Dispose();
                pickingIDs.Dispose();
                return drawOutputHandle;
            }
            else if(cc.viewType == BatchCullingViewType.Filtering)
            {
                NativeArray<bool> filteredRenderers = new NativeArray<bool>(rendererInstanceIDs.Length, Allocator.TempJob);
                EditorCameraUtils.GetRenderersFilteringResults(rendererInstanceIDs, filteredRenderers);
                var dummyExcludedRenderers = new NativeArray<int>(0, Allocator.TempJob);
                var drawOutputJob = new DrawCommandOutputFiltering()
                {
                    viewID = cc.viewID.GetInstanceID(),
                    batchIDs = batchIDs,
                    rendererVisibilityMasks = rendererVisibilityMasks,
                    rendererCrossFadeValues = rendererCrossFadeValues,
                    drawInstanceIndices = instanceData.drawInstanceIndices,
                    drawBatches = instanceData.drawBatches,
                    drawRanges = instanceData.drawRanges,
                    drawBatchIndices = instanceData.drawBatchIndices,
                    instanceDrawData = instanceDrawData,
                    rendererInstanceIDs = rendererInstanceIDs,
                    filteringResults = filteredRenderers,
                    excludedRenderers = dummyExcludedRenderers,
                    cullingOutput = cullingOutput.drawCommands,
                    mode = FilteringJobMode.Filtering
                };

                var drawOutputHandle = drawOutputJob.Schedule(cullingJobHandle);
                drawOutputHandle.Complete();
                filteredRenderers.Dispose();
                dummyExcludedRenderers.Dispose();

                return drawOutputHandle;
            }
            else
#endif
            {
                var batchCount = instanceData.drawBatches.Length;
                int maxBinCount = ComputeWorstCaseDrawCommandCount(cc, binningConfig, instanceData, crossFadedRendererCount);

                var batchBinAllocOffsets = new NativeArray<int>(batchCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var batchBinCounts = new NativeArray<int>(batchCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var batchDrawCommandOffsets = new NativeArray<int>(batchCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                var binAllocCounter = new NativeArray<int>(JobsUtility.CacheLineSize / sizeof(int), Allocator.TempJob);
                var binConfigIndices = new NativeArray<short>(maxBinCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var binVisibleInstanceCounts = new NativeArray<int>(maxBinCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var binVisibleInstanceOffsets = new NativeArray<int>(maxBinCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                var allocateBinsJob = new AllocateBinsPerBatch
                {
                    binningConfig = binningConfig,
                    drawBatches = instanceData.drawBatches,
                    drawInstanceIndices = instanceData.drawInstanceIndices,
                    instanceDrawData = instanceDrawData,
                    rendererVisibilityMasks = rendererVisibilityMasks,
                    batchBinAllocOffsets = batchBinAllocOffsets,
                    batchBinCounts = batchBinCounts,
                    binAllocCounter = binAllocCounter,
                    binConfigIndices = binConfigIndices,
                    binVisibleInstanceCounts = binVisibleInstanceCounts,
                };
                var allocateBinsHandle = allocateBinsJob.Schedule(batchCount, 1, cullingJobHandle);

                var prefixSumJob = new PrefixSumDrawsAndInstances
                {
                    drawRanges = instanceData.drawRanges,
                    drawBatchIndices = instanceData.drawBatchIndices,
                    batchBinAllocOffsets = batchBinAllocOffsets,
                    batchBinCounts = batchBinCounts,
                    binVisibleInstanceCounts = binVisibleInstanceCounts,
                    batchDrawCommandOffsets = batchDrawCommandOffsets,
                    binVisibleInstanceOffsets = binVisibleInstanceOffsets,
                    cullingOutput = cullingOutput.drawCommands,
                };
                var prefixSumHandle = prefixSumJob.Schedule(allocateBinsHandle);

                var drawCommandOutputJob = new DrawCommandOutputPerBatch
                {
                    binningConfig = binningConfig,
                    batchIDs = batchIDs,
                    drawBatches = instanceData.drawBatches,
                    drawInstanceIndices = instanceData.drawInstanceIndices,
                    instanceDrawData = instanceDrawData,
                    rendererVisibilityMasks = rendererVisibilityMasks,
                    rendererCrossFadeValues = rendererCrossFadeValues,
                    batchBinAllocOffsets = batchBinAllocOffsets,
                    batchBinCounts = batchBinCounts,
                    batchDrawCommandOffsets = batchDrawCommandOffsets,
                    binConfigIndices = binConfigIndices,
                    binVisibleInstanceOffsets = binVisibleInstanceOffsets,
                    binVisibleInstanceCounts = binVisibleInstanceCounts,
                    cullingOutput = cullingOutput.drawCommands,
                };
                return drawCommandOutputJob.Schedule(batchCount, 1, prefixSumHandle);
            }
        }

        public void Dispose()
        {
        }
    }
}
