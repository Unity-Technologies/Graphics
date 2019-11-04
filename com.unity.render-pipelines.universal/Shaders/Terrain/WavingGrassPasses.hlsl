#ifndef UNIVERSAL_WAVING_GRASS_PASSES_INCLUDED
#define UNIVERSAL_WAVING_GRASS_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

#ifdef _MAIN_LIGHT_SHADOWS
    float4 shadowCoord              : TEXCOORD6;
#endif
    half4 color                     : TEXCOORD7;

    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(GrassVertexOutput input, out InputData inputData)
{
    inputData.positionWS = input.posWSShininess.xyz;

    half3 viewDirWS = input.viewDir;
#if SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    inputData.normalWS = NormalizeNormalPerPixel(input.normal);

    inputData.viewDirectionWS = viewDirWS;
#ifdef _MAIN_LIGHT_SHADOWS
    inputData.shadowCoord = input.shadowCoord;
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
}

void InitializeVertData(GrassVertexInput input, inout GrassVertexOutput vertData)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);

    vertData.uv = input.texcoord;
    vertData.posWSShininess.xyz = vertexInput.positionWS;
    vertData.posWSShininess.w = 32;
    vertData.clipPos = vertexInput.positionCS;

    vertData.viewDir = GetCameraPositionWS() - vertexInput.positionWS;

#if !SHADER_QUALITY_NICE_HINT
    vertData.viewDir = SafeNormalize(vertData.viewDir);
#endif

    vertData.normal = TransformObjectToWorldNormal(input.normal);

    // We either sample GI from lightmap or SH.
    // Lightmap UV and vertex SH coefficients use the same interpolator ("float2 lightmapUV" for lightmap or "half3 vertexSH" for SH)
    // see DECLARE_LIGHTMAP_OR_SH macro.
    // The following funcions initialize the correct variable with correct data
    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, vertData.lightmapUV);
    OUTPUT_SH(vertData.normal, vertData.vertexSH);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, vertData.normal.xyz);
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    vertData.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#ifdef _MAIN_LIGHT_SHADOWS
    vertData.shadowCoord = GetShadowCoord(vertexInput);
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
half4 LitPassFragmentGrass(GrassVertexOutput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.uv;
    half4 diffuseAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex));
    half3 diffuse = diffuseAlpha.rgb * input.color.rgb;

    half alpha = diffuseAlpha.a;
    AlphaDiscard(alpha, _Cutoff);
    alpha *= input.color.a;

    half3 emission = 0;
    half4 specularGloss = 0.1;// SampleSpecularSmoothness(uv, diffuseAlpha.a, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap));
    half shininess = input.posWSShininess.w;

    InputData inputData;
    InitializeInputData(input, inputData);

    half4 color = UniversalFragmentBlinnPhong(inputData, diffuse, specularGloss, shininess, emission, alpha);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
};

struct VertexInput
{
    float4 position     : POSITION;
    half4 color         : COLOR;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float2 uv           : TEXCOORD0;
    half4 color         : TEXCOORD1;
    float4 clipPos      : SV_POSITION;
};

VertexOutput DepthOnlyVertex(VertexInput v)
{
    VertexOutput o = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);

    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    // MeshGrass v.color.a: 1 on top vertices, 0 on bottom vertices
    // _WaveAndDistance.z == 0 for MeshLit
    float waveAmount = v.color.a * _WaveAndDistance.z;
    o.color = TerrainWaveGrass(v.position, waveAmount, v.color);
    o.clipPos = TransformObjectToHClip(v.position.xyz);
    return o;
}

half4 DepthOnlyFragment(VertexOutput IN) : SV_TARGET
{
    Alpha(SampleAlbedoAlpha(IN.uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex)).a, IN.color, _Cutoff);
return 0;
}

#endif
