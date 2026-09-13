#ifndef AMBIENT_OCCLUSION_DEPRECATED_INCLUDED
#define AMBIENT_OCCLUSION_DEPRECATED_INCLUDED

// 2023.3 Deprecated. This is for backwards compatibility. Remove in the future.
#define sampler_ScreenSpaceOcclusionTexture sampler_LinearClamp

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusionFactor.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (AmbientOcclusion.hlsl).

AmbientOcclusionFactor GetScreenSpaceAmbientOcclusion(float2 normalizedScreenSpaceUV, bool surfaceTypeTransparent);
AmbientOcclusionFactor CreateAmbientOcclusionFactor(float2 normalizedScreenSpaceUV, half occlusion, bool surfaceTypeTransparent);
AmbientOcclusionFactor CreateAmbientOcclusionFactor(InputData inputData, SurfaceData surfaceData, bool surfaceTypeTransparent);

#if !defined(_SURFACE_TYPE_TRANSPARENT_KEYWORD_DECLARED)
    #if !defined(_SURFACE_TYPE_TRANSPARENT)
        #define _SURFACE_TYPE_TRANSPARENT 0
        #define _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SURFACE_TYPE_TRANSPARENT)
        #undef _SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT 1
    #endif
#endif

// Deprecated. Use GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV, surfaceTypeTransparent).
AmbientOcclusionFactor GetScreenSpaceAmbientOcclusion(float2 normalizedScreenSpaceUV)
{
    return GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated. Use CreateAmbientOcclusionFactor(normalizedScreenSpaceUV, occlusion, surfaceTypeTransparent).
AmbientOcclusionFactor CreateAmbientOcclusionFactor(float2 normalizedScreenSpaceUV, half occlusion)
{
    return CreateAmbientOcclusionFactor(normalizedScreenSpaceUV, occlusion, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated. Use CreateAmbientOcclusionFactor(inputData, surfaceData, surfaceTypeTransparent).
AmbientOcclusionFactor CreateAmbientOcclusionFactor(InputData inputData, SurfaceData surfaceData)
{
    return CreateAmbientOcclusionFactor(inputData, surfaceData, _SURFACE_TYPE_TRANSPARENT);
}

// Prevents leaking the fallback keyword definition to shaders that include this file.
#if defined(_SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY)
    #undef _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY
    #undef _SURFACE_TYPE_TRANSPARENT
#endif

#endif // AMBIENT_OCCLUSION_DEPRECATED_INCLUDED
