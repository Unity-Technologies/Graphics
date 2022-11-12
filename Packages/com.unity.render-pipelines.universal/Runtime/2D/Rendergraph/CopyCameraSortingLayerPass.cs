using System;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class CopyCameraSortingLayerPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("CopyCameraSortingLayerPass");
        private static readonly ProfilingSampler m_ExecuteProfilingSampler = new ProfilingSampler("Copy");
        public static readonly string k_CameraSortingLayerTexture = "_CameraSortingLayerTexture";
        static Material m_BlitMaterial;

        public CopyCameraSortingLayerPass(Material blitMaterial)
        {
            m_BlitMaterial = blitMaterial;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        public static void ConfigureDescriptor(Downsampling downsamplingMethod, ref RenderTextureDescriptor descriptor, out FilterMode filterMode)
        {
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            if (downsamplingMethod == Downsampling._2xBilinear)
            {
                descriptor.width /= 2;
                descriptor.height /= 2;
            }
            else if (downsamplingMethod == Downsampling._4xBox || downsamplingMethod == Downsampling._4xBilinear)
            {
                descriptor.width /= 4;
                descriptor.height /= 4;
            }

            filterMode = downsamplingMethod == Downsampling.None || downsamplingMethod == Downsampling._4xBox ? FilterMode.Point : FilterMode.Bilinear;
        }

        private static void Execute(ref RenderingData renderingData, RTHandle source)
        {
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, m_ExecuteProfilingSampler))
            {
                Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                Blitter.BlitTexture(cmd, source, viewportScale, m_BlitMaterial, source.rt.filterMode == FilterMode.Bilinear ? 1 : 0);
            }
        }

        class PassData
        {
            internal RenderingData renderingData;
            internal TextureHandle source;
        }

        public void Render(RenderGraph graph, ref RenderingData renderingData, in TextureHandle cameraColorAttachment, in TextureHandle destination)
        {
            using (var builder = graph.AddRenderPass<PassData>("Copy Camera Sorting Layer Pass", out var passData, m_ProfilingSampler))
            {
                passData.renderingData = renderingData;
                passData.source = cameraColorAttachment;

                builder.UseColorBuffer(destination, 0);
                builder.ReadTexture(passData.source);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    Execute(ref data.renderingData, data.source);
                });
            }

            RenderGraphUtils.SetGlobalTexture(graph, k_CameraSortingLayerTexture, destination, "Set Camera Sorting Layer Texture");
        }
    }
}
