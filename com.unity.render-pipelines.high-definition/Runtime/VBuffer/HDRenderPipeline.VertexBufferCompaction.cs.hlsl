//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef HDRENDERPIPELINE_VERTEXBUFFERCOMPACTION_CS_HLSL
#define HDRENDERPIPELINE_VERTEXBUFFERCOMPACTION_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.HDRenderPipeline+VisibilityBufferConstants:  static fields
//
#define CLUSTER_SIZE_IN_TRIANGLES (128)
#define CLUSTER_SIZE_IN_INDICES (384)

// Generated from UnityEngine.Rendering.HighDefinition.HDRenderPipeline+CompactVertex
// PackingRules = Exact
struct CompactVertex
{
    float3 pos;
    float2 uv;
    float2 uv1;
    float3 N;
    float4 T;
};

// Generated from UnityEngine.Rendering.HighDefinition.HDRenderPipeline+InstanceVData
// PackingRules = Exact
struct InstanceVData
{
    float4x4 localToWorld;
    uint materialData;
    uint chunkStartIndex;
    float4 lightmapST;
};


#endif
