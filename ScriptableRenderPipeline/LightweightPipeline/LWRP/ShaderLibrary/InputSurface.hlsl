#ifndef LIGHTWEIGHT_SURFACE_INPUT_INCLUDED
#define LIGHTWEIGHT_SURFACE_INPUT_INCLUDED

#include "Core.hlsl"
#include "CoreRP/ShaderLibrary/Packing.hlsl"
#include "CoreRP/ShaderLibrary/CommonMaterial.hlsl"

#ifdef _SPECULAR_SETUP
#define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
#else
#define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
#endif

CBUFFER_START(UnityPerMaterial)
half4 _MainTex_ST;
half4 _Color;
half _Cutoff;
half _Glossiness;
half _GlossMapScale;
half _SmoothnessTextureChannel;
half _Metallic;
half4 _SpecColor;
half _BumpScale;
half _OcclusionStrength;
half4 _EmissionColor;
half _Shininess;
CBUFFER_END

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_SpecGlossMap); SAMPLER(sampler_SpecGlossMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

// Must match Lightweigth ShaderGraph master node
struct SurfaceData
{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normalTS;
    half3 emission;
    half  occlusion;
    half  alpha;
};

///////////////////////////////////////////////////////////////////////////////
//                      Material Property Helpers                            //
///////////////////////////////////////////////////////////////////////////////
inline half Alpha(half albedoAlpha)
{
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
    half alpha = _Color.a;
#else
    half alpha = albedoAlpha * _Color.a;
#endif

#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif

    return alpha;
}

half3 Normal(float2 uv)
{
#if _NORMALMAP
    return UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
#else
    return half3(0.0h, 0.0h, 1.0h);
#endif
}

half4 SpecularGloss(half2 uv, half alpha)
{
    half4 specularGloss = half4(0, 0, 0, 1);
#ifdef _SPECGLOSSMAP
    specularGloss = SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv);
    specularGloss.rgb = specularGloss.rgb;
#elif defined(_SPECULAR_COLOR)
    specularGloss = _SpecColor;
#endif

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    specularGloss.a = alpha;
#endif
    return specularGloss;
}

half4 MetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss;

#ifdef _METALLICSPECGLOSSMAP
    specGloss = specGloss = SAMPLE_METALLICSPECULAR(uv);
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    specGloss.a = albedoAlpha * _GlossMapScale;
#else
    specGloss.a *= _GlossMapScale;
#endif

#else // _METALLICSPECGLOSSMAP
#if _SPECULAR_SETUP
    specGloss.rgb = _SpecColor.rgb;
#else
    specGloss.rgb = _Metallic.rrr;
#endif

#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    specGloss.a = albedoAlpha * _GlossMapScale;
#else
    specGloss.a = _Glossiness;
#endif
#endif

    return specGloss;
}

half Occlusion(float2 uv)
{
#ifdef _OCCLUSIONMAP
#if (SHADER_TARGET < 30)
    // SM20: instruction count limitation
    // SM20: simpler occlusion
    return SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
#else
    half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
    return LerpWhiteTo(occ, _OcclusionStrength);
#endif
#else
    return 1.0;
#endif
}

half3 Emission(float2 uv)
{
#ifndef _EMISSION
    return 0;
#else
    return SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
#endif
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

    half4 specGloss = MetallicSpecGloss(uv, albedoAlpha.a);
    outSurfaceData.albedo = albedoAlpha.rgb * _Color.rgb;

#if _SPECULAR_SETUP
    outSurfaceData.metallic = 1.0h;
    outSurfaceData.specular = specGloss.rgb;
#else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normalTS = Normal(uv);
    outSurfaceData.occlusion = Occlusion(uv);
    outSurfaceData.emission = Emission(uv);
    outSurfaceData.alpha = Alpha(albedoAlpha.a);
}

#endif
