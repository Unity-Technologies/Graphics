#ifndef UNIVERSAL_REALTIME_LIGHTS_DEPRECATED_INCLUDED
#define UNIVERSAL_REALTIME_LIGHTS_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusionFactor.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Light.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (RealtimeLights.hlsl).

Light GetMainLight(float4 shadowCoord, bool receiveShadows, bool surfaceTypeTransparent);
Light GetMainLight(float4 shadowCoord, float3 positionWS, half4 shadowMask, bool receiveShadows, bool surfaceTypeTransparent);
Light GetMainLight(InputData inputData, half4 shadowMask, AmbientOcclusionFactor aoFactor, bool receiveShadows, bool surfaceTypeTransparent);
Light GetAdditionalLight(uint i, float3 positionWS, half4 shadowMask, bool receiveShadows);
Light GetAdditionalLight(uint i, InputData inputData, half4 shadowMask, AmbientOcclusionFactor aoFactor, bool receiveShadows, bool surfaceTypeTransparent);

#if !defined(_SURFACE_TYPE_TRANSPARENT_KEYWORD_DECLARED)
    #if !defined(_SURFACE_TYPE_TRANSPARENT)
        #define _SURFACE_TYPE_TRANSPARENT 0
        #define _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SURFACE_TYPE_TRANSPARENT)
        #undef _SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT 1
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

// Deprecated. Use GetMainLight(shadowCoord, receiveShadows, surfaceTypeTransparent).
Light GetMainLight(float4 shadowCoord)
{
    return GetMainLight(shadowCoord, !_RECEIVE_SHADOWS_OFF, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated. Use GetMainLight(shadowCoord, positionWS, shadowMask, receiveShadows, surfaceTypeTransparent).
Light GetMainLight(float4 shadowCoord, float3 positionWS, half4 shadowMask)
{
    return GetMainLight(shadowCoord, positionWS, shadowMask, !_RECEIVE_SHADOWS_OFF, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated. Use GetMainLight(inputData, shadowMask, aoFactor, receiveShadows, surfaceTypeTransparent).
Light GetMainLight(InputData inputData, half4 shadowMask, AmbientOcclusionFactor aoFactor)
{
    return GetMainLight(inputData, shadowMask, aoFactor, !_RECEIVE_SHADOWS_OFF, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated. Use GetAdditionalLight(i, positionWS, shadowMask, receiveShadows).
Light GetAdditionalLight(uint i, float3 positionWS, half4 shadowMask)
{
    return GetAdditionalLight(i, positionWS, shadowMask, !_RECEIVE_SHADOWS_OFF);
}

// Deprecated. Use GetAdditionalLight(i, inputData, shadowMask, aoFactor, receiveShadows, surfaceTypeTransparent).
Light GetAdditionalLight(uint i, InputData inputData, half4 shadowMask, AmbientOcclusionFactor aoFactor)
{
    return GetAdditionalLight(i, inputData, shadowMask, aoFactor, !_RECEIVE_SHADOWS_OFF, _SURFACE_TYPE_TRANSPARENT);
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY)
    #undef _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY
    #undef _SURFACE_TYPE_TRANSPARENT
#endif

#if defined(_RECEIVE_SHADOWS_OFF_DEFINED_LOCALLY)
    #undef _RECEIVE_SHADOWS_OFF_DEFINED_LOCALLY
    #undef _RECEIVE_SHADOWS_OFF
#endif

#endif // UNIVERSAL_REALTIME_LIGHTS_DEPRECATED_INCLUDED
