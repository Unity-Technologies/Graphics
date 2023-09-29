#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Draw the XR occlusion mesh into the current depth buffer when XR is enabled.
    /// </summary>
    public class XROcclusionMeshPass : ScriptableRenderPass
    {
        PassData m_PassData;

        public XROcclusionMeshPass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler(nameof(XROcclusionMeshPass));
            renderPassEvent = evt;
            m_PassData = new PassData();
            base.profilingSampler = new ProfilingSampler("XR Occlusion Pass");
        }

        private static void ExecutePass(RasterCommandBuffer cmd, XRPass xr)
        {
            if (xr.hasValidOcclusionMesh)
            {
                xr.RenderOcclusionMesh(cmd);
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), renderingData.cameraData.xr);
        }

        private class PassData
        {
            internal XRPass xr;
            internal TextureHandle cameraDepthAttachment;
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle cameraDepthAttachment)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("XR Occlusion Pass", out var passData, base.profilingSampler))
            {
                passData.xr = cameraData.xr;
                passData.cameraDepthAttachment = builder.UseTextureFragmentDepth(cameraDepthAttachment, IBaseRenderGraphBuilder.AccessFlags.Write);

                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data.xr);
                });

                return;
            }
        }
    }
}

#endif
