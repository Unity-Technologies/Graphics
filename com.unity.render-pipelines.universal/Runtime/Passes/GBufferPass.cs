using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Profiling;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
        public const int GBufferSlicesCount = 4;

        // attachments are like "binding points", internally they identify the texture shader properties declared with the same names
        public RenderTargetHandle[] m_GBufferAttachments = new RenderTargetHandle[GBufferSlicesCount];
        public RenderTargetHandle m_DepthBufferAttachment;

        RenderTextureDescriptor[] m_GBufferDescriptors = new RenderTextureDescriptor[GBufferSlicesCount];
        RenderTextureDescriptor m_DepthBufferDescriptor;

        ShaderTagId m_ShaderTagId = new ShaderTagId("UniversalGBuffer");

        FilteringSettings m_FilteringSettings;

        public GBufferPass(RenderPassEvent evt, RenderQueueRange renderQueueRange)
        {
            base.renderPassEvent = evt;

            m_GBufferAttachments[0].Init("_GBuffer0"); // Use these strings to refer to the GBuffers in shaders
            m_GBufferAttachments[1].Init("_GBuffer1");
            m_GBufferAttachments[2].Init("_GBuffer2");
            m_GBufferAttachments[3].Init("_GBuffer3");

            const int initialWidth = 1920;
            const int initialHeight = 1080;
            m_GBufferDescriptors[0] = new RenderTextureDescriptor(initialWidth, initialHeight, Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, 0);  // albedo   albedo   albedo   occlusion
            m_GBufferDescriptors[1] = new RenderTextureDescriptor(initialWidth, initialHeight, Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, 0);  // specular specular specular smoothness
            m_GBufferDescriptors[2] = new RenderTextureDescriptor(initialWidth, initialHeight, Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 0); // normal   normal   normal   alpha
            m_GBufferDescriptors[3] = new RenderTextureDescriptor(initialWidth, initialHeight, Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 0); // emission emission emission metallic

            m_FilteringSettings = new FilteringSettings(renderQueueRange);
        }

        public void Setup(ref RenderingData renderingData, RenderTargetHandle depthTexture)
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
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
        {
            // Create and declare the render targets used in the pass
            cmd.GetTemporaryRT(m_DepthBufferAttachment.id, m_DepthBufferDescriptor, FilterMode.Point);
            RenderTargetIdentifier[] colorAttachments = new RenderTargetIdentifier[GBufferSlicesCount];
            for (int gbufferIndex = 0; gbufferIndex < GBufferSlicesCount; ++gbufferIndex)
            {
                cmd.GetTemporaryRT(m_GBufferAttachments[gbufferIndex].id, m_GBufferDescriptors[gbufferIndex]);
                colorAttachments[gbufferIndex] = m_GBufferAttachments[gbufferIndex].Identifier();
            }
            ConfigureTarget(colorAttachments, m_DepthBufferAttachment.Identifier());

            // TODO: if depth-prepass is enabled, do not clear depth here!
            ConfigureClear(ClearFlag.Depth, Color.black);
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
            for (int gbufferIndex = 0; gbufferIndex < m_GBufferAttachments.Length; ++gbufferIndex)
                cmd.ReleaseTemporaryRT(m_GBufferAttachments[gbufferIndex].id);

            cmd.ReleaseTemporaryRT(m_DepthBufferAttachment.id);
            // Note: a special case might be required if(m_CameraDepthTexture==RenderTargetHandle.CameraTarget) - see reference in DepthOnlyPass.Execute
        }
    }
}
