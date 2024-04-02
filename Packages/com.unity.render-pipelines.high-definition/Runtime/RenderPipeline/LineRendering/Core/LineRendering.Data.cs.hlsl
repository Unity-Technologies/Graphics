//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef LINERENDERING_DATA_CS_HLSL
#define LINERENDERING_DATA_CS_HLSL
//
// UnityEngine.Rendering.LineRendering+DebugMode:  static fields
//
#define DEBUGMODE_SEGMENTS_PER_TILE (0)
#define DEBUGMODE_TILE_PROCESSOR_UV (1)
#define DEBUGMODE_CLUSTER_DEPTH (2)

//
// UnityEngine.Rendering.LineRendering+ShaderVariables:  static fields
//
#define NUM_LANE_SEGMENT_SETUP (1024)
#define NUM_LANE_RASTER_BIN (512)

// Generated from UnityEngine.Rendering.LineRendering+ClusterRecord
// PackingRules = Exact
struct ClusterRecord
{
    uint segmentIndex;
    uint clusterIndex;
    uint clusterOffset;
};

// Generated from UnityEngine.Rendering.LineRendering+SegmentRecord
// PackingRules = Exact
struct SegmentRecord
{
    float2 positionSS0;
    float2 positionSS1;
    float depthVS0;
    float depthVS1;
    uint vertexIndex0;
    uint vertexIndex1;
};

// Generated from UnityEngine.Rendering.LineRendering+ShaderVariables
// PackingRules = Exact
CBUFFER_START(ShaderVariables)
    float4 _Params0;
    float4 _Params1;
    float4 _Params2;
    float4 _Params3;
    float4 _Params4;
    float4 _Params5;
CBUFFER_END

// Generated from UnityEngine.Rendering.LineRendering+VertexRecord
// PackingRules = Exact
struct VertexRecord
{
    float4 positionCS;
    float4 previousPositionCS;
    float3 positionRWS;
    float3 tangentWS;
    float3 normalWS;
    uint texCoord0;
    uint texCoord1;
};


#endif
