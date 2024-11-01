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
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    internal struct OccluderDerivedData
    {
        /// <summary></summary>
        public Matrix4x4 viewProjMatrix; // from view-centered world space
        /// <summary></summary>
        public Vector4 viewOriginWorldSpace;
        /// <summary></summary>
        public Vector4 radialDirWorldSpace;
        /// <summary></summary>
        public Vector4 facingDirWorldSpace;

        public static OccluderDerivedData FromParameters(in OccluderSubviewUpdate occluderSubviewUpdate)
        {
            var origin = occluderSubviewUpdate.viewOffsetWorldSpace + (Vector3)occluderSubviewUpdate.invViewMatrix.GetColumn(3); // view origin in world space
            var xViewVec = (Vector3)occluderSubviewUpdate.invViewMatrix.GetColumn(0); // positive x axis in world space
            var yViewVec = (Vector3)occluderSubviewUpdate.invViewMatrix.GetColumn(1); // positive y axis in world space
            var towardsVec = (Vector3)occluderSubviewUpdate.invViewMatrix.GetColumn(2); // positive z axis in world space

            var viewMatrixNoTranslation = occluderSubviewUpdate.viewMatrix;
            viewMatrixNoTranslation.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            return new OccluderDerivedData
            {
                viewOriginWorldSpace = origin,
                facingDirWorldSpace = towardsVec.normalized,
                radialDirWorldSpace = (xViewVec + yViewVec).normalized,
                viewProjMatrix = occluderSubviewUpdate.gpuProjMatrix * viewMatrixNoTranslation,
            };
        }
    }

    internal struct OccluderHandles
    {
        public TextureHandle occluderDepthPyramid;
        public BufferHandle occlusionDebugOverlay;

        public bool IsValid()
        {
            return occluderDepthPyramid.IsValid();
        }

        public void UseForOcclusionTest(IBaseRenderGraphBuilder builder)
        {
            builder.UseTexture(occluderDepthPyramid, AccessFlags.Read);
            if (occlusionDebugOverlay.IsValid())
                builder.UseBuffer(occlusionDebugOverlay, AccessFlags.ReadWrite);
        }

        public void UseForOccluderUpdate(IBaseRenderGraphBuilder builder)
        {
            builder.UseTexture(occluderDepthPyramid, AccessFlags.ReadWrite);
            if (occlusionDebugOverlay.IsValid())
                builder.UseBuffer(occlusionDebugOverlay, AccessFlags.ReadWrite);
        }
    }

    [GenerateHLSL(needAccessors = false)]
    internal enum InstanceOcclusionTestDebugCounter
    {
        Occluded,
        NotOccluded,
        Count,
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct IndirectInstanceInfo
    {
        public int drawOffsetAndSplitMask;    // [31:8]=draw_offset, [7:0]=split_mask
        public int instanceIndexAndCrossFade; // DOTS instance index
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct IndirectDrawInfo
    {
        public uint indexCount;
        public uint firstIndex;
        public uint baseVertex;
        public uint firstInstanceGlobalIndex;
        public uint maxInstanceCount;
    }

    internal struct IndirectBufferAllocInfo
    {
        public int drawAllocIndex;
        public int drawCount;
        public int instanceAllocIndex;
        public int instanceCount;

        public bool IsEmpty()
        {
            return drawCount == 0;
        }

        public bool IsWithinLimits(in IndirectBufferLimits limits)
        {
            return drawAllocIndex + drawCount <= limits.maxDrawCount
                && instanceAllocIndex + instanceCount <= limits.maxInstanceCount;
        }

        public int GetExtraDrawInfoSlotIndex()
        {
            return drawAllocIndex + drawCount;
        }
    }

    internal struct IndirectBufferContext
    {
        public JobHandle cullingJobHandle;

        public enum BufferState
        {
            Pending,                        // Not synced with culling output yet
            Zeroed,                         // All draws have zero instances
            NoOcclusionTest,                // Copy the results of CPU frustum/LOD culling
            AllInstancesOcclusionTested,    // Occlusion test the results of CPU frustum/LOD culling
            OccludedInstancesReTested,      // Re-test previously occluded instances (against updated occluders)
        }

        public BufferState bufferState;
        public int occluderVersion;
        public int subviewMask;

        public IndirectBufferContext(JobHandle cullingJobHandle)
        {
            this.cullingJobHandle = cullingJobHandle;
            this.bufferState = BufferState.Pending;
            this.occluderVersion = 0;
            this.subviewMask = 0;
        }

        public bool Matches(BufferState bufferState, int occluderVersion, int subviewMask)
        {
            return this.bufferState == bufferState
                && this.occluderVersion == occluderVersion
                && this.subviewMask == subviewMask;
        }
    }

    internal struct OccluderMipBounds
    {
        public Vector2Int offset;
        public Vector2Int size;
    }

    internal struct OccluderContext : IDisposable
    {
        private static class ShaderIDs
        {
            public static readonly int _SrcDepth = Shader.PropertyToID("_SrcDepth");
            public static readonly int _DstDepth = Shader.PropertyToID("_DstDepth");
            public static readonly int OccluderDepthPyramidConstants = Shader.PropertyToID("OccluderDepthPyramidConstants");
        }

        public const int k_FirstDepthMipIndex = 3; // 8x8 tiles
        public const int k_MaxOccluderMips = (int)OcclusionCullingCommonConfig.MaxOccluderMips;
        public const int k_MaxSilhouettePlanes = (int)OcclusionCullingCommonConfig.MaxOccluderSilhouettePlanes;
        public const int k_MaxSubviewsPerView = (int)OcclusionCullingCommonConfig.MaxSubviewsPerView;

        public int version;
        public Vector2Int depthBufferSize;

        public NativeArray<OccluderDerivedData> subviewData;
        public int subviewCount { get { return subviewData.Length; } }
        public int subviewValidMask;

        public bool IsSubviewValid(int subviewIndex)
        {
            return subviewIndex < subviewCount && (subviewValidMask & (1 << subviewIndex)) != 0;
        }

        public NativeArray<OccluderMipBounds> occluderMipBounds;
        public Vector2Int occluderMipLayoutSize; // total size of 2D layout specified by occluderMipBounds
        public Vector2Int occluderDepthPyramidSize; // at least the size of N mip layouts tiled vertically (one per subview)
        public RTHandle occluderDepthPyramid;
        public int occlusionDebugOverlaySize;
        public GraphicsBuffer occlusionDebugOverlay;
        public bool debugNeedsClear;
        public ComputeBuffer constantBuffer;
        public NativeArray<OccluderDepthPyramidConstants> constantBufferData;

        public Vector2 depthBufferSizeInOccluderPixels {
            get
            {
                int occluderPixelSize = 1 << k_FirstDepthMipIndex;
                return new Vector2(
                    (float)depthBufferSize.x / (float)occluderPixelSize,
                    (float)depthBufferSize.y / (float)occluderPixelSize);
            }
        }

        public void Dispose()
        {
            if (subviewData.IsCreated)
                subviewData.Dispose();

            if (occluderMipBounds.IsCreated)
                occluderMipBounds.Dispose();

            if (occluderDepthPyramid != null)
            {
                occluderDepthPyramid.Release();
                occluderDepthPyramid = null;
            }
            if (occlusionDebugOverlay != null)
            {
                occlusionDebugOverlay.Release();
                occlusionDebugOverlay = null;
            }
            if (constantBuffer != null)
            {
                constantBuffer.Release();
                constantBuffer = null;
            }

            if (constantBufferData.IsCreated)
                constantBufferData.Dispose();
        }

        private void UpdateMipBounds()
        {
            int occluderPixelSize = 1 << k_FirstDepthMipIndex;
            Vector2Int topMipSize = (depthBufferSize + (occluderPixelSize - 1) * Vector2Int.one) / occluderPixelSize;

            Vector2Int totalSize = Vector2Int.zero;
            Vector2Int mipOffset = Vector2Int.zero;
            Vector2Int mipSize = topMipSize;

            if (!occluderMipBounds.IsCreated)
                occluderMipBounds = new NativeArray<OccluderMipBounds>(k_MaxOccluderMips, Allocator.Persistent);

            for (int mipIndex = 0; mipIndex < k_MaxOccluderMips; ++mipIndex)
            {
                occluderMipBounds[mipIndex] = new OccluderMipBounds { offset = mipOffset, size = mipSize };

                totalSize.x = Mathf.Max(totalSize.x, mipOffset.x + mipSize.x);
                totalSize.y = Mathf.Max(totalSize.y, mipOffset.y + mipSize.y);

                if (mipIndex == 0)
                {
                    mipOffset.x = 0;
                    mipOffset.y += mipSize.y;
                }
                else
                {
                    mipOffset.x += mipSize.x;
                }
                mipSize.x = (mipSize.x + 1) / 2;
                mipSize.y = (mipSize.y + 1) / 2;
            }

            occluderMipLayoutSize = totalSize;
        }

        private void AllocateTexturesIfNecessary(bool debugOverlayEnabled)
        {
            Vector2Int minDepthPyramidSize = new Vector2Int(occluderMipLayoutSize.x, occluderMipLayoutSize.y * subviewCount);
            if (occluderDepthPyramidSize.x < minDepthPyramidSize.x || occluderDepthPyramidSize.y < minDepthPyramidSize.y)
            {
                if (occluderDepthPyramid != null)
                    occluderDepthPyramid.Release();

                occluderDepthPyramidSize = minDepthPyramidSize;
                occluderDepthPyramid = RTHandles.Alloc(
                    occluderDepthPyramidSize.x, occluderDepthPyramidSize.y,
                    format: GraphicsFormat.R32_SFloat,
                    dimension: TextureDimension.Tex2D,                    
                    filterMode: FilterMode.Point,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "Occluder Depths");
            }

            int newDebugOverlaySize = debugOverlayEnabled ? (minDepthPyramidSize.x * minDepthPyramidSize.y) : 0;
            if (occlusionDebugOverlaySize < newDebugOverlaySize)
            {
                if (occlusionDebugOverlay != null)
                    occlusionDebugOverlay.Release();

                occlusionDebugOverlaySize = newDebugOverlaySize;
                debugNeedsClear = true;

                // We use buffer instead of texture, because some platforms don't support atmoic operations for Texture2D<uint>
                occlusionDebugOverlay = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None,
                    occlusionDebugOverlaySize + (int)OcclusionCullingCommonConfig.DebugPyramidOffset, sizeof(uint));
            }
            if (newDebugOverlaySize == 0)
            {
                if (occlusionDebugOverlay != null)
                {
                    occlusionDebugOverlay.Release();
                    occlusionDebugOverlay = null;
                }

                occlusionDebugOverlaySize = newDebugOverlaySize;
            }

            if (constantBuffer == null)
                constantBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<OccluderDepthPyramidConstants>(), ComputeBufferType.Constant);

            if (!constantBufferData.IsCreated)
                constantBufferData = new NativeArray<OccluderDepthPyramidConstants>(1, Allocator.Persistent);
        }

        internal static void SetKeyword(ComputeCommandBuffer cmd, ComputeShader cs, in LocalKeyword keyword, bool value)
        {
            if (value)
                cmd.EnableKeyword(cs, keyword);
            else
                cmd.DisableKeyword(cs, keyword);
        }

        private OccluderDepthPyramidConstants SetupFarDepthPyramidConstants(ReadOnlySpan<OccluderSubviewUpdate> occluderSubviewUpdates, NativeArray<Plane> silhouettePlanes)
        {
            OccluderDepthPyramidConstants cb = new OccluderDepthPyramidConstants();

            // write globals
            cb._OccluderMipLayoutSizeX = (uint)occluderMipLayoutSize.x;
            cb._OccluderMipLayoutSizeY = (uint)occluderMipLayoutSize.y;

            // write per-subview data
            int updateCount = occluderSubviewUpdates.Length;
            for (int updateIndex = 0; updateIndex < updateCount; ++updateIndex)
            {
                ref readonly OccluderSubviewUpdate update = ref occluderSubviewUpdates[updateIndex];

                int subviewIndex = update.subviewIndex;
                subviewData[subviewIndex] = OccluderDerivedData.FromParameters(update);
                subviewValidMask |= 1 << update.subviewIndex;

                Matrix4x4 viewProjMatrix
                    = update.gpuProjMatrix
                    * update.viewMatrix
                    * Matrix4x4.Translate(-update.viewOffsetWorldSpace);
                Matrix4x4 invViewProjMatrix = viewProjMatrix.inverse;

                unsafe
                {
                    for (int j = 0; j < 16; ++j)
                        cb._InvViewProjMatrix[16 * updateIndex + j] = invViewProjMatrix[j];

                    cb._SrcOffset[4 * updateIndex + 0] = (uint)update.depthOffset.x;
                    cb._SrcOffset[4 * updateIndex + 1] = (uint)update.depthOffset.y;
                    cb._SrcOffset[4 * updateIndex + 2] = 0;
                    cb._SrcOffset[4 * updateIndex + 3] = 0;
                }

                cb._SrcSliceIndices |= (((uint)update.depthSliceIndex & 0xf) << (4 * updateIndex));
                cb._DstSubviewIndices |= ((uint)subviewIndex << (4 * updateIndex));
            }

            // TODO: transform these planes from world space into NDC space planes
            for (int i = 0; i < k_MaxSilhouettePlanes; ++i)
            {
                Plane plane = new Plane(Vector3.zero, 0.0f);
                if (i < silhouettePlanes.Length)
                    plane = silhouettePlanes[i];
                unsafe
                {
                    cb._SilhouettePlanes[4 * i + 0] = plane.normal.x;
                    cb._SilhouettePlanes[4 * i + 1] = plane.normal.y;
                    cb._SilhouettePlanes[4 * i + 2] = plane.normal.z;
                    cb._SilhouettePlanes[4 * i + 3] = plane.distance;
                }
            }
            cb._SilhouettePlaneCount = (uint)silhouettePlanes.Length;

            return cb;
        }

        public void CreateFarDepthPyramid(ComputeCommandBuffer cmd, in OccluderParameters occluderParams, ReadOnlySpan<OccluderSubviewUpdate> occluderSubviewUpdates, in OccluderHandles occluderHandles, NativeArray<Plane> silhouettePlanes, ComputeShader occluderDepthPyramidCS, int occluderDepthDownscaleKernel)
        {
            OccluderDepthPyramidConstants cb = SetupFarDepthPyramidConstants(occluderSubviewUpdates, silhouettePlanes);

            var cs = occluderDepthPyramidCS;
            int kernel = occluderDepthDownscaleKernel;

            var srcKeyword = new LocalKeyword(cs, "USE_SRC");
            var srcIsArrayKeyword = new LocalKeyword(cs, "SRC_IS_ARRAY");
            var srcIsMsaaKeyword = new LocalKeyword(cs, "SRC_IS_MSAA");

            bool srcIsArray = occluderParams.depthIsArray;

            RTHandle depthTexture = (RTHandle)occluderParams.depthTexture;
            bool srcIsMsaa = depthTexture?.isMSAAEnabled ?? false;

            int mipCount = k_FirstDepthMipIndex + k_MaxOccluderMips;
            for (int mipIndexBase = 0; mipIndexBase < mipCount - 1; mipIndexBase += 4)
            {
                cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._DstDepth, occluderHandles.occluderDepthPyramid);

                bool useSrc = (mipIndexBase == 0);
                SetKeyword(cmd, cs, srcKeyword, useSrc);
                SetKeyword(cmd, cs, srcIsArrayKeyword, useSrc && srcIsArray);
                SetKeyword(cmd, cs, srcIsMsaaKeyword, useSrc && srcIsMsaa);
                if (useSrc)
                    cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._SrcDepth, occluderParams.depthTexture);

                cb._MipCount = (uint)Math.Min(mipCount - 1 - mipIndexBase, 4);

                Vector2Int srcSize = Vector2Int.zero;
                for (int i = 0; i < 5; ++i)
                {
                    Vector2Int offset = Vector2Int.zero;
                    Vector2Int size = Vector2Int.zero;
                    int mipIndex = mipIndexBase + i;
                    if (mipIndex == 0)
                    {
                        size = occluderParams.depthSize;
                    }
                    else
                    {
                        int occMipIndex = mipIndex - k_FirstDepthMipIndex;
                        if (0 <= occMipIndex && occMipIndex < k_MaxOccluderMips)
                        {
                            offset = occluderMipBounds[occMipIndex].offset;
                            size = occluderMipBounds[occMipIndex].size;
                        }
                    }
                    if (i == 0)
                        srcSize = size;
                    unsafe
                    {
                        cb._MipOffsetAndSize[4 * i + 0] = (uint)offset.x;
                        cb._MipOffsetAndSize[4 * i + 1] = (uint)offset.y;
                        cb._MipOffsetAndSize[4 * i + 2] = (uint)size.x;
                        cb._MipOffsetAndSize[4 * i + 3] = (uint)size.y;
                    }
                }

                constantBufferData[0] = cb;
                cmd.SetBufferData(constantBuffer, constantBufferData);
                cmd.SetComputeConstantBufferParam(cs, ShaderIDs.OccluderDepthPyramidConstants, constantBuffer, 0, constantBuffer.stride);

                cmd.DispatchCompute(cs, kernel, (srcSize.x + 15) / 16, (srcSize.y + 15) / 16, occluderSubviewUpdates.Length);
            }
        }

        public OccluderHandles Import(RenderGraph renderGraph)
        {
            RenderTargetInfo rtInfo = new RenderTargetInfo
            {
                width = occluderDepthPyramidSize.x,
                height = occluderDepthPyramidSize.y,
                volumeDepth = 1,
                msaaSamples = 1,
                format = GraphicsFormat.R32_SFloat,
                bindMS = false,
            };
            OccluderHandles occluderHandles = new OccluderHandles()
            {
                occluderDepthPyramid = renderGraph.ImportTexture(occluderDepthPyramid, rtInfo)
            };
            if (occlusionDebugOverlay != null)
                occluderHandles.occlusionDebugOverlay = renderGraph.ImportBuffer(occlusionDebugOverlay);
            return occluderHandles;
        }

        public void PrepareOccluders(in OccluderParameters occluderParams)
        {
            if (subviewCount != occluderParams.subviewCount)
            {
                if (subviewData.IsCreated)
                    subviewData.Dispose();

                subviewData = new NativeArray<OccluderDerivedData>(occluderParams.subviewCount, Allocator.Persistent);
                subviewValidMask = 0;
            }
            depthBufferSize = occluderParams.depthSize;

            // enable debug counters for cameras when the overlay is enabled
            bool debugOverlayEnabled = GPUResidentDrawer.GetDebugStats()?.occlusionOverlayEnabled ?? false;
            UpdateMipBounds();
            AllocateTexturesIfNecessary(debugOverlayEnabled);
        }

        internal OcclusionCullingDebugOutput GetDebugOutput()
        {
            var debugOutput = new OcclusionCullingDebugOutput
            {
                occluderDepthPyramid = occluderDepthPyramid,
                occlusionDebugOverlay = occlusionDebugOverlay,
            };

            debugOutput.cb._DepthSizeInOccluderPixels = depthBufferSizeInOccluderPixels;
            debugOutput.cb._OccluderMipLayoutSizeX = (uint)occluderMipLayoutSize.x;
            debugOutput.cb._OccluderMipLayoutSizeY = (uint)occluderMipLayoutSize.y;
            for (int i = 0; i < occluderMipBounds.Length; ++i)
            {
                var mipBounds = occluderMipBounds[i];
                unsafe
                {
                    debugOutput.cb._OccluderMipBounds[4 * i + 0] = (uint)mipBounds.offset.x;
                    debugOutput.cb._OccluderMipBounds[4 * i + 1] = (uint)mipBounds.offset.y;
                    debugOutput.cb._OccluderMipBounds[4 * i + 2] = (uint)mipBounds.size.x;
                    debugOutput.cb._OccluderMipBounds[4 * i + 3] = (uint)mipBounds.size.y;
                }
            }

            return debugOutput;
        }
    }

    internal enum IndirectAllocator
    {
        NextInstanceIndex,
        NextDrawIndex,
        Count // keep last
    }

    internal struct IndirectBufferLimits
    {
        public int maxInstanceCount;
        public int maxDrawCount;
    }

    internal struct InstanceOcclusionTestSubviewSettings
    {
        public int testCount;
        public int occluderSubviewIndices;
        public int occluderSubviewMask;
        public int cullingSplitIndices;
        public int cullingSplitMask;

        public static InstanceOcclusionTestSubviewSettings FromSpan(ReadOnlySpan<SubviewOcclusionTest> subviewOcclusionTests)
        {
            InstanceOcclusionTestSubviewSettings settings = new InstanceOcclusionTestSubviewSettings();
            for (int testIndex = 0; testIndex < subviewOcclusionTests.Length; ++testIndex)
            {
                SubviewOcclusionTest subviewTest = subviewOcclusionTests[testIndex];
                settings.occluderSubviewIndices |= subviewTest.occluderSubviewIndex << (4 * testIndex);
                settings.occluderSubviewMask |= 1 << subviewTest.occluderSubviewIndex;
                settings.cullingSplitIndices |= subviewTest.cullingSplitIndex << (4 * testIndex);
                settings.cullingSplitMask |= 1 << subviewTest.cullingSplitIndex;
            }
            settings.testCount = subviewOcclusionTests.Length;
            return settings;
        }
    }

    internal struct IndirectBufferContextHandles
    {
        public BufferHandle instanceBuffer;
        public BufferHandle instanceInfoBuffer;
        public BufferHandle argsBuffer;
        public BufferHandle drawInfoBuffer;

        public void UseForOcclusionTest(IBaseRenderGraphBuilder builder)
        {
            instanceBuffer = builder.UseBuffer(instanceBuffer, AccessFlags.ReadWrite);
            instanceInfoBuffer = builder.UseBuffer(instanceInfoBuffer, AccessFlags.Read);
            argsBuffer = builder.UseBuffer(argsBuffer, AccessFlags.ReadWrite);
            drawInfoBuffer = builder.UseBuffer(drawInfoBuffer, AccessFlags.Read);
        }
    }

    internal struct IndirectBufferContextStorage : IDisposable
    {
        private const int kAllocatorCount = (int)IndirectAllocator.Count;
        internal const int kExtraDrawAllocationCount = 1;           // over-allocate by one for indirect args scratch space GPU-side
        internal const int kInstanceInfoGpuOffsetMultiplier = 2;    // GPU side allocates storage for extra copy of instance list

        private IndirectBufferLimits m_BufferLimits;

        private GraphicsBuffer m_InstanceBuffer;
        private GraphicsBuffer m_InstanceInfoBuffer;
        private NativeArray<IndirectInstanceInfo> m_InstanceInfoStaging;

        private GraphicsBuffer m_ArgsBuffer;
        private GraphicsBuffer m_DrawInfoBuffer;
        private NativeArray<IndirectDrawInfo> m_DrawInfoStaging;

        private int m_ContextAllocCounter;
        private NativeHashMap<int, int> m_ContextIndexFromViewID;
        private NativeList<IndirectBufferContext> m_Contexts;
        private NativeArray<IndirectBufferAllocInfo> m_ContextAllocInfo;
        private NativeArray<int> m_AllocationCounters;

        public GraphicsBuffer instanceBuffer { get { return m_InstanceBuffer; } }
        public GraphicsBuffer instanceInfoBuffer { get { return m_InstanceInfoBuffer; } }
        public GraphicsBuffer argsBuffer { get { return m_ArgsBuffer; } }
        public GraphicsBuffer drawInfoBuffer { get { return m_DrawInfoBuffer; } }

        public GraphicsBufferHandle visibleInstanceBufferHandle { get { return m_InstanceBuffer.bufferHandle; } }
        public GraphicsBufferHandle indirectArgsBufferHandle { get { return m_ArgsBuffer.bufferHandle; } }

        public IndirectBufferContextHandles ImportBuffers(RenderGraph renderGraph)
        {
            return new IndirectBufferContextHandles()
            {
                instanceBuffer = renderGraph.ImportBuffer(m_InstanceBuffer),
                instanceInfoBuffer = renderGraph.ImportBuffer(m_InstanceInfoBuffer),
                argsBuffer = renderGraph.ImportBuffer(m_ArgsBuffer),
                drawInfoBuffer = renderGraph.ImportBuffer(m_DrawInfoBuffer),
            };
        }

        public NativeArray<IndirectInstanceInfo> instanceInfoGlobalArray { get { return m_InstanceInfoStaging;  } }
        public NativeArray<IndirectDrawInfo> drawInfoGlobalArray { get { return m_DrawInfoStaging; } }
        public NativeArray<int> allocationCounters { get { return m_AllocationCounters; } }

        public void Init()
        {
            int initialDrawCount = 256;
            int initialInstanceCount = 64 * initialDrawCount;
            int initialContextCount = 8;

            AllocateInstanceBuffers(initialInstanceCount);
            AllocateDrawBuffers(initialDrawCount);

            m_ContextIndexFromViewID = new NativeHashMap<int, int>(initialContextCount, Allocator.Persistent);
            m_Contexts = new NativeList<IndirectBufferContext>(initialContextCount, Allocator.Persistent);
            m_ContextAllocInfo = new NativeArray<IndirectBufferAllocInfo>(initialContextCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            m_AllocationCounters = new NativeArray<int>(kAllocatorCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            ResetAllocators();
        }

        void AllocateInstanceBuffers(int maxInstanceCount)
        {
            m_InstanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, maxInstanceCount, sizeof(int));
            m_InstanceInfoBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, kInstanceInfoGpuOffsetMultiplier * maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf<IndirectInstanceInfo>());
            m_InstanceInfoStaging = new NativeArray<IndirectInstanceInfo>(maxInstanceCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_BufferLimits.maxInstanceCount = maxInstanceCount;
        }

        void FreeInstanceBuffers()
        {
            m_InstanceBuffer.Release();
            m_InstanceInfoBuffer.Release();
            m_InstanceInfoStaging.Dispose();
            m_BufferLimits.maxInstanceCount = 0;
        }

        void AllocateDrawBuffers(int maxDrawCount)
        {
            m_ArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.IndirectArguments, (maxDrawCount + kExtraDrawAllocationCount) * (GraphicsBuffer.IndirectDrawIndexedArgs.size / sizeof(int)), sizeof(int));
            m_DrawInfoBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxDrawCount, System.Runtime.InteropServices.Marshal.SizeOf<IndirectDrawInfo>());
            m_DrawInfoStaging = new NativeArray<IndirectDrawInfo>(maxDrawCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_BufferLimits.maxDrawCount = maxDrawCount;
        }

        void FreeDrawBuffers()
        {
            m_ArgsBuffer.Release();
            m_DrawInfoBuffer.Release();
            m_DrawInfoStaging.Dispose();
            m_BufferLimits.maxDrawCount = 0;
        }

        public void Dispose()
        {
            SyncContexts();

            FreeInstanceBuffers();
            FreeDrawBuffers();

            m_ContextIndexFromViewID.Dispose();
            m_Contexts.Dispose();
            m_ContextAllocInfo.Dispose();
            m_AllocationCounters.Dispose();
        }

        private void SyncContexts()
        {
            for (int contextIndex = 0; contextIndex < m_Contexts.Length; ++contextIndex)
                m_Contexts[contextIndex].cullingJobHandle.Complete();
        }

        private void ResetAllocators()
        {
            m_ContextAllocCounter = 0;
            m_ContextIndexFromViewID.Clear();
            m_Contexts.Clear();
            m_AllocationCounters.FillArray(0);
        }

        private void GrowBuffers()
        {
            if (m_ContextAllocCounter > m_ContextAllocInfo.Length)
            {
                // allocate 20% more than the high water mark
                int newContextCount = (m_ContextAllocCounter * 6) / 5;
                m_Contexts.Clear();
                m_Contexts.SetCapacity(newContextCount);
                m_ContextAllocInfo.Dispose();
                m_ContextAllocInfo = new NativeArray<IndirectBufferAllocInfo>(newContextCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                //Debug.Log("Raised indirect context count to " + newContextCount);
            }
            int instanceAllocCounter = m_AllocationCounters[(int)IndirectAllocator.NextInstanceIndex];
            if (instanceAllocCounter > m_BufferLimits.maxInstanceCount)
            {
                // allocate 20% more than the high water mark
                int newInstanceCount = (instanceAllocCounter * 6) / 5;
                FreeInstanceBuffers();
                AllocateInstanceBuffers(newInstanceCount);
                //Debug.Log("Raised indirect instance count to " + newInstanceCount);
            }
            int drawAllocCounter = m_AllocationCounters[(int)IndirectAllocator.NextDrawIndex];
            if (drawAllocCounter > m_BufferLimits.maxDrawCount)
            {
                // allocate 20% more than the high water mark
                int newDrawCount = (drawAllocCounter * 6) / 5;
                FreeDrawBuffers();
                AllocateDrawBuffers(newDrawCount);
                //Debug.Log("Raised indirect draw count to " + newDrawCount);
            }
        }

        public void ClearContextsAndGrowBuffers()
        {
            SyncContexts();
            GrowBuffers();
            ResetAllocators();
        }

        public int TryAllocateContext(int viewID)
        {
            // Disallow using the same viewID multiple times for a frame, since it is used as a UID to update indirect args
            // This will prevent multiple context being created for example if a custom pass is being used
            if (m_ContextIndexFromViewID.ContainsKey(viewID))
                return -1;

            int contextIndex = -1;
            m_ContextAllocCounter += 1;
            if (m_Contexts.Length < m_ContextAllocInfo.Length)
            {
                contextIndex = m_Contexts.Length;
                m_Contexts.Add(new IndirectBufferContext());
                m_ContextIndexFromViewID.Add(viewID, contextIndex);
            }
            return contextIndex;
        }

        public int TryGetContextIndex(int viewID)
        {
            if (!m_ContextIndexFromViewID.TryGetValue(viewID, out var contextIndex))
                contextIndex = -1;
            return contextIndex;
        }

        public NativeArray<IndirectBufferAllocInfo> GetAllocInfoSubArray(int contextIndex)
        {
            int safeIndex = Mathf.Max(contextIndex, 0);
            return m_ContextAllocInfo.GetSubArray(safeIndex, 1);
        }

        public IndirectBufferAllocInfo GetAllocInfo(int contextIndex)
        {
            IndirectBufferAllocInfo allocInfo = new IndirectBufferAllocInfo();
            if (0 <= contextIndex && contextIndex < m_Contexts.Length)
                allocInfo = m_ContextAllocInfo[contextIndex];
            return allocInfo;
        }

        public void CopyFromStaging(CommandBuffer cmd, in IndirectBufferAllocInfo allocInfo)
        {
            if (!allocInfo.IsEmpty())
            {
                cmd.SetBufferData(
                    m_DrawInfoBuffer,
                    m_DrawInfoStaging,
                    allocInfo.drawAllocIndex,
                    allocInfo.drawAllocIndex,
                    allocInfo.drawCount);

                cmd.SetBufferData(
                    m_InstanceInfoBuffer,
                    m_InstanceInfoStaging,
                    allocInfo.instanceAllocIndex,
                    kInstanceInfoGpuOffsetMultiplier * allocInfo.instanceAllocIndex,
                    allocInfo.instanceCount);
            }
        }

        public IndirectBufferLimits GetLimits(int contextIndex)
        {
            IndirectBufferLimits limits = new IndirectBufferLimits();
            if (contextIndex >= 0)
                limits = m_BufferLimits;
            return limits;
        }

        public IndirectBufferContext GetBufferContext(int contextIndex)
        {
            IndirectBufferContext ctx = new IndirectBufferContext();
            if (0 <= contextIndex && contextIndex < m_Contexts.Length)
                ctx = m_Contexts[contextIndex];
            return ctx;
        }

        public void SetBufferContext(int contextIndex, IndirectBufferContext ctx)
        {
            if (0 <= contextIndex && contextIndex < m_Contexts.Length)
                m_Contexts[contextIndex] = ctx;
        }
    }
}
