using System;
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
            internal RenderingData renderingData;
            internal FilteringSettings filterSettings;
            internal DrawingSettings drawSettings;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        private static void Execute(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, m_ExecuteProfilingSampler))
            {
                cmd.ClearRenderTarget(RTClearFlags.Color, RendererLighting.k_NormalClearColor, 1, 0);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var param = new RendererListParams(renderingData.cullResults, passData.drawSettings, passData.filterSettings);
                var rl = context.CreateRendererList(ref param);
                cmd.DrawRendererList(rl);
            }
        }

        public void Render(RenderGraph graph, ref RenderingData renderingData, ref LayerBatch layerBatch, in TextureHandle normalTexture, in TextureHandle depthTexture)
        {
            if (!layerBatch.lightStats.useNormalMap)
                return;

            using (var builder = graph.AddRenderPass<PassData>("Normals 2D Pass", out var passData, m_ProfilingSampler))
            {

                var filterSettings = new FilteringSettings();
                filterSettings.renderQueueRange = RenderQueueRange.all;
                filterSettings.layerMask = -1;
                filterSettings.renderingLayerMask = 0xFFFFFFFF;
                filterSettings.sortingLayerRange = new SortingLayerRange(layerBatch.layerRange.lowerBound, layerBatch.layerRange.upperBound);
                var drawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

                builder.AllowPassCulling(false);
                builder.UseColorBuffer(in normalTexture, 0);
                builder.UseDepthBuffer(depthTexture, DepthAccess.Write);

                passData.filterSettings = filterSettings;
                passData.drawSettings = drawSettings;
                passData.renderingData = renderingData;

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    Execute(context.renderContext, data, ref data.renderingData);
                });
            }
        }
    }
}
