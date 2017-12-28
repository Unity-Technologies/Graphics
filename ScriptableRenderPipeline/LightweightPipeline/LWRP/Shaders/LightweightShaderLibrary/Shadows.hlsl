#ifndef LIGHTWEIGHT_SHADOWS_INCLUDED
#define LIGHTWEIGHT_SHADOWS_INCLUDED

#include "CoreRP/ShaderLibrary/Common.hlsl"

#define MAX_SHADOW_CASCADES 4

///////////////////////////////////////////////////////////////////////////////
// Light Classification shadow defines                                       //
//                                                                           //
// In order to reduce shader variations main light keywords were combined    //
// here we define shadow keywords.                                           //
///////////////////////////////////////////////////////////////////////////////
#if defined(_MAIN_LIGHT_DIRECTIONAL_SHADOW) || defined(_MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE) || defined(_MAIN_LIGHT_DIRECTIONAL_SHADOW_SOFT) || defined(_MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE_SOFT) || defined(_MAIN_LIGHT_SPOT_SHADOW) || defined(_MAIN_LIGHT_SPOT_SHADOW_SOFT)
    #define _SHADOWS_ENABLED
#endif

#if defined(_MAIN_LIGHT_DIRECTIONAL_SHADOW_SOFT) || defined(_MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE_SOFT) || defined(_MAIN_LIGHT_SPOT_SHADOW_SOFT)
    #define _SHADOWS_SOFT
#endif

#if defined(_MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE) || defined(_MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE_SOFT)
    #define _SHADOWS_CASCADE
#endif

#if defined(_MAIN_LIGHT_SPOT_SHADOW) || defined(_MAIN_LIGHT_SPOT_SHADOW_SOFT)
    #define _SHADOWS_PERSPECTIVE
#endif

TEXTURE2D_SHADOW(_ShadowMap);
SAMPLER_CMP(sampler_ShadowMap);

CBUFFER_START(_ShadowBuffer)
// Last cascade is initialized with a no-op matrix. It always transforms
// shadow coord to half(0, 0, NEAR_PLANE). We use this trick to avoid
// branching since ComputeCascadeIndex can return cascade index = MAX_SHADOW_CASCADES
float4x4 _WorldToShadow[MAX_SHADOW_CASCADES + 1];
float4 _DirShadowSplitSpheres[MAX_SHADOW_CASCADES];
float4 _DirShadowSplitSphereRadii;
half4 _ShadowOffset0;
half4 _ShadowOffset1;
half4 _ShadowOffset2;
half4 _ShadowOffset3;
half4 _ShadowData; // (x: shadowStrength)
CBUFFER_END

inline half SampleShadowmap(float4 shadowCoord)
{
#if defined(_SHADOWS_PERSPECTIVE)
    shadowCoord.xyz = shadowCoord.xyz /= shadowCoord.w;
#endif

#ifdef _SHADOWS_SOFT
    // 4-tap hardware comparison
    half4 attenuation4;
    attenuation4.x = SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, shadowCoord.xyz + _ShadowOffset0.xyz);
    attenuation4.y = SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, shadowCoord.xyz + _ShadowOffset1.xyz);
    attenuation4.z = SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, shadowCoord.xyz + _ShadowOffset2.xyz);
    attenuation4.w = SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, shadowCoord.xyz + _ShadowOffset3.xyz);
    half attenuation = dot(attenuation4, 0.25);
#else
    // 1-tap hardware comparison
    half attenuation = SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, shadowCoord.xyz);
#endif

    // Apply shadow strength
    attenuation = LerpWhiteTo(attenuation, _ShadowData.x);

    // Shadow coords that fall out of the light frustum volume must always return attenuation 1.0
    // TODO: We can set shadowmap sampler to clamptoborder when we don't have a shadow atlas and avoid xy coord bounds check
    return (shadowCoord.x <= 0 || shadowCoord.x >= 1 || shadowCoord.y <= 0 || shadowCoord.y >= 1 || shadowCoord.z >= 1) ? 1.0 : attenuation;
}

inline half ComputeCascadeIndex(float3 wpos)
{
    // TODO: profile if there's a performance improvement if we avoid indexing here
    float3 fromCenter0 = wpos.xyz - _DirShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = wpos.xyz - _DirShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = wpos.xyz - _DirShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = wpos.xyz - _DirShadowSplitSpheres[3].xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    half4 weights = half4(distances2 < _DirShadowSplitSphereRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);

    return 4 - dot(weights, half4(4, 3, 2, 1));
}

inline float4 ComputeShadowCoord(float3 positionWS, half cascadeIndex = 0)
{
#ifdef _SHADOWS_CASCADE
    return mul(_WorldToShadow[cascadeIndex], float4(positionWS, 1.0));
#endif

    return mul(_WorldToShadow[0], float4(positionWS, 1.0));
}

inline half RealtimeShadowAttenuation(float3 positionWS)
{
#if !defined(_SHADOWS_ENABLED)
    return 1.0;
#endif

    half cascadeIndex = ComputeCascadeIndex(positionWS);
    float4 shadowCoord = ComputeShadowCoord(positionWS, cascadeIndex);
    return SampleShadowmap(shadowCoord);
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
    realtimeShadow = lerp(lightmap, realtimeShadow, shadowStrength);

    // 3) Pick darkest color
    return min(lightmap, realtimeShadow);
}

#endif
