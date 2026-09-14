#ifndef UNIVERSAL_PARTICLES_UNLIT_INPUT_INCLUDED
#define UNIVERSAL_PARTICLES_UNLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/SurfaceType.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesUnlitInput.deprecated.hlsl"

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
    half _DistortionStrengthScaled;
    half _DistortionBlend;
    half _Blend;
CBUFFER_END

#include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/Particles/Particles.hlsl"

#define SOFT_PARTICLE_NEAR_FADE _SoftParticleFadeParams.x
#define SOFT_PARTICLE_INV_FADE_DISTANCE _SoftParticleFadeParams.y

#define CAMERA_NEAR_FADE _CameraFadeParams.x
#define CAMERA_INV_FADE_DISTANCE _CameraFadeParams.y

#define _BumpScale 1.0

half4 SampleAlbedo(float2 uv, float3 blendUv, half4 color, float4 particleColor, float4 projectedPosition, UnityTexture2D albedoMap, bool useAlphaPremultiply, bool useAlphaModulate)
{
    half4 albedo = BlendTexture(albedoMap, uv, blendUv) * color;

    // No distortion Support
    half4 colorAddSubDiff = half4(0, 0, 0, 0);
#if defined (_COLORADDSUBDIFF_ON)
    colorAddSubDiff = _BaseColorAddSubDiff;
#endif
    albedo = MixParticleColor(albedo, half4(particleColor), colorAddSubDiff);

    albedo.a = AlphaDiscard(albedo.a, _Cutoff);

    if (useAlphaModulate || useAlphaPremultiply)
        albedo.rgb = AlphaModulateAndPremultiply(albedo.rgb, albedo.a, useAlphaModulate);

#if defined(_SOFTPARTICLES_ON)
    SoftParticleMulAlbedo(albedo, half(SoftParticles(SOFT_PARTICLE_NEAR_FADE, SOFT_PARTICLE_INV_FADE_DISTANCE, projectedPosition)), useAlphaPremultiply, useAlphaModulate);
#endif

#if defined(_FADING_ON)
    MulAlbedo(albedo, CameraFade(CAMERA_NEAR_FADE, CAMERA_INV_FADE_DISTANCE, projectedPosition), useAlphaPremultiply);
#endif

    return albedo;
}

half4 SampleAlbedo(UnityTexture2D albedoMap, ParticleParams params, bool useAlphaPremultiply, bool useAlphaModulate)
{
    half4 albedo = BlendTexture(albedoMap, params.uv, params.blendUv) * params.baseColor;

    // No distortion Support
    #if defined (_COLORADDSUBDIFF_ON)
        half4 colorAddSubDiff = _BaseColorAddSubDiff;
    #else
        half4 colorAddSubDiff = half4(0, 0, 0, 0);
    #endif
    albedo = MixParticleColor(albedo, half4(params.vertexColor), colorAddSubDiff);

    albedo.a = AlphaDiscard(albedo.a, _Cutoff);
    if (useAlphaModulate || useAlphaPremultiply)
        albedo.rgb = AlphaModulateAndPremultiply(albedo.rgb, albedo.a, useAlphaModulate);

    #if defined(_SOFTPARTICLES_ON)
        SoftParticleMulAlbedo(albedo, half(SoftParticles(SOFT_PARTICLE_NEAR_FADE, SOFT_PARTICLE_INV_FADE_DISTANCE, params)), useAlphaPremultiply, useAlphaModulate);
    #endif

    #if defined(_FADING_ON)
        MulAlbedo(albedo, CameraFade(CAMERA_NEAR_FADE, CAMERA_INV_FADE_DISTANCE, params.projectedPosition), useAlphaPremultiply);
    #endif

    return albedo;
}

#endif // UNIVERSAL_PARTICLES_PBR_INCLUDED
