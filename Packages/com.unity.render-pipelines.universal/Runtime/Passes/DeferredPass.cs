using System;
using UnityEngine.Rendering.RenderGraphModule;

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

        private class PassData
        {
            internal UniversalCameraData cameraData;
            internal UniversalLightData lightData;
            internal UniversalShadowData shadowData;

            internal TextureHandle[] gbuffer;
            internal DeferredLights deferredLights;
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle color, TextureHandle depth, TextureHandle[] gbuffer)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                passData.cameraData = cameraData;
                passData.lightData = lightData;
                passData.shadowData = shadowData;

                builder.SetRenderAttachment(color, 0, AccessFlags.Write);
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
