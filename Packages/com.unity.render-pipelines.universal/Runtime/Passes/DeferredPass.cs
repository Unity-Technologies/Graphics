using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Profiling;
using Unity.Collections;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// cleanup code
// listMinDepth and maxDepth should be stored in a different uniform block?
// Point lights stored as vec4
// RelLightIndices should be stored in ushort instead of uint.
// TODO use Unity.Mathematics
// TODO Check if there is a bitarray structure (with dynamic size) available in Unity

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.
    internal class DeferredPass : ScriptableRenderPass
    {
        DeferredLights m_DeferredLights;

        public DeferredPass(RenderPassEvent evt, DeferredLights deferredLights)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DeferredPass));
            base.renderPassEvent = evt;
            m_DeferredLights = deferredLights;
        }

        // ScriptableRenderPass
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
        {
            var lightingAttachment = m_DeferredLights.GbufferAttachments[m_DeferredLights.GBufferLightingIndex];
            var depthAttachment = m_DeferredLights.DepthAttachmentHandle;
            if (m_DeferredLights.UseFramebufferFetch)
                ConfigureInputAttachments(m_DeferredLights.DeferredInputAttachments, m_DeferredLights.DeferredInputIsTransient);

            // TODO: Cannot currently bind depth texture as read-only!
            ConfigureTarget(lightingAttachment, depthAttachment);
        }

        // ScriptableRenderPass
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            m_DeferredLights.ExecuteDeferredPass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), cameraData, lightData, shadowData);
        }

        private class PassData
        {
            internal UniversalCameraData cameraData;
            internal UniversalLightData lightData;
            internal UniversalShadowData shadowData;

            internal TextureHandle color;
            internal TextureHandle depth;
            internal TextureHandle[] gbuffer;
            internal DeferredLights deferredLights;
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle color, TextureHandle depth, TextureHandle[] gbuffer)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Deferred Lighting Pass", out var passData, base.profilingSampler))
            {
                passData.cameraData = cameraData;
                passData.lightData = lightData;
                passData.shadowData = shadowData;

                passData.color = builder.UseTextureFragment(color, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.depth = builder.UseTextureFragmentDepth(depth, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.deferredLights = m_DeferredLights;

                if (!m_DeferredLights.UseFramebufferFetch)
                {
                    for (int i = 0; i < gbuffer.Length; ++i)
                    {
                        if (i != m_DeferredLights.GBufferLightingIndex)
                            builder.UseTexture(gbuffer[i], IBaseRenderGraphBuilder.AccessFlags.Read);
                    }
                }
                else
                {
                    var idx = 0;
                    for (int i = 0; i < gbuffer.Length; ++i)
                    {
                        if (i != m_DeferredLights.GBufferLightingIndex)
                        {
                            builder.UseTextureFragmentInput(gbuffer[i], idx, IBaseRenderGraphBuilder.AccessFlags.Read);
                            idx++;
                        }
                    }
                }

                // Without NRP GBuffer textures are set after GBuffer, we only do this here to avoid breaking the pass
                if (renderGraph.NativeRenderPassesEnabled)
                    GBufferPass.SetGlobalGBufferTextures(builder, gbuffer, ref m_DeferredLights);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.deferredLights.ExecuteDeferredPass(context.cmd, data.cameraData, data.lightData, data.shadowData);
                });
            }
        }

        // ScriptableRenderPass
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            m_DeferredLights.OnCameraCleanup(cmd);
        }
    }
}
