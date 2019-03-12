#ifndef UNITY_POSTFX_SMAA_BRIDGE
#define UNITY_POSTFX_SMAA_BRIDGE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

#define SMAA_HLSL_4_1

TEXTURE2D_X(_InputTexture);
TEXTURE2D_X(_BlendTex);
TEXTURE2D(_AreaTex);
TEXTURE2D(_SearchTex);

float4 _SMAARTMetrics;

#define SMAA_RT_METRICS _SMAARTMetrics
#define SMAA_AREATEX_SELECT(s) s.rg
#define SMAA_SEARCHTEX_SELECT(s) s.a
#define LinearSampler s_linear_clamp_sampler
#define PointSampler s_point_clamp_sampler
#define GAMMA_FOR_EDGE_DETECTION (1/2.2)

#include "SubpixelMorphologicalAntialiasing.hlsl"

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// ----------------------------------------------------------------------------------------
// Edge Detection

struct VaryingsEdge
{
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float4 offsets[3] : TEXCOORD1;
};

VaryingsEdge VertEdge(Attributes v)
{
    VaryingsEdge o;
    o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
    o.texcoord = GetFullScreenTriangleTexCoord(v.vertexID);

    SMAAEdgeDetectionVS(o.texcoord, o.offsets);

    return o;
}

float4 FragEdge(VaryingsEdge i) : SV_Target
{
    return float4(SMAAColorEdgeDetectionPS(i.texcoord, i.offsets, _InputTexture), 0.0, 0.0);
}

// ----------------------------------------------------------------------------------------
// Blend Weights Calculation

struct VaryingsBlend
{
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float2 pixcoord : TEXCOORD1;
    float4 offsets[3] : TEXCOORD2;
};

VaryingsBlend VertBlend(Attributes v)
{
    VaryingsBlend o;
    o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
    o.texcoord = GetFullScreenTriangleTexCoord(v.vertexID);

    SMAABlendingWeightCalculationVS(o.texcoord, o.pixcoord, o.offsets);

    return o;
}

float4 FragBlend(VaryingsBlend i) : SV_Target
{
    return SMAABlendingWeightCalculationPS(i.texcoord, i.pixcoord, i.offsets, _InputTexture, _AreaTex, _SearchTex, 0);
}

// ----------------------------------------------------------------------------------------
// Neighborhood Blending

struct VaryingsNeighbor
{
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float4 offset : TEXCOORD1;
};

VaryingsNeighbor VertNeighbor(Attributes v)
{
    VaryingsNeighbor o;
    o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
    o.texcoord = GetFullScreenTriangleTexCoord(v.vertexID);

    SMAANeighborhoodBlendingVS(o.texcoord, o.offset);
    return o;
}

float4 FragNeighbor(VaryingsNeighbor i) : SV_Target
{
    return SMAANeighborhoodBlendingPS(i.texcoord, i.offset, _InputTexture, _BlendTex);
}

#endif
