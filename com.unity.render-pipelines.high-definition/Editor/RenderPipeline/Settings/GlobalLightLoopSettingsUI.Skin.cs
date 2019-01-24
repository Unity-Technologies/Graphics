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

        static readonly GUIContent k_CookiesHeaderContent = EditorGUIUtility.TrTextContent("Cookies");
        static readonly GUIContent k_ReflectionsHeaderContent = EditorGUIUtility.TrTextContent("Reflections");
        static readonly GUIContent k_SkyHeaderContent = EditorGUIUtility.TrTextContent("Sky");
        static readonly GUIContent k_LightLoopHeaderContent = EditorGUIUtility.TrTextContent("LightLoop");

        static readonly GUIContent k_CoockieSizeContent = EditorGUIUtility.TrTextContent("Cookie Size");
        static readonly GUIContent k_CookieTextureArraySizeContent = EditorGUIUtility.TrTextContent("Texture Array Size");
        static readonly GUIContent k_PointCoockieSizeContent = EditorGUIUtility.TrTextContent("Point Cookie Size");
        static readonly GUIContent k_PointCookieTextureArraySizeContent = EditorGUIUtility.TrTextContent("Cubemap Array Size");


        static readonly GUIContent k_CompressProbeCacheContent = EditorGUIUtility.TrTextContent("Compress Reflection Probe Cache");
        static readonly GUIContent k_CubemapSizeContent = EditorGUIUtility.TrTextContent("Reflection Cubemap Size");
        static readonly GUIContent k_ProbeCacheSizeContent = EditorGUIUtility.TrTextContent("Probe Cache Size");

        static readonly GUIContent k_CompressPlanarProbeCacheContent = EditorGUIUtility.TrTextContent("Compress Planar Reflection Probe Cache");
        static readonly GUIContent k_PlanarTextureSizeContent = EditorGUIUtility.TrTextContent("Planar Reflection Texture Size");
        static readonly GUIContent k_PlanarProbeCacheSizeContent = EditorGUIUtility.TrTextContent("Planar Probe Cache Size");

        static readonly GUIContent k_SupportFabricBSDFConvolutionContent = EditorGUIUtility.TrTextContent("Support Fabric BSDF Convolution");

        static readonly GUIContent k_SkyReflectionSizeContent = EditorGUIUtility.TrTextContent("Reflection Size");
        static readonly GUIContent k_SkyLightingOverrideMaskContent = EditorGUIUtility.TrTextContent("Lighting Override Mask", "This layer mask will define in which layers the sky system will look for sky settings volumes for lighting override");
        const string k_SkyLightingHelpBoxContent = "Be careful, Sky Lighting Override Mask is set to Everything. This is most likely a mistake as it serves no purpose.";

        static readonly GUIContent k_MaxDirectionalContent = EditorGUIUtility.TrTextContent("Max Directional Lights On Screen");
        static readonly GUIContent k_MaxPonctualContent = EditorGUIUtility.TrTextContent("Max Punctual Lights On Screen");
        static readonly GUIContent k_MaxAreaContent = EditorGUIUtility.TrTextContent("Max Area Lights On Screen");
        static readonly GUIContent k_MaxEnvContent = EditorGUIUtility.TrTextContent("Max Env Lights On Screen");
        static readonly GUIContent k_MaxDecalContent = EditorGUIUtility.TrTextContent("Max Decals On Screen");
    }
}
