#ifndef LIGHTWEIGHT_PASS_LIT_INCLUDED
#define LIGHTWEIGHT_PASS_LIT_INCLUDED

#include "LightweightSurfaceInput.cginc"
#include "LightweightLighting.cginc"

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
    float4 uv01                     : TEXCOORD0; // xy: main UV, zw: lightmap UV (directional / non-directional)
    float3 posWS                    : TEXCOORD1;

#if _NORMALMAP
    half3 tangent                   : TEXCOORD2;
    half3 binormal                  : TEXCOORD3;
    half3 normal                    : TEXCOORD4;
#else
    half3 normal                    : TEXCOORD2;
#endif

    half3 viewDir                   : TEXCOORD5;
    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

#ifndef LIGHTMAP_ON
    half4 vertexSH                  : TEXCOORD7;
#endif

    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Vertex: Used for Standard and StandardSimpleLighting shaders
LightweightVertexOutput LitPassVertex(LightweightVertexInput v)
{
    LightweightVertexOutput o = (LightweightVertexOutput)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv01.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
#ifdef LIGHTMAP_ON
    o.uv01.zw = v.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

    float4 positionWS = mul(unity_ObjectToWorld, v.vertex);
    half3 viewDirectionWS = SafeNormalize(_WorldSpaceCameraPos - positionWS.xyz);

#if _NORMALMAP
    OutputTangentToWorld(v.tangent, v.normal, o.tangent, o.binormal, o.normal);
#else
    o.normal = UnityObjectToWorldNormal(v.normal);
#endif

    float4 clipPos = mul(UNITY_MATRIX_VP, positionWS);

#ifndef LIGHTMAP_ON
    o.vertexSH = half4(EvaluateSHPerVertex(o.normal), 0.0);
#endif

    o.posWS = positionWS;
    o.viewDir = viewDirectionWS;
    o.fogFactorAndVertexLight.yzw = VertexLighting(positionWS.xyz, o.normal);
    o.fogFactorAndVertexLight.x = ComputeFogFactor(clipPos.z);
    o.clipPos = clipPos;

    return o;
}

// Used for Standard shader
half4 LitPassFragment(LightweightVertexOutput IN) : SV_Target
{
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(IN.uv01.xy, surfaceData);

#if _NORMALMAP
    half3 normalWS = TangentToWorldNormal(surfaceData.normal, IN.tangent, IN.binormal, IN.normal);
#else
    half3 normalWS = normalize(IN.normal);
#endif

#if LIGHTMAP_ON
    half3 indirectDiffuse = SampleLightmap(IN.uv01.zw, normalWS);
#else
    half3 indirectDiffuse = EvaluateSHPerPixel(normalWS, IN.vertexSH);
#endif

    float fogFactor = IN.fogFactorAndVertexLight.x;
    half4 color = LightweightFragmentPBR(IN.posWS, normalWS, IN.viewDir, indirectDiffuse, IN.fogFactorAndVertexLight.yzw, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);

    // Computes fog factor per-vertex
    ApplyFog(color.rgb, fogFactor);
    return OUTPUT_COLOR(color);
}

// Used for StandardSimpleLighting shader
half4 LitPassFragmentSimple(LightweightVertexOutput IN) : SV_Target
{
    float2 uv = IN.uv01.xy;
    float2 lightmapUV = IN.uv01.zw;

    half4 diffuseAlpha = tex2D(_MainTex, uv);
    half3 diffuse = LIGHTWEIGHT_GAMMA_TO_LINEAR(diffuseAlpha.rgb) * _Color.rgb;

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    half alpha = _Color.a;
#else
    half alpha = diffuseAlpha.a * _Color.a;
#endif

#ifdef _ALPHATEST_ON
    clip(alpha - _Cutoff);
#endif

#if _NORMALMAP
    half3 normalTangent = Normal(uv);
    half3 normalWS = TangentToWorldNormal(normalTangent, IN.tangent, IN.binormal, IN.normal);
#else
    half3 normalWS = normalize(IN.normal);
#endif

    half3 emission = Emission(uv);

    half3 viewDirectionWS = SafeNormalize(IN.viewDir.xyz);
    float3 positionWS = IN.posWS.xyz;

#if defined(LIGHTMAP_ON)
    half3 diffuseGI = SampleLightmap(lightmapUV, normalWS);
#else
    half3 diffuseGI = EvaluateSHPerPixel(normalWS, IN.vertexSH);
#endif

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
