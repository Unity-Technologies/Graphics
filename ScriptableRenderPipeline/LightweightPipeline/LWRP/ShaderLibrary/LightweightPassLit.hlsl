#ifndef LIGHTWEIGHT_PASS_LIT_INCLUDED
#define LIGHTWEIGHT_PASS_LIT_INCLUDED

#include "LWRP/ShaderLibrary/Lighting.hlsl"

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
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

#ifdef _ADDITIONAL_LIGHTS
    float3 posWS                    : TEXCOORD2;
#endif

#ifdef _NORMALMAP
    half4 normal                    : TEXCOORD3;    // xyz: normal, w: viewDir.x
    half4 tangent                   : TEXCOORD4;    // xyz: tangent, w: viewDir.y
    half4 binormal                  : TEXCOORD5;    // xyz: binormal, w: viewDir.z
#else
    half3  normal                   : TEXCOORD3;
    half3 viewDir                   : TEXCOORD4;
#endif

    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

#ifdef _SHADOWS_ENABLED
    float4 shadowCoord              : TEXCOORD7;
#endif

    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(LightweightVertexOutput IN, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#ifdef _ADDITIONAL_LIGHTS
    inputData.positionWS = IN.posWS;
#endif

#ifdef _NORMALMAP
    half3 viewDir = half3(IN.normal.w, IN.tangent.w, IN.binormal.w);
    inputData.normalWS = TangentToWorldNormal(normalTS, IN.tangent.xyz, IN.binormal.xyz, IN.normal.xyz);
#else
    half3 viewDir = IN.viewDir;
    inputData.normalWS = FragmentNormalWS(IN.normal);
#endif

    inputData.viewDirectionWS = FragmentViewDirWS(viewDir);
#ifdef _SHADOWS_ENABLED
    inputData.shadowCoord = IN.shadowCoord;
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    inputData.fogCoord = IN.fogFactorAndVertexLight.x;
    inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, inputData.normalWS);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
LightweightVertexOutput LitPassVertex(LightweightVertexInput v)
{
    LightweightVertexOutput o = (LightweightVertexOutput)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

    float3 posWS = TransformObjectToWorld(v.vertex.xyz);
    o.clipPos = TransformWorldToHClip(posWS);

    half3 viewDir = VertexViewDirWS(GetCameraPositionWS() - posWS);

#ifdef _NORMALMAP
    o.normal.w = viewDir.x;
    o.tangent.w = viewDir.y;
    o.binormal.w = viewDir.z;
#else
    o.viewDir = viewDir;
#endif

    // initializes o.normal and if _NORMALMAP also o.tangent and o.binormal
    OUTPUT_NORMAL(v, o);

    // We either sample GI from lightmap or SH.
    // Lightmap UV and vertex SH coefficients use the same interpolator ("float2 lightmapUV" for lightmap or "half3 vertexSH" for SH)
    // see DECLARE_LIGHTMAP_OR_SH macro.
    // The following funcions initialize the correct variable with correct data
    OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
    OUTPUT_SH(o.normal.xyz, o.vertexSH);

    half3 vertexLight = VertexLighting(posWS, o.normal.xyz);
    half fogFactor = ComputeFogFactor(o.clipPos.z);
    o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#ifdef _SHADOWS_ENABLED
#if SHADOWS_SCREEN
    o.shadowCoord = ComputeShadowCoord(o.clipPos);
#else
    o.shadowCoord = TransformWorldToShadowCoord(posWS);
#endif
#endif

#ifdef _ADDITIONAL_LIGHTS
    o.posWS = posWS;
#endif

    return o;
}

// Used in Standard (Physically Based) shader
half4 LitPassFragment(LightweightVertexOutput IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(IN.uv, surfaceData);

    InputData inputData;
    InitializeInputData(IN, surfaceData.normalTS, inputData);

    half4 color = LightweightFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);

    ApplyFog(color.rgb, inputData.fogCoord);
    return color;
}

#endif
