using System;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawLight2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Light2DPass");
        private static readonly ProfilingSampler m_ProfilingSamplerVolume = new ProfilingSampler("LightVolume2DPass");
        private static readonly ProfilingSampler m_ExecuteProfilingSampler = new ProfilingSampler("Draw Lights");
        private static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");
        private static readonly int k_CookieTexID = Shader.PropertyToID("_CookieTex");
        TextureHandle[] intermediateTexture = new TextureHandle[1];

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        private static void Execute(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData, ref LayerBatch layerBatch)
        {
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, m_ExecuteProfilingSampler))
            {
                cmd.SetGlobalFloat(k_InverseHDREmulationScaleID, 1.0f / passData.rendererData.hdrEmulationScale);

                var lightBlendStyles = passData.rendererData.lightBlendStyles;

                for (var blendStyleIndex = 0; blendStyleIndex < lightBlendStyles.Length; blendStyleIndex++)
                {
                    var blendStyleMask = (uint)(1 << blendStyleIndex);
                    var blendStyleUsed = (layerBatch.lightStats.blendStylesUsed & blendStyleMask) > 0;

                    if (!blendStyleUsed)
                        continue;

                    var blendOpName = lightBlendStyles[blendStyleIndex].name;
                    cmd.BeginSample(blendOpName);

                    if (!passData.isVolumetric)
                        RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleUsed);

                    var visibleLights = passData.rendererData.lightCullResult.visibleLights;
                    foreach (var light in visibleLights)
                    {
                        // Check if light is valid
                        if (light == null ||
                            light.lightType == Light2D.LightType.Global ||
                            light.blendStyleIndex != blendStyleIndex ||
                            !light.IsLitLayer(layerBatch.startLayerID))
                            continue;

                        // Check if light is volumetric
                        if (passData.isVolumetric &&
                            (light.volumeIntensity <= 0.0f ||
                            !light.volumetricEnabled ||
                            layerBatch.endLayerValue != light.GetTopMostLitLayer()))
                            continue;

                        var lightMaterial = passData.rendererData.GetLightMaterial(light, passData.isVolumetric);
                        var lightMesh = light.lightMesh;

                        // For Batching.
                        var index = light.batchSlotIndex;
                        var slotIndex = RendererLighting.lightBatch.SlotIndex(index);
                        bool canBatch = RendererLighting.lightBatch.CanBatch(light, lightMaterial, index, out int lightHash);

                        RendererLighting.SetPerLightShaderGlobals(cmd, light, slotIndex, passData.isVolumetric, false, LightBatch.isBatchingSupported);

                        if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled || light.lightType == Light2D.LightType.Point)
                            RendererLighting.SetPerPointLightShaderGlobals(passData.rendererData, cmd, light, slotIndex, LightBatch.isBatchingSupported);

                        RendererLighting.SetCookieShaderGlobals(cmd, light);

                        if (LightBatch.isBatchingSupported)
                        {
                            RendererLighting.lightBatch.AddBatch(light, lightMaterial, light.GetMatrix(), lightMesh, 0, lightHash, index);
                            RendererLighting.lightBatch.Flush(cmd);
                        }
                        else
                        {
                            cmd.DrawMesh(lightMesh, light.GetMatrix(), lightMaterial);
                        }

                    }

                    RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, false);
                    cmd.EndSample(blendOpName);
                }
            }
        }

        class PassData
        {
            internal RenderingData renderingData;
            internal LayerBatch layerBatch;
            internal Renderer2DData rendererData;
            internal TextureHandle normalTexture;
            internal bool isVolumetric;
        }

        public void Render(RenderGraph graph, ref RenderingData renderingData, ref Renderer2DData rendererData, ref LayerBatch layerBatch, in TextureHandle lightTexture, in TextureHandle normalTexture, in TextureHandle depthTexture, bool isVolumetric = false)
        {
            intermediateTexture[0] = lightTexture;
            Render(graph, ref renderingData, ref rendererData, ref layerBatch, intermediateTexture, normalTexture, depthTexture, isVolumetric);
        }

        public void Render(RenderGraph graph, ref RenderingData renderingData, ref Renderer2DData rendererData, ref LayerBatch layerBatch, in TextureHandle[] lightTextures, in TextureHandle normalTexture, in TextureHandle depthTexture, bool isVolumetric = false)
        {
            if (!layerBatch.lightStats.useLights ||
                isVolumetric && !layerBatch.lightStats.useVolumetricLights)
                return;

            using (var builder = graph.AddRenderPass<PassData>("Light 2D Pass", out var passData, !isVolumetric ? m_ProfilingSampler : m_ProfilingSamplerVolume))
            {
                for (var i = 0; i < lightTextures.Length; i++)
                {
                    builder.UseColorBuffer(lightTextures[i], i);
                }

                builder.UseDepthBuffer(depthTexture, DepthAccess.Write);

                if (layerBatch.lightStats.useNormalMap)
                {
                    builder.ReadTexture(normalTexture);
                    passData.normalTexture = normalTexture;
                }

                passData.renderingData = renderingData;
                passData.layerBatch = layerBatch;
                passData.rendererData = rendererData;
                passData.isVolumetric = isVolumetric;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    if (data.layerBatch.lightStats.useNormalMap)
                        context.cmd.SetGlobalTexture("_NormalMap", data.normalTexture);

                    Execute(context.renderContext, data, ref data.renderingData, ref data.layerBatch);
                });
            }
        }
    }
}
