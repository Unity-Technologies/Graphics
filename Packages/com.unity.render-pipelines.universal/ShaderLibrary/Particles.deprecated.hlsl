#ifndef UNIVERSAL_PARTICLES_DEPRECATED_INCLUDED
#define UNIVERSAL_PARTICLES_DEPRECATED_INCLUDED

#include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Particles/Particles.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.deprecated.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"

// Deprecated pre-multiplied alpha helper macros; must precede the local keyword fallbacks
// below so defined() sees the raw keyword state. New code uses the function forms.
#if defined(_ALPHAPREMULTIPLY_ON)
    #define ALBEDO_MUL albedo
#else
    #define ALBEDO_MUL albedo.a
#endif

#if defined(_ALPHAPREMULTIPLY_ON)
    #define SOFT_PARTICLE_MUL_ALBEDO(albedo, val) albedo * val
#elif defined(_ALPHAMODULATE_ON)
    #define SOFT_PARTICLE_MUL_ALBEDO(albedo, val) half4(lerp(half3(1.0, 1.0, 1.0), albedo.rgb, albedo.a * val), albedo.a * val)
#else
    #define SOFT_PARTICLE_MUL_ALBEDO(albedo, val) albedo * half4(1.0, 1.0, 1.0, val)
#endif

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (Shaders/Particles/Particles.hlsl).

#if !defined(_NORMALMAP_KEYWORD_DECLARED)
    #if !defined(_NORMALMAP)
        #define _NORMALMAP 0
        #define _NORMALMAP_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_NORMALMAP)
        #undef _NORMALMAP
        #define _NORMALMAP 1
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

#if !defined(_ALPHAPREMULTIPLY_ON_KEYWORD_DECLARED)
    #if !defined(_ALPHAPREMULTIPLY_ON)
        #define _ALPHAPREMULTIPLY_ON 0
        #define _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_ALPHAPREMULTIPLY_ON)
        #undef _ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON 1
    #endif
#endif

// Deprecated. Use AlphaModulateAndPremultiply(albedo, alpha, alphaModulateEnabled) gated on either blend mode being enabled.
half3 AlphaModulateAndPremultiply(half3 albedo, half alpha)
{
    half3 result = albedo;
    if (_ALPHAMODULATE_ON || _ALPHAPREMULTIPLY_ON)
        result = AlphaModulateAndPremultiply(albedo, alpha, _ALPHAMODULATE_ON);
    return result;
}

// Deprecated. Gate SampleParticleNormalTS at the call site instead.
half3 SampleNormalTS(float2 uv, float3 blendUv, UnityTexture2D bumpMap, half scale = half(1.0))
{
    return _NORMALMAP ? SampleParticleNormalTS(uv, blendUv, bumpMap, scale) : half3(0.0, 0.0, 1.0);
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_NORMALMAP_DEFINED_LOCALLY)
    #undef _NORMALMAP_DEFINED_LOCALLY
    #undef _NORMALMAP
#endif

#if defined(_ALPHAMODULATE_ON_DEFINED_LOCALLY)
    #undef _ALPHAMODULATE_ON_DEFINED_LOCALLY
    #undef _ALPHAMODULATE_ON
#endif

#if defined(_ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY)
    #undef _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY
    #undef _ALPHAPREMULTIPLY_ON
#endif

#endif // UNIVERSAL_PARTICLES_DEPRECATED_INCLUDED
