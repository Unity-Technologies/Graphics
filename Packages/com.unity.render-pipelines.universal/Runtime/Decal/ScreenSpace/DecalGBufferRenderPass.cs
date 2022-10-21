using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalDrawGBufferSystem : DecalDrawSystem
    {
        public DecalDrawGBufferSystem(DecalEntityManager entityManager) : base("DecalDrawGBufferSystem.Execute", entityManager) { }
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk) => decalCachedChunk.passIndexGBuffer;
    }

    internal class DecalGBufferRenderPass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler;
        private List<ShaderTagId> m_ShaderTagIdList;
        private DecalDrawGBufferSystem m_DrawSystem;
        private DecalScreenSpaceSettings m_Settings;
        private DeferredLights m_DeferredLights;
        private RTHandle[] m_GbufferAttachments;
        private bool m_DecalLayers;
        private PassData m_PassData;

        public DecalGBufferRenderPass(DecalScreenSpaceSettings settings, DecalDrawGBufferSystem drawSystem, bool decalLayers)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;

            m_DrawSystem = drawSystem;
            m_Settings = settings;
            m_ProfilingSampler = new ProfilingSampler("Decal GBuffer Render");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
            m_DecalLayers = decalLayers;

            m_ShaderTagIdList = new List<ShaderTagId>();
            if (drawSystem == null)
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalGBufferProjector));
            else
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalGBufferMesh));

            m_PassData = new PassData();
        }

        internal void Setup(DeferredLights deferredLights)
        {
            m_DeferredLights = deferredLights;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (m_DeferredLights.UseRenderPass)
            {
                m_GbufferAttachments = new RTHandle[]
                {
                    m_DeferredLights.GbufferAttachments[0], m_DeferredLights.GbufferAttachments[1],
                    m_DeferredLights.GbufferAttachments[2], m_DeferredLights.GbufferAttachments[3]
                };

                if (m_DecalLayers)
                {
                    var deferredInputAttachments = new RTHandle[]
                    {
                        m_DeferredLights.GbufferAttachments[m_DeferredLights.GbufferDepthIndex],
                        m_DeferredLights.GbufferAttachments[m_DeferredLights.GBufferRenderingLayers],
                    };

                    var deferredInputIsTransient = new bool[]
                    {
                        true, false, // TODO: Make rendering layers transient
                    };

                    ConfigureInputAttachments(deferredInputAttachments, deferredInputIsTransient);
                }
                else
                {
                    var deferredInputAttachments = new RTHandle[]
                    {
                        m_DeferredLights.GbufferAttachments[m_DeferredLights.GbufferDepthIndex],
                    };

                    var deferredInputIsTransient = new bool[]
                    {
                        true,
                    };

                    ConfigureInputAttachments(deferredInputAttachments, deferredInputIsTransient);
                }
            }
            else
            {
                m_GbufferAttachments = new RTHandle[]
                {
                        m_DeferredLights.GbufferAttachments[0], m_DeferredLights.GbufferAttachments[1],
                        m_DeferredLights.GbufferAttachments[2], m_DeferredLights.GbufferAttachments[3]
                };
            }

            ConfigureTarget(m_GbufferAttachments, m_DeferredLights.DepthAttachmentHandle);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            InitPassData(ref m_PassData);
            ExecutePass(context, m_PassData, ref renderingData, renderingData.commandBuffer);
        }

        private class PassData
        {
            internal FilteringSettings filteringSettings;
            internal ProfilingSampler profilingSampler;
            internal List<ShaderTagId> shaderTagIdList;
            internal DecalDrawGBufferSystem drawSystem;
            internal DecalScreenSpaceSettings settings;
            internal bool decalLayers;

            internal RenderingData renderingData;
        }

        void InitPassData(ref PassData passData)
        {
            passData.filteringSettings = m_FilteringSettings;
            passData.profilingSampler = m_ProfilingSampler;
            passData.shaderTagIdList = m_ShaderTagIdList;
            passData.drawSystem = m_DrawSystem;
            passData.settings = m_Settings;
            passData.decalLayers = m_DecalLayers;
        }

        private static void ExecutePass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData, CommandBuffer cmd)
        {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(passData.shaderTagIdList, ref renderingData, sortingCriteria);

            using (new ProfilingScope(cmd, passData.profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                NormalReconstruction.SetupProperties(cmd, renderingData.cameraData);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendLow, passData.settings.normalBlend == DecalNormalBlend.Low);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendMedium, passData.settings.normalBlend == DecalNormalBlend.Medium);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendHigh, passData.settings.normalBlend == DecalNormalBlend.High);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalLayers, passData.decalLayers);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                passData.drawSystem?.Execute(cmd);

                var param = new RendererListParams(renderingData.cullResults, drawingSettings, passData.filteringSettings);
                var rl = context.CreateRendererList(ref param);
                cmd.DrawRendererList(rl);
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

            RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID("_CameraDepthTexture"), renderer.frameResources.cameraDepthTexture);

            using (var builder = renderGraph.AddRenderPass<PassData>("Decal GBuffer Pass", out var passData, m_ProfilingSampler))
            {
                InitPassData(ref passData);
                passData.renderingData = renderingData;

                UniversalRenderer.RenderGraphFrameResources frameResources = renderer.frameResources;

                builder.UseColorBuffer(frameResources.gbuffer[0], 0);
                builder.UseColorBuffer(frameResources.gbuffer[1], 1);
                builder.UseColorBuffer(frameResources.gbuffer[2], 2);
                builder.UseColorBuffer(frameResources.gbuffer[3], 3);
                builder.UseDepthBuffer(UniversalRenderer.m_ActiveRenderGraphDepth, DepthAccess.Read);

                if (frameResources.cameraDepthTexture.IsValid())
                    builder.ReadTexture(frameResources.cameraDepthTexture);

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    ExecutePass(rgContext.renderContext, data, ref data.renderingData, data.renderingData.commandBuffer);
                });
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new System.ArgumentNullException("cmd");
            }

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendLow, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendMedium, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendHigh, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalLayers, false);
        }
    }
}
