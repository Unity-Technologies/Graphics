#ifndef LIGHTWEIGHT_SURFACE_INPUT_INCLUDED
#define LIGHTWEIGHT_SURFACE_INPUT_INCLUDED

#include "LightweightCore.cginc"

#ifdef _SPECULAR_SETUP
#define SAMPLE_METALLICSPECULAR(uv) tex2D(_SpecGlossMap, uv)
#else
#define SAMPLE_METALLICSPECULAR(uv) tex2D(_MetallicGlossMap, uv)
#endif

CBUFFER_START(MaterialProperties)
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

sampler2D _MainTex;
sampler2D _MetallicGlossMap;
sampler2D _SpecGlossMap;
sampler2D _BumpMap;
sampler2D _OcclusionMap;
sampler2D _EmissionMap;

// Must match Lightweigth ShaderGraph master node
struct SurfaceData
{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normal;
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
    return UnpackNormalScale(tex2D(_BumpMap, uv), _BumpScale);
#else
    return half3(0.0h, 0.0h, 1.0h);
#endif
}

half4 SpecularGloss(half2 uv, half alpha)
{
    half4 specularGloss = half4(0, 0, 0, 1);
#ifdef _SPECGLOSSMAP
    specularGloss = tex2D(_SpecGlossMap, uv);
    specularGloss.rgb = LIGHTWEIGHT_GAMMA_TO_LINEAR(specularGloss.rgb);
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
    return tex2D(_OcclusionMap, uv).g;
#else
    half occ = tex2D(_OcclusionMap, uv).g;
    return _LerpOneTo(occ, _OcclusionStrength);
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
    return LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_EmissionMap, uv).rgb) * _EmissionColor.rgb;
#endif
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = tex2D(_MainTex, uv);

    half4 specGloss = MetallicSpecGloss(uv, albedoAlpha);
    outSurfaceData.albedo = LIGHTWEIGHT_GAMMA_TO_LINEAR(albedoAlpha.rgb) * _Color.rgb;

#if _SPECULAR_SETUP
    outSurfaceData.metallic = 1.0h;
    outSurfaceData.specular = specGloss.rgb;
#else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normal = Normal(uv);
    outSurfaceData.occlusion = Occlusion(uv);
    outSurfaceData.emission = Emission(uv);
    outSurfaceData.alpha = Alpha(albedoAlpha.a);
}

#endif
