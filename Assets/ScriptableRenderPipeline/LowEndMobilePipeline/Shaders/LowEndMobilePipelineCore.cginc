#define MAX_LIGHTS 8

#define INITIALIZE_LIGHT(light, lightIndex) \
                            light.pos = globalLightPos[lightIndex]; \
                            light.color = globalLightColor[lightIndex]; \
                            light.atten = globalLightAtten[lightIndex]; \
                            light.spotDir = globalLightSpotDir[lightIndex]

#if defined(_HARD_SHADOWS) || defined(_SOFT_SHADOWS) || defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOWS
#endif

#if defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOW_CASCADES
#endif

struct LightInput
{
    half4 pos;
    half4 color;
    half4 atten;
    half4 spotDir;
};

struct LowendVertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float3 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
};

struct v2f
{
    float4 uv01 : TEXCOORD0; // uv01.xy: uv0, uv01.zw: uv1
    float3 posWS : TEXCOORD1;
#if _NORMALMAP
    half3 tangentToWorld0 : TEXCOORD2; // tangentToWorld matrix
    half3 tangentToWorld1 : TEXCOORD3; // tangentToWorld matrix
    half3 tangentToWorld2 : TEXCOORD4; // tangentToWorld matrix
#else
    half3 normal : TEXCOORD2;
#endif
    half4 viewDir : TEXCOORD5; // xyz: viewDir
    UNITY_FOG_COORDS_PACKED(6, half4) // x: fogCoord, yzw: vertexColor
    float4 hpos : SV_POSITION;
};

// The variables are very similar to built-in unity_LightColor, unity_LightPosition,
// unity_LightAtten, unity_SpotDirection as used by the VertexLit shaders, except here
// we use world space positions instead of view space.
half4 globalLightColor[MAX_LIGHTS];
float4 globalLightPos[MAX_LIGHTS];
half4 globalLightSpotDir[MAX_LIGHTS];
half4 globalLightAtten[MAX_LIGHTS];
float4  globalLightData; // x: pixelLightCount, y = totalLightCount (pixel + vert), z = minShadowNormalBiasOffset, w = shadowNormalBiasOffset

half _Shininess;
samplerCUBE _Cube;
half4 _ReflectColor;

#ifdef _SHADOWS
#include "LowEndMobilePipelineShadows.cginc"
#endif

inline void NormalMap(v2f i, out half3 normal)
{
#if _NORMALMAP
    half3 normalmap = UnpackNormal(tex2D(_BumpMap, i.uv01.xy));

    // glsl compiler will generate underperforming code by using a row-major pre multiplication matrix: mul(normalmap, i.tangentToWorld)
    // i.tangetToWorld was initialized as column-major in vs and here dot'ing individual for better performance.
    // The code below is similar to post multiply: mul(i.tangentToWorld, normalmap)
    normal = half3(dot(normalmap, i.tangentToWorld0), dot(normalmap, i.tangentToWorld1), dot(normalmap, i.tangentToWorld2));
#else
    normal = normalize(i.normal);
#endif
}

inline void SpecularGloss(half2 uv, half alpha, out half4 specularGloss)
{
#ifdef _SPECGLOSSMAP
    specularGloss = tex2D(_SpecGlossMap, uv) * _SpecColor;
#elif defined(_SPECGLOSSMAP_BASE_ALPHA)
    specularGloss = tex2D(_SpecGlossMap, uv) * _SpecColor;
    specularGloss.a = alpha;
#else
    specularGloss = _SpecColor;
#endif
}

inline void Emission(half2 uv, inout half3 color)
{
#ifdef _EMISSION
    color += tex2D(_EmissionMap, uv) * _EmissionColor;
#else
    color += _EmissionColor;
#endif
}

half4 OutputColor(half3 color, half alpha)
{
#ifdef _ALPHABLEND_ON
    return half4(color, alpha);
#else
    return half4(color, 1);
#endif
}

inline half3 EvaluateOneLight(LightInput lightInput, half3 diffuseColor, half4 specularGloss, half3 normal, float3 posWorld, half3 viewDir, out half NdotL)
{
    float3 posToLight = lightInput.pos.xyz;
    posToLight -= posWorld * lightInput.pos.w;

    float distanceSqr = max(dot(posToLight, posToLight), 0.001);
    float lightAtten = 1.0 / (1.0 + distanceSqr * lightInput.atten.z);

    float3 lightDir = posToLight * rsqrt(distanceSqr);
    half SdotL = saturate(dot(lightInput.spotDir.xyz, lightDir));
    lightAtten *= saturate((SdotL - lightInput.atten.x) / lightInput.atten.y);

    half cutoff = step(distanceSqr, lightInput.atten.w);
    lightAtten *= cutoff;

    NdotL = saturate(dot(normal, lightDir));

    half3 halfVec = normalize(lightDir + viewDir);
    half NdotH = saturate(dot(normal, halfVec));

    half3 lightColor = lightInput.color.rgb * lightAtten;
    half3 diffuse = diffuseColor * lightColor * NdotL;

#if defined(_SPECGLOSSMAP_BASE_ALPHA) || defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    half3 specular = specularGloss.rgb * lightColor * pow(NdotH, _Shininess * 128.0) * specularGloss.a;
    return diffuse + specular;
#else
    return diffuse;
#endif
}
