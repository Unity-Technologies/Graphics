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

        private static void ExecutePass(RasterCommandBuffer cmd, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.xr.hasValidOcclusionMesh)
            {
                renderingData.cameraData.xr.RenderOcclusionMesh(cmd);
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), ref renderingData);
        }

        private class PassData
        {
            internal RenderingData renderingData;
            internal TextureHandle cameraDepthAttachment;
        }

        internal void Render(RenderGraph renderGraph, in TextureHandle cameraDepthAttachment, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("XR Occlusion Pass", out var passData, base.profilingSampler))
            {
                passData.renderingData = renderingData;
                passData.cameraDepthAttachment = builder.UseTextureFragmentDepth(cameraDepthAttachment, IBaseRenderGraphBuilder.AccessFlags.Write);

                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, ref data.renderingData);
                });

                return;
            }
        }
    }
}

#endif
