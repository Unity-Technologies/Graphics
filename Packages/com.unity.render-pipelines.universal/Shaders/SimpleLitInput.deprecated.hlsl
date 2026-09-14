#ifndef UNIVERSAL_SIMPLE_LIT_INPUT_DEPRECATED_INCLUDED
#define UNIVERSAL_SIMPLE_LIT_INPUT_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

// Deprecated keyword-reading helpers, kept for external back-compat.
// New code passes the keyword state to InitializeSimpleLitSurfaceData as parameters (SimpleLitInput.hlsl).

// Trunk's SimpleLitInput.hlsl transitively provided the SurfaceInput helpers (Alpha,
// SampleAlbedoAlpha, SampleNormal, SampleEmission); custom shaders built on copies of
// SimpleLitInput rely on them.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInputFunctions.deprecated.hlsl"

void InitializeSimpleLitSurfaceData(float2 uv, out SurfaceData outSurfaceData, bool specGlossMap, bool specularColor, bool glossinessFromBaseAlpha, bool alphaModulateEnabled, bool normalMap, bool emission);

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

#if !defined(_GLOSSINESS_FROM_BASE_ALPHA_KEYWORD_DECLARED)
    #if !defined(_GLOSSINESS_FROM_BASE_ALPHA)
        #define _GLOSSINESS_FROM_BASE_ALPHA 0
        #define _GLOSSINESS_FROM_BASE_ALPHA_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_GLOSSINESS_FROM_BASE_ALPHA)
        #undef _GLOSSINESS_FROM_BASE_ALPHA
        #define _GLOSSINESS_FROM_BASE_ALPHA 1
    #endif
#endif

#if !defined(_ALPHAMODULATE_ON_KEYWORD_DECLARED)
    #if !defined(_ALPHAMODULATE_ON)
        #define _ALPHAMODULATE_ON 0
        #define _ALPHAMODULATE_ON_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_ALPHAMODULATE_ON)
        #undef _ALPHAMODULATE_ON
        #define _ALPHAMODULATE_ON 1
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

// Deprecated. Sample the specular map and apply the smoothness source at the call site instead.
half4 SampleSpecularSmoothness(float2 uv, half alpha, half4 specColor, TEXTURE2D_PARAM(specMap, sampler_specMap))
{
    half4 specularSmoothness = half4(0, 0, 0, 1);
    if (_SPECGLOSSMAP)
        specularSmoothness = SAMPLE_TEXTURE2D(specMap, sampler_specMap, uv) * specColor;
    else if (_SPECULAR_COLOR)
        specularSmoothness = specColor;

    if (_GLOSSINESS_FROM_BASE_ALPHA)
        specularSmoothness.a = alpha;

    return specularSmoothness;
}

// Deprecated. Use InitializeSimpleLitSurfaceData(..., specGlossMap, specularColor, glossinessFromBaseAlpha, alphaModulateEnabled, normalMap, emission).
inline void InitializeSimpleLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    InitializeSimpleLitSurfaceData(uv, outSurfaceData, _SPECGLOSSMAP, _SPECULAR_COLOR, _GLOSSINESS_FROM_BASE_ALPHA, _ALPHAMODULATE_ON, _NORMALMAP, _EMISSION);
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_SPECGLOSSMAP_DEFINED_LOCALLY)
    #undef _SPECGLOSSMAP_DEFINED_LOCALLY
    #undef _SPECGLOSSMAP
#endif

#if defined(_SPECULAR_COLOR_DEFINED_LOCALLY)
    #undef _SPECULAR_COLOR_DEFINED_LOCALLY
    #undef _SPECULAR_COLOR
#endif

#if defined(_GLOSSINESS_FROM_BASE_ALPHA_DEFINED_LOCALLY)
    #undef _GLOSSINESS_FROM_BASE_ALPHA_DEFINED_LOCALLY
    #undef _GLOSSINESS_FROM_BASE_ALPHA
#endif

#if defined(_ALPHAMODULATE_ON_DEFINED_LOCALLY)
    #undef _ALPHAMODULATE_ON_DEFINED_LOCALLY
    #undef _ALPHAMODULATE_ON
#endif

#if defined(_NORMALMAP_DEFINED_LOCALLY)
    #undef _NORMALMAP_DEFINED_LOCALLY
    #undef _NORMALMAP
#endif

#if defined(_EMISSION_DEFINED_LOCALLY)
    #undef _EMISSION_DEFINED_LOCALLY
    #undef _EMISSION
#endif

#endif // UNIVERSAL_SIMPLE_LIT_INPUT_DEPRECATED_INCLUDED
