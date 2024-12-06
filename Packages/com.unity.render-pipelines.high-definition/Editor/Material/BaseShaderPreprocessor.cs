using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class ShadowKeywords
    {
        ShaderKeyword PunctualShadowLow;
        ShaderKeyword PunctualShadowMedium;
        ShaderKeyword PunctualShadowHigh;

        ShaderKeyword DirectionalShadowLow;
        ShaderKeyword DirectionalShadowMedium;
        ShaderKeyword DirectionalShadowHigh;

        ShaderKeyword AreaShadowMedium;
        ShaderKeyword AreaShadowHigh;

        public Dictionary<HDShadowFilteringQuality, ShaderKeyword> PunctualShadowVariants;
        public Dictionary<HDShadowFilteringQuality, ShaderKeyword> DirectionalShadowVariants;
        public Dictionary<HDAreaShadowFilteringQuality, ShaderKeyword> AreaShadowVariants;

        public ShadowKeywords()
        {
            PunctualShadowLow = new ShaderKeyword("PUNCTUAL_SHADOW_LOW");
            PunctualShadowMedium = new ShaderKeyword("PUNCTUAL_SHADOW_MEDIUM");
            PunctualShadowHigh = new ShaderKeyword("PUNCTUAL_SHADOW_HIGH");

            DirectionalShadowLow = new ShaderKeyword("DIRECTIONAL_SHADOW_LOW");
            DirectionalShadowMedium = new ShaderKeyword("DIRECTIONAL_SHADOW_MEDIUM");
            DirectionalShadowHigh = new ShaderKeyword("DIRECTIONAL_SHADOW_HIGH");

            AreaShadowMedium = new ShaderKeyword("AREA_SHADOW_MEDIUM");
            AreaShadowHigh = new ShaderKeyword("AREA_SHADOW_HIGH");

            PunctualShadowVariants = new Dictionary<HDShadowFilteringQuality, ShaderKeyword>
            {
                {HDShadowFilteringQuality.Low, PunctualShadowLow},
                {HDShadowFilteringQuality.Medium, PunctualShadowMedium},
                {HDShadowFilteringQuality.High, PunctualShadowHigh},
            };

            DirectionalShadowVariants = new Dictionary<HDShadowFilteringQuality, ShaderKeyword>
            {
                {HDShadowFilteringQuality.Low, DirectionalShadowLow},
                {HDShadowFilteringQuality.Medium, DirectionalShadowMedium},
                {HDShadowFilteringQuality.High, DirectionalShadowHigh},
            };

            AreaShadowVariants = new Dictionary<HDAreaShadowFilteringQuality, ShaderKeyword>
            {
                {HDAreaShadowFilteringQuality.Medium, AreaShadowMedium},
                {HDAreaShadowFilteringQuality.High, AreaShadowHigh},
            };
        }
    }

    abstract class BaseShaderPreprocessor
    {
        // Common keyword list
        protected ShaderKeyword m_Transparent;
        protected ShaderKeyword m_AlphaTestOn;
        protected ShaderKeyword m_DebugDisplay;
        protected ShaderKeyword m_TileLighting;
        protected ShaderKeyword m_ClusterLighting;
        protected ShaderKeyword m_LodFadeCrossFade;
        protected ShaderKeyword m_DecalsOFF;
        protected ShaderKeyword m_Decals3RT;
        protected ShaderKeyword m_Decals4RT;
        protected ShaderKeyword m_RenderingLayers;
        protected ShaderKeyword m_ShadowLow;
        protected ShaderKeyword m_ShadowMedium;
        protected ShaderKeyword m_ShadowHigh;
        protected ShaderKeyword m_WriteNormalBuffer;
        protected ShaderKeyword m_WriteDecalBuffer;
        protected ShaderKeyword m_WriteRenderingLayer;
        protected ShaderKeyword m_WriteDecalBufferAndRenderingLayer;
        protected ShaderKeyword m_WriteMSAADepth;
        protected ShaderKeyword m_SubsurfaceScattering;
        protected ShaderKeyword m_ScreenSpaceShadowOFFKeywords;
        protected ShaderKeyword m_ScreenSpaceShadowONKeywords;
        protected ShaderKeyword m_ProbeVolumesL1;
        protected ShaderKeyword m_ProbeVolumesL2;
        protected ShaderKeyword m_LightmapBicubicSampling;
        protected ShaderKeyword m_DecalSurfaceGradient;
        protected ShaderKeyword m_EditorVisualization;
        protected ShaderKeyword m_SupportWater;
        protected ShaderKeyword m_WaterDecalPartial;
        protected ShaderKeyword m_WaterDecalComplete;
        protected ShaderKeyword m_SupportWaterCaustics;
        protected ShaderKeyword m_SupportWaterCausticsShadow;
        protected ShaderKeyword m_SupportWaterAbsorption;

        protected ShadowKeywords m_ShadowKeywords;

#if !ENABLE_SENSOR_SDK
        protected ShaderKeyword m_SensorEnableLidar;
        protected ShaderKeyword m_SensorOverrideReflectance;
#endif

        protected Dictionary<HDShadowFilteringQuality, ShaderKeyword> m_ShadowVariants;

        public virtual int Priority => 0;

        public BaseShaderPreprocessor()
        {
            // NOTE: All these keyword should be automatically stripped so there's no need to handle them ourselves.
            // LIGHTMAP_ON, DIRLIGHTMAP_COMBINED, DYNAMICLIGHTMAP_ON, LIGHTMAP_SHADOW_MIXING, SHADOWS_SHADOWMASK
            // FOG_LINEAR, FOG_EXP, FOG_EXP2
            // STEREO_INSTANCING_ON, STEREO_MULTIVIEW_ON, STEREO_CUBEMAP_RENDER_ON
            // INSTANCING_ON
            m_Transparent = new ShaderKeyword("_SURFACE_TYPE_TRANSPARENT");
            m_AlphaTestOn = new ShaderKeyword("_ALPHATEST_ON");
            m_DebugDisplay = new ShaderKeyword("DEBUG_DISPLAY");
            m_TileLighting = new ShaderKeyword("USE_FPTL_LIGHTLIST");
            m_ClusterLighting = new ShaderKeyword("USE_CLUSTERED_LIGHTLIST");
            m_LodFadeCrossFade = new ShaderKeyword("LOD_FADE_CROSSFADE");
            m_DecalsOFF = new ShaderKeyword("DECALS_OFF");
            m_Decals3RT = new ShaderKeyword("DECALS_3RT");
            m_Decals4RT = new ShaderKeyword("DECALS_4RT");
            m_RenderingLayers = new ShaderKeyword("RENDERING_LAYERS");
            m_WriteNormalBuffer = new ShaderKeyword("WRITE_NORMAL_BUFFER");
            m_WriteDecalBuffer = new ShaderKeyword("WRITE_DECAL_BUFFER");
            m_WriteRenderingLayer = new ShaderKeyword("WRITE_RENDERING_LAYER");
            m_WriteDecalBufferAndRenderingLayer = new ShaderKeyword("WRITE_DECAL_BUFFER_AND_RENDERING_LAYER");
            m_WriteMSAADepth = new ShaderKeyword("WRITE_MSAA_DEPTH");
            m_SubsurfaceScattering = new ShaderKeyword("OUTPUT_SPLIT_LIGHTING");
            m_ScreenSpaceShadowOFFKeywords = new ShaderKeyword("SCREEN_SPACE_SHADOWS_OFF");
            m_ScreenSpaceShadowONKeywords = new ShaderKeyword("SCREEN_SPACE_SHADOWS_ON");
            m_ProbeVolumesL1 = new ShaderKeyword("PROBE_VOLUMES_L1");
            m_ProbeVolumesL2 = new ShaderKeyword("PROBE_VOLUMES_L2");
            m_LightmapBicubicSampling = new ShaderKeyword("LIGHTMAP_BICUBIC_SAMPLING");
            m_DecalSurfaceGradient = new ShaderKeyword("DECAL_SURFACE_GRADIENT");
            m_EditorVisualization = new ShaderKeyword("EDITOR_VISUALIZATION");
            m_SupportWater = new ShaderKeyword("SUPPORT_WATER");
            m_WaterDecalPartial = new ShaderKeyword("WATER_DECAL_PARTIAL");
            m_WaterDecalComplete = new ShaderKeyword("WATER_DECAL_COMPLETE");
            m_SupportWaterCaustics = new ShaderKeyword("SUPPORT_WATER_CAUSTICS");
            m_SupportWaterCausticsShadow = new ShaderKeyword("SUPPORT_WATER_CAUSTICS_SHADOW");
            m_SupportWaterAbsorption = new ShaderKeyword("SUPPORT_WATER_ABSORPTION");
            m_ShadowKeywords = new ShadowKeywords();

#if !ENABLE_SENSOR_SDK
            m_SensorEnableLidar = new ShaderKeyword("SENSORSDK_ENABLE_LIDAR");
            m_SensorOverrideReflectance = new ShaderKeyword("SENSORSDK_OVERRIDE_REFLECTANCE");
#endif
        }

        public bool ShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet,
            ShaderCompilerData inputData)
        {
            return IsMaterialQualityVariantStripped(hdrpAsset, inputData) || DoShadersStripper(hdrpAsset, shader, snippet, inputData);
        }

        protected abstract bool DoShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData);

        protected static bool IsMaterialQualityVariantStripped(HDRenderPipelineAsset hdrpAsset, ShaderCompilerData inputData)
        {
            var shaderMaterialLevel = inputData.shaderKeywordSet.GetMaterialQuality();
            // if there are material quality defines in this shader
            // and they don't match the material quality accepted by the hdrp asset
            if (shaderMaterialLevel != 0 && (hdrpAsset.availableMaterialQualityLevels & shaderMaterialLevel) == 0)
            {
                // then strip this variant
                return true;
            }

            return false;
        }
    }
}
