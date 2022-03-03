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
        public XROcclusionMeshPass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler(nameof(XROcclusionMeshPass));
            renderPassEvent = evt;
        }

        private static void ExecutePass(ScriptableRenderContext context, PassData passData)
        {
            var renderingData = passData.renderingData;
            var cmd = renderingData.commandBuffer;

            if (renderingData.cameraData.xr.hasValidOcclusionMesh)
            {
                renderingData.cameraData.xr.RenderOcclusionMesh(cmd);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            PassData passData = new PassData();
            passData.renderingData = renderingData;
            ExecutePass(context, passData);
        }

        public class PassData
        {
            public RenderingData renderingData;
            public TextureHandle cameraDepthAttachment;
        }

        public void Render(in TextureHandle cameraDepthAttachment, ref RenderingData renderingData)
        {
            RenderGraph graph = renderingData.renderGraph;

            using (var builder = graph.AddRenderPass<PassData>("XR Occlusion Pass", out var passData, new ProfilingSampler("XR Occlusion Pass")))
            {
                passData.renderingData = renderingData;
                passData.cameraDepthAttachment = builder.UseDepthBuffer(cameraDepthAttachment, DepthAccess.Write);

                //  TODO RENDERGRAPH: culling? force culluing off for testing
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    ExecutePass(context.renderContext, data);
                });

                return;
            }
        }
    }
}

#endif
