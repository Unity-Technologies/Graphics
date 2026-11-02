#ifndef UNIVERSAL_PARTICLES_SIMPLE_LIT_INPUT_DEPRECATED_INCLUDED
#define UNIVERSAL_PARTICLES_SIMPLE_LIT_INPUT_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticleParams.hlsl"

// Deprecated keyword-reading helpers, kept for external back-compat.
// New code passes the keyword state as parameters (ParticlesSimpleLitInput.hlsl).

half4 SampleAlbedo(float2 uv, float3 blendUv, half4 color, float4 particleColor, float4 projectedPosition, UnityTexture2D albedoMap, bool alphaPremultiplyEnabled);
half4 SampleAlbedo(UnityTexture2D albedoMap, ParticleParams params, bool alphaPremultiplyEnabled);
half4 BlendTexture(UnityTexture2D _Texture, float2 uv, float3 blendUv);

#if !defined(_ALPHAPREMULTIPLY_ON_KEYWORD_DECLARED)
    #if !defined(_ALPHAPREMULTIPLY_ON)
        #define _ALPHAPREMULTIPLY_ON 0
        #define _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_ALPHAPREMULTIPLY_ON)
        #undef _ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON 1
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

#if !defined(_GLOSSINESS_FROM_BASE_ALPHA_KEYWORD_DECLARED)
    #if !defined(_GLOSSINESS_FROM_BASE_ALPHA)
        #define _GLOSSINESS_FROM_BASE_ALPHA 0
        #define _GLOSSINESS_FROM_BASE_ALPHA_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_GLOSSINESS_FROM_BASE_ALPHA)
        #undef _GLOSSINESS_FROM_BASE_ALPHA
        #define _GLOSSINESS_FROM_BASE_ALPHA 1
    #endif
#endif

// Deprecated. Use SampleAlbedo(..., alphaPremultiplyEnabled).
half4 SampleAlbedo(float2 uv, float3 blendUv, half4 color, float4 particleColor, float4 projectedPosition, UnityTexture2D albedoMap)
{
    return SampleAlbedo(uv, blendUv, color, particleColor, projectedPosition, albedoMap, _ALPHAPREMULTIPLY_ON);
}

// Deprecated. Use SampleAlbedo(albedoMap, params, alphaPremultiplyEnabled).
half4 SampleAlbedo(UnityTexture2D albedoMap, ParticleParams params)
{
    return SampleAlbedo(albedoMap, params, _ALPHAPREMULTIPLY_ON);
}

// Deprecated. Sample the specular gloss data at the call site; the caller knows the keyword state.
half4 SampleSpecularSmoothness(float2 uv, float3 blendUv, half alpha, half4 specColor, UnityTexture2D specGlossMap)
{
    half4 specularGloss = half4(0, 0, 0, 1);
    if (_SPECGLOSSMAP)
        specularGloss = BlendTexture(specGlossMap, uv, blendUv);
    else if (_SPECULAR_COLOR)
        specularGloss = specColor;

    if (_GLOSSINESS_FROM_BASE_ALPHA)
        specularGloss.a = alpha;

    return specularGloss;
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY)
    #undef _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY
    #undef _ALPHAPREMULTIPLY_ON
#endif

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

#endif // UNIVERSAL_PARTICLES_SIMPLE_LIT_INPUT_DEPRECATED_INCLUDED
