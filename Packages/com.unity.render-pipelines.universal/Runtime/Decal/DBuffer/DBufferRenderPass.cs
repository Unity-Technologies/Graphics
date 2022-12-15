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
            using (new ProfilingScope(cmd, passData.profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SetGlobalTextures(renderingData.commandBuffer, m_PassData);
                SetKeywords(renderingData.commandBuffer, m_PassData);
                Clear(renderingData.commandBuffer, m_PassData);
                ExecutePass(context, m_PassData, ref renderingData, renderingData.commandBuffer, false);
            }
        }

        private static void ExecutePass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData, CommandBuffer cmd, bool renderGraph)
        {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(passData.shaderTagIdList, ref renderingData, sortingCriteria);

            passData.drawSystem.Execute(cmd);

            var param = new RendererListParams(renderingData.cullResults, drawingSettings, passData.filteringSettings);
            var rl = context.CreateRendererList(ref param);
            cmd.DrawRendererList(rl);
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

        private static void SetKeywords(CommandBuffer cmd, PassData passData)
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

            internal FilteringSettings filteringSettings;
            internal List<ShaderTagId> shaderTagIdList;
            internal ProfilingSampler profilingSampler;
            internal ProfilingSampler dBufferClearSampler;

            internal bool decalLayers;
            internal RTHandle dBufferDepth;
            internal RTHandle[] dBufferColorHandles;

            internal RenderingData renderingData;
        }

        void InitPassData(ref PassData passData)
        {
            passData.drawSystem = m_DrawSystem;
            passData.settings = m_Settings;
            passData.dBufferClear = m_DBufferClear;
            passData.filteringSettings = m_FilteringSettings;
            passData.shaderTagIdList = m_ShaderTagIdList;
            passData.profilingSampler = m_ProfilingSampler;
            passData.dBufferClearSampler = m_DBufferClearSampler;
            passData.decalLayers = m_DecalLayers;
            passData.dBufferDepth = m_DBufferDepth;
            passData.dBufferColorHandles = dBufferColorHandles;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

            TextureHandle cameraDepthTexture = frameResources.GetTexture(UniversalResource.CameraDepthTexture);
            TextureHandle cameraNormalsTexture = frameResources.GetTexture(UniversalResource.CameraNormalsTexture);

            TextureHandle depthTarget = (renderer.renderingModeActual == RenderingMode.Deferred) ? renderer.activeDepthTexture : cameraDepthTexture;

            RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID("_CameraDepthTexture"), depthTarget);
            RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID("_CameraNormalsTexture"), cameraNormalsTexture);

            using (var builder = renderGraph.AddRenderPass<PassData>("DBuffer Pass", out var passData, m_ProfilingSampler))
            {
                InitPassData(ref passData);
                passData.renderingData = renderingData;

                // TODO RENDERGRAPH: move decals frame resources to the new FrameResources manager

                if (renderer.frameResources.dbuffer == null)
                    renderer.frameResources.dbuffer = new TextureHandle[3];

                // base
                {
                    var desc = renderingData.cameraData.cameraTargetDescriptor;
                    desc.graphicsFormat = QualitySettings.activeColorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                    desc.depthBufferBits = 0;
                    desc.msaaSamples = 1;

                    renderer.frameResources.dbuffer[0] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, s_DBufferNames[0], true, new Color(0, 0, 0, 1));
                    builder.UseColorBuffer(renderer.frameResources.dbuffer[0], 0);
                }

                if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormal || m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
                {
                    var desc = renderingData.cameraData.cameraTargetDescriptor;
                    desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                    desc.depthBufferBits = 0;
                    desc.msaaSamples = 1;

                    renderer.frameResources.dbuffer[1] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, s_DBufferNames[1], true, new Color(0.5f, 0.5f, 0.5f, 1));
                    builder.UseColorBuffer(renderer.frameResources.dbuffer[1], 1);
                }

                if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
                {
                    var desc = renderingData.cameraData.cameraTargetDescriptor;
                    desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                    desc.depthBufferBits = 0;
                    desc.msaaSamples = 1;

                    renderer.frameResources.dbuffer[2] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, s_DBufferNames[2], true, new Color(0, 0, 0, 1));
                    builder.UseColorBuffer(renderer.frameResources.dbuffer[2], 2);
                }

                builder.UseDepthBuffer(renderer.frameResources.dbufferDepth, DepthAccess.Read);

                if (cameraDepthTexture.IsValid())
                    builder.ReadTexture(cameraDepthTexture);
                if (cameraNormalsTexture.IsValid())
                    builder.ReadTexture(cameraNormalsTexture);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    var cmd = rgContext.cmd;
                    SetKeywords(rgContext.cmd, data);
                    ExecutePass(rgContext.renderContext, data, ref data.renderingData, cmd, true);
                });
            }

            RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID(s_DBufferNames[0]), renderer.frameResources.dbuffer[0]);
            if (renderer.frameResources.dbuffer[1].IsValid())
                RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID(s_DBufferNames[1]), renderer.frameResources.dbuffer[1]);
            if (renderer.frameResources.dbuffer[2].IsValid())
                RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID(s_DBufferNames[2]), renderer.frameResources.dbuffer[2]);
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
