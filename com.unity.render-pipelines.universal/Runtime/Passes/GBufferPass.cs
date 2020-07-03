using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
        RenderTargetHandle[] m_ColorAttachments;
        RenderTargetHandle m_DepthBufferAttachment;

        DeferredLights m_DeferredLights;
        bool m_HasDepthPrepass;

        ShaderTagId m_ShaderTagId = new ShaderTagId("UniversalGBuffer");
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render GBuffer");

        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;

        public GBufferPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference, DeferredLights deferredLights)
        {
            base.renderPassEvent = evt;
            m_DeferredLights = deferredLights;
            m_HasDepthPrepass = false;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            if (stencilState.enabled)
            {
                m_RenderStateBlock.stencilReference = stencilReference;
                m_RenderStateBlock.mask = RenderStateMask.Stencil;
                m_RenderStateBlock.stencilState = stencilState;
            }
        }

        public void Setup(ref RenderingData renderingData, RenderTargetHandle depthTexture, RenderTargetHandle[] colorAttachments, bool hasDepthPrepass)
        {
            m_DepthBufferAttachment = depthTexture;
            m_ColorAttachments = colorAttachments;
            m_HasDepthPrepass = hasDepthPrepass;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Create and declare the render targets used in the pass
            for (int i = 0; i < m_DeferredLights.GBufferSliceCount; ++i)
            {
                // Lighting buffer has already been declared with line ConfigureCameraTarget(m_ActiveCameraColorAttachment.Identifier(), ...) in DeferredRenderer.Setup
                if (i != m_DeferredLights.GBufferLightingIndex)
                {
                    RenderTextureDescriptor gbufferSlice = cameraTextureDescriptor;
                    gbufferSlice.graphicsFormat = m_DeferredLights.GetGBufferFormat(i);
                    cmd.GetTemporaryRT(m_ColorAttachments[i].id, gbufferSlice);
                }
            }

            RenderTargetIdentifier[] colorAttachmentIdentifiers = new RenderTargetIdentifier[m_DeferredLights.GBufferSliceCount];
            for (int i = 0; i < colorAttachmentIdentifiers.Length; ++i)
                colorAttachmentIdentifiers[i] = m_ColorAttachments[i].Identifier();

            ConfigureTarget(colorAttachmentIdentifiers, m_DepthBufferAttachment.Identifier());

            // If depth-prepass exists, do not clear depth here or we will lose it.
            // Lighting buffer is cleared independently regardless of what we ask for here.
            ConfigureClear(m_HasDepthPrepass ? ClearFlag.None : ClearFlag.Depth, Color.black);
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

                gbufferCommands.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);

                context.ExecuteCommandBuffer(gbufferCommands); // send the gbufferCommands to the scriptableRenderContext - this should be done *before* calling scriptableRenderContext.DrawRenderers
                gbufferCommands.Clear();

                DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings/*, ref m_RenderStateBlock*/);
            }
            context.ExecuteCommandBuffer(gbufferCommands);
            CommandBufferPool.Release(gbufferCommands);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            for (int i = 0; i < m_ColorAttachments.Length; ++i)
                if (i != m_DeferredLights.GBufferLightingIndex)
                    cmd.ReleaseTemporaryRT(m_ColorAttachments[i].id);
        }
    }
}
