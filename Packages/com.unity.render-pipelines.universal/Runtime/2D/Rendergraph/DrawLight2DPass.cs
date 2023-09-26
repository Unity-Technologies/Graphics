using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawLight2DPass : ScriptableRenderPass
    {
        static readonly string k_LightPass = "Light2D Pass";
        static readonly string k_LightVolumetricPass = "Light2D Volumetric Pass";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_LightPass);
        private static readonly ProfilingSampler m_ProfilingSamplerVolume = new ProfilingSampler(k_LightVolumetricPass);
        internal static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");
        internal static readonly int k_NormalMapID = Shader.PropertyToID("_NormalMap");
        internal static readonly int k_ShadowMapID = Shader.PropertyToID("_ShadowTex");
        internal static readonly int k_LightLookupID = Shader.PropertyToID("_LightLookup");
        internal static readonly int k_FalloffLookupID = Shader.PropertyToID("_FalloffLookup");
        internal static Color[] clearColors = new Color[RendererLighting.k_ShapeLightTextureIDs.Length];

        TextureHandle[] intermediateTexture = new TextureHandle[1];
        internal static RTHandle m_FallOffRTHandle = null;
        internal static RTHandle m_LightLookupRTHandle = null;
        private int lightLookupInstanceID;
        private int fallOffLookupInstanceID;

        public void Setup(RenderGraph renderGraph, ref Renderer2DData rendererData)
        {
            // Reallocate external texture if needed
            var fallOffLookupTexture = Light2DLookupTexture.GetFallOffLookupTexture();
            if (fallOffLookupInstanceID != fallOffLookupTexture.GetInstanceID())
            {
                m_FallOffRTHandle = RTHandles.Alloc(fallOffLookupTexture);
                fallOffLookupInstanceID = fallOffLookupTexture.GetInstanceID();
            }

            var lightLookupTexture = Light2DLookupTexture.GetLightLookupTexture();
            if (lightLookupInstanceID != lightLookupTexture.GetInstanceID())
            {
                m_LightLookupRTHandle = RTHandles.Alloc(lightLookupTexture);
                lightLookupInstanceID = lightLookupTexture.GetInstanceID();
            }

            foreach (var light in rendererData.lightCullResult.visibleLights)
            {
                if (light.useCookieSprite && light.m_CookieSpriteTexture != null)
                    light.m_CookieSpriteTextureHandle = renderGraph.ImportTexture(light.m_CookieSpriteTexture);
            }
        }

        public void Dispose()
        {
            m_FallOffRTHandle?.Release();
            m_LightLookupRTHandle?.Release();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        private static void Execute(RasterCommandBuffer cmd, PassData passData, ref LayerBatch layerBatch)
        {
            // Set Global Textures
            cmd.SetGlobalTexture(k_FalloffLookupID, passData.fallOffLookUp);
            cmd.SetGlobalTexture(k_LightLookupID, passData.lightLookUp);

            if (passData.normalMap.IsValid())
                cmd.SetGlobalTexture(k_NormalMapID, passData.normalMap);

            if (passData.shadowMap.IsValid())
                cmd.SetGlobalTexture(k_ShadowMapID, passData.shadowMap);

            cmd.SetGlobalFloat(k_InverseHDREmulationScaleID, 1.0f / passData.rendererData.hdrEmulationScale);

            var lightBlendStyles = passData.rendererData.lightBlendStyles;

            // Clear light textures
            if (passData.clear)
            {
                for (var i = 0; i < clearColors.Length; i++)
                {
                    if ((layerBatch.lightStats.blendStylesUsed & (uint)(1 << i)) == 0)
                        continue;

                    Light2DManager.GetGlobalColor(layerBatch.startLayerID, i, out clearColors[i]);
                }

                cmd.ClearRenderTarget(RTClearFlags.Color, clearColors, 1, 0);
            }

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

                var lights = passData.layerBatch.lights;

                for (int i = 0; i < lights.Count; ++i)
                {
                    var light = lights[i];

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

                    // Set shader global properties
                    RendererLighting.SetPerLightShaderGlobals(cmd, light, slotIndex, passData.isVolumetric, false, LightBatch.isBatchingSupported);

                    if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled || light.lightType == Light2D.LightType.Point)
                        RendererLighting.SetPerPointLightShaderGlobals(cmd, light, slotIndex, LightBatch.isBatchingSupported);

                    if (!passData.isVolumetric || (passData.isVolumetric && light.volumetricEnabled))
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

        class PassData
        {
            internal LayerBatch layerBatch;
            internal Renderer2DData rendererData;
            internal bool isVolumetric;
            internal bool clear;

            internal TextureHandle normalMap;
            internal TextureHandle shadowMap;
            internal TextureHandle fallOffLookUp;
            internal TextureHandle lightLookUp;
        }

        public void Render(RenderGraph graph, Renderer2DData rendererData, ref LayerBatch layerBatch, ContextContainer frameData, TextureHandle lightTexture, TextureHandle depthTexture, bool isVolumetric = false)
        {
            intermediateTexture[0] = lightTexture;
            Render(graph, rendererData, ref layerBatch, frameData, intermediateTexture, depthTexture, isVolumetric);
        }

        public void Render(RenderGraph graph, Renderer2DData rendererData, ref LayerBatch layerBatch, ContextContainer frameData, TextureHandle[] lightTextures, TextureHandle depthTexture, bool isVolumetric = false, bool clear = false)
        {
            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();

            if (!layerBatch.lightStats.useLights ||
                isVolumetric && !layerBatch.lightStats.useVolumetricLights)
                return;

            using (var builder = graph.AddRasterRenderPass<PassData>(!isVolumetric ? k_LightPass : k_LightVolumetricPass, out var passData, !isVolumetric ? m_ProfilingSampler : m_ProfilingSamplerVolume))
            {
                for (var i = 0; i < lightTextures.Length; i++)
                    builder.UseTextureFragment(lightTextures[i], i);

                builder.UseTextureFragmentDepth(depthTexture);

                if (layerBatch.lightStats.useNormalMap)
                    builder.UseTexture(resourceData.normalsTexture);

                if (layerBatch.lightStats.useShadows)
                    builder.UseTexture(resourceData.shadowsTexture);

                foreach (var light in layerBatch.lights)
                {
                    if (!light.m_CookieSpriteTextureHandle.IsValid())
                        continue;

                    if (!isVolumetric || (isVolumetric && light.volumetricEnabled))
                        builder.UseTexture(light.m_CookieSpriteTextureHandle);
                }

                passData.layerBatch = layerBatch;
                passData.rendererData = rendererData;
                passData.isVolumetric = isVolumetric;
                passData.clear = clear;
                passData.normalMap = layerBatch.lightStats.useNormalMap ? resourceData.normalsTexture : TextureHandle.nullHandle;
                passData.shadowMap = layerBatch.lightStats.useShadows ? resourceData.shadowsTexture : TextureHandle.nullHandle;
                passData.fallOffLookUp = graph.ImportTexture(m_FallOffRTHandle);
                passData.lightLookUp = graph.ImportTexture(m_LightLookupRTHandle);

                builder.UseTexture(passData.fallOffLookUp);
                builder.UseTexture(passData.lightLookUp);

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
