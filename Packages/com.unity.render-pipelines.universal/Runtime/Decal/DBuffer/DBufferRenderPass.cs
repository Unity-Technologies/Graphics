using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalDrawDBufferSystem : DecalDrawSystem
    {
        public DecalDrawDBufferSystem(DecalEntityManager entityManager) : base("DecalDrawIntoDBufferSystem.Execute", entityManager) { }
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk) => decalCachedChunk.passIndexDBuffer;
    }

    internal class DBufferRenderPass : ScriptableRenderPass
    {
        internal static string[] s_DBufferNames = { "_DBufferTexture0", "_DBufferTexture1", "_DBufferTexture2", "_DBufferTexture3" };
        internal static string s_DBufferDepthName = "DBufferDepth";

        private DecalDrawDBufferSystem m_DrawSystem;
        private DBufferSettings m_Settings;
        private Material m_DBufferClear;

        private FilteringSettings m_FilteringSettings;
        private List<ShaderTagId> m_ShaderTagIdList;
        private ProfilingSampler m_ProfilingSampler;
        private ProfilingSampler m_DBufferClearSampler;

        private bool m_DecalLayers;

        private RTHandle m_DBufferDepth;

        private PassData m_PassData;

        internal RTHandle[] dBufferColorHandles { get; private set; }
        internal RTHandle depthHandle { get; private set; }
        internal RTHandle dBufferDepth { get => m_DBufferDepth; }

        private TextureHandle[] dbufferHandles;

        public DBufferRenderPass(Material dBufferClear, DBufferSettings settings, DecalDrawDBufferSystem drawSystem, bool decalLayers)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses + 1;

            var scriptableRenderPassInput = ScriptableRenderPassInput.Normal;
            ConfigureInput(scriptableRenderPassInput);

            m_DrawSystem = drawSystem;
            m_Settings = settings;
            m_DBufferClear = dBufferClear;
            m_ProfilingSampler = new ProfilingSampler("DBuffer Render");
            m_DBufferClearSampler = new ProfilingSampler("Clear");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
            m_DecalLayers = decalLayers;

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DBufferMesh));
            m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DBufferProjectorVFX));

            int dBufferCount = (int)settings.surfaceData + 1;
            dBufferColorHandles = new RTHandle[dBufferCount];

            m_PassData = new PassData();
        }

        public void Dispose()
        {
            m_DBufferDepth?.Release();
            foreach (var handle in dBufferColorHandles)
                handle?.Release();
        }

        public void Setup(in CameraData cameraData)
        {
            var depthDesc = cameraData.cameraTargetDescriptor;
            depthDesc.graphicsFormat = GraphicsFormat.None; //Depth only rendering
            depthDesc.depthStencilFormat = cameraData.cameraTargetDescriptor.depthStencilFormat;
            depthDesc.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(ref m_DBufferDepth, depthDesc, name: s_DBufferDepthName);

            Setup(cameraData, m_DBufferDepth);
        }

        public void Setup(in CameraData cameraData, RTHandle depthTextureHandle)
        {
            // base
            {
                var desc = cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = QualitySettings.activeColorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;

                RenderingUtils.ReAllocateIfNeeded(ref dBufferColorHandles[0], desc, name: s_DBufferNames[0]);
            }

            if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormal || m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
            {
                var desc = cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;

                RenderingUtils.ReAllocateIfNeeded(ref dBufferColorHandles[1], desc, name: s_DBufferNames[1]);
            }

            if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
            {
                var desc = cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;

                RenderingUtils.ReAllocateIfNeeded(ref dBufferColorHandles[2], desc, name: s_DBufferNames[2]);
            }

            // depth
            depthHandle = depthTextureHandle;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(dBufferColorHandles, depthHandle);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            InitPassData(ref m_PassData);
            var cmd = renderingData.commandBuffer;
            var passData = m_PassData;
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SetGlobalTextures(renderingData.commandBuffer, m_PassData);
                SetKeywords(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData);
                Clear(renderingData.commandBuffer, m_PassData);

                var param = InitRendererListParams(ref renderingData);
                var rendererList = context.CreateRendererList(ref param);
                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, rendererList, ref renderingData, false);
            }
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList, ref RenderingData renderingData, bool renderGraph)
        {
            passData.drawSystem.Execute(cmd);
            cmd.DrawRendererList(rendererList);
        }

        private static void SetGlobalTextures(CommandBuffer cmd, PassData passData)
        {
            var dBufferColorHandles = passData.dBufferColorHandles;
            cmd.SetGlobalTexture(dBufferColorHandles[0].name, dBufferColorHandles[0].nameID);
            if (passData.settings.surfaceData == DecalSurfaceData.AlbedoNormal || passData.settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
                cmd.SetGlobalTexture(dBufferColorHandles[1].name, dBufferColorHandles[1].nameID);
            if (passData.settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
                cmd.SetGlobalTexture(dBufferColorHandles[2].name, dBufferColorHandles[2].nameID);
        }

        private static void SetKeywords(RasterCommandBuffer cmd, PassData passData)
        {
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT1, passData.settings.surfaceData == DecalSurfaceData.Albedo);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT2, passData.settings.surfaceData == DecalSurfaceData.AlbedoNormal);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT3, passData.settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS);

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalLayers, passData.decalLayers);
        }

        private static void Clear(CommandBuffer cmd, PassData passData)
        {
            // TODO: This should be replace with mrt clear once we support it
            // Clear render targets
            using (new ProfilingScope(cmd, passData.dBufferClearSampler))
            {
                // for alpha compositing, color is cleared to 0, alpha to 1
                // https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
                Blitter.BlitTexture(cmd, passData.dBufferColorHandles[0], new Vector4(1, 1, 0, 0), passData.dBufferClear, 0);
            }
        }

        private class PassData
        {
            internal DecalDrawDBufferSystem drawSystem;
            internal DBufferSettings settings;
            internal Material dBufferClear;

            internal ProfilingSampler dBufferClearSampler;

            internal bool decalLayers;
            internal RTHandle dBufferDepth;
            internal RTHandle[] dBufferColorHandles;

            internal RenderingData renderingData;
            internal RendererListHandle rendererList;
        }

        private void InitPassData(ref PassData passData)
        {
            passData.drawSystem = m_DrawSystem;
            passData.settings = m_Settings;
            passData.dBufferClear = m_DBufferClear;
            passData.dBufferClearSampler = m_DBufferClearSampler;
            passData.decalLayers = m_DecalLayers;
            passData.dBufferDepth = m_DBufferDepth;
            passData.dBufferColorHandles = dBufferColorHandles;
        }

        private RendererListParams InitRendererListParams(ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            return new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

            TextureHandle cameraDepthTexture = frameResources.GetTexture(UniversalResource.CameraDepthTexture);
            TextureHandle cameraNormalsTexture = frameResources.GetTexture(UniversalResource.CameraNormalsTexture);

            TextureHandle depthSrc = (renderer.renderingModeActual == RenderingMode.Deferred)? renderer.activeDepthTexture : cameraDepthTexture;
            TextureHandle depthTarget = frameResources.GetTexture(UniversalResource.DBufferDepth);
            RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID("_CameraDepthTexture"), depthSrc);
            RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID("_CameraNormalsTexture"), cameraNormalsTexture);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("DBuffer Pass", out var passData, m_ProfilingSampler))
            {
                InitPassData(ref passData);
                passData.renderingData = renderingData;

                if (dbufferHandles == null)
                    dbufferHandles = new TextureHandle[RenderGraphUtils.DBufferSize];

                // base
                {
                    var desc = renderingData.cameraData.cameraTargetDescriptor;
                    desc.graphicsFormat = QualitySettings.activeColorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                    desc.depthBufferBits = 0;
                    desc.msaaSamples = 1;
                    dbufferHandles[0] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, s_DBufferNames[0], true, new Color(0, 0, 0, 1));
                    builder.UseTextureFragment(dbufferHandles[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                }

                if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormal || m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
                {
                    var desc = renderingData.cameraData.cameraTargetDescriptor;
                    desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                    desc.depthBufferBits = 0;
                    desc.msaaSamples = 1;
                    dbufferHandles[1] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, s_DBufferNames[1], true, new Color(0.5f, 0.5f, 0.5f, 1));
                    builder.UseTextureFragment(dbufferHandles[1], 1, IBaseRenderGraphBuilder.AccessFlags.Write);
                }

                if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
                {
                    var desc = renderingData.cameraData.cameraTargetDescriptor;
                    desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                    desc.depthBufferBits = 0;
                    desc.msaaSamples = 1;
                    dbufferHandles[2] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, s_DBufferNames[2], true, new Color(0, 0, 0, 1));
                    builder.UseTextureFragment(dbufferHandles[2], 2, IBaseRenderGraphBuilder.AccessFlags.Write);
                }

                builder.UseTextureFragmentDepth(depthTarget, IBaseRenderGraphBuilder.AccessFlags.Read);

                if (cameraDepthTexture.IsValid())
                    builder.UseTexture(depthSrc, IBaseRenderGraphBuilder.AccessFlags.Read);
                if (cameraNormalsTexture.IsValid())
                    builder.UseTexture(cameraNormalsTexture, IBaseRenderGraphBuilder.AccessFlags.Read);

                var param = InitRendererListParams(ref renderingData);
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    var cmd = rgContext.cmd;
                    SetKeywords(rgContext.cmd, data);
                    ExecutePass(rgContext.cmd, data, data.rendererList, ref data.renderingData, true);
                });
            }

            for (int i = 0; i < RenderGraphUtils.DBufferSize; ++i)
            {
                if (dbufferHandles[i].IsValid())
                    RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID(s_DBufferNames[i]), dbufferHandles[i]);
                frameResources.SetTexture((UniversalResource) (UniversalResource.DBuffer0 + i), dbufferHandles[i]);
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new System.ArgumentNullException("cmd");
            }

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT1, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT2, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT3, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalLayers, false);
        }
    }
}
