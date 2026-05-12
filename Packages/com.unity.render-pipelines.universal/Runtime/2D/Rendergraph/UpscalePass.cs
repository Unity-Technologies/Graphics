using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.U2D.Profiler;

namespace UnityEngine.Rendering.Universal
{
    internal class UpscalePass : ScriptableRenderPass
    {
        Material m_BlitMaterial;

        private class PassData
        {
            internal TextureHandle source;
            internal Material blitMaterial;
        }

        public UpscalePass(RenderPassEvent evt, Material blitMaterial)
        {
            renderPassEvent = evt;
            m_BlitMaterial = blitMaterial;
        }

        private static void ExecutePass(RasterCommandBuffer cmd, RTHandle source, Material blitMaterial)
        {
            using (new ProfilingScope(cmd, ProfilerMarkers.s_ProfilingSamplerDrawUpscale))
            {
                Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                Blitter.BlitTexture(cmd, source, viewportScale, blitMaterial, source.rt.filterMode == FilterMode.Bilinear ? 1 : 0);
            }
        }

        public void Render(RenderGraph graph, Camera camera, in TextureHandle cameraColorAttachment, in TextureHandle upscaleHandle)
        {
            camera.TryGetComponent<PixelPerfectCamera>(out var ppc);
            if (ppc == null || !ppc.enabled || !ppc.requiresUpscalePass)
                return;

            using (var builder = graph.AddRasterRenderPass<PassData>(ProfilerMarkers.s_UpscalePass, out var passData, ProfilerMarkers.s_ProfilingSamplerUpscalePass))
            {
                passData.source = cameraColorAttachment;
                passData.blitMaterial = m_BlitMaterial;

                builder.SetRenderAttachment(upscaleHandle, 0);
                builder.UseTexture(cameraColorAttachment);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data.source, data.blitMaterial);
                });
            }
        }
    }
}
