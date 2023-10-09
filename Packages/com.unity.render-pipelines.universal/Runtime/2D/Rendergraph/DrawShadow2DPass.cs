using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawShadow2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Shadow2DPass");
        private static readonly ProfilingSampler m_ExecuteProfilingSampler = new ProfilingSampler("Draw Shadows");

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        private static void Execute(RasterCommandBuffer cmd, PassData passData)
        {
            using (new ProfilingScope(cmd, m_ExecuteProfilingSampler))
            {
                var light = passData.layerBatch.shadowLights[passData.shadowIndex];
                ShadowRendering.PrerenderShadows(cmd, passData.rendererData, ref passData.layerBatch, light, 0, light.shadowIntensity);
            }
        }

        class PassData
        {
            internal LayerBatch layerBatch;
            internal Renderer2DData rendererData;
            internal int shadowIndex;
        }

        public void Render(RenderGraph graph, Renderer2DData rendererData, ref LayerBatch layerBatch, FrameResources resources, int shadowIndex)
        {
            var shadowTexture = resources.GetTexture(Renderer2DResource.ShadowsTexture);
            var depthTexture = resources.GetTexture(Renderer2DResource.IntermediateDepth);

            ClearTargets2DPass.Render(graph, shadowTexture, depthTexture, RTClearFlags.All, Color.black);

            using (var builder = graph.AddRasterRenderPass<PassData>("Shadow 2D Pass", out var passData, m_ProfilingSampler))
            {
                builder.UseTextureFragment(shadowTexture, 0);
                builder.UseTextureFragmentDepth(depthTexture, IBaseRenderGraphBuilder.AccessFlags.Write);

                passData.layerBatch = layerBatch;
                passData.rendererData = rendererData;
                passData.shadowIndex = shadowIndex;

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Execute(context.cmd, data);
                });
            }
        }
    }
}
