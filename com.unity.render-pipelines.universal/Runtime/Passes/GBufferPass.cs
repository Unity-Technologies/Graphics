using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
        DeferredLights m_DeferredLights;

        ShaderTagId m_ShaderTagId = new ShaderTagId("UniversalGBuffer");
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render GBuffer");

        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;

        public GBufferPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference, DeferredLights deferredLights)
        {
            base.renderPassEvent = evt;

            m_DeferredLights = deferredLights;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            if (stencilState.enabled)
            {
                m_RenderStateBlock.stencilReference = stencilReference;
                m_RenderStateBlock.mask = RenderStateMask.Stencil;
                m_RenderStateBlock.stencilState = stencilState;
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTargetHandle[] gbufferAttachments = m_DeferredLights.GbufferAttachments;

                // Create and declare the render targets used in the pass
            for (int i = 0; i < gbufferAttachments.Length; ++i)
            {
                // Lighting buffer has already been declared with line ConfigureCameraTarget(m_ActiveCameraColorAttachment.Identifier(), ...) in DeferredRenderer.Setup
                if (i != m_DeferredLights.GBufferLightingIndex)
                {
                    RenderTextureDescriptor gbufferSlice = cameraTextureDescriptor;
                    gbufferSlice.graphicsFormat = m_DeferredLights.GetGBufferFormat(i);
                    cmd.GetTemporaryRT(m_DeferredLights.GbufferAttachments[i].id, gbufferSlice);
                }
            }

            ConfigureTarget(m_DeferredLights.GbufferAttachmentIdentifiers, m_DeferredLights.DepthAttachmentIdentifier);

            // If depth-prepass exists, do not clear depth here or we will lose it.
            // Lighting buffer is cleared independently regardless of what we ask for here.
            ConfigureClear(m_DeferredLights.HasDepthPrepass ? ClearFlag.None : ClearFlag.Depth, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer gbufferCommands = CommandBufferPool.Get("Render GBuffer");
            using (new ProfilingScope(gbufferCommands, m_ProfilingSampler))
            {
                if (m_DeferredLights.AccurateGbufferNormals)
                    gbufferCommands.EnableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
                else
                    gbufferCommands.DisableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);

                context.ExecuteCommandBuffer(gbufferCommands); // send the gbufferCommands to the scriptableRenderContext - this should be done *before* calling scriptableRenderContext.DrawRenderers
                gbufferCommands.Clear();

                DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings/*, ref m_RenderStateBlock*/);

                // Render objects that did not match any shader pass with error shader
                RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, m_FilteringSettings, SortingCriteria.None);
            }
            context.ExecuteCommandBuffer(gbufferCommands);
            CommandBufferPool.Release(gbufferCommands);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            RenderTargetHandle[] gbufferAttachments = m_DeferredLights.GbufferAttachments;

            for (int i = 0; i < gbufferAttachments.Length; ++i)
                if (i != m_DeferredLights.GBufferLightingIndex)
                    cmd.ReleaseTemporaryRT(gbufferAttachments[i].id);
        }
    }
}
