using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class UpscalePass : ScriptableRenderPass
    {
        static readonly string k_UpscalePass = "Upscale2D Pass";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_UpscalePass);
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

        public void Setup(RTHandle colorTargetHandle, int width, int height, FilterMode mode, RenderTextureDescriptor cameraTargetDescriptor, out RTHandle upscaleHandle)
        {
            source = colorTargetHandle;

            RenderTextureDescriptor desc = cameraTargetDescriptor;
            desc.width = width;
            desc.height = height;
            desc.depthStencilFormat = GraphicsFormat.None;
            RenderingUtils.ReAllocateHandleIfNeeded(ref destination, desc, mode, TextureWrapMode.Clamp, name: "_UpscaleTexture");

            upscaleHandle = destination;
        }

        public void Dispose()
        {
            destination?.Release();
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
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

        public void Render(RenderGraph graph, Camera camera, in TextureHandle cameraColorAttachment, in TextureHandle upscaleHandle)
        {
            camera.TryGetComponent<PixelPerfectCamera>(out var ppc);
            if (ppc == null || !ppc.enabled || !ppc.requiresUpscalePass)
                return;

            using (var builder = graph.AddRasterRenderPass<PassData>(k_UpscalePass, out var passData, m_ProfilingSampler))
            {
                passData.source = cameraColorAttachment;
                builder.SetRenderAttachment(upscaleHandle, 0);
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
