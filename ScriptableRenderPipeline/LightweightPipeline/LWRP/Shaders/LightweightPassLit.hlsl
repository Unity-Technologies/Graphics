#ifndef LIGHTWEIGHT_PASS_LIT_INCLUDED
#define LIGHTWEIGHT_PASS_LIT_INCLUDED

#include "LightweightShaderLibrary/InputSurface.hlsl"
#include "LightweightShaderLibrary/Lighting.hlsl"

struct LightweightVertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
};

struct LightweightVertexOutput
{
    float2 uv                       : TEXCOORD0;
    float4 lightmapUVOrVertexSH     : TEXCOORD1; // holds either lightmapUV or vertex SH. depending on LIGHTMAP_ON
    float3 posWS                    : TEXCOORD2;
    half3  normal                   : TEXCOORD3;

#if _NORMALMAP
    half3 tangent                   : TEXCOORD4;
    half3 binormal                  : TEXCOORD5;
#endif

    half3 viewDir                   : TEXCOORD6;
    half4 fogFactorAndVertexLight   : TEXCOORD7; // x: fogFactor, yzw: vertex light

    float4 clipPos                  : SV_POSITION;
};

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Vertex: Used for Standard and StandardSimpleLighting shaders
LightweightVertexOutput LitPassVertex(LightweightVertexInput v)
{
    LightweightVertexOutput o = (LightweightVertexOutput)0;

    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

    o.posWS = TransformObjectToWorld(v.vertex.xyz);
    o.clipPos = TransformWorldToHClip(o.posWS);
    o.viewDir = SafeNormalize(_WorldSpaceCameraPos - o.posWS);

    // initializes o.normal and if _NORMALMAP also o.tangent and o.binormal
    OUTPUT_NORMAL(v, o);

    // We either sample GI from lightmap or SH. lightmap UV and vertex SH coefficients
    // are packed in lightmapUVOrVertexSH to save interpolator.
    // The following funcions initialize
    OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUVOrVertexSH);
    OUTPUT_SH(o.normal, o.lightmapUVOrVertexSH);

    half3 vertexLight = VertexLighting(o.posWS, o.normal);
    half fogFactor = ComputeFogFactor(o.clipPos.z);
    o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    return o;
}

// Used for Standard shader
half4 LitPassFragment(LightweightVertexOutput IN) : SV_Target
{
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(IN.uv, surfaceData);

#if _NORMALMAP
    half3 normalWS = TangentToWorldNormal(surfaceData.normal, IN.tangent, IN.binormal, IN.normal);
#else
    half3 normalWS = normalize(IN.normal);
#endif

    half3 indirectDiffuse = SampleGI(IN.lightmapUVOrVertexSH, normalWS);
    float fogFactor = IN.fogFactorAndVertexLight.x;

    half4 color = LightweightFragmentPBR(IN.posWS.xyz, normalWS, IN.viewDir, indirectDiffuse, IN.fogFactorAndVertexLight.yzw, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);

    // Computes fog factor per-vertex
    ApplyFog(color.rgb, fogFactor);
    return color;
}

// Used for StandardSimpleLighting shader
half4 LitPassFragmentSimple(LightweightVertexOutput IN) : SV_Target
{
    float2 uv = IN.uv;

    half4 diffuseAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    half3 diffuse = diffuseAlpha.rgb * _Color.rgb;

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    half alpha = _Color.a;
#else
    half alpha = diffuseAlpha.a * _Color.a;
#endif

    AlphaDiscard(alpha, _Cutoff);

#if _NORMALMAP
    half3 normalTangent = Normal(uv);
    half3 normalWS = TangentToWorldNormal(normalTangent, IN.tangent, IN.binormal, IN.normal);
#else
    half3 normalWS = normalize(IN.normal);
#endif

    half3 emission = Emission(uv);

    half3 viewDirectionWS = SafeNormalize(IN.viewDir.xyz);
    float3 positionWS = IN.posWS.xyz;

    half3 diffuseGI = SampleGI(IN.lightmapUVOrVertexSH, normalWS);

#if _VERTEX_LIGHTS
    diffuseGI += IN.fogFactorAndVertexLight.yzw;
#endif

    half shininess = _Shininess * 128.0h;
    half fogFactor = IN.fogFactorAndVertexLight.x;

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    half4 specularGloss = SpecularGloss(uv, diffuseAlpha.a);
    return LightweightFragmentBlinnPhong(positionWS, normalWS, viewDirectionWS, fogFactor, diffuseGI, diffuse, specularGloss, shininess, emission, alpha);
#else
    return LightweightFragmentLambert(positionWS, normalWS, viewDirectionWS, fogFactor, diffuseGI, diffuse, emission, alpha);
#endif
};

#endif
