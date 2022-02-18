#ifndef UNIVERSAL_POSTPROCESSING_SMAA_BRIDGE
#define UNIVERSAL_POSTPROCESSING_SMAA_BRIDGE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

#define SMAA_HLSL_4_1

#if _SMAA_PRESET_LOW
    #define SMAA_PRESET_LOW
#elif _SMAA_PRESET_MEDIUM
    #define SMAA_PRESET_MEDIUM
#else
    #define SMAA_PRESET_HIGH
#endif

TEXTURE2D_X(_ColorTexture);
TEXTURE2D_X(_BlendTexture);
TEXTURE2D(_AreaTexture);
TEXTURE2D(_SearchTexture);

float4 _Metrics;

#define SMAA_RT_METRICS _Metrics
#define SMAA_AREATEX_SELECT(s) s.rg
#define SMAA_SEARCHTEX_SELECT(s) s.a
#define LinearSampler sampler_LinearClamp
#define PointSampler sampler_PointClamp

#if UNITY_COLORSPACE_GAMMA
    #define GAMMA_FOR_EDGE_DETECTION (1)
#else
    #define GAMMA_FOR_EDGE_DETECTION (1/2.2)
#endif

#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/SubpixelMorphologicalAntialiasing.hlsl"

// ----------------------------------------------------------------------------------------
// Edge Detection

struct VaryingsEdge
{
    float4 positionCS    : SV_POSITION;
    float2 uv            : TEXCOORD0;
    float4 offsets[3]    : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsEdge VertEdge(Attributes input)
{
    VaryingsEdge output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if _USE_DRAW_PROCEDURAL
    GetProceduralQuad(input.vertexID, output.positionCS, output.uv);
#else
    output.positionCS = TransformFullscreenMesh(input.positionOS.xyz);
    output.uv = UnityStereoTransformScreenSpaceTex(input.uv);
#endif
    SMAAEdgeDetectionVS(output.uv, output.offsets);
    return output;
}

float4 FragEdge(VaryingsEdge input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return float4(SMAAColorEdgeDetectionPS(input.uv, input.offsets, _ColorTexture), 0.0, 0.0);
}

// ----------------------------------------------------------------------------------------
// Blend Weights Calculation

struct VaryingsBlend
{
    float4 positionCS    : SV_POSITION;
    float2 uv            : TEXCOORD0;
    float2 pixcoord      : TEXCOORD1;
    float4 offsets[3]    : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsBlend VertBlend(Attributes input)
{
    VaryingsBlend output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if _USE_DRAW_PROCEDURAL
    GetProceduralQuad(input.vertexID, output.positionCS, output.uv);
#else
    output.positionCS = TransformFullscreenMesh(input.positionOS.xyz);
    output.uv = UnityStereoTransformScreenSpaceTex(input.uv);
#endif
    SMAABlendingWeightCalculationVS(output.uv, output.pixcoord, output.offsets);
    return output;
}

float4 FragBlend(VaryingsBlend input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return SMAABlendingWeightCalculationPS(input.uv, input.pixcoord, input.offsets, _ColorTexture, _AreaTexture, _SearchTexture, 0);
}

// ----------------------------------------------------------------------------------------
// Neighborhood Blending

struct VaryingsNeighbor
{
    float4 positionCS    : SV_POSITION;
    float2 uv            : TEXCOORD0;
    float4 offset        : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsNeighbor VertNeighbor(Attributes input)
{
    VaryingsNeighbor output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if _USE_DRAW_PROCEDURAL
    GetProceduralQuad(input.vertexID, output.positionCS, output.uv);
#else
    output.positionCS = TransformFullscreenMesh(input.positionOS.xyz);
    output.uv = UnityStereoTransformScreenSpaceTex(input.uv);
#endif
    SMAANeighborhoodBlendingVS(output.uv, output.offset);
    return output;
}

float4 FragNeighbor(VaryingsNeighbor input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return SMAANeighborhoodBlendingPS(input.uv, input.offset, _ColorTexture, _BlendTexture);
}

#endif // UNIVERSAL_POSTPROCESSING_SMAA_BRIDGE
