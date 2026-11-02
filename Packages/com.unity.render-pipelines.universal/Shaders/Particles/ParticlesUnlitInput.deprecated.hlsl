#ifndef UNIVERSAL_PARTICLES_UNLIT_INPUT_DEPRECATED_INCLUDED
#define UNIVERSAL_PARTICLES_UNLIT_INPUT_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticleParams.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (ParticlesUnlitInput.hlsl).

half4 SampleAlbedo(float2 uv, float3 blendUv, half4 color, float4 particleColor, float4 projectedPosition, UnityTexture2D albedoMap, bool alphaPremultiplyEnabled, bool alphaModulateEnabled);
half4 SampleAlbedo(UnityTexture2D albedoMap, ParticleParams params, bool alphaPremultiplyEnabled, bool alphaModulateEnabled);

#if !defined(_ALPHAPREMULTIPLY_ON_KEYWORD_DECLARED)
    #if !defined(_ALPHAPREMULTIPLY_ON)
        #define _ALPHAPREMULTIPLY_ON 0
        #define _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_ALPHAPREMULTIPLY_ON)
        #undef _ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON 1
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

// Deprecated. Use SampleAlbedo(..., alphaPremultiplyEnabled, alphaModulateEnabled).
half4 SampleAlbedo(float2 uv, float3 blendUv, half4 color, float4 particleColor, float4 projectedPosition, UnityTexture2D albedoMap)
{
    return SampleAlbedo(uv, blendUv, color, particleColor, projectedPosition, albedoMap, _ALPHAPREMULTIPLY_ON, _ALPHAMODULATE_ON);
}

// Deprecated. Use SampleAlbedo(albedoMap, params, alphaPremultiplyEnabled, alphaModulateEnabled).
half4 SampleAlbedo(UnityTexture2D albedoMap, ParticleParams params)
{
    return SampleAlbedo(albedoMap, params, _ALPHAPREMULTIPLY_ON, _ALPHAMODULATE_ON);
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY)
    #undef _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY
    #undef _ALPHAPREMULTIPLY_ON
#endif

#if defined(_ALPHAMODULATE_ON_DEFINED_LOCALLY)
    #undef _ALPHAMODULATE_ON_DEFINED_LOCALLY
    #undef _ALPHAMODULATE_ON
#endif

#endif // UNIVERSAL_PARTICLES_UNLIT_INPUT_DEPRECATED_INCLUDED
