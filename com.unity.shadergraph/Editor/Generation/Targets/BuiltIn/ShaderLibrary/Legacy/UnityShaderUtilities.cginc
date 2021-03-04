#ifndef UNITY_SHADER_UTILITIES_INCLUDED
#define UNITY_SHADER_UTILITIES_INCLUDED

// This file is always included in all unity shaders.

#include "UnityShaderVariables.cginc"

float3 ODSOffset(float3 worldPos, float ipd)
{
    //based on google's omni-directional stereo rendering thread
    const float EPSILON = 2.4414e-4;
    float3 worldUp = float3(0.0, 1.0, 0.0);
    float3 camOffset = worldPos.xyz - _WorldSpaceCameraPos.xyz;
    float4 direction = float4(camOffset.xyz, dot(camOffset.xyz, camOffset.xyz));
    direction.w = max(EPSILON, direction.w);
    direction *= rsqrt(direction.w);

    float3 tangent = cross(direction.xyz, worldUp.xyz);
    if (dot(tangent, tangent) < EPSILON)
        return float3(0, 0, 0);
    tangent = normalize(tangent);

    float directionMinusIPD = max(EPSILON, direction.w*direction.w - ipd*ipd);
    float a = ipd * ipd / direction.w;
    float b = ipd / direction.w * sqrt(directionMinusIPD);
    float3 offset = -a * direction.xyz + b * tangent;
    return offset;
}

inline float4 UnityObjectToClipPosODS(float3 inPos)
{
    float4 clipPos;
    float3 posWorld = mul(unity_ObjectToWorld, float4(inPos, 1.0)).xyz;
#if defined(STEREO_CUBEMAP_RENDER_ON)
    float3 offset = ODSOffset(posWorld, unity_HalfStereoSeparation.x);
    clipPos = mul(UNITY_MATRIX_VP, float4(posWorld + offset, 1.0));
#else
    clipPos = mul(UNITY_MATRIX_VP, float4(posWorld, 1.0));
#endif
    return clipPos;
}

// Tranforms position from object to homogenous space
inline float4 UnityObjectToClipPos(in float3 pos)
{
#if defined(STEREO_CUBEMAP_RENDER_ON)
    return UnityObjectToClipPosODS(pos);
#else
    // More efficient than computing M*VP matrix product
    return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 1.0)));
#endif
}
inline float4 UnityObjectToClipPos(float4 pos) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return UnityObjectToClipPos(pos.xyz);
}

#endif
