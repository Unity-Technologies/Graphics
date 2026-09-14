#ifndef UNIVERSAL_INPUT_SURFACE_FUNCTIONS_DEPRECATED_INCLUDED
#define UNIVERSAL_INPUT_SURFACE_FUNCTIONS_DEPRECATED_INCLUDED

// Deprecated keyword-reading material helpers, kept for external back-compat.
// They are fully parameterized (no texture bindings), so the material input files can expose
// them through their own .deprecated counterparts without duplicating resource declarations.
// New code samples through the material getters and the Shaders/Utils feature helpers.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

#if !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A_KEYWORD_DECLARED)
    #if !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
        #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 0
        #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
        #undef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1
    #endif
#endif

#if !defined(_GLOSSINESS_FROM_BASE_ALPHA_KEYWORD_DECLARED)
    #if !defined(_GLOSSINESS_FROM_BASE_ALPHA)
        #define _GLOSSINESS_FROM_BASE_ALPHA 0
        #define _GLOSSINESS_FROM_BASE_ALPHA_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_GLOSSINESS_FROM_BASE_ALPHA)
        #undef _GLOSSINESS_FROM_BASE_ALPHA
        #define _GLOSSINESS_FROM_BASE_ALPHA 1
    #endif
#endif

#if !defined(_NORMALMAP_KEYWORD_DECLARED)
    #if !defined(_NORMALMAP)
        #define _NORMALMAP 0
        #define _NORMALMAP_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_NORMALMAP)
        #undef _NORMALMAP
        #define _NORMALMAP 1
    #endif
#endif

#if !defined(_EMISSION_KEYWORD_DECLARED)
    #if !defined(_EMISSION)
        #define _EMISSION 0
        #define _EMISSION_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_EMISSION)
        #undef _EMISSION
        #define _EMISSION 1
    #endif
#endif

///////////////////////////////////////////////////////////////////////////////
//                      Material Property Helpers                            //
///////////////////////////////////////////////////////////////////////////////
half Alpha(half albedoAlpha, half4 color, half cutoff)
{
    half alpha = (_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A || _GLOSSINESS_FROM_BASE_ALPHA) ? color.a : albedoAlpha * color.a;

    alpha = AlphaDiscard(alpha, cutoff);

    return alpha;
}

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
}

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
{
    half3 normalTS = half3(0.0h, 0.0h, 1.0h);
    if (_NORMALMAP)
    {
        half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
        #if BUMP_SCALE_NOT_SUPPORTED
            normalTS = UnpackNormal(n);
        #else
            normalTS = UnpackNormalScale(n, scale);
        #endif
    }
    return normalTS;
}

half3 SampleEmission(float2 uv, half3 emissionColor, TEXTURE2D_PARAM(emissionMap, sampler_emissionMap))
{
    half3 emission = half3(0.0h, 0.0h, 0.0h);
    if (_EMISSION)
        emission = SAMPLE_TEXTURE2D(emissionMap, sampler_emissionMap, uv).rgb * emissionColor;
    return emission;
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A_DEFINED_LOCALLY)
    #undef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A_DEFINED_LOCALLY
    #undef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
#endif

#if defined(_GLOSSINESS_FROM_BASE_ALPHA_DEFINED_LOCALLY)
    #undef _GLOSSINESS_FROM_BASE_ALPHA_DEFINED_LOCALLY
    #undef _GLOSSINESS_FROM_BASE_ALPHA
#endif

#if defined(_NORMALMAP_DEFINED_LOCALLY)
    #undef _NORMALMAP_DEFINED_LOCALLY
    #undef _NORMALMAP
#endif

#if defined(_EMISSION_DEFINED_LOCALLY)
    #undef _EMISSION_DEFINED_LOCALLY
    #undef _EMISSION
#endif

#endif // UNIVERSAL_INPUT_SURFACE_FUNCTIONS_DEPRECATED_INCLUDED
