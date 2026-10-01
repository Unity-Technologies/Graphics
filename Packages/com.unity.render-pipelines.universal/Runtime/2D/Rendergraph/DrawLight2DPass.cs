using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.U2D.Profiler;
using CommonResourceData = UnityEngine.Rendering.Universal.UniversalResourceData;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawLight2DPass : ScriptableRenderPass
    {
        internal static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");
        internal static readonly string k_NormalMapID = "_NormalMap";
        internal static readonly string k_ShadowMapID = "_ShadowTex";
        TextureHandle[] intermediateTexture = new TextureHandle[1];

        internal static MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        static void ResetStaticsOnLoad()
        {
            s_PropertyBlock = new MaterialPropertyBlock();
        }
#endif

        internal void Setup(RenderGraph renderGraph, ContextContainer frameData)
        {
            var renderingData = frameData.Get<Universal2DRenderingData>().renderingData;

            foreach (var light in renderingData.lightCullResult.visibleLights)
            {
                if (light.useCookieSprite && light.m_CookieSpriteTexture != null)
                    light.m_CookieSpriteTextureHandle = renderGraph.ImportTexture(light.m_CookieSpriteTexture);

#if ENABLE_PROFILER && PROFILER_INSTALLED
                if (Renderer2D.canProfilerCapture)
                    ProfilerMarkers.s_LightMeshFrameData.Capture(light.gameObject, light.lightMesh);
#endif
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

        // The keyword state CreateLightMaterial bakes onto a built-in light material, recorded per draw
        // for a custom (Light2D.material) one. A custom material is shared by every light it is assigned
        // to, and by both the volumetric and non-volumetric pass, so it cannot hold these as baked state.
        static void SetCustomMaterialKeywords(RasterCommandBuffer cmd, Light2D light, Material lightMaterial, bool isVolumetric)
        {
            RendererLighting.SetVolumetricKeyword(cmd, lightMaterial, isVolumetric);

            // USE_ADDITIVE_BLENDING: non-volumetric pass only, matching CreateLightMaterial. On the
            // volumetric pass its branch would win over USE_VOLUMETRIC's, making the volume too
            // bright by 1/_L2D_VOLUME_OPACITY.
            RendererLighting.SetAdditiveBlendingKeyword(cmd, lightMaterial, !isVolumetric && light.overlapOperation == Light2D.OverlapOperation.Additive);

            // USE_POINT_LIGHT_COOKIES: enabled only for Point lights that have a valid cookie
            // sprite assigned. Matches the built-in cache condition in CreateLightMaterial.
            bool useCookie = light.lightType == Light2D.LightType.Point
                && light.lightCookieSprite != null && light.lightCookieSprite.texture != null;
            RendererLighting.SetPointLightCookiesKeyword(cmd, lightMaterial, useCookie);

            // USE_NORMAL_MAP: enabled per-light based on normalMapQuality, matching CreateLightMaterial.
            // Without it, Light2DPass.hlsl skips its normal-map block on custom materials. Required by
            // the 056_2D_Lights Material Override graphics tests.
            bool useNormalMap = light.normalMapQuality != Light2D.NormalMapQuality.Disabled;
            RendererLighting.SetNormalMapKeyword(cmd, lightMaterial, useNormalMap);
        }

        private static void Execute(RasterCommandBuffer cmd, PassData passData, LayerBatch layerBatch, int lightTextureIndex)
        {
            cmd.SetGlobalFloat(k_InverseHDREmulationScaleID, 1.0f / passData.rendererData.hdrEmulationScale);

            var blendStyleIndex = layerBatch.activeBlendStylesIndices[lightTextureIndex];
            var blendOpName = passData.rendererData.lightBlendStyles[blendStyleIndex].name;
            cmd.BeginSample(blendOpName);

            var indicesIndex = Renderer2D.supportsMRT ? lightTextureIndex : 0;
            if (!passData.isVolumetric)
                RendererLighting.EnableBlendStyle(cmd, indicesIndex, true);

            // Enable Rendering Layers
            if (passData.useRenderingLayers)
                cmd.EnableKeyword(ShaderGlobalKeywords.LightLayers);

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

                var useShadows = (!passData.isVolumetric && passData.layerBatch.lightStats.useShadows) || (passData.isVolumetric && passData.layerBatch.lightStats.useVolumetricShadowLights);
                useShadows &= layerBatch.shadowIndices.Contains(j);
                var lightMaterial = passData.rendererData.GetLightMaterial(light, passData.isVolumetric, useShadows);

                // Same test GetLightMaterial uses to pick the custom clone over the built-in cache.
                bool isCustomMaterial = light.material != null;

                // Dual-pass materials (ShaderGraph Light2D) use pass 0 for non-volumetric and pass 1 for
                // volumetric. Built-in materials are single-pass, so only a custom material can be one.
                bool isDualPassMaterial = isCustomMaterial && lightMaterial.passCount > 1;
                int shaderPass = passData.isVolumetric && isDualPassMaterial ? 1 : 0;

                // Custom materials bypass the built-in cache, so their keywords are recorded per draw.
                // Built-in materials already bake the same keywords, and recording them anyway costs
                // four keyword commands per light draw (UUM-154097).
                if (isCustomMaterial)
                    SetCustomMaterialKeywords(cmd, light, lightMaterial, passData.isVolumetric);

                var lightMesh = light.lightMesh;

                // For Batching.
                var index = light.batchSlotIndex;
                var slotIndex = RendererLighting.lightBatch.SlotIndex(index);
                bool canBatch = RendererLighting.lightBatch.CanBatch(light, lightMaterial, index, out int lightHash);

                bool breakBatch = !canBatch;
                if (breakBatch && LightBatch.isBatchingSupported)
                    RendererLighting.lightBatch.Flush(cmd);

                // s_PropertyBlock is shared and never cleared, so a stale texture stays bound.
                // Render Graph can recycle that texture into this pass's own render target, and
                // the ShaderGraph light shader samples _ShadowTex in every variant. Rebind both
                // every draw so an unused slot holds black rather than a live colour target.
                if (passData.layerBatch.lightStats.useNormalMap)
                    s_PropertyBlock.SetTexture(k_NormalMapID, passData.normalMap);
                else
                    s_PropertyBlock.SetTexture(k_NormalMapID, passData.fallbackTexture);

                if (useShadows && TryGetShadowIndex(ref layerBatch, j, out var shadowIndex))
                    s_PropertyBlock.SetTexture(k_ShadowMapID, passData.shadowTextures[shadowIndex]);
                else
                    s_PropertyBlock.SetTexture(k_ShadowMapID, passData.fallbackTexture);

                if (!passData.isVolumetric || (passData.isVolumetric && light.volumetricEnabled))
                    RendererLighting.SetCookieShaderProperties(light, s_PropertyBlock);

                // Set shader global properties
                RendererLighting.SetPerLightShaderGlobals(cmd, light, slotIndex, passData.isVolumetric, useShadows, LightBatch.isBatchingSupported);

                if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled || light.lightType == Light2D.LightType.Point)
                    RendererLighting.SetPerPointLightShaderGlobals(cmd, light, slotIndex, LightBatch.isBatchingSupported);

                // The batch path uses DrawMultipleMeshes with shaderPass=-1 (all passes), which would
                // render both passes and double-draw dual-pass materials, so skip batching for them.
                if (LightBatch.isBatchingSupported && !isDualPassMaterial)
                {
                    RendererLighting.lightBatch.AddBatch(light, lightMaterial, light.GetMatrix(), lightMesh, 0, lightHash, index);
                    RendererLighting.lightBatch.Flush(cmd);
                }
                else
                {
#if ENABLE_PROFILER && PROFILER_INSTALLED
                    if (Renderer2D.canProfilerCapture)
                        ProfilerMarkers.s_U2DLightBatchCounterValue.Value++;
#endif
                    cmd.DrawMesh(lightMesh, light.GetMatrix(), lightMaterial, 0, shaderPass, s_PropertyBlock);
                }
#if ENABLE_PROFILER && PROFILER_INSTALLED
                if (Renderer2D.canProfilerCapture)
                    ProfilerMarkers.s_LightRenderFrameData.Capture(light.gameObject.GetEntityId());
#endif
            }
            RendererLighting.EnableBlendStyle(cmd, indicesIndex, false);

            // Disable Rendering Layers
            if (passData.useRenderingLayers)
                cmd.DisableKeyword(ShaderGlobalKeywords.LightLayers);

            cmd.EndSample(blendOpName);
        }

        internal class PassData
        {
            internal LayerBatch layerBatch;
            internal Renderer2DData rendererData;
            internal bool isVolumetric;

            internal TextureHandle normalMap;
            internal TextureHandle[] shadowTextures;
            internal TextureHandle fallbackTexture;

            internal int lightTextureIndex;
            internal bool useRenderingLayers;
            internal RenderingLayerUtils.MaskSize maskSize;
        }

        void InitializeRenderPass(RenderGraph graph, IRasterRenderGraphBuilder builder, ContextContainer frameData, PassData passData, int batchIndex, bool isVolumetric = false)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            Renderer2DData rendererData = frameData.Get<Universal2DRenderingData>().renderingData;
            var layerBatch = frameData.Get<Universal2DRenderingData>().layerBatches[batchIndex];

            intermediateTexture[0] = commonResourceData.activeColorTexture;

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

            if (rendererData.useRenderingLayers)
                builder.UseTexture(universal2DResourceData.renderingLayersTexture);

            passData.useRenderingLayers = rendererData.useRenderingLayers;
            passData.maskSize = rendererData.renderingLayersMaskSize;
            passData.layerBatch = layerBatch;
            passData.rendererData = rendererData;
            passData.isVolumetric = isVolumetric;
            passData.normalMap = layerBatch.lightStats.useNormalMap ? universal2DResourceData.normalsTexture[batchIndex] : TextureHandle.nullHandle;
            passData.fallbackTexture = graph.defaultResources.blackTexture;
            builder.UseTexture(passData.fallbackTexture);

            builder.AllowGlobalStateModification(true);
        }

        internal void Render(RenderGraph graph, ContextContainer frameData, int batchIndex, bool isVolumetric = false)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            var layerBatch = frameData.Get<Universal2DRenderingData>().layerBatches[batchIndex];

            // Check for lighting in scene/prefab/preview camera 
            var isLightingActive = Renderer2D.IsSceneViewOrPreviewLightingActive(cameraData);

            if (!layerBatch.lightStats.useLights ||
                isVolumetric && !layerBatch.lightStats.useVolumetricLights ||
                !isLightingActive)
                return;

            // Render single RTs by for apis that don't support MRTs
            if (!isVolumetric && !Renderer2D.supportsMRT)
            {
                var passName = ProfilerMarkers.s_LightSRTPass;
                LayerDebug.FormatPassName(layerBatch, ref passName);

                for (var i = 0; i < layerBatch.activeBlendStylesIndices.Length; ++i)
                {
                    using (var builder = graph.AddRasterRenderPass<PassData>(passName, out var passData, LayerDebug.GetProfilingSampler(passName, ProfilerMarkers.s_ProfilingSampleSRT)))
                    {
                        InitializeRenderPass(graph, builder, frameData, passData, batchIndex, isVolumetric);

                        var lightTextures = universal2DResourceData.lightTextures[batchIndex];

                        builder.SetRenderAttachment(lightTextures[i], 0);

                        passData.lightTextureIndex = i;

                        builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                        {
                            RenderingLayerUtils.SetupProperties(context.cmd, data.maskSize);

                            Execute(context.cmd, data, data.layerBatch, data.lightTextureIndex);
                        });
                    }
                }
            }
            else
            {
                var passName = !isVolumetric ? ProfilerMarkers.s_LightPass : ProfilerMarkers.s_LightVolumetricPass;
                var profilingSampler = !isVolumetric ? ProfilerMarkers.s_ProfilingSampler : ProfilerMarkers.s_ProfilingSamplerVolume;
                LayerDebug.FormatPassName(layerBatch, ref passName);

                // Default Raster Pass with MRTs
                using (var builder = graph.AddRasterRenderPass<PassData>(passName, out var passData, LayerDebug.GetProfilingSampler(passName, profilingSampler)))
                {
                    InitializeRenderPass(graph, builder, frameData, passData, batchIndex, isVolumetric);

                    var lightTextures = !isVolumetric ? universal2DResourceData.lightTextures[batchIndex] : intermediateTexture;

                    for (var i = 0; i < lightTextures.Length; i++)
                        builder.SetRenderAttachment(lightTextures[i], i);

                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    {
                        RenderingLayerUtils.SetupProperties(context.cmd, data.maskSize);

                        for (var i = 0; i < data.layerBatch.activeBlendStylesIndices.Length; ++i)
                            Execute(context.cmd, data, data.layerBatch, i);
                    });
                }
            }
        }
    }
}
