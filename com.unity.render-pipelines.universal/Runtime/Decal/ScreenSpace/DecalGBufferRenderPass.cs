using System.Collections.Generic;
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

        public DecalGBufferRenderPass(DecalScreenSpaceSettings settings, DecalDrawGBufferSystem drawSystem, bool decalLayers)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;
            if (decalLayers)
                ConfigureInput(ScriptableRenderPassInput.RenderingLayer);

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
        }

        internal void Setup(DeferredLights deferredLights)
        {
            m_DeferredLights = deferredLights;
        }

        //public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) // todo find out why on camera setup fails here
        {
            if (m_DeferredLights.UseRenderPass)
            {
                //if (m_GbufferAttachments == null)
                m_GbufferAttachments = new RTHandle[]
                {
                        m_DeferredLights.GbufferAttachments[0], m_DeferredLights.GbufferAttachments[1],
                        m_DeferredLights.GbufferAttachments[2], m_DeferredLights.GbufferAttachments[3]
                };

                //m_GbufferAttachments = m_DeferredLights.GbufferAttachmentIdentifiers;

                if (m_DecalLayers)
                {
                    var deferredInputAttachments = new RTHandle[]
                    {
                        //m_DeferredLights.DepthCopyTexture,
                        m_DeferredLights.GbufferAttachments[m_DeferredLights.GbufferDepthIndex],
                        m_DeferredLights.GbufferAttachments[m_DeferredLights.GBufferRenderingLayers],
                    };

                    var deferredInputIsTransient = new bool[]
                    {
                        true, false, // todo
                    };

                    ConfigureInputAttachments(deferredInputAttachments, deferredInputIsTransient);
                }
                else
                {
                    var deferredInputAttachments = new RTHandle[]
                    {
                        //m_DeferredLights.DepthCopyTexture,
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
                //m_GbufferAttachments = m_DeferredLights.GbufferAttachmentIdentifiers;

                //if (m_GbufferAttachments == null)
                m_GbufferAttachments = new RTHandle[]
                {
                        m_DeferredLights.GbufferAttachments[0], m_DeferredLights.GbufferAttachments[1],
                        m_DeferredLights.GbufferAttachments[2], m_DeferredLights.GbufferAttachments[3]
                };
                if (m_DeferredLights.GbufferAttachments[0].rt == null)
                    Debug.Assert(m_DeferredLights.GbufferAttachments[0].rt != null);
            }

            ConfigureTarget(m_GbufferAttachments, m_DeferredLights.DepthAttachmentHandle);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                NormalReconstruction.SetupProperties(cmd, renderingData.cameraData);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendLow, m_Settings.normalBlend == DecalNormalBlend.Low);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendMedium, m_Settings.normalBlend == DecalNormalBlend.Medium);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendHigh, m_Settings.normalBlend == DecalNormalBlend.High);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalLayers, m_DecalLayers);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                m_DrawSystem?.Execute(cmd);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
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
