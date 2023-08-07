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

        private static void Execute(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData, ref Renderer2DData rendererData)
        {
            var isLitView = true;

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
                isLitView = UnityEditor.SceneView.currentDrawingSceneView.sceneLighting;

            if (renderingData.cameraData.camera.cameraType == CameraType.Preview)
                isLitView = false;
#endif

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, m_ExecuteProfilingSampler))
            {
                var blendStylesCount = rendererData.lightBlendStyles.Length;

                cmd.SetGlobalFloat(k_HDREmulationScaleID, rendererData.hdrEmulationScale);
                cmd.SetGlobalFloat(k_UseSceneLightingID, isLitView ? 1.0f : 0.0f);
                cmd.SetGlobalColor(k_RendererColorID, Color.white);
                RendererLighting.SetLightShaderGlobals(rendererData, cmd);

                if (passData.layerBatch.lightStats.useLights)
                {
                    for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                    {
                        cmd.SetGlobalTexture(RendererLighting.k_ShapeLightTextureIDs[blendStyleIndex], passData.lightTextures[blendStyleIndex]);

                        var blendStyleMask = (uint)(1 << blendStyleIndex);
                        var blendStyleUsed = (passData.layerBatch.lightStats.blendStylesUsed & blendStyleMask) > 0;
                        RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleUsed);
                    }
                }
                else
                {
                    if (rendererData.lightCullResult.IsSceneLit())
                    {
                        for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                        {
                            cmd.SetGlobalTexture(RendererLighting.k_ShapeLightTextureIDs[blendStyleIndex], Texture2D.blackTexture);
                            RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleIndex == 0);
                        }
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Draw all renderers in layer batch
                var param = new RendererListParams(renderingData.cullResults, passData.drawSettings, passData.filterSettings);
                var rl = context.CreateRendererList(ref param);
                cmd.DrawRendererList(rl);

                RendererLighting.DisableAllKeywords(cmd);
            }
        }

        class PassData
        {
            internal FilteringSettings filterSettings;
            internal DrawingSettings drawSettings;
            internal RenderingData renderingData;
            internal Renderer2DData renderer2DData;
            internal LayerBatch layerBatch;
            internal TextureHandle[] lightTextures;
        }

        public void Render(RenderGraph graph, ref RenderingData renderingData, Renderer2DData rendererData, ref LayerBatch layerBatch, ref FilteringSettings filterSettings, in TextureHandle cameraColorAttachment, in TextureHandle cameraDepthAttachment, in TextureHandle[] lightTextures)
        {
            using (var builder = graph.AddRenderPass<PassData>("Renderer 2D Pass", out var passData, m_ProfilingSampler))
            {
                passData.filterSettings = filterSettings;
                passData.drawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
                passData.renderingData = renderingData;
                passData.renderer2DData = rendererData;
                passData.layerBatch = layerBatch;

                if (layerBatch.lightStats.useLights)
                {
                    passData.lightTextures = lightTextures;
                    for (var i = 0; i < lightTextures.Length; i++)
                    {
                        passData.lightTextures[i] = builder.ReadTexture(lightTextures[i]);
                    }
                }

                builder.UseColorBuffer(cameraColorAttachment, 0);
                builder.UseDepthBuffer(cameraDepthAttachment, DepthAccess.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    Execute(context.renderContext, data, ref data.renderingData, ref data.renderer2DData);
                });
            }
        }
    }
}
