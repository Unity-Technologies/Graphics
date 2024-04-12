using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;
using CommonResourceData = UnityEngine.Rendering.Universal.UniversalResourceData;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawRenderer2DPass : ScriptableRenderPass
    {
        static readonly string k_RenderPass = "Renderer2D Pass";
        static readonly string k_SetLightGlobalPass = "SetLightGlobals Pass";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_RenderPass);
        private static readonly ProfilingSampler m_SetLightGlobalProfilingSampler = new ProfilingSampler(k_SetLightGlobalPass);
        private static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");
        private static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");

#if UNITY_EDITOR
        private static readonly int k_DefaultWhiteTextureID = Shader.PropertyToID("_DefaultWhiteTex");
#endif

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
                    for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                    {
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

            // Preset global light textures for first batch
            if (batchIndex == 0)
            {
                using (var builder = graph.AddRasterRenderPass<SetGlobalPassData>(k_SetLightGlobalPass, out var passData, m_SetLightGlobalProfilingSampler))
                {
                    if (layerBatch.lightStats.useAnyLights)
                    {
                        passData.lightTextures = universal2DResourceData.lightTextures[batchIndex];
                        for (var i = 0; i < passData.lightTextures.Length; i++)
                            builder.UseTexture(passData.lightTextures[i]);
                    }

                    SetGlobalLightTextures(graph, builder, passData.lightTextures, cameraData, ref layerBatch, rendererData);

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
                passData.layerUseLights = layerBatch.lightStats.useAnyLights;

#if UNITY_EDITOR
                passData.isLitView = true;

                // Early out for prefabs
                if (cameraData.isSceneViewCamera && !UnityEditor.SceneView.currentDrawingSceneView.sceneLighting)
                    passData.isLitView = false;

                // Early out for preview camera
                if (cameraData.cameraType == CameraType.Preview)
                    passData.isLitView = false;
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
                    SetGlobalLightTextures(graph, builder, universal2DResourceData.lightTextures[nextBatch], cameraData, ref layerBatches[nextBatch], rendererData);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Execute(context, data);
                });
            }
        }

        void SetGlobalLightTextures(RenderGraph graph, IRasterRenderGraphBuilder builder, TextureHandle[] lightTextures, UniversalCameraData cameraData, ref LayerBatch layerBatch, Renderer2DData rendererData)
        {
#if UNITY_EDITOR
            bool isLitView = true;

            // Early out for prefabs
            if (cameraData.isSceneViewCamera && !UnityEditor.SceneView.currentDrawingSceneView.sceneLighting)
                isLitView = false;

            // Early out for preview camera
            if (cameraData.cameraType == CameraType.Preview)
                isLitView = false;

            builder.SetGlobalTextureAfterPass(graph.defaultResources.whiteTexture, k_DefaultWhiteTextureID);

            if (isLitView)
#endif
            {
                if (layerBatch.lightStats.useAnyLights)
                {
                    for (var blendStyleIndex = 0; blendStyleIndex < lightTextures.Length; blendStyleIndex++)
                    {
                        builder.SetGlobalTextureAfterPass(lightTextures[blendStyleIndex], Shader.PropertyToID(RendererLighting.k_ShapeLightTextureIDs[blendStyleIndex]));
                    }
                }
                else if (rendererData.lightCullResult.IsSceneLit())
                {
                    builder.SetGlobalTextureAfterPass(graph.defaultResources.blackTexture, Shader.PropertyToID(RendererLighting.k_ShapeLightTextureIDs[0]));
                }
            }
        }
    }
}
