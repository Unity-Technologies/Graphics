using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class UpscalePass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Upscale Pass");
        private static readonly ProfilingSampler m_ExecuteProfilingSampler = new ProfilingSampler("Draw Upscale");
        static Material m_BlitMaterial;

        private RTHandle source;
        private RTHandle destination;

        private class PassData
        {
            internal TextureHandle source;
        }

        public UpscalePass(RenderPassEvent evt, Material blitMaterial)
        {
            renderPassEvent = evt;
            m_BlitMaterial = blitMaterial;
        }

        public void Setup(RTHandle colorTargetHandle, int width, int height, FilterMode mode, ref RenderingData renderingData, out RTHandle upscaleHandle)
        {
            source = colorTargetHandle;

            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.width = width;
            desc.height = height;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref destination, desc, mode, TextureWrapMode.Clamp, name: "_UpscaleTexture");

            upscaleHandle = destination;
        }

        public void Dispose()
        {
            destination?.Release();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            cmd.SetRenderTarget(destination);

            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(cmd), source);
        }

        private static void ExecutePass(RasterCommandBuffer cmd, RTHandle source)
        {
            using (new ProfilingScope(cmd, m_ExecuteProfilingSampler))
            {
                Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                Blitter.BlitTexture(cmd, source, viewportScale, m_BlitMaterial, source.rt.filterMode == FilterMode.Bilinear ? 1 : 0);
            }
        }

        public void Render(RenderGraph graph, ref CameraData cameraData, in TextureHandle cameraColorAttachment, in TextureHandle upscaleHandle)
        {
            cameraData.camera.TryGetComponent<PixelPerfectCamera>(out var ppc);
            if (ppc == null || !ppc.enabled || !ppc.requiresUpscalePass)
                return;

            using (var builder = graph.AddRasterRenderPass<PassData>("Upscale Pass", out var passData, m_ProfilingSampler))
            {
                passData.source = cameraColorAttachment;
                builder.UseTextureFragment(upscaleHandle, 0);
                builder.UseTexture(cameraColorAttachment);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data.source);
                });
            }
        }
    }
}
