using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.U2D.Profiler;

namespace UnityEngine.Rendering.Universal
{
    internal class CopyCameraSortingLayerPass : ScriptableRenderPass
    {
        internal static readonly string k_CameraSortingLayerTexture = "_CameraSortingLayerTexture";
        internal static readonly int k_CameraSortingLayerTextureId = Shader.PropertyToID(k_CameraSortingLayerTexture);
        Material m_BlitMaterial;

        public CopyCameraSortingLayerPass(Material blitMaterial)
        {
            m_BlitMaterial = blitMaterial;
        }

        public static void ConfigureDescriptor(Downsampling downsamplingMethod, ref RenderTextureDescriptor descriptor, out FilterMode filterMode)
        {
            descriptor.msaaSamples = 1;
            descriptor.depthStencilFormat = GraphicsFormat.None;
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

        private static void Execute(RasterCommandBuffer cmd, RTHandle source, Material blitMaterial)
        {
            using (new ProfilingScope(cmd, ProfilerMarkers.s_ProfilingSamplerCopy))
            {
                Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                Blitter.BlitTexture(cmd, source, viewportScale, blitMaterial, source.rt.filterMode == FilterMode.Bilinear ? 1 : 0);
            }
        }

        class PassData
        {
            internal TextureHandle source;
            internal Material blitMaterial;
        }

        public void Render(RenderGraph graph, ContextContainer frameData)
        {
            UniversalResourceData commonResourceData = frameData.Get<UniversalResourceData>();
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();

            using (var builder = graph.AddRasterRenderPass<PassData>(ProfilerMarkers.s_CopyCameraSortingLayerPass, out var passData, ProfilerMarkers.s_ProfilingSamplerCopyCameraSortingLayerPass))
            {
                passData.source = commonResourceData.activeColorTexture;
                passData.blitMaterial = m_BlitMaterial;

                builder.SetRenderAttachment(universal2DResourceData.cameraSortingLayerTexture, 0);
                builder.UseTexture(passData.source);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    Execute(context.cmd, data.source, data.blitMaterial);
                });
            }
        }
    }
}
