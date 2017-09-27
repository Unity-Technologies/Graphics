#ifndef LIGHTWEIGHT_INPUT_INCLUDED
#define LIGHTWEIGHT_INPUT_INCLUDED

#define MAX_VISIBLE_LIGHTS 16

// Main light initialized without indexing
#define INITIALIZE_MAIN_LIGHT(light) \
    light.pos = _MainLightPosition; \
    light.color = _MainLightColor; \
    light.atten = _MainLightAttenuationParams; \
    light.spotDir = _MainLightSpotDir;

// Indexing might have a performance hit for old mobile hardware
#define INITIALIZE_LIGHT(light, lightIndex) \
                            light.pos = _AdditionalLightPosition[lightIndex]; \
                            light.color = _AdditionalLightColor[lightIndex]; \
                            light.atten = _AdditionalLightAttenuationParams[lightIndex]; \
                            light.spotDir = _AdditionalLightSpotDir[lightIndex]

#if (defined(_MAIN_DIRECTIONAL_LIGHT) || defined(_MAIN_SPOT_LIGHT) || defined(_MAIN_POINT_LIGHT))
#define _MAIN_LIGHT
#endif

#ifdef _SPECULAR_SETUP
#define SAMPLE_METALLICSPECULAR(uv) tex2D(_SpecGlossMap, uv)
#else
#define SAMPLE_METALLICSPECULAR(uv) tex2D(_MetallicGlossMap, uv)
#endif

#if defined(UNITY_COLORSPACE_GAMMA) && defined(_LIGHTWEIGHT_FORCE_LINEAR)
#define LIGHTWEIGHT_GAMMA_TO_LINEAR(gammaColor) gammaColor * gammaColor
#define LIGHTWEIGHT_LINEAR_TO_GAMMA(linColor) sqrt(color)
#else
#define LIGHTWEIGHT_GAMMA_TO_LINEAR(color) color
#define LIGHTWEIGHT_LINEAR_TO_GAMMA(color) color
#endif

struct LightInput
{
    float4 pos;
    half4 color;
    float4 atten;
    half4 spotDir;
};

CBUFFER_START(_PerObject)
half4 unity_LightIndicesOffsetAndCount;
half4 unity_4LightIndices0;
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

struct LightweightVertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct LightweightVertexOutput
{
    float4 uv01 : TEXCOORD0; // uv01.xy: uv0, uv01.zw: uv1
    float4 posWS : TEXCOORD1;
#if _NORMALMAP
    half3 tangentToWorld0 : TEXCOORD2; // tangentToWorld matrix
    half3 tangentToWorld1 : TEXCOORD3; // tangentToWorld matrix
    half3 tangentToWorld2 : TEXCOORD4; // tangentToWorld matrix
#else
    half3 normal : TEXCOORD2;
#endif
    half4 viewDir : TEXCOORD5; // xyz: viewDir
    half4 fogCoord : TEXCOORD6; // x: fogCoord, yzw: vertexColor
    float4 hpos : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SurfaceData
{
    half3 albedo;
    half  alpha;
    half4 metallicSpecGloss;
    half3 normalWorld;
    half  ao;
    half3 emission;
};

struct BRDFData
{
    half3 diffuse;
    half3 specular;
    half perceptualRoughness;
    half roughness;
    half grazingTerm;
};

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

inline half3 Normal(LightweightVertexOutput i)
{
    #if _NORMALMAP
    half3 normalTangent = UnpackNormal(tex2D(_BumpMap, i.uv01.xy));

    // glsl compiler will generate underperforming code by using a row-major pre multiplication matrix: mul(normalmap, i.tangentToWorld)
    // i.tangetToWorld was initialized as column-major in vs and here dot'ing individual for better performance.
    // The code below is similar to post multiply: mul(i.tangentToWorld, normalmap)
    half3 normalWorld = normalize(half3(dot(normalTangent, i.tangentToWorld0), dot(normalTangent, i.tangentToWorld1), dot(normalTangent, i.tangentToWorld2)));
#else
    half3 normalWorld = normalize(i.normal);
#endif

    return normalWorld;
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
