using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;
using CommonResourceData = UnityEngine.Rendering.Universal.UniversalResourceData;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawLight2DPass : ScriptableRenderPass
    {
        static readonly string k_LightPass = "Light2D Pass";
        static readonly string k_LightSRTPass = "Light2D SRT Pass";
        static readonly string k_LightVolumetricPass = "Light2D Volumetric Pass";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_LightPass);
        private static readonly ProfilingSampler m_ProfilingSampleSRT = new ProfilingSampler(k_LightSRTPass);
        private static readonly ProfilingSampler m_ProfilingSamplerVolume = new ProfilingSampler(k_LightVolumetricPass);
        internal static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");
        internal static readonly string k_NormalMapID = "_NormalMap";
        internal static readonly string k_ShadowMapID = "_ShadowTex";

        TextureHandle[] intermediateTexture = new TextureHandle[1];

        internal static MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        internal void Setup(RenderGraph renderGraph, ref Renderer2DData rendererData)
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

        private static void Execute(RasterCommandBuffer cmd, PassData passData, ref LayerBatch layerBatch, int lightTextureIndex)
        {
            cmd.SetGlobalFloat(k_InverseHDREmulationScaleID, 1.0f / passData.rendererData.hdrEmulationScale);

            var blendStyleIndex = layerBatch.activeBlendStylesIndices[lightTextureIndex];
            var blendOpName = passData.rendererData.lightBlendStyles[blendStyleIndex].name;
            cmd.BeginSample(blendOpName);

            var indicesIndex = Renderer2D.supportsMRT ? lightTextureIndex : 0;
            if (!passData.isVolumetric)
                RendererLighting.EnableBlendStyle(cmd, indicesIndex, true);

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

                var useShadows = passData.layerBatch.lightStats.useShadows && layerBatch.shadowIndices.Contains(j);
                var lightMaterial = passData.rendererData.GetLightMaterial(light, passData.isVolumetric, useShadows);
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

            RendererLighting.EnableBlendStyle(cmd, indicesIndex, false);
            cmd.EndSample(blendOpName);
        }

        internal class PassData
        {
            internal LayerBatch layerBatch;
            internal Renderer2DData rendererData;
            internal bool isVolumetric;

            internal TextureHandle normalMap;
            internal TextureHandle[] shadowTextures;

            internal int lightTextureIndex;
        }

        void InitializeRenderPass(IRasterRenderGraphBuilder builder, ContextContainer frameData, PassData passData, Renderer2DData rendererData, ref LayerBatch layerBatch, int batchIndex, bool isVolumetric = false)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();

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

            passData.layerBatch = layerBatch;
            passData.rendererData = rendererData;
            passData.isVolumetric = isVolumetric;
            passData.normalMap = layerBatch.lightStats.useNormalMap ? universal2DResourceData.normalsTexture[batchIndex] : TextureHandle.nullHandle;

            builder.AllowGlobalStateModification(true);
        }

        internal void Render(RenderGraph graph, ContextContainer frameData, Renderer2DData rendererData, ref LayerBatch layerBatch, int batchIndex, bool isVolumetric = false)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
            var isDebugLightingActive = debugHandler?.IsLightingActive ?? true;

#if UNITY_EDITOR
            if (cameraData.isSceneViewCamera && UnityEditor.SceneView.currentDrawingSceneView != null)
                isDebugLightingActive &= UnityEditor.SceneView.currentDrawingSceneView.sceneLighting;

            if (cameraData.camera.cameraType == CameraType.Preview)
                isDebugLightingActive = false;
#endif

            if (!layerBatch.lightStats.useLights ||
                isVolumetric && !layerBatch.lightStats.useVolumetricLights ||
                !isDebugLightingActive)
                return;

            // Render single RTs by for apis that don't support MRTs
            if (!isVolumetric && !Renderer2D.supportsMRT)
            {
                for (var i = 0; i < layerBatch.activeBlendStylesIndices.Length; ++i)
                {
                    using (var builder = graph.AddRasterRenderPass<PassData>(k_LightSRTPass, out var passData, m_ProfilingSampleSRT))
                    {
                        InitializeRenderPass(builder, frameData, passData, rendererData, ref layerBatch, batchIndex, isVolumetric);

                        var lightTextures = universal2DResourceData.lightTextures[batchIndex];

                        builder.SetRenderAttachment(lightTextures[i], 0);

                        passData.lightTextureIndex = i;

                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        {
                            Execute(context.cmd, data, ref data.layerBatch, data.lightTextureIndex);
                        });
                    }
                }
            }
            // Default Raster Pass with MRTs
            else
            {
                using (var builder = graph.AddRasterRenderPass<PassData>(!isVolumetric ? k_LightPass : k_LightVolumetricPass, out var passData, !isVolumetric ? m_ProfilingSampler : m_ProfilingSamplerVolume))
                {
                    InitializeRenderPass(builder, frameData, passData, rendererData, ref layerBatch, batchIndex, isVolumetric);

                    var lightTextures = !isVolumetric ? universal2DResourceData.lightTextures[batchIndex] : intermediateTexture;

                    for (var i = 0; i < lightTextures.Length; i++)
                        builder.SetRenderAttachment(lightTextures[i], i);
                   
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        for (var i = 0; i < data.layerBatch.activeBlendStylesIndices.Length; ++i)
                            Execute(context.cmd, data, ref data.layerBatch, i);
                    });
                }
            }
        }
    }
}
