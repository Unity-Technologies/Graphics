#ifndef UNITY_SHADER_VARIABLES_FUNCTIONS_DEPRECATED_INCLUDED
#define UNITY_SHADER_VARIABLES_FUNCTIONS_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

// Deprecated: A confusingly named and duplicate function that scales clipspace to unity NDC range. (-w < x(-y) < w --> 0 < xy < w)
// Use GetVertexPositionInputs().positionNDC instead for vertex shader
// Or a similar function in Common.hlsl, ComputeNormalizedDeviceCoordinatesWithZ()
float4 ComputeScreenPos(float4 positionCS)
{
    float4 o = positionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = positionCS.zw;
    return o;
}

// Deprecated: Call 'firstbitlow' directly instead.
#define FIRST_BIT_LOW firstbitlow

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (ShaderVariablesFunctions.hlsl).

half3 ApplyAlphaModulate(half3 albedo, half alpha);
half3 ApplyAlphaPremultiply(half3 albedo, half alpha);
half3 NormalizeNormalPerPixel(half3 normalWS, bool normalMap);
float3 NormalizeNormalPerPixel(float3 normalWS, bool normalMap);

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

#if !defined(_NORMALMAP_KEYWORD_DECLARED)
    #if !defined(_NORMALMAP)
        #define _NORMALMAP 0
        #define _NORMALMAP_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_NORMALMAP)
        #undef _NORMALMAP
        #define _NORMALMAP 1
    #endif
#endif

// Deprecated. Gate ApplyAlphaModulate at the call site instead.
half3 AlphaModulate(half3 albedo, half alpha)
{
    return _ALPHAMODULATE_ON ? ApplyAlphaModulate(albedo, alpha) : albedo;
}

// Deprecated. Gate ApplyAlphaPremultiply at the call site instead.
half3 AlphaPremultiply(half3 albedo, half alpha)
{
    return _ALPHAPREMULTIPLY_ON ? ApplyAlphaPremultiply(albedo, alpha) : albedo;
}

// Deprecated. Use NormalizeNormalPerPixel(normalWS, normalMap).
half3 NormalizeNormalPerPixel(half3 normalWS)
{
    return NormalizeNormalPerPixel(normalWS, _NORMALMAP);
}

// Deprecated. Use NormalizeNormalPerPixel(normalWS, normalMap).
float3 NormalizeNormalPerPixel(float3 normalWS)
{
    return NormalizeNormalPerPixel(normalWS, _NORMALMAP);
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_ALPHAMODULATE_ON_DEFINED_LOCALLY)
    #undef _ALPHAMODULATE_ON_DEFINED_LOCALLY
    #undef _ALPHAMODULATE_ON
#endif

#if defined(_ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY)
    #undef _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY
    #undef _ALPHAPREMULTIPLY_ON
#endif

#if defined(_NORMALMAP_DEFINED_LOCALLY)
    #undef _NORMALMAP_DEFINED_LOCALLY
    #undef _NORMALMAP
#endif

#endif // UNITY_SHADER_VARIABLES_FUNCTIONS_DEPRECATED_INCLUDED
