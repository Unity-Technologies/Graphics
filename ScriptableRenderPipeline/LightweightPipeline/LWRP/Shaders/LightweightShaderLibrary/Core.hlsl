#ifndef LIGHTWEIGHT_PIPELINE_CORE_INCLUDED
#define LIGHTWEIGHT_PIPELINE_CORE_INCLUDED

#include "CoreRP/ShaderLibrary/Common.hlsl"
#include "CoreRP/ShaderLibrary/Packing.hlsl"
#include "Input.hlsl"

#ifdef _NORMALMAP
    #define OUTPUT_NORMAL(IN, OUT) OutputTangentToWorld(IN.tangent, IN.normal, OUT.tangent, OUT.binormal, OUT.normal)
#else
    #define OUTPUT_NORMAL(IN, OUT) OUT.normal = TransformObjectToWorldNormal(IN.normal)
#endif

#if defined(UNITY_REVERSED_Z)
    #if UNITY_REVERSED_Z == 1
    //D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
    //max is required to protect ourselves from near plane not being correct/meaningfull in case of oblique matrices.
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
#else
    //GL with reversed z => z clip range is [near, -far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(-(coord), 0)
    #endif
#elif UNITY_UV_STARTS_AT_TOP
    //D3d without reversed z => z clip range is [0, far] -> nothing to do
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
    //Opengl => z clip range is [-near, far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#endif

half Pow4(half x)
{
    return (x * x) * (x * x);
}

void AlphaDiscard(half alpha, half cutoff)
{
#ifdef _ALPHATEST_ON
    clip(alpha - cutoff);
#endif
}

half3 UnpackNormal(half4 packedNormal)
{
    // Compiler will optimize the scale away
#if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(packedNormal, 1.0);
#else
    return UnpackNormalmapRGorAG(packedNormal, 1.0);
#endif
}

half3 UnpackNormalScale(half4 packedNormal, half bumpScale)
{
#if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(packedNormal, bumpScale);
#else
    return UnpackNormalmapRGorAG(packedNormal, bumpScale);
#endif
}

void OutputTangentToWorld(half4 vertexTangent, half3 vertexNormal, out half3 tangentWS, out half3 binormalWS, out half3 normalWS)
{
    half sign = vertexTangent.w * GetOddNegativeScale();
    normalWS = TransformObjectToWorldNormal(vertexNormal);
    tangentWS = normalize(mul((half3x3)unity_ObjectToWorld, vertexTangent.xyz));
    binormalWS = cross(normalWS, tangentWS) * sign;
}

half3 TangentToWorldNormal(half3 normalTangent, half3 tangent, half3 binormal, half3 normal)
{
    half3x3 tangentToWorld = half3x3(tangent, binormal, normal);
    return normalize(mul(normalTangent, tangentToWorld));
}

float ComputeFogFactor(float z)
{
    float clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(z);

#if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    float fogFactor = saturate(clipZ_01 * unity_FogParams.z + unity_FogParams.w);
    return half(fogFactor);
#elif defined(FOG_EXP)
    // factor = exp(-density*z)
    float unityFogFactor = unity_FogParams.y * clipZ_01;
    return half(saturate(exp2(-unityFogFactor)));
#elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    float unityFogFactor = unity_FogParams.x * clipZ_01;
    return half(saturate(exp2(-unityFogFactor*unityFogFactor)));
#else
    return 0.0h;
#endif
}

void ApplyFog(inout half3 color, half fogFactor)
{
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    color = lerp(unity_FogColor, color, fogFactor);
#endif
}
#endif
