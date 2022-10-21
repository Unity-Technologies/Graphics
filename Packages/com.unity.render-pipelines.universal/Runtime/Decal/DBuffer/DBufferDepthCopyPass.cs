using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Used by DBuffer to copy depth into different texture.
    /// In future this should be replaced by proper depth copy logic, as current CopyDepthPass do not implement RecordRenderGraph.
    /// </summary>
    internal class DBufferCopyDepthPass : CopyDepthPass
    {
        public DBufferCopyDepthPass(RenderPassEvent evt, Material copyDepthMaterial, bool shouldClear = false, bool copyToDepth = false, bool copyResolvedDepth = false)
            : base(evt, copyDepthMaterial, shouldClear, copyToDepth, copyResolvedDepth)
        {
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

            var depthDesc = renderingData.cameraData.cameraTargetDescriptor;
            depthDesc.graphicsFormat = GraphicsFormat.None; //Depth only rendering
            depthDesc.depthStencilFormat = renderingData.cameraData.cameraTargetDescriptor.depthStencilFormat;
            depthDesc.msaaSamples = 1;
            var depthTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, DBufferRenderPass.s_DBufferDepthName, true);

            if (renderer.renderingModeActual == RenderingMode.Deferred)
                depthTexture = renderer.frameResources.cameraDepthTexture;

            UniversalRenderer.RenderGraphFrameResources frameResources = renderer.frameResources;
            frameResources.dbufferDepth = depthTexture;

            TextureHandle depthTarget = (renderer.renderingModeActual == RenderingMode.Deferred) ? UniversalRenderer.m_ActiveRenderGraphDepth : renderer.frameResources.cameraDepthTexture;

            Render(renderGraph, ref depthTexture, in depthTarget, ref renderingData);
        }
    }
}
