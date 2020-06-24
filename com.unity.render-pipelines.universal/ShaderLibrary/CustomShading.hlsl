#ifndef CUSTOM_SHADING
#define CUSTOM_SHADING

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;

    float2 uv           : TEXCOORD0;
#if LIGHTMAP_ON
    float2 uvLightmap   : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    float2 uvLightmap               : TEXCOORD1;
    float3 positionWS               : TEXCOORD2;
    half3  normalWS                 : TEXCOORD3;

#ifdef _NORMALMAP
    half4 tangentWS                 : TEXCOORD4;
#endif

    float4 positionCS               : SV_POSITION;
};

// User defined surface data.
struct CustomSurfaceData
{
    half3 diffuse;              // diffuse color. should be black for metals.
    half3 reflectance;          // reflectance color at normal indicence. It's monochromatic for dieletrics.
    half3 normalWS;             // normal in world space
    half  ao;                   // ambient occlusion
    half  roughness;            // roughness = perceptualRoughness * perceptualRoughness;
    half3 emission;             // emissive color
    half  alpha;                // 0 for transparent materials, 1.0 for opaque.
};

struct LightingData 
{
    Light light;
    half3 halfDirectionWS;
    half3 normalWS;
    half NdotL;
    half NdotH;
    half LdotH;
};

#endif