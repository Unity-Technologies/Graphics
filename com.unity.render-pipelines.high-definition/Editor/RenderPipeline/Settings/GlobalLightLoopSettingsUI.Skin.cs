using UnityEditor.AnimatedValues;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static partial class GlobalLightLoopSettingsUI
    {
        const string k_CacheErrorFormat = "This configuration will lead to more than 2 GB reserved for this cache at runtime! ({0} requested) Only {1} element will be reserved instead.";
        const string k_CacheInfoFormat = "Reserving {0} in memory at runtime.";

        static readonly GUIContent k_CookiesHeaderContent = CoreEditorUtils.GetContent("Cookies");
        static readonly GUIContent k_ReflectionsHeaderContent = CoreEditorUtils.GetContent("Reflections");
        static readonly GUIContent k_SkyHeaderContent = CoreEditorUtils.GetContent("Sky");
        static readonly GUIContent k_LightLoopHeaderContent = CoreEditorUtils.GetContent("LightLoop");

        static readonly GUIContent k_CoockieSizeContent = CoreEditorUtils.GetContent("Cookie Size");
        static readonly GUIContent k_CookieTextureArraySizeContent = CoreEditorUtils.GetContent("Texture Array Size");
        static readonly GUIContent k_PointCoockieSizeContent = CoreEditorUtils.GetContent("Point Cookie Size");
        static readonly GUIContent k_PointCookieTextureArraySizeContent = CoreEditorUtils.GetContent("Cubemap Array Size");


        static readonly GUIContent k_CompressProbeCacheContent = CoreEditorUtils.GetContent("Compress Reflection Probe Cache");
        static readonly GUIContent k_CubemapSizeContent = CoreEditorUtils.GetContent("Cubemap Size");
        static readonly GUIContent k_ProbeCacheSizeContent = CoreEditorUtils.GetContent("Probe Cache Size");

        static readonly GUIContent k_CompressPlanarProbeCacheContent = CoreEditorUtils.GetContent("Compress Planar Reflection Probe Cache");
        static readonly GUIContent k_PlanarTextureSizeContent = CoreEditorUtils.GetContent("Planar Reflection Texture Size");
        static readonly GUIContent k_PlanarProbeCacheSizeContent = CoreEditorUtils.GetContent("Planar Probe Cache Size");

        static readonly GUIContent k_SupportFabricBSDFConvolutionContent = CoreEditorUtils.GetContent("Support Fabric BSDF Convolution");

        static readonly GUIContent k_SkyReflectionSizeContent = CoreEditorUtils.GetContent("Reflection Size");
        static readonly GUIContent k_SkyLightingOverrideMaskContent = CoreEditorUtils.GetContent("Lighting Override Mask|This layer mask will define in which layers the sky system will look for sky settings volumes for lighting override");
        const string k_SkyLightingHelpBoxContent = "Be careful, Sky Lighting Override Mask is set to Everything. This is most likely a mistake as it serves no purpose.";

        static readonly GUIContent k_MaxDirectionalContent = CoreEditorUtils.GetContent("Max Directional Lights On Screen");
        static readonly GUIContent k_MaxPonctualContent = CoreEditorUtils.GetContent("Max Punctual Lights On Screen");
        static readonly GUIContent k_MaxAreaContent = CoreEditorUtils.GetContent("Max Area Lights On Screen");
        static readonly GUIContent k_MaxEnvContent = CoreEditorUtils.GetContent("Max Env Lights On Screen");
        static readonly GUIContent k_MaxDecalContent = CoreEditorUtils.GetContent("Max Decals On Screen");
    }
}
