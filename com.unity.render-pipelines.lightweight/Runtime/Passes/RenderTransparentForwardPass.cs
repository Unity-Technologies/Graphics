using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    /// <summary>
    /// Render all transparent forward objects into the given color and depth target 
    ///
    /// You can use this pass to render objects that have a material and/or shader
    /// with the pass names LightweightForward or SRPDefaultUnlit. The pass only renders
    /// objects in the rendering queue range of Transparent objects.
    /// </summary>
    public class RenderTransparentForwardPass : ScriptableRenderPass
    {
        const string k_RenderTransparentsTag = "Render Transparents";

        FilteringSettings m_TransparentFilterSettings;

        RenderTargetHandle colorAttachmentHandle { get; set; }
        RenderTargetHandle depthAttachmentHandle { get; set; }
        RenderTextureDescriptor descriptor { get; set; }
        PerObjectData rendererConfiguration;

        public RenderTransparentForwardPass()
        {
            RegisterShaderPassName("LightweightForward");
            RegisterShaderPassName("SRPDefaultUnlit");

            m_TransparentFilterSettings = new FilteringSettings(RenderQueueRange.transparent);
        }

        /// <summary>
        /// Configure the pass before execution
        /// </summary>
        /// <param name="baseDescriptor">Current target descriptor</param>
        /// <param name="colorAttachmentHandle">Color attachment to render into</param>
        /// <param name="depthAttachmentHandle">Depth attachment to render into</param>
        /// <param name="configuration">Specific render configuration</param>
        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            RenderTargetHandle depthAttachmentHandle,
            PerObjectData configuration)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            this.depthAttachmentHandle = depthAttachmentHandle;
            descriptor = baseDescriptor;
            rendererConfiguration = configuration;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
            CommandBuffer cmd = CommandBufferPool.Get(k_RenderTransparentsTag);
            using (new ProfilingSample(cmd, k_RenderTransparentsTag))
            {
                RenderBufferLoadAction loadOp = RenderBufferLoadAction.Load;
                RenderBufferStoreAction storeOp = RenderBufferStoreAction.Store;
                SetRenderTarget(cmd, colorAttachmentHandle.Identifier(), loadOp, storeOp,
                    depthAttachmentHandle.Identifier(), loadOp, storeOp, ClearFlag.None, Color.black, descriptor.dimension);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Camera camera = renderingData.cameraData.camera;
                var drawSettings = CreateDrawingSettings(camera, SortingCriteria.CommonTransparent, rendererConfiguration, renderingData.supportsDynamicBatching);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_TransparentFilterSettings);

                // Render objects that did not match any shader pass with error shader
                renderer.RenderObjectsWithError(context, ref renderingData.cullResults, camera, m_TransparentFilterSettings, SortingCriteria.None);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
