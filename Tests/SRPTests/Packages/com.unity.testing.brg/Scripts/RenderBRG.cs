#if UNITY_EDITOR
#define ENABLE_PICKING
#define ENABLE_ERROR_LOADING_MATERIALS
#endif

using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using FrustumPlanes = Unity.Rendering.FrustumPlanes.FrustumPlanes;

public struct RangeKey : IEquatable<RangeKey>, IComparable<RangeKey>
{
    public int rendererPriority;
    public ShadowCastingMode shadows;

    public override int GetHashCode()
    {
        return HashCode.Combine(rendererPriority, shadows);
    }

    public int CompareTo(RangeKey other)
    {
        int cmp_rendererPriority = rendererPriority.CompareTo(other.rendererPriority);
        int cmp_shadows          = shadows.CompareTo(other.shadows);

        if (cmp_rendererPriority != 0) return cmp_rendererPriority;
        return cmp_shadows;
    }

    public bool Equals(RangeKey other) => CompareTo(other) == 0;
}

public struct DrawRange
{
    public RangeKey key;
    public int drawCount;
    public int drawOffset;
}

public struct DrawKey : IEquatable<DrawKey>, IComparable<DrawKey>
{
    public RangeKey rangeKey;
    public BatchID batchID;
    public BatchMeshID meshID;
    public uint submeshIndex;
    public BatchMaterialID material;
    public int transparentInstanceID;

    public override int GetHashCode()
    {
        return HashCode.Combine(rangeKey, batchID, meshID, submeshIndex, material, transparentInstanceID);
    }

    public int CompareTo(DrawKey other)
    {
        int cmp_range    = rangeKey.CompareTo(other.rangeKey);
        int cmp_material = material.CompareTo(other.material);
        int cmp_mesh     = meshID.CompareTo(other.meshID);
        int cmp_submesh  = submeshIndex.CompareTo(other.submeshIndex);
        int cmp_iid      = transparentInstanceID.CompareTo(other.transparentInstanceID);
        int cmp_batch    = batchID.CompareTo(other.batchID);

        if (cmp_range    != 0) return cmp_range;
        if (cmp_material != 0) return cmp_material;
        if (cmp_mesh     != 0) return cmp_mesh;
        if (cmp_submesh  != 0) return cmp_submesh;
        if (cmp_iid      != 0) return cmp_iid;
        return cmp_batch;
    }
    public bool Equals(DrawKey other) => CompareTo(other) == 0;

    public bool isTransparent { get { return transparentInstanceID != 0; } }

    public BatchDrawCommandFlags drawCommandFlags { get {
        var flags = BatchDrawCommandFlags.None;
        if (isTransparent)
            flags |= BatchDrawCommandFlags.HasSortingPosition;
        return flags;
    } }
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
    public int rendererIndex;
}
public struct DrawRenderer
{
    public AABB bounds;
    public DrawRendererFlags flags;
}

[Flags]
public enum DrawRendererFlags
{
    None = 0,
    AffectsLightmaps = 1 << 0,    // is lightmapped or influence-only
}

public unsafe class RenderBRG : MonoBehaviour
{
    public bool SetFallbackMaterialsOnStart = true;

    private BatchRendererGroup m_BatchRendererGroup;
    private GraphicsBuffer m_GPUPersistentInstanceData;
    private GraphicsBuffer m_Globals;

    private bool m_initialized;

    private NativeParallelHashMap<RangeKey, int> m_rangeHash;
    private NativeList<DrawRange> m_drawRanges;

    private NativeParallelHashMap<DrawKey, int> m_batchHash;
    private NativeList<DrawBatch> m_drawBatches;

    private NativeList<DrawInstance> m_instances;
    private NativeArray<int> m_instanceIndices;
    private NativeArray<int> m_rendererIndices;
    private NativeArray<int> m_drawIndices;
    private NativeArray<DrawRenderer> m_renderers;
    private NativeArray<int> m_pickingIDs;

    private int m_maxInstancesPerBatch;
    private BatchBufferTarget m_brgBufferTarget;
    private uint m_brgBatchWindowSize;
    private bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;

    public bool useBatchLayer = false;
    public int batchLayer = 0;

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
        public bool cullLightmapShadowCasters;

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<FrustumPlanes.PlanePacket4> planes;

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<int> splitCounts;

        [ReadOnly]
        public NativeArray<DrawRenderer> renderers;

        [WriteOnly]
        public NativeArray<ulong> rendererVisibility;

        public void Execute(int index)
        {
            // Each invocation is culling 8 renderers (8 split bits * 8 renderers = 64 bit bitfield)
            int start = index * 8;
            int end = math.min(start + 8, renderers.Length);

            ulong visibleBits = 0;
            for (int i = start; i < end; i++)
            {
                var renderer = renderers[i];

                if (cullLightmapShadowCasters && (renderer.flags & DrawRendererFlags.AffectsLightmaps) != 0)
                    continue;

                ulong splitMask = FrustumPlanes.Intersect2NoPartialMulti(planes, splitCounts, renderer.bounds);
                visibleBits |= splitMask << (8 * (i - start));
            }

            rendererVisibility[index] = visibleBits;
        }
    }

    [BurstCompile]
    private struct DrawCommandOutputJob : IJob
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<ulong> rendererVisibility;

        [ReadOnly]
        public NativeArray<int> instanceIndices;

        [ReadOnly]
        public NativeArray<int> rendererIndices;

        [ReadOnly]
        public NativeArray<DrawRenderer> renderers;

        [ReadOnly]
        public NativeList<DrawBatch> drawBatches;

        [ReadOnly]
        public NativeList<DrawRange> drawRanges;

        [ReadOnly]
        public NativeArray<int> drawIndices;

        public NativeArray<BatchCullingOutputDrawCommands> drawCommands;

        [ReadOnly]
        public NativeArray<int> pickingIDs;
        public bool isPickingCulling;
        public int batchLayer;

        public void Execute()
        {
            var draws = drawCommands[0];

            if (isPickingCulling)
            {
                drawCommands[0] = OutputPickingDrawCommands(draws);
                return;
            }

            if (draws.drawCommands == null)
                draws = CountAndAllocateDrawCommands();

            int outIndex = 0;
            int outBatch = 0;
            int outRange = 0;
            int outSortingPosition = 0;

            int activeBatch = 0;
            int internalIndex = 0;
            int batchStartIndex = 0;

            int activeRange = 0;
            int internalDraw = 0;
            int rangeStartIndex = 0;

            uint visibleMaskPrev = 0;

            for (int i = 0; i < instanceIndices.Length; i++)
            {
                var remappedIndex = drawIndices[activeBatch];   // DrawIndices remap to get DrawCommands ordered by DrawRange
                var rendererIndex = rendererIndices[i];
                uint visibleMask = (uint)((rendererVisibility[rendererIndex / 8] >> ((rendererIndex % 8) * 8)) & 0xfful);

                if (visibleMask != 0)
                {
                    // Emit extra DrawCommand if visible mask changes
                    // TODO: Sort draws by visibilityMask in batch first to minimize the number of DrawCommands
                    if (visibleMask != visibleMaskPrev)
                    {
                        var visibleCount = outIndex - batchStartIndex;
                        if (visibleCount > 0)
                        {
                            var key = drawBatches[remappedIndex].key;
                            int sortingPosition = 0;
                            if (key.isTransparent)
                            {
                                sortingPosition = outSortingPosition;
                                outSortingPosition += 3;

                                var center = renderers[rendererIndex].bounds.Center;
                                draws.instanceSortingPositions[sortingPosition] = center.x;
                                draws.instanceSortingPositions[sortingPosition + 1] = center.y;
                                draws.instanceSortingPositions[sortingPosition + 2] = center.z;
                            }
                            draws.drawCommands[outBatch] = new BatchDrawCommand
                            {
                                visibleOffset = (uint)batchStartIndex,
                                visibleCount = (uint)visibleCount,
                                batchID = key.batchID,
                                materialID = key.material,
                                meshID = key.meshID,
                                submeshIndex = (ushort)key.submeshIndex,
                                splitVisibilityMask = (ushort)visibleMaskPrev,
                                flags = key.drawCommandFlags,
                                sortingPosition = sortingPosition
                            };
                            outBatch++;
                        }
                        batchStartIndex = outIndex;
                    }
                    visibleMaskPrev = visibleMask;

                    // Insert the visible instance to the array
                    draws.visibleInstances[outIndex] = instanceIndices[i];
                    outIndex++;
                }
                internalIndex++;

                // Next draw batch?
                if (internalIndex == drawBatches[remappedIndex].instanceCount)
                {
                    var visibleCount = outIndex - batchStartIndex;
                    if (visibleCount > 0)
                    {
                        var key = drawBatches[remappedIndex].key;
                        int sortingPosition = 0;
                        if (key.isTransparent)
                        {
                            var center = renderers[rendererIndex].bounds.Center;
                            sortingPosition = 3*outBatch;
                            draws.instanceSortingPositions[sortingPosition] = center.x;
                            draws.instanceSortingPositions[sortingPosition + 1] = center.y;
                            draws.instanceSortingPositions[sortingPosition + 2] = center.z;
                        }
                        draws.drawCommands[outBatch] = new BatchDrawCommand
                        {
                            visibleOffset = (uint)batchStartIndex,
                            visibleCount = (uint)visibleCount,
                            batchID = key.batchID,
                            materialID = key.material,
                            meshID = key.meshID,
                            submeshIndex = (ushort)key.submeshIndex,
                            splitVisibilityMask = (ushort)visibleMaskPrev,
                            flags = key.drawCommandFlags,
                            sortingPosition = sortingPosition
                        };
                        outBatch++;
                    }

                    visibleMaskPrev = 0;
                    batchStartIndex = outIndex;
                    internalIndex = 0;
                    activeBatch++;
                    internalDraw++;

                    // Next draw range?
                    if (internalDraw == drawRanges[activeRange].drawCount)
                    {
                        var visibleDrawCount = outBatch - rangeStartIndex;
                        if (visibleDrawCount > 0)
                        {
                            draws.drawRanges[outRange] = new BatchDrawRange
                            {
                                drawCommandsBegin = (uint)rangeStartIndex,
                                drawCommandsCount = (uint)visibleDrawCount,
                                filterSettings = new BatchFilterSettings
                                {
                                    rendererPriority = drawRanges[activeRange].key.rendererPriority,
                                    renderingLayerMask = 1,
                                    layer = 1,
                                    batchLayer = (byte)batchLayer,
                                    motionMode = MotionVectorGenerationMode.Camera,
                                    shadowCastingMode = drawRanges[activeRange].key.shadows,
                                    receiveShadows = true,
                                    staticShadowCaster = false,
                                    allDepthSorted = false,
                                },
                            };
                            outRange++;
                        }

                        rangeStartIndex = outBatch;
                        internalDraw = 0;
                        activeRange++;
                    }
                }
            }

            draws.drawCommandCount = outBatch;
            draws.instanceSortingPositionFloatCount = outSortingPosition;
            draws.visibleInstanceCount = outIndex;
            draws.drawRangeCount = outRange;
            drawCommands[0] = draws;
        }

        private BatchCullingOutputDrawCommands CountAndAllocateDrawCommands()
        {
            int activeBatch = 0;
            uint visibleMaskPrev = 0;
            int outIndex = 0;
            int outBatch = 0;
            int batchStartIndex = 0;
            int internalIndex = 0;

            // The loop structure of the output loop is such that it's hard to count without
            // replicating the loop logic, so replicate the loop logic.

            for (int i = 0; i < instanceIndices.Length; i++)
            {
                var remappedIndex = drawIndices[activeBatch];   // DrawIndices remap to get DrawCommands ordered by DrawRange
                var rendererIndex = rendererIndices[i];
                uint visibleMask = (uint)((rendererVisibility[rendererIndex / 8] >> ((rendererIndex % 8) * 8)) & 0xfful);

                if (visibleMask != 0)
                {
                    // Emit extra DrawCommand if visible mask changes
                    // TODO: Sort draws by visibilityMask in batch first to minimize the number of DrawCommands
                    if (visibleMask != visibleMaskPrev)
                    {
                        var visibleCount = outIndex - batchStartIndex;
                        if (visibleCount > 0)
                        {
                            outBatch++;
                        }
                        batchStartIndex = outIndex;
                    }
                    visibleMaskPrev = visibleMask;

                    // Insert the visible instance to the array
                    outIndex++;
                }
                internalIndex++;

                // Next draw batch?
                if (internalIndex == drawBatches[remappedIndex].instanceCount)
                {
                    var visibleCount = outIndex - batchStartIndex;
                    if (visibleCount > 0)
                    {
                        outBatch++;
                    }

                    visibleMaskPrev = 0;
                    batchStartIndex = outIndex;
                    internalIndex = 0;
                    activeBatch++;
                }
            }

            int numDrawCommands = outBatch;

            var draws = (BatchCullingOutputDrawCommands*)drawCommands.GetUnsafePtr();
            draws->drawCommands = Malloc<BatchDrawCommand>(numDrawCommands);
            draws->instanceSortingPositions = Malloc<float>(3 * numDrawCommands);
            return *draws;
        }

        private BatchCullingOutputDrawCommands OutputPickingDrawCommands(BatchCullingOutputDrawCommands draws)
        {
#if !ENABLE_PICKING
            return draws;
#endif

            // In picking mode, output a single draw range with a dedicated draw command for each visible renderer
            int drawCommandIndex = 0;
            for (int i = 0; i < drawBatches.Length; i++)
            {
                var drawBatch = drawBatches[i];
                for (int j = 0; j < drawBatch.instanceCount; ++j)
                {
                    int instanceIndex = instanceIndices[drawBatch.instanceOffset + j];
                    int rendererIndex = rendererIndices[drawBatch.instanceOffset + j];

                    uint visibleMask = (uint)((rendererVisibility[rendererIndex / 8] >> ((rendererIndex % 8) * 8)) & 0xfful);
                    if (visibleMask != 0)
                    {
                        draws.drawCommands[drawCommandIndex] = new BatchDrawCommand
                        {
                            visibleOffset = (uint)drawCommandIndex,
                            visibleCount = 1,
                            batchID = drawBatch.key.batchID,
                            meshID = drawBatch.key.meshID,
                            submeshIndex = (ushort)drawBatch.key.submeshIndex,
                            materialID = drawBatch.key.material,
                            splitVisibilityMask = 0xffff,
                            sortingPosition = 0,
                            flags = BatchDrawCommandFlags.None,
                        };
                        draws.visibleInstances[drawCommandIndex] = instanceIndex;
                        draws.drawCommandPickingInstanceIDs[drawCommandIndex] = pickingIDs[rendererIndex];
                        ++drawCommandIndex;
                    }
                }
            }
            draws.visibleInstanceCount = drawCommandIndex;
            draws.drawCommandCount = drawCommandIndex;

            draws.drawRangeCount = 1;
            draws.drawRanges[0].filterSettings =
                new BatchFilterSettings
                {
                    renderingLayerMask = 1,
                    layer = 1,
                    batchLayer = (byte)batchLayer,
                    motionMode = MotionVectorGenerationMode.Camera,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = true,
                    staticShadowCaster = false,
                    allDepthSorted = false,
                };
            draws.drawRanges[0].drawCommandsBegin = 0;
            draws.drawRanges[0].drawCommandsCount = (uint)draws.drawCommandCount;

            return draws;
        }
    }

    public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
    {
        if (!m_initialized)
        {
            return new JobHandle();
        }

        bool needInstanceIDs = cullingContext.viewType == BatchCullingViewType.Picking ||
                               cullingContext.viewType == BatchCullingViewType.SelectionOutline;

        bool cullLightmapShadowCasters = (cullingContext.cullingFlags & BatchCullingFlags.CullLightmappedShadowCasters) != 0;

        var splitCounts = new NativeArray<int>(cullingContext.cullingSplits.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < splitCounts.Length; ++i)
        {
            var split = cullingContext.cullingSplits[i];
            splitCounts[i] = split.cullingPlaneCount;
        }

        var planes = FrustumPlanes.BuildSOAPlanePacketsMulti(cullingContext.cullingPlanes, splitCounts, Allocator.TempJob);

        BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();
        drawCommands.drawRanges = Malloc<BatchDrawRange>(m_drawRanges.Length);

        // When culling for picking, output one draw command per renderer, so each one
        // can have a unique picking ID
        int maxDrawCommands = needInstanceIDs ? m_instanceIndices.Length : m_drawBatches.Length;

        // If splits are involved, defer allocation until we know exactly how many we will need
        if (splitCounts.Length > 1)
        {
            drawCommands.drawCommands = null;
            drawCommands.instanceSortingPositions = null;
        }
        else
        {
            drawCommands.drawCommands = Malloc<BatchDrawCommand>(maxDrawCommands);
            drawCommands.instanceSortingPositions = Malloc<float>(3 * maxDrawCommands);
        }

        drawCommands.visibleInstances = Malloc<int>(m_instanceIndices.Length);
        drawCommands.drawCommandPickingInstanceIDs = needInstanceIDs ? Malloc<int>(m_instanceIndices.Length) : null;

        // Zero init: Culling job sets the values!
        drawCommands.drawRangeCount = 0;
        drawCommands.drawCommandCount = 0;
        drawCommands.visibleInstanceCount = 0;
        drawCommands.instanceSortingPositionFloatCount = 0;

        cullingOutput.drawCommands[0] = drawCommands;

        var visibilityLength = (m_renderers.Length + 7) / 8;
        var rendererVisibility = new NativeArray<ulong>(visibilityLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        var cullingJob = new CullingJob
        {
            cullLightmapShadowCasters = cullLightmapShadowCasters,
            planes = planes,
            splitCounts = splitCounts,
            renderers = m_renderers,
            rendererVisibility = rendererVisibility
        };

        var drawOutputJob = new DrawCommandOutputJob
        {
            rendererVisibility = rendererVisibility,
            instanceIndices = m_instanceIndices,
            rendererIndices = m_rendererIndices,
            renderers = m_renderers,
            drawBatches = m_drawBatches,
            drawRanges = m_drawRanges,
            drawIndices = m_drawIndices,
            drawCommands = cullingOutput.drawCommands,
            isPickingCulling = needInstanceIDs,
            pickingIDs = m_pickingIDs,
            batchLayer = useBatchLayer ? batchLayer : 0
        };

        var jobHandleCulling = cullingJob.Schedule(visibilityLength, 8);
        var jobHandleOutput = drawOutputJob.Schedule(jobHandleCulling);

        return jobHandleOutput;
    }

    static Material LoadMaterialWithHideAndDontSave(string name)
    {
        Shader shader = Shader.Find(name);

        if (shader == null) return null;

        Material material = new Material(shader);

        // Prevent Material unloading when switching scene
        material.hideFlags = HideFlags.HideAndDontSave;

        return material;
    }

    Material m_PickingMaterial;
    Material m_ErrorMaterial;
    Material m_LoadingMaterial;

    private int RoundToNextMultipleOf(int x, int y) => (x + y - 1) / y * y;

    struct BRGBatchAllocator
    {
        private int[] m_PropertySizes;

        public BRGBatchAllocator(params int[] propertySizes)
        {
            m_PropertySizes = propertySizes;
        }

        private int NextMultipleOf16(int x) => ((x + 15) >> 4) << 4;
        private int NextMultipleOf(int x, int y) => ((x + y - 1) / y * y);

        public int NumProperties => m_PropertySizes.Length;

        public int NumBytesForProperty(int numInstances, int propertyIndex) =>
            NextMultipleOf16(m_PropertySizes[propertyIndex] * numInstances);

        public int NumBytesForBatch(int numInstances)
        {
            int bytes = 0;
            for (int i = 0; i < NumProperties; ++i)
                bytes += NumBytesForProperty(numInstances, i);
            return bytes;
        }

        public struct BatchAllocation : IDisposable
        {
            public NativeArray<int> PropertyOffsets;
            public int AllocationBegin;
            public int AllocationSize;
            public int AllocationEnd => AllocationBegin + AllocationSize;

            public int OffsetOfPropertyFromBegin(int propertyIndex) => PropertyOffsets[propertyIndex];
            public int BufferOffsetOfProperty(int propertyIndex) => OffsetOfPropertyFromBegin(propertyIndex) + AllocationBegin;
            public int MetadataOffsetOfProperty(int propertyIndex, BatchBufferTarget bufferTarget = BatchBufferTarget.Unknown)
            {
                // UBO mode uses indexing from the UBO window start
                // SSBO mode uses indexing from the buffer start
                if (bufferTarget != BatchBufferTarget.ConstantBuffer)
                    return BufferOffsetOfProperty(propertyIndex);
                else
                    return OffsetOfPropertyFromBegin(propertyIndex);
            }

            public void Dispose()
            {
                PropertyOffsets.Dispose();
            }

            public int BatchBufferOffset(BatchBufferTarget bufferTarget = BatchBufferTarget.Unknown)
            {
                return (bufferTarget == BatchBufferTarget.ConstantBuffer)
                    ? AllocationBegin
                    : 0;
            }
        }

        public BatchAllocation AllocateInstances(Allocator allocator, int numInstances, int allocationBegin = 0, int constantBufferAlignment = 0)
        {
            var allocation = new BatchAllocation
            {
                PropertyOffsets = new NativeArray<int>(NumProperties, allocator),
                AllocationBegin = allocationBegin,
                AllocationSize = 0,
            };

            int propertyOffset = 0;
            for (int i = 0; i < NumProperties; ++i)
            {
                allocation.PropertyOffsets[i] = propertyOffset;
                int sizeBytes = NumBytesForProperty(numInstances, i);
                propertyOffset += sizeBytes;
            }

            allocation.AllocationSize = propertyOffset;
            if (constantBufferAlignment > 0)
                allocation.AllocationSize = NextMultipleOf(allocation.AllocationSize, constantBufferAlignment);

            return allocation;
        }

        public int InstancePropertyBufferOffset(
            BatchAllocation allocation,
            int propertyIndex,
            int instanceIndex) =>
            allocation.BufferOffsetOfProperty(propertyIndex) +
            instanceIndex * m_PropertySizes[propertyIndex];

        public int MaxInstancesForConstantBufferSize(uint constantBufferMaxSize, uint constantBufferAlignment)
        {
            int sumOfPropertySizes = 0;
            for (int i = 0; i < m_PropertySizes.Length; ++i)
                sumOfPropertySizes += m_PropertySizes[i];

            if (sumOfPropertySizes == 0)
                return 0;

            // Property size specifies the upper bound of how many instances can fit
            // in a single constant buffer.
            int upperBound = (int)constantBufferMaxSize / sumOfPropertySizes;

            // We have to test for alignment restrictions, so keep decreasing the count
            // until we find a count that actually fits.
            int numInstances = upperBound;
            while (numInstances > 0)
            {
                int batchSize = NumBytesForBatch(numInstances);
                int alignedBatchSize = NextMultipleOf(batchSize, (int)constantBufferAlignment);

                if (alignedBatchSize <= constantBufferMaxSize)
                    return numInstances;

                --numInstances;
            }

            return 0;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

        const int kFloat4Size = 16;

        uint kBRGBufferMaxWindowSize = 128 * 1024 * 1024;
        uint kBRGBufferAlignment = 16;
        if (UseConstantBuffer)
        {
            kBRGBufferMaxWindowSize = (uint)(BatchRendererGroup.GetConstantBufferMaxWindowSize());
            kBRGBufferAlignment = (uint)(BatchRendererGroup.GetConstantBufferOffsetAlignment());
        }

#if ENABLE_PICKING
#if HDRP_7_0_0_OR_NEWER
        var pickingName = "Hidden/HDRP/BRGPicking";
#elif URP_7_0_0_OR_NEWER
        var pickingName ="Hidden/Universal Render Pipeline/BRGPicking";
#else
    #error "You need either HDRP or URP package to use this script"
#endif
        m_PickingMaterial = LoadMaterialWithHideAndDontSave(pickingName);
        m_BatchRendererGroup.SetPickingMaterial(m_PickingMaterial);
        m_BatchRendererGroup.SetEnabledViewTypes(new BatchCullingViewType[]
        {
            BatchCullingViewType.Camera,
            BatchCullingViewType.Light,
            BatchCullingViewType.Picking,
            BatchCullingViewType.SelectionOutline
        });
#else
        m_BatchRendererGroup.SetEnabledViewTypes(new BatchCullingViewType[]
        {
            BatchCullingViewType.Camera,
            BatchCullingViewType.Light,
        });
#endif

#if ENABLE_ERROR_LOADING_MATERIALS
        if (SetFallbackMaterialsOnStart)
        {
#if HDRP_7_0_0_OR_NEWER
            var errorName = "Hidden/HDRP/MaterialError";
            var loadingName = "Hidden/HDRP/MaterialLoading";
#elif URP_7_0_0_OR_NEWER
            var errorName ="Hidden/Universal Render Pipeline/FallbackError";
            var loadingName = "Hidden/Universal Render Pipeline/FallbackLoading";
#else
    #error "You need either HDRP or URP package to use this script"
#endif

            m_ErrorMaterial = LoadMaterialWithHideAndDontSave(errorName);
            m_BatchRendererGroup.SetErrorMaterial(m_ErrorMaterial);

            m_LoadingMaterial = LoadMaterialWithHideAndDontSave(loadingName);
            m_BatchRendererGroup.SetLoadingMaterial(m_LoadingMaterial);
        }
#endif

        var renderers = GetComponentsInChildren<MeshRenderer>();
        Debug.Log("Converting " + renderers.Length + " renderers...");

#if ENABLE_PICKING
        int numPickingIDs = renderers.Length;
#else
        int numPickingIDs = 0;
#endif

        m_renderers = new NativeArray<DrawRenderer>(renderers.Length, Allocator.Persistent);
        m_pickingIDs = new NativeArray<int>(numPickingIDs, Allocator.Persistent);
        m_batchHash = new NativeParallelHashMap<DrawKey, int>(1024, Allocator.Persistent);
        m_rangeHash = new NativeParallelHashMap<RangeKey, int>(1024, Allocator.Persistent);
        m_drawBatches = new NativeList<DrawBatch>(Allocator.Persistent);
        m_drawRanges = new NativeList<DrawRange>(Allocator.Persistent);

        // Fill global data (shared between all batches)
        m_Globals = new GraphicsBuffer(GraphicsBuffer.Target.Constant,
            1,
            UnsafeUtility.SizeOf<BatchRendererGroupGlobals>());
        m_Globals.SetData(new [] { BatchRendererGroupGlobals.Default });

        m_brgBufferTarget = BatchRendererGroup.BufferTarget;
        m_instances = new NativeList<DrawInstance>(1024, Allocator.Persistent);

        int sizeOfFloat3x4 = UnsafeUtility.SizeOf<Vector4>() * 3;
        var instanceAllocator = new BRGBatchAllocator(
            sizeOfFloat3x4,
            sizeOfFloat3x4);

        // Bin renderers first so we know exactly how many instances we will need.
        var renderersByKey = new NativeParallelMultiHashMap<DrawKey, int>(1024, Allocator.Temp);
        int totalInstances = BinRenderers(renderers, renderersByKey);

        // RawBuffer mode can handle unlimited instances per batch, but
        // ConstantBuffer mode can't.
        m_maxInstancesPerBatch = totalInstances;
        if (UseConstantBuffer)
        {
            m_maxInstancesPerBatch = math.min(
                m_maxInstancesPerBatch,
                instanceAllocator.MaxInstancesForConstantBufferSize(
                    kBRGBufferMaxWindowSize,
                    kBRGBufferAlignment));
        }

        // For simplicity, allocate all batches with maximum size
        var batchAllocation = instanceAllocator.AllocateInstances(
            Allocator.Temp, m_maxInstancesPerBatch,
            allocationBegin: 0,
            constantBufferAlignment: (int)kBRGBufferAlignment);
        int numBatches = RoundToNextMultipleOf(renderers.Length, m_maxInstancesPerBatch) / m_maxInstancesPerBatch;
        int totalBytes = batchAllocation.AllocationSize * numBatches;

        int zeroPrefixBytes = math.max(4 * UnsafeUtility.SizeOf<Vector4>(), (int)kBRGBufferAlignment);
        int bigDataBufferVector4Count = (zeroPrefixBytes + totalBytes) / UnsafeUtility.SizeOf<Vector4>();
        var vectorBuffer = new NativeArray<Vector4>(bigDataBufferVector4Count, Allocator.Temp);

        // In ConstantBuffer mode we have to create a buffer that is usable as a constant buffer
        if (UseConstantBuffer)
        {
            m_GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Constant,
                (int)bigDataBufferVector4Count * 16 / kFloat4Size, kFloat4Size);
            m_brgBatchWindowSize = (uint)batchAllocation.AllocationSize;
        }
        else
        {
            m_GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
            (int)bigDataBufferVector4Count * 16 / 4, 4);
            m_brgBatchWindowSize = 0;
        }

        // First 4xfloat4 of ComputeBuffer needed to be zero filled for default property fall back!
        vectorBuffer[0] = new Vector4(0, 0, 0, 0);
        vectorBuffer[1] = new Vector4(0, 0, 0, 0);
        vectorBuffer[2] = new Vector4(0, 0, 0, 0);
        vectorBuffer[3] = new Vector4(0, 0, 0, 0);

        // Start first batch allocation after zeroes, but take care to align the allocation
        batchAllocation.AllocationBegin = zeroPrefixBytes;

        CreateBatchesForRenderers(renderers, renderersByKey, instanceAllocator, batchAllocation, vectorBuffer);
        BinBatchesIntoRanges();
        GenerateDrawIndicesForRanges();
        GenerateInstanceIndicesForBatches();

        m_GPUPersistentInstanceData.SetData(vectorBuffer);

        Debug.Log($"DrawRanges: {m_drawRanges.Length}, DrawBatches: {m_drawBatches.Length}, Instances: {m_instances.Length}, BRG Batches: {numBatches}, BufferTarget: {m_brgBufferTarget}");

        // Bounds ("infinite")
        UnityEngine.Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
        m_BatchRendererGroup.SetGlobalBounds(bounds);

        m_initialized = true;
    }

    private void GenerateInstanceIndicesForBatches()
    {
        // Prefix sum to calculate instance offsets for each DrawCommand
        int prefixSum = 0;
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
        m_rendererIndices = new NativeArray<int>(m_instances.Length, Allocator.Persistent);
        var internalDrawIndices =
            new NativeArray<int>(m_drawBatches.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
        for (int i = 0; i < m_instances.Length; i++)
        {
            var instance = m_instances[i];
            if (m_batchHash.TryGetValue(instance.key, out int drawBatchIndex))
            {
                var drawBatch = m_drawBatches[drawBatchIndex];
                GenerateInstanceIndex(instance, drawBatch, internalDrawIndices, drawBatchIndex);
            }
        }

        internalDrawIndices.Dispose();
    }

    private void GenerateDrawIndicesForRanges()
    {
        // Allocate offsets for ranges
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
        var internalRangeIndex = new NativeArray<int>(m_drawRanges.Length, Allocator.Temp);
        for (int i = 0; i < m_drawBatches.Length; i++)
        {
            var draw = m_drawBatches[i];
            if (m_rangeHash.TryGetValue(draw.key.rangeKey, out int drawRangeIndex))
            {
                var drawRange = m_drawRanges[drawRangeIndex];
                m_drawIndices[drawRange.drawOffset + internalRangeIndex[drawRangeIndex]] = i;
                internalRangeIndex[drawRangeIndex]++;
            }
        }
        internalRangeIndex.Dispose();
    }

    private void BinBatchesIntoRanges()
    {
        for (int i = 0; i < m_drawBatches.Length; ++i)
        {
            var drawBatch = m_drawBatches[i];
            var rangeKey = drawBatch.key.rangeKey;
            m_drawBatches[i] = drawBatch;

            if (!m_rangeHash.TryGetValue(rangeKey, out int rangeIndex))
            {
                rangeIndex = m_drawRanges.Length;
                m_rangeHash.Add(rangeKey, rangeIndex);
                m_drawRanges.Add(new DrawRange { key = rangeKey });
            }

            var range = m_drawRanges[rangeIndex];
            ++range.drawCount;
            m_drawRanges[rangeIndex] = range;
        }
    }

    private void CreateBatchesForRenderers(
        MeshRenderer[] renderers,
        NativeParallelMultiHashMap<DrawKey, int> renderersByKey,
        BRGBatchAllocator instanceAllocator,
        BRGBatchAllocator.BatchAllocation batchAllocation,
        NativeArray<Vector4> vectorBuffer)
    {
        BatchID currentBatch = default;
        int instanceIndex = 0;
        int sizeOfFloat4 = UnsafeUtility.SizeOf<Vector4>();

        var (keys, count) = renderersByKey.GetUniqueKeyArray(Allocator.Temp);
        for (int i = 0; i < count; ++i)
        {
            var key = keys[i];
            foreach (int rendererIndex in renderersByKey.GetValuesForKey(key))
            {
                var renderer = renderers[rendererIndex];

                int localToWorldIndex = instanceAllocator.InstancePropertyBufferOffset(batchAllocation, 0, instanceIndex) / sizeOfFloat4;
                int worldToLocalIndex = instanceAllocator.InstancePropertyBufferOffset(batchAllocation, 1, instanceIndex) / sizeOfFloat4;
                ExtractMatrices(renderer, vectorBuffer, localToWorldIndex, worldToLocalIndex);

                if (instanceIndex == 0)
                    currentBatch = CreateBRGBatch(batchAllocation);

                var keyWithBatch = key;
                keyWithBatch.batchID = currentBatch;

                AddDrawInstance(keyWithBatch, instanceIndex, rendererIndex);
                ExtractPickingID(rendererIndex, renderer);

                ++instanceIndex;
                if (instanceIndex >= m_maxInstancesPerBatch)
                {
                    instanceIndex = 0;
                    currentBatch = default;
                    batchAllocation.AllocationBegin += batchAllocation.AllocationSize;
                }
            }
        }
    }

    private void ExtractPickingID(int rendererIndex, MeshRenderer meshRenderer)
    {
#if ENABLE_PICKING
        m_pickingIDs[rendererIndex] = meshRenderer.gameObject.GetInstanceID();
#endif
    }

    private void AddDrawInstance(DrawKey key, int instanceIndex, int rendererIndex)
    {
        DrawBatch drawBatch = new DrawBatch
        {
            key = key,
            instanceCount = 0,
            instanceOffset = 0,
        };

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
        }
        drawBatch.instanceCount++;
        m_drawBatches[drawBatchIndex] = drawBatch;

        m_instances.Add(new DrawInstance { key = key, instanceIndex = instanceIndex, rendererIndex = rendererIndex });
    }

    private static int s_objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
    private static int s_worldToObjectID = Shader.PropertyToID("unity_WorldToObject");

    private BatchID CreateBRGBatch(BRGBatchAllocator.BatchAllocation batchAllocation)
    {
        NativeArray<MetadataValue> metadataValues = new NativeArray<MetadataValue>(2, Allocator.Temp);
        metadataValues[0] = CreateMetadataValue(s_objectToWorldID, batchAllocation.MetadataOffsetOfProperty(0, m_brgBufferTarget), true);
        metadataValues[1] = CreateMetadataValue(s_worldToObjectID, batchAllocation.MetadataOffsetOfProperty(1, m_brgBufferTarget), true);
        var batchID = m_BatchRendererGroup.AddBatch(
            metadataValues,
            m_GPUPersistentInstanceData.bufferHandle,
            (uint)batchAllocation.BatchBufferOffset(m_brgBufferTarget),
            m_brgBatchWindowSize);
        Debug.Assert(batchID != BatchID.Null, "Failed to create BRG batch");
        return batchID;
    }

    private void GenerateInstanceIndex(DrawInstance instance, DrawBatch drawBatch,
        NativeArray<int> internalDrawIndices, int drawBatchIndex)
    {
        int outIndex = drawBatch.instanceOffset + internalDrawIndices[drawBatchIndex];
        m_instanceIndices[outIndex] = instance.instanceIndex;
        m_rendererIndices[outIndex] = instance.rendererIndex;
        internalDrawIndices[drawBatchIndex]++;
    }

    private int BinRenderers(MeshRenderer[] renderers, NativeParallelMultiHashMap<DrawKey, int> renderersByKey)
    {
        int totalInstances = 0;

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            var renderer = renderers[rendererIndex];
            var flags = DrawRendererFlags.None;

            m_renderers[rendererIndex] = new DrawRenderer
                { bounds = new AABB { Center = new float3(0, 0, 0), Extents = new float3(0, 0, 0) }, flags = flags };

            var meshFilter = renderer.gameObject.GetComponent<MeshFilter>();
            if (!renderer || !meshFilter || !meshFilter.sharedMesh || renderer.enabled == false)
                continue;

            const int kLightmapIndexMask = 0xffff;
            const int kLightmapIndexNotLightmapped = 0xffff;
            //const int kLightmapIndexInfluenceOnly = 0xfffe;
            var lightmapIndexMasked = renderer.lightmapIndex & kLightmapIndexMask;
            if (lightmapIndexMasked != kLightmapIndexNotLightmapped)
                flags |= DrawRendererFlags.AffectsLightmaps;

            // Disable the existing Unity MeshRenderer to avoid double rendering!
            renderer.enabled = false;

            // Renderer bounds
            var transformedBounds = AABB.Transform(renderer.transform.localToWorldMatrix,
                meshFilter.sharedMesh.bounds.ToAABB());
            m_renderers[rendererIndex] = new DrawRenderer { bounds = transformedBounds, flags = flags };

            var mesh = m_BatchRendererGroup.RegisterMesh(meshFilter.sharedMesh);

            var sharedMaterials = new List<Material>();
            renderer.GetSharedMaterials(sharedMaterials);

            var rangeKey = new RangeKey
            {
                rendererPriority = renderer.rendererPriority,
                shadows = renderer.shadowCastingMode,
            };

            for (int matIndex = 0; matIndex < sharedMaterials.Count; matIndex++)
            {
                var material = m_BatchRendererGroup.RegisterMaterial(sharedMaterials[matIndex]);

                bool isTransparent = sharedMaterials[matIndex]?.renderQueue > (int)RenderQueue.GeometryLast;

                var key = new DrawKey
                {
                    rangeKey = rangeKey,
                    batchID = new BatchID(), // This is assigned later
                    material = material,
                    meshID = mesh,
                    submeshIndex = (uint)matIndex,
                    transparentInstanceID = isTransparent ? renderer.GetInstanceID() : 0,
                };

                renderersByKey.Add(key, rendererIndex);
                ++totalInstances;
            }
        }

        return totalInstances;
    }

    private static void ExtractMatrices(
        MeshRenderer renderer, NativeArray<Vector4> vectorBuffer,
        int localToWorldIndex, int worldToLocalIndex)
    {
        /*  mat4x3 packed like this:
                  p1.x, p1.w, p2.z, p3.y,
                  p1.y, p2.x, p2.w, p3.z,
                  p1.z, p2.y, p3.x, p3.w,
                  0.0,  0.0,  0.0,  1.0
            */

        var m = renderer.transform.localToWorldMatrix;
        vectorBuffer[0 + localToWorldIndex] = new Vector4(m.m00, m.m10, m.m20, m.m01);
        vectorBuffer[1 + localToWorldIndex] = new Vector4(m.m11, m.m21, m.m02, m.m12);
        vectorBuffer[2 + localToWorldIndex] = new Vector4(m.m22, m.m03, m.m13, m.m23);

        var mi = renderer.transform.worldToLocalMatrix;
        vectorBuffer[0 + worldToLocalIndex] = new Vector4(mi.m00, mi.m10, mi.m20, mi.m01);
        vectorBuffer[1 + worldToLocalIndex] = new Vector4(mi.m11, mi.m21, mi.m02, mi.m12);
        vectorBuffer[2 + worldToLocalIndex] = new Vector4(mi.m22, mi.m03, mi.m13, mi.m23);
    }

    void Update()
    {
        // TODO: Implement delta update for transforms
        // https://docs.unity3d.com/ScriptReference/Transform-hasChanged.html
        // https://docs.unity3d.com/ScriptReference/Jobs.TransformAccess.html
        Shader.SetGlobalConstantBuffer(BatchRendererGroupGlobals.kGlobalsPropertyId, m_Globals, 0, m_Globals.stride);
    }

    private void OnDisable()
    {
        // Always dispose the BRG, to avoid leaking it even in error cases. Unnecessary Dispose() is OK.
        // NOTE: Don't need to remove batch or unregister BatchRendererGroup resources. BRG.Dispose takes care of that.
        m_BatchRendererGroup.Dispose();

        if (m_initialized)
        {
            m_GPUPersistentInstanceData.Dispose();
            m_Globals.Dispose();

            m_renderers.Dispose();
            m_pickingIDs.Dispose();
            m_batchHash.Dispose();
            m_rangeHash.Dispose();
            m_drawBatches.Dispose();
            m_drawRanges.Dispose();
            m_instances.Dispose();
            m_instanceIndices.Dispose();
            m_rendererIndices.Dispose();
            m_drawIndices.Dispose();

#if ENABLE_PICKING
            DestroyImmediate(m_PickingMaterial);
#endif
            DestroyImmediate(m_ErrorMaterial);
            DestroyImmediate(m_LoadingMaterial);
        }
    }
}
