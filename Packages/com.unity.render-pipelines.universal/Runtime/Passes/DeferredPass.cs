using System;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Profiling;
using Unity.Collections;
using UnityEngine.Rendering.RenderGraphModule;
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
            profilingSampler = new ProfilingSampler("Render Deferred Lighting");
            base.renderPassEvent = evt;
            m_DeferredLights = deferredLights;
        }

        // ScriptableRenderPass
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
        {
            var lightingAttachment = m_DeferredLights.GbufferAttachments[m_DeferredLights.GBufferLightingIndex];
            var depthAttachment = m_DeferredLights.DepthAttachmentHandle;

            if (m_DeferredLights.UseFramebufferFetch)
            {
                // Disable obsolete warning for internal usage
                #pragma warning disable CS0618
                ConfigureInputAttachments(m_DeferredLights.DeferredInputAttachments, m_DeferredLights.DeferredInputIsTransient);
                #pragma warning restore CS0618
            }

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            // TODO: Cannot currently bind depth texture as read-only!
            ConfigureTarget(lightingAttachment, depthAttachment);
            #pragma warning restore CS0618
        }

        // ScriptableRenderPass
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
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

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                passData.cameraData = cameraData;
                passData.lightData = lightData;
                passData.shadowData = shadowData;

                passData.color = color;
                builder.SetRenderAttachment(color, 0, AccessFlags.Write);
                passData.depth = depth;
                builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                passData.deferredLights = m_DeferredLights;

                if (!m_DeferredLights.UseFramebufferFetch)
                {
                    for (int i = 0; i < gbuffer.Length; ++i)
                    {
                        if (i != m_DeferredLights.GBufferLightingIndex)
                            builder.UseTexture(gbuffer[i], AccessFlags.Read);
                    }
                }
                else
                {
                    var idx = 0;
                    for (int i = 0; i < gbuffer.Length; ++i)
                    {
                        if (i != m_DeferredLights.GBufferLightingIndex)
                        {
                            builder.SetInputAttachment(gbuffer[i], idx, AccessFlags.Read);
                            idx++;
                        }
                    }
                }

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
