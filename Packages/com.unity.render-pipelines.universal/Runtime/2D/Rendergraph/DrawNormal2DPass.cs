using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawNormal2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Normals2DPass");
        private static readonly ProfilingSampler m_ExecuteProfilingSampler = new ProfilingSampler("Draw Normals");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");

        private class PassData
        {
            internal RendererListHandle rendererList;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        private static void Execute(RasterCommandBuffer cmd, PassData passData)
        {
            using (new ProfilingScope(cmd, m_ExecuteProfilingSampler))
            {
                cmd.ClearRenderTarget(RTClearFlags.Color, RendererLighting.k_NormalClearColor, 1, 0);
                cmd.DrawRendererList(passData.rendererList);
            }
        }

        public void Render(RenderGraph graph, ref RenderingData renderingData, ref LayerBatch layerBatch, in TextureHandle normalTexture, in TextureHandle depthTexture)
        {
            if (!layerBatch.lightStats.useNormalMap)
                return;

            using (var builder = graph.AddRasterRenderPass<PassData>("Normals 2D Pass", out var passData, m_ProfilingSampler))
            {
                var filterSettings = new FilteringSettings();
                filterSettings.renderQueueRange = RenderQueueRange.all;
                filterSettings.layerMask = -1;
                filterSettings.renderingLayerMask = 0xFFFFFFFF;
                filterSettings.sortingLayerRange = new SortingLayerRange(layerBatch.layerRange.lowerBound, layerBatch.layerRange.upperBound);
                var drawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

                builder.AllowPassCulling(false);
                builder.UseTextureFragment(normalTexture, 0);
                builder.UseTextureFragmentDepth(depthTexture, IBaseRenderGraphBuilder.AccessFlags.Write);

                var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
                passData.rendererList = graph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Execute(context.cmd, data);
                });
            }
        }
    }
}
