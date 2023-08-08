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

        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

            TextureHandle cameraDepthTexture = frameResources.GetTexture(UniversalResource.CameraDepthTexture);


            TextureHandle src, dest;
            if (renderer.renderingModeActual == RenderingMode.Deferred)
            {
                src = renderer.activeDepthTexture;
                dest = cameraDepthTexture;
            }
            else
            {
                var depthDesc = renderingData.cameraData.cameraTargetDescriptor;
                depthDesc.graphicsFormat = GraphicsFormat.None; //Depth only rendering
                depthDesc.depthStencilFormat = renderingData.cameraData.cameraTargetDescriptor.depthStencilFormat;
                depthDesc.msaaSamples = 1;
                var depthTarget = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, DBufferRenderPass.s_DBufferDepthName, true);
                frameResources.SetTexture(UniversalResource.DBufferDepth, depthTarget);

                src = cameraDepthTexture;
                dest = renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1 ? depthTarget : renderer.activeDepthTexture;
            }

            //TODO: bindAsCameraDepth should be investigated as without it DBufferDepth will not be bound correctly, though it should
            Render(renderGraph, dest, src, ref renderingData, renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1);
        }
    }
}
