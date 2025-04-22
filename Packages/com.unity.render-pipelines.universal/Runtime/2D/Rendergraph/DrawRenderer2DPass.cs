using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;
using CommonResourceData = UnityEngine.Rendering.Universal.UniversalResourceData;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawRenderer2DPass : ScriptableRenderPass
    {
        static readonly string k_RenderPass = "Renderer2D Pass";
        static readonly string k_SetLightBlendTexture = "SetLightBlendTextures";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_RenderPass);
        private static readonly ProfilingSampler m_SetLightBlendTextureProfilingSampler = new ProfilingSampler(k_SetLightBlendTexture);
        private static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");
        private static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");

        private static readonly List<ShaderTagId> k_ShaderTags =
            new List<ShaderTagId>() {k_LegacyPassName, k_CombinedRenderingPassName};

        private static readonly int k_HDREmulationScaleID = Shader.PropertyToID("_HDREmulationScale");
        private static readonly int k_RendererColorID = Shader.PropertyToID("_RendererColor");

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        private static void Execute(RasterGraphContext context, PassData passData)
        {
            var cmd = context.cmd;
            var blendStylesCount = passData.blendStyleIndices.Length;

            cmd.SetGlobalFloat(k_HDREmulationScaleID, passData.hdrEmulationScale);
            cmd.SetGlobalColor(k_RendererColorID, Color.white);
            RendererLighting.SetLightShaderGlobals(cmd, passData.lightBlendStyles, passData.blendStyleIndices);

#if UNITY_EDITOR
            if (passData.isLitView)
#endif
            {
                if (passData.layerUseLights)
                {
                    for (var i = 0; i < blendStylesCount; i++)
                    {
                        var blendStyleIndex = passData.blendStyleIndices[i];
                        RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, true);
                    }
                }
                else if (passData.isSceneLit)
                {
                    RendererLighting.EnableBlendStyle(cmd, 0, true);
                }
            }

            // Draw all renderers in layer batch
            cmd.DrawRendererList(passData.rendererList);

            RendererLighting.DisableAllKeywords(cmd);
        }

        class SetGlobalPassData
        {
            internal TextureHandle[] lightTextures;
        }

        class PassData
        {
            internal Light2DBlendStyle[] lightBlendStyles;
            internal int[] blendStyleIndices;
            internal float hdrEmulationScale;
            internal bool isSceneLit;
            internal bool layerUseLights;
            internal TextureHandle[] lightTextures;
            internal RendererListHandle rendererList;

#if UNITY_EDITOR
            internal bool isLitView; // Required for prefab view and preview camera
#endif
        }

        public void Render(RenderGraph graph, ContextContainer frameData, Renderer2DData rendererData, ref LayerBatch[] layerBatches, int batchIndex, ref FilteringSettings filterSettings)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();

            var layerBatch = layerBatches[batchIndex];
            bool isLitView = true;

#if UNITY_EDITOR
            // Early out for prefabs
            if (cameraData.isSceneViewCamera && UnityEditor.SceneView.currentDrawingSceneView != null)
                isLitView = UnityEditor.SceneView.currentDrawingSceneView.sceneLighting;

            // Early out for preview camera
            if (cameraData.cameraType == CameraType.Preview)
                isLitView = false;
#endif

            // Preset global light textures for first batch
            if (batchIndex == 0)
            {
                using (var builder = graph.AddRasterRenderPass<SetGlobalPassData>(k_SetLightBlendTexture, out var passData, m_SetLightBlendTextureProfilingSampler))
                {
                    if (layerBatch.lightStats.useLights)
                    {
                        passData.lightTextures = universal2DResourceData.lightTextures[batchIndex];
                        for (var i = 0; i < passData.lightTextures.Length; i++)
                            builder.UseTexture(passData.lightTextures[i]);
                    }

                    SetGlobalLightTextures(graph, builder, passData.lightTextures, ref layerBatch, rendererData, isLitView);

                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((SetGlobalPassData data, RasterGraphContext context) =>
                    {
                    });
                }
            }

            // Renderer Pass
            using (var builder = graph.AddRasterRenderPass<PassData>(k_RenderPass, out var passData, m_ProfilingSampler))
            {
                passData.lightBlendStyles = rendererData.lightBlendStyles;
                passData.blendStyleIndices = layerBatch.activeBlendStylesIndices;
                passData.hdrEmulationScale = rendererData.hdrEmulationScale;
                passData.isSceneLit = rendererData.lightCullResult.IsSceneLit();
                passData.layerUseLights = layerBatch.lightStats.useLights;
#if UNITY_EDITOR
                passData.isLitView = isLitView;
#endif

                var drawSettings = CreateDrawingSettings(k_ShaderTags, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
                var sortSettings = drawSettings.sortingSettings;
                RendererLighting.GetTransparencySortingMode(rendererData, cameraData.camera, ref sortSettings);
                drawSettings.sortingSettings = sortSettings;

                var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
                passData.rendererList = graph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                if (passData.layerUseLights)
                {
                    passData.lightTextures = universal2DResourceData.lightTextures[batchIndex];
                    for (var i = 0; i < passData.lightTextures.Length; i++)
                        builder.UseTexture(passData.lightTextures[i]);
                }

                builder.SetRenderAttachment(commonResourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(commonResourceData.activeDepthTexture);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.UseAllGlobalTextures(true);

                // Post set global light textures for next renderer pass 
                var nextBatch = batchIndex + 1;
                if (nextBatch < universal2DResourceData.lightTextures.Length)
                    SetGlobalLightTextures(graph, builder, universal2DResourceData.lightTextures[nextBatch], ref layerBatches[nextBatch], rendererData, isLitView);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Execute(context, data);
                });
            }
        }

        void SetGlobalLightTextures(RenderGraph graph, IRasterRenderGraphBuilder builder, TextureHandle[] lightTextures, ref LayerBatch layerBatch, Renderer2DData rendererData, bool isLitView)
        {
            if (isLitView)
            {
                if (layerBatch.lightStats.useLights)
                {
                    for (var i = 0; i < lightTextures.Length; i++)
                    {
                        var blendStyleIndex = layerBatch.activeBlendStylesIndices[i];
                        builder.SetGlobalTextureAfterPass(lightTextures[i], Shader.PropertyToID(RendererLighting.k_ShapeLightTextureIDs[blendStyleIndex]));
                    }
                }
                else if (rendererData.lightCullResult.IsSceneLit())
                {
                    for (var i = 0; i < RendererLighting.k_ShapeLightTextureIDs.Length; i++)
                        builder.SetGlobalTextureAfterPass(graph.defaultResources.blackTexture, Shader.PropertyToID(RendererLighting.k_ShapeLightTextureIDs[i]));
                }
            }
        }
    }
}
