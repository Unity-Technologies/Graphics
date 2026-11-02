#ifndef UNIVERSAL_DBUFFER_DEPRECATED_INCLUDED
#define UNIVERSAL_DBUFFER_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (DBuffer.hlsl).

void ApplyDecalSpecular(float4 positionCS, inout half3 baseColor, inout half3 specularColor, inout half3 normalWS, inout half occlusion, inout half smoothness);
void ApplyDecalMetallic(float4 positionCS, inout half3 baseColor, inout half3 normalWS, inout half metallic, inout half occlusion, inout half smoothness);
void ApplyDecalToSurfaceData(float4 positionCS, inout SurfaceData surfaceData, inout InputData inputData, bool specularSetup);

#if !defined(_SPECULAR_SETUP_KEYWORD_DECLARED)
    #if !defined(_SPECULAR_SETUP)
        #define _SPECULAR_SETUP 0
        #define _SPECULAR_SETUP_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SPECULAR_SETUP)
        #undef _SPECULAR_SETUP
        #define _SPECULAR_SETUP 1
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

// Deprecated. Use ApplyDecalSpecular or ApplyDecalMetallic.
void ApplyDecal(float4 positionCS,
    inout half3 baseColor,
    inout half3 specularColor,
    inout half3 normalWS,
    inout half metallic,
    inout half occlusion,
    inout half smoothness)
{
    if (_SURFACE_TYPE_TRANSPARENT)
        return;

    if (_SPECULAR_SETUP)
        ApplyDecalSpecular(positionCS, baseColor, specularColor, normalWS, occlusion, smoothness);
    else
        ApplyDecalMetallic(positionCS, baseColor, normalWS, metallic, occlusion, smoothness);
}

// Deprecated. Use ApplyDecalToSurfaceData(positionCS, surfaceData, inputData, specularSetup).
void ApplyDecalToSurfaceData(float4 positionCS, inout SurfaceData surfaceData, inout InputData inputData)
{
    if (!_SURFACE_TYPE_TRANSPARENT)
        ApplyDecalToSurfaceData(positionCS, surfaceData, inputData, _SPECULAR_SETUP);
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_SPECULAR_SETUP_DEFINED_LOCALLY)
    #undef _SPECULAR_SETUP_DEFINED_LOCALLY
    #undef _SPECULAR_SETUP
#endif
#if defined(_SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY)
    #undef _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY
    #undef _SURFACE_TYPE_TRANSPARENT
#endif

#endif // UNIVERSAL_DBUFFER_DEPRECATED_INCLUDED
