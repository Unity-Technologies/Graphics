using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class CopyCameraSortingLayerPass : ScriptableRenderPass
    {
        static readonly string k_CopyCameraSortingLayerPass = "CopyCameraSortingLayer Pass";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_CopyCameraSortingLayerPass);
        private static readonly ProfilingSampler m_ExecuteProfilingSampler = new ProfilingSampler("Copy");
        internal static readonly string k_CameraSortingLayerTexture = "_CameraSortingLayerTexture";
        private static readonly int k_CameraSortingLayerTextureId = Shader.PropertyToID(k_CameraSortingLayerTexture);
        static Material m_BlitMaterial;

        public CopyCameraSortingLayerPass(Material blitMaterial)
        {
            m_BlitMaterial = blitMaterial;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
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

        private static void Execute(RasterCommandBuffer cmd, RTHandle source)
        {
            using (new ProfilingScope(cmd, m_ExecuteProfilingSampler))
            {
                Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                Blitter.BlitTexture(cmd, source, viewportScale, m_BlitMaterial, source.rt.filterMode == FilterMode.Bilinear ? 1 : 0);
            }
        }

        class PassData
        {
            internal TextureHandle source;
        }

        public void Render(RenderGraph graph, in TextureHandle cameraColorAttachment, in TextureHandle destination)
        {
            using (var builder = graph.AddRasterRenderPass<PassData>(k_CopyCameraSortingLayerPass, out var passData, m_ProfilingSampler))
            {
                passData.source = cameraColorAttachment;

                builder.SetRenderAttachment(destination, 0);
                builder.UseTexture(passData.source);
                builder.AllowPassCulling(false);

                builder.SetGlobalTextureAfterPass(destination, k_CameraSortingLayerTextureId);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Execute(context.cmd, data.source);
                });
            }
        }
    }
}
