using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;
using CommonResourceData = UnityEngine.Rendering.Universal.UniversalResourceData;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawLight2DPass : ScriptableRenderPass
    {
        static readonly string k_LightPass = "Light2D Pass";
        static readonly string k_LightLowLevelPass = "Light2D LowLevelPass";
        static readonly string k_LightVolumetricPass = "Light2D Volumetric Pass";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_LightPass);
        internal static readonly ProfilingSampler m_ProfilingSamplerLowLevel = new ProfilingSampler(k_LightLowLevelPass);
        private static readonly ProfilingSampler m_ProfilingSamplerVolume = new ProfilingSampler(k_LightVolumetricPass);
        internal static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");
        internal static readonly string k_NormalMapID = "_NormalMap";
        internal static readonly string k_ShadowMapID = "_ShadowTex";

        TextureHandle[] intermediateTexture = new TextureHandle[1];

        internal static MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        public void Setup(RenderGraph renderGraph, ref Renderer2DData rendererData)
        {
            foreach (var light in rendererData.lightCullResult.visibleLights)
            {
                if (light.useCookieSprite && light.m_CookieSpriteTexture != null)
                    light.m_CookieSpriteTextureHandle = renderGraph.ImportTexture(light.m_CookieSpriteTexture);
            }
        }

        static bool TryGetShadowIndex(ref LayerBatch layerBatch, int lightIndex, out int shadowIndex)
        {
            shadowIndex = 0;

            for (int i = 0; i < layerBatch.shadowIndices.Count; ++i)
            {
                if (layerBatch.shadowIndices[i] == lightIndex)
                {
                    shadowIndex = i;
                    return true;
                }
            }

            return false;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        private static void Execute(RasterCommandBuffer cmd, PassData passData, ref LayerBatch layerBatch)
        {
            cmd.SetGlobalFloat(k_InverseHDREmulationScaleID, 1.0f / passData.rendererData.hdrEmulationScale);

            for (var i = 0; i < layerBatch.activeBlendStylesIndices.Length; ++i)
            {
                var blendStyleIndex = layerBatch.activeBlendStylesIndices[i];
                var blendOpName = passData.rendererData.lightBlendStyles[blendStyleIndex].name;
                cmd.BeginSample(blendOpName);

                if (!passData.isVolumetric)
                    RendererLighting.EnableBlendStyle(cmd, i, true);

                var lights = passData.layerBatch.lights;

                for (int j = 0; j < lights.Count; ++j)
                {
                    var light = lights[j];

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

                    var lightMaterial = passData.rendererData.GetLightMaterial(light, passData.isVolumetric);
                    var lightMesh = light.lightMesh;

                    // For Batching.
                    var index = light.batchSlotIndex;
                    var slotIndex = RendererLighting.lightBatch.SlotIndex(index);
                    bool canBatch = RendererLighting.lightBatch.CanBatch(light, lightMaterial, index, out int lightHash);

                    bool breakBatch = !canBatch;
                    if (breakBatch && LightBatch.isBatchingSupported)
                        RendererLighting.lightBatch.Flush(cmd);

                    if (passData.layerBatch.lightStats.useNormalMap)
                        s_PropertyBlock.SetTexture(k_NormalMapID, passData.normalMap);

                    var useShadows = passData.layerBatch.lightStats.useShadows && layerBatch.shadowIndices.Contains(j);
                    if (useShadows && TryGetShadowIndex(ref layerBatch, j, out var shadowIndex))
                        s_PropertyBlock.SetTexture(k_ShadowMapID, passData.shadowTextures[shadowIndex]);

                    if (!passData.isVolumetric || (passData.isVolumetric && light.volumetricEnabled))
                        RendererLighting.SetCookieShaderProperties(light, s_PropertyBlock);

                    // Set shader global properties
                    RendererLighting.SetPerLightShaderGlobals(cmd, light, slotIndex, passData.isVolumetric, useShadows, LightBatch.isBatchingSupported);

                    if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled || light.lightType == Light2D.LightType.Point)
                        RendererLighting.SetPerPointLightShaderGlobals(cmd, light, slotIndex, LightBatch.isBatchingSupported);

                    if (LightBatch.isBatchingSupported)
                    {
                        RendererLighting.lightBatch.AddBatch(light, lightMaterial, light.GetMatrix(), lightMesh, 0, lightHash, index);
                        RendererLighting.lightBatch.Flush(cmd);
                    }
                    else
                    {
                        cmd.DrawMesh(lightMesh, light.GetMatrix(), lightMaterial, 0, 0, s_PropertyBlock);
                    }
                }

                RendererLighting.EnableBlendStyle(cmd, i, false);
                cmd.EndSample(blendOpName);
            }
        }

        internal static void ExecuteUnsafe(UnsafeCommandBuffer cmd, PassData passData, ref LayerBatch layerBatch, List<Light2D> lights)
        {
            cmd.SetGlobalFloat(k_InverseHDREmulationScaleID, 1.0f / passData.rendererData.hdrEmulationScale);

            for (var i = 0; i < layerBatch.activeBlendStylesIndices.Length; ++i)
            {
                var blendStyleIndex = layerBatch.activeBlendStylesIndices[i];
                var blendOpName = passData.rendererData.lightBlendStyles[blendStyleIndex].name;
                cmd.BeginSample(blendOpName);

                if (!Renderer2D.supportsMRT && !passData.isVolumetric)
                    cmd.SetRenderTarget(passData.lightTextures[i], passData.depthTexture);

                var indicesIndex = Renderer2D.supportsMRT ? i : 0;
                if (!passData.isVolumetric)
                    RendererLighting.EnableBlendStyle(cmd, indicesIndex, true);

                for (int j = 0; j < lights.Count; ++j)
                {
                    var light = lights[j];

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

                    var lightMaterial = passData.rendererData.GetLightMaterial(light, passData.isVolumetric);
                    var lightMesh = light.lightMesh;

                    // For Batching.
                    var index = light.batchSlotIndex;
                    var slotIndex = RendererLighting.lightBatch.SlotIndex(index);
                    bool canBatch = RendererLighting.lightBatch.CanBatch(light, lightMaterial, index, out int lightHash);

                    //bool breakBatch = !canBatch;
                    //if (breakBatch && LightBatch.isBatchingSupported)
                    //    RendererLighting.lightBatch.Flush(cmd);

                    if (passData.layerBatch.lightStats.useNormalMap)
                        s_PropertyBlock.SetTexture(k_NormalMapID, passData.normalMap);

                    var useShadows = passData.layerBatch.lightStats.useShadows && layerBatch.shadowIndices.Contains(j);
                    if (useShadows && TryGetShadowIndex(ref layerBatch, j, out var shadowIndex))
                        s_PropertyBlock.SetTexture(k_ShadowMapID, passData.shadowTextures[shadowIndex]);

                    if (!passData.isVolumetric || (passData.isVolumetric && light.volumetricEnabled))
                        RendererLighting.SetCookieShaderProperties(light, s_PropertyBlock);

                    // Set shader global properties
                    RendererLighting.SetPerLightShaderGlobals(cmd, light, slotIndex, passData.isVolumetric, useShadows, LightBatch.isBatchingSupported);

                    if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled || light.lightType == Light2D.LightType.Point)
                        RendererLighting.SetPerPointLightShaderGlobals(cmd, light, slotIndex, LightBatch.isBatchingSupported);

                    if (LightBatch.isBatchingSupported)
                    {
                        //RendererLighting.lightBatch.AddBatch(light, lightMaterial, light.GetMatrix(), lightMesh, 0, lightHash, index);
                        //RendererLighting.lightBatch.Flush(cmd);
                    }
                    else
                    {
                        cmd.DrawMesh(lightMesh, light.GetMatrix(), lightMaterial, 0, 0, s_PropertyBlock);
                    }
                }

                RendererLighting.EnableBlendStyle(cmd, indicesIndex, false);
                cmd.EndSample(blendOpName);
            }
        }

        internal class PassData
        {
            internal LayerBatch layerBatch;
            internal Renderer2DData rendererData;
            internal bool isVolumetric;

            internal TextureHandle normalMap;
            internal TextureHandle[] shadowTextures;

            // TODO: Optimize and remove low level pass
            // For low level shadow and light pass
            internal TextureHandle[] lightTextures;
            internal TextureHandle depthTexture;
        }

        public void Render(RenderGraph graph, ContextContainer frameData, Renderer2DData rendererData, ref LayerBatch layerBatch, int batchIndex, bool isVolumetric = false)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();

            if (!layerBatch.lightStats.useLights ||
                isVolumetric && !layerBatch.lightStats.useVolumetricLights)
                return;

            // OpenGL has a bug with MRTs - support single RTs by using low level pass
            if (!isVolumetric && Renderer2D.IsGLDevice())
            {
                using (var builder = graph.AddUnsafePass<PassData>( k_LightLowLevelPass, out var passData, m_ProfilingSamplerLowLevel))
                {
                    intermediateTexture[0] = commonResourceData.activeColorTexture;
                    passData.lightTextures = universal2DResourceData.lightTextures[batchIndex];
                    passData.depthTexture = universal2DResourceData.intermediateDepth;

                    for (var i = 0; i < passData.lightTextures.Length; i++)
                        builder.UseTexture(passData.lightTextures[i], AccessFlags.Write);

                    builder.UseTexture(passData.depthTexture, AccessFlags.Write);

                    if (layerBatch.lightStats.useNormalMap)
                        builder.UseTexture(universal2DResourceData.normalsTexture[batchIndex]);

                    if (layerBatch.lightStats.useShadows)
                    {
                        passData.shadowTextures = universal2DResourceData.shadowTextures[batchIndex];
                        for (var i = 0; i < passData.shadowTextures.Length; i++)
                            builder.UseTexture(passData.shadowTextures[i]);
                    }

                    foreach (var light in layerBatch.lights)
                    {
                        if (light == null || !light.m_CookieSpriteTextureHandle.IsValid())
                            continue;

                        if (!isVolumetric || (isVolumetric && light.volumetricEnabled))
                            builder.UseTexture(light.m_CookieSpriteTextureHandle);
                    }

                    passData.layerBatch = layerBatch;
                    passData.rendererData = rendererData;
                    passData.isVolumetric = isVolumetric;
                    passData.normalMap = layerBatch.lightStats.useNormalMap ? universal2DResourceData.normalsTexture[batchIndex] : TextureHandle.nullHandle;

                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((PassData data, UnsafeGraphContext context) =>
                    {
                        ExecuteUnsafe(context.cmd, data, ref data.layerBatch, data.layerBatch.lights);
                    });
                }
            }
            // Default Raster Pass with MRTs
            else
            {
                using (var builder = graph.AddRasterRenderPass<PassData>(!isVolumetric ? k_LightPass : k_LightVolumetricPass, out var passData, !isVolumetric ? m_ProfilingSampler : m_ProfilingSamplerVolume))
                {
                    intermediateTexture[0] = commonResourceData.activeColorTexture;
                    var lightTextures = !isVolumetric ? universal2DResourceData.lightTextures[batchIndex] : intermediateTexture;
                    var depthTexture = !isVolumetric ? universal2DResourceData.intermediateDepth : commonResourceData.activeDepthTexture;

                    for (var i = 0; i < lightTextures.Length; i++)
                        builder.SetRenderAttachment(lightTextures[i], i);

                    builder.SetRenderAttachmentDepth(depthTexture);

                    if (layerBatch.lightStats.useNormalMap)
                        builder.UseTexture(universal2DResourceData.normalsTexture[batchIndex]);

                    if (layerBatch.lightStats.useShadows)
                    {
                        passData.shadowTextures = universal2DResourceData.shadowTextures[batchIndex];
                        for (var i = 0; i < passData.shadowTextures.Length; i++)
                            builder.UseTexture(passData.shadowTextures[i]);
                    }

                    foreach (var light in layerBatch.lights)
                    {
                        if (light == null || !light.m_CookieSpriteTextureHandle.IsValid())
                            continue;

                        if (!isVolumetric || (isVolumetric && light.volumetricEnabled))
                            builder.UseTexture(light.m_CookieSpriteTextureHandle);
                    }

                    passData.layerBatch = layerBatch;
                    passData.rendererData = rendererData;
                    passData.isVolumetric = isVolumetric;
                    passData.normalMap = layerBatch.lightStats.useNormalMap ? universal2DResourceData.normalsTexture[batchIndex] : TextureHandle.nullHandle;

                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        Execute(context.cmd, data, ref data.layerBatch);
                    });
                }
            }
        }
    }
}
