#ifndef UNIVERSAL_PARTICLES_SIMPLE_LIT_INPUT_INCLUDED
#define UNIVERSAL_PARTICLES_SIMPLE_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesInput.hlsl"

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
    float4 _SoftParticleFadeParams;
    float4 _CameraFadeParams;
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _EmissionColor;
    half4 _BaseColorAddSubDiff;
    half4 _SpecColor;
    half _Cutoff;
    half _Smoothness;
    half _DistortionStrengthScaled;
    half _DistortionBlend;
    half _Surface;
CBUFFER_END

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl"

TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);

#define SOFT_PARTICLE_NEAR_FADE _SoftParticleFadeParams.x
#define SOFT_PARTICLE_INV_FADE_DISTANCE _SoftParticleFadeParams.y

#define CAMERA_NEAR_FADE _CameraFadeParams.x
#define CAMERA_INV_FADE_DISTANCE _CameraFadeParams.y

half4 SampleAlbedo(float2 uv, float3 blendUv, half4 color, float4 particleColor, float4 projectedPosition, TEXTURE2D_PARAM(albedoMap, sampler_albedoMap))
{
    half4 albedo = BlendTexture(TEXTURE2D_ARGS(albedoMap, sampler_albedoMap), uv, blendUv) * color;

    half4 colorAddSubDiff = half4(0, 0, 0, 0);
#if defined (_COLORADDSUBDIFF_ON)
    colorAddSubDiff = _BaseColorAddSubDiff;
#endif
    albedo = MixParticleColor(albedo, particleColor, colorAddSubDiff);

    AlphaDiscard(albedo.a, _Cutoff);

 #if defined(_SOFTPARTICLES_ON)
     ALBEDO_MUL *= SoftParticles(SOFT_PARTICLE_NEAR_FADE, SOFT_PARTICLE_INV_FADE_DISTANCE, projectedPosition);
 #endif

 #if defined(_FADING_ON)
     ALBEDO_MUL *= CameraFade(CAMERA_NEAR_FADE, CAMERA_INV_FADE_DISTANCE, projectedPosition);
 #endif

    return albedo;
}

half4 SampleAlbedo(TEXTURE2D_PARAM(albedoMap, sampler_albedoMap), ParticleParams params)
{
    half4 albedo = BlendTexture(TEXTURE2D_ARGS(albedoMap, sampler_albedoMap), params.uv, params.blendUv) * params.baseColor;

    half4 colorAddSubDiff = half4(0, 0, 0, 0);
    #if defined (_COLORADDSUBDIFF_ON)
        colorAddSubDiff = _BaseColorAddSubDiff;
    #endif
    albedo = MixParticleColor(albedo, params.vertexColor, colorAddSubDiff);

    AlphaDiscard(albedo.a, _Cutoff);

     #if defined(_SOFTPARTICLES_ON)
         ALBEDO_MUL *= SoftParticles(SOFT_PARTICLE_NEAR_FADE, SOFT_PARTICLE_INV_FADE_DISTANCE, params);
     #endif

     #if defined(_FADING_ON)
         ALBEDO_MUL *= CameraFade(CAMERA_NEAR_FADE, CAMERA_INV_FADE_DISTANCE, params.projectedPosition);
     #endif

    return albedo;
}

half4 SampleSpecularSmoothness(float2 uv, float3 blendUv, half alpha, half4 specColor, TEXTURE2D_PARAM(specGlossMap, sampler_specGlossMap))
{
    half4 specularGloss = half4(0.0h, 0.0h, 0.0h, 1.0h);
#ifdef _SPECGLOSSMAP
    specularGloss = BlendTexture(TEXTURE2D_ARGS(specGlossMap, sampler_specGlossMap), uv, blendUv);
#elif defined(_SPECULAR_COLOR)
    specularGloss = specColor;
#endif

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    specularGloss.a = alpha;
#endif
    specularGloss.a = exp2(10 * specularGloss.a + 1);

    return specularGloss;
}

#endif // UNIVERSAL_PARTICLES_SIMPLE_LIT_INPUT_INCLUDED
