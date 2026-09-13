#ifndef UNIVERSAL_SHADOWS_INCLUDED
#define UNIVERSAL_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lightmaps.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShadowSamplingData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.deprecated.hlsl"

#if !defined(_ADDITIONAL_LIGHT_SHADOWS_KEYWORD_DECLARED)
    #if !defined(_ADDITIONAL_LIGHT_SHADOWS)
        static const bool _ADDITIONAL_LIGHT_SHADOWS = 0;
    #elif DEFINED_NONZERO(_ADDITIONAL_LIGHT_SHADOWS)
        #undef _ADDITIONAL_LIGHT_SHADOWS
        #define _ADDITIONAL_LIGHT_SHADOWS 1
    #endif
#endif

#if !defined(_MAIN_LIGHT_SHADOWS_KEYWORD_DECLARED)
    #if !defined(_MAIN_LIGHT_SHADOWS)
        static const bool _MAIN_LIGHT_SHADOWS = 0;
    #elif DEFINED_NONZERO(_MAIN_LIGHT_SHADOWS)
        #undef _MAIN_LIGHT_SHADOWS
        #define _MAIN_LIGHT_SHADOWS 1
    #endif
#endif

#if !defined(_MAIN_LIGHT_SHADOWS_CASCADE_KEYWORD_DECLARED)
    #if !defined(_MAIN_LIGHT_SHADOWS_CASCADE)
        static const bool _MAIN_LIGHT_SHADOWS_CASCADE = 0;
    #elif DEFINED_NONZERO(_MAIN_LIGHT_SHADOWS_CASCADE)
        #undef _MAIN_LIGHT_SHADOWS_CASCADE
        #define _MAIN_LIGHT_SHADOWS_CASCADE 1
    #endif
#endif

#if !defined(_MAIN_LIGHT_SHADOWS_SCREEN_KEYWORD_DECLARED)
    #if !defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        static const bool _MAIN_LIGHT_SHADOWS_SCREEN = 0;
    #elif DEFINED_NONZERO(_MAIN_LIGHT_SHADOWS_SCREEN)
        #undef _MAIN_LIGHT_SHADOWS_SCREEN
        #define _MAIN_LIGHT_SHADOWS_SCREEN 1
    #endif
#endif

#if !defined(_SHADOWS_SOFT_LOW_KEYWORD_DECLARED)
    #if !defined(_SHADOWS_SOFT_LOW)
        static const bool _SHADOWS_SOFT_LOW = 0;
    #elif DEFINED_NONZERO(_SHADOWS_SOFT_LOW)
        #undef _SHADOWS_SOFT_LOW
        #define _SHADOWS_SOFT_LOW 1
    #endif
#endif

#if !defined(_SHADOWS_SOFT_MEDIUM_KEYWORD_DECLARED)
    #if !defined(_SHADOWS_SOFT_MEDIUM)
        static const bool _SHADOWS_SOFT_MEDIUM = 0;
    #elif DEFINED_NONZERO(_SHADOWS_SOFT_MEDIUM)
        #undef _SHADOWS_SOFT_MEDIUM
        #define _SHADOWS_SOFT_MEDIUM 1
    #endif
#endif

#if !defined(_SHADOWS_SOFT_HIGH_KEYWORD_DECLARED)
    #if !defined(_SHADOWS_SOFT_HIGH)
        static const bool _SHADOWS_SOFT_HIGH = 0;
    #elif DEFINED_NONZERO(_SHADOWS_SOFT_HIGH)
        #undef _SHADOWS_SOFT_HIGH
        #define _SHADOWS_SOFT_HIGH 1
    #endif
#endif

#if !defined(_SHADOWS_SOFT_KEYWORD_DECLARED)
    #if !defined(_SHADOWS_SOFT)
        static const bool _SHADOWS_SOFT = 0;
    #elif DEFINED_NONZERO(_SHADOWS_SOFT)
        #undef _SHADOWS_SOFT
        #define _SHADOWS_SOFT 1
    #endif
#endif

// 1 when the soft-shadow quality keywords are runtime-branching: all qualities then share one
// variant, so filtering must use the single-body filter instead of the per-quality unrolled ones.
#if defined(_SHADOWS_SOFT_LOW_KEYWORD_DECLARED) && (_SHADOWS_SOFT_LOW_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #define SHADOWS_SOFT_DYNAMIC_BRANCH 1
#else
    #define SHADOWS_SOFT_DYNAMIC_BRANCH 0
#endif

#if !defined(SHADOWS_SHADOWMASK_KEYWORD_DECLARED)
    #if !defined(SHADOWS_SHADOWMASK)
        static const bool SHADOWS_SHADOWMASK = 0;
    #elif DEFINED_NONZERO(SHADOWS_SHADOWMASK)
        #undef SHADOWS_SHADOWMASK
        #define SHADOWS_SHADOWMASK 1
    #endif
#endif

#if !defined(LIGHTMAP_SHADOW_MIXING_KEYWORD_DECLARED)
    #if !defined(LIGHTMAP_SHADOW_MIXING)
        static const bool LIGHTMAP_SHADOW_MIXING = 0;
    #elif DEFINED_NONZERO(LIGHTMAP_SHADOW_MIXING)
        #undef LIGHTMAP_SHADOW_MIXING
        #define LIGHTMAP_SHADOW_MIXING 1
    #endif
#endif

#if !defined(_MAIN_LIGHT_SHADOWS_KEYWORD_DECLARED) || (_MAIN_LIGHT_SHADOWS_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if defined(_MAIN_LIGHT_SHADOWS) && !defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        #define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR 1
    #endif
#endif

#if !defined(_MAIN_LIGHT_SHADOWS_SCREEN_KEYWORD_DECLARED) || (_MAIN_LIGHT_SHADOWS_SCREEN_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        #define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR 1
    #endif
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) || (_MAIN_LIGHT_SHADOWS_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING) || (_MAIN_LIGHT_SHADOWS_SCREEN_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #define USE_VERTEX_SHADOW_COORD_INTERPOLATOR 1
#else
    #define USE_VERTEX_SHADOW_COORD_INTERPOLATOR 0
#endif

bool ShadowCoordInterpolatorAvailable()
{
    #if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
    return _MAIN_LIGHT_SHADOWS || _MAIN_LIGHT_SHADOWS_SCREEN;
    #else
    return false;
    #endif
}

bool MainLightShadowsAvailable()
{
    return _MAIN_LIGHT_SHADOWS || _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS_SCREEN;
}

bool MainLightScreenShadowsAvailable()
{
    return _MAIN_LIGHT_SHADOWS_SCREEN;
}

bool AdditionalLightShadowsAvailable()
{
    return _ADDITIONAL_LIGHT_SHADOWS;
}

bool SoftShadowsAvailable()
{
    return _SHADOWS_SOFT || _SHADOWS_SOFT_LOW || _SHADOWS_SOFT_MEDIUM || _SHADOWS_SOFT_HIGH;
}

// Should match: UnityEngine.Rendering.Universal + 1
#define SOFT_SHADOW_QUALITY_OFF     half(0)
#define SOFT_SHADOW_QUALITY_LOW     half(1)
#define SOFT_SHADOW_QUALITY_MEDIUM  half(2)
#define SOFT_SHADOW_QUALITY_HIGH    half(3)

#if defined(UNITY_DOTS_INSTANCING_ENABLED) && !defined(USE_LEGACY_LIGHTMAPS)
// ^ GPU-driven rendering is enabled, and we haven't opted-out from lightmap
// texture arrays. This minimizes batch breakages, but texture arrays aren't
// supported in a performant way on all GPUs.
#define SHADOWMASK_NAME unity_ShadowMasks
#define SHADOWMASK_SAMPLER_NAME samplerunity_ShadowMasks
#define SHADOWMASK_SAMPLE_EXTRA_ARGS , unity_LightmapIndex.x
#else
// ^ Lightmaps are not bound as texture arrays, but as individual textures. The
// batch is broken every time lightmaps are changed, but this is well-supported
// on all GPUs.
#define SHADOWMASK_NAME unity_ShadowMask
#define SHADOWMASK_SAMPLER_NAME samplerunity_ShadowMask
#define SHADOWMASK_SAMPLE_EXTRA_ARGS
#endif

bool ShadowMaskAvailable()
{
    return SHADOWS_SHADOWMASK;
}

bool LightmapShadowMixingAvailable()
{
    return LIGHTMAP_SHADOW_MIXING;
}

bool BakedShadowsAvailable()
{
    return LightmapAvailable() || LightmapShadowMixingAvailable() || ShadowMaskAvailable();
}

bool MixedLightingSubtractive()
{
    return LightmapShadowMixingAvailable() && !ShadowMaskAvailable();
}

half4 SampleShadowMask(float2 lightmapUV)
{
    if (LightmapAvailable())
    {
        if (ShadowMaskAvailable())
        {
            #if defined(LIGHTMAP_BICUBIC_SAMPLING)
            return SampleLightmapBicubic(SHADOWMASK_NAME, SHADOWMASK_SAMPLER_NAME, lightmapUV SHADOWMASK_SAMPLE_EXTRA_ARGS);
            #else
            return SAMPLE_TEXTURE2D_LIGHTMAP(SHADOWMASK_NAME, SHADOWMASK_SAMPLER_NAME, lightmapUV SHADOWMASK_SAMPLE_EXTRA_ARGS);
            #endif
        }
        else
        {
            return half4(1, 1, 1, 1);
        }
    }
    else
    {
        return unity_ProbesOcclusion;
    }
}

// Back-compat macro; without a lightmap UV interpolator the uv argument may not exist, so it is discarded.
#if USE_LIGHTMAP_UV_INTERPOLATOR
    #define SAMPLE_SHADOWMASK(uv) SampleShadowMask(uv)
#else
    #define SAMPLE_SHADOWMASK(uv) unity_ProbesOcclusion;
#endif

#if !(defined(_SPOT) || defined(_POINT) || defined(_DIRECTIONAL))
#define FORWARD_RENDER_PATH
#endif

TEXTURE2D_X(_ScreenSpaceShadowmapTexture);

TEXTURE2D_SHADOW(_MainLightShadowmapTexture);
TEXTURE2D_SHADOW(_AdditionalLightsShadowmapTexture);
SAMPLER_CMP(sampler_LinearClampCompare);

float4      _MainLightShadowParams;   // (x: shadowStrength, y: >= 1.0 if soft shadows, 0.0 otherwise, z: main light fade scale, w: main light fade bias)
float4      _AdditionalShadowFadeParams; // x: additional light fade scale, y: additional light fade bias, z: 0.0, w: 0.0)


// Point lights can use 6 shadow slices. Some mobile GPUs performance decrease drastically with uniform
// blocks bigger than 8kb while others have a 64kb max uniform block size. This number ensures size of buffer
// AdditionalLightShadows stays reasonable. It also avoids shader compilation errors on SHADER_API_GLES30
// devices where max number of uniforms per shader GL_MAX_FRAGMENT_UNIFORM_VECTORS is low (224)

// GLES3 causes a performance regression in some devices when using CBUFFER.
#ifndef LIGHT_SHADOWS_NO_CBUFFER
CBUFFER_START(AdditionalLightShadows)
#endif
float4x4    _AdditionalLightsWorldToShadow[MAX_VISIBLE_LIGHTS];  // Per-shadow-slice-data
float4      _AdditionalShadowParams[MAX_VISIBLE_LIGHTS];         // Per-light data: (x: shadowStrength, y: softShadows, z: light type (Spot: 0, Point: 1), w: perLightFirstShadowSliceIndex)
#ifndef LIGHT_SHADOWS_NO_CBUFFER
CBUFFER_END
#endif

// x: depth bias,
// y: normal bias,
// z: light type (Spot = 0, Directional = 1, Point = 2, Area/Rectangle = 3, Disc = 4, Pyramid = 5, Box = 6, Tube = 7)
// w: unused
float4 _ShadowBias;

half IsSpotLight()
{
    return round(_ShadowBias.z) == 0.0 ? 1 : 0;
}

half IsDirectionalLight()
{
    return round(_ShadowBias.z) == 1.0 ? 1 : 0;
}

half IsPointLight()
{
    return round(_ShadowBias.z) == 2.0 ? 1 : 0;
}

#define BEYOND_SHADOW_FAR(shadowCoord) shadowCoord.z <= 0.0 || shadowCoord.z >= 1.0

ShadowSamplingData GetMainLightShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;

    // shadowOffsets are used in SampleShadowmapFiltered for low quality soft shadows.
    shadowSamplingData.shadowOffset0 = half4(_MainLightShadowOffset0);
    shadowSamplingData.shadowOffset1 = half4(_MainLightShadowOffset1);

    // shadowmapSize is used in SampleShadowmapFiltered otherwise
    shadowSamplingData.shadowmapSize = _MainLightShadowmapSize;
    shadowSamplingData.softShadowQuality = half(_MainLightShadowParams.y);

    return shadowSamplingData;
}

ShadowSamplingData GetAdditionalLightShadowSamplingData(int index)
{
    ShadowSamplingData shadowSamplingData = (ShadowSamplingData)0;

    if (AdditionalLightShadowsAvailable())
    {
        // shadowOffsets are used in SampleShadowmapFiltered for low quality soft shadows.
        shadowSamplingData.shadowOffset0 = _AdditionalShadowOffset0;
        shadowSamplingData.shadowOffset1 = _AdditionalShadowOffset1;

        // shadowmapSize is used in SampleShadowmapFiltered otherwise.
        shadowSamplingData.shadowmapSize = _AdditionalShadowmapSize;
        shadowSamplingData.softShadowQuality = _AdditionalShadowParams[index].y;
    }

    return shadowSamplingData;
}

// ShadowParams
// x: ShadowStrength
// y: 1.0 if shadow is soft, 0.0 otherwise
half4 GetMainLightShadowParams()
{
    return half4(_MainLightShadowParams);
}

// ShadowParams
// x: ShadowStrength
// y: >= 1.0 if shadow is soft, 0.0 otherwise. Higher value for higher quality. (1.0 == low, 2.0 == medium, 3.0 == high)
// z: 1.0 if cast by a point light (6 shadow slices), 0.0 if cast by a spot light (1 shadow slice)
// w: first shadow slice index for this light, there can be 6 in case of point lights. (-1 for non-shadow-casting-lights)
half4 GetAdditionalLightShadowParams(int lightIndex)
{
    // Same defaults as set in AdditionalLightsShadowCasterPass.cs
    half4 results = half4(0, 0, 0, -1);

    if (AdditionalLightShadowsAvailable())
    {
        results = _AdditionalShadowParams[lightIndex];
        // workaround: Avoid failing the graphics test using Terrain Shader on Android Vulkan when using dynamic branching for fog keywords.
        #if !SKIP_SHADOWS_LIGHT_INDEX_CHECK
            results.w = lightIndex < 0 ? -1 : results.w;
        #endif
    }

    return results;
}

half SampleScreenSpaceShadowmap(float4 shadowCoord)
{
    shadowCoord.xy /= max(0.00001, shadowCoord.w); // Prevent division by zero.

    // The stereo transform has to happen after the manual perspective divide
    shadowCoord.xy = UnityStereoTransformScreenSpaceTex(shadowCoord.xy);

#if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION)
    shadowCoord.xy = RemovePretransformRotation(shadowCoord.xy);
#endif

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    half attenuation = SAMPLE_TEXTURE2D_ARRAY(_ScreenSpaceShadowmapTexture, sampler_PointClamp, shadowCoord.xy, unity_StereoEyeIndex).x;
#else
    half attenuation = half(SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_PointClamp, shadowCoord.xy).x);
#endif

    return attenuation;
}

real SampleShadowmapFilteredLowQuality(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData)
{
    // 4-tap hardware comparison
    real4 attenuation4;
    attenuation4.x = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + float3(samplingData.shadowOffset0.xy, 0)));
    attenuation4.y = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + float3(samplingData.shadowOffset0.zw, 0)));
    attenuation4.z = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + float3(samplingData.shadowOffset1.xy, 0)));
    attenuation4.w = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + float3(samplingData.shadowOffset1.zw, 0)));
    return dot(attenuation4, real(0.25));
}

real SampleShadowmapFilteredMediumQuality(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData)
{
    float fetchesWeights[9];
    float2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_Filter_5x5(float, samplingData.shadowmapSize, shadowCoord, fetchesWeights, fetchesUV);

    return fetchesWeights[0] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[0].xy, shadowCoord.z))
                + fetchesWeights[1] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[1].xy, shadowCoord.z))
                + fetchesWeights[2] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[2].xy, shadowCoord.z))
                + fetchesWeights[3] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[3].xy, shadowCoord.z))
                + fetchesWeights[4] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[4].xy, shadowCoord.z))
                + fetchesWeights[5] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[5].xy, shadowCoord.z))
                + fetchesWeights[6] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[6].xy, shadowCoord.z))
                + fetchesWeights[7] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[7].xy, shadowCoord.z))
                + fetchesWeights[8] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[8].xy, shadowCoord.z));
}

real SampleShadowmapFilteredHighQuality(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData)
{
    float fetchesWeights[16];
    float2 fetchesUV[16];
    SampleShadow_ComputeSamples_Tent_Filter_7x7(float, samplingData.shadowmapSize, shadowCoord, fetchesWeights, fetchesUV);

    return fetchesWeights[0] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[0].xy, shadowCoord.z))
                + fetchesWeights[1] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[1].xy, shadowCoord.z))
                + fetchesWeights[2] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[2].xy, shadowCoord.z))
                + fetchesWeights[3] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[3].xy, shadowCoord.z))
                + fetchesWeights[4] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[4].xy, shadowCoord.z))
                + fetchesWeights[5] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[5].xy, shadowCoord.z))
                + fetchesWeights[6] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[6].xy, shadowCoord.z))
                + fetchesWeights[7] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[7].xy, shadowCoord.z))
                + fetchesWeights[8] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[8].xy, shadowCoord.z))
                + fetchesWeights[9] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[9].xy, shadowCoord.z))
                + fetchesWeights[10] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[10].xy, shadowCoord.z))
                + fetchesWeights[11] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[11].xy, shadowCoord.z))
                + fetchesWeights[12] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[12].xy, shadowCoord.z))
                + fetchesWeights[13] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[13].xy, shadowCoord.z))
                + fetchesWeights[14] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[14].xy, shadowCoord.z))
                + fetchesWeights[15] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[15].xy, shadowCoord.z));
}

// Single-body soft filter for runtime-selected quality (per-light, and dynamic-keyword builds).
half SampleShadowmapFilteredUnified(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData, half quality)
{
    // Up to 16 taps for the active quality, accumulated in fixed 4-tap chunks gated by quality.
    // All indexing is compile-time constant for better codegen.
    float fetchesWeights[16];
    float2 fetchesUV[16];

    // Zero-init so unused taps contribute nothing even if the hardware flattens the quality gates below.
    UNITY_UNROLL for (int z = 0; z < 16; z++) { fetchesWeights[z] = 0.0; fetchesUV[z] = shadowCoord.xy; }

    if (quality >= SOFT_SHADOW_QUALITY_HIGH)
    {
        SampleShadow_ComputeSamples_Tent_Filter_7x7(float, samplingData.shadowmapSize, shadowCoord, fetchesWeights, fetchesUV);
    }
    else if (quality >= SOFT_SHADOW_QUALITY_MEDIUM)
    {
        SampleShadow_ComputeSamples_Tent_Filter_5x5(float, samplingData.shadowmapSize, shadowCoord, fetchesWeights, fetchesUV);
    }
    else
    {
        fetchesWeights[0] = 0.25; fetchesUV[0] = shadowCoord.xy + samplingData.shadowOffset0.xy;
        fetchesWeights[1] = 0.25; fetchesUV[1] = shadowCoord.xy + samplingData.shadowOffset0.zw;
        fetchesWeights[2] = 0.25; fetchesUV[2] = shadowCoord.xy + samplingData.shadowOffset1.xy;
        fetchesWeights[3] = 0.25; fetchesUV[3] = shadowCoord.xy + samplingData.shadowOffset1.zw;
    }

    #define SHADOW_TAP(i) half(fetchesWeights[i]) * half(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[i].xy, shadowCoord.z)))

    // chunk 0: 4 taps (LOW and up)
    half attenuation = SHADOW_TAP(0) + SHADOW_TAP(1) + SHADOW_TAP(2) + SHADOW_TAP(3);

    UNITY_BRANCH
    if (quality >= SOFT_SHADOW_QUALITY_MEDIUM)
    {
        // chunk 1: taps 4..8 (MEDIUM is 9 taps; pad with the zeroed 9th)
        attenuation += SHADOW_TAP(4) + SHADOW_TAP(5) + SHADOW_TAP(6) + SHADOW_TAP(7) + SHADOW_TAP(8);

        UNITY_BRANCH
        if (quality >= SOFT_SHADOW_QUALITY_HIGH)
        {
            // chunk 2: taps 9..15 (HIGH is 16 taps)
            attenuation += SHADOW_TAP(9) + SHADOW_TAP(10) + SHADOW_TAP(11) + SHADOW_TAP(12)
                         + SHADOW_TAP(13) + SHADOW_TAP(14) + SHADOW_TAP(15);
        }
    }

    #undef SHADOW_TAP
    return attenuation;
}

half SampleShadowmapFiltered(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData)
{
    // Per-light soft-shadow quality is a runtime value, so always use the single-body filter.
    return SampleShadowmapFilteredUnified(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData, samplingData.softShadowQuality);
}

real SampleShadowmap(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData, half4 shadowParams, bool isPerspectiveProjection = true)
{
    // Compiler will optimize this branch away as long as isPerspectiveProjection is known at compile time
    if (isPerspectiveProjection)
        shadowCoord.xyz /= shadowCoord.w;

    real attenuation;
    real shadowStrength = shadowParams.x;

#if SHADOWS_SOFT_DYNAMIC_BRANCH
    half quality = _SHADOWS_SOFT_HIGH   ? SOFT_SHADOW_QUALITY_HIGH
                 : _SHADOWS_SOFT_MEDIUM ? SOFT_SHADOW_QUALITY_MEDIUM
                 : _SHADOWS_SOFT_LOW    ? SOFT_SHADOW_QUALITY_LOW
                 : _SHADOWS_SOFT        ? shadowParams.y
                 : SOFT_SHADOW_QUALITY_OFF;

    if (quality > SOFT_SHADOW_QUALITY_OFF)
    {
        attenuation = SampleShadowmapFilteredUnified(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData, quality);
    }
    else
    {
        attenuation = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz));
    }
#else
    if (_SHADOWS_SOFT_LOW)
    {
        attenuation = SampleShadowmapFilteredLowQuality(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
    }
    else if (_SHADOWS_SOFT_MEDIUM)
    {
        attenuation = SampleShadowmapFilteredMediumQuality(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
    }
    else if (_SHADOWS_SOFT_HIGH)
    {
        attenuation = SampleShadowmapFilteredHighQuality(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
    }
    else if (_SHADOWS_SOFT)
    {
        if (shadowParams.y > (half)SOFT_SHADOW_QUALITY_OFF)
        {
            attenuation = SampleShadowmapFiltered(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
        }
        else
        {
            attenuation = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz));
        }
    }
    else
    {
        attenuation = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz));
    }
#endif


    attenuation = LerpWhiteTo(attenuation, shadowStrength);

    // Shadow coords that fall out of the light frustum volume must always return attenuation 1.0
    // TODO: We could use branch here to save some perf on some platforms.
    return BEYOND_SHADOW_FAR(shadowCoord) ? 1.0 : attenuation;
}

half ComputeCascadeIndex(float3 positionWS)
{
    float3 fromCenter0 = positionWS - _CascadeShadowSplitSpheres0.xyz;
    float3 fromCenter1 = positionWS - _CascadeShadowSplitSpheres1.xyz;
    float3 fromCenter2 = positionWS - _CascadeShadowSplitSpheres2.xyz;
    float3 fromCenter3 = positionWS - _CascadeShadowSplitSpheres3.xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    half4 weights = half4(distances2 < _CascadeShadowSplitSphereRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);

    return half(4.0) - dot(weights, half4(4, 3, 2, 1));
}

float4 TransformWorldToShadowCoord(float3 positionWS, bool isSurfaceTypeTransparent)
{
    float4 shadowCoord = (float4)0;

    if (_MAIN_LIGHT_SHADOWS_SCREEN && !isSurfaceTypeTransparent)
    {
        shadowCoord = float4(ComputeNormalizedDeviceCoordinatesWithZ(positionWS, GetWorldToHClipMatrix()), 1.0);
    }
    else
    {
        half cascadeIndex = _MAIN_LIGHT_SHADOWS_CASCADE ? ComputeCascadeIndex(positionWS) : 0;
        shadowCoord = float4(mul(_MainLightWorldToShadow[cascadeIndex], float4(positionWS, 1.0)).xyz, 1.0);
    }

    return shadowCoord;
}

half SampleMainLightRealtimeShadow(float4 shadowCoord, half4 shadowParams, ShadowSamplingData shadowSamplingData, bool isSurfaceTypeTransparent)
{
    half result = 1;

#if (UNITY_PLATFORM_META_QUEST) // Avoid shadowmap lookup if coordinates are outside of the shadowmap.
    bool shadowCoordsValid = !(shadowCoord.z < 0.0 || any(saturate(shadowCoord.xy) != shadowCoord.xy));
#else
    bool shadowCoordsValid = true;
#endif

    if (MainLightShadowsAvailable() && shadowCoordsValid)
    {
        if (_MAIN_LIGHT_SHADOWS_SCREEN && !isSurfaceTypeTransparent)
        {
            result = SampleScreenSpaceShadowmap(shadowCoord);
        }
        else
        {
            result = SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, false);
        }
    }

    return result;
}

half SampleMainLightRealtimeShadow(float4 shadowCoord, bool isSurfaceTypeTransparent)
{
    return SampleMainLightRealtimeShadow(shadowCoord, GetMainLightShadowParams(), GetMainLightShadowSamplingData(), isSurfaceTypeTransparent);
}

// returns 0.0 if position is in light's shadow
// returns 1.0 if position is in light
half SampleAdditionalLightRealtimeShadow(int lightIndex, float3 positionWS, half3 lightDirection, half4 shadowParams, ShadowSamplingData shadowSamplingData)
{
    half result = 1.0;

    if (AdditionalLightShadowsAvailable())
    {
        int shadowSliceIndex = shadowParams.w;
        if (shadowSliceIndex >= 0)
        {
            // Add cube-face offset to shadowSliceIndex when this is a point light.
            // - FORWARD_RENDER_PATH: Apply offset code behind if-statement, since we determine light type at runtime.
            // - _POINT: Deferred point path applies offset code unconditionally since we know light type at compile time.
            // - _SPOT / _DIRECTIONAL: Deferred spot/directional paths skip the offset code entirely.
        #if defined(FORWARD_RENDER_PATH)
            half isPointLight = shadowParams.z;
            UNITY_BRANCH
            if (isPointLight)
                shadowSliceIndex += CubeMapFaceID(-lightDirection);
        #elif defined(_POINT)
            shadowSliceIndex += CubeMapFaceID(-lightDirection);
        #endif

            float4 shadowCoord = mul(_AdditionalLightsWorldToShadow[shadowSliceIndex], float4(positionWS, 1.0));

            result = SampleShadowmap(TEXTURE2D_ARGS(_AdditionalLightsShadowmapTexture, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, true);
        }
    }

    return result;
}

half SampleAdditionalLightRealtimeShadow(int lightIndex, float3 positionWS, half3 lightDirection)
{
    half result = half(1.0);
    if (AdditionalLightShadowsAvailable())
        result = SampleAdditionalLightRealtimeShadow(lightIndex, positionWS, lightDirection, GetAdditionalLightShadowParams(lightIndex), GetAdditionalLightShadowSamplingData(lightIndex));
    return result;
}

half GetMainLightShadowFade(float3 positionWS)
{
    float3 camToPixel = positionWS - _WorldSpaceCameraPos;
    float distanceCamToPixel2 = dot(camToPixel, camToPixel);

    float fade = saturate(distanceCamToPixel2 * float(_MainLightShadowParams.z) + float(_MainLightShadowParams.w));
    return half(fade);
}

half GetAdditionalLightShadowFade(float3 positionWS)
{
    half shadowFade = half(1.0);
    if (AdditionalLightShadowsAvailable())
    {
        float3 camToPixel = positionWS - _WorldSpaceCameraPos;
        float distanceCamToPixel2 = dot(camToPixel, camToPixel);

        float fade = saturate(distanceCamToPixel2 * float(_AdditionalShadowFadeParams.x) + float(_AdditionalShadowFadeParams.y));
        shadowFade = half(fade);
    }

    return shadowFade;
}

half MixRealtimeAndBakedShadows(half realtimeShadow, half bakedShadow, half shadowFade)
{
    if (LightmapShadowMixingAvailable())
        return min(lerp(realtimeShadow, 1, shadowFade), bakedShadow);
    else
        return lerp(realtimeShadow, bakedShadow, shadowFade);
}

half BakedShadow(half4 shadowMask, half4 occlusionProbeChannels, half4 shadowParams)
{
    // Here occlusionProbeChannels used as mask selector to select shadows in shadowMask
    // If occlusionProbeChannels all components are zero we use default baked shadow value 1.0
    // This code is optimized for mobile platforms:
    // half bakedShadow = any(occlusionProbeChannels) ? dot(shadowMask, occlusionProbeChannels) : 1.0h;
    half bakedShadow = half(1.0) + dot(shadowMask - half(1.0), occlusionProbeChannels);
    bakedShadow = LerpWhiteTo(bakedShadow, shadowParams.x);

    return bakedShadow;
}

half MainLightShadow(float4 shadowCoord, float3 positionWS, half4 shadowMask, half4 occlusionProbeChannels, bool receiveShadows, bool isSurfaceTypeTransparent)
{
    half4 shadowParams = GetMainLightShadowParams();
    // receiveShadows only disables the realtime contribution; the baked shadowmask below still
    // applies (the material Receive Shadows toggle never affected baked shadows).
    half realtimeShadow = receiveShadows ? SampleMainLightRealtimeShadow(shadowCoord, shadowParams, GetMainLightShadowSamplingData(), isSurfaceTypeTransparent) : half(1.0);

    half bakedShadow = BakedShadowsAvailable() ? BakedShadow(shadowMask, occlusionProbeChannels, shadowParams) : half(1.0);

    half shadowFade = (MainLightShadowsAvailable() && receiveShadows) ? GetMainLightShadowFade(positionWS) : half(1.0);

    return MixRealtimeAndBakedShadows(realtimeShadow, bakedShadow, shadowFade);
}

half AdditionalLightShadow(int lightIndex, float3 positionWS, half3 lightDirection, half4 shadowMask, half4 occlusionProbeChannels, bool receiveShadows)
{
    half4 shadowParams = GetAdditionalLightShadowParams(lightIndex);
    ShadowSamplingData samplingData = GetAdditionalLightShadowSamplingData(lightIndex);
    // receiveShadows only disables the realtime contribution; the baked shadowmask below still
    // applies (the material Receive Shadows toggle never affected baked shadows).
    half realtimeShadow = receiveShadows ? SampleAdditionalLightRealtimeShadow(lightIndex, positionWS, lightDirection, shadowParams, samplingData) : half(1.0);

    half bakedShadow = half(1.0);
    if (BakedShadowsAvailable())
    {
        // This fading of the baked shadow using the light's shadow strength parameter needs
        // to be guarded against the Real-Time Shadow keyword as _AdditionalShadowParams is
        // only included in URP Shaders and updated when real time additional shadows are enabled.
        if (!(AdditionalLightShadowsAvailable() && receiveShadows))
            shadowParams.x = half(1.0);

        bakedShadow = BakedShadow(shadowMask, occlusionProbeChannels, shadowParams);
    }

    half shadowFade = (AdditionalLightShadowsAvailable() && receiveShadows) ? GetAdditionalLightShadowFade(positionWS) : half(1.0);

    return MixRealtimeAndBakedShadows(realtimeShadow, bakedShadow, shadowFade);
}

float4 GetShadowCoord(VertexPositionInputs vertexInput, bool isSurfaceTypeTransparent)
{
    float4 result = (float4)0;

    if (_MAIN_LIGHT_SHADOWS_SCREEN && !isSurfaceTypeTransparent)
    {
        result = vertexInput.positionNDC;
    }
    else
    {
        result = TransformWorldToShadowCoord(vertexInput.positionWS, isSurfaceTypeTransparent);
    }

    return result;
}

float3 ApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
{
    float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
    float scale = invNdotL * _ShadowBias.y;

    // normal bias is negative since we want to apply an inset normal offset
    positionWS = lightDirection * _ShadowBias.xxx + positionWS;
    positionWS = normalWS * scale.xxx + positionWS;
    return positionWS;
}

float4 ApplyShadowClamping(float4 positionCS)
{
    #if UNITY_REVERSED_Z
        float clamped = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
        float clamped = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif

    // The current implementation of vertex clamping in Universal RP is the same as in Unity Built-In RP.
    // We follow the same convention in Universal RP where it's only enabled for Directional Lights
    // (see: Shadows.cpp::RenderShadowMaps() #L2161-L2162)
    // (see: Shadows.cpp::RenderShadowMaps() #L2086-L2102)
    // (see: Shadows.cpp::PrepareStateForShadowMap() #L1685-L1686)
    positionCS.z = lerp(positionCS.z, clamped, IsDirectionalLight());

    return positionCS;
}

///////////////////////////////////////////////////////////////////////////////
// Deprecated                                                                 /
///////////////////////////////////////////////////////////////////////////////

// Renamed -> _MainLightShadowParams
#define _MainLightShadowData _MainLightShadowParams

// Deprecated: Use GetMainLightShadowFade or GetAdditionalLightShadowFade instead.
float GetShadowFade(float3 positionWS)
{
    float3 camToPixel = positionWS - _WorldSpaceCameraPos;
    float distanceCamToPixel2 = dot(camToPixel, camToPixel);

    float fade = saturate(distanceCamToPixel2 * float(_MainLightShadowParams.z) + float(_MainLightShadowParams.w));
    return fade * fade;
}

// Deprecated: Use GetShadowFade instead.
float ApplyShadowFade(float shadowAttenuation, float3 positionWS)
{
    float fade = GetShadowFade(positionWS);
    return shadowAttenuation + (1 - shadowAttenuation) * fade * fade;
}

// Deprecated: Use GetMainLightShadowParams instead.
half GetMainLightShadowStrength()
{
    return half(_MainLightShadowData.x);
}

// Deprecated: Use SampleShadowmap that takes shadowParams instead of strength.
real SampleShadowmap(float4 shadowCoord, TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), ShadowSamplingData samplingData, half shadowStrength, bool isPerspectiveProjection = true)
{
    half4 shadowParams = half4(shadowStrength, 1.0, 0.0, 0.0);
    return SampleShadowmap(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData, shadowParams, isPerspectiveProjection);
}

// Deprecated: Use BakedShadow(half4 shadowMask, half4 occlusionProbeChannels, half4 shadowParams) as it supports shadowStrength from the light
half BakedShadow(half4 shadowMask, half4 occlusionProbeChannels)
{
    return BakedShadow(shadowMask, occlusionProbeChannels, half4(1,0,0,0));
}

#endif
