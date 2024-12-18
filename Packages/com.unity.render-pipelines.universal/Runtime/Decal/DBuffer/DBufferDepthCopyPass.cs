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

            bool usesDeferredLighting = universalRenderer.usesDeferredLighting;
            bool useDepthPriming = universalRenderer.useDepthPriming;
            bool isMsaa = cameraData.cameraTargetDescriptor.msaaSamples > 1;

            // We must create a temporary depth buffer for dbuffer rendering if the existing one isn't compatible.
            // The deferred path always has compatible depth
            // The forward path only has compatible depth when depth priming is enabled without MSAA
            bool hasCompatibleDepth = usesDeferredLighting || (useDepthPriming && !isMsaa);
            if (!hasCompatibleDepth)
            {
                var depthDesc = cameraData.cameraTargetDescriptor;
                depthDesc.graphicsFormat = GraphicsFormat.None; //Depth only rendering
                depthDesc.depthStencilFormat = cameraData.cameraTargetDescriptor.depthStencilFormat;
                depthDesc.msaaSamples = 1;
                resourceData.dBufferDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, DBufferRenderPass.s_DBufferDepthName, true);

                // Copy the current depth data into the new attachment
                Render(renderGraph, resourceData.dBufferDepth, resourceData.cameraDepthTexture, resourceData, cameraData, false, "Copy DBuffer Depth");
            }
        }
    }
}
