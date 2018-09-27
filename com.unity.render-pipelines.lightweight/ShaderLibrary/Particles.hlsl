#ifndef LIGHTWEIGHT_PARTICLES_INCLUDED
#define LIGHTWEIGHT_PARTICLES_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _SoftParticleFadeParams;
float4 _CameraFadeParams;
float4 _MainTex_ST;
half4 _Color;
half4 _EmissionColor;
half4 _SpecColor;

#if defined (_COLORADDSUBDIFF_ON)
    half4 _ColorAddSubDiff;
#endif

half _Cutoff;
half _Shininess;
half _Metallic;
half _Glossiness;
half _BumpScale;
CBUFFER_END

TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);

#define SOFT_PARTICLE_NEAR_FADE _SoftParticleFadeParams.x
#define SOFT_PARTICLE_INV_FADE_DISTANCE _SoftParticleFadeParams.y

#define CAMERA_NEAR_FADE _CameraFadeParams.x
#define CAMERA_INV_FADE_DISTANCE _CameraFadeParams.y

// Color function
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
#define vertColor(c) \
        vertInstancingColor(c);
#else
#define vertColor(c)
#endif

// Flipbook vertex function
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
#if defined(_FLIPBOOK_BLENDING)
#define vertTexcoord(v, o) \
        vertInstancingUVs(v.texcoords.xy, o.texcoord, o.texcoord2AndBlend);
#else
#define vertTexcoord(v, o) \
        vertInstancingUVs(v.texcoords.xy, o.texcoord); \
        o.texcoord = TRANSFORM_TEX(o.texcoord, _MainTex);
#endif
#else
#if defined(_FLIPBOOK_BLENDING)
#define vertTexcoord(v, o) \
        o.texcoord = v.texcoords.xy; \
        o.texcoord2AndBlend.xy = v.texcoords.zw; \
        o.texcoord2AndBlend.z = v.texcoordBlend;
#else
#define vertTexcoord(v, o) \
        o.texcoord = TRANSFORM_TEX(v.texcoords.xy, _MainTex);
#endif
#endif

// Fading vertex function
#if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
#define vertFading(o, positionWS, positionCS) \
    o.projectedPosition.xy = positionCS.xy * 0.5 + positionCS.w; \
    o.projectedPosition.y *= _ProjectionParams.x; \
    o.projectedPosition.w = positionCS.w; \
    o.projectedPosition.z = -TransformWorldToView(positionWS.xyz).z
#else
#define vertFading(o, positionWS, positionCS)
#endif

// Color blending fragment function
#if defined(_COLOROVERLAY_ON)
#define fragColorMode(i) \
    albedo.rgb = lerp(1 - 2 * (1 - albedo.rgb) * (1 - i.color.rgb), 2 * albedo.rgb * i.color.rgb, step(albedo.rgb, 0.5)); \
    albedo.a *= i.color.a;
#elif defined(_COLORCOLOR_ON)
#define fragColorMode(i) \
    half3 aHSL = RgbToHsv(albedo.rgb); \
    half3 bHSL = RgbToHsv(i.color.rgb); \
    half3 rHSL = half3(bHSL.x, bHSL.y, aHSL.z); \
    albedo = half4(HsvToRgb(rHSL), albedo.a * i.color.a);
#elif defined(_COLORADDSUBDIFF_ON)
#define fragColorMode(i) \
    albedo.rgb = albedo.rgb + i.color.rgb * _ColorAddSubDiff.x; \
    albedo.rgb = lerp(albedo.rgb, abs(albedo.rgb), _ColorAddSubDiff.y); \
    albedo.a *= i.color.a;
#else
#define fragColorMode(i) \
    albedo *= i.color;
#endif

// Pre-multiplied alpha helper
#if defined(_ALPHAPREMULTIPLY_ON)
#define ALBEDO_MUL albedo
#else
#define ALBEDO_MUL albedo.a
#endif

// Soft particles fragment function
#if defined(SOFTPARTICLES_ON) && defined(_FADING_ON)
#define fragSoftParticles(i) \
    if (SOFT_PARTICLE_NEAR_FADE > 0.0 || SOFT_PARTICLE_INV_FADE_DISTANCE > 0.0) \
    { \
        float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.projectedPosition.xy / i.projectedPosition.w), _ZBufferParams); \
        float fade = saturate (SOFT_PARTICLE_INV_FADE_DISTANCE * ((sceneZ - SOFT_PARTICLE_NEAR_FADE) - i.projectedPosition.z)); \
        ALBEDO_MUL *= fade; \
    }
#else
#define fragSoftParticles(i)
#endif

// Camera fading fragment function
#if defined(_FADING_ON)
#define fragCameraFading(i) \
    float cameraFade = saturate((i.projectedPosition.z - CAMERA_NEAR_FADE) * CAMERA_INV_FADE_DISTANCE); \
    ALBEDO_MUL *= cameraFade;
#else
#define fragCameraFading(i)
#endif

// Vertex shader input
struct AttributesParticle
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    half4 color : COLOR;
#if defined(_FLIPBOOK_BLENDING) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    float4 texcoords : TEXCOORD0;
    float texcoordBlend : TEXCOORD1;
#else
    float2 texcoords : TEXCOORD0;
#endif
#if defined(_NORMALMAP)
    float4 tangent : TANGENT;
#endif
};

struct VaryingsParticle
{
    half4 color                     : COLOR;
    float2 texcoord                 : TEXCOORD0;
#ifdef _NORMALMAP
    half3 tangent                   : TEXCOORD1;
    half3 bitangent                  : TEXCOORD2;
    half3 normal                    : TEXCOORD3;
#else
    half3 normal                    : TEXCOORD1;
#endif

#if defined(_FLIPBOOK_BLENDING)
    float3 texcoord2AndBlend        : TEXCOORD4;
#endif
#if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
    float4 projectedPosition        : TEXCOORD5;
#endif
    float4 posWS                    : TEXCOORD6; // xyz: position; w = fogFactor;
    half4 viewDirShininess          : TEXCOORD7; // xyz: viewDir; w = shininess
    float4 clipPos                  : SV_POSITION;
};

half4 readTexture(TEXTURE2D_ARGS(_Texture, sampler_Texture), VaryingsParticle input)
{
    half4 color = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, input.texcoord);
#ifdef _FLIPBOOK_BLENDING
    half4 color2 = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, input.texcoord2AndBlend.xy);
    color = lerp(color, color2, IN.texcoord2AndBlend.z);
#endif
    return color;
}

half3 SampleNormalTS(VaryingsParticle input, TEXTURE2D_ARGS(bumpMap, sampler_bumpMap), half scale = 1.0h)
{
#if defined(_NORMALMAP)
    half4 n = readTexture(TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), input);
    #if BUMP_SCALE_NOT_SUPPORTED
        return UnpackNormal(n);
    #else
        return UnpackNormalScale(n, scale);
    #endif
#else
    return half3(0.0h, 0.0h, 1.0h);
#endif
}

half3 SampleEmission(VaryingsParticle input, half3 emissionColor, TEXTURE2D_ARGS(emissionMap, sampler_emissionMap))
{
#if defined(_EMISSION)
    return readTexture(TEXTURE2D_PARAM(emissionMap, sampler_emissionMap), input).rgb * emissionColor.rgb;
#else
    return half3(0.0h, 0.0h, 0.0h);
#endif
}

half4 SampleAlbedo(VaryingsParticle input, TEXTURE2D_ARGS(albedoMap, sampler_albedoMap))
{
    half4 albedo = readTexture(TEXTURE2D_PARAM(albedoMap, sampler_albedoMap), input) * _Color;

    // No distortion Support
    fragColorMode(input);
    fragSoftParticles(input);
    fragCameraFading(input);

    return albedo;
}

half4 SampleSpecularGloss(VaryingsParticle input, half alpha, half4 specColor, TEXTURE2D_ARGS(specGlossMap, sampler_specGlossMap))
{
    half4 specularGloss = half4(0.0h, 0.0h, 0.0h, 1.0h);
#ifdef _SPECGLOSSMAP
    specularGloss = readTexture(TEXTURE2D_PARAM(specGlossMap, sampler_specGlossMap), input);
#elif defined(_SPECULAR_COLOR)
    specularGloss = specColor;
#endif

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    specularGloss.a = alpha;
#endif
    return specularGloss;
}

half AlphaBlendAndTest(half alpha, half cutoff)
{
#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAOVERLAY_ON)
    half result = alpha;
#else
    half result = 1.0h;
#endif
    AlphaDiscard(alpha, cutoff, 0.0001h);

    return result;
}

half3 AlphaModulate(half3 albedo, half alpha)
{
#if defined(_ALPHAMODULATE_ON)
    return lerp(half3(1.0h, 1.0h, 1.0h), albedo, alpha);
#else
    return albedo;
#endif
}

void InitializeInputData(VaryingsParticle input, half3 normalTS, out InputData output)
{
    half3 viewDirWS = input.viewDirShininess.xyz;
#if SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    output.positionWS = input.posWS.xyz;

#if _NORMALMAP
    output.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangent, input.bitangent, input.normal));
#else
    output.normalWS = input.normal;
#endif
    output.normalWS = NormalizeNormalPerPixel(output.normalWS);

    output.viewDirectionWS = viewDirWS;
    output.shadowCoord = float4(0, 0, 0, 0);
    output.fogCoord = (half)input.posWS.w;
    output.vertexLighting = half3(0.0h, 0.0h, 0.0h);
    output.bakedGI = half3(0.0h, 0.0h, 0.0h);
}

#endif
