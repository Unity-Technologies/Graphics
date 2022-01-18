#if SHADERPASS != SHADERPASS_VISIBILITY_OCCLUSION_CULLING
#error SHADERPASS_is_not_correctly_define
#endif

#ifndef ATTRIBUTES_NEED_VERTEX_ID
    #error Attributes_requires_vertex_id
#endif

#define CreateProceduralPositionOS BoundingBoxPosition

static const uint kVerticesPerInstance = 6;

float3 BoundingBoxPosition(AttributesMesh input)
{
    uint vid = input.vertexIndex;
    uint instanceIndex = vid / kVerticesPerInstance;
    uint vertexIndex = vid % kVerticesPerInstance;
    return 0;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

struct OcclusionVaryings
{
    uint4 instanceIndex : COLOR0;
    SV_POSITION_QUALIFIERS float4 positionCS : SV_Position;
};

OcclusionVaryings VertOcclusion(AttributesMesh inputMesh)
{
    VaryingsMeshToPS vmesh = VertMesh(inputMesh);
    OcclusionVaryings o;

    o.instanceIndex = 0;
    o.positionCS = vmesh.positionCS;

    return o;
}

[earlydepthstencil]
void FragOcclusion(uint4 instanceIndex : COLOR0)
{
}
