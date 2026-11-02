#ifndef UNIVERSAL_PARTICLES_LIT_INPUT_INCLUDED
#define UNIVERSAL_PARTICLES_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/SurfaceType.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesLitInput.deprecated.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);
UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_BaseMap);

half4 SampleBaseMap(float2 uv) { return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv); }

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
float4 _SoftParticleFadeParams;
float4 _CameraFadeParams;
float4 _BaseMap_ST;
float4 _BaseMap_TexelSize;
half4 _BaseColor;
half4 _EmissionColor;
half4 _BaseColorAddSubDiff;
half _Cutoff;
half _Metallic;
half _Smoothness;
half _BumpScale;
half _DistortionStrengthScaled;
half _DistortionBlend;
half _Blend;
CBUFFER_END

#include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Particles/Particles.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);

#define SOFT_PARTICLE_NEAR_FADE _SoftParticleFadeParams.x
#define SOFT_PARTICLE_INV_FADE_DISTANCE _SoftParticleFadeParams.y

#define CAMERA_NEAR_FADE _CameraFadeParams.x
#define CAMERA_INV_FADE_DISTANCE _CameraFadeParams.y

half4 SampleAlbedo(float2 uv, float3 blendUv, half4 color, float4 particleColor, float4 projectedPosition, UnityTexture2D albedoMap, bool useAlphaPremultiply)
{
    half4 albedo = BlendTexture(albedoMap, uv, blendUv) * color;

    half4 colorAddSubDiff = half4(0, 0, 0, 0);
#if defined (_COLORADDSUBDIFF_ON)
    colorAddSubDiff = _BaseColorAddSubDiff;
#endif
    // No distortion Support
    albedo = MixParticleColor(albedo, half4(particleColor), colorAddSubDiff);

    albedo.a = AlphaDiscard(albedo.a, _Cutoff);

#if defined(_SOFTPARTICLES_ON)
    MulAlbedo(albedo, half(SoftParticles(SOFT_PARTICLE_NEAR_FADE, SOFT_PARTICLE_INV_FADE_DISTANCE, projectedPosition)), useAlphaPremultiply);
#endif

#if defined(_FADING_ON)
    MulAlbedo(albedo, CameraFade(CAMERA_NEAR_FADE, CAMERA_INV_FADE_DISTANCE, projectedPosition), useAlphaPremultiply);
#endif

    return albedo;
}

half4 SampleAlbedo(UnityTexture2D albedoMap, ParticleParams params, bool useAlphaPremultiply)
{
    half4 albedo = BlendTexture(albedoMap, params.uv, params.blendUv) * params.baseColor;

    half4 colorAddSubDiff = half4(0, 0, 0, 0);
#if defined (_COLORADDSUBDIFF_ON)
    colorAddSubDiff = _BaseColorAddSubDiff;
#endif
    // No distortion Support
    albedo = MixParticleColor(albedo, half4(params.vertexColor), colorAddSubDiff);

    albedo.a = AlphaDiscard(albedo.a, _Cutoff);

#if defined(_SOFTPARTICLES_ON)
    MulAlbedo(albedo, half(SoftParticles(SOFT_PARTICLE_NEAR_FADE, SOFT_PARTICLE_INV_FADE_DISTANCE, params)), useAlphaPremultiply);
#endif

#if defined(_FADING_ON)
    MulAlbedo(albedo, CameraFade(CAMERA_NEAR_FADE, CAMERA_INV_FADE_DISTANCE, params.projectedPosition), useAlphaPremultiply);
#endif

    return albedo;
}

inline void InitializeParticleLitSurfaceData(float2 uv, float3 blendUv, float4 particleColor, float4 projectedPosition, out SurfaceData outSurfaceData, bool useAlphaPremultiply, bool useMetallicSpecGlossMap, bool useEmission, bool useNormalMap, bool useAlphaModulate)
{
    half4 albedo = SampleAlbedo(uv, blendUv, _BaseColor, particleColor, projectedPosition, UnityBuildTexture2DStructNoScaleNoTexelSize(_BaseMap), useAlphaPremultiply);

    half2 metallicGloss = useMetallicSpecGlossMap ? BlendTexture(UnityBuildTexture2DStructNoScaleNoTexelSize(_MetallicGlossMap), uv, blendUv).ra * half2(1.0, _Smoothness) : half2(_Metallic, _Smoothness);

    half3 normalTS = half3(0.0, 0.0, 1.0);
    if (useNormalMap)
        normalTS = SampleParticleNormalTS(uv, blendUv, UnityBuildTexture2DStructNoScaleNoTexelSize(_BumpMap), _BumpScale);

    half3 emissionColor = useEmission ? BlendTexture(UnityBuildTexture2DStructNoScaleNoTexelSize(_EmissionMap), uv, blendUv).rgb * _EmissionColor.rgb : half3(0, 0, 0);

#if defined(_DISTORTION_ON)
    albedo.rgb = Distortion(albedo, normalTS, _DistortionStrengthScaled, _DistortionBlend, projectedPosition);
#endif

    outSurfaceData = (SurfaceData)0;
    outSurfaceData.albedo = albedo.rgb;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
    outSurfaceData.normalTS = normalTS;
    outSurfaceData.emission = emissionColor;
    outSurfaceData.metallic = metallicGloss.r;
    outSurfaceData.smoothness = metallicGloss.g;
    outSurfaceData.occlusion = 1.0;

    if (useAlphaModulate)
        outSurfaceData.albedo = ApplyAlphaModulate(outSurfaceData.albedo, albedo.a); // Premultiply in UniversalPBR, BRDF init.
    outSurfaceData.alpha = albedo.a;

    outSurfaceData.clearCoatMask       = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(1.0);
}

inline void InitializeParticleLitSurfaceData(ParticleParams params, out SurfaceData outSurfaceData, bool useAlphaPremultiply, bool useMetallicSpecGlossMap, bool useEmission, bool useNormalMap, bool useAlphaModulate)
{
    half4 albedo = SampleAlbedo(UnityBuildTexture2DStructNoScaleNoTexelSize(_BaseMap), params, useAlphaPremultiply);

    half2 metallicGloss = useMetallicSpecGlossMap ? BlendTexture(UnityBuildTexture2DStructNoScaleNoTexelSize(_MetallicGlossMap), params.uv, params.blendUv).ra * half2(1.0, _Smoothness) : half2(_Metallic, _Smoothness);

    half3 normalTS = half3(0.0, 0.0, 1.0);
    if (useNormalMap)
        normalTS = SampleParticleNormalTS(params.uv, params.blendUv, UnityBuildTexture2DStructNoScaleNoTexelSize(_BumpMap), _BumpScale);

    half3 emissionColor = useEmission ? BlendTexture(UnityBuildTexture2DStructNoScaleNoTexelSize(_EmissionMap), params.uv, params.blendUv).rgb * _EmissionColor.rgb : half3(0, 0, 0);

    #if defined(_DISTORTION_ON)
        albedo.rgb = Distortion(albedo, normalTS, _DistortionStrengthScaled, _DistortionBlend, params.projectedPosition);
    #endif

    outSurfaceData = (SurfaceData)0;
    outSurfaceData.albedo = albedo.rgb;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
    outSurfaceData.normalTS = normalTS;
    outSurfaceData.emission = emissionColor;
    outSurfaceData.metallic = metallicGloss.r;
    outSurfaceData.smoothness = metallicGloss.g;
    outSurfaceData.occlusion = 1.0;

    if (useAlphaModulate)
        outSurfaceData.albedo = ApplyAlphaModulate(outSurfaceData.albedo, albedo.a);
    outSurfaceData.alpha = albedo.a;

    outSurfaceData.clearCoatMask       = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(1.0);
}

#endif // UNIVERSAL_PARTICLES_LIT_INPUT_INCLUDED
