//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef HDRENDERPIPELINE_VERTEXBUFFERCOMPACTION_CS_HLSL
#define HDRENDERPIPELINE_VERTEXBUFFERCOMPACTION_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.HDRenderPipeline+CompactVertex
// PackingRules = Exact
struct CompactVertex
{
    float3 pos;
    float2 uv;
    float3 N;
    float4 T;
};

// Generated from UnityEngine.Rendering.HighDefinition.HDRenderPipeline+InstanceVData
// PackingRules = Exact
struct InstanceVData
{
    float4x4 localToWorld;
    uint startIndex;
};


#endif
