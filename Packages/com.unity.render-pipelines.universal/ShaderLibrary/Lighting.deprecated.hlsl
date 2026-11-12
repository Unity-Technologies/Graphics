#ifndef UNIVERSAL_LIGHTING_DEPRECATED_INCLUDED
#define UNIVERSAL_LIGHTING_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDFData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Light.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ExposureFunctions.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (Lighting.hlsl).

half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat,
    half3 lightColor, half3 lightDirectionWS, float lightAttenuation,
    half3 normalWS, half3 viewDirectionWS,
    half clearCoatMask, bool specularHighlights, bool clearCoatEnabled);
half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat, Light light, half3 normalWS, half3 viewDirectionWS, half clearCoatMask, bool specularHighlights, bool clearCoatEnabled);
half3 LightingPhysicallyBased(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS, bool specularHighlights, bool clearCoatEnabled);
half3 CalculateBlinnPhong(Light light, InputData inputData, SurfaceData surfaceData, bool specularEnabled, bool alphaPremultiplyEnabled);
URP_LIGHT_ACCUM4 UniversalFragmentPBR(InputData inputData, SurfaceData surfaceData, bool specularSetup, bool specularHighlights, bool alphaPremultiplyEnabled, bool clearCoatEnabled, bool receiveShadows, bool surfaceTypeTransparent, bool environmentReflections);
URP_LIGHT_ACCUM4 UniversalFragmentBlinnPhong(InputData inputData, SurfaceData surfaceData, bool specularEnabled, bool alphaPremultiplyEnabled, bool receiveShadows, bool surfaceTypeTransparent);
URP_LIGHT_ACCUM4 UniversalFragmentBakedLit(InputData inputData, SurfaceData surfaceData, bool surfaceTypeTransparent);

#if !defined(_ALPHAPREMULTIPLY_ON_KEYWORD_DECLARED)
    #if !defined(_ALPHAPREMULTIPLY_ON)
        #define _ALPHAPREMULTIPLY_ON 0
        #define _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_ALPHAPREMULTIPLY_ON)
        #undef _ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON 1
    #endif
#endif

#if !defined(_SPECULARHIGHLIGHTS_OFF_KEYWORD_DECLARED)
    #if !defined(_SPECULARHIGHLIGHTS_OFF)
        #define _SPECULARHIGHLIGHTS_OFF 0
        #define _SPECULARHIGHLIGHTS_OFF_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SPECULARHIGHLIGHTS_OFF)
        #undef _SPECULARHIGHLIGHTS_OFF
        #define _SPECULARHIGHLIGHTS_OFF 1
    #endif
#endif

#if !defined(_ENVIRONMENTREFLECTIONS_OFF_KEYWORD_DECLARED)
    #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
        #define _ENVIRONMENTREFLECTIONS_OFF 0
        #define _ENVIRONMENTREFLECTIONS_OFF_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_ENVIRONMENTREFLECTIONS_OFF)
        #undef _ENVIRONMENTREFLECTIONS_OFF
        #define _ENVIRONMENTREFLECTIONS_OFF 1
    #endif
#endif

#if !defined(_SPECGLOSSMAP_KEYWORD_DECLARED)
    #if !defined(_SPECGLOSSMAP)
        #define _SPECGLOSSMAP 0
        #define _SPECGLOSSMAP_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SPECGLOSSMAP)
        #undef _SPECGLOSSMAP
        #define _SPECGLOSSMAP 1
    #endif
#endif

#if !defined(_SPECULAR_COLOR_KEYWORD_DECLARED)
    #if !defined(_SPECULAR_COLOR)
        #define _SPECULAR_COLOR 0
        #define _SPECULAR_COLOR_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SPECULAR_COLOR)
        #undef _SPECULAR_COLOR
        #define _SPECULAR_COLOR 1
    #endif
#endif

#if !defined(_CLEARCOAT_KEYWORD_DECLARED)
    #if !defined(_CLEARCOAT)
        #define _CLEARCOAT 0
        #define _CLEARCOAT_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_CLEARCOAT)
        #undef _CLEARCOAT
        #define _CLEARCOAT 1
    #endif
#endif

#if !defined(_CLEARCOATMAP_KEYWORD_DECLARED)
    #if !defined(_CLEARCOATMAP)
        #define _CLEARCOATMAP 0
        #define _CLEARCOATMAP_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_CLEARCOATMAP)
        #undef _CLEARCOATMAP
        #define _CLEARCOATMAP 1
    #endif
#endif

#if !defined(_SPECULAR_SETUP_KEYWORD_DECLARED)
    #if !defined(_SPECULAR_SETUP)
        #define _SPECULAR_SETUP 0
        #define _SPECULAR_SETUP_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SPECULAR_SETUP)
        #undef _SPECULAR_SETUP
        #define _SPECULAR_SETUP 1
    #endif
#endif

#if !defined(_RECEIVE_SHADOWS_OFF_KEYWORD_DECLARED)
    #if !defined(_RECEIVE_SHADOWS_OFF)
        #define _RECEIVE_SHADOWS_OFF 0
        #define _RECEIVE_SHADOWS_OFF_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_RECEIVE_SHADOWS_OFF)
        #undef _RECEIVE_SHADOWS_OFF
        #define _RECEIVE_SHADOWS_OFF 1
    #endif
#endif

#if !defined(_SURFACE_TYPE_TRANSPARENT_KEYWORD_DECLARED)
    #if !defined(_SURFACE_TYPE_TRANSPARENT)
        #define _SURFACE_TYPE_TRANSPARENT 0
        #define _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SURFACE_TYPE_TRANSPARENT)
        #undef _SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT 1
    #endif
#endif

// Deprecated. Use LightingPhysicallyBased(..., specularHighlights, clearCoatEnabled).
half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat,
    half3 lightColor, half3 lightDirectionWS, float lightAttenuation,
    half3 normalWS, half3 viewDirectionWS,
    half clearCoatMask, bool specularHighlightsOff)
{
    return LightingPhysicallyBased(brdfData, brdfDataClearCoat, lightColor, lightDirectionWS, lightAttenuation, normalWS, viewDirectionWS, clearCoatMask, !specularHighlightsOff, _CLEARCOAT || _CLEARCOATMAP);
}

// Deprecated. Use LightingPhysicallyBased(..., specularHighlights, clearCoatEnabled).
half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat, Light light, half3 normalWS, half3 viewDirectionWS, half clearCoatMask, bool specularHighlightsOff)
{
    return LightingPhysicallyBased(brdfData, brdfDataClearCoat, light, normalWS, viewDirectionWS, clearCoatMask, !specularHighlightsOff, _CLEARCOAT || _CLEARCOATMAP);
}

// Deprecated. Construct a Light and use LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS, specularHighlights, clearCoatEnabled).
half3 LightingPhysicallyBased(BRDFData brdfData, half3 lightColor, half3 lightDirectionWS, float lightAttenuation, half3 normalWS, half3 viewDirectionWS, bool specularHighlights, bool clearCoatEnabled)
{
    Light light;
    light.color = lightColor;
    light.direction = lightDirectionWS;
    light.distanceAttenuation = lightAttenuation;
    light.shadowAttenuation   = 1;
    return LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS, specularHighlights, clearCoatEnabled);
}

// Deprecated. Use LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS, specularHighlights, clearCoatEnabled).
half3 LightingPhysicallyBased(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS)
{
    return LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS, !_SPECULARHIGHLIGHTS_OFF, _CLEARCOAT || _CLEARCOATMAP);
}

// Deprecated. Use LightingPhysicallyBased(..., specularHighlights, clearCoatEnabled).
half3 LightingPhysicallyBased(BRDFData brdfData, half3 lightColor, half3 lightDirectionWS, float lightAttenuation, half3 normalWS, half3 viewDirectionWS)
{
    return LightingPhysicallyBased(brdfData, lightColor, lightDirectionWS, lightAttenuation, normalWS, viewDirectionWS, !_SPECULARHIGHLIGHTS_OFF, _CLEARCOAT || _CLEARCOATMAP);
}

// Deprecated. Use LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS, specularHighlights, clearCoatEnabled).
half3 LightingPhysicallyBased(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS, bool specularHighlightsOff)
{
    return LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS, !specularHighlightsOff, _CLEARCOAT || _CLEARCOATMAP);
}

// Deprecated. Use LightingPhysicallyBased(..., specularHighlights, clearCoatEnabled).
half3 LightingPhysicallyBased(BRDFData brdfData, half3 lightColor, half3 lightDirectionWS, float lightAttenuation, half3 normalWS, half3 viewDirectionWS, bool specularHighlightsOff)
{
    return LightingPhysicallyBased(brdfData, lightColor, lightDirectionWS, lightAttenuation, normalWS, viewDirectionWS, !specularHighlightsOff, _CLEARCOAT || _CLEARCOATMAP);
}

// Deprecated. Use CalculateBlinnPhong(light, inputData, surfaceData, specularEnabled, alphaPremultiplyEnabled).
half3 CalculateBlinnPhong(Light light, InputData inputData, SurfaceData surfaceData)
{
    return CalculateBlinnPhong(light, inputData, surfaceData, _SPECGLOSSMAP || _SPECULAR_COLOR, _ALPHAPREMULTIPLY_ON);
}

// Deprecated. Use UniversalFragmentPBR(inputData, surfaceData, specularSetup, specularHighlights, alphaPremultiplyEnabled, clearCoatEnabled, receiveShadows, surfaceTypeTransparent, environmentReflections).
URP_LIGHT_ACCUM4 UniversalFragmentPBR(InputData inputData, SurfaceData surfaceData)
{
    return UniversalFragmentPBR(inputData, surfaceData, _SPECULAR_SETUP, !_SPECULARHIGHLIGHTS_OFF, _ALPHAPREMULTIPLY_ON, _CLEARCOAT || _CLEARCOATMAP, !_RECEIVE_SHADOWS_OFF, _SURFACE_TYPE_TRANSPARENT, !_ENVIRONMENTREFLECTIONS_OFF);
}

// Deprecated: Use the version which takes "SurfaceData" instead of passing all of these arguments...
URP_LIGHT_ACCUM4 UniversalFragmentPBR(InputData inputData, half3 albedo, half metallic, half3 specular,
    half smoothness, half occlusion, half3 emission, half alpha)
{
    SurfaceData surfaceData;

    surfaceData.albedo = albedo;
    surfaceData.specular = specular;
    surfaceData.metallic = metallic;
    surfaceData.smoothness = smoothness;
    surfaceData.normalTS = half3(0, 0, 1);
    surfaceData.emission = emission;
    surfaceData.occlusion = occlusion;
    surfaceData.alpha = alpha;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;

    return UniversalFragmentPBR(inputData, surfaceData, _SPECULAR_SETUP, !_SPECULARHIGHLIGHTS_OFF, _ALPHAPREMULTIPLY_ON, _CLEARCOAT || _CLEARCOATMAP, !_RECEIVE_SHADOWS_OFF, _SURFACE_TYPE_TRANSPARENT, !_ENVIRONMENTREFLECTIONS_OFF);
}

// Deprecated. Use UniversalFragmentBlinnPhong(inputData, surfaceData, specularEnabled, alphaPremultiplyEnabled, receiveShadows, surfaceTypeTransparent).
URP_LIGHT_ACCUM4 UniversalFragmentBlinnPhong(InputData inputData, SurfaceData surfaceData)
{
    return UniversalFragmentBlinnPhong(inputData, surfaceData, _SPECGLOSSMAP || _SPECULAR_COLOR, _ALPHAPREMULTIPLY_ON, !_RECEIVE_SHADOWS_OFF, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated: Use the version which takes "SurfaceData" instead of passing all of these arguments...
URP_LIGHT_ACCUM4 UniversalFragmentBlinnPhong(InputData inputData, half3 diffuse, half4 specularGloss, half smoothness, half3 emission, half alpha, half3 normalTS)
{
    SurfaceData surfaceData;

    surfaceData.albedo = diffuse;
    surfaceData.alpha = alpha;
    surfaceData.emission = emission;
    surfaceData.metallic = 0;
    surfaceData.occlusion = 1;
    surfaceData.smoothness = smoothness;
    surfaceData.specular = specularGloss.rgb;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;
    surfaceData.normalTS = normalTS;

    return UniversalFragmentBlinnPhong(inputData, surfaceData, _SPECGLOSSMAP || _SPECULAR_COLOR, _ALPHAPREMULTIPLY_ON, !_RECEIVE_SHADOWS_OFF, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated. Use UniversalFragmentBakedLit(inputData, surfaceData, surfaceTypeTransparent).
URP_LIGHT_ACCUM4 UniversalFragmentBakedLit(InputData inputData, SurfaceData surfaceData)
{
    return UniversalFragmentBakedLit(inputData, surfaceData, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated: Use the version which takes "SurfaceData" instead of passing all of these arguments...
URP_LIGHT_ACCUM4 UniversalFragmentBakedLit(InputData inputData, half3 color, half alpha, half3 normalTS)
{
    SurfaceData surfaceData;

    surfaceData.albedo = color;
    surfaceData.alpha = alpha;
    surfaceData.emission = half3(0, 0, 0);
    surfaceData.metallic = 0;
    surfaceData.occlusion = 1;
    surfaceData.smoothness = 1;
    surfaceData.specular = half3(0, 0, 0);
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;
    surfaceData.normalTS = normalTS;

    return UniversalFragmentBakedLit(inputData, surfaceData, _SURFACE_TYPE_TRANSPARENT);
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY)
    #undef _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY
    #undef _ALPHAPREMULTIPLY_ON
#endif

#if defined(_SPECULARHIGHLIGHTS_OFF_DEFINED_LOCALLY)
    #undef _SPECULARHIGHLIGHTS_OFF_DEFINED_LOCALLY
    #undef _SPECULARHIGHLIGHTS_OFF
#endif

#if defined(_SPECGLOSSMAP_DEFINED_LOCALLY)
    #undef _SPECGLOSSMAP_DEFINED_LOCALLY
    #undef _SPECGLOSSMAP
#endif

#if defined(_SPECULAR_COLOR_DEFINED_LOCALLY)
    #undef _SPECULAR_COLOR_DEFINED_LOCALLY
    #undef _SPECULAR_COLOR
#endif

#if defined(_CLEARCOAT_DEFINED_LOCALLY)
    #undef _CLEARCOAT_DEFINED_LOCALLY
    #undef _CLEARCOAT
#endif

#if defined(_CLEARCOATMAP_DEFINED_LOCALLY)
    #undef _CLEARCOATMAP_DEFINED_LOCALLY
    #undef _CLEARCOATMAP
#endif

#if defined(_SPECULAR_SETUP_DEFINED_LOCALLY)
    #undef _SPECULAR_SETUP_DEFINED_LOCALLY
    #undef _SPECULAR_SETUP
#endif

#if defined(_RECEIVE_SHADOWS_OFF_DEFINED_LOCALLY)
    #undef _RECEIVE_SHADOWS_OFF_DEFINED_LOCALLY
    #undef _RECEIVE_SHADOWS_OFF
#endif

#if defined(_ENVIRONMENTREFLECTIONS_OFF_DEFINED_LOCALLY)
    #undef _ENVIRONMENTREFLECTIONS_OFF_DEFINED_LOCALLY
    #undef _ENVIRONMENTREFLECTIONS_OFF
#endif

#if defined(_SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY)
    #undef _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY
    #undef _SURFACE_TYPE_TRANSPARENT
#endif

#endif // UNIVERSAL_LIGHTING_DEPRECATED_INCLUDED
