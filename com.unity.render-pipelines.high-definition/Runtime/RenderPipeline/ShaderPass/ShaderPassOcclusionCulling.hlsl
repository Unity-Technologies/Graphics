#if SHADERPASS != SHADERPASS_VISIBILITY_OCCLUSION_CULLING
#error SHADERPASS_is_not_correctly_define
#endif

ByteAddressBuffer inputVisibleInstanceData;
RWByteAddressBuffer instanceVisibilityBitfield : register(u2);

#if defined(PROCEDURAL_CUBE)

#ifndef ATTRIBUTES_NEED_VERTEX_ID
    #error Attributes_requires_vertex_id
#endif

#define CreateProceduralPositionOS BoundingBoxPositionOS

static const uint kVerticesPerInstance = 6;

static const float4 BoxVerts[kVerticesPerInstance] =
{
    float4(1, 0, 0, 1),
    float4(1, 1, 0, 1),
    float4(0, 0, 0, 1),
    float4(0, 1, 0, 1),
    float4(0, 1, 1, 1),
    float4(0, 0, 0, 1),
};

float3 BoundingBoxPositionOS(AttributesMesh input)
{
    uint vid = input.vertexIndex;
    uint instanceIndex = vid / kVerticesPerInstance;
    uint vertexIndex = vid % kVerticesPerInstance;
    return TransformBoundsVertex(BoxVerts[vertexIndex].xyz);
}
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

struct OcclusionVaryings
{
    uint4 instanceID : COLOR0;
    SV_POSITION_QUALIFIERS float4 positionCS : SV_Position;
};

MeshBounds GetMeshBounds(uint instanceID, uint batchID)
{
    GeoPoolMetadataEntry metadata = GeometryPool::GetMetadataEntry(instanceID, batchID);
    MeshBounds bounds;
    bounds.center = metadata.boundsCenter;
    bounds.extents = metadata.boundsExtents;
    return bounds;
}

OcclusionVaryings VertOcclusion(AttributesMesh input, uint svInstanceId : SV_InstanceID)
{
#if defined(PROCEDURAL_CUBE)
    uint vid = input.vertexIndex;
    uint instanceIndex = vid / kVerticesPerInstance;
#else
    uint instanceIndex = svInstanceId;
    input.positionOS *= 2; // Unity Cube goes from 0 to 0.5, scale it from 0 to 1
#endif
    occlusion_instanceID = inputVisibleInstanceData.Load(instanceIndex << 2);
    occlusion_meshBounds = GetMeshBounds(occlusion_instanceID, instanceBatchID);

    VaryingsMeshToPS vmesh = VertMesh(input);

    OcclusionVaryings o;
    o.instanceID = uint4(instanceIndex, occlusion_instanceID, 0, 0);
    o.positionCS = vmesh.positionCS;

    return o;
}

struct PSVaryings
{
    uint4 instanceIDVarying : COLOR0;
#if defined(DEBUG_OUTPUT)
    float4 svPosition : SV_Position;
#endif
};

[earlydepthstencil]
#if defined(DEBUG_OUTPUT)
float4 FragOcclusion(PSVaryings v) : SV_Target
#else
void FragOcclusion(PSVaryings v)
#endif
{
    uint instanceIndex = v.instanceIDVarying.x;
    uint instanceID = v.instanceIDVarying.y;

    uint instanceDword =  instanceIndex >> 5;
    uint bitIndex =  instanceIndex & 0x1f;
    uint mask = 1 << bitIndex;
    uint dwordAddress = instanceDword << 2;

    uint value = instanceVisibilityBitfield.Load(dwordAddress);
    if (!(value & mask))
    {
        instanceVisibilityBitfield.InterlockedOr(dwordAddress, mask);
    }

#if defined(DEBUG_OUTPUT)
    return v.svPosition.z;
#endif
}
