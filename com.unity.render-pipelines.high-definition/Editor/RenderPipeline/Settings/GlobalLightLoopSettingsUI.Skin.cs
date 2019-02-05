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

        static readonly GUIContent k_CoockieSizeContent = EditorGUIUtility.TrTextContent("Cookie Size", "Specifies the maximum size for the individual 2D cookies that HDRP uses for Directional and Spot Lights.");
        static readonly GUIContent k_CookieTextureArraySizeContent = EditorGUIUtility.TrTextContent("Texture Array Size", "Sets the maximum Texture Array size for the 2D cookies HDRP uses for Directional and Spot Lights. Higher values allow HDRP to use more cookies concurrently on screen.");
        static readonly GUIContent k_PointCoockieSizeContent = EditorGUIUtility.TrTextContent("Point Cookie Size", "Specifies the maximum size for the Cube cookes HDRP uses for Point Lights.");
        static readonly GUIContent k_PointCookieTextureArraySizeContent = EditorGUIUtility.TrTextContent("Cubemap Array Size", "Sets the maximum Texture Array size for the Cube cookies HDRP uses for Directional and Spot Lights. Higher values allow HDRP to use more cookies concurrently on screen.");


        static readonly GUIContent k_CompressProbeCacheContent = EditorGUIUtility.TrTextContent("Compress Reflection Probe Cache", "When enabled, HDRP compresses the Reflection Probe cache to save disk space.");
        static readonly GUIContent k_CubemapSizeContent = EditorGUIUtility.TrTextContent("Reflection Cubemap Size", "Specifies the maximum resolution of the individual Reflection Probe cube maps.");
        static readonly GUIContent k_ProbeCacheSizeContent = EditorGUIUtility.TrTextContent("Probe Cache Size", "Sets the maximum size of the Probe Cache.");

        static readonly GUIContent k_CompressPlanarProbeCacheContent = EditorGUIUtility.TrTextContent("Compress Planar Reflection Probe Cache", "When enabled, HDRP compresses the Planar Reflection Probe cache to save disk space.");
        static readonly GUIContent k_PlanarTextureSizeContent = EditorGUIUtility.TrTextContent("Planar Reflection Texture Size", "Specifies the maximum resolution of Planar Reflection Textures.");
        static readonly GUIContent k_PlanarProbeCacheSizeContent = EditorGUIUtility.TrTextContent("Planar Probe Cache Size", "Sets the maximum size of the Planar Probe Cache.");

        static readonly GUIContent k_SupportFabricBSDFConvolutionContent = EditorGUIUtility.TrTextContent("Support Fabric BSDF Convolution", "When enabled, HDRP calculates a separate version of each Reflection Probe for the Fabric Shader, creating more accurate lighting effects. See the documentation for more information and limitations of this feature.");

        static readonly GUIContent k_SkyReflectionSizeContent = EditorGUIUtility.TrTextContent("Reflection Size", "Specifies the maximum resolution of the cube map HDRP uses to represent the sky.");
        static readonly GUIContent k_SkyLightingOverrideMaskContent = EditorGUIUtility.TrTextContent("Lighting Override Mask", "Specifies the layer mask HDRP uses to override sky lighting.");
        const string k_SkyLightingHelpBoxContent = "Be careful, Sky Lighting Override Mask is set to Everything. This is most likely a mistake as it serves no purpose.";

        static readonly GUIContent k_MaxDirectionalContent = EditorGUIUtility.TrTextContent("Max Directional Lights On Screen", "Sets the maximum number of Directional Lights HDRP can handle on screen at once.");
        static readonly GUIContent k_MaxPonctualContent = EditorGUIUtility.TrTextContent("Max Punctual Lights On Screen", "Sets the maximum number of Point and Spot Lights HDRP can handle on screen at once.");
        static readonly GUIContent k_MaxAreaContent = EditorGUIUtility.TrTextContent("Max Area Lights On Screen", "Sets the maximum number of area Lights HDRP can handle on screen at once.");
        static readonly GUIContent k_MaxEnvContent = EditorGUIUtility.TrTextContent("Max Env Lights On Screen", "Sets the maximum number of environment Lights HDRP can handle on screen at once.");
        static readonly GUIContent k_MaxDecalContent = EditorGUIUtility.TrTextContent("Max Decals On Screen", "Sets the maximum number of Decals HDRP can handle on screen at once.");

    }
}
