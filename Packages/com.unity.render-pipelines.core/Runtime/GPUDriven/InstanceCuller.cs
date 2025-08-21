using System;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
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
        public bool supportsIndirect;

        public bool Equals(RangeKey other)
        {
            return
                layer == other.layer &&
                renderingLayerMask == other.renderingLayerMask &&
                motionMode == other.motionMode &&
                shadowCastingMode == other.shadowCastingMode &&
                staticShadowCaster == other.staticShadowCaster &&
                rendererPriority == other.rendererPriority &&
                supportsIndirect == other.supportsIndirect;
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
            hash = (hash * 23) + (supportsIndirect ? 1 : 0);
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
        public int activeMeshLod; // or -1 if this draw is not using mesh LOD
        public BatchMaterialID materialID;
        public BatchDrawCommandFlags flags;
        public int transparentInstanceId; // non-zero for transparent instances, to ensure each instance has its own draw command (for sorting)
        public uint overridenComponents;
        public RangeKey range;
        public int lightmapIndex;

        public bool Equals(DrawKey other)
        {
            return
                meshID == other.meshID &&
                submeshIndex == other.submeshIndex &&
                activeMeshLod == other.activeMeshLod &&
                materialID == other.materialID &&
                flags == other.flags &&
                transparentInstanceId == other.transparentInstanceId &&
                overridenComponents == other.overridenComponents &&
                range.Equals(other.range) &&
                lightmapIndex == other.lightmapIndex;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 23) + (int)meshID.value;
            hash = (hash * 23) + (int)submeshIndex;
            hash = (hash * 23) + (int)activeMeshLod;
            hash = (hash * 23) + (int)materialID.value;
            hash = (hash * 23) + (int)flags;
            hash = (hash * 23) + transparentInstanceId;
            hash = (hash * 23) + range.GetHashCode();
            hash = (hash * 23) + (int)overridenComponents;
            hash = (hash * 23) + lightmapIndex;
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

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct AnimateCrossFadeJob : IJobParallelFor
    {
        public const int k_BatchSize = 512;
        public const byte k_MeshLODTransitionToLowerLODBit = 0x80;

        private const byte k_LODFadeOff = (byte)CullingJob.k_LODFadeOff;
        private const float k_CrossfadeAnimationTimeS = 0.333f;

        [ReadOnly] public float deltaTime;

        public UnsafeList<byte> crossFadeArray;

        public unsafe void Execute(int instanceIndex)
        {
            ref var crossFadeValue = ref crossFadeArray.ElementAt(instanceIndex);

            if(crossFadeValue == k_LODFadeOff)
                return;

            var prevTransitionBit = (crossFadeValue & k_MeshLODTransitionToLowerLODBit);

            crossFadeValue += (byte)((deltaTime / k_CrossfadeAnimationTimeS) * 127.0f);

            //If done with crossfade - reset mask
            if (prevTransitionBit != ((crossFadeValue + 1) & k_MeshLODTransitionToLowerLODBit))
            {
                crossFadeValue = k_LODFadeOff;
            }
        }
    }

    [BurstCompile]
    internal struct CullingJob : IJobParallelFor
    {
        public const int k_BatchSize = 32;

        public const uint k_MeshLodCrossfadeActive = 0x40;
        public const uint k_MeshLodCrossfadeSignBit = 0x80;
        public const uint k_MeshLodCrossfadeBits = k_MeshLodCrossfadeSignBit | k_MeshLodCrossfadeActive;

        public const uint k_LODFadeOff = 255u;
        public const uint k_LODFadeZeroPacked = 127u;
        public const uint k_LODFadeIsSpeedTree = 256u;

        private const uint k_InvalidCrossFadeAndLevel = 0xFFFFFFFF;

        const uint k_VisibilityMaskNotVisible = 0u;

        const float k_SmallMeshTransitionWidth = 0.1f;

        enum CrossFadeType
        {
            kDisabled,
            kCrossFadeOut, // 1 == instance is visible in current lod, and not next - could be fading out
            kCrossFadeIn, // 2 == instance is visible in next lod level, but not current - could be fading in
            kVisible // 3 == instance is visible in both current and next lod level - could not be impacted by fade
        }

        [ReadOnly] public BinningConfig binningConfig;

        [ReadOnly] public BatchCullingViewType viewType;
        [ReadOnly] public float3 cameraPosition;
        [ReadOnly] public float sqrMeshLodSelectionConstant;
        [ReadOnly] public float sqrScreenRelativeMetric;
        [ReadOnly] public float minScreenRelativeHeight;
        [ReadOnly] public bool isOrtho;
        [ReadOnly] public bool cullLightmappedShadowCasters;
        [ReadOnly] public int maxLOD;
        [ReadOnly] public uint cullingLayerMask;
        [ReadOnly] public ulong sceneCullingMask;
        [ReadOnly] public bool animateCrossFades;

        [ReadOnly] public NativeArray<FrustumPlaneCuller.PlanePacket4> frustumPlanePackets;
        [ReadOnly] public NativeArray<FrustumPlaneCuller.SplitInfo> frustumSplitInfos;
        [ReadOnly] public NativeArray<Plane> lightFacingFrustumPlanes;
        [ReadOnly] public NativeArray<ReceiverSphereCuller.SplitInfo> receiverSplitInfos;
        public float3x3 worldToLightSpaceRotation;

        [ReadOnly] public CPUInstanceData.ReadOnly instanceData;
        [ReadOnly] public CPUSharedInstanceData.ReadOnly sharedInstanceData;
        [NativeDisableContainerSafetyRestriction, NoAlias] [ReadOnly] public NativeList<LODGroupCullingData> lodGroupCullingData;
        [NativeDisableUnsafePtrRestriction] [ReadOnly] public IntPtr occlusionBuffer;

        [NativeDisableContainerSafetyRestriction] public CPUPerCameraInstanceData.PerCameraInstanceDataArrays cameraInstanceData;

        [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<byte> rendererVisibilityMasks;
        [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<byte> rendererMeshLodSettings;
        [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<byte> rendererCrossFadeValues;


        // float [-1.0f... 1.0f] -> uint [0...254]
        static uint PackFloatToUint8(float percent)
        {
            uint packed = (uint)((1.0f + percent) * 127.0f + 0.5f);
            // avoid zero
            if (percent < 0.0f)
                packed = math.clamp(packed, 0, 126);
            else
                packed = math.clamp(packed, 128, 254);
            return packed;
        }

        unsafe uint CalculateLODVisibility(int instanceIndex, int sharedInstanceIndex, InstanceFlags instanceFlags)
        {
            var lodDataIndexAndMask = sharedInstanceData.lodGroupAndMasks[sharedInstanceIndex];

            if (lodDataIndexAndMask == k_InvalidCrossFadeAndLevel)
            {
                if(viewType >= BatchCullingViewType.SelectionOutline
                    || (instanceFlags & InstanceFlags.SmallMeshCulling) == 0
                    || minScreenRelativeHeight == 0.0f)
                    return k_LODFadeOff;

                // If no LODGroup available - small mesh culling.
                ref readonly AABB worldAABB = ref instanceData.worldAABBs.UnsafeElementAt(instanceIndex);
                var cameraSqrDist = isOrtho ? sqrScreenRelativeMetric : LODRenderingUtils.CalculateSqrPerspectiveDistance(worldAABB.center, cameraPosition, sqrScreenRelativeMetric);
                var cameraDist = math.sqrt(cameraSqrDist);

                var aabbSize = worldAABB.extents * 2.0f;
                var worldSpaceSize = math.max(math.max(aabbSize.x, aabbSize.y), aabbSize.z);
                var maxDist = LODRenderingUtils.CalculateLODDistance(minScreenRelativeHeight, worldSpaceSize);
                if (maxDist < cameraDist)
                    return k_LODFadeZeroPacked;

                var transitionHeight = minScreenRelativeHeight + k_SmallMeshTransitionWidth * minScreenRelativeHeight;
                var fadeOutRange = Mathf.Max(0.0f,maxDist - LODRenderingUtils.CalculateLODDistance(transitionHeight, worldSpaceSize));

                var lodPercent = (maxDist - cameraDist) / fadeOutRange;
                return lodPercent > 1.0f ? k_LODFadeOff : PackFloatToUint8(lodPercent);
            }

            var lodIndex = lodDataIndexAndMask >> 8;
            var lodMask = lodDataIndexAndMask & 0xFF;
            Assert.IsTrue(lodMask > 0);

            ref var lodGroup = ref lodGroupCullingData.ElementAt((int)lodIndex);

            if (lodGroup.forceLODMask != 0)
                return (lodGroup.forceLODMask & lodMask) != 0 ? k_LODFadeOff : k_LODFadeZeroPacked;

            float cameraSqrDistToLODCenter = isOrtho ? sqrScreenRelativeMetric : LODRenderingUtils.CalculateSqrPerspectiveDistance(lodGroup.worldSpaceReferencePoint, cameraPosition, sqrScreenRelativeMetric);

            // Remove lods that are beyond the max lod.
            uint maxLodMask = 0xffffffff << maxLOD;
            lodMask &= maxLodMask;

            // Offset to the lod preceding the first for proper cross fade calculation.
            int m = math.max(math.tzcnt(lodMask) - 1, maxLOD);
            lodMask >>= m;

            while (lodMask > 0)
            {
                var lodRangeSqrMin = m == maxLOD ? 0.0f : lodGroup.sqrDistances[m - 1];
                var lodRangeSqrMax = lodGroup.sqrDistances[m];

                // Camera is beyond the range of this all further lods. No need to proceed.
                if (cameraSqrDistToLODCenter < lodRangeSqrMin)
                    break;

                if (cameraSqrDistToLODCenter > lodRangeSqrMax)
                {

                    ++m;
                    lodMask >>= 1;
                    continue;
                }

                // Instance is in the min/max range of this lod. Proceeding.
                var type = (CrossFadeType)(lodMask & 3);

                // Instance is in neither this LOD Level nor next - invisible
                if (type == CrossFadeType.kDisabled)
                {
                    return k_LODFadeZeroPacked;
                }
                // Instance is in both this and the next lod. No need to fade - fully visible
                if (type == CrossFadeType.kVisible)
                {
                    return k_LODFadeOff;
                }

                var distanceToLodCenter = math.sqrt(cameraSqrDistToLODCenter);
                var maxDist = math.sqrt(lodRangeSqrMax);

                // SpeedTree cross fade.
                if (lodGroup.percentageFlags[m])
                {
                    // The fading-in instance is not visible but the fading-out is visible and it does the speed tree vertex deformation.
                    if (type == CrossFadeType.kCrossFadeIn)
                    {
                        return k_LODFadeZeroPacked + 1;
                    }
                    else if (type == CrossFadeType.kCrossFadeOut)
                    {
                        var minDist = m > 0
                            ? math.sqrt(lodGroup.sqrDistances[m - 1])
                            : lodGroup.worldSpaceSize;
                        var lodPercent = math.max(distanceToLodCenter - minDist, 0.0f) / (maxDist - minDist);
                        return PackFloatToUint8(lodPercent) | k_LODFadeIsSpeedTree;
                    }
                }
                // Dithering cross fade.
                else
                {
                    var transitionDist = lodGroup.transitionDistances[m];
                    var dif = maxDist - distanceToLodCenter;
                    // If in the transition zone, both fading-in and fading-out instances are visible. Calculate the lod percent.
                    // If not then only the fading-out instance is fully visible, and fading-in is invisible.
                    if (dif < transitionDist)
                    {
                        var lodPercent = dif / transitionDist;

                        if (type == CrossFadeType.kCrossFadeIn)
                            lodPercent = -lodPercent;
                        return PackFloatToUint8(lodPercent);
                    }

                    return type == CrossFadeType.kCrossFadeOut ? k_LODFadeOff : k_LODFadeZeroPacked;
                }

            }

            return k_LODFadeZeroPacked;
        }

        private unsafe uint CalculateVisibilityMask(int instanceIndex, int sharedInstanceIndex, InstanceFlags instanceFlags)
        {
            if (cullingLayerMask == 0)
                return 0;

            if ((cullingLayerMask & (1 << sharedInstanceData.gameObjectLayers[sharedInstanceIndex])) == 0)
                return 0;

            if (cullLightmappedShadowCasters && (instanceFlags & InstanceFlags.AffectsLightmaps) != 0)
                return 0;

#if UNITY_EDITOR
            if ((sceneCullingMask & instanceData.editorData.sceneCullingMasks[instanceIndex]) == 0)
                return 0;

            if(viewType == BatchCullingViewType.SelectionOutline && !instanceData.editorData.selectedBits.Get(instanceIndex))
                return 0;
#endif

            // cull early for camera and shadow views based on the shadow culling mode
            if (viewType == BatchCullingViewType.Camera && (instanceFlags & InstanceFlags.IsShadowsOnly) != 0)
                return 0;
            if (viewType == BatchCullingViewType.Light && (instanceFlags & InstanceFlags.IsShadowsOff) != 0)
                return 0;

            ref readonly AABB worldAABB = ref instanceData.worldAABBs.UnsafeElementAt(instanceIndex);
            uint visibilityMask = FrustumPlaneCuller.ComputeSplitVisibilityMask(frustumPlanePackets, frustumSplitInfos, worldAABB);

            if (visibilityMask != 0 && receiverSplitInfos.Length > 0)
                visibilityMask &= ReceiverSphereCuller.ComputeSplitVisibilityMask(lightFacingFrustumPlanes, receiverSplitInfos, worldToLightSpaceRotation, worldAABB);

            // Perform an occlusion test on the instance bounds if we have an occlusion buffer available and the instance is still visible
            if (visibilityMask != 0 && occlusionBuffer != IntPtr.Zero)
                visibilityMask = BatchRendererGroup.OcclusionTestAABB(occlusionBuffer, worldAABB.ToBounds()) ? visibilityMask : 0;

            return visibilityMask;
        }

        // Algorithm is detailed and must be kept in sync with CalculateMeshLod (C++)
        private uint ComputeMeshLODLevel(int instanceIndex, int sharedInstanceIndex)
        {
            ref readonly GPUDrivenRendererMeshLodData meshLodData = ref instanceData.meshLodData.UnsafeElementAt(instanceIndex);
            var meshLodInfo = sharedInstanceData.meshLodInfos[sharedInstanceIndex];

            if (meshLodData.forceLod >= 0)
                return (uint)math.clamp(meshLodData.forceLod, 0, meshLodInfo.levelCount - 1);

            ref readonly AABB worldAABB = ref instanceData.worldAABBs.UnsafeElementAt(instanceIndex);

            var radiusSqr = math.max(math.lengthsq(worldAABB.extents), 1e-5f);
            var diameterSqr = radiusSqr * 4;
            var cameraSqrHeightAtDistance = isOrtho ? sqrMeshLodSelectionConstant :
                LODRenderingUtils.CalculateSqrPerspectiveDistance(worldAABB.center, cameraPosition, sqrMeshLodSelectionConstant);

            var boundsDesiredPercentage = Math.Sqrt(cameraSqrHeightAtDistance / diameterSqr);

            var levelIndexFlt = math.log2(boundsDesiredPercentage) * meshLodInfo.lodSlope + meshLodInfo.lodBias;

            // We apply Bias after max to enforce that a positive bias of +N we would select lodN instead of Lod0
            levelIndexFlt = math.max(levelIndexFlt, 0);
            levelIndexFlt += meshLodData.lodSelectionBias;

            levelIndexFlt = math.clamp(levelIndexFlt, 0, meshLodInfo.levelCount - 1);

            return (uint)math.floor(levelIndexFlt);
        }

        // A crossfading instance is assigned a [0.0(invisible) - 1.0(fully visible)] value.
        // The complementary instance is assigned the negative of this value[-1.0(invisible) - -0.0(fully visible)]
        // As we pack it to a 8-bit uint, we transition from [0-126] when transitioning from a Lower to a higher LOD level,
        // and from [127-254] when transitioning from a higher to a lower LOD level.
        // This means that we can use (k_MeshLODTransitionToLowerLODBit = 0x80) to select the correct instance to blend with during draw command generation.
        private unsafe uint ComputeMeshLODCrossfade(int instanceIndex, ref uint meshLodLevel)
        {
            var previousLodLevel = cameraInstanceData.meshLods[instanceIndex];

            //1st frame - previous LOD level is invalid. Just update it and return.
            if(previousLodLevel == 0xff)
            {
                cameraInstanceData.meshLods[instanceIndex] = (byte)meshLodLevel;
                return k_LODFadeOff;
            }

            var currentCrossFade = cameraInstanceData.crossFades[instanceIndex];

            // If not already crossfading, check if we changed Lod level this frame, and starts crossfading.
            if (currentCrossFade == k_LODFadeOff)
            {
                if (previousLodLevel == meshLodLevel)
                    return k_LODFadeOff;

                cameraInstanceData.meshLods[instanceIndex] = (byte)meshLodLevel;
                cameraInstanceData.crossFades[instanceIndex] = (byte)(meshLodLevel < previousLodLevel ? AnimateCrossFadeJob.k_MeshLODTransitionToLowerLODBit : 1);
                meshLodLevel = previousLodLevel;
                return k_LODFadeOff;
            }

            //On the first crossfading frame, other cameras could have set the current crossfade values, but we do not want to fade until next frame.
            if ((currentCrossFade - 1) % k_LODFadeZeroPacked == 0)
            {
                meshLodLevel = previousLodLevel;
                return k_LODFadeOff;
            }

            meshLodLevel =  previousLodLevel | (currentCrossFade > k_LODFadeZeroPacked ? k_MeshLodCrossfadeBits : k_MeshLodCrossfadeActive);

            return currentCrossFade;
        }

        private unsafe void EnforcePreviousFrameMeshLOD(int instanceIndex, ref uint meshLodLevel)
        {
            ref var previousLodLevel = ref cameraInstanceData.meshLods.ElementAt(instanceIndex);

            if (previousLodLevel != k_LODFadeOff)
            {
                meshLodLevel = previousLodLevel;
            }
        }

        public void Execute(int instanceIndex)
        {
            InstanceHandle instance = instanceData.instances[instanceIndex];
            int sharedInstanceIndex = sharedInstanceData.InstanceToIndex(instanceData, instance);
            var instanceFlags = sharedInstanceData.flags[sharedInstanceIndex].instanceFlags;

            var visibilityMask = CalculateVisibilityMask(instanceIndex, sharedInstanceIndex, instanceFlags);
            if (visibilityMask == k_VisibilityMaskNotVisible)
            {
                rendererVisibilityMasks[instance.index] = (byte)k_VisibilityMaskNotVisible;
                return;
            }

            var crossFadeValue = CalculateLODVisibility(instanceIndex, sharedInstanceIndex, instanceFlags);
            if (crossFadeValue == k_LODFadeZeroPacked)
            {
                rendererVisibilityMasks[instance.index] = (byte)k_VisibilityMaskNotVisible;
                return;
            }

            if (binningConfig.supportsMotionCheck)
            {
                bool hasMotion = instanceData.movedInPreviousFrameBits.Get(instanceIndex);
                visibilityMask = (visibilityMask << 1) | (hasMotion ? 1U : 0);
            }

            var meshLodLevel = 0u;
            // select mesh LOD level
            var hasMeshLod = (instanceFlags & InstanceFlags.HasMeshLod) != 0;
            if (hasMeshLod)
            {
                meshLodLevel = ComputeMeshLODLevel(instanceIndex, sharedInstanceIndex);
            }

            if (binningConfig.supportsCrossFade)
            {
                if(hasMeshLod && animateCrossFades)
                {
                    //Only Crossfade mesh LOD if we aren't already crossfading through LOD groups or SpeedTree
                    if (crossFadeValue == k_LODFadeOff)
                    {
                        crossFadeValue = ComputeMeshLODCrossfade(instanceIndex, ref meshLodLevel);
                    }
                    else
                    {
                        // If we are cross fading through LODGroup, we reuse previous frame's LodLevel to avoid popping
                        EnforcePreviousFrameMeshLOD(instanceIndex, ref meshLodLevel);
                    }
                }
                // if crossfade is 255 or more, either crossfade is off or it's speedTreeFade, cases for which we do not need to set the fade bit.
                visibilityMask = (visibilityMask << 1) | (crossFadeValue < k_LODFadeOff ? 1U : 0);
            }

            rendererVisibilityMasks[instance.index] = (byte)visibilityMask;
            rendererMeshLodSettings[instance.index] = (byte)meshLodLevel;
            rendererCrossFadeValues[instance.index] = (byte)(crossFadeValue & 0xFF);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct AllocateBinsPerBatch : IJobParallelFor
    {
        [ReadOnly] public BinningConfig binningConfig;

        [ReadOnly] public NativeList<DrawBatch> drawBatches;
        [ReadOnly] public NativeArray<int> drawInstanceIndices;
        [ReadOnly] public CPUInstanceData.ReadOnly instanceData;
        [ReadOnly] public NativeArray<byte> rendererVisibilityMasks;
        [ReadOnly] public NativeArray<byte> rendererMeshLodSettings;

        [NativeDisableContainerSafetyRestriction, NoAlias] [WriteOnly] public NativeArray<int> batchBinAllocOffsets;
        [NativeDisableContainerSafetyRestriction, NoAlias] [WriteOnly] public NativeArray<int> batchBinCounts;

        [NativeDisableContainerSafetyRestriction, NoAlias] [DeallocateOnJobCompletion] public NativeArray<int> binAllocCounter;
        [NativeDisableContainerSafetyRestriction, NoAlias] [WriteOnly] public NativeArray<short> binConfigIndices;
        [NativeDisableContainerSafetyRestriction, NoAlias] [WriteOnly] public NativeArray<int> binVisibleInstanceCounts;

        [ReadOnly] public int debugCounterIndexBase;
        [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> splitDebugCounters;

        bool IsInstanceFlipped(int rendererIndex)
        {
            InstanceHandle instance = InstanceHandle.FromInt(rendererIndex);
            int instanceIndex = instanceData.InstanceToIndex(instance);
            return instanceData.localToWorldIsFlippedBits.Get(instanceIndex);
        }

        // Keep in sync with isMeshLodVisible from DrawCommandOutputPerBatch
        private bool IsMeshLodVisible(int batchLodLevel, int rendererIndex, bool supportsCrossFade)
        {
            if (batchLodLevel < 0)
                return true;

            var meshLodLevel = rendererMeshLodSettings[rendererIndex];
            var instanceLodLevel = meshLodLevel & ~CullingJob.k_MeshLodCrossfadeBits;
            if (batchLodLevel == instanceLodLevel)
                return true;

            if (!supportsCrossFade)
                return false;

            var crossfadeBits = (meshLodLevel & CullingJob.k_MeshLodCrossfadeBits);
            if (crossfadeBits == 0)
                return false;

            // This sets sign to 1 if the 8th bit (e.g. k_MeshCrossfadeSignBit is set), and -1 otherwise.
            var sign = (int)(crossfadeBits - CullingJob.k_MeshLodCrossfadeSignBit) >> 6;

            return batchLodLevel == instanceLodLevel + sign;
        }

        static int GetPrimitiveCount(int indexCount, MeshTopology topology, bool nativeQuads)
        {
            switch (topology)
            {
                case MeshTopology.Triangles: return indexCount / 3;
                case MeshTopology.Quads: return nativeQuads ? (indexCount / 4) : (indexCount / 4 * 2);
                case MeshTopology.Lines: return indexCount / 2;
                case MeshTopology.LineStrip: return (indexCount >= 1) ? (indexCount - 1) : 0;
                case MeshTopology.Points: return indexCount;
                default: Debug.Assert(false, "unknown primitive type"); return 0;
            }
        }

        public void Execute(int batchIndex)
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
            var supportsCrossFade = (drawBatch.key.flags & BatchDrawCommandFlags.LODCrossFadeKeyword) != 0;
            for (int i = 0; i < instanceCount; ++i)
            {
                var rendererIndex = drawInstanceIndices[instanceOffset + i];

                bool isFlipped = IsInstanceFlipped(rendererIndex);
                int visibilityMask = (int)rendererVisibilityMasks[rendererIndex];

                bool isVisible = (visibilityMask != 0);
                if (!isVisible)
                    continue;

                if (!IsMeshLodVisible(drawBatch.key.activeMeshLod, rendererIndex, supportsCrossFade))
                    continue;

                int configIndex = (int)(visibilityMask << 1) | (isFlipped ? 1 : 0);
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
                var drawCommandCountPerView = stackalloc int[binningConfig.viewCount];
                var visibleCountPerView = stackalloc int[binningConfig.viewCount];
                for (int i = 0; i < binningConfig.viewCount; ++i)
                {
                    drawCommandCountPerView[i] = 0;
                    visibleCountPerView[i] = 0;
                }

                bool countVisibilityStats = (debugCounterIndexBase >= 0);
                int shiftForVisibilityMask = 1 + (binningConfig.supportsMotionCheck ? 1 : 0) + (binningConfig.supportsCrossFade ? 1 : 0);

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

                        int visibilityMask = countVisibilityStats ? (configIndex >> shiftForVisibilityMask) : 0;
                        while (visibilityMask != 0)
                        {
                            var viewIndex = math.tzcnt(visibilityMask);
                            visibilityMask ^= 1 << viewIndex;

                            drawCommandCountPerView[viewIndex] += 1;
                            visibleCountPerView[viewIndex] += visibleCount;
                        }
                    }
                }
                Assert.IsTrue(allocOffset == allocOffsetEnd);

                if (countVisibilityStats)
                {
                    for (int viewIndex = 0; viewIndex < binningConfig.viewCount; ++viewIndex)
                    {
                        int* counterPtr = (int*)splitDebugCounters.GetUnsafePtr() + (debugCounterIndexBase + viewIndex) * (int)InstanceCullerSplitDebugCounter.Count;

                        int drawCommandCount = drawCommandCountPerView[viewIndex];
                        if (drawCommandCount > 0)
                            Interlocked.Add(ref UnsafeUtility.AsRef<int>(counterPtr + (int)InstanceCullerSplitDebugCounter.DrawCommands), drawCommandCount);

                        int visibleCount = visibleCountPerView[viewIndex];
                        if (visibleCount > 0)
                        {
                            int primitiveCount = GetPrimitiveCount((int)drawBatch.procInfo.indexCount, drawBatch.procInfo.topology, false);

                            Interlocked.Add(ref UnsafeUtility.AsRef<int>(counterPtr + (int)InstanceCullerSplitDebugCounter.VisibleInstances), visibleCount);
                            Interlocked.Add(ref UnsafeUtility.AsRef<int>(counterPtr + (int)InstanceCullerSplitDebugCounter.VisiblePrimitives), visibleCount * primitiveCount);
                        }
                    }
                }
            }
            batchBinAllocOffsets[batchIndex] = allocOffsetStart;
            batchBinCounts[batchIndex] = binCount;
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct PrefixSumDrawsAndInstances : IJob
    {
        [ReadOnly] public NativeList<DrawRange> drawRanges;
        [ReadOnly] public NativeArray<int> drawBatchIndices;

        [ReadOnly] public NativeArray<int> batchBinAllocOffsets;
        [ReadOnly] public NativeArray<int> batchBinCounts;
        [ReadOnly] public NativeArray<int> binVisibleInstanceCounts;

        [NativeDisableContainerSafetyRestriction, NoAlias] [WriteOnly] public NativeArray<int> batchDrawCommandOffsets;
        [NativeDisableContainerSafetyRestriction, NoAlias] [WriteOnly] public NativeArray<int> binVisibleInstanceOffsets;

        [NativeDisableUnsafePtrRestriction] public NativeArray<BatchCullingOutputDrawCommands> cullingOutput;

        [ReadOnly] public IndirectBufferLimits indirectBufferLimits;
        [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<IndirectBufferAllocInfo> indirectBufferAllocInfo;
        [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> indirectAllocationCounters;

        unsafe public void Execute()
        {
            BatchCullingOutputDrawCommands output = cullingOutput[0];

            bool allowIndirect = indirectBufferLimits.maxInstanceCount > 0;

            int outRangeIndex;
            int outDirectCommandIndex;
            int outDirectVisibleInstanceIndex;
            int outIndirectCommandIndex;
            int outIndirectVisibleInstanceIndex;

            for (;;)
            {
                // reset counters
                outRangeIndex = 0;
                outDirectCommandIndex = 0;
                outDirectVisibleInstanceIndex = 0;
                outIndirectCommandIndex = 0;
                outIndirectVisibleInstanceIndex = 0;

                for (int rangeIndex = 0; rangeIndex < drawRanges.Length; ++rangeIndex)
                {
                    var drawRangeInfo = drawRanges[rangeIndex];
                    bool isIndirect = allowIndirect && drawRangeInfo.key.supportsIndirect;

                    int rangeDrawCommandCount = 0;
                    int rangeDrawCommandOffset = isIndirect ? outIndirectCommandIndex : outDirectCommandIndex;

                    for (int drawIndexInRange = 0; drawIndexInRange < drawRangeInfo.drawCount; ++drawIndexInRange)
                    {
                        var batchIndex = drawBatchIndices[drawRangeInfo.drawOffset + drawIndexInRange];
                        var binAllocOffset = batchBinAllocOffsets[batchIndex];
                        var binCount = batchBinCounts[batchIndex];

                        if (isIndirect)
                        {
                            batchDrawCommandOffsets[batchIndex] = outIndirectCommandIndex;
                            outIndirectCommandIndex += binCount;
                        }
                        else
                        {
                            batchDrawCommandOffsets[batchIndex] = outDirectCommandIndex;
                            outDirectCommandIndex += binCount;
                        }
                        rangeDrawCommandCount += binCount;

                        for (int binIndexInBatch = 0; binIndexInBatch < binCount; ++binIndexInBatch)
                        {
                            var binIndex = binAllocOffset + binIndexInBatch;
                            if (isIndirect)
                            {
                                binVisibleInstanceOffsets[binIndex] = outIndirectVisibleInstanceIndex;
                                outIndirectVisibleInstanceIndex += binVisibleInstanceCounts[binIndex];
                            }
                            else
                            {
                                binVisibleInstanceOffsets[binIndex] = outDirectVisibleInstanceIndex;
                                outDirectVisibleInstanceIndex += binVisibleInstanceCounts[binIndex];
                            }
                        }
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
                            drawCommandsType = isIndirect ? BatchDrawCommandType.Indirect : BatchDrawCommandType.Direct,
                            filterSettings = new BatchFilterSettings
                            {
                                renderingLayerMask = rangeKey.renderingLayerMask,
                                rendererPriority = rangeKey.rendererPriority,
                                layer = rangeKey.layer,
                                batchLayer = isIndirect ? BatchLayer.InstanceCullingIndirect : BatchLayer.InstanceCullingDirect,
                                motionMode = rangeKey.motionMode,
                                shadowCastingMode = rangeKey.shadowCastingMode,
                                receiveShadows = true,
                                staticShadowCaster = rangeKey.staticShadowCaster,
                                allDepthSorted = false,
                            }
                        };
                        outRangeIndex++;
                    }
                }

                output.drawRangeCount = outRangeIndex; // trim to the number of written ranges

                // try to allocate buffer space for indirect
                bool isValid = true;
                if (allowIndirect)
                {
                    int* allocCounters = (int*)indirectAllocationCounters.GetUnsafePtr<int>();

                    var allocInfo = new IndirectBufferAllocInfo();
                    allocInfo.drawCount = outIndirectCommandIndex;
                    allocInfo.instanceCount = outIndirectVisibleInstanceIndex;

                    int drawAllocCount = allocInfo.drawCount + IndirectBufferContextStorage.kExtraDrawAllocationCount;
                    int drawAllocEnd = Interlocked.Add(ref UnsafeUtility.AsRef<int>(allocCounters + (int)IndirectAllocator.NextDrawIndex), drawAllocCount);
                    allocInfo.drawAllocIndex = drawAllocEnd - drawAllocCount;

                    int instanceAllocEnd = Interlocked.Add(ref UnsafeUtility.AsRef<int>(allocCounters + (int)IndirectAllocator.NextInstanceIndex), allocInfo.instanceCount);
                    allocInfo.instanceAllocIndex = instanceAllocEnd - allocInfo.instanceCount;

                    if (!allocInfo.IsWithinLimits(indirectBufferLimits))
                    {
                        allocInfo = new IndirectBufferAllocInfo();
                        isValid = false;
                    }

                    indirectBufferAllocInfo[0] = allocInfo;
                }
                if (isValid)
                    break;

                // out of indirect memory, reset counters and try again without indirect
                //Debug.Log("Out of indirect buffer space: falling back to direct draws for this frame!");
                allowIndirect = false;
            }

            if (outDirectCommandIndex != 0)
            {
                output.drawCommandCount = outDirectCommandIndex;
                output.drawCommands = MemoryUtilities.Malloc<BatchDrawCommand>(outDirectCommandIndex, Allocator.TempJob);

                output.visibleInstanceCount = outDirectVisibleInstanceIndex;
                output.visibleInstances = MemoryUtilities.Malloc<int>(outDirectVisibleInstanceIndex, Allocator.TempJob);
            }
            if (outIndirectCommandIndex != 0)
            {
                output.indirectDrawCommandCount = outIndirectCommandIndex;
                output.indirectDrawCommands = MemoryUtilities.Malloc<BatchDrawCommandIndirect>(outIndirectCommandIndex, Allocator.TempJob);
            }

            int totalCommandCount = outDirectCommandIndex + outIndirectCommandIndex;
            output.instanceSortingPositions = MemoryUtilities.Malloc<float>(3 * totalCommandCount, Allocator.TempJob);

            cullingOutput[0] = output;
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct DrawCommandOutputPerBatch : IJobParallelFor
    {
        [ReadOnly] public BinningConfig binningConfig;
        [ReadOnly] public NativeParallelHashMap<uint, BatchID> batchIDs;

        [ReadOnly] public GPUInstanceDataBuffer.ReadOnly instanceDataBuffer;

        [ReadOnly] public NativeList<DrawBatch> drawBatches;
        [ReadOnly] public NativeArray<int> drawInstanceIndices;
        [ReadOnly] public CPUInstanceData.ReadOnly instanceData;

        [ReadOnly] public NativeArray<byte> rendererVisibilityMasks;
        [ReadOnly] public NativeArray<byte> rendererMeshLodSettings;
        [ReadOnly] public NativeArray<byte> rendererCrossFadeValues;

        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> batchBinAllocOffsets;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> batchBinCounts;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> batchDrawCommandOffsets;

        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<short> binConfigIndices;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> binVisibleInstanceOffsets;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> binVisibleInstanceCounts;

        [ReadOnly] public NativeArray<BatchCullingOutputDrawCommands> cullingOutput;

        [ReadOnly] public IndirectBufferLimits indirectBufferLimits;
        [ReadOnly] public GraphicsBufferHandle visibleInstancesBufferHandle;
        [ReadOnly] public GraphicsBufferHandle indirectArgsBufferHandle;
        [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<IndirectBufferAllocInfo> indirectBufferAllocInfo;
        [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<IndirectDrawInfo> indirectDrawInfoGlobalArray;
        [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<IndirectInstanceInfo> indirectInstanceInfoGlobalArray;

        int EncodeGPUInstanceIndexAndCrossFade(int rendererIndex, bool negateCrossFade)
        {
            var gpuInstanceIndex = instanceDataBuffer.CPUInstanceToGPUInstance(InstanceHandle.FromInt(rendererIndex));
            var crossFadeValue = (int)rendererCrossFadeValues[rendererIndex];

            if (crossFadeValue == CullingJob.k_LODFadeOff)
                return gpuInstanceIndex.index;

            crossFadeValue -= (int)CullingJob.k_LODFadeZeroPacked;
            if (negateCrossFade)
                crossFadeValue = -crossFadeValue;
            gpuInstanceIndex.index |= crossFadeValue << 24;
            return gpuInstanceIndex.index;
        }

        bool IsInstanceFlipped(int rendererIndex)
        {
            InstanceHandle instance = InstanceHandle.FromInt(rendererIndex);
            int instanceIndex = instanceData.InstanceToIndex(instance);
            return instanceData.localToWorldIsFlippedBits.Get(instanceIndex);
        }

        // This must be kept in sync with IsMeshLodVisible from binning pass
        private bool IsMeshLodVisible(int batchLodLevel, int rendererIndex, bool supportsCrossFade, ref bool negateCrossfade)
        {
            if (batchLodLevel < 0)
                return true;

            var meshLodSetting = rendererMeshLodSettings[rendererIndex];
            var instanceLodLevel = meshLodSetting & ~CullingJob.k_MeshLodCrossfadeBits;
            if (batchLodLevel == instanceLodLevel)
                return true;

            if (!supportsCrossFade)
                return false;

            var crossfadeBits = (meshLodSetting & CullingJob.k_MeshLodCrossfadeBits);
            if (crossfadeBits == 0)
                return false;

            // This sets sign to 1 if the 8th bit (e.g. k_MeshLodCrossfadeSignBit is set), and -1 otherwise.
            var sign = (int)(crossfadeBits - CullingJob.k_MeshLodCrossfadeSignBit) >> 6;

            negateCrossfade = true;

            return batchLodLevel == instanceLodLevel + sign;
        }

        unsafe public void Execute(int batchIndex)
        {
            DrawBatch drawBatch = drawBatches[batchIndex];

            var binCount = batchBinCounts[batchIndex];
            if (binCount == 0)
                return;

            BatchCullingOutputDrawCommands output = cullingOutput[0];

            IndirectBufferAllocInfo indirectAllocInfo = new IndirectBufferAllocInfo();
            if (indirectBufferLimits.maxDrawCount > 0)
                indirectAllocInfo = indirectBufferAllocInfo[0];
            bool allowIndirect = !indirectAllocInfo.IsEmpty();

            bool isIndirect = allowIndirect && drawBatch.key.range.supportsIndirect;

            // figure out how many combinations of views/features we need to partition by
            int configCount = binningConfig.visibilityConfigCount;

            // allocate storage for the instance offsets, set to zero
            var instanceOffsetPerConfig = stackalloc int[configCount];
            for (int i = 0; i < configCount; ++i)
                instanceOffsetPerConfig[i] = 0;

            // allocate storage to be able to look up the draw index per instance (by config)
            var drawCommandOffsetPerConfig = stackalloc int[configCount];

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
                drawCommandOffsetPerConfig[configIndex] = drawCommandOffset;

                var drawFlags = drawBatch.key.flags;
                bool isFlipped = ((configIndex & 1) != 0);
                if (isFlipped)
                    drawFlags |= BatchDrawCommandFlags.FlipWinding;

                int visibilityMask = configIndex >> 1;
                if (binningConfig.supportsCrossFade)
                {
                    var crossFadeVisibilityBit = (visibilityMask & 1);
                    if (crossFadeVisibilityBit == 0)
                        drawFlags &= ~BatchDrawCommandFlags.LODCrossFadeKeyword;
                    else
                        drawFlags |= BatchDrawCommandFlags.LODCrossFadeKeyword;
                    visibilityMask >>= 1;
                }
                else
                {
                    drawFlags &= ~BatchDrawCommandFlags.LODCrossFadeKeyword;
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
                {
                    int globalCommandOffset = drawCommandOffset;
                    if (isIndirect)
                        globalCommandOffset += output.drawCommandCount; // skip over direct commands
                    sortingPosition = 3 * globalCommandOffset;
                }

#if DEBUG
                if (!batchIDs.ContainsKey(drawBatch.key.overridenComponents))
                    throw new Exception("Draw command created with an invalid BatchID");
#endif
                if (isIndirect)
                {
#if DEBUG
                    if (drawCommandOffset >= output.indirectDrawCommandCount)
                        throw new Exception("Exceeding draw command count");
#endif
                    int instanceInfoGlobalIndex = indirectAllocInfo.instanceAllocIndex + visibleInstanceOffset;
                    int drawInfoGlobalIndex = indirectAllocInfo.drawAllocIndex + drawCommandOffset;

                    indirectDrawInfoGlobalArray[drawInfoGlobalIndex] = new IndirectDrawInfo
                    {
                        indexCount = drawBatch.procInfo.indexCount,
                        firstIndex = drawBatch.procInfo.firstIndex,
                        baseVertex = drawBatch.procInfo.baseVertex,
                        firstInstanceGlobalIndex = (uint)instanceInfoGlobalIndex,
                        maxInstanceCountAndTopology = ((uint)visibleInstanceCount << 3) | (uint)drawBatch.procInfo.topology,
                    };
                    output.indirectDrawCommands[drawCommandOffset] = new BatchDrawCommandIndirect
                    {
                        flags = drawFlags,
                        visibleOffset = (uint)instanceInfoGlobalIndex,
                        batchID = batchIDs[drawBatch.key.overridenComponents],
                        materialID = drawBatch.key.materialID,
                        splitVisibilityMask = (ushort)visibilityMask,
                        lightmapIndex = (ushort)drawBatch.key.lightmapIndex,
                        sortingPosition = sortingPosition,
                        meshID = drawBatch.key.meshID,
                        topology = drawBatch.procInfo.topology,
                        visibleInstancesBufferHandle = visibleInstancesBufferHandle,
                        indirectArgsBufferHandle = indirectArgsBufferHandle,
                        indirectArgsBufferOffset = (uint)(drawInfoGlobalIndex * GraphicsBuffer.IndirectDrawIndexedArgs.size),
                    };
                }
                else
                {
#if DEBUG
                    if (drawCommandOffset >= output.drawCommandCount)
                        throw new Exception("Exceeding draw command count");
#endif
                    output.drawCommands[drawCommandOffset] = new BatchDrawCommand
                    {
                        flags = drawFlags,
                        visibleOffset = (uint)visibleInstanceOffset,
                        visibleCount = (uint)visibleInstanceCount,
                        batchID = batchIDs[drawBatch.key.overridenComponents],
                        materialID = drawBatch.key.materialID,
                        splitVisibilityMask = (ushort)visibilityMask,
						lightmapIndex = (ushort)drawBatch.key.lightmapIndex,
                        sortingPosition = sortingPosition,
                        meshID = drawBatch.key.meshID,
                        submeshIndex = (ushort)drawBatch.key.submeshIndex,
                        activeMeshLod = (ushort)drawBatch.key.activeMeshLod,
                    };
                }
            }

            // write the visible instances
            var instanceOffset = drawBatch.instanceOffset;
            var instanceCount = drawBatch.instanceCount;
            var supportsCrossFade = (drawBatch.key.flags & BatchDrawCommandFlags.LODCrossFadeKeyword) != 0;
            var lastRendererIndex = 0;
            if (binCount > 1)
            {
                for (int i = 0; i < instanceCount; ++i)
                {
                    var rendererIndex = drawInstanceIndices[instanceOffset + i];

                    bool isFlipped = IsInstanceFlipped(rendererIndex);
                    int visibilityMask = (int)rendererVisibilityMasks[rendererIndex];

                    bool isVisible = (visibilityMask != 0);
                    if (!isVisible)
                        continue;

                    bool negateCrossFade = false;
                    if (!IsMeshLodVisible(drawBatch.key.activeMeshLod, rendererIndex, supportsCrossFade, ref negateCrossFade))
                        continue;

                    lastRendererIndex = rendererIndex;

                    // add to the instance list for this bin
                    int configIndex = (int)(visibilityMask << 1) | (isFlipped ? 1 : 0);
                    Assert.IsTrue(configIndex < binningConfig.visibilityConfigCount);
                    var visibleInstanceOffset = instanceOffsetPerConfig[configIndex];
                    instanceOffsetPerConfig[configIndex]++;

                    var instanceIndexAndCrossFade = EncodeGPUInstanceIndexAndCrossFade(rendererIndex, negateCrossFade);

                    if (isIndirect)
                    {
#if DEBUG
                        if (visibleInstanceOffset >= indirectAllocInfo.instanceCount)
                            throw new Exception("Exceeding visible instance count");
#endif

                        // remove extra bits so that the visibility mask is just the view mask
                        if (binningConfig.supportsCrossFade)
                            visibilityMask >>= 1;
                        if (binningConfig.supportsMotionCheck)
                            visibilityMask >>= 1;

                        indirectInstanceInfoGlobalArray[indirectAllocInfo.instanceAllocIndex + visibleInstanceOffset] = new IndirectInstanceInfo
                        {
                            drawOffsetAndSplitMask = (drawCommandOffsetPerConfig[configIndex] << 8) | visibilityMask,
                            instanceIndexAndCrossFade = instanceIndexAndCrossFade,
                        };
                    }
                    else
                    {
#if DEBUG
                        if (visibleInstanceOffset >= output.visibleInstanceCount)
                            throw new Exception("Exceeding visible instance count");
#endif
                        output.visibleInstances[visibleInstanceOffset] = instanceIndexAndCrossFade;
                    }
                }
            }
            else
            {
                int visibleInstanceOffset = lastBinInstanceOffset;
                for (int i = 0; i < instanceCount; ++i)
                {
                    var rendererIndex = drawInstanceIndices[instanceOffset + i];
                    int visibilityMask = (int)rendererVisibilityMasks[rendererIndex];

                    bool isVisible = (visibilityMask != 0);
                    if (!isVisible)
                        continue;

                    bool negateCrossFade = false;
                    if (!IsMeshLodVisible(drawBatch.key.activeMeshLod, rendererIndex, supportsCrossFade, ref negateCrossFade))
                        continue;

                    lastRendererIndex = rendererIndex;

                    var instanceIndexAndCrossFade = EncodeGPUInstanceIndexAndCrossFade(rendererIndex, negateCrossFade);

                    // only one bin for this batch
                    if (isIndirect)
                    {
                        // remove extra bits so that the visibility mask is just the view mask
                        if (binningConfig.supportsCrossFade)
                            visibilityMask >>= 1;
                        if (binningConfig.supportsMotionCheck)
                            visibilityMask >>= 1;

                        indirectInstanceInfoGlobalArray[indirectAllocInfo.instanceAllocIndex + visibleInstanceOffset] = new IndirectInstanceInfo
                        {
                            drawOffsetAndSplitMask = (batchDrawCommandOffset << 8) | visibilityMask,
                            instanceIndexAndCrossFade = instanceIndexAndCrossFade,
                        };
                    }
                    else
                    {
                        output.visibleInstances[visibleInstanceOffset] = instanceIndexAndCrossFade;
                    }
                    visibleInstanceOffset++;
                }
            }

            // use the first instance position of each batch as the sorting position if necessary
            if ((drawBatch.key.flags & BatchDrawCommandFlags.HasSortingPosition) != 0)
            {
                InstanceHandle instance = InstanceHandle.FromInt(lastRendererIndex & 0xffffff);
                int instanceIndex = instanceData.InstanceToIndex(instance);

                ref readonly AABB worldAABB = ref instanceData.worldAABBs.UnsafeElementAt(instanceIndex);
                float3 position = worldAABB.center;

                int globalCommandOffset = batchDrawCommandOffset;
                if (isIndirect)
                    globalCommandOffset += output.drawCommandCount; // skip over direct commands
                int sortingPosition = 3 * globalCommandOffset;

                output.instanceSortingPositions[sortingPosition + 0] = position.x;
                output.instanceSortingPositions[sortingPosition + 1] = position.y;
                output.instanceSortingPositions[sortingPosition + 2] = position.z;
            }
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct CompactVisibilityMasksJob : IJobParallelForBatch
    {
        public const int k_BatchSize = 64;

        [ReadOnly] public NativeArray<byte> rendererVisibilityMasks;

        [NativeDisableContainerSafetyRestriction, NoAlias] public ParallelBitArray compactedVisibilityMasks;

        unsafe public void Execute(int startIndex, int count)
        {
            ulong chunkBits = 0;

            for(int i = 0; i < count; ++i)
            {
                var visibilityMask = rendererVisibilityMasks[startIndex + i];

                if(visibilityMask != 0)
                    chunkBits |= (1ul << i);
            }

            var chunkIndex = startIndex / k_BatchSize;
            compactedVisibilityMasks.InterlockedOrChunk(chunkIndex, chunkBits);
        }
    }

#if UNITY_EDITOR
    internal enum FilteringJobMode
    {
        Filtering,
        Picking
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct DrawCommandOutputFiltering : IJob
    {
        [ReadOnly] public NativeParallelHashMap<uint, BatchID> batchIDs;
        [ReadOnly] public int viewID;

        [ReadOnly] public GPUInstanceDataBuffer.ReadOnly instanceDataBuffer;

        [ReadOnly] public NativeArray<byte> rendererVisibilityMasks;
        [ReadOnly] public NativeArray<byte> rendererMeshLodSettings;
        [ReadOnly] public NativeArray<byte> rendererCrossFadeValues;

        [ReadOnly] public CPUInstanceData.ReadOnly instanceData;
        [ReadOnly] public CPUSharedInstanceData.ReadOnly sharedInstanceData;

        [ReadOnly] public NativeArray<int> drawInstanceIndices;
        [ReadOnly] public NativeList<DrawBatch> drawBatches;
        [ReadOnly] public NativeList<DrawRange> drawRanges;
        [ReadOnly] public NativeArray<int> drawBatchIndices;

        [ReadOnly] public NativeArray<bool> filteringResults;
        [ReadOnly] public NativeArray<int> excludedRenderers;

        [ReadOnly] public FilteringJobMode mode;

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
                        var visibilityMask = rendererVisibilityMasks[rendererIndex];
                        if (drawBatch.key.activeMeshLod >= 0 && drawBatch.key.activeMeshLod != rendererMeshLodSettings[rendererIndex])
                            visibilityMask = 0;
                        if (visibilityMask == 0)
                            continue;

                        InstanceHandle instance = InstanceHandle.FromInt(rendererIndex);
                        int sharedInstanceIndex = sharedInstanceData.InstanceToIndex(instanceData, instance);

                        if (mode == FilteringJobMode.Filtering && filteringResults.IsCreated && (sharedInstanceIndex >= filteringResults.Length || !filteringResults[sharedInstanceIndex]))
                            continue;

                        var rendererID = sharedInstanceData.rendererGroupIDs[sharedInstanceIndex];
                        if (mode == FilteringJobMode.Picking && excludedRenderers.IsCreated && excludedRenderers.Contains(rendererID))
                            continue;

#if DEBUG
                        if (outVisibleInstanceIndex >= output.visibleInstanceCount)
                            throw new Exception("Exceeding visible instance count");

                        if (outCommandIndex >= output.drawCommandCount)
                            throw new Exception("Exceeding draw command count");

                        if (!batchIDs.ContainsKey(drawBatch.key.overridenComponents))
                            throw new Exception("Draw command created with an invalid BatchID");
#endif
                        output.visibleInstances[outVisibleInstanceIndex] = instanceDataBuffer.CPUInstanceToGPUInstance(instance).index;
                        output.drawCommandPickingInstanceIDs[outCommandIndex] = rendererID;
                        output.drawCommands[outCommandIndex] = new BatchDrawCommand
                        {
                            flags = BatchDrawCommandFlags.None,
                            visibleOffset = (uint)outVisibleInstanceIndex,
                            visibleCount = (uint)1,
                            batchID = batchIDs[drawBatch.key.overridenComponents],
                            materialID = drawBatch.key.materialID,
                            splitVisibilityMask = 0x1,
                            lightmapIndex = (ushort)drawBatch.key.lightmapIndex,
                            sortingPosition = 0,
                            meshID = drawBatch.key.meshID,
                            submeshIndex = (ushort)drawBatch.key.submeshIndex,
                        };

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
                            batchLayer = BatchLayer.InstanceCullingDirect,
                            motionMode = rangeKey.motionMode,
                            shadowCastingMode = rangeKey.shadowCastingMode,
                            receiveShadows = true,
                            staticShadowCaster = rangeKey.staticShadowCaster,
                            allDepthSorted = false,
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

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct CullSceneViewHiddenRenderersJob : IJobParallelFor
    {
        public const int k_BatchSize = 128;

        [ReadOnly] public CPUInstanceData.ReadOnly instanceData;
        [ReadOnly] public CPUSharedInstanceData.ReadOnly sharedInstanceData;
        [ReadOnly] public ParallelBitArray hiddenBits;

        [NativeDisableParallelForRestriction] public NativeArray<byte> rendererVisibilityMasks;

        public void Execute(int instanceIndex)
        {
            InstanceHandle instance = instanceData.instances[instanceIndex];

            if (rendererVisibilityMasks[instance.index] > 0)
            {
                int sharedInstanceIndex = sharedInstanceData.InstanceToIndex(instanceData, instance);

                if (hiddenBits.Get(sharedInstanceIndex))
                    rendererVisibilityMasks[instance.index] = 0;
            }
        }
    }
#endif

    internal enum InstanceCullerSplitDebugCounter
    {
        VisibleInstances,
        VisiblePrimitives,
        DrawCommands,
        Count,
    }

    internal struct InstanceCullerSplitDebugArray : IDisposable
    {
        private const int MaxSplitCount = 64;

        internal struct Info
        {
            public BatchCullingViewType viewType;
            public int viewInstanceID;
            public int splitIndex;
        }

        private NativeList<Info> m_Info;
        private NativeArray<int> m_Counters;
        private NativeQueue<JobHandle> m_CounterSync;

        public NativeArray<int> Counters { get => m_Counters; }

        public void Init()
        {
            m_Info = new NativeList<Info>(Allocator.Persistent);
            m_Counters = new NativeArray<int>(MaxSplitCount * (int)InstanceCullerSplitDebugCounter.Count, Allocator.Persistent);
            m_CounterSync = new NativeQueue<JobHandle>(Allocator.Persistent);
        }

        public void Dispose()
        {
            m_Info.Dispose();
            m_Counters.Dispose();
            m_CounterSync.Dispose();
        }

        public int TryAddSplits(BatchCullingViewType viewType, int viewInstanceID, int splitCount)
        {
            int baseIndex = m_Info.Length;
            if (baseIndex + splitCount > MaxSplitCount)
                return -1;

            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex)
            {
                m_Info.Add(new Info()
                {
                    viewType = viewType,
                    viewInstanceID = viewInstanceID,
                    splitIndex = splitIndex,
                });
            }
            return baseIndex;
        }

        public void AddSync(int baseIndex, JobHandle jobHandle)
        {
            if (baseIndex != -1)
                m_CounterSync.Enqueue(jobHandle);
        }

        public void MoveToDebugStatsAndClear(DebugRendererBatcherStats debugStats)
        {
            // wait for stats-writing jobs to complete
            while (m_CounterSync.TryDequeue(out var jobHandle))
            {
                jobHandle.Complete();
            }

            // overwrite debug stats with the latest
            debugStats.instanceCullerStats.Clear();
            for (int index = 0; index < m_Info.Length; ++index)
            {
                var info = m_Info[index];
                int counterBase = index * (int)InstanceCullerSplitDebugCounter.Count;
                debugStats.instanceCullerStats.Add(new InstanceCullerViewStats
                {
                    viewType = info.viewType,
                    viewInstanceID = info.viewInstanceID,
                    splitIndex = info.splitIndex,
                    visibleInstancesOnCPU = m_Counters[counterBase + (int)InstanceCullerSplitDebugCounter.VisibleInstances],
                    visibleInstancesOnGPU = 0, // Unknown at this point, will be filled in later
                    visiblePrimitivesOnCPU = m_Counters[counterBase + (int)InstanceCullerSplitDebugCounter.VisiblePrimitives],
                    visiblePrimitivesOnGPU = 0, // Unknown at this point, will be filled in later
                    drawCommands = m_Counters[counterBase + (int)InstanceCullerSplitDebugCounter.DrawCommands],
                });
            }

            // clear for next frame
            m_Info.Clear();
            m_Counters.FillArray(0);
        }
    }

    internal struct InstanceOcclusionEventDebugArray : IDisposable
    {
        private const int InitialPassCount = 4;
        private const int MaxPassCount = 64;

        internal struct Info
        {
            public int viewInstanceID;
            public InstanceOcclusionEventType eventType;
            public int occluderVersion;
            public int subviewMask;
            public OcclusionTest occlusionTest;

            public bool HasVersion()
            {
                return eventType == InstanceOcclusionEventType.OccluderUpdate || occlusionTest != OcclusionTest.None;
            }
        }

        internal struct Request
        {
            public UnsafeList<Info> info;
            public AsyncGPUReadbackRequest readback;
        }

        private GraphicsBuffer m_CounterBuffer;

        private UnsafeList<Info> m_PendingInfo;
        private NativeQueue<Request> m_Requests;

        private UnsafeList<Info> m_LatestInfo;
        private NativeArray<int> m_LatestCounters;
        private bool m_HasLatest;

        public GraphicsBuffer CounterBuffer { get => m_CounterBuffer; }

        public void Init()
        {
            m_CounterBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MaxPassCount * (int)InstanceOcclusionTestDebugCounter.Count, sizeof(uint));
            m_PendingInfo = new UnsafeList<Info>(InitialPassCount, Allocator.Persistent);
            m_Requests = new NativeQueue<Request>(Allocator.Persistent);
        }

       public void Dispose()
        {
            if (m_HasLatest)
            {
                m_LatestInfo.Dispose();
                m_LatestCounters.Dispose();
                m_HasLatest = false;
            }
            while (m_Requests.TryDequeue(out var req))
            {
                req.readback.WaitForCompletion();
                req.info.Dispose();
            }
            m_Requests.Dispose();
            m_PendingInfo.Dispose();
            m_CounterBuffer.Dispose();
        }

        public int TryAdd(int viewInstanceID, InstanceOcclusionEventType eventType, int occluderVersion, int subviewMask, OcclusionTest occlusionTest)
        {
            int passIndex = m_PendingInfo.Length;
            if (passIndex + 1 > MaxPassCount)
                return -1;

            m_PendingInfo.Add(new Info()
            {
                viewInstanceID = viewInstanceID,
                eventType = eventType,
                occluderVersion = occluderVersion,
                subviewMask = subviewMask,
                occlusionTest = occlusionTest,
            });
            return passIndex;
        }

        public void MoveToDebugStatsAndClear(DebugRendererBatcherStats debugStats)
        {
            // commit the pending set of stats
            if (m_PendingInfo.Length > 0)
            {
                m_Requests.Enqueue(new Request
                {
                    info = m_PendingInfo,
                    readback = AsyncGPUReadback.Request(m_CounterBuffer, m_PendingInfo.Length * (int)InstanceOcclusionTestDebugCounter.Count * sizeof(uint), 0)
                });
                m_PendingInfo = new UnsafeList<Info>(InitialPassCount, Allocator.Persistent);
            }

            // update the latest set of results that are ready
            while (!m_Requests.IsEmpty() && m_Requests.Peek().readback.done)
            {
                var req = m_Requests.Dequeue();
                if (!req.readback.hasError)
                {
                    NativeArray<int> src = req.readback.GetData<int>(0);
                    if (src.Length == req.info.Length * (int)InstanceOcclusionTestDebugCounter.Count)
                    {
                        if (m_HasLatest)
                        {
                            m_LatestInfo.Dispose();
                            m_LatestCounters.Dispose();
                            m_HasLatest = false;
                        }
                        m_LatestInfo = req.info;
                        m_LatestCounters = new NativeArray<int>(src, Allocator.Persistent);
                        m_HasLatest = true;
                    }
                }
            }

            // overwrite debug stats with the latest
            debugStats.instanceOcclusionEventStats.Clear();
            if (m_HasLatest)
            {
                for (int index = 0; index < m_LatestInfo.Length; ++index)
                {
                    var info = m_LatestInfo[index];

                    // make occluder version relative to the first one this frame
                    int occluderVersion = -1;
                    if (info.HasVersion())
                    {
                        occluderVersion = 0;
                        for (int prevIndex = 0; prevIndex < index; ++prevIndex)
                        {
                            var prevInfo = m_LatestInfo[prevIndex];
                            if (prevInfo.HasVersion() && prevInfo.viewInstanceID == info.viewInstanceID)
                            {
                                occluderVersion = info.occluderVersion - prevInfo.occluderVersion;
                                break;
                            }
                        }
                    }

                    int counterBase = index * (int)InstanceOcclusionTestDebugCounter.Count;
                    int instancesOccludedCounter = m_LatestCounters[counterBase + (int)InstanceOcclusionTestDebugCounter.InstancesOccluded];
                    int instancesNotOccludedCounter = m_LatestCounters[counterBase + (int)InstanceOcclusionTestDebugCounter.InstancesNotOccluded];
                    int primitivesOccludedCounter = m_LatestCounters[counterBase + (int)InstanceOcclusionTestDebugCounter.PrimitivesOccluded];
                    int primitivesNotOccludedCounter = m_LatestCounters[counterBase + (int)InstanceOcclusionTestDebugCounter.PrimitivesNotOccluded];

                    debugStats.instanceOcclusionEventStats.Add(new InstanceOcclusionEventStats
                    {
                        viewInstanceID = info.viewInstanceID,
                        eventType = info.eventType,
                        occluderVersion = occluderVersion,
                        subviewMask = info.subviewMask,
                        occlusionTest = info.occlusionTest,
                        visibleInstances = instancesNotOccludedCounter,
                        culledInstances = instancesOccludedCounter,
                        visiblePrimitives = primitivesNotOccludedCounter,
                        culledPrimitives = primitivesOccludedCounter,
                    });
                }
            }

            // clear the GPU buffer for the next frame
            var zeros = new NativeArray<int>(MaxPassCount * (int)InstanceOcclusionTestDebugCounter.Count, Allocator.Temp, NativeArrayOptions.ClearMemory);
            m_CounterBuffer.SetData(zeros);
            zeros.Dispose();
        }
    }

    internal struct InstanceCuller : IDisposable
    {
        private struct AnimatedFadeData
        {
            public int cameraID;
            public JobHandle jobHandle;
        }

        private NativeParallelHashMap<int, AnimatedFadeData> m_LODParamsToCameraID;
        //@ Move this in CPUInstanceData.
        private ParallelBitArray m_CompactedVisibilityMasks;
        private JobHandle m_CompactedVisibilityMasksJobsHandle;

        private IndirectBufferContextStorage m_IndirectStorage;

        private OcclusionTestComputeShader m_OcclusionTestShader;
        private int m_ResetDrawArgsKernel;
        private int m_CopyInstancesKernel;
        private int m_CullInstancesKernel;

        private DebugRendererBatcherStats m_DebugStats;
        private InstanceCullerSplitDebugArray m_SplitDebugArray;
        private InstanceOcclusionEventDebugArray m_OcclusionEventDebugArray;
        private ProfilingSampler m_ProfilingSampleInstanceOcclusionTest;

        private NativeArray<InstanceOcclusionCullerShaderVariables> m_ShaderVariables;
        private ComputeBuffer m_ConstantBuffer;

        private CommandBuffer m_CommandBuffer;

#if UNITY_EDITOR
        private bool m_IsSceneViewCamera;
        private ParallelBitArray m_SceneViewHiddenBits;
#endif

        private static class ShaderIDs
        {
            public static readonly int InstanceOcclusionCullerShaderVariables = Shader.PropertyToID("InstanceOcclusionCullerShaderVariables");
            public static readonly int _DrawInfo = Shader.PropertyToID("_DrawInfo");
            public static readonly int _InstanceInfo = Shader.PropertyToID("_InstanceInfo");
            public static readonly int _DrawArgs = Shader.PropertyToID("_DrawArgs");
            public static readonly int _InstanceIndices = Shader.PropertyToID("_InstanceIndices");
            public static readonly int _InstanceDataBuffer = Shader.PropertyToID("_InstanceDataBuffer");

            // Debug
            public static readonly int _OccluderDepthPyramid = Shader.PropertyToID("_OccluderDepthPyramid");
            public static readonly int _OcclusionDebugCounters = Shader.PropertyToID("_OcclusionDebugCounters");
        }

        internal void Init(GPUResidentDrawerResources resources, DebugRendererBatcherStats debugStats = null)
        {
            m_IndirectStorage.Init();

            m_OcclusionTestShader.Init(resources.instanceOcclusionCullingKernels);
            m_ResetDrawArgsKernel = m_OcclusionTestShader.cs.FindKernel("ResetDrawArgs");
            m_CopyInstancesKernel = m_OcclusionTestShader.cs.FindKernel("CopyInstances");
            m_CullInstancesKernel = m_OcclusionTestShader.cs.FindKernel("CullInstances");

            m_DebugStats = debugStats;
            m_SplitDebugArray = new InstanceCullerSplitDebugArray();
            m_SplitDebugArray.Init();
            m_OcclusionEventDebugArray = new InstanceOcclusionEventDebugArray();
            m_OcclusionEventDebugArray.Init();

            m_ProfilingSampleInstanceOcclusionTest = new ProfilingSampler("InstanceOcclusionTest");

            m_ShaderVariables = new NativeArray<InstanceOcclusionCullerShaderVariables>(1, Allocator.Persistent);
            m_ConstantBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<InstanceOcclusionCullerShaderVariables>(), ComputeBufferType.Constant);

            m_CommandBuffer = new CommandBuffer();
            m_CommandBuffer.name = "EnsureValidOcclusionTestResults";

            m_LODParamsToCameraID = new NativeParallelHashMap<int, AnimatedFadeData>(16, Allocator.Persistent);
        }


        // This relies on the fact that camera culling is scheduled ahead of shadow culling.
        private JobHandle AnimateCrossFades(CPUPerCameraInstanceData perCameraInstanceData, BatchCullingContext cc, out CPUPerCameraInstanceData.PerCameraInstanceDataArrays cameraInstanceData, out bool hasAnimatedCrossfade)
        {
            var lodHash = cc.lodParameters.GetHashCode();
            hasAnimatedCrossfade = m_LODParamsToCameraID.TryGetValue(lodHash, out var animatedFadeData);

            if (hasAnimatedCrossfade)
            {
                cameraInstanceData = perCameraInstanceData.perCameraData[animatedFadeData.cameraID];
                Assert.IsTrue(cameraInstanceData.IsCreated);
                return animatedFadeData.jobHandle;
            }

            if (cc.viewType != BatchCullingViewType.Camera && !hasAnimatedCrossfade)
            {
                // For picking / filtering and outlining passes. We do not have animated crossfade data.
                cameraInstanceData = new CPUPerCameraInstanceData.PerCameraInstanceDataArrays();
                return new JobHandle();
            }

            //For main camera, animate crossfades, and store the result in the hashmap to be retrieved by other cameras
            var viewID = cc.viewID.GetInstanceID();

            hasAnimatedCrossfade = perCameraInstanceData.perCameraData.TryGetValue(viewID, out var tmpCameraInstanceData);
            if (hasAnimatedCrossfade == false)
            {
                // For picking / filtering and outlining passes. We do not have animated crossfade data.
                cameraInstanceData = new CPUPerCameraInstanceData.PerCameraInstanceDataArrays();
                return new JobHandle();
            }

            cameraInstanceData = tmpCameraInstanceData;
            Assert.IsTrue(cameraInstanceData.IsCreated);

            var handle = new AnimateCrossFadeJob()
            {
                deltaTime = Time.deltaTime,
                crossFadeArray = cameraInstanceData.crossFades
            }.Schedule(perCameraInstanceData.instancesLength, AnimateCrossFadeJob.k_BatchSize);

            m_LODParamsToCameraID.TryAdd(lodHash, new AnimatedFadeData(){ cameraID = viewID, jobHandle = handle});
            return handle;
        }

        private unsafe JobHandle CreateFrustumCullingJob(
            in BatchCullingContext cc,
            in CPUInstanceData.ReadOnly instanceData,
            in CPUSharedInstanceData.ReadOnly sharedInstanceData,
            in CPUPerCameraInstanceData perCameraInstanceData,
            NativeList<LODGroupCullingData> lodGroupCullingData,
            in BinningConfig binningConfig,
            float smallMeshScreenPercentage,
            OcclusionCullingCommon occlusionCullingCommon,
            NativeArray<byte> rendererVisibilityMasks,
            NativeArray<byte> rendererMeshLodSettings,
            NativeArray<byte> rendererCrossFadeValues)
        {
            Assert.IsTrue(cc.cullingSplits.Length <= 6, "InstanceCuller supports up to 6 culling splits.");

            ReceiverPlanes receiverPlanes;
            ReceiverSphereCuller receiverSphereCuller;
            FrustumPlaneCuller frustumPlaneCuller;
            float screenRelativeMetric;
            float meshLodConstant;

            fixed (BatchCullingContext* contextPtr = &cc)
            {
                InstanceCullerBurst.SetupCullingJobInput(QualitySettings.lodBias, QualitySettings.meshLodThreshold, contextPtr, &receiverPlanes, &receiverSphereCuller,
                                                         &frustumPlaneCuller, &screenRelativeMetric, &meshLodConstant);
            }

            if (occlusionCullingCommon != null)
                occlusionCullingCommon.UpdateSilhouettePlanes(cc.viewID.GetInstanceID(), receiverPlanes.SilhouettePlaneSubArray());

            var jobHandle = AnimateCrossFades(perCameraInstanceData, cc, out var cameraInstanceData, out var hasAnimatedCrossfade);

            var cullingJob = new CullingJob
            {
                binningConfig = binningConfig,
                viewType = cc.viewType,
                frustumPlanePackets = frustumPlaneCuller.planePackets.AsArray(),
                frustumSplitInfos = frustumPlaneCuller.splitInfos.AsArray(),
                lightFacingFrustumPlanes = receiverPlanes.LightFacingFrustumPlaneSubArray(),
                receiverSplitInfos = receiverSphereCuller.splitInfos.AsArray(),
                worldToLightSpaceRotation = receiverSphereCuller.worldToLightSpaceRotation,
                cullLightmappedShadowCasters = (cc.cullingFlags & BatchCullingFlags.CullLightmappedShadowCasters) != 0,
                cameraPosition = cc.lodParameters.cameraPosition,
                sqrMeshLodSelectionConstant = meshLodConstant * meshLodConstant,
                sqrScreenRelativeMetric = screenRelativeMetric * screenRelativeMetric,
                minScreenRelativeHeight = smallMeshScreenPercentage * 0.01f,
                isOrtho = cc.lodParameters.isOrthographic,
                animateCrossFades = hasAnimatedCrossfade,
                instanceData = instanceData,
                sharedInstanceData = sharedInstanceData,
                cameraInstanceData = cameraInstanceData,
                lodGroupCullingData = lodGroupCullingData,
                occlusionBuffer = cc.occlusionBuffer,
                rendererVisibilityMasks = rendererVisibilityMasks,
                rendererMeshLodSettings = rendererMeshLodSettings,
                rendererCrossFadeValues = rendererCrossFadeValues,
                maxLOD = QualitySettings.maximumLODLevel,
                cullingLayerMask = cc.cullingLayerMask,
                sceneCullingMask = cc.sceneCullingMask,
            }.Schedule(instanceData.instancesLength, CullingJob.k_BatchSize, jobHandle);

            receiverPlanes.Dispose(cullingJob);
            frustumPlaneCuller.Dispose(cullingJob);
            receiverSphereCuller.Dispose(cullingJob);

            return cullingJob;
        }

        private int ComputeWorstCaseDrawCommandCount(
            in BatchCullingContext cc,
            BinningConfig binningConfig,
            CPUDrawInstanceData drawInstanceData)
        {
            int visibleInstancesCount = drawInstanceData.drawInstances.Length;
            int drawCommandCount = drawInstanceData.drawBatches.Length;

            // add the number of batches split due to actively cross-fading
            if (binningConfig.supportsCrossFade)
                drawCommandCount *= 2;

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

        public unsafe JobHandle CreateCullJobTree(
            in BatchCullingContext cc,
            BatchCullingOutput cullingOutput,
            in CPUInstanceData.ReadOnly instanceData,
            in CPUSharedInstanceData.ReadOnly sharedInstanceData,
            in CPUPerCameraInstanceData perCameraInstanceData,
            in GPUInstanceDataBuffer.ReadOnly instanceDataBuffer,
            NativeList<LODGroupCullingData> lodGroupCullingData,
            CPUDrawInstanceData drawInstanceData,
            NativeParallelHashMap<uint, BatchID> batchIDs,
            float smallMeshScreenPercentage,
            OcclusionCullingCommon occlusionCullingCommon)
        {
            // allocate for worst case number of draw ranges (all other arrays allocated after size is known)
            var drawCommands = new BatchCullingOutputDrawCommands();
            drawCommands.drawRangeCount = drawInstanceData.drawRanges.Length;
            drawCommands.drawRanges = MemoryUtilities.Malloc<BatchDrawRange>(drawCommands.drawRangeCount, Allocator.TempJob);
            for (int i = 0; i < drawCommands.drawRangeCount; ++i)
                drawCommands.drawRanges[i].drawCommandsCount = 0;
            cullingOutput.drawCommands[0] = drawCommands;
            cullingOutput.customCullingResult[0] = IntPtr.Zero;

            var binningConfig = new BinningConfig
            {
                viewCount = cc.cullingSplits.Length,
                supportsCrossFade = QualitySettings.enableLODCrossFade,
                supportsMotionCheck = (cc.viewType == BatchCullingViewType.Camera), // TODO: could disable here if RP never needs object motion vectors, for now always batch on it
            };

            var visibilityLength = instanceData.handlesLength;
            var rendererVisibilityMasks = new NativeArray<byte>(visibilityLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var rendererCrossFadeValues = new NativeArray<byte>(visibilityLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var rendererMeshLodSettings = new NativeArray<byte>(visibilityLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var cullingJobHandle = CreateFrustumCullingJob(cc, instanceData, sharedInstanceData, perCameraInstanceData, lodGroupCullingData, binningConfig,
                smallMeshScreenPercentage, occlusionCullingCommon, rendererVisibilityMasks, rendererMeshLodSettings, rendererCrossFadeValues);

#if UNITY_EDITOR
            // Unfortunately BatchCullingContext doesn't provide full visibility and picking context.
            // Including which object is hidden in the hierarchy panel or not pickable in the scene view for tooling purposes.
            // So we have to manually handle bold editor logic here inside the culler.
            // This should be redesigned in the future. Culler should not be responsible for custom editor handling logic or even know that the editor exist.

            // This additionally culls game objects hidden in the hierarchy panel or the scene view or in context editing.
            cullingJobHandle = CreateSceneViewHiddenObjectsCullingJob_EditorOnly(cc, instanceData, sharedInstanceData, rendererVisibilityMasks,
                cullingJobHandle);

            if (cc.viewType == BatchCullingViewType.Picking)
            {
                // This outputs picking draw commands for the objects that can be picked.
                cullingJobHandle = CreatePickingCullingOutputJob_EditorOnly(cc, cullingOutput, instanceData, sharedInstanceData, instanceDataBuffer,
                    drawInstanceData, batchIDs, rendererVisibilityMasks, rendererMeshLodSettings, rendererCrossFadeValues, cullingJobHandle);
            }
            else if (cc.viewType == BatchCullingViewType.Filtering)
            {
                // This outputs draw commands for the objects filtered by search input in the hierarchy on in the scene view.
                cullingJobHandle = CreateFilteringCullingOutputJob_EditorOnly(cc, cullingOutput, instanceData, sharedInstanceData, instanceDataBuffer,
                    drawInstanceData, batchIDs, rendererVisibilityMasks, rendererMeshLodSettings, rendererCrossFadeValues, cullingJobHandle);
            }
#endif
            // This outputs regular draw commands.
            if (cc.viewType == BatchCullingViewType.Camera || cc.viewType == BatchCullingViewType.Light || cc.viewType == BatchCullingViewType.SelectionOutline)
            {
                cullingJobHandle = CreateCompactedVisibilityMaskJob(instanceData, rendererVisibilityMasks, cullingJobHandle);

                int debugCounterBaseIndex = -1;
                if (m_DebugStats?.enabled ?? false)
                {
                    debugCounterBaseIndex = m_SplitDebugArray.TryAddSplits(cc.viewType, cc.viewID.GetInstanceID(), cc.cullingSplits.Length);
                }

                var batchCount = drawInstanceData.drawBatches.Length;
                int maxBinCount = ComputeWorstCaseDrawCommandCount(cc, binningConfig, drawInstanceData);

                var batchBinAllocOffsets = new NativeArray<int>(batchCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var batchBinCounts = new NativeArray<int>(batchCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var batchDrawCommandOffsets = new NativeArray<int>(batchCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                var binAllocCounter = new NativeArray<int>(JobsUtility.CacheLineSize / sizeof(int), Allocator.TempJob);
                var binConfigIndices = new NativeArray<short>(maxBinCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var binVisibleInstanceCounts = new NativeArray<int>(maxBinCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var binVisibleInstanceOffsets = new NativeArray<int>(maxBinCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                int indirectContextIndex = -1;
                bool useOcclusionCulling = (occlusionCullingCommon != null) && occlusionCullingCommon.HasOccluderContext(cc.viewID.GetInstanceID());
                if (useOcclusionCulling)
                {
                    int viewInstanceID = cc.viewID.GetInstanceID();
                    indirectContextIndex = m_IndirectStorage.TryAllocateContext(viewInstanceID);
                    cullingOutput.customCullingResult[0] = (IntPtr)viewInstanceID;
                }
                IndirectBufferLimits indirectBufferLimits = m_IndirectStorage.GetLimits(indirectContextIndex);
                NativeArray<IndirectBufferAllocInfo> indirectBufferAllocInfo = m_IndirectStorage.GetAllocInfoSubArray(indirectContextIndex);

                var allocateBinsJob = new AllocateBinsPerBatch
                {
                    binningConfig = binningConfig,
                    drawBatches = drawInstanceData.drawBatches,
                    drawInstanceIndices = drawInstanceData.drawInstanceIndices,
                    instanceData = instanceData,
                    rendererVisibilityMasks = rendererVisibilityMasks,
                    rendererMeshLodSettings = rendererMeshLodSettings,
                    batchBinAllocOffsets = batchBinAllocOffsets,
                    batchBinCounts = batchBinCounts,
                    binAllocCounter = binAllocCounter,
                    binConfigIndices = binConfigIndices,
                    binVisibleInstanceCounts = binVisibleInstanceCounts,
                    splitDebugCounters = m_SplitDebugArray.Counters,
                    debugCounterIndexBase = debugCounterBaseIndex,
                };

                var allocateBinsHandle = allocateBinsJob.Schedule(batchCount, 1, cullingJobHandle);

                m_SplitDebugArray.AddSync(debugCounterBaseIndex, allocateBinsHandle);

                var prefixSumJob = new PrefixSumDrawsAndInstances
                {
                    drawRanges = drawInstanceData.drawRanges,
                    drawBatchIndices = drawInstanceData.drawBatchIndices,
                    batchBinAllocOffsets = batchBinAllocOffsets,
                    batchBinCounts = batchBinCounts,
                    binVisibleInstanceCounts = binVisibleInstanceCounts,
                    batchDrawCommandOffsets = batchDrawCommandOffsets,
                    binVisibleInstanceOffsets = binVisibleInstanceOffsets,
                    cullingOutput = cullingOutput.drawCommands,
                    indirectBufferLimits = indirectBufferLimits,
                    indirectBufferAllocInfo = indirectBufferAllocInfo,
                    indirectAllocationCounters = m_IndirectStorage.allocationCounters,
                };

                var prefixSumHandle = prefixSumJob.Schedule(allocateBinsHandle);

                var drawCommandOutputJob = new DrawCommandOutputPerBatch
                {
                    binningConfig = binningConfig,
                    batchIDs = batchIDs,
                    instanceDataBuffer = instanceDataBuffer,
                    drawBatches = drawInstanceData.drawBatches,
                    drawInstanceIndices = drawInstanceData.drawInstanceIndices,
                    instanceData = instanceData,
                    rendererVisibilityMasks = rendererVisibilityMasks,
                    rendererMeshLodSettings = rendererMeshLodSettings,
                    rendererCrossFadeValues = rendererCrossFadeValues,
                    batchBinAllocOffsets = batchBinAllocOffsets,
                    batchBinCounts = batchBinCounts,
                    batchDrawCommandOffsets = batchDrawCommandOffsets,
                    binConfigIndices = binConfigIndices,
                    binVisibleInstanceOffsets = binVisibleInstanceOffsets,
                    binVisibleInstanceCounts = binVisibleInstanceCounts,
                    cullingOutput = cullingOutput.drawCommands,
                    indirectBufferLimits = indirectBufferLimits,
                    visibleInstancesBufferHandle = m_IndirectStorage.visibleInstanceBufferHandle,
                    indirectArgsBufferHandle = m_IndirectStorage.indirectArgsBufferHandle,
                    indirectBufferAllocInfo = indirectBufferAllocInfo,
                    indirectInstanceInfoGlobalArray = m_IndirectStorage.instanceInfoGlobalArray,
                    indirectDrawInfoGlobalArray = m_IndirectStorage.drawInfoGlobalArray,
                };

                var drawCommandOutputHandle = drawCommandOutputJob.Schedule(batchCount, 1, prefixSumHandle);

                if (useOcclusionCulling)
                    m_IndirectStorage.SetBufferContext(indirectContextIndex, new IndirectBufferContext(drawCommandOutputHandle));

                cullingJobHandle = drawCommandOutputHandle;
            }

            cullingJobHandle = rendererVisibilityMasks.Dispose(cullingJobHandle);
            cullingJobHandle = rendererCrossFadeValues.Dispose(cullingJobHandle);
            cullingJobHandle = rendererMeshLodSettings.Dispose(cullingJobHandle);

            return cullingJobHandle;
        }

        private JobHandle CreateCompactedVisibilityMaskJob(in CPUInstanceData.ReadOnly instanceData, NativeArray<byte> rendererVisibilityMasks, JobHandle cullingJobHandle)
        {
            if (!m_CompactedVisibilityMasks.IsCreated)
            {
                Assert.IsTrue(m_CompactedVisibilityMasksJobsHandle.IsCompleted);
                m_CompactedVisibilityMasks = new ParallelBitArray(instanceData.handlesLength, Allocator.TempJob);
            }

            var compactVisibilityMasksJob = new CompactVisibilityMasksJob
            {
                rendererVisibilityMasks = rendererVisibilityMasks,
                compactedVisibilityMasks = m_CompactedVisibilityMasks
            };

            var compactVisibilityMasksJobHandle = compactVisibilityMasksJob.ScheduleBatch(rendererVisibilityMasks.Length, CompactVisibilityMasksJob.k_BatchSize, cullingJobHandle);
            m_CompactedVisibilityMasksJobsHandle = JobHandle.CombineDependencies(m_CompactedVisibilityMasksJobsHandle, compactVisibilityMasksJobHandle);

            return compactVisibilityMasksJobHandle;
        }

#if UNITY_EDITOR

        private JobHandle CreateSceneViewHiddenObjectsCullingJob_EditorOnly(in BatchCullingContext cc, in CPUInstanceData.ReadOnly instanceData,
            in CPUSharedInstanceData.ReadOnly sharedInstanceData, NativeArray<byte> rendererVisibilityMasks, JobHandle cullingJobHandle)
        {
            bool isSceneViewCamera = m_IsSceneViewCamera && (cc.viewType == BatchCullingViewType.Camera || cc.viewType == BatchCullingViewType.Light);
            bool isEditorCullingViewType = cc.viewType == BatchCullingViewType.Picking || cc.viewType == BatchCullingViewType.SelectionOutline
                || cc.viewType == BatchCullingViewType.Filtering;

            if (!isSceneViewCamera && !isEditorCullingViewType)
                return cullingJobHandle;

            bool isEditingPrefab = PrefabStageUtility.GetCurrentPrefabStage() != null;
            bool isAnyObjectHidden = false;

            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (SceneVisibilityManager.instance.AreAnyDescendantsHidden(scene))
                {
                    isAnyObjectHidden = true;
                    break;
                }
            }

            if (!isAnyObjectHidden && !isEditingPrefab)
                return cullingJobHandle;

            int renderersLength = sharedInstanceData.rendererGroupIDs.Length;

            if (!m_SceneViewHiddenBits.IsCreated)
            {
                m_SceneViewHiddenBits = new ParallelBitArray(renderersLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                EditorCameraUtils.GetRenderersHiddenResultBits(sharedInstanceData.rendererGroupIDs, m_SceneViewHiddenBits.GetBitsArray().Reinterpret<ulong>());
            }

            var jobHandle = new CullSceneViewHiddenRenderersJob
            {
                instanceData = instanceData,
                sharedInstanceData = sharedInstanceData,
                rendererVisibilityMasks = rendererVisibilityMasks,
                hiddenBits = m_SceneViewHiddenBits,
            }.Schedule(instanceData.instancesLength, CullSceneViewHiddenRenderersJob.k_BatchSize, cullingJobHandle);

            return jobHandle;
        }

        private JobHandle CreateFilteringCullingOutputJob_EditorOnly(in BatchCullingContext cc, BatchCullingOutput cullingOutput,
            in CPUInstanceData.ReadOnly instanceData, in CPUSharedInstanceData.ReadOnly sharedInstanceData, in GPUInstanceDataBuffer.ReadOnly instanceDataBuffer,
            in CPUDrawInstanceData drawInstanceData, NativeParallelHashMap<uint, BatchID> batchIDs, NativeArray<byte> rendererVisibilityMasks, NativeArray<byte> rendererMeshLodSettings,
            NativeArray<byte> rendererCrossFadeValues, JobHandle cullingJobHandle)
        {
            NativeArray<bool> filteredRenderers = new NativeArray<bool>(sharedInstanceData.rendererGroupIDs.Length, Allocator.TempJob);
            EditorCameraUtils.GetRenderersFilteringResults(sharedInstanceData.rendererGroupIDs, filteredRenderers);
            var dummyExcludedRenderers = new NativeArray<int>(0, Allocator.TempJob);

            var drawOutputJob = new DrawCommandOutputFiltering
            {
                viewID = cc.viewID.GetInstanceID(),
                batchIDs = batchIDs,
                instanceDataBuffer = instanceDataBuffer,
                rendererVisibilityMasks = rendererVisibilityMasks,
                rendererMeshLodSettings = rendererMeshLodSettings,
                rendererCrossFadeValues = rendererCrossFadeValues,
                instanceData = instanceData,
                sharedInstanceData = sharedInstanceData,
                drawInstanceIndices = drawInstanceData.drawInstanceIndices,
                drawBatches = drawInstanceData.drawBatches,
                drawRanges = drawInstanceData.drawRanges,
                drawBatchIndices = drawInstanceData.drawBatchIndices,
                filteringResults = filteredRenderers,
                excludedRenderers = dummyExcludedRenderers,
                cullingOutput = cullingOutput.drawCommands,
                mode = FilteringJobMode.Filtering
            };

            var drawOutputHandle = drawOutputJob.Schedule(cullingJobHandle);

            filteredRenderers.Dispose(drawOutputHandle);
            dummyExcludedRenderers.Dispose(drawOutputHandle);

            return drawOutputHandle;
        }

        private JobHandle CreatePickingCullingOutputJob_EditorOnly(in BatchCullingContext cc, BatchCullingOutput cullingOutput,
            in CPUInstanceData.ReadOnly instanceData, in CPUSharedInstanceData.ReadOnly sharedInstanceData, in GPUInstanceDataBuffer.ReadOnly instanceDataBuffer,
            in CPUDrawInstanceData drawInstanceData, NativeParallelHashMap<uint, BatchID> batchIDs, NativeArray<byte> rendererVisibilityMasks,
            NativeArray<byte> rendererMeshLodSettings,
            NativeArray<byte> rendererCrossFadeValues, JobHandle cullingJobHandle)
        {
            // GPUResindetDrawer doesn't handle rendering of persistent game objects like prefabs. They are rendered by SRP.
            // When we are in prefab editing mode all the objects that are not part of the prefab should not be pickable.
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                return cullingJobHandle;

            var pickingIDs = HandleUtility.GetPickingIncludeExcludeList(Allocator.TempJob);
            var excludedRenderers = pickingIDs.ExcludeRenderers.IsCreated ? pickingIDs.ExcludeRenderers : new NativeArray<int>(0, Allocator.TempJob);
            var dummyFilteringResults = new NativeArray<bool>(0, Allocator.TempJob);

            var drawOutputJob = new DrawCommandOutputFiltering
            {
                viewID = cc.viewID.GetInstanceID(),
                batchIDs = batchIDs,
                instanceDataBuffer = instanceDataBuffer,
                rendererVisibilityMasks = rendererVisibilityMasks,
                rendererMeshLodSettings = rendererMeshLodSettings,
                rendererCrossFadeValues = rendererCrossFadeValues,
                instanceData = instanceData,
                sharedInstanceData = sharedInstanceData,
                drawInstanceIndices = drawInstanceData.drawInstanceIndices,
                drawBatches = drawInstanceData.drawBatches,
                drawRanges = drawInstanceData.drawRanges,
                drawBatchIndices = drawInstanceData.drawBatchIndices,
                filteringResults = dummyFilteringResults,
                excludedRenderers = excludedRenderers,
                cullingOutput = cullingOutput.drawCommands,
                mode = FilteringJobMode.Picking
            };

            var drawOutputHandle = drawOutputJob.Schedule(cullingJobHandle);
            drawOutputHandle.Complete();

            dummyFilteringResults.Dispose();
            if (!pickingIDs.ExcludeRenderers.IsCreated)
                excludedRenderers.Dispose();
            pickingIDs.Dispose();

            return drawOutputHandle;
        }

#endif
        public void InstanceOccludersUpdated(int viewInstanceID, int subviewMask, RenderersBatchersContext batchersContext)
        {
            if (m_DebugStats?.enabled ?? false)
            {
                var occlusionCullingCommon = batchersContext.occlusionCullingCommon;
                bool hasOccluders = occlusionCullingCommon.GetOccluderContext(viewInstanceID, out OccluderContext occluderCtx);
                if (hasOccluders)
                {
                    m_OcclusionEventDebugArray.TryAdd(
                        viewInstanceID,
                        InstanceOcclusionEventType.OccluderUpdate,
                        occluderCtx.version,
                        subviewMask,
                        OcclusionTest.None);
                }
            }
        }

        private void DisposeCompactVisibilityMasks()
        {
            if (m_CompactedVisibilityMasks.IsCreated)
            {
                Assert.IsTrue(m_CompactedVisibilityMasksJobsHandle.IsCompleted);
                m_CompactedVisibilityMasks.Dispose();
            }
        }

        private void DisposeSceneViewHiddenBits()
        {
#if UNITY_EDITOR
            if (m_SceneViewHiddenBits.IsCreated)
                m_SceneViewHiddenBits.Dispose();
#endif
        }

        public ParallelBitArray GetCompactedVisibilityMasks(bool syncCullingJobs)
        {
            if (syncCullingJobs)
                m_CompactedVisibilityMasksJobsHandle.Complete();

            return m_CompactedVisibilityMasks;
        }

        private class InstanceOcclusionTestPassData
        {
            public OcclusionCullingSettings settings;
            public InstanceOcclusionTestSubviewSettings subviewSettings;
            public OccluderHandles occluderHandles;
            public IndirectBufferContextHandles bufferHandles;
        }

        public void InstanceOcclusionTest(RenderGraph renderGraph, in OcclusionCullingSettings settings, ReadOnlySpan<SubviewOcclusionTest> subviewOcclusionTests, RenderersBatchersContext batchersContext)
        {
            if (!batchersContext.occlusionCullingCommon.GetOccluderContext(settings.viewInstanceID, out OccluderContext occluderCtx))
                return;

            var occluderHandles = occluderCtx.Import(renderGraph);
            if (!occluderHandles.IsValid())
                return;

            using (var builder = renderGraph.AddComputePass<InstanceOcclusionTestPassData>("Instance Occlusion Test", out var passData, m_ProfilingSampleInstanceOcclusionTest))
            {
                builder.AllowGlobalStateModification(true);

                passData.settings = settings;
                passData.subviewSettings = InstanceOcclusionTestSubviewSettings.FromSpan(subviewOcclusionTests);
                passData.bufferHandles = m_IndirectStorage.ImportBuffers(renderGraph);
                passData.occluderHandles = occluderHandles;

                passData.bufferHandles.UseForOcclusionTest(builder);
                passData.occluderHandles.UseForOcclusionTest(builder);

                builder.SetRenderFunc((InstanceOcclusionTestPassData data, ComputeGraphContext context) =>
                {
                    var batcher = GPUResidentDrawer.instance.batcher;
                    batcher.instanceCullingBatcher.culler.AddOcclusionCullingDispatch(
                        context.cmd,
                        data.settings,
                        data.subviewSettings,
                        data.bufferHandles,
                        data.occluderHandles,
                        batcher.batchersContext);
                });
            }
        }

        internal void EnsureValidOcclusionTestResults(int viewInstanceID)
        {
            int indirectContextIndex = m_IndirectStorage.TryGetContextIndex(viewInstanceID);
            if (indirectContextIndex >= 0)
            {
                // sync before checking the allocation results
                IndirectBufferContext bufferCtx = m_IndirectStorage.GetBufferContext(indirectContextIndex);
                if (bufferCtx.bufferState == IndirectBufferContext.BufferState.Pending)
                    bufferCtx.cullingJobHandle.Complete();

                // if this did allocate, then ensure the indirect args start with valid data that renders everything
                IndirectBufferAllocInfo allocInfo = m_IndirectStorage.GetAllocInfo(indirectContextIndex);
                if (!allocInfo.IsEmpty())
                {
                    var cmd = m_CommandBuffer;

                    cmd.Clear();
                    m_IndirectStorage.CopyFromStaging(cmd, allocInfo);

                    var cs = m_OcclusionTestShader.cs;

                    m_ShaderVariables[0] = new InstanceOcclusionCullerShaderVariables
                    {
                        _DrawInfoAllocIndex = (uint)allocInfo.drawAllocIndex,
                        _DrawInfoCount = (uint)allocInfo.drawCount,
                        _InstanceInfoAllocIndex = (uint)(IndirectBufferContextStorage.kInstanceInfoGpuOffsetMultiplier * allocInfo.instanceAllocIndex),
                        _InstanceInfoCount = (uint)allocInfo.instanceCount,
                        _BoundingSphereInstanceDataAddress = 0,
                        _DebugCounterIndex = -1,
                        _InstanceMultiplierShift = 0,
                    };
                    cmd.SetBufferData(m_ConstantBuffer, m_ShaderVariables);
                    cmd.SetComputeConstantBufferParam(cs, ShaderIDs.InstanceOcclusionCullerShaderVariables, m_ConstantBuffer, 0, m_ConstantBuffer.stride);

                    int kernel = m_CopyInstancesKernel;
                    cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._DrawInfo, m_IndirectStorage.drawInfoBuffer);
                    cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._InstanceInfo, m_IndirectStorage.instanceInfoBuffer);
                    cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._DrawArgs, m_IndirectStorage.argsBuffer);
                    cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._InstanceIndices, m_IndirectStorage.instanceBuffer);

                    cmd.DispatchCompute(cs, kernel, (allocInfo.instanceCount + 63) / 64, 1, 1);

                    Graphics.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }
            }
        }

        private void AddOcclusionCullingDispatch(
            ComputeCommandBuffer cmd,
            in OcclusionCullingSettings settings,
            in InstanceOcclusionTestSubviewSettings subviewSettings,
            in IndirectBufferContextHandles bufferHandles,
            in OccluderHandles occluderHandles,
            RenderersBatchersContext batchersContext)
        {
            var occlusionCullingCommon = batchersContext.occlusionCullingCommon;
            int indirectContextIndex = m_IndirectStorage.TryGetContextIndex(settings.viewInstanceID);
            if (indirectContextIndex >= 0)
            {
                IndirectBufferContext bufferCtx = m_IndirectStorage.GetBufferContext(indirectContextIndex);

                // check what compute we need to do (if any)
                bool hasOccluders = occlusionCullingCommon.GetOccluderContext(settings.viewInstanceID, out OccluderContext occluderCtx);

                // check we have occluders for all the required subviews, disable the occlusion test if not
                hasOccluders = hasOccluders && ((subviewSettings.occluderSubviewMask & occluderCtx.subviewValidMask) == subviewSettings.occluderSubviewMask);

                IndirectBufferContext.BufferState newBufferState = IndirectBufferContext.BufferState.Zeroed;
                int newOccluderVersion = 0;
                int newSubviewMask = 0;
                switch (settings.occlusionTest)
                {
                    case OcclusionTest.None:
                        newBufferState = IndirectBufferContext.BufferState.NoOcclusionTest;
                        break;
                    case OcclusionTest.TestAll:
                        if (hasOccluders)
                        {
                            newBufferState = IndirectBufferContext.BufferState.AllInstancesOcclusionTested;
                            newOccluderVersion = occluderCtx.version;
                            newSubviewMask = subviewSettings.occluderSubviewMask;
                        }
                        else
                        {
                            newBufferState = IndirectBufferContext.BufferState.NoOcclusionTest;
                        }
                        break;
                    case OcclusionTest.TestCulled:
                        if (hasOccluders)
                        {
                            bool hasMatchingCullingOutput = true;
                            switch (bufferCtx.bufferState)
                            {
                                case IndirectBufferContext.BufferState.AllInstancesOcclusionTested:
                                case IndirectBufferContext.BufferState.OccludedInstancesReTested:
                                    // valid or already done
                                    if (bufferCtx.subviewMask != subviewSettings.occluderSubviewMask)
                                    {
                                        Debug.Log("Expected an occlusion test of TestCulled to use the same subview mask as the previous occlusion test");
                                        hasMatchingCullingOutput = false;
                                    }
                                    break;

                                case IndirectBufferContext.BufferState.NoOcclusionTest:
                                case IndirectBufferContext.BufferState.Zeroed:
                                    // no instances, keep the new buffer state zeroed
                                    hasMatchingCullingOutput = false;
                                    break;

                                default:
                                    // unexpected, keep the new buffer state zeroed
                                    hasMatchingCullingOutput = false;
                                    Debug.Log("Expected the previous occlusion test to be TestAll before using TestCulled");
                                    break;
                            }
                            if (hasMatchingCullingOutput)
                            {
                                newBufferState = IndirectBufferContext.BufferState.OccludedInstancesReTested;
                                newOccluderVersion = occluderCtx.version;
                                newSubviewMask = subviewSettings.occluderSubviewMask;
                            }
                        }
                        break;
                }

                // issue the work (if any)
                if (!bufferCtx.Matches(newBufferState, newOccluderVersion, newSubviewMask))
                {
                    bool isFirstPass = (newBufferState == IndirectBufferContext.BufferState.AllInstancesOcclusionTested);
                    bool isSecondPass = (newBufferState == IndirectBufferContext.BufferState.OccludedInstancesReTested);

                    bool doWait = (bufferCtx.bufferState == IndirectBufferContext.BufferState.Pending);
                    bool doCopyInstances = (newBufferState == IndirectBufferContext.BufferState.NoOcclusionTest);
                    bool doResetDraws = (bufferCtx.bufferState != IndirectBufferContext.BufferState.Zeroed) && !doCopyInstances;
                    bool doCullInstances = (newBufferState != IndirectBufferContext.BufferState.Zeroed) && !doCopyInstances;

                    // sync before checking the allocation results
                    if (doWait)
                        bufferCtx.cullingJobHandle.Complete();

                    IndirectBufferAllocInfo allocInfo = m_IndirectStorage.GetAllocInfo(indirectContextIndex);

                    bufferCtx.bufferState = newBufferState;
                    bufferCtx.occluderVersion = newOccluderVersion;
                    bufferCtx.subviewMask = newSubviewMask;

                    if (!allocInfo.IsEmpty())
                    {
                        int debugCounterIndex = -1;
                        if (m_DebugStats?.enabled ?? false)
                        {
                            debugCounterIndex = m_OcclusionEventDebugArray.TryAdd(
                                settings.viewInstanceID,
                                InstanceOcclusionEventType.OcclusionTest,
                                newOccluderVersion,
                                newSubviewMask,
                                isFirstPass ? OcclusionTest.TestAll : isSecondPass ? OcclusionTest.TestCulled : OcclusionTest.None);
                        }

                        // set up keywords
                        bool occlusionDebug = false;
                        if (isFirstPass || isSecondPass)
                        {
                            occlusionDebug = OcclusionCullingCommon.UseOcclusionDebug(in occluderCtx) && occluderHandles.occlusionDebugOverlay.IsValid();
                        }
                        var cs = m_OcclusionTestShader.cs;
                        var firstPassKeyword = new LocalKeyword(cs, "OCCLUSION_FIRST_PASS");
                        var secondPassKeyword = new LocalKeyword(cs, "OCCLUSION_SECOND_PASS");
                        OccluderContext.SetKeyword(cmd, cs, firstPassKeyword, isFirstPass);
                        OccluderContext.SetKeyword(cmd, cs, secondPassKeyword, isSecondPass);

                        m_ShaderVariables[0] = new InstanceOcclusionCullerShaderVariables
                        {
                            _DrawInfoAllocIndex = (uint)allocInfo.drawAllocIndex,
                            _DrawInfoCount = (uint)allocInfo.drawCount,
                            _InstanceInfoAllocIndex = (uint)(IndirectBufferContextStorage.kInstanceInfoGpuOffsetMultiplier * allocInfo.instanceAllocIndex),
                            _InstanceInfoCount = (uint)allocInfo.instanceCount,
                            _BoundingSphereInstanceDataAddress = batchersContext.renderersParameters.boundingSphere.gpuAddress,
                            _DebugCounterIndex = debugCounterIndex,
                            _InstanceMultiplierShift = (settings.instanceMultiplier == 2) ? 1 : 0,
                        };
                        cmd.SetBufferData(m_ConstantBuffer, m_ShaderVariables);
                        cmd.SetComputeConstantBufferParam(cs, ShaderIDs.InstanceOcclusionCullerShaderVariables, m_ConstantBuffer, 0, m_ConstantBuffer.stride);

                        occlusionCullingCommon.PrepareCulling(cmd, in occluderCtx, settings, subviewSettings, m_OcclusionTestShader, occlusionDebug);

                        if (doCopyInstances)
                        {
                            int kernel = m_CopyInstancesKernel;
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._DrawInfo, m_IndirectStorage.drawInfoBuffer);
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._InstanceInfo, m_IndirectStorage.instanceInfoBuffer);
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._DrawArgs, m_IndirectStorage.argsBuffer);
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._InstanceIndices, m_IndirectStorage.instanceBuffer);

                            cmd.DispatchCompute(cs, kernel, (allocInfo.instanceCount + 63) / 64, 1, 1);
                        }

                        if (doResetDraws)
                        {
                            int kernel = m_ResetDrawArgsKernel;
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._DrawInfo, bufferHandles.drawInfoBuffer);
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._DrawArgs, bufferHandles.argsBuffer);
                            cmd.DispatchCompute(cs, kernel, (allocInfo.drawCount + 63) / 64, 1, 1);
                        }

                        if (doCullInstances)
                        {
                            int kernel = m_CullInstancesKernel;
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._DrawInfo, bufferHandles.drawInfoBuffer);
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._InstanceInfo, bufferHandles.instanceInfoBuffer);
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._DrawArgs, bufferHandles.argsBuffer);
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._InstanceIndices, bufferHandles.instanceBuffer);
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._InstanceDataBuffer, batchersContext.gpuInstanceDataBuffer);
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._OcclusionDebugCounters, m_OcclusionEventDebugArray.CounterBuffer);

                            if (isFirstPass || isSecondPass)
                                OcclusionCullingCommon.SetDepthPyramid(cmd, m_OcclusionTestShader, kernel, occluderHandles);

                            if (occlusionDebug)
                                OcclusionCullingCommon.SetDebugPyramid(cmd, m_OcclusionTestShader, kernel, occluderHandles);

                            if (isSecondPass)
                                cmd.DispatchCompute(cs, kernel, bufferHandles.argsBuffer, (uint)(GraphicsBuffer.IndirectDrawIndexedArgs.size * allocInfo.GetExtraDrawInfoSlotIndex()));
                            else
                                cmd.DispatchCompute(cs, kernel, (allocInfo.instanceCount + 63) / 64, 1, 1);
                        }
                    }
                }

                // update to the new buffer state
                m_IndirectStorage.SetBufferContext(indirectContextIndex, bufferCtx);
            }
        }

        private void FlushDebugCounters()
        {
            if (m_DebugStats?.enabled ?? false)
            {
                m_SplitDebugArray.MoveToDebugStatsAndClear(m_DebugStats);
                m_OcclusionEventDebugArray.MoveToDebugStatsAndClear(m_DebugStats);
                m_DebugStats.FinalizeInstanceCullerViewStats();
            }
        }

        private void OnBeginSceneViewCameraRendering()
        {
#if UNITY_EDITOR
            m_IsSceneViewCamera = true;
#endif
        }

        private void OnEndSceneViewCameraRendering()
        {
#if UNITY_EDITOR
            m_IsSceneViewCamera = false;
#endif
        }

        public void UpdateFrame(int cameraCount)
        {
            DisposeCompactVisibilityMasks();
            if (cameraCount > m_LODParamsToCameraID.Capacity)
                m_LODParamsToCameraID.Capacity = cameraCount;
            m_LODParamsToCameraID.Clear();
            FlushDebugCounters();
            m_IndirectStorage.ClearContextsAndGrowBuffers();
        }

        public void OnBeginCameraRendering(Camera camera)
        {
            if (camera.cameraType == CameraType.SceneView)
                OnBeginSceneViewCameraRendering();
        }

        public void OnEndCameraRendering(Camera camera)
        {
            if (camera.cameraType == CameraType.SceneView)
                OnEndSceneViewCameraRendering();
        }

        public void Dispose()
        {
            DisposeSceneViewHiddenBits();
            DisposeCompactVisibilityMasks();
            m_IndirectStorage.Dispose();
            m_DebugStats = null;
            m_OcclusionEventDebugArray.Dispose();
            m_SplitDebugArray.Dispose();
            m_ShaderVariables.Dispose();
            m_ConstantBuffer.Release();
            m_CommandBuffer.Dispose();
            m_LODParamsToCameraID.Dispose();
        }
    }
}
