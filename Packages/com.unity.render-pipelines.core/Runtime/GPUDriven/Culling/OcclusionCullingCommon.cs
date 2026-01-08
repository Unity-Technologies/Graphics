using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    [GenerateHLSL]
    internal enum OcclusionCullingCommonConfig
    {
        MaxOccluderMips = 8,
        MaxOccluderSilhouettePlanes = 6,
        MaxSubviewsPerView = 6,
        DebugPyramidOffset = 4, // TODO: rename
    }

    [GenerateHLSL(needAccessors = false)]
    internal enum OcclusionTestDebugFlag
    {
        AlwaysPass = (1 << 0),
        CountVisible = (1 << 1),
    }

    internal struct OcclusionTestComputeShader
    {
        public ComputeShader cs;
        public LocalKeyword occlusionDebugKeyword;

        public void Init(ComputeShader cs)
        {
            this.cs = cs;
            this.occlusionDebugKeyword = new LocalKeyword(cs, "OCCLUSION_DEBUG");
        }
    }

    internal struct SilhouettePlaneCache : IDisposable
    {
        private struct Slot
        {
            public bool isActive;
            public EntityId viewID;
            public int planeCount;  // planeIndex = slotIndex * kMaxSilhouettePlanes
            public int lastUsedFrameIndex;

            public Slot(EntityId viewID, int planeCount, int frameIndex)
            {
                this.isActive = true;
                this.viewID = viewID;
                this.planeCount = planeCount;
                this.lastUsedFrameIndex = frameIndex;
            }
        }

        private const int kMaxSilhouettePlanes = (int)OcclusionCullingCommonConfig.MaxOccluderSilhouettePlanes;

        private NativeParallelHashMap<EntityId, int> m_SubViewIDToIndexMap;
        private NativeList<int> m_SlotFreeList;
        private NativeList<Slot> m_Slots;
        private NativeList<Plane> m_PlaneStorage;

        public void Init()
        {
            m_SubViewIDToIndexMap = new NativeParallelHashMap<EntityId, int>(16, Allocator.Persistent);
            m_SlotFreeList = new NativeList<int>(16, Allocator.Persistent);
            m_Slots = new NativeList<Slot>(16, Allocator.Persistent);
            m_PlaneStorage = new NativeList<Plane>(16 * kMaxSilhouettePlanes, Allocator.Persistent);
        }

        public void Dispose()
        {
            m_SubViewIDToIndexMap.Dispose();
            m_SlotFreeList.Dispose();
            m_Slots.Dispose();
            m_PlaneStorage.Dispose();
        }

        public void Update(EntityId viewID, NativeArray<Plane> planes, int frameIndex)
        {
            int planeCount = Math.Min(planes.Length, kMaxSilhouettePlanes);

            if (!m_SubViewIDToIndexMap.TryGetValue(viewID, out int slotIndex))
            {
                if (m_SlotFreeList.Length > 0)
                {
                    // take a free slot from the free list
                    slotIndex = m_SlotFreeList[m_SlotFreeList.Length - 1];
                    m_SlotFreeList.Length = m_SlotFreeList.Length - 1;
                }
                else
                {
                    // ensure we have capacity for a few more
                    if (m_Slots.Length == m_Slots.Capacity)
                    {
                        int newCapacity = m_Slots.Length + 8;
                        m_Slots.SetCapacity(newCapacity);
                        m_PlaneStorage.SetCapacity(newCapacity * kMaxSilhouettePlanes);
                    }

                    // use the next slot in storage
                    slotIndex = m_Slots.Length;
                    int newSlotCount = slotIndex + 1;
                    m_Slots.ResizeUninitialized(newSlotCount);
                    m_PlaneStorage.ResizeUninitialized(newSlotCount * kMaxSilhouettePlanes);
                }

                // associate with this view ID
                m_SubViewIDToIndexMap.Add(viewID, slotIndex);
            }

            m_Slots[slotIndex] = new Slot(viewID, planeCount, frameIndex);
            m_PlaneStorage.AsArray().GetSubArray(slotIndex * kMaxSilhouettePlanes, planeCount).CopyFrom(planes);
        }

        public void FreeUnusedSlots(int frameIndex, int maximumAge)
        {
            for (int slotIndex = 0; slotIndex < m_Slots.Length; ++slotIndex)
            {
                var slot = m_Slots[slotIndex];
                if (!slot.isActive)
                    continue;

                if ((frameIndex - slot.lastUsedFrameIndex) > maximumAge)
                {
                    slot.isActive = false;
                    m_Slots[slotIndex] = slot;
                    m_SubViewIDToIndexMap.Remove(slot.viewID);
                    m_SlotFreeList.Add(slotIndex);
                }
            }
        }

        public NativeArray<Plane> GetSubArray(EntityId viewID)
        {
            int planeOffset = 0;
            int planeCount = 0;
            if (m_SubViewIDToIndexMap.TryGetValue(viewID, out int slotIndex))
            {
                planeOffset = slotIndex * kMaxSilhouettePlanes;
                planeCount = m_Slots[slotIndex].planeCount;
            }
            return m_PlaneStorage.AsArray().GetSubArray(planeOffset, planeCount);
        }
    }

    internal class OcclusionCullingCommon : IDisposable
    {
        private struct OccluderContextSlot
        {
            public bool valid;
            public int lastUsedFrameIndex;
            public EntityId viewID;
        }

        private static readonly int s_MaxContextGCFrame = 8; // Allow a few frames for alternate frame shadow updates before cleanup

        private Material m_DebugOcclusionTestMaterial;
        private Material m_OccluderDebugViewMaterial;

        private ComputeShader m_OcclusionDebugCS;
        private int m_ClearOcclusionDebugKernel;

        private ComputeShader m_OccluderDepthPyramidCS;
        private int m_OccluderDepthDownscaleKernel;
        private int m_FrameIndex = 0;

        private SilhouettePlaneCache m_SilhouettePlaneCache;

        private NativeParallelHashMap<EntityId, int> m_ViewIDToIndexMap;
        private List<OccluderContext> m_OccluderContextData;
        private NativeList<OccluderContextSlot> m_OccluderContextSlots;
        private NativeList<int> m_FreeOccluderContexts;

        private NativeArray<OcclusionCullingCommonShaderVariables> m_CommonShaderVariables;
        private ComputeBuffer m_CommonConstantBuffer;
        private NativeArray<OcclusionCullingDebugShaderVariables> m_DebugShaderVariables;
        private ComputeBuffer m_DebugConstantBuffer;

        private ProfilingSampler m_ProfilingSamplerUpdateOccluders;
        private ProfilingSampler m_ProfilingSamplerOcclusionTestOverlay;
        private ProfilingSampler m_ProfilingSamplerOccluderOverlay;

        private BaseRenderFunc<OcclusionTestOverlaySetupPassData, ComputeGraphContext> m_ComputePassRenderFunc;
        private BaseRenderFunc<OcclusionTestOverlayPassData, RasterGraphContext> m_RasterPassRenderFunc;

        internal void Initialize(GPUResidentDrawerResources resources)
        {
            m_DebugOcclusionTestMaterial = CoreUtils.CreateEngineMaterial(resources.debugOcclusionTestPS);
            m_OccluderDebugViewMaterial = CoreUtils.CreateEngineMaterial(resources.debugOccluderPS);

            m_OcclusionDebugCS = resources.occlusionCullingDebugKernels;
            m_ClearOcclusionDebugKernel = m_OcclusionDebugCS.FindKernel("ClearOcclusionDebug");

            m_OccluderDepthPyramidCS = resources.occluderDepthPyramidKernels;
            m_OccluderDepthDownscaleKernel = m_OccluderDepthPyramidCS.FindKernel("OccluderDepthDownscale");

            m_SilhouettePlaneCache.Init();

            m_ViewIDToIndexMap = new NativeParallelHashMap<EntityId, int>(64, Allocator.Persistent);
            m_OccluderContextData = new List<OccluderContext>();
            m_OccluderContextSlots = new NativeList<OccluderContextSlot>(64, Allocator.Persistent);
            m_FreeOccluderContexts = new NativeList<int>(64, Allocator.Persistent);

            m_ProfilingSamplerUpdateOccluders = new ProfilingSampler("UpdateOccluders");
            m_ProfilingSamplerOcclusionTestOverlay = new ProfilingSampler("OcclusionTestOverlay");
            m_ProfilingSamplerOccluderOverlay = new ProfilingSampler("OccluderOverlay");

            m_CommonShaderVariables = new NativeArray<OcclusionCullingCommonShaderVariables>(1, Allocator.Persistent);
            m_CommonConstantBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<OcclusionCullingCommonShaderVariables>(), ComputeBufferType.Constant);
            m_DebugShaderVariables = new NativeArray<OcclusionCullingDebugShaderVariables>(1, Allocator.Persistent);
            m_DebugConstantBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<OcclusionCullingDebugShaderVariables>(), ComputeBufferType.Constant);

            m_ComputePassRenderFunc = (OcclusionTestOverlaySetupPassData data, ComputeGraphContext ctx) =>
            {
                m_DebugShaderVariables[0] = data.cb;
                ctx.cmd.SetBufferData(m_DebugConstantBuffer, m_DebugShaderVariables);

                m_DebugOcclusionTestMaterial.SetConstantBuffer(
                    ShaderIDs.OcclusionCullingDebugShaderVariables,
                    m_DebugConstantBuffer,
                    0,
                    m_DebugConstantBuffer.stride);
            };

            m_RasterPassRenderFunc = (OcclusionTestOverlayPassData data, RasterGraphContext ctx) =>
            {
                ctx.cmd.SetGlobalBuffer(ShaderIDs._OcclusionDebugOverlay, data.debugPyramid);
                CoreUtils.DrawFullScreen(ctx.cmd, m_DebugOcclusionTestMaterial);
            };
        }

        private static class ShaderIDs
        {
            public static readonly int OcclusionCullingCommonShaderVariables = Shader.PropertyToID("OcclusionCullingCommonShaderVariables");
            public static readonly int _OccluderDepthPyramid = Shader.PropertyToID("_OccluderDepthPyramid");
            public static readonly int _OcclusionDebugOverlay = Shader.PropertyToID("_OcclusionDebugOverlay");

            public static readonly int OcclusionCullingDebugShaderVariables = Shader.PropertyToID("OcclusionCullingDebugShaderVariables");
        }

        internal static bool UseOcclusionDebug(in OccluderContext occluderCtx)
        {
            return occluderCtx.occlusionDebugOverlaySize != 0;
        }

        internal void PrepareCulling(ComputeCommandBuffer cmd, in OccluderContext occluderCtx, in OcclusionCullingSettings settings, in InstanceOcclusionTestSubviewSettings subviewSettings, in OcclusionTestComputeShader shader, bool useOcclusionDebug)
        {
            OccluderContext.SetKeyword(cmd, shader.cs, shader.occlusionDebugKeyword, useOcclusionDebug);

            var debugStats = GPUResidentDrawer.GetDebugStats();

            m_CommonShaderVariables[0] = new OcclusionCullingCommonShaderVariables(
                in occluderCtx,
                subviewSettings,
                debugStats?.occlusionOverlayCountVisible ?? false,
                debugStats?.overrideOcclusionTestToAlwaysPass ?? false);
            cmd.SetBufferData(m_CommonConstantBuffer, m_CommonShaderVariables);

            cmd.SetComputeConstantBufferParam(shader.cs, ShaderIDs.OcclusionCullingCommonShaderVariables, m_CommonConstantBuffer, 0, m_CommonConstantBuffer.stride);

            DispatchDebugClear(cmd, settings.viewInstanceID);
        }

        internal static void SetDepthPyramid(ComputeCommandBuffer cmd, in OcclusionTestComputeShader shader, int kernel, in OccluderHandles occluderHandles)
        {
            cmd.SetComputeTextureParam(shader.cs, kernel, ShaderIDs._OccluderDepthPyramid, occluderHandles.occluderDepthPyramid);
        }

        internal static void SetDebugPyramid(ComputeCommandBuffer cmd, in OcclusionTestComputeShader shader, int kernel, in OccluderHandles occluderHandles)
        {
            cmd.SetComputeBufferParam(shader.cs, kernel, ShaderIDs._OcclusionDebugOverlay, occluderHandles.occlusionDebugOverlay);
        }

        private class OcclusionTestOverlaySetupPassData
        {
            public OcclusionCullingDebugShaderVariables cb;
        }

        private class OcclusionTestOverlayPassData
        {
            public BufferHandle debugPyramid;
        }

        public void RenderDebugOcclusionTestOverlay(RenderGraph renderGraph, DebugDisplayGPUResidentDrawer debugSettings, EntityId viewID, TextureHandle colorBuffer)
        {
            if (debugSettings == null)
                return;
            if (!debugSettings.occlusionTestOverlayEnable)
                return;

            OcclusionCullingDebugOutput debugOutput = GetOcclusionTestDebugOutput(viewID);
            if (debugOutput.occlusionDebugOverlay == null)
                return;

            using (var builder = renderGraph.AddComputePass<OcclusionTestOverlaySetupPassData>("OcclusionTestOverlay", out var passData, m_ProfilingSamplerOcclusionTestOverlay))
            {
                builder.AllowPassCulling(false);

                passData.cb = debugOutput.cb;

                builder.SetRenderFunc(m_ComputePassRenderFunc);
            }

            using (var builder = renderGraph.AddRasterRenderPass<OcclusionTestOverlayPassData>("OcclusionTestOverlay", out var passData, m_ProfilingSamplerOcclusionTestOverlay))
            {
                builder.AllowGlobalStateModification(true);

                passData.debugPyramid = renderGraph.ImportBuffer(debugOutput.occlusionDebugOverlay);

                builder.SetRenderAttachment(colorBuffer, 0);
                builder.UseBuffer(passData.debugPyramid);

                builder.SetRenderFunc(m_RasterPassRenderFunc);
            }
        }

        private struct DebugOccluderViewData
        {
            public int passIndex;
            public Rect viewport;
            public bool valid;
        }

        class OccluderOverlayPassData
        {
            public Material debugMaterial;
            public RTHandle occluderTexture;
            public Rect viewport;
            public int passIndex;
            public Vector2 validRange;
        }

        public void RenderDebugOccluderOverlay(RenderGraph renderGraph, DebugDisplayGPUResidentDrawer debugSettings, Vector2 screenPos, float maxHeight, TextureHandle colorBuffer)
        {
            if (debugSettings == null)
                return;
            if (!debugSettings.occluderDebugViewEnable)
                return;

            if (!debugSettings.GetOccluderViewID(out var camera))
                return;

            var occluderTexture = GetOcclusionTestDebugOutput(camera).occluderDepthPyramid;
            if (occluderTexture == null)
                return;

            Material debugMaterial = m_OccluderDebugViewMaterial;
            int passIndex = debugMaterial.FindPass("DebugOccluder");

            Vector2 outputSize = occluderTexture.referenceSize;
            float scaleFactor = maxHeight / outputSize.y;
            outputSize *= scaleFactor;
            Rect viewport = new Rect(screenPos.x, screenPos.y, outputSize.x, outputSize.y);

            using (var builder = renderGraph.AddRasterRenderPass<OccluderOverlayPassData>("OccluderOverlay", out var passData, m_ProfilingSamplerOccluderOverlay))
            {
                builder.AllowGlobalStateModification(true);

                builder.SetRenderAttachment(colorBuffer, 0);

                passData.debugMaterial = debugMaterial;
                passData.occluderTexture = occluderTexture;
                passData.viewport = viewport;
                passData.passIndex = passIndex;
                passData.validRange = debugSettings.occluderDebugViewRange;

                builder.SetRenderFunc(
                    static (OccluderOverlayPassData data, RasterGraphContext ctx) =>
                    {
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();

                        mpb.SetTexture("_OccluderTexture", data.occluderTexture);
                        mpb.SetVector("_ValidRange", data.validRange);

                        ctx.cmd.SetViewport(data.viewport);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugMaterial, data.passIndex, MeshTopology.Triangles, 3, 1, mpb);
                    });
            }
        }

        private void DispatchDebugClear(ComputeCommandBuffer cmd, EntityId viewID)
        {
            if (!m_ViewIDToIndexMap.TryGetValue(viewID, out var contextIndex))
                return;

            OccluderContext occluderCtx = m_OccluderContextData[contextIndex];

            if (UseOcclusionDebug(in occluderCtx) && occluderCtx.debugNeedsClear)
            {
                var cs = m_OcclusionDebugCS;
                int kernel = m_ClearOcclusionDebugKernel;

                cmd.SetComputeConstantBufferParam(cs, ShaderIDs.OcclusionCullingCommonShaderVariables, m_CommonConstantBuffer, 0, m_CommonConstantBuffer.stride);

                cmd.SetComputeBufferParam(cs, kernel, ShaderIDs._OcclusionDebugOverlay, occluderCtx.occlusionDebugOverlay);

                Vector2Int mip0Size = occluderCtx.occluderMipBounds[0].size;
                cmd.DispatchCompute(cs, kernel, (mip0Size.x + 7) / 8, (mip0Size.y + 7) / 8, occluderCtx.subviewCount);

                // mark as cleared in the dictionary
                occluderCtx.debugNeedsClear = false;
                m_OccluderContextData[contextIndex] = occluderCtx;
            }
        }

        private OccluderHandles PrepareOccluders(RenderGraph renderGraph, in OccluderParameters occluderParams)
        {
            OccluderHandles occluderHandles = new OccluderHandles();
            if (occluderParams.depthTexture.IsValid())
            {
                if (!m_ViewIDToIndexMap.TryGetValue(occluderParams.viewInstanceID, out var contextIndex))
                    contextIndex = NewContext(occluderParams.viewInstanceID);

                OccluderContext ctx = m_OccluderContextData[contextIndex];
                ctx.PrepareOccluders(occluderParams);
                occluderHandles = ctx.Import(renderGraph);
                m_OccluderContextData[contextIndex] = ctx;
            }
            else
            {
                DeleteContext(occluderParams.viewInstanceID);
            }
            return occluderHandles;
        }

        private void CreateFarDepthPyramid(ComputeCommandBuffer cmd, in OccluderParameters occluderParams, ReadOnlySpan<OccluderSubviewUpdate> occluderSubviewUpdates, in OccluderHandles occluderHandles)
        {
            if (!m_ViewIDToIndexMap.TryGetValue(occluderParams.viewInstanceID, out var contextIndex))
                return;

            var silhouettePlanes = m_SilhouettePlaneCache.GetSubArray(occluderParams.viewInstanceID);

            OccluderContext ctx = m_OccluderContextData[contextIndex];
            ctx.CreateFarDepthPyramid(cmd, occluderParams, occluderSubviewUpdates, occluderHandles, silhouettePlanes, m_OccluderDepthPyramidCS, m_OccluderDepthDownscaleKernel);
            ctx.version++;
            m_OccluderContextData[contextIndex] = ctx;

            var slot = m_OccluderContextSlots[contextIndex];
            slot.lastUsedFrameIndex = m_FrameIndex;
            m_OccluderContextSlots[contextIndex] = slot;
        }

        private class UpdateOccludersPassData
        {
            public OccluderParameters occluderParams;
            public List<OccluderSubviewUpdate> occluderSubviewUpdates;
            public OccluderHandles occluderHandles;
            public GPUResidentContext grdContext;
        }

        public bool UpdateInstanceOccluders(RenderGraph renderGraph, GPUResidentContext grdContext, in OccluderParameters occluderParams, ReadOnlySpan<OccluderSubviewUpdate> occluderSubviewUpdates)
        {
            var occluderHandles = PrepareOccluders(renderGraph, occluderParams);
            if (!occluderHandles.occluderDepthPyramid.IsValid())
                return false;

            using (var builder = renderGraph.AddComputePass<UpdateOccludersPassData>("Update Occluders", out var passData, m_ProfilingSamplerUpdateOccluders))
            {
                builder.AllowGlobalStateModification(true);

                passData.grdContext = grdContext;
                passData.occluderParams = occluderParams;
                if (passData.occluderSubviewUpdates is null)
                    passData.occluderSubviewUpdates = new List<OccluderSubviewUpdate>();
                else
                    passData.occluderSubviewUpdates.Clear();
                for (int i = 0; i < occluderSubviewUpdates.Length; ++i)
                    passData.occluderSubviewUpdates.Add(occluderSubviewUpdates[i]);
                passData.occluderHandles = occluderHandles;

                builder.UseTexture(passData.occluderParams.depthTexture);
                passData.occluderHandles.UseForOccluderUpdate(builder);

                builder.SetRenderFunc(
                    static (UpdateOccludersPassData data, ComputeGraphContext context) =>
                    {
                        Span<OccluderSubviewUpdate> occluderSubviewUpdates = stackalloc OccluderSubviewUpdate[data.occluderSubviewUpdates.Count];
                        int subviewMask = 0;
                        for (int i = 0; i < data.occluderSubviewUpdates.Count; ++i)
                        {
                            occluderSubviewUpdates[i] = data.occluderSubviewUpdates[i];
                            subviewMask |= 1 << data.occluderSubviewUpdates[i].subviewIndex;
                        }

                        OcclusionCullingCommon occlusionCullingCommon = data.grdContext.occlusionCullingCommon;
                        occlusionCullingCommon.CreateFarDepthPyramid(context.cmd, in data.occluderParams, occluderSubviewUpdates, in data.occluderHandles);
                        data.grdContext.culler.InstanceOccludersUpdated(data.occluderParams.viewInstanceID, subviewMask, occlusionCullingCommon);
                    });
            }

            return true;
        }

        internal void UpdateSilhouettePlanes(EntityId viewID, NativeArray<Plane> planes)
        {
            m_SilhouettePlaneCache.Update(viewID, planes, m_FrameIndex);
        }

        internal OcclusionCullingDebugOutput GetOcclusionTestDebugOutput(EntityId viewID)
        {
            if (m_ViewIDToIndexMap.TryGetValue(viewID, out var contextIndex) && m_OccluderContextSlots[contextIndex].valid)
                return m_OccluderContextData[contextIndex].GetDebugOutput();
            return new OcclusionCullingDebugOutput();
        }

        public void UpdateOccluderStats(DebugRendererBatcherStats debugStats)
        {
            debugStats.occluderStats.Clear();
            foreach (var pair in m_ViewIDToIndexMap)
            {
                if (pair.Value < m_OccluderContextSlots.Length && m_OccluderContextSlots[pair.Value].valid)
                {
                    debugStats.occluderStats.Add(new DebugOccluderStats
                    {
                        viewID = pair.Key,
                        subviewCount = m_OccluderContextData[pair.Value].subviewCount,
                        occluderMipLayoutSize = m_OccluderContextData[pair.Value].occluderMipLayoutSize,
                    });
                }
            }
        }

        internal bool HasOccluderContext(EntityId viewID)
        {
            return m_ViewIDToIndexMap.ContainsKey(viewID);
        }

        internal bool GetOccluderContext(EntityId viewID, out OccluderContext occluderContext)
        {
            if (m_ViewIDToIndexMap.TryGetValue(viewID, out var contextIndex) && m_OccluderContextSlots[contextIndex].valid)
            {
                occluderContext = m_OccluderContextData[contextIndex];
                return true;
            }

            occluderContext = new OccluderContext();
            return false;
        }

        internal void UpdateFrame()
        {
            for (int i = 0; i < m_OccluderContextData.Count; ++i)
            {
                if (!m_OccluderContextSlots[i].valid)
                    continue;

                OccluderContext occluderCtx = m_OccluderContextData[i];
                var slot = m_OccluderContextSlots[i];
                //Garbage collect unused contexts for a long time:
                if ((m_FrameIndex - slot.lastUsedFrameIndex) >= s_MaxContextGCFrame)
                {
                    DeleteContext(slot.viewID);
                    continue;
                }

                occluderCtx.debugNeedsClear = true;
                m_OccluderContextData[i] = occluderCtx;
            }
            m_SilhouettePlaneCache.FreeUnusedSlots(m_FrameIndex, s_MaxContextGCFrame);
            ++m_FrameIndex;
        }

        private int NewContext(EntityId viewID)
        {
            int newSlot = -1;
            var newCtxSlot = new OccluderContextSlot { valid = true, viewID = viewID, lastUsedFrameIndex = m_FrameIndex };
            var newCtx = new OccluderContext() {};
            if (m_FreeOccluderContexts.Length > 0)
            {
                newSlot = m_FreeOccluderContexts[m_FreeOccluderContexts.Length - 1];
                m_FreeOccluderContexts.RemoveAt(m_FreeOccluderContexts.Length - 1);
                m_OccluderContextData[newSlot] = newCtx;
                m_OccluderContextSlots[newSlot] = newCtxSlot;
            }
            else
            {
                newSlot = m_OccluderContextData.Count;
                m_OccluderContextData.Add(newCtx);
                m_OccluderContextSlots.Add(newCtxSlot);
            }

            m_ViewIDToIndexMap.Add(viewID, newSlot);
            return newSlot;
        }

        private void DeleteContext(EntityId viewID)
        {
            if (!m_ViewIDToIndexMap.TryGetValue(viewID, out var contextIndex) || !m_OccluderContextSlots[contextIndex].valid)
                return;

            m_OccluderContextData[contextIndex].Dispose();
            m_OccluderContextSlots[contextIndex] = new OccluderContextSlot { valid = false };
            m_FreeOccluderContexts.Add(contextIndex);
            m_ViewIDToIndexMap.Remove(viewID);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_DebugOcclusionTestMaterial);
            CoreUtils.Destroy(m_OccluderDebugViewMaterial);

            for (int i = 0; i < m_OccluderContextData.Count; ++i)
            {
                if (m_OccluderContextSlots[i].valid)
                    m_OccluderContextData[i].Dispose();
            }

            m_SilhouettePlaneCache.Dispose();

            m_ViewIDToIndexMap.Dispose();
            m_FreeOccluderContexts.Dispose();
            m_OccluderContextData.Clear();
            m_OccluderContextSlots.Dispose();

            m_CommonShaderVariables.Dispose();
            m_CommonConstantBuffer.Release();
            m_DebugShaderVariables.Dispose();
            m_DebugConstantBuffer.Release();
        }
    }
}
