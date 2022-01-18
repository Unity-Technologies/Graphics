#if SHADERPASS != SHADERPASS_VISIBILITY_OCCLUSION_CULLING
#error SHADERPASS_is_not_correctly_define
#endif

#ifndef ATTRIBUTES_NEED_VERTEX_ID
    #error Attributes_requires_vertex_id
#endif

#define CreateProceduralPositionOS BoundingBoxPosition

static const uint kVerticesPerInstance = 6;

ByteAddressBuffer instanceData;
RWByteAddressBuffer instanceVisibilityBitfield : register(u1);
uint instancePositionMetadata;

float4x4 LoadDOTSInstancedData_float4x4_from_float3x4_customBuffer(uint metadata)
{
#ifdef DOTS_INSTANCING_ON
    uint address = ComputeDOTSInstanceDataAddress(metadata, 3 * 16);
#else
    uint address = 0;
#endif
    float4 p1 = asfloat(instanceData.Load4(address + 0 * 16));
    float4 p2 = asfloat(instanceData.Load4(address + 1 * 16));
    float4 p3 = asfloat(instanceData.Load4(address + 2 * 16));

    return float4x4(
        p1.x, p1.w, p2.z, p3.y,
        p1.y, p2.x, p2.w, p3.z,
        p1.z, p2.y, p3.x, p3.w,
        0.0,  0.0,  0.0,  1.0
    );
}

float4x4 LoadObjectToWorld(uint positionMetadata, uint instanceIndex)
{
#ifdef DOTS_INSTANCING_ON
    DOTSVisibleData dotsVisData;
    dotsVisData.VisibleData = uint4(instanceIndex, 0, 0, 0);
    unity_SampledDOTSVisibleData = dotsVisData;

#ifdef MODIFY_MATRIX_FOR_CAMERA_RELATIVE_RENDERING
    return ApplyCameraTranslationToMatrix(LoadDOTSInstancedData_float4x4_from_float3x4_customBuffer(positionMetadata));
#else
    return LoadDOTSInstancedData_float4x4_from_float3x4_customBuffer(positionMetadata);
#endif
#else
    return 0;
#endif
}

static const float4 BoxVerts[kVerticesPerInstance] =
{
    float4(1, 0, 0, 1),
    float4(1, 1, 0, 1),
    float4(0, 0, 0, 1),
    float4(0, 1, 0, 1),
    float4(0, 1, 1, 1),
    float4(0, 0, 0, 1),
};

float3 BoundingBoxPosition(AttributesMesh input)
{
    uint vid = input.vertexIndex;
    uint instanceIndex = vid / kVerticesPerInstance;
    uint vertexIndex = vid % kVerticesPerInstance;
    float4x4 objectToWorld = LoadObjectToWorld(instancePositionMetadata, instanceIndex);
    float3 worldPosition = mul(objectToWorld, BoxVerts[vertexIndex]).xyz;
    return worldPosition;
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

    uint vid = inputMesh.vertexIndex;
    o.instanceIndex = vid / kVerticesPerInstance;
    o.positionCS = vmesh.positionCS;

    return o;
}

[earlydepthstencil]
void FragOcclusion(uint4 instanceIndexVarying : COLOR0)
{
    uint instanceIndex = instanceIndexVarying.x;

    uint instanceDword = instanceIndex >> 5;
    uint bitIndex = instanceIndex & 0x1f;
    uint mask = 1 << bitIndex;

    uint value = instanceVisibilityBitfield.Load(instanceDword);
    if (!(value & mask))
    {
        instanceVisibilityBitfield.InterlockedOr(instanceDword, mask);
    }
}
