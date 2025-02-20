using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
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
            profilingSampler = new ProfilingSampler("Draw Decal To GBuffer");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
            m_DecalLayers = decalLayers;

            m_ShaderTagIdList = new List<ShaderTagId>();
            if (drawSystem == null)
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalGBufferProjector));
            else
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalGBufferMesh));

            m_PassData = new PassData();
            m_GbufferAttachments = new RTHandle[UniversalRenderer.k_GbufferCountMandatory];

            breakGBufferAndDeferredRenderPass = false;
        }

        internal void Setup(DeferredLights deferredLights)
        {
            m_DeferredLights = deferredLights;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (m_DeferredLights.UseFramebufferFetch)
            {
                m_GbufferAttachments[0] = m_DeferredLights.GbufferAttachments[0];
                m_GbufferAttachments[1] = m_DeferredLights.GbufferAttachments[1];
                m_GbufferAttachments[2] = m_DeferredLights.GbufferAttachments[2];
                m_GbufferAttachments[3] = m_DeferredLights.GbufferAttachments[3];

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

                    // Disable obsolete warning for internal usage
                    #pragma warning disable CS0618
                    ConfigureInputAttachments(deferredInputAttachments, deferredInputIsTransient);
                    #pragma warning restore CS0618
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

                    // Disable obsolete warning for internal usage
                    #pragma warning disable CS0618
                    ConfigureInputAttachments(deferredInputAttachments, deferredInputIsTransient);
                    #pragma warning restore CS0618
                }
            }
            else
            {
                m_GbufferAttachments[0] = m_DeferredLights.GbufferAttachments[0];
                m_GbufferAttachments[1] = m_DeferredLights.GbufferAttachments[1];
                m_GbufferAttachments[2] = m_DeferredLights.GbufferAttachments[2];
                m_GbufferAttachments[3] = m_DeferredLights.GbufferAttachments[3];
            }

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            ConfigureTarget(m_GbufferAttachments, m_DeferredLights.DepthAttachmentHandle);
            #pragma warning restore CS0618
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();

            InitPassData(cameraData, ref m_PassData);

            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            var param = new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
            var rendererList = context.CreateRendererList(ref param);
            using (new ProfilingScope(renderingData.commandBuffer, profilingSampler))
            {
                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, rendererList);
            }
        }

        private class PassData
        {
            internal DecalDrawGBufferSystem drawSystem;
            internal DecalScreenSpaceSettings settings;
            internal bool decalLayers;

            internal UniversalCameraData cameraData;
            internal RendererListHandle rendererList;
        }

        private void InitPassData(UniversalCameraData cameraData, ref PassData passData)
        {
            passData.drawSystem = m_DrawSystem;
            passData.settings = m_Settings;
            passData.decalLayers = m_DecalLayers;
            passData.cameraData = cameraData;
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList)
        {
            NormalReconstruction.SetupProperties(cmd, passData.cameraData);

            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendLow, passData.settings.normalBlend == DecalNormalBlend.Low);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendMedium, passData.settings.normalBlend == DecalNormalBlend.Medium);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendHigh, passData.settings.normalBlend == DecalNormalBlend.High);

            cmd.SetKeyword(ShaderGlobalKeywords.DecalLayers, passData.decalLayers);

            passData.drawSystem?.Execute(cmd);
            cmd.DrawRendererList(rendererList);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                InitPassData(cameraData, ref passData);

                TextureHandle[] gBufferHandles = resourceData.gBuffer;
                builder.SetRenderAttachment(gBufferHandles[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(gBufferHandles[1], 1, AccessFlags.Write);
                builder.SetRenderAttachment(gBufferHandles[2], 2, AccessFlags.Write);
                builder.SetRenderAttachment(gBufferHandles[3], 3, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

                if (renderGraph.nativeRenderPassesEnabled)
                {
                    builder.SetInputAttachment(gBufferHandles[UniversalRenderer.k_GbufferCountMandatory], 0, AccessFlags.Read);
                    if (m_DecalLayers)
                        builder.SetInputAttachment(gBufferHandles[UniversalRenderer.k_GbufferCountMandatory + 1], 1, AccessFlags.Read);
                }
                else if (cameraDepthTexture.IsValid())
                    builder.UseTexture(cameraDepthTexture, AccessFlags.Read);

                SortingCriteria sortingCriteria = passData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData,
                    passData.cameraData, lightData, sortingCriteria);
                var param = new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    ExecutePass(rgContext.cmd, data, data.rendererList);
                });
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new System.ArgumentNullException("cmd");
            }

            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendLow, false);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendMedium, false);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendHigh, false);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalLayers, false);
        }
    }
}
