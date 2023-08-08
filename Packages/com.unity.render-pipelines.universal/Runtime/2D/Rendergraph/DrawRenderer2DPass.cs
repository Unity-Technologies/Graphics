using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawRenderer2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Renderer2DPass");
        private static readonly ProfilingSampler m_ExecuteProfilingSampler = new ProfilingSampler("Draw Renderers");
        private static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");
        private static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");

        private static readonly List<ShaderTagId> k_ShaderTags =
            new List<ShaderTagId>() {k_LegacyPassName, k_CombinedRenderingPassName};

        private static readonly int k_HDREmulationScaleID = Shader.PropertyToID("_HDREmulationScale");
        private static readonly int k_UseSceneLightingID = Shader.PropertyToID("_UseSceneLighting");
        private static readonly int k_RendererColorID = Shader.PropertyToID("_RendererColor");

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        private static void Execute(RasterGraphContext context, PassData passData)
        {
            var cmd = context.cmd;
            using (new ProfilingScope(cmd, m_ExecuteProfilingSampler))
            {
                var blendStylesCount = passData.lightBlendStyles.Length;

                cmd.SetGlobalFloat(k_HDREmulationScaleID, passData.hdrEmulationScale);
                cmd.SetGlobalFloat(k_UseSceneLightingID, passData.useSceneLighting ? 1.0f : 0.0f);
                cmd.SetGlobalColor(k_RendererColorID, Color.white);
                RendererLighting.SetLightShaderGlobals(ref passData.lightBlendStyles, cmd);

                if (passData.layerUseLights)
                {
                    for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                    {
                        cmd.SetGlobalTexture(RendererLighting.k_ShapeLightTextureIDs[blendStyleIndex], passData.lightTextures[blendStyleIndex]);

                        var blendStyleMask = (uint)(1 << blendStyleIndex);
                        var blendStyleUsed = (passData.layerBlendStylesUsed & blendStyleMask) > 0;
                        RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleUsed);
                    }
                }
                else
                {
                    if (passData.isSceneLit)
                    {
                        for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                        {
                            cmd.SetGlobalTexture(RendererLighting.k_ShapeLightTextureIDs[blendStyleIndex], context.defaultResources.blackTexture);
                            RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleIndex == 0);
                        }
                    }
                }

                // Draw all renderers in layer batch
                cmd.DrawRendererList(passData.rendererList);

                RendererLighting.DisableAllKeywords(cmd);
            }
        }

        class PassData
        {
            internal bool useSceneLighting;
            internal Light2DBlendStyle[] lightBlendStyles;
            internal float hdrEmulationScale;
            internal bool isSceneLit;
            internal bool layerUseLights;
            internal uint layerBlendStylesUsed;
            internal TextureHandle[] lightTextures;
            internal RendererListHandle rendererList;
        }

        public void Render(RenderGraph graph, ref RenderingData renderingData, Renderer2DData rendererData, ref LayerBatch layerBatch, ref FilteringSettings filterSettings, in TextureHandle cameraColorAttachment, in TextureHandle cameraDepthAttachment, in TextureHandle[] lightTextures)
        {
            using (var builder = graph.AddRasterRenderPass<PassData>("Renderer 2D Pass", out var passData, m_ProfilingSampler))
            {
                passData.useSceneLighting = true;

#if UNITY_EDITOR
                if (renderingData.cameraData.isSceneViewCamera)
                    passData.useSceneLighting = UnityEditor.SceneView.currentDrawingSceneView.sceneLighting;

                if (renderingData.cameraData.camera.cameraType == CameraType.Preview)
                    passData.useSceneLighting = false;
#endif

                passData.lightBlendStyles = rendererData.lightBlendStyles;
                passData.hdrEmulationScale = rendererData.hdrEmulationScale;
                passData.isSceneLit = rendererData.lightCullResult.IsSceneLit();
                passData.layerUseLights = layerBatch.lightStats.useLights;
                passData.layerBlendStylesUsed = layerBatch.lightStats.blendStylesUsed;

                var drawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
                var sortSettings = drawSettings.sortingSettings;
                RendererLighting.GetTransparencySortingMode(rendererData, renderingData.cameraData.camera, ref sortSettings);
                drawSettings.sortingSettings = sortSettings;

                var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
                passData.rendererList = graph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                if (layerBatch.lightStats.useLights)
                {
                    passData.lightTextures = lightTextures;
                    for (var i = 0; i < lightTextures.Length; i++)
                    {
                        builder.UseTexture(lightTextures[i]);
                    }
                }

                builder.UseTextureFragment(cameraColorAttachment, 0);
                builder.UseTextureFragmentDepth(cameraDepthAttachment, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Execute(context, data);
                });
            }
        }
    }
}
