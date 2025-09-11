#if ENABLE_VR && ENABLE_XR_MODULE
using System;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Draw the XR occlusion mesh into the current depth buffer when XR is enabled.
    /// </summary>
    public partial class XROcclusionMeshPass : ScriptableRenderPass
    {
#if URP_COMPATIBILITY_MODE
        /// <summary>
        /// Used to indicate if the active target of the pass is the back buffer
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete + " #from(6000.3)")]
        public bool m_IsActiveTargetBackBuffer; // TODO: Remove this when we remove non-RG path

        PassData m_PassData;
#endif

        public XROcclusionMeshPass(RenderPassEvent evt)
        {
            profilingSampler = new ProfilingSampler("Draw XR Occlusion Mesh");
            renderPassEvent = evt;

#if URP_COMPATIBILITY_MODE
#pragma warning disable CS0618
            m_IsActiveTargetBackBuffer = false;
#pragma warning restore CS0618
            m_PassData = new PassData();
#endif
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData data)
        {
            if (data.xr.hasValidOcclusionMesh)
            {
                if (data.isActiveTargetBackBuffer)
                    cmd.SetViewport(data.xr.GetViewport());

                data.xr.RenderOcclusionMesh(cmd, renderIntoTexture: data.shouldYFlip);
            }
        }

#if URP_COMPATIBILITY_MODE
        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsoleteFrom2023_3)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.xr = renderingData.cameraData.xr;
            m_PassData.isActiveTargetBackBuffer = m_IsActiveTargetBackBuffer;
            m_PassData.shouldYFlip = !m_PassData.isActiveTargetBackBuffer;
            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData);
        }
#endif

        private class PassData
        {
            internal XRPass xr;
            internal bool isActiveTargetBackBuffer;
            internal bool shouldYFlip;
            internal TextureHandle cameraColorAttachment;
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle cameraColorAttachment, in TextureHandle cameraDepthAttachment)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                passData.xr = cameraData.xr;
                passData.cameraColorAttachment = cameraColorAttachment;
                builder.SetRenderAttachment(cameraColorAttachment, 0);
                builder.SetRenderAttachmentDepth(cameraDepthAttachment, AccessFlags.Write);

                passData.isActiveTargetBackBuffer = resourceData.isActiveTargetBackBuffer;

                builder.AllowGlobalStateModification(true);
                if (cameraData.xr.enabled)
                {
                    bool passSupportsFoveation = cameraData.xrUniversal.canFoveateIntermediatePasses || resourceData.isActiveTargetBackBuffer;
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                }

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    passData.shouldYFlip = RenderingUtils.IsHandleYFlipped(context, in data.cameraColorAttachment);
                    ExecutePass(context.cmd, data);
                });

                return;
            }
        }
    }
}

#endif
