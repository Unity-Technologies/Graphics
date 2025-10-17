using System;
using UnityEngine.Rendering.RenderGraphModule;
using CommonResourceData = UnityEngine.Rendering.Universal.UniversalResourceData;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawNormal2DPass : ScriptableRenderPass
    {
        static readonly string k_NormalPass = "Normal2D Pass";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_NormalPass);
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");

        private class PassData
        {
            internal RendererListHandle rendererList;
        }

        private static void Execute(RasterCommandBuffer cmd, PassData passData)
        {
            cmd.DrawRendererList(passData.rendererList);
        }

        public void Render(RenderGraph graph, ContextContainer frameData, int batchIndex)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            Renderer2DData rendererData = frameData.Get<Universal2DRenderingData>().renderingData;
            var layerBatch = frameData.Get<Universal2DRenderingData>().layerBatches[batchIndex];

            if (!layerBatch.useNormals)
                return;

            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            var passName = k_NormalPass;
            LayerDebug.FormatPassName(layerBatch, ref passName);

            using (var builder = graph.AddRasterRenderPass<PassData>(passName, out var passData, LayerDebug.GetProfilingSampler(passName, m_ProfilingSampler)))
            {
                LayerUtility.GetFilterSettings(rendererData, layerBatch, out var filterSettings);

                var drawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
                var sortSettings = drawSettings.sortingSettings;
                RendererLighting.GetTransparencySortingMode(rendererData, cameraData.camera, ref sortSettings);
                drawSettings.sortingSettings = sortSettings;

                builder.AllowPassCulling(false);

                builder.SetRenderAttachment(universal2DResourceData.normalsTexture[batchIndex], 0);

                // Depth needed for sprite mask stencil or z test for 3d meshes
                if (rendererData.useDepthStencilBuffer)
                {
                    var depth = universal2DResourceData.normalsDepth.IsValid() ? universal2DResourceData.normalsDepth : commonResourceData.activeDepthTexture;
                    builder.SetRenderAttachmentDepth(depth);
                }

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
