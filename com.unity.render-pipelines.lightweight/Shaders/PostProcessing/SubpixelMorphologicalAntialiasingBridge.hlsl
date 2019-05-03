#ifndef LIGHTWEIGHT_POSTPROCESSING_SMAA_BRIDGE
#define LIGHTWEIGHT_POSTPROCESSING_SMAA_BRIDGE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/Shaders/PostProcessing/Common.hlsl"

#define SMAA_HLSL_4_1

TEXTURE2D(_InputTexture);
TEXTURE2D(_BlendTexture);
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

#include "Packages/com.unity.render-pipelines.lightweight/Shaders/PostProcessing/SubpixelMorphologicalAntialiasing.hlsl"

// ----------------------------------------------------------------------------------------
// Edge Detection

struct VaryingsEdge
{
    float4 positionCS    : SV_POSITION;
    float2 uv            : TEXCOORD0;
    float4 offsets[3]    : TEXCOORD1;
};

VaryingsEdge VertEdge(Attributes input)
{
    VaryingsEdge output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    SMAAEdgeDetectionVS(output.uv, output.offsets);
    return output;
}

float4 FragEdge(VaryingsEdge input) : SV_Target
{
    return float4(SMAAColorEdgeDetectionPS(input.uv, input.offsets, _InputTexture), 0.0, 0.0);
}

// ----------------------------------------------------------------------------------------
// Blend Weights Calculation

struct VaryingsBlend
{
    float4 positionCS    : SV_POSITION;
    float2 uv            : TEXCOORD0;
    float2 pixcoord      : TEXCOORD1;
    float4 offsets[3]    : TEXCOORD2;
};

VaryingsBlend VertBlend(Attributes input)
{
    VaryingsBlend output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    SMAABlendingWeightCalculationVS(output.uv, output.pixcoord, output.offsets);
    return output;
}

float4 FragBlend(VaryingsBlend input) : SV_Target
{
    return SMAABlendingWeightCalculationPS(input.uv, input.pixcoord, input.offsets, _InputTexture, _AreaTexture, _SearchTexture, 0);
}

// ----------------------------------------------------------------------------------------
// Neighborhood Blending

struct VaryingsNeighbor
{
    float4 positionCS    : SV_POSITION;
    float2 uv            : TEXCOORD0;
    float4 offset        : TEXCOORD1;
};

VaryingsNeighbor VertNeighbor(Attributes input)
{
    VaryingsNeighbor output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    SMAANeighborhoodBlendingVS(output.uv, output.offset);
    return output;
}

float4 FragNeighbor(VaryingsNeighbor input) : SV_Target
{
    return SMAANeighborhoodBlendingPS(input.uv, input.offset, _InputTexture, _BlendTexture);
}

#endif // LIGHTWEIGHT_POSTPROCESSING_SMAA_BRIDGE
