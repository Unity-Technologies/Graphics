#ifndef UNIVERSAL_PARTICLES_LIT_INPUT_DEPRECATED_INCLUDED
#define UNIVERSAL_PARTICLES_LIT_INPUT_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticleParams.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (ParticlesLitInput.hlsl).

half4 SampleAlbedo(float2 uv, float3 blendUv, half4 color, float4 particleColor, float4 projectedPosition, UnityTexture2D albedoMap, bool alphaPremultiplyEnabled);
half4 SampleAlbedo(UnityTexture2D albedoMap, ParticleParams params, bool alphaPremultiplyEnabled);
void InitializeParticleLitSurfaceData(float2 uv, float3 blendUv, float4 particleColor, float4 projectedPosition, out SurfaceData outSurfaceData, bool alphaPremultiplyEnabled, bool metallicSpecGlossMap, bool emission, bool normalMap, bool alphaModulateEnabled);
void InitializeParticleLitSurfaceData(ParticleParams params, out SurfaceData outSurfaceData, bool alphaPremultiplyEnabled, bool metallicSpecGlossMap, bool emission, bool normalMap, bool alphaModulateEnabled);

#if !defined(_ALPHAPREMULTIPLY_ON_KEYWORD_DECLARED)
    #if !defined(_ALPHAPREMULTIPLY_ON)
        #define _ALPHAPREMULTIPLY_ON 0
        #define _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_ALPHAPREMULTIPLY_ON)
        #undef _ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON 1
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

#if !defined(_METALLICSPECGLOSSMAP_KEYWORD_DECLARED)
    #if !defined(_METALLICSPECGLOSSMAP)
        #define _METALLICSPECGLOSSMAP 0
        #define _METALLICSPECGLOSSMAP_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_METALLICSPECGLOSSMAP)
        #undef _METALLICSPECGLOSSMAP
        #define _METALLICSPECGLOSSMAP 1
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

#if !defined(_ALPHAMODULATE_ON_KEYWORD_DECLARED)
    #if !defined(_ALPHAMODULATE_ON)
        #define _ALPHAMODULATE_ON 0
        #define _ALPHAMODULATE_ON_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_ALPHAMODULATE_ON)
        #undef _ALPHAMODULATE_ON
        #define _ALPHAMODULATE_ON 1
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

// Deprecated. Use InitializeParticleLitSurfaceData(..., alphaPremultiplyEnabled, metallicSpecGlossMap, emission, normalMap, alphaModulateEnabled).
inline void InitializeParticleLitSurfaceData(float2 uv, float3 blendUv, float4 particleColor, float4 projectedPosition, out SurfaceData outSurfaceData)
{
    InitializeParticleLitSurfaceData(uv, blendUv, particleColor, projectedPosition, outSurfaceData, _ALPHAPREMULTIPLY_ON, _METALLICSPECGLOSSMAP, _EMISSION, _NORMALMAP, _ALPHAMODULATE_ON);
}

// Deprecated. Use InitializeParticleLitSurfaceData(params, outSurfaceData, alphaPremultiplyEnabled, metallicSpecGlossMap, emission, normalMap, alphaModulateEnabled).
inline void InitializeParticleLitSurfaceData(ParticleParams params, out SurfaceData outSurfaceData)
{
    InitializeParticleLitSurfaceData(params, outSurfaceData, _ALPHAPREMULTIPLY_ON, _METALLICSPECGLOSSMAP, _EMISSION, _NORMALMAP, _ALPHAMODULATE_ON);
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY)
    #undef _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY
    #undef _ALPHAPREMULTIPLY_ON
#endif

#if defined(_EMISSION_DEFINED_LOCALLY)
    #undef _EMISSION_DEFINED_LOCALLY
    #undef _EMISSION
#endif

#if defined(_METALLICSPECGLOSSMAP_DEFINED_LOCALLY)
    #undef _METALLICSPECGLOSSMAP_DEFINED_LOCALLY
    #undef _METALLICSPECGLOSSMAP
#endif

#if defined(_NORMALMAP_DEFINED_LOCALLY)
    #undef _NORMALMAP_DEFINED_LOCALLY
    #undef _NORMALMAP
#endif

#if defined(_ALPHAMODULATE_ON_DEFINED_LOCALLY)
    #undef _ALPHAMODULATE_ON_DEFINED_LOCALLY
    #undef _ALPHAMODULATE_ON
#endif

#endif // UNIVERSAL_PARTICLES_LIT_INPUT_DEPRECATED_INCLUDED
