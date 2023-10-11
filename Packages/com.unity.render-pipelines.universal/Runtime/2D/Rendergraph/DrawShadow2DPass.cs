using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawShadow2DPass : ScriptableRenderPass
    {
        static readonly string k_ShadowPass = "Shadow2D LowLevelPass";
        static readonly string k_ShadowVolumetricPass = "Shadow2D Volumetric LowLevelPass";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_ShadowPass);
        private static readonly ProfilingSampler m_ProfilingSamplerVolume = new ProfilingSampler(k_ShadowVolumetricPass);
        private static readonly ProfilingSampler m_ExecuteProfilingSampler = new ProfilingSampler("Draw Shadow");
        private static readonly ProfilingSampler m_ExecuteLightProfilingSampler = new ProfilingSampler("Draw Light");

        TextureHandle[] intermediateTexture = new TextureHandle[1];

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        private static void ExecuteShadowPass(LowLevelCommandBuffer cmd, PassData passData, Light2D light)
        {
            using (new ProfilingScope(cmd, m_ExecuteProfilingSampler))
            {
                cmd.SetRenderTarget(passData.shadowTarget, passData.shadowDepth);
                cmd.ClearRenderTarget(RTClearFlags.All, Color.clear, 1, 0);

                var projectedShadowMaterial = passData.rendererData.GetProjectedShadowMaterial();
                var projectedUnshadowMaterial = passData.rendererData.GetProjectedUnshadowMaterial();

                projectedShadowMaterial.SetTexture(DrawLight2DPass.k_FalloffLookupID, passData.fallOffLookUp);
                projectedUnshadowMaterial.SetTexture(DrawLight2DPass.k_FalloffLookupID, passData.fallOffLookUp);

                ShadowRendering.PrerenderShadows(cmd, passData.rendererData, ref passData.layerBatch, light, 0, light.shadowIntensity);
            }
        }

        private static void ExecuteLightPass(LowLevelCommandBuffer cmd, PassData passData, ref LayerBatch layerBatch, Light2D light, bool clear)
        {
            using (new ProfilingScope(cmd, m_ExecuteLightProfilingSampler))
            {
                for (int i = 0; i < passData.lightTextures.Length; ++i)
                    passData.lightTexturesRT[i] = passData.lightTextures[i];

                if (!passData.isVolumetric)
                    cmd.SetRenderTarget(passData.lightTexturesRT, passData.lightDepth);
                else
                    cmd.SetRenderTarget(passData.lightTexturesRT[0], passData.lightDepth);

                cmd.SetGlobalFloat(DrawLight2DPass.k_InverseHDREmulationScaleID, 1.0f / passData.rendererData.hdrEmulationScale);

                var lightBlendStyles = passData.rendererData.lightBlendStyles;

                // Clear light textures
                if (clear)
                {
                    for (var i = 0; i < DrawLight2DPass.clearColors.Length; i++)
                    {
                        if ((layerBatch.lightStats.blendStylesUsed & (uint)(1 << i)) == 0)
                            continue;

                        Light2DManager.GetGlobalColor(layerBatch.startLayerID, i, out DrawLight2DPass.clearColors[i]);
                    }

                    cmd.ClearRenderTarget(RTClearFlags.Color, DrawLight2DPass.clearColors, 1, 0);
                }

                for (var blendStyleIndex = 0; blendStyleIndex < lightBlendStyles.Length; blendStyleIndex++)
                {
                    var blendStyleMask = (uint)(1 << blendStyleIndex);
                    var blendStyleUsed = (layerBatch.lightStats.blendStylesUsed & blendStyleMask) > 0;

                    if (!blendStyleUsed)
                        continue;
                
                    // Check if light is valid
                    if (light == null ||
                        light.lightType == Light2D.LightType.Global ||
                        light.blendStyleIndex != blendStyleIndex)
                        continue;

                    // Check if light is volumetric
                    if (passData.isVolumetric &&
                        (light.volumeIntensity <= 0.0f ||
                        !light.volumetricEnabled ||
                        layerBatch.endLayerValue != light.GetTopMostLitLayer()))
                        continue;

                    var blendOpName = lightBlendStyles[blendStyleIndex].name;
                    cmd.BeginSample(blendOpName);

                    if (!passData.isVolumetric)
                        RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleUsed);

                    var lightMaterial = passData.rendererData.GetLightMaterial(light, passData.isVolumetric);
                    var lightMesh = light.lightMesh;

                    // For Batching.
                    var index = light.batchSlotIndex;
                    var slotIndex = RendererLighting.lightBatch.SlotIndex(index);
                    bool canBatch = RendererLighting.lightBatch.CanBatch(light, lightMaterial, index, out int lightHash);

                    //bool breakBatch = !canBatch;
                    //if (breakBatch && LightBatch.isBatchingSupported)
                    //    RendererLighting.lightBatch.Flush(cmd);

                    // Set material properties
                    lightMaterial.SetTexture(DrawLight2DPass.k_LightLookupID, passData.lightLookUp);
                    lightMaterial.SetTexture(DrawLight2DPass.k_FalloffLookupID, passData.fallOffLookUp);

                    if (passData.layerBatch.lightStats.useNormalMap)
                        lightMaterial.SetTexture(DrawLight2DPass.k_NormalMapID, passData.normalMap);

                    if (passData.layerBatch.lightStats.useShadows)
                        lightMaterial.SetTexture(DrawLight2DPass.k_ShadowMapID, passData.shadowTarget);

                    if (!passData.isVolumetric || (passData.isVolumetric && light.volumetricEnabled))
                        RendererLighting.SetCookieShaderProperties(light, lightMaterial);

                    // Set shader global properties
                    RendererLighting.SetPerLightShaderGlobals(cmd, light, slotIndex, passData.isVolumetric, true, LightBatch.isBatchingSupported);

                    if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled || light.lightType == Light2D.LightType.Point)
                        RendererLighting.SetPerPointLightShaderGlobals(cmd, light, slotIndex, LightBatch.isBatchingSupported);

                    if (LightBatch.isBatchingSupported)
                    {
                        //RendererLighting.lightBatch.AddBatch(light, lightMaterial, light.GetMatrix(), lightMesh, 0, lightHash, index);
                        //RendererLighting.lightBatch.Flush(cmd);
                    }
                    else
                    {
                        cmd.DrawMesh(lightMesh, light.GetMatrix(), lightMaterial);
                    }

                    RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, false);
                    cmd.EndSample(blendOpName);
                }
            }
        }

        class PassData
        {
            internal LayerBatch layerBatch;
            internal Renderer2DData rendererData;
            internal bool isVolumetric;

            internal TextureHandle shadowTarget;
            internal TextureHandle shadowDepth;
            internal TextureHandle normalMap;
            internal TextureHandle fallOffLookUp;
            internal TextureHandle lightLookUp;
            internal TextureHandle lightDepth;
            internal TextureHandle[] lightTextures;
            internal RenderTargetIdentifier[] lightTexturesRT;
        }

        public void Render(RenderGraph graph, Renderer2DData rendererData, ref LayerBatch layerBatch, ContextContainer frameData, bool isVolumetric = false)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();

            if (!layerBatch.lightStats.useShadows ||
                isVolumetric && !layerBatch.lightStats.useVolumetricShadowLights)
                return;

            var shadowTexture = resourceData.shadowsTexture;
            var depthTexture = resourceData.shadowsDepth;

            using (var builder = graph.AddLowLevelPass<PassData>(!isVolumetric ? k_ShadowPass : k_ShadowVolumetricPass, out var passData, !isVolumetric ? m_ProfilingSampler : m_ProfilingSamplerVolume))
            {
                if (passData.lightTexturesRT == null)
                    passData.lightTexturesRT = new RenderTargetIdentifier[RendererLighting.k_ShapeLightTextureIDs.Length];

                passData.layerBatch = layerBatch;
                passData.rendererData = rendererData;
                passData.isVolumetric = isVolumetric;
                passData.shadowTarget = shadowTexture;
                passData.shadowDepth = depthTexture;
                passData.lightDepth = !isVolumetric ? resourceData.intermediateDepth : resourceData.activeDepthTexture;
                passData.normalMap = layerBatch.lightStats.useNormalMap ? resourceData.normalsTexture : TextureHandle.nullHandle;
                passData.fallOffLookUp = graph.ImportTexture(DrawLight2DPass.m_FallOffRTHandle);
                passData.lightLookUp = graph.ImportTexture(DrawLight2DPass.m_LightLookupRTHandle);

                if (!isVolumetric)
                    passData.lightTextures = resourceData.lightTextures;
                else
                {
                    intermediateTexture[0] = resourceData.activeColorTexture;
                    passData.lightTextures = intermediateTexture;
                }

                for (int i = 0; i < passData.lightTextures.Length; ++i)
                    builder.UseTexture(passData.lightTextures[i], IBaseRenderGraphBuilder.AccessFlags.Write);

                if (layerBatch.lightStats.useNormalMap)
                    builder.UseTexture(resourceData.normalsTexture);

                builder.UseTexture(shadowTexture, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTexture(depthTexture, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTexture(passData.lightDepth, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTexture(passData.fallOffLookUp);
                builder.UseTexture(passData.lightLookUp);

                foreach (var light in layerBatch.shadowLights)
                {
                    if (!light.m_CookieSpriteTextureHandle.IsValid())
                        continue;

                    if (!isVolumetric || (isVolumetric && light.volumetricEnabled))
                        builder.UseTexture(light.m_CookieSpriteTextureHandle);
                }

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, LowLevelGraphContext context) =>
                {
                    for (int i = 0; i < data.layerBatch.shadowLights.Count; ++i)
                    {
                        var light = data.layerBatch.shadowLights[i];
                        ExecuteShadowPass(context.cmd, data, light);
                        ExecuteLightPass(context.cmd, data, ref data.layerBatch, light, i == 0 && !data.isVolumetric);
                    }
                });
            }
        }
    }
}
