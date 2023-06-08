using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawLight2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Light2DPass");
        private static readonly ProfilingSampler m_ProfilingSamplerVolume = new ProfilingSampler("LightVolume2DPass");
        private static readonly ProfilingSampler m_ExecuteProfilingSampler = new ProfilingSampler("Draw Lights");
        private static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");
        private static readonly int k_NormalMapID = Shader.PropertyToID("_NormalMap");
        private static readonly int k_ShadowMapID = Shader.PropertyToID("_ShadowTex");
        private static readonly int k_LightLookupID = Shader.PropertyToID("_LightLookup");
        private static readonly int k_FalloffLookupID = Shader.PropertyToID("_FalloffLookup");

        TextureHandle[] intermediateTexture = new TextureHandle[1];
        private RTHandle m_FallOffRTHandle = null;
        private RTHandle m_LightLookupRTHandle = null;
        private int lightLookupInstanceID;

        public void Setup(RenderGraph renderGraph, ref Renderer2DData rendererData)
        {
            if (m_FallOffRTHandle == null)
                m_FallOffRTHandle = RTHandles.Alloc(rendererData.fallOffLookup);

            // Reallocate external texture if needed
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

                    bool isShadow = passData.shadowIndex != -1;
                    var lights = isShadow ? passData.layerBatch.shadowLights : passData.layerBatch.lights;

                    for (int i = 0; i < lights.Count; ++i)
                    {
                        var light = lights[i];

                        // Check if light is valid
                        if (light == null ||
                            light.lightType == Light2D.LightType.Global ||
                            light.blendStyleIndex != blendStyleIndex ||
                            (isShadow && passData.shadowIndex != i) )
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
                        RendererLighting.SetPerLightShaderGlobals(cmd, light, slotIndex, passData.isVolumetric, isShadow, LightBatch.isBatchingSupported);

                        if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled || light.lightType == Light2D.LightType.Point)
                            RendererLighting.SetPerPointLightShaderGlobals(passData.rendererData, cmd, light, slotIndex, LightBatch.isBatchingSupported);

                        RendererLighting.SetCookieShaderGlobals(cmd, light);
                        ShadowRendering.SetGlobalShadowProp(cmd);

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
            internal LayerBatch layerBatch;
            internal Renderer2DData rendererData;
            internal int shadowIndex;
            internal bool isVolumetric;
        }

        public void Render(RenderGraph graph, ref Renderer2DData rendererData, ref LayerBatch layerBatch, in TextureHandle lightTexture, in TextureHandle normalTexture, in TextureHandle depthTexture, in TextureHandle shadowTexture, int shadowlightIndex = -1, bool isVolumetric = false)
        {
            intermediateTexture[0] = lightTexture;
            Render(graph, ref rendererData, ref layerBatch, intermediateTexture, normalTexture, depthTexture, shadowTexture, shadowlightIndex, isVolumetric);
        }

        public void Render(RenderGraph graph, ref Renderer2DData rendererData, ref LayerBatch layerBatch, in TextureHandle[] lightTextures, in TextureHandle normalTexture, in TextureHandle depthTexture, in TextureHandle shadowTexture, int shadowlightIndex = -1, bool isVolumetric = false)
        {
            if (!layerBatch.lightStats.useLights ||
                isVolumetric && !layerBatch.lightStats.useVolumetricLights)
                return;

            if (layerBatch.lightStats.useNormalMap)
                RenderGraphUtils.SetGlobalTexture(graph, k_NormalMapID, normalTexture, "Set Normal");

            if (layerBatch.lightStats.useShadows)
                RenderGraphUtils.SetGlobalTexture(graph, k_ShadowMapID, shadowTexture, "Set Shadows");

            RenderGraphUtils.SetGlobalTexture(graph, k_FalloffLookupID, graph.ImportTexture(m_FallOffRTHandle), "Set Global FalloffLookup");
            RenderGraphUtils.SetGlobalTexture(graph, k_LightLookupID, graph.ImportTexture(m_LightLookupRTHandle), "Set Global LightLookup");

            using (var builder = graph.AddRasterRenderPass<PassData>("Light 2D Pass", out var passData, !isVolumetric ? m_ProfilingSampler : m_ProfilingSamplerVolume))
            {
                for (var i = 0; i < lightTextures.Length; i++)
                    builder.UseTextureFragment(lightTextures[i], i);

                builder.UseTextureFragmentDepth(depthTexture, IBaseRenderGraphBuilder.AccessFlags.Write);

                if (layerBatch.lightStats.useNormalMap)
                    builder.UseTexture(normalTexture);

                if (layerBatch.lightStats.useShadows)
                    builder.UseTexture(shadowTexture);

                var lights = shadowlightIndex != -1 ? layerBatch.shadowLights : layerBatch.lights;
                foreach (var light in lights)
                {
                    if (light.m_CookieSpriteTextureHandle.IsValid())
                        builder.UseTexture(light.m_CookieSpriteTextureHandle);
                }

                passData.layerBatch = layerBatch;
                passData.rendererData = rendererData;
                passData.shadowIndex = shadowlightIndex;
                passData.isVolumetric = isVolumetric;

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
