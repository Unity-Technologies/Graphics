using System;
using UnityEngine.Rendering.RenderGraphModule;
using CommonResourceData = UnityEngine.Rendering.Universal.UniversalResourceData;

namespace UnityEngine.Rendering.Universal
{
    internal class CameraRenderingLayerMaskPass : ScriptableRenderPass
    {
        static readonly string k_RenderingLayerMaskPass = "RenderingLayerMask Pass";
        internal static readonly string k_RenderingLayersTextureName = "_CameraRenderingLayersTexture";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_RenderingLayerMaskPass);
        private static readonly int k_RenderingLayerMaskID = Shader.PropertyToID(k_RenderingLayersTextureName);

        Material m_Material = null;

        internal CameraRenderingLayerMaskPass()
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<Renderer2DResources>(out var resources))
            {
                m_Material = CoreUtils.CreateEngineMaterial(resources.renderingLayerMaskShader);
            }
        }

        internal void Dispose()
        {
            CoreUtils.Destroy(m_Material);
        }

        private class PassData
        {
            internal RendererListHandle rendererList;
        }

        private static void Execute(RasterCommandBuffer cmd, PassData passData)
        {
            cmd.DrawRendererList(passData.rendererList);
        }

        internal void Render(RenderGraph graph, ContextContainer frameData)
        {
            Renderer2DData rendererData = frameData.Get<Universal2DRenderingData>().renderingData;

            if (!rendererData.useRenderingLayers || m_Material == null)
                return;

            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = graph.AddRasterRenderPass<PassData>(k_RenderingLayerMaskPass, out var passData, LayerDebug.GetProfilingSampler(passName, m_ProfilingSampler)))
            {
                // Render all
                var filterSettings = new FilteringSettings(RenderQueueRange.all);
                filterSettings.layerMask = rendererData.layerMask;

                var drawSettings = CreateDrawingSettings(DrawRenderer2DPass.k_ShaderTags, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
                drawSettings.overrideMaterial = m_Material; // render with override RenderingLayerMask.shader
                var sortSettings = drawSettings.sortingSettings;
                RendererLighting.GetTransparencySortingMode(rendererData, cameraData.camera, ref sortSettings);
                drawSettings.sortingSettings = sortSettings;

                builder.AllowPassCulling(false);

                builder.SetRenderAttachment(universal2DResourceData.renderingLayersTexture, 0);

                var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
                passData.rendererList = graph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                builder.SetGlobalTextureAfterPass(universal2DResourceData.renderingLayersTexture, k_RenderingLayerMaskID);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    Execute(context.cmd, data);
                });
            }
        }
    }
}
