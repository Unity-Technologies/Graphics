#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

//Attributes
#define ATTRIBUTE_NEEDS_PROCEDURAL_POSITION
#define ATTRIBUTES_NEED_VERTEX_ID
#define VARYINGS_NEED_PASS
#define VARYINGS_NEED_PRIMITIVEID

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GPUVisibilityBRG/VisibleClustersCommon.hlsl"

ByteAddressBuffer _GlobalVisibleClusters;

//Declarations required by the attributes above

struct PackedVaryingsPassToPS
{
    uint instanceID : ATTRIBUTE6;
    uint triangleIndex : ATTRIBUTE7;
    uint clusterIndex : ATTRIBUTE8;
};

struct VaryingsPassToPS
{
    uint instanceID;
    uint triangleIndex;
    uint clusterIndex;
};

struct VertexVisibilityInfo
{
    uint instanceID;
    uint indexOffset;
    uint clusterIndex;
};

PackedVaryingsPassToPS PackVaryingsPassToPS(VaryingsPassToPS vpass)
{
    PackedVaryingsPassToPS packedToPS;
    packedToPS.instanceID = vpass.instanceID;
    packedToPS.triangleIndex = vpass.triangleIndex;
    packedToPS.clusterIndex = vpass.clusterIndex;
    return packedToPS;
}

static VertexVisibilityInfo _VertexVisPassInfo;

void PrepareVisibilityPass(inout AttributesMesh input)
{
    uint instanceID;
    uint indexOffset;
    uint clusterIndex;

    VisibleClusters::LoadVisibleClusterInfo(
        _GlobalVisibleClusters,
        input.vertexIndex,
        instanceID, indexOffset, clusterIndex);

    #ifdef DOTS_INSTANCING_ON
        input.instanceID = _VertexVisPassInfo.instanceID;
    #endif

    _VertexVisPassInfo.instanceID = instanceID;
    _VertexVisPassInfo.indexOffset = indexOffset;
    _VertexVisPassInfo.clusterIndex = clusterIndex;
}

float3 LoadPositionFromGeometryPool(AttributesMesh input)
{
    GeoPoolMeshEntry meshEntry = GeometryPool::GetMeshEntry(asuint(_DeferredMaterialInstanceData.x));
    GeoPoolVertex vertexData = GeometryPool::LoadVertex(meshEntry, _VertexVisPassInfo.clusterIndex, _VertexVisPassInfo.indexOffset);
    return vertexData.pos;
}

//required by VertMesh
#define CreateProceduralPositionOS LoadPositionFromGeometryPool
