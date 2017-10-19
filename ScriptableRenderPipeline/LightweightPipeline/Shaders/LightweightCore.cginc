#ifndef LIGHTWEIGHT_PIPELINE_CORE_INCLUDED
#define LIGHTWEIGHT_PIPELINE_CORE_INCLUDED

#include "UnityCG.cginc"
#include "UnityStandardInput.cginc"

#define MAX_VISIBLE_LIGHTS 16

#if defined(UNITY_COLORSPACE_GAMMA)
    #define LIGHTWEIGHT_GAMMA_TO_LINEAR(gammaColor) gammaColor * gammaColor
    #define LIGHTWEIGHT_LINEAR_TO_GAMMA(linColor) sqrt(color)
#else
    #define LIGHTWEIGHT_GAMMA_TO_LINEAR(color) color
    #define LIGHTWEIGHT_LINEAR_TO_GAMMA(color) color
#endif

#ifdef _SPECULAR_SETUP
    #define SAMPLE_METALLICSPECULAR(uv) tex2D(_SpecGlossMap, uv)
#else
    #define SAMPLE_METALLICSPECULAR(uv) tex2D(_MetallicGlossMap, uv)
#endif

CBUFFER_START(_PerObject)
half4 unity_LightIndicesOffsetAndCount;
half4 unity_4LightIndices0;
half4 unity_4LightIndices1;
half _Shininess;
CBUFFER_END

CBUFFER_START(_PerCamera)
float4 _MainLightPosition;
half4 _MainLightColor;
float4 _MainLightAttenuationParams;
half4 _MainLightSpotDir;

half4 _AdditionalLightCount;
float4 _AdditionalLightPosition[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightColor[MAX_VISIBLE_LIGHTS];
float4 _AdditionalLightAttenuationParams[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightSpotDir[MAX_VISIBLE_LIGHTS];
CBUFFER_END

CBUFFER_START(_PerFrame)
half4 _GlossyEnvironmentColor;
sampler2D _AttenuationTexture;
CBUFFER_END

half3 TangentToWorldNormal(half3 normalTangent, half3 tangent, half3 binormal, half3 normal)
{
    half3x3 tangentToWorld = half3x3(tangent, binormal, normal);
    return normalize(mul(normalTangent, tangentToWorld));
}

float ComputeFogFactor(float z)
{
    float clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(z);

#if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    float fogFactor = saturate(clipZ_01 * unity_FogParams.z + unity_FogParams.w);
    return half(fogFactor);
#elif defined(FOG_EXP)
    // factor = exp(-density*z)
    float unityFogFactor = unity_FogParams.y * clipZ_01;
    return half(saturate(exp2(-unityFogFactor)));
#elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    float unityFogFactor = unity_FogParams.x * clipZ_01;
    return half(saturate(exp2(-unityFogFactor*unityFogFactor)));
#else
    return 0.0h;
#endif
}

void ApplyFog(inout half3 color, half fogFactor)
{
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    color = lerp(unity_FogColor, color, fogFactor);
#endif
}

half4 OutputColor(half3 color, half alpha)
{
#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
    return half4(LIGHTWEIGHT_LINEAR_TO_GAMMA(color), alpha);
#else
    return half4(LIGHTWEIGHT_LINEAR_TO_GAMMA(color), 1);
#endif
}

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
    return UnpackNormal(tex2D(_BumpMap, uv));
#else
    return half3(0.0h, 0.0h, 1.0h);
#endif
}

inline void SpecularGloss(half2 uv, half alpha, out half4 specularGloss)
{
    specularGloss = half4(0, 0, 0, 1);
#ifdef _SPECGLOSSMAP
    specularGloss = tex2D(_SpecGlossMap, uv);
    specularGloss.rgb = LIGHTWEIGHT_GAMMA_TO_LINEAR(specularGloss.rgb);
#elif defined(_SPECULAR_COLOR)
    specularGloss = _SpecColor;
#endif

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    specularGloss.a = alpha;
#endif
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
#if _METALLIC_SETUP
    specGloss.rgb = _Metallic.rrr;
#else
    specGloss.rgb = _SpecColor.rgb;
#endif

#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    specGloss.a = albedoAlpha * _GlossMapScale;
#else
    specGloss.a = _Glossiness;
#endif
#endif

    return specGloss;
}

half OcclusionLW(float2 uv)
{
#ifdef _OCCLUSIONMAP
    #if (SHADER_TARGET < 30)
    // SM20: instruction count limitation
    // SM20: simpler occlusion
    return tex2D(_OcclusionMap, uv).g;
#else
    half occ = tex2D(_OcclusionMap, uv).g;
    return LerpOneTo(occ, _OcclusionStrength);
#endif
#else
    return 1.0;
#endif
}

half3 EmissionLW(float2 uv)
{
#ifndef _EMISSION
    return 0;
#else
    return LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_EmissionMap, uv).rgb) * _EmissionColor.rgb;
#endif
}



#endif
