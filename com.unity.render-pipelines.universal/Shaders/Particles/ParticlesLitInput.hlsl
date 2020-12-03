#ifndef UNIVERSAL_PARTICLES_LIT_INPUT_INCLUDED
#define UNIVERSAL_PARTICLES_LIT_INPUT_INCLUDED

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
half _Cutoff;
half _Metallic;
half _Smoothness;
half _BumpScale;
half _DistortionStrengthScaled;
half _DistortionBlend;
half _Surface;
CBUFFER_END

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl"

TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);

#define SOFT_PARTICLE_NEAR_FADE _SoftParticleFadeParams.x
#define SOFT_PARTICLE_INV_FADE_DISTANCE _SoftParticleFadeParams.y

#define CAMERA_NEAR_FADE _CameraFadeParams.x
#define CAMERA_INV_FADE_DISTANCE _CameraFadeParams.y

// Pre-multiplied alpha helper
#if defined(_ALPHAPREMULTIPLY_ON)
#define ALBEDO_MUL albedo
#else
#define ALBEDO_MUL albedo.a
#endif

half4 SampleAlbedo(float2 uv, float3 blendUv, half4 color, float4 particleColor, float4 projectedPosition, TEXTURE2D_PARAM(albedoMap, sampler_albedoMap))
{
    half4 albedo = BlendTexture(TEXTURE2D_ARGS(albedoMap, sampler_albedoMap), uv, blendUv) * color;

    half4 colorAddSubDiff = half4(0, 0, 0, 0);
#if defined (_COLORADDSUBDIFF_ON)
    colorAddSubDiff = _BaseColorAddSubDiff;
#endif
    // No distortion Support
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
    // No distortion Support
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

inline void InitializeParticleLitSurfaceData(float2 uv, float3 blendUv, float4 particleColor, float4 projectedPosition, out SurfaceData outSurfaceData)
{
    half4 albedo = SampleAlbedo(uv, blendUv, _BaseColor, particleColor, projectedPosition, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));

#if defined(_METALLICSPECGLOSSMAP)
    half2 metallicGloss = BlendTexture(TEXTURE2D_ARGS(_MetallicGlossMap, sampler_MetallicGlossMap), uv, blendUv).ra * half2(1.0, _Smoothness);
#else
    half2 metallicGloss = half2(_Metallic, _Smoothness);
#endif

    half3 normalTS = SampleNormalTS(uv, blendUv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

#if defined(_EMISSION)
    half3 emission = BlendTexture(TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap), uv, blendUv).rgb * _EmissionColor.rgb;
#else
    half3 emission = half3(0, 0, 0);
#endif

#if defined(_DISTORTION_ON)
    albedo.rgb = Distortion(albedo, normalTS, _DistortionStrengthScaled, _DistortionBlend, projectedPosition);
#endif

    outSurfaceData = (SurfaceData)0;
    outSurfaceData.albedo = albedo.rgb;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
    outSurfaceData.normalTS = normalTS;
    outSurfaceData.emission = emission;
    outSurfaceData.metallic = metallicGloss.r;
    outSurfaceData.smoothness = metallicGloss.g;
    outSurfaceData.occlusion = 1.0;

    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, albedo.a);
    outSurfaceData.alpha = albedo.a;

    outSurfaceData.clearCoatMask       = 0.0h;
    outSurfaceData.clearCoatSmoothness = 1.0h;
}

inline void InitializeParticleLitSurfaceData(ParticleParams params, out SurfaceData outSurfaceData)
{
    half4 albedo = SampleAlbedo(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), params);

    #if defined(_METALLICSPECGLOSSMAP)
        half2 metallicGloss = BlendTexture(TEXTURE2D_ARGS(_MetallicGlossMap, sampler_MetallicGlossMap), params.uv, params.blendUv).ra * half2(1.0, _Smoothness);
    #else
        half2 metallicGloss = half2(_Metallic, _Smoothness);
    #endif

    half3 normalTS = SampleNormalTS(params.uv, params.blendUv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

    #if defined(_EMISSION)
        half3 emission = BlendTexture(TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap), params.uv, params.blendUv).rgb * _EmissionColor.rgb;
    #else
        half3 emission = half3(0, 0, 0);
    #endif

    #if defined(_DISTORTION_ON)
        albedo.rgb = Distortion(albedo, normalTS, _DistortionStrengthScaled, _DistortionBlend, params.projectedPosition);
    #endif

    outSurfaceData = (SurfaceData)0;
    outSurfaceData.albedo = albedo.rgb;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
    outSurfaceData.normalTS = normalTS;
    outSurfaceData.emission = emission;
    outSurfaceData.metallic = metallicGloss.r;
    outSurfaceData.smoothness = metallicGloss.g;
    outSurfaceData.occlusion = 1.0;

    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, albedo.a);
    outSurfaceData.alpha = albedo.a;

    outSurfaceData.clearCoatMask       = 0.0h;
    outSurfaceData.clearCoatSmoothness = 1.0h;
}

#endif // UNIVERSAL_PARTICLES_LIT_INPUT_INCLUDED
