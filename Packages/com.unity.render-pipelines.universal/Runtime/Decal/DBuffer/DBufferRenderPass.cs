using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
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
        static readonly int s_SSAOTextureID = Shader.PropertyToID("_ScreenSpaceOcclusionTexture");

        private DecalDrawDBufferSystem m_DrawSystem;
        private DBufferSettings m_Settings;

        private FilteringSettings m_FilteringSettings;
        private List<ShaderTagId> m_ShaderTagIdList;

        private bool m_DecalLayers;

        private TextureHandle[] dbufferHandles;

        public DBufferRenderPass(Material dBufferClear, DBufferSettings settings, DecalDrawDBufferSystem drawSystem, bool decalLayers)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses + 1;

            var scriptableRenderPassInput = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            ConfigureInput(scriptableRenderPassInput);

            // DBuffer requires color texture created as it does not handle y flip correctly
            requiresIntermediateTexture = true;

            m_DrawSystem = drawSystem;
            m_Settings = settings;
            profilingSampler = new ProfilingSampler("Draw DBuffer");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
            m_DecalLayers = decalLayers;

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DBufferMesh));
            m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DBufferProjectorVFX));
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList, bool renderGraph)
        {
            passData.drawSystem.Execute(cmd);
            cmd.DrawRendererList(rendererList);
        }

        private static void SetKeywords(RasterCommandBuffer cmd, PassData passData)
        {
            cmd.SetKeyword(ShaderGlobalKeywords.DBufferMRT1, passData.settings.surfaceData == DecalSurfaceData.Albedo);
            cmd.SetKeyword(ShaderGlobalKeywords.DBufferMRT2, passData.settings.surfaceData == DecalSurfaceData.AlbedoNormal);
            cmd.SetKeyword(ShaderGlobalKeywords.DBufferMRT3, passData.settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS);

            cmd.SetKeyword(ShaderGlobalKeywords.DecalLayers, passData.decalLayers);
        }

        private class PassData
        {
            internal DecalDrawDBufferSystem drawSystem;
            internal DBufferSettings settings;
            internal bool decalLayers;
            internal RTHandle dBufferDepth;
            internal RTHandle[] dBufferColorHandles;

            internal RendererListHandle rendererList;
        }

        private void InitPassData(ref PassData passData)
        {
            passData.drawSystem = m_DrawSystem;
            passData.settings = m_Settings;
            passData.decalLayers = m_DecalLayers;
        }

        private RendererListParams InitRendererListParams(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
        {
            SortingCriteria sortingCriteria = cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData, cameraData, lightData, sortingCriteria);
            return new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
            TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;

            TextureHandle depthTarget = resourceData.dBufferDepth.IsValid() ? resourceData.dBufferDepth : resourceData.activeDepthTexture;

            TextureHandle renderingLayersTexture = resourceData.renderingLayersTexture;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                InitPassData(ref passData);

                if (dbufferHandles == null)
                    dbufferHandles = new TextureHandle[RenderGraphUtils.DBufferSize];

                // base
                {
                    var desc = cameraData.cameraTargetDescriptor;
                    desc.graphicsFormat = QualitySettings.activeColorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                    desc.depthStencilFormat = GraphicsFormat.None;
                    desc.msaaSamples = 1;
                    dbufferHandles[0] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, s_DBufferNames[0], true, new Color(0, 0, 0, 1));
                    builder.SetRenderAttachment(dbufferHandles[0], 0, AccessFlags.Write);
                }

                if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormal || m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
                {
                    var desc = cameraData.cameraTargetDescriptor;
                    desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                    desc.depthStencilFormat = GraphicsFormat.None;
                    desc.msaaSamples = 1;
                    dbufferHandles[1] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, s_DBufferNames[1], true, new Color(0.5f, 0.5f, 0.5f, 1));
                    builder.SetRenderAttachment(dbufferHandles[1], 1, AccessFlags.Write);
                }

                if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
                {
                    var desc = cameraData.cameraTargetDescriptor;
                    desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                    desc.depthStencilFormat = GraphicsFormat.None;
                    desc.msaaSamples = 1;
                    dbufferHandles[2] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, s_DBufferNames[2], true, new Color(0, 0, 0, 1));
                    builder.SetRenderAttachment(dbufferHandles[2], 2, AccessFlags.Write);
                }

                builder.SetRenderAttachmentDepth(depthTarget, AccessFlags.Read);

                if (cameraDepthTexture.IsValid())
                    builder.UseTexture(cameraDepthTexture, AccessFlags.Read);
                if (cameraNormalsTexture.IsValid())
                    builder.UseTexture(cameraNormalsTexture, AccessFlags.Read);
                if (passData.decalLayers && renderingLayersTexture.IsValid())
                    builder.UseTexture(renderingLayersTexture, AccessFlags.Read);

                if (resourceData.ssaoTexture.IsValid())
                    builder.UseGlobalTexture(s_SSAOTextureID);

                var param = InitRendererListParams(renderingData, cameraData, lightData);
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                for (int i = 0; i < RenderGraphUtils.DBufferSize; ++i)
                {
                    if (dbufferHandles[i].IsValid())
                        builder.SetGlobalTextureAfterPass(dbufferHandles[i], Shader.PropertyToID(s_DBufferNames[i]));
                }

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    SetKeywords(rgContext.cmd, data);
                    ExecutePass(rgContext.cmd, data, data.rendererList, true);
                });
            }

            resourceData.dBuffer = dbufferHandles;
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new System.ArgumentNullException("cmd");
            }

            cmd.SetKeyword(ShaderGlobalKeywords.DBufferMRT1, false);
            cmd.SetKeyword(ShaderGlobalKeywords.DBufferMRT2, false);
            cmd.SetKeyword(ShaderGlobalKeywords.DBufferMRT3, false);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalLayers, false);
        }
    }
}
