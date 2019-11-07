using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Profiling;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
        RenderTargetHandle[] m_ColorAttachments;
        RenderTargetHandle m_DepthBufferAttachment;

        RenderTextureDescriptor[] m_GBufferDescriptors = new RenderTextureDescriptor[DeferredRenderer.GBufferSlicesCount];
        RenderTextureDescriptor m_DepthBufferDescriptor;

        bool m_HasDepthPrepass;

        ShaderTagId m_ShaderTagId = new ShaderTagId("UniversalGBuffer");

        FilteringSettings m_FilteringSettings;

        public GBufferPass(RenderPassEvent evt, RenderQueueRange renderQueueRange)
        {
            base.renderPassEvent = evt;

            const int initialWidth = 1920;
            const int initialHeight = 1080;
            m_GBufferDescriptors[0] = new RenderTextureDescriptor(initialWidth, initialHeight, Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, 0);            // albedo          albedo          albedo          occlusion       (sRGB rendertarget)
            m_GBufferDescriptors[1] = new RenderTextureDescriptor(initialWidth, initialHeight, Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, 0);            // specular        specular        specular        metallic        (sRGB rendertarget)
            m_GBufferDescriptors[2] = new RenderTextureDescriptor(initialWidth, initialHeight, Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 0);           // encoded-normal  encoded-normal  encoded-normal  smoothness
            //m_GBufferDescriptors[3] = new RenderTextureDescriptor(initialWidth, initialHeight, Experimental.Rendering.GraphicsFormat.B10G11R11_UFloatPack32, 0); // GI              GI              GI              [unused]        (lighting buffer)  // <- initialized in DeferredRenderer.cs as DeferredRenderer.m_CameraColorAttachment

            m_HasDepthPrepass = false;

            m_FilteringSettings = new FilteringSettings(renderQueueRange);
        }

        public void Setup(ref RenderingData renderingData, RenderTargetHandle depthTexture, RenderTargetHandle[] colorAttachments, bool hasDepthPrepass)
        {
            for(int gbufferIndex = 0; gbufferIndex < m_GBufferDescriptors.Length ; ++gbufferIndex)
            {
                m_GBufferDescriptors[gbufferIndex].width = renderingData.cameraData.cameraTargetDescriptor.width;
                m_GBufferDescriptors[gbufferIndex].height = renderingData.cameraData.cameraTargetDescriptor.height;
            }

            m_DepthBufferDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            m_DepthBufferDescriptor.colorFormat = RenderTextureFormat.Depth;
            m_DepthBufferDescriptor.depthBufferBits = 32;
            m_DepthBufferDescriptor.msaaSamples = 1;

            m_DepthBufferAttachment = depthTexture;
            m_ColorAttachments = colorAttachments;

            m_HasDepthPrepass = hasDepthPrepass;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
        {
            // Create and declare the render targets used in the pass

            cmd.GetTemporaryRT(m_DepthBufferAttachment.id, m_DepthBufferDescriptor, FilterMode.Point);

            // Only declare GBuffer 0, 1 and 2.
            // GBuffer 3 has already been declared with line ConfigureCameraTarget(m_ActiveCameraColorAttachment.Identifier(), ...) in DeferredRenderer.Setup
            for (int gbufferIndex = 0; gbufferIndex < DeferredRenderer.GBufferSlicesCount; ++gbufferIndex)
                cmd.GetTemporaryRT(m_ColorAttachments[gbufferIndex].id, m_GBufferDescriptors[gbufferIndex]);

            RenderTargetIdentifier[] colorAttachmentIdentifiers = new RenderTargetIdentifier[m_ColorAttachments.Length];
            for (int gbufferIndex = 0; gbufferIndex < m_ColorAttachments.Length; ++gbufferIndex)
                colorAttachmentIdentifiers[gbufferIndex] = m_ColorAttachments[gbufferIndex].Identifier();

            ConfigureTarget(colorAttachmentIdentifiers, m_DepthBufferAttachment.Identifier());

            // If depth-prepass exists, do not clear depth here or we will lose it.
            ConfigureClear(m_HasDepthPrepass ? ClearFlag.None : ClearFlag.Depth, Color.black);
        }

        public override void Execute(ScriptableRenderContext scriptableRenderContext, ref RenderingData renderingData)
        {
            CommandBuffer gbufferCommands = CommandBufferPool.Get("Render GBuffer");
            using (new ProfilingSample(gbufferCommands, "Render GBuffer"))
            {
                gbufferCommands.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);
                // Note: a special case might be required if(renderingData.cameraData.isStereoEnabled) - see reference in ScreenSpaceShadowResolvePass.Execute

                scriptableRenderContext.ExecuteCommandBuffer(gbufferCommands); // send the gbufferCommands to the scriptableRenderContext - this should be done *before* calling scriptableRenderContext.DrawRenderers
                gbufferCommands.Clear();

                DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

                scriptableRenderContext.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }
            scriptableRenderContext.ExecuteCommandBuffer(gbufferCommands);
            CommandBufferPool.Release(gbufferCommands);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            // Release the render targets created during Configure()
            for (int gbufferIndex = 0; gbufferIndex < DeferredRenderer.GBufferSlicesCount; ++gbufferIndex)
                cmd.ReleaseTemporaryRT(m_ColorAttachments[gbufferIndex].id);

            cmd.ReleaseTemporaryRT(m_DepthBufferAttachment.id);
            // Note: a special case might be required if(m_CameraDepthTexture==RenderTargetHandle.CameraTarget) - see reference in DepthOnlyPass.Execute
        }
    }
}
