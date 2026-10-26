#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Draws a near-plane depth occluder during the periphery (outer) QuadView pass over the inset rect
    /// to early-Z reject opaque geometry in the inset region that will not be seen after compositing the inner QuadView pass.
    /// Must declare the same render attachments as the following pass (GBufferPass or DrawObjectsPass) to ensure native subpass merging.
    /// </summary>
    internal class XRQuadViewPrepass : ScriptableRenderPass
    {
        readonly Material m_OccluderMaterial;
        const int k_FeatherFallbackPx = 4;

        public XRQuadViewPrepass(Material occluderMaterial)
        {
            base.profilingSampler = new ProfilingSampler("XR QuadView Inset Occluder");
            base.renderPassEvent = RenderPassEvent.BeforeRenderingGbuffer;
            m_OccluderMaterial = occluderMaterial;
        }

        private class PassData
        {
            internal Material occluderMaterial;
            internal Vector4 insetNdc;          // bottom-left-origin fraction (x=left, y=bottom, z=w, w=h)
            internal Vector2 insetFeatherNdc;   // per-axis inward feather as a fraction per axis
        }

        static void ExecutePass(RasterCommandBuffer cmd, PassData data)
        {
            Vector2Int fullView = RTHandles.rtHandleProperties.currentViewportSize;

            float insetX = data.insetNdc.x * fullView.x;
            float insetY = data.insetNdc.y * fullView.y;
            float insetW = data.insetNdc.z * fullView.x;
            float insetH = data.insetNdc.w * fullView.y;

            // Shrink by the safety feather to avoid rejecting pixels the compositor still blends at the inset boundary
            float featherX = data.insetFeatherNdc.x > 0f ? data.insetFeatherNdc.x * fullView.x : k_FeatherFallbackPx;
            float featherY = data.insetFeatherNdc.y > 0f ? data.insetFeatherNdc.y * fullView.y : k_FeatherFallbackPx;
            float viewX = insetX + featherX;
            float viewY = insetY + featherY;
            float viewW = insetW - 2f * featherX;
            float viewH = insetH - 2f * featherY;
            if (viewW <= 0f || viewH <= 0f)
                return;

            cmd.SetViewport(new Rect(viewX, viewY, viewW, viewH));
            cmd.DrawProcedural(Matrix4x4.identity, data.occluderMaterial, 0, MeshTopology.Triangles, 3, 1);
            cmd.SetViewport(new Rect(0f, 0f, fullView.x, fullView.y));
        }

        // Deferred doesn't have an explicit target handle, the GBuffer attachments are derived from resourceData
        internal void RenderDeferred(RenderGraph renderGraph, ContextContainer frameData)
            => Render(renderGraph, frameData, default, default);

        // In Forward, renderingLayersTarget is only supplied when DrawObjectsWithRenderingLayersPass runs
        internal void RenderForward(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle colorTarget, in TextureHandle renderingLayersTarget = default)
            => Render(renderGraph, frameData, colorTarget, renderingLayersTarget);

        internal void Render(RenderGraph renderGraph, ContextContainer frameData,
            in TextureHandle colorTarget, in TextureHandle renderingLayersTarget = default)
        {
            var xrLayout = XRSystem.currentLayout;
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (m_OccluderMaterial == null || xrLayout == null ||
                cameraData.xr.xrLayoutType != XRLayoutType.TwoPassQuadViews || cameraData.xr.isQuadViewInnerPass)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler);
            builder.AllowGlobalStateModification(true); // Needed for SetViewport calls

            // Declare the same attachments as the following pass so both land into the same Vulkan subpass.
            // Without this, vkCmdNextSubpass is issued inbetween and Adreno LRZ state is lost which loses the early-Z optimization.
            if (colorTarget.IsValid())
            {
                // Forward: match DrawObjectsPass's SubPassDescriptor (slot 0, optional slot 1 for rendering layers)
                builder.SetRenderAttachment(colorTarget, 0, AccessFlags.Write);
                if (renderingLayersTarget.IsValid())
                    builder.SetRenderAttachment(renderingLayersTarget, 1, AccessFlags.Write);
            }
            else
            {
                // Deferred: match GBufferPass's SubPassDescriptor
                // ColorMask 0 in the shader means no actual color writes, the declaration is for subpass matching only
                var gbuffer = resourceData.gBuffer;
                for (int i = 0; i < gbuffer.Length; i++)
                {
                    if (gbuffer[i].IsValid())
                        builder.SetRenderAttachment(gbuffer[i], i, AccessFlags.Write);
                }
            }
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

            // Match GBufferPass/DrawObjectsPass foveated rasterization state to avoid FRStateMismatch breaking the native pass merge
            bool passSupportsFoveation = cameraData.xrUniversal.canFoveateIntermediatePasses || resourceData.isActiveTargetBackBuffer;
            builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);

            passData.occluderMaterial = m_OccluderMaterial;
            passData.insetNdc = xrLayout.quadView.peripheryInsetViewport;
            passData.insetFeatherNdc = xrLayout.quadView.peripheryInsetFeather;

            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                ExecutePass(context.cmd, data));
        }
    }
}
#endif
