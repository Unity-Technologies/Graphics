#ifndef LIGHTWEIGHT_SHADOWS_INCLUDED
#define LIGHTWEIGHT_SHADOWS_INCLUDED

#include "LightweightInput.cginc"

#define MAX_SHADOW_CASCADES 4

#if defined(_HARD_SHADOWS) || defined(_SOFT_SHADOWS) || defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOWS
#endif

#if defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOW_CASCADES
#endif

#ifdef _SHADOWS
#define LIGHTWEIGHT_SHADOW_ATTENUATION(posWorld, vertexNormal, shadowDir) ComputeShadowAttenuation(posWorld, vertexNormal, shadowDir)
#else
#define LIGHTWEIGHT_SHADOW_ATTENUATION(posWorld, vertexNormal, shadowDir) 1.0h
#endif

sampler2D_float _ShadowMap;
float _PCFKernel[8];
float4x4 _WorldToShadow[MAX_SHADOW_CASCADES];
float4 _DirShadowSplitSpheres[MAX_SHADOW_CASCADES];
half4 _ShadowData;

inline half ShadowAttenuation(float3 shadowCoord)
{
    if (shadowCoord.x <= 0 || shadowCoord.x >= 1 || shadowCoord.y <= 0 || shadowCoord.y >= 1)
        return 1;

    float depth = tex2D(_ShadowMap, shadowCoord).r;

#if defined(UNITY_REVERSED_Z)
    return step(depth - _ShadowData.y, shadowCoord.z);
#else
    return step(shadowCoord.z, depth + _ShadowData.y);
#endif
}

inline half ComputeCascadeIndex(float3 wpos)
{
    float3 fromCenter0 = wpos.xyz - _DirShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = wpos.xyz - _DirShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = wpos.xyz - _DirShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = wpos.xyz - _DirShadowSplitSpheres[3].xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    float4 vDirShadowSplitSphereSqRadii;
    vDirShadowSplitSphereSqRadii.x = _DirShadowSplitSpheres[0].w;
    vDirShadowSplitSphereSqRadii.y = _DirShadowSplitSpheres[1].w;
    vDirShadowSplitSphereSqRadii.z = _DirShadowSplitSpheres[2].w;
    vDirShadowSplitSphereSqRadii.w = _DirShadowSplitSpheres[3].w;
    fixed4 weights = fixed4(distances2 < vDirShadowSplitSphereSqRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);
    return 4 - dot(weights, fixed4(4, 3, 2, 1));
}

inline half ShadowPCF(half3 shadowCoord)
{
    // TODO: simulate textureGatherOffset not available, simulate it
    half2 offset = half2(0, 0);
    half attenuation = ShadowAttenuation(half3(shadowCoord.xy + half2(_PCFKernel[0], _PCFKernel[1]) + offset, shadowCoord.z)) +
        ShadowAttenuation(half3(shadowCoord.xy + half2(_PCFKernel[2], _PCFKernel[3]) + offset, shadowCoord.z)) +
        ShadowAttenuation(half3(shadowCoord.xy + half2(_PCFKernel[4], _PCFKernel[5]) + offset, shadowCoord.z)) +
        ShadowAttenuation(half3(shadowCoord.xy + half2(_PCFKernel[6], _PCFKernel[7]) + offset, shadowCoord.z));
    return attenuation * 0.25;
}

inline half ComputeShadowAttenuation(float3 posWorld, half3 vertexNormal, half3 shadowDir)
{
    half NdotL = dot(vertexNormal, shadowDir);
    half bias = saturate(1.0 - NdotL) * _ShadowData.z;

    float3 posWorldOffsetNormal = posWorld + vertexNormal * bias;

    int cascadeIndex = 0;
#ifdef _SHADOW_CASCADES
    cascadeIndex = ComputeCascadeIndex(posWorldOffsetNormal);
    if (cascadeIndex >= MAX_SHADOW_CASCADES)
        return 1.0;
#endif
    float4 shadowCoord = mul(_WorldToShadow[cascadeIndex], float4(posWorldOffsetNormal, 1.0));
    shadowCoord.xyz /= shadowCoord.w;
    shadowCoord.z = saturate(shadowCoord.z);

#if defined(_SOFT_SHADOWS) || defined(_SOFT_SHADOWS_CASCADES)
    return ShadowPCF(shadowCoord.xyz);
#else
    return ShadowAttenuation(shadowCoord.xyz);
#endif
}

half MixRealtimeAndBakedOcclusion(half realtimeAttenuation, half4 bakedOcclusion, half4 distanceAttenuation)
{
#if defined(LIGHTMAP_ON)
#if defined(_MIXED_LIGHTING_SHADOWMASK)
    // TODO:
#elif defined(_MIXED_LIGHTING_SUBTRACTIVE)
    // Subtractive Light mode has direct light contribution baked into lightmap for mixed lights.
    // We need to remove direct realtime contribution from mixed lights
    // distanceAttenuation.w is set 0.0 if this light is mixed, 1.0 otherwise.
    return realtimeAttenuation * distanceAttenuation.w;
#endif
#endif

    return realtimeAttenuation;
}

inline half3 SubtractDirectMainLightFromLightmap(half3 lightmap, half attenuation, half3 lambert)
{
    // Let's try to make realtime shadows work on a surface, which already contains
    // baked lighting and shadowing from the main sun light.
    // Summary:
    // 1) Calculate possible value in the shadow by subtracting estimated light contribution from the places occluded by realtime shadow:
    //      a) preserves other baked lights and light bounces
    //      b) eliminates shadows on the geometry facing away from the light
    // 2) Clamp against user defined ShadowColor.
    // 3) Pick original lightmap value, if it is the darkest one.


    // 1) Gives good estimate of illumination as if light would've been shadowed during the bake.
    //    Preserves bounce and other baked lights
    //    No shadows on the geometry facing away from the light
    half shadowStrength = _ShadowData.x;
    half3 estimatedLightContributionMaskedByInverseOfShadow = lambert * (1.0 - attenuation);
    half3 subtractedLightmap = lightmap - estimatedLightContributionMaskedByInverseOfShadow;

    // 2) Allows user to define overall ambient of the scene and control situation when realtime shadow becomes too dark.
    half3 realtimeShadow = max(subtractedLightmap, _SubtractiveShadowColor.xyz);
    realtimeShadow = lerp(realtimeShadow, lightmap, shadowStrength);

    // 3) Pick darkest color
    return min(lightmap, realtimeShadow);
}

#endif
