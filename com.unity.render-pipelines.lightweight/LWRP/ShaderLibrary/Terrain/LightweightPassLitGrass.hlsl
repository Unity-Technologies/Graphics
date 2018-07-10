#ifndef LIGHTWEIGHT_PASS_LIT_INCLUDED
#define LIGHTWEIGHT_PASS_LIT_INCLUDED

#include "LWRP/ShaderLibrary/Lighting.hlsl"

struct GrassVertexInput
{
    float4 vertex       : POSITION;
    float3 normal       : NORMAL;
    float4 tangent      : TANGENT;
    half4 color         : COLOR;
    float2 texcoord     : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct GrassVertexOutput
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

    float4 posWSShininess           : TEXCOORD2;    // xyz: posWS, w: Shininess * 128

    half3  normal                   : TEXCOORD3;
    half3 viewDir                   : TEXCOORD4;

    half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light

#ifdef _SHADOWS_ENABLED
    float4 shadowCoord              : TEXCOORD6;
#endif
    half4 color                     : TEXCOORD7;

    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(GrassVertexOutput IN, out InputData inputData)
{
    inputData.positionWS = IN.posWSShininess.xyz;

    half3 viewDir = IN.viewDir;
    inputData.normalWS = FragmentNormalWS(IN.normal);

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

void InitializeVertData(GrassVertexInput IN, inout GrassVertexOutput vertData)
{
    vertData.uv = IN.texcoord;

    vertData.posWSShininess.xyz = TransformObjectToWorld(IN.vertex.xyz);
    vertData.posWSShininess.w = 32;
    vertData.clipPos = TransformWorldToHClip(vertData.posWSShininess.xyz);

    half3 viewDir = VertexViewDirWS(GetCameraPositionWS() - vertData.posWSShininess.xyz);
    vertData.viewDir = viewDir;
    // initializes o.normal and if _NORMALMAP also o.tangent and o.binormal
    OUTPUT_NORMAL(IN, vertData);

    // We either sample GI from lightmap or SH.
    // Lightmap UV and vertex SH coefficients use the same interpolator ("float2 lightmapUV" for lightmap or "half3 vertexSH" for SH)
    // see DECLARE_LIGHTMAP_OR_SH macro.
    // The following funcions initialize the correct variable with correct data
    OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, vertData.lightmapUV);
    OUTPUT_SH(vertData.normal.xyz, vertData.vertexSH);

    half3 vertexLight = VertexLighting(vertData.posWSShininess.xyz, vertData.normal.xyz);
    half fogFactor = ComputeFogFactor(vertData.clipPos.z);
    vertData.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#ifdef _SHADOWS_ENABLED
#if SHADOWS_SCREEN
    vertData.shadowCoord = ComputeShadowCoord(vertData.clipPos);
#else
    vertData.shadowCoord = TransformWorldToShadowCoord(vertData.posWSShininess.xyz);
#endif
#endif
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Grass: appdata_full usage
// color        - .xyz = color, .w = wave scale
// normal       - normal
// tangent.xy   - billboard extrusion
// texcoord     - UV coords
// texcoord1    - 2nd UV coords

GrassVertexOutput WavingGrassVert(GrassVertexInput v)
{
    GrassVertexOutput o = (GrassVertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    // MeshGrass v.color.a: 1 on top vertices, 0 on bottom vertices
    // _WaveAndDistance.z == 0 for MeshLit
    float waveAmount = v.color.a * _WaveAndDistance.z;
    o.color = TerrainWaveGrass (v.vertex, waveAmount, v.color);

    InitializeVertData(v, o);

    return o;
}

GrassVertexOutput WavingGrassBillboardVert(GrassVertexInput v)
{
    GrassVertexOutput o = (GrassVertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    TerrainBillboardGrass (v.vertex, v.tangent.xy);
    // wave amount defined by the grass height
    float waveAmount = v.tangent.y;
    o.color = TerrainWaveGrass (v.vertex, waveAmount, v.color);

    InitializeVertData(v, o);

    return o;
}

// Used for StandardSimpleLighting shader
half4 LitPassFragmentGrass(GrassVertexOutput IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);

    float2 uv = IN.uv;
    half4 diffuseAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_PARAM(_MainTex, sampler_MainTex));
    half3 diffuse = diffuseAlpha.rgb * IN.color.rgb;

    half alpha = diffuseAlpha.a;
    AlphaDiscard(alpha, _Cutoff);
    alpha *= IN.color.a;

    half3 emission = 0;
    half4 specularGloss = 0.1;// SampleSpecularGloss(uv, diffuseAlpha.a, _SpecColor, TEXTURE2D_PARAM(_SpecGlossMap, sampler_SpecGlossMap));
    half shininess = IN.posWSShininess.w;

    InputData inputData;
    InitializeInputData(IN, inputData);

    half4 color = LightweightFragmentBlinnPhong(inputData, diffuse, specularGloss, shininess, emission, alpha);
    ApplyFog(color.rgb, inputData.fogCoord);
    return color;
};

#endif
