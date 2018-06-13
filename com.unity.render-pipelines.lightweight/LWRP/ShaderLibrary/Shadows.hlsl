#ifndef LIGHTWEIGHT_SHADOWS_INCLUDED
#define LIGHTWEIGHT_SHADOWS_INCLUDED

#include "CoreRP/ShaderLibrary/Common.hlsl"
#include "CoreRP/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#include "Core.hlsl"

#define MAX_SHADOW_CASCADES 4

#ifndef SHADOWS_SCREEN
#if defined(_SHADOWS_ENABLED) && defined(_SHADOWS_CASCADE) && !defined(SHADER_API_GLES)
#define SHADOWS_SCREEN 1
#else
#define SHADOWS_SCREEN 0
#endif
#endif

SCREENSPACE_TEXTURE(_ScreenSpaceShadowMapTexture);
SAMPLER(sampler_ScreenSpaceShadowMapTexture);

TEXTURE2D_SHADOW(_DirectionalShadowmapTexture);
SAMPLER_CMP(sampler_DirectionalShadowmapTexture);

TEXTURE2D_SHADOW(_LocalShadowmapTexture);
SAMPLER_CMP(sampler_LocalShadowmapTexture);

CBUFFER_START(_DirectionalShadowBuffer)
// Last cascade is initialized with a no-op matrix. It always transforms
// shadow coord to half(0, 0, NEAR_PLANE). We use this trick to avoid
// branching since ComputeCascadeIndex can return cascade index = MAX_SHADOW_CASCADES
float4x4    _WorldToShadow[MAX_SHADOW_CASCADES + 1];
float4      _DirShadowSplitSpheres[MAX_SHADOW_CASCADES];
float4      _DirShadowSplitSphereRadii;
half4       _ShadowOffset0;
half4       _ShadowOffset1;
half4       _ShadowOffset2;
half4       _ShadowOffset3;
half4       _ShadowData;    // (x: shadowStrength)
float4      _ShadowmapSize; // (xy: 1/width and 1/height, zw: width and height)
CBUFFER_END

CBUFFER_START(_LocalShadowBuffer)
float4x4    _LocalWorldToShadowAtlas[MAX_VISIBLE_LIGHTS];
half        _LocalShadowStrength[MAX_VISIBLE_LIGHTS];
half4       _LocalShadowOffset0;
half4       _LocalShadowOffset1;
half4       _LocalShadowOffset2;
half4       _LocalShadowOffset3;
float4      _LocalShadowmapSize; // (xy: 1/width and 1/height, zw: width and height)
CBUFFER_END

#if UNITY_REVERSED_Z
#define BEYOND_SHADOW_FAR(shadowCoord) shadowCoord.z <= UNITY_RAW_FAR_CLIP_VALUE
#else
#define BEYOND_SHADOW_FAR(shadowCoord) shadowCoord.z >= UNITY_RAW_FAR_CLIP_VALUE
#endif

struct ShadowSamplingData
{
    half4 shadowOffset0;
    half4 shadowOffset1;
    half4 shadowOffset2;
    half4 shadowOffset3;
    float4 shadowmapSize;
};

ShadowSamplingData GetMainLightShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;
    shadowSamplingData.shadowOffset0 = _ShadowOffset0;
    shadowSamplingData.shadowOffset1 = _ShadowOffset1;
    shadowSamplingData.shadowOffset2 = _ShadowOffset2;
    shadowSamplingData.shadowOffset3 = _ShadowOffset3;
    shadowSamplingData.shadowmapSize = _ShadowmapSize;
    return shadowSamplingData;
}

ShadowSamplingData GetLocalLightShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;
    shadowSamplingData.shadowOffset0 = _LocalShadowOffset0;
    shadowSamplingData.shadowOffset1 = _LocalShadowOffset1;
    shadowSamplingData.shadowOffset2 = _LocalShadowOffset2;
    shadowSamplingData.shadowOffset3 = _LocalShadowOffset3;
    shadowSamplingData.shadowmapSize = _LocalShadowmapSize;
    return shadowSamplingData;
}

half GetMainLightShadowStrength()
{
    return _ShadowData.x;
}

half GetLocalLightShadowStrenth(int lightIndex)
{
    return _LocalShadowStrength[lightIndex];
}

half SampleScreenSpaceShadowMap(float4 shadowCoord)
{
    shadowCoord.xy /= shadowCoord.w;

    // The stereo transform has to happen after the manual perspective divide
    shadowCoord.xy = UnityStereoTransformScreenSpaceTex(shadowCoord.xy);

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    half attenuation = SAMPLE_TEXTURE2D_ARRAY(_ScreenSpaceShadowMapTexture, sampler_ScreenSpaceShadowMapTexture, shadowCoord.xy, unity_StereoEyeIndex).x;
#else
    half attenuation = SAMPLE_TEXTURE2D(_ScreenSpaceShadowMapTexture, sampler_ScreenSpaceShadowMapTexture, shadowCoord.xy).x;
#endif

    return attenuation;
}

real SampleShadowmap(float4 shadowCoord, TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), ShadowSamplingData samplingData, half shadowStrength)
{
    shadowCoord.xyz /= shadowCoord.w;

    real attenuation;

#ifdef _SHADOWS_SOFT
    #ifdef SHADER_API_MOBILE
        // 4-tap hardware comparison
        real4 attenuation4;
        attenuation4.x = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset0.xyz);
        attenuation4.y = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset1.xyz);
        attenuation4.z = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset2.xyz);
        attenuation4.w = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset3.xyz);
        attenuation = dot(attenuation4, 0.25);
    #else
        #ifdef _SHADOWS_CASCADE //Assume screen space shadows when cascades enabled
            float fetchesWeights[16];
            float2 fetchesUV[16];
            SampleShadow_ComputeSamples_Tent_7x7(samplingData.shadowmapSize, shadowCoord.xy, fetchesWeights, fetchesUV);

            attenuation  = fetchesWeights[0]  * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[0].xy,  shadowCoord.z));
            attenuation += fetchesWeights[1]  * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[1].xy,  shadowCoord.z));
            attenuation += fetchesWeights[2]  * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[2].xy,  shadowCoord.z));
            attenuation += fetchesWeights[3]  * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[3].xy,  shadowCoord.z));
            attenuation += fetchesWeights[4]  * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[4].xy,  shadowCoord.z));
            attenuation += fetchesWeights[5]  * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[5].xy,  shadowCoord.z));
            attenuation += fetchesWeights[6]  * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[6].xy,  shadowCoord.z));
            attenuation += fetchesWeights[7]  * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[7].xy,  shadowCoord.z));
            attenuation += fetchesWeights[8]  * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[8].xy,  shadowCoord.z));
            attenuation += fetchesWeights[9]  * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[9].xy,  shadowCoord.z));
            attenuation += fetchesWeights[10] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[10].xy, shadowCoord.z));
            attenuation += fetchesWeights[11] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[11].xy, shadowCoord.z));
            attenuation += fetchesWeights[12] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[12].xy, shadowCoord.z));
            attenuation += fetchesWeights[13] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[13].xy, shadowCoord.z));
            attenuation += fetchesWeights[14] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[14].xy, shadowCoord.z));
            attenuation += fetchesWeights[15] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[15].xy, shadowCoord.z));
        #else
            float fetchesWeights[9];
            float2 fetchesUV[9];
            SampleShadow_ComputeSamples_Tent_5x5(samplingData.shadowmapSize, shadowCoord.xy, fetchesWeights, fetchesUV);

            attenuation  = fetchesWeights[0] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[0].xy, shadowCoord.z));
            attenuation += fetchesWeights[1] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[1].xy, shadowCoord.z));
            attenuation += fetchesWeights[2] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[2].xy, shadowCoord.z));
            attenuation += fetchesWeights[3] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[3].xy, shadowCoord.z));
            attenuation += fetchesWeights[4] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[4].xy, shadowCoord.z));
            attenuation += fetchesWeights[5] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[5].xy, shadowCoord.z));
            attenuation += fetchesWeights[6] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[6].xy, shadowCoord.z));
            attenuation += fetchesWeights[7] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[7].xy, shadowCoord.z));
            attenuation += fetchesWeights[8] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[8].xy, shadowCoord.z));
        #endif
    #endif
#else
    // 1-tap hardware comparison
    attenuation = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz);
#endif

    attenuation = LerpWhiteTo(attenuation, shadowStrength);

    // Shadow coords that fall out of the light frustum volume must always return attenuation 1.0
    return BEYOND_SHADOW_FAR(shadowCoord) ? 1.0 : attenuation;
}

half ComputeCascadeIndex(float3 positionWS)
{
    // TODO: profile if there's a performance improvement if we avoid indexing here
    float3 fromCenter0 = positionWS.xyz - _DirShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = positionWS.xyz - _DirShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = positionWS.xyz - _DirShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = positionWS.xyz - _DirShadowSplitSpheres[3].xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    half4 weights = half4(distances2 < _DirShadowSplitSphereRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);

    return 4 - dot(weights, half4(4, 3, 2, 1));
}

float4 TransformWorldToShadowCoord(float3 positionWS)
{
#ifdef _SHADOWS_CASCADE
    half cascadeIndex = ComputeCascadeIndex(positionWS);
    return mul(_WorldToShadow[cascadeIndex], float4(positionWS, 1.0));
#else
    return mul(_WorldToShadow[0], float4(positionWS, 1.0));
#endif
}

float4 ComputeShadowCoord(float4 clipPos)
{
    // TODO: This might have to be corrected for double-wide and texture arrays
    return ComputeScreenPos(clipPos);
}

half MainLightRealtimeShadowAttenuation(float4 shadowCoord)
{
#if !defined(_SHADOWS_ENABLED)
    return 1.0h;
#endif

#if SHADOWS_SCREEN
    return SampleScreenSpaceShadowMap(shadowCoord);
#else
    ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
    half shadowStrength = GetMainLightShadowStrength();
    return SampleShadowmap(shadowCoord, TEXTURE2D_PARAM(_DirectionalShadowmapTexture, sampler_DirectionalShadowmapTexture), shadowSamplingData, shadowStrength);
#endif
}

half LocalLightRealtimeShadowAttenuation(int lightIndex, float3 positionWS)
{
#if !defined(_LOCAL_SHADOWS_ENABLED)
    return 1.0h;
#else
    float4 shadowCoord = mul(_LocalWorldToShadowAtlas[lightIndex], float4(positionWS, 1.0));
    ShadowSamplingData shadowSamplingData = GetLocalLightShadowSamplingData();
    half shadowStrength = GetLocalLightShadowStrenth(lightIndex);
    return SampleShadowmap(shadowCoord, TEXTURE2D_PARAM(_LocalShadowmapTexture, sampler_LocalShadowmapTexture), shadowSamplingData, shadowStrength);
#endif
}

#endif
