#if ENABLE_VR && ENABLE_XR_MODULE
using System;
using UnityEngine.Rendering.RenderGraphModule;
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

        private static void ExecutePass(RasterCommandBuffer cmd, PassData data)
        {
            if (data.xr.hasValidOcclusionMesh)
            {
                if (data.isActiveTargetBackBuffer)
                    cmd.SetViewport(data.xr.GetViewport());

                data.xr.RenderOcclusionMesh(cmd);
            }
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.xr = renderingData.cameraData.xr;
            m_PassData.isActiveTargetBackBuffer = m_IsActiveTargetBackBuffer;
            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData);
        }

        private class PassData
        {
            internal XRPass xr;
            internal TextureHandle cameraColorAttachment;
            internal TextureHandle cameraDepthAttachment;
            internal bool isActiveTargetBackBuffer;
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle cameraColorAttachment, in TextureHandle cameraDepthAttachment)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("XR Occlusion Pass", out var passData, base.profilingSampler))
            {
                passData.xr = cameraData.xr;
				passData.cameraColorAttachment = cameraColorAttachment;
                builder.SetRenderAttachment(cameraColorAttachment, 0);
                passData.cameraDepthAttachment = cameraDepthAttachment;
                builder.SetRenderAttachmentDepth(cameraDepthAttachment, AccessFlags.Write);

                passData.isActiveTargetBackBuffer = resourceData.isActiveTargetBackBuffer;

                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                if (cameraData.xr.enabled)
                {
                    bool passSupportsFoveation = cameraData.xrUniversal.canFoveateIntermediatePasses || resourceData.isActiveTargetBackBuffer;
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);
                }

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data);
                });

                return;
            }
        }
    }
}

#endif
