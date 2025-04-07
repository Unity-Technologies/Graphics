using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    // The common shader stripper function
    class CommonShaderPreprocessor : BaseShaderPreprocessor
    {
        public override int Priority => 100;
        private HDRenderPipelineRuntimeShaders m_ShaderResources;
        private HDRenderPipelineRuntimeMaterials m_MaterialResources;

        public CommonShaderPreprocessor()
        {
           m_ShaderResources = HDRPBuildData.instance.runtimeShaders;
           m_MaterialResources = HDRPBuildData.instance.materialResources;
        }

        protected override bool DoShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData)
        {
            bool stripDebugVariants = HDRPBuildData.instance.stripDebugVariants;

            // CAUTION: Pass Name and Lightmode name must match in master node and .shader.
            // HDRP use LightMode to do drawRenderer and pass name is use here for stripping!
            var settings = hdrpAsset.currentPlatformRenderPipelineSettings;

            // Remove water if disabled
            if (!settings.supportWater)
            {
                if (shader == m_ShaderResources.opaqueAtmosphericScatteringPS)
                {
                    if (inputData.shaderKeywordSet.IsEnabled(m_SupportWater) ||
                        inputData.shaderKeywordSet.IsEnabled(m_SupportWaterCaustics) ||
                        inputData.shaderKeywordSet.IsEnabled(m_SupportWaterCausticsShadow))
                        return true;
                }

                if (inputData.shaderKeywordSet.IsEnabled(m_SupportWaterAbsorption))
                    return true;

                if (stripDebugVariants && snippet.passName.StartsWith(WaterSystem.k_WaterDebugPass))
                    return true;
            }
            if (HDRPBuildData.instance.waterDecalMaskAndCurrent)
            {
                if (inputData.shaderKeywordSet.IsEnabled(m_WaterDecalPartial))
                    return true;
            }
            else
            {
                if (inputData.shaderKeywordSet.IsEnabled(m_WaterDecalComplete))
                    return true;
            }

            // If Screen Space Lens Flare is disabled, strip all the shaders
            if (!settings.supportScreenSpaceLensFlare)
            {
                if (shader == m_ShaderResources.lensFlareScreenSpacePS)
                    return true;
            }

            // If Data Driven Lens Flare is disabled, strip all the shaders (the preview shader LensFlareDataDrivenPreview.shader in Core will not be stripped)
            if (!settings.supportDataDrivenLensFlare)
            {
                if (shader == m_ShaderResources.lensFlareDataDrivenPS)
                    return true;
            }

            // Remove editor only pass
            bool isSceneSelectionPass = snippet.passName == "SceneSelectionPass";
            bool isScenePickingPass = snippet.passName == "ScenePickingPass";
            bool metaPassUnused = (snippet.passName == "META") && (SupportedRenderingFeatures.active.enlighten == false ||
                ((int)SupportedRenderingFeatures.active.lightmapBakeTypes | (int)LightmapBakeType.Realtime) == 0);
            bool editorVisualization = inputData.shaderKeywordSet.IsEnabled(m_EditorVisualization);
            if (isSceneSelectionPass || isScenePickingPass || metaPassUnused || editorVisualization)
                return true;

            // CAUTION: We can't identify transparent material in the stripped in a general way.
            // Shader Graph don't produce any keyword - However it will only generate the pass that are required, so it already handle transparent (Note that shader Graph still define _SURFACE_TYPE_TRANSPARENT but as a #define)
            // For inspector version of shader, we identify transparent with a shader feature _SURFACE_TYPE_TRANSPARENT.
            // Only our Lit (and inherited) shader use _SURFACE_TYPE_TRANSPARENT, so the specific stripping based on this keyword is in LitShadePreprocessor.
            // Here we can't strip based on opaque or transparent but we will strip based on HDRP Asset configuration.

            bool isMotionPass = snippet.passName == "MotionVectors";
            if (isMotionPass && !settings.supportMotionVectors)
                return true;

            bool isDistortionPass = snippet.passName == "DistortionVectors";
            if (isDistortionPass && !settings.supportDistortion)
                return true;

            bool isTransparentBackface = snippet.passName == "TransparentBackface";
            if (isTransparentBackface && !settings.supportTransparentBackface)
                return true;

            bool isTransparentPrepass = snippet.passName == "TransparentDepthPrepass";
            if (isTransparentPrepass && !settings.supportTransparentDepthPrepass)
                return true;

            bool isTransparentPostpass = snippet.passName == "TransparentDepthPostpass";
            if (isTransparentPostpass && !settings.supportTransparentDepthPostpass)
                return true;

            bool isRayTracingPrepass = snippet.passName == "RayTracingPrepass";
            if (isRayTracingPrepass && !settings.supportRayTracing)
                return true;

            // If requested by the render pipeline settings, or if we are in a release build,
            // don't compile fullscreen debug display variant
            bool isFullScreenDebugPass = snippet.passName == "FullScreenDebug";
            if (isFullScreenDebugPass && stripDebugVariants)
                return true;

            // Debug Display shader is currently the longest shader to compile, so we allow users to disable it at runtime.
            // We also don't want it in release build.
            // However our AOV API rely on several debug display shader. In case AOV API is requested at runtime (like for the Graphics Compositor)
            // we allow user to make explicit request for it and it bypass other request
            if (stripDebugVariants && !settings.supportRuntimeAOVAPI)
            {
                if (shader == m_ShaderResources.debugDisplayLatlongPS ||
                    shader == m_ShaderResources.debugViewMaterialGBufferPS ||
                    shader == m_ShaderResources.debugViewTilesPS ||
                    shader == m_ShaderResources.debugFullScreenPS ||
                    shader == m_ShaderResources.debugColorPickerPS ||
                    shader == m_ShaderResources.debugExposurePS ||
                    shader == m_ShaderResources.debugHDRPS ||
                    shader == m_ShaderResources.debugLightVolumePS ||
                    shader == m_ShaderResources.debugBlitQuad ||
                    shader == m_ShaderResources.debugViewVirtualTexturingBlit ||
                    shader == m_ShaderResources.debugWaveformPS ||
                    shader == m_ShaderResources.debugVectorscopePS ||
                    shader == m_ShaderResources.debugLocalVolumetricFogAtlasPS)
                    return true;

                if (inputData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
                    return true;
            }

            if (inputData.shaderKeywordSet.IsEnabled(m_WriteMSAADepth) && (settings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly))
                return true;

            if (!settings.supportSubsurfaceScattering)
            {
                if (shader == m_ShaderResources.combineLightingPS)
                    return true;
                // Note that this is only going to affect the deferred shader and for a debug case, so it won't save much.
                if (inputData.shaderKeywordSet.IsEnabled(m_SubsurfaceScattering))
                    return true;
            }

            if (!settings.lightLoopSettings.supportFabricConvolution)
            {
                if (shader == m_ShaderResources.charlieConvolvePS)
                    return true;
            }

            if (inputData.shaderKeywordSet.IsEnabled(m_Transparent))
            {
                // If transparent we don't need the depth only pass
                bool isDepthOnlyPass = snippet.passName == "DepthForwardOnly";
                if (isDepthOnlyPass)
                    return true;

                // If transparent we don't need the motion vector pass
                if (isMotionPass)
                    return true;

                // If we are transparent we use cluster lighting and not tile lighting
                if (inputData.shaderKeywordSet.IsEnabled(m_TileLighting))
                    return true;
            }
            else // Opaque
            {
                // If opaque, we never need transparent specific passes (even in forward only mode)
                bool isTransparentForwardPass = isTransparentPostpass || isTransparentBackface || isTransparentPrepass || isDistortionPass;
                if (isTransparentForwardPass)
                    return true;

                // TODO: Should we remove Cluster version if we know MSAA is disabled ? This prevent to manipulate LightLoop Settings (useFPTL option)
                // For now comment following code
                // if (inputData.shaderKeywordSet.IsEnabled(m_ClusterLighting) && !hdrpAsset.currentPlatformRenderPipelineSettings.supportMSAA)
                //    return true;
            }

            // SHADOW

            // Strip every useless shadow configs
            var shadowInitParams = settings.hdShadowInitParams;

            foreach (var shadowVariant in m_ShadowKeywords.PunctualShadowVariants)
            {
                if (shadowVariant.Key != shadowInitParams.punctualShadowFilteringQuality)
                    if (inputData.shaderKeywordSet.IsEnabled(shadowVariant.Value))
                        return true;
            }

            foreach (var shadowVariant in m_ShadowKeywords.DirectionalShadowVariants)
            {
                if (shadowVariant.Key != shadowInitParams.directionalShadowFilteringQuality)
                    if (inputData.shaderKeywordSet.IsEnabled(shadowVariant.Value))
                        return true;
            }

            foreach (var areaShadowVariant in m_ShadowKeywords.AreaShadowVariants)
            {
                if (areaShadowVariant.Key != shadowInitParams.areaShadowFilteringQuality)
                    if (inputData.shaderKeywordSet.IsEnabled(areaShadowVariant.Value))
                        return true;
            }

            if (!shadowInitParams.supportScreenSpaceShadows && shader == m_ShaderResources.screenSpaceShadowPS)
                return true;

            // Screen space shadow variant is exclusive, either we have a variant with dynamic if that support screen space shadow or not
            // either we have a variant that don't support at all. We can't have both at the same time.
            if (inputData.shaderKeywordSet.IsEnabled(m_ScreenSpaceShadowOFFKeywords) && shadowInitParams.supportScreenSpaceShadows)
                return true;

            if (inputData.shaderKeywordSet.IsEnabled(m_ScreenSpaceShadowONKeywords) && !shadowInitParams.supportScreenSpaceShadows)
                return true;

            // DECAL

            // Rendering layers and decal layers output to the same buffer
            // Difference is that decal layers need also geometric normals, and rendering layers ignore _DISABLE_DECALS
            // To reduce variants, we assume that enabling rendering layers will always enable decal layers, so we have 3 modes:
            // - All off
            // - Output layers and normal for relevant materials
            // - Output layers and normals for everyone. (But if decal are disabled, buffer is only 16 bits so we don't write normals)
            if (settings.renderingLayerMaskBuffer)
            {
                if (inputData.shaderKeywordSet.IsEnabled(m_WriteDecalBuffer))
                    return true;
            }
            else
            {
                if (inputData.shaderKeywordSet.IsEnabled(m_WriteRenderingLayer))
                    return true;
                // If we don't require the rendering layers, strip the decal prepass variant when decals are disabled
                if ((inputData.shaderKeywordSet.IsEnabled(m_WriteDecalBuffer) || inputData.shaderKeywordSet.IsEnabled(m_WriteDecalBufferAndRenderingLayer)) &&
                    !(settings.supportDecals && settings.supportDecalLayers))
                    return true;
            }

            // If decal support, remove unused variant
            if (settings.supportDecals)
            {
                // Remove the no decal case
                if (inputData.shaderKeywordSet.IsEnabled(m_DecalsOFF))
                    return true;

                // If decal but with 4RT remove 3RT variant and vice versa for both Material and Decal Material
                if (inputData.shaderKeywordSet.IsEnabled(m_Decals3RT) && settings.decalSettings.perChannelMask)
                    return true;

                if (inputData.shaderKeywordSet.IsEnabled(m_Decals4RT) && !settings.decalSettings.perChannelMask)
                    return true;

                // Remove the surface gradient blending if not enabled
                if (inputData.shaderKeywordSet.IsEnabled(m_DecalSurfaceGradient) && !settings.supportSurfaceGradient)
                    return true;
            }
            else
            {
                // Strip if it is a decal pass
                bool isDBufferMesh = snippet.passName == "DBufferMesh";
                bool isDecalMeshForwardEmissive = snippet.passName == "DecalMeshForwardEmissive";
                bool isDBufferProjector = snippet.passName == "DBufferProjector";
                bool isDecalProjectorForwardEmissive = snippet.passName == "DecalProjectorForwardEmissive";
                bool isAtlasProjector = snippet.passName == "AtlasProjector";
                if (isDBufferMesh || isDecalMeshForwardEmissive || isDBufferProjector || isDecalProjectorForwardEmissive || isAtlasProjector)
                    return true;

                // If no decal support, remove decal variant
                if (inputData.shaderKeywordSet.IsEnabled(m_Decals3RT) || inputData.shaderKeywordSet.IsEnabled(m_Decals4RT))
                    return true;

                // Remove the surface gradient blending
                if (inputData.shaderKeywordSet.IsEnabled(m_DecalSurfaceGradient))
                    return true;
            }

            // Global Illumination
            if (inputData.shaderKeywordSet.IsEnabled(m_ProbeVolumesL1) &&
                (!settings.supportProbeVolume || settings.probeVolumeSHBands != ProbeVolumeSHBands.SphericalHarmonicsL1))
                return true;

            if (inputData.shaderKeywordSet.IsEnabled(m_ProbeVolumesL2) &&
                (!settings.supportProbeVolume || settings.probeVolumeSHBands != ProbeVolumeSHBands.SphericalHarmonicsL2))
                return true;

            bool hasBicubicKeyword = shader.keywordSpace.FindKeyword(m_LightmapBicubicSampling.name).isValid;
            if (hasBicubicKeyword)
            {
                bool useBicubicLightmapSampling = false;
                if (GraphicsSettings.TryGetRenderPipelineSettings<LightmapSamplingSettings>(out var lightmapSamplingSettings))
                    useBicubicLightmapSampling = lightmapSamplingSettings.useBicubicLightmapSampling;
                if (inputData.shaderKeywordSet.IsEnabled(m_LightmapBicubicSampling) != useBicubicLightmapSampling)
                    return true;
            }

#if !ENABLE_SENSOR_SDK
            // If the SensorSDK package is not present, make sure that all code related to it is stripped away
            if (inputData.shaderKeywordSet.IsEnabled(m_SensorEnableLidar) || inputData.shaderKeywordSet.IsEnabled(m_SensorOverrideReflectance))
                return true;
#endif

            // HDR Output
            if (!HDROutputUtils.IsShaderVariantValid(inputData.shaderKeywordSet, PlayerSettings.allowHDRDisplaySupport))
                 return true;

            return false;
        }
    }
}
