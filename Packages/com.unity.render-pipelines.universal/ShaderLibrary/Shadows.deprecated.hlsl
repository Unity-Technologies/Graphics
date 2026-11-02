#ifndef UNIVERSAL_SHADOWS_DEPRECATED_INCLUDED
#define UNIVERSAL_SHADOWS_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShadowSamplingData.hlsl"

// Deprecated: Reduce the number of unique samplers by using inline samplers instead.
// Some graphics APIs support only a low number of unique active samplers.
#define sampler_ScreenSpaceShadowmapTexture sampler_PointClamp
#define sampler_MainLightShadowmapTexture sampler_LinearClampCompare
#define sampler_AdditionalLightsShadowmapTexture sampler_LinearClampCompare

// Legacy: _MIXED_LIGHTING_SUBTRACTIVE was an alias for (LIGHTMAP_SHADOW_MIXING && !SHADOWS_SHADOWMASK).
// This runs before the Shadows.hlsl keyword fallbacks, so shaders that still declare it keep working.
#if !defined(LIGHTMAP_SHADOW_MIXING_KEYWORD_DECLARED) && !defined(SHADOWS_SHADOWMASK_KEYWORD_DECLARED)
    #if defined(_MIXED_LIGHTING_SUBTRACTIVE) && !defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK)
        #define LIGHTMAP_SHADOW_MIXING 1
    #endif
#endif

// Deprecated, callers should use MainLightShadowsAvailable(). The compile-time keyword tests
// stay inside their own keyword's _KEYWORD_DECLARED arms so they never restrict conversion.
#if (_MAIN_LIGHT_SHADOWS_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #if !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        #define MAIN_LIGHT_CALCULATE_SHADOWS 1
    #endif
#elif !defined(_MAIN_LIGHT_SHADOWS_KEYWORD_DECLARED) || (_MAIN_LIGHT_SHADOWS_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if defined(_MAIN_LIGHT_SHADOWS) && !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        #define MAIN_LIGHT_CALCULATE_SHADOWS 1
    #endif
#endif

#if (_MAIN_LIGHT_SHADOWS_CASCADE_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #if !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        #define MAIN_LIGHT_CALCULATE_SHADOWS 1
    #endif
#elif !defined(_MAIN_LIGHT_SHADOWS_CASCADE_KEYWORD_DECLARED) || (_MAIN_LIGHT_SHADOWS_CASCADE_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if defined(_MAIN_LIGHT_SHADOWS_CASCADE) && !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        #define MAIN_LIGHT_CALCULATE_SHADOWS 1
    #endif
#endif

#if (_MAIN_LIGHT_SHADOWS_SCREEN_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #if !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        #define MAIN_LIGHT_CALCULATE_SHADOWS 1
    #endif
#elif !defined(_MAIN_LIGHT_SHADOWS_SCREEN_KEYWORD_DECLARED) || (_MAIN_LIGHT_SHADOWS_SCREEN_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        #define MAIN_LIGHT_CALCULATE_SHADOWS 1
    #endif
#endif

// Deprecated, callers should use AdditionalLightShadowsAvailable().
#if (_ADDITIONAL_LIGHT_SHADOWS_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #define ADDITIONAL_LIGHT_CALCULATE_SHADOWS 1
#elif !defined(_ADDITIONAL_LIGHT_SHADOWS_KEYWORD_DECLARED) || (_ADDITIONAL_LIGHT_SHADOWS_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if defined(_ADDITIONAL_LIGHT_SHADOWS)
        #define ADDITIONAL_LIGHT_CALCULATE_SHADOWS 1
    #endif
#endif

// Deprecated. Baked shadow evaluation is always available; keywords have fallbacks.
#define CALCULATE_BAKED_SHADOWS 1

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (Shadows.hlsl).

float4 TransformWorldToShadowCoord(float3 positionWS, bool surfaceTypeTransparent);
half SampleMainLightRealtimeShadow(float4 shadowCoord, half4 shadowParams, ShadowSamplingData shadowSamplingData, bool surfaceTypeTransparent);
half SampleMainLightRealtimeShadow(float4 shadowCoord, bool surfaceTypeTransparent);
half SampleAdditionalLightRealtimeShadow(int lightIndex, float3 positionWS, half3 lightDirection, half4 shadowParams, ShadowSamplingData shadowSamplingData);
half SampleAdditionalLightRealtimeShadow(int lightIndex, float3 positionWS, half3 lightDirection);
half MainLightShadow(float4 shadowCoord, float3 positionWS, half4 shadowMask, half4 occlusionProbeChannels, bool receiveShadows, bool surfaceTypeTransparent);
half AdditionalLightShadow(int lightIndex, float3 positionWS, half3 lightDirection, half4 shadowMask, half4 occlusionProbeChannels, bool receiveShadows);
float4 GetShadowCoord(VertexPositionInputs vertexInput, bool surfaceTypeTransparent);
bool AdditionalLightShadowsAvailable();
half4 GetAdditionalLightShadowParams(int lightIndex);

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

// Deprecated. Use TransformWorldToShadowCoord(positionWS, surfaceTypeTransparent).
float4 TransformWorldToShadowCoord(float3 positionWS)
{
    return TransformWorldToShadowCoord(positionWS, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated. Gate SampleMainLightRealtimeShadow(shadowCoord, shadowParams, shadowSamplingData, surfaceTypeTransparent) at the call site instead.
half MainLightRealtimeShadow(float4 shadowCoord, half4 shadowParams, ShadowSamplingData shadowSamplingData)
{
    return _RECEIVE_SHADOWS_OFF ? half(1.0) : SampleMainLightRealtimeShadow(shadowCoord, shadowParams, shadowSamplingData, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated. Gate SampleMainLightRealtimeShadow(shadowCoord, surfaceTypeTransparent) at the call site instead.
half MainLightRealtimeShadow(float4 shadowCoord)
{
    return _RECEIVE_SHADOWS_OFF ? half(1.0) : SampleMainLightRealtimeShadow(shadowCoord, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated. Gate SampleAdditionalLightRealtimeShadow(lightIndex, positionWS, lightDirection, shadowParams, shadowSamplingData) at the call site instead.
half AdditionalLightRealtimeShadow(int lightIndex, float3 positionWS, half3 lightDirection, half4 shadowParams, ShadowSamplingData shadowSamplingData)
{
    return _RECEIVE_SHADOWS_OFF ? half(1.0) : SampleAdditionalLightRealtimeShadow(lightIndex, positionWS, lightDirection, shadowParams, shadowSamplingData);
}

// Deprecated. Gate SampleAdditionalLightRealtimeShadow(lightIndex, positionWS, lightDirection) at the call site instead.
half AdditionalLightRealtimeShadow(int lightIndex, float3 positionWS, half3 lightDirection)
{
    return _RECEIVE_SHADOWS_OFF ? half(1.0) : SampleAdditionalLightRealtimeShadow(lightIndex, positionWS, lightDirection);
}

// Deprecated: Use AdditionalLightRealtimeShadow(int lightIndex, float3 positionWS, half3 lightDirection) in Shadows.hlsl instead, as it supports Point Light shadows
half AdditionalLightRealtimeShadow(int lightIndex, float3 positionWS)
{
    return _RECEIVE_SHADOWS_OFF ? half(1.0) : SampleAdditionalLightRealtimeShadow(lightIndex, positionWS, half3(1, 0, 0));
}

// Deprecated. Use MainLightShadow(shadowCoord, positionWS, shadowMask, occlusionProbeChannels, receiveShadows, surfaceTypeTransparent).
half MainLightShadow(float4 shadowCoord, float3 positionWS, half4 shadowMask, half4 occlusionProbeChannels)
{
    return MainLightShadow(shadowCoord, positionWS, shadowMask, occlusionProbeChannels, !_RECEIVE_SHADOWS_OFF, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated. Use AdditionalLightShadow(lightIndex, positionWS, lightDirection, shadowMask, occlusionProbeChannels, receiveShadows).
half AdditionalLightShadow(int lightIndex, float3 positionWS, half3 lightDirection, half4 shadowMask, half4 occlusionProbeChannels)
{
    return AdditionalLightShadow(lightIndex, positionWS, lightDirection, shadowMask, occlusionProbeChannels, !_RECEIVE_SHADOWS_OFF);
}

// Deprecated. Use GetShadowCoord(vertexInput, surfaceTypeTransparent).
float4 GetShadowCoord(VertexPositionInputs vertexInput)
{
    return GetShadowCoord(vertexInput, _SURFACE_TYPE_TRANSPARENT);
}

// Deprecated: Use GetAdditionalLightShadowParams instead.
half GetAdditionalLightShadowStrenth(int lightIndex)
{
    if (!AdditionalLightShadowsAvailable() || _RECEIVE_SHADOWS_OFF)
        return half(1.0);

    return GetAdditionalLightShadowParams(lightIndex).x;
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

#endif // UNIVERSAL_SHADOWS_DEPRECATED_INCLUDED
