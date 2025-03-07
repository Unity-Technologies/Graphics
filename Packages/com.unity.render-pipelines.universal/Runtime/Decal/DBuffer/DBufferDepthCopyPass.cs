using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Used by DBuffer to copy depth into different texture.
    /// In future this should be replaced by proper depth copy logic, as current CopyDepthPass do not implement RecordRenderGraph.
    /// </summary>
    internal class DBufferCopyDepthPass : CopyDepthPass
    {
        public DBufferCopyDepthPass(RenderPassEvent evt, Shader copyDepthShader, bool shouldClear = false, bool copyToDepth = false, bool copyResolvedDepth = false)
            : base(evt, copyDepthShader, shouldClear, copyToDepth, copyResolvedDepth)
        {
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            var universalRenderer = cameraData.renderer as UniversalRenderer;

            Debug.Assert(renderPassEvent < RenderPassEvent.BeforeRenderingGbuffer, "DBufferCopyDepthPass assumes the pipeline does a prepass and it is injected before opaque/gbuffer rendering.");

            var currentCameraDepthInfo = renderGraph.GetRenderTargetInfo(resourceData.activeDepthTexture);

            //Check if the active depth texture is already written to so we can use it
            bool useDepthPriming = universalRenderer.useDepthPriming;

            //Check if the active depth texture is compabible with the other render targets that we'll use for the dbuffer pass
            bool isMsaa = currentCameraDepthInfo.msaaSamples > 1;

            // We must create a temporary depth buffer for dbuffer rendering if the cameraDepth isn't compatible or was written to.
            bool hasCompatibleDepth = useDepthPriming && !isMsaa;
            if (!hasCompatibleDepth)
            {
                //Here we assume that when using depth priming, there is no prepass to the cameraDepthTexture but a copy, so that the texture is a color format. 
                var source = (useDepthPriming) ? resourceData.cameraDepth : resourceData.cameraDepthTexture;

                Debug.Assert(source.IsValid(), "DBufferCopyDepthPass needs a valid cameraDepth or cameraDepth texture to copy from. You might be using depth priming, with MSAA and direct to backbuffer rendering, which is not supported.");
                Debug.Assert(GraphicsFormatUtility.IsDepthFormat(source.GetDescriptor(renderGraph).format), "DBufferCopyDepthPass assumes source has a depth format.");

                var depthDesc = cameraData.cameraTargetDescriptor;
                depthDesc.graphicsFormat = GraphicsFormat.None; //Depth only rendering
                depthDesc.depthStencilFormat = cameraData.cameraTargetDescriptor.depthStencilFormat;
                depthDesc.msaaSamples = 1;
                resourceData.dBufferDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, DBufferRenderPass.s_DBufferDepthName, true);

                //The code shared with Compatibility Mode has some logic based on the deferred path. Here, we need it to always copy to depth, ignoring any other setting.
                CopyToDepth = true;

                // Copy the depth texture (filled by a prepass) into the new attachment so it can be used for depth testing
                Render(renderGraph, resourceData.dBufferDepth, source, resourceData, cameraData, false, "Copy DBuffer Depth");
            }
        }
    }
}
