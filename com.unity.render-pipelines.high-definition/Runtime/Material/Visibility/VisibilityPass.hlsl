#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

//Attributes
#define ATTRIBUTE_NEEDS_PROCEDURAL_POSITION
#define ATTRIBUTES_NEED_VERTEX_ID
#if SHADERPASS != SHADERPASS_VISIBILITY_OCCLUSION_CULLING
#define VARYINGS_NEED_PASS
#define VARYINGS_NEED_PRIMITIVEID
#endif

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"

//Declarations required by the attributes above

struct PackedVaryingsPassToPS
{
    uint batchID : ATTRIBUTE6;
};

struct VaryingsPassToPS
{
    uint batchID;
};

PackedVaryingsPassToPS PackVaryingsPassToPS(VaryingsPassToPS vpass)
{
    PackedVaryingsPassToPS packedToPS;
    packedToPS.batchID = vpass.batchID;
    return packedToPS;
}

float3 LoadPositionFromGeometryPool(AttributesMesh input)
{
    GeoPoolMetadataEntry metadata = _GeoPoolGlobalMetadataBuffer[(int)_DeferredMaterialInstanceData.x];
    GeoPoolVertex vertexData = GeometryPool::LoadVertex(input.vertexIndex, metadata);
    return vertexData.pos;
}

//required by VertMesh
#if SHADERPASS != SHADERPASS_VISIBILITY_OCCLUSION_CULLING
#define CreateProceduralPositionOS LoadPositionFromGeometryPool
#endif
