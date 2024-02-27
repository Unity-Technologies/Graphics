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

        /// <summary>
        /// Used to indicate if the active target of the pass is the back buffer
        /// </summary>
        public bool m_IsActiveTargetBackBuffer; // TODO: Remove this when we remove non-RG path

        public XROcclusionMeshPass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler(nameof(XROcclusionMeshPass));
            renderPassEvent = evt;
            m_PassData = new PassData();
            m_IsActiveTargetBackBuffer = false;
            base.profilingSampler = new ProfilingSampler("XR Occlusion Pass");
        }

		private static void ExecutePass(ScriptableRenderContext context, PassData data)
        {
            var cmd = data.renderingData.commandBuffer;

            if (data.renderingData.cameraData.xr.hasValidOcclusionMesh)
            {
                if (data.isActiveTargetBackBuffer)
                    cmd.SetViewport(data.renderingData.cameraData.xr.GetViewport());

                data.renderingData.cameraData.xr.RenderOcclusionMesh(cmd);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.renderingData = renderingData;
            m_PassData.isActiveTargetBackBuffer = m_IsActiveTargetBackBuffer;
            ExecutePass(context, m_PassData);
        }

        private class PassData
        {
            internal RenderingData renderingData;
            internal TextureHandle cameraColorAttachment;
            internal TextureHandle cameraDepthAttachment;
            internal bool isActiveTargetBackBuffer;
        }

        internal void Render(RenderGraph renderGraph, in TextureHandle cameraColorAttachment, in TextureHandle cameraDepthAttachment, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("XR Occlusion Pass", out var passData, base.profilingSampler))
            {
                passData.renderingData = renderingData;
                passData.cameraColorAttachment = builder.UseColorBuffer(cameraColorAttachment, 0);
                passData.cameraDepthAttachment = builder.UseDepthBuffer(cameraDepthAttachment, DepthAccess.Write);

                passData.isActiveTargetBackBuffer = m_IsActiveTargetBackBuffer;

                //  TODO RENDERGRAPH: culling? force culling off for testing
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
