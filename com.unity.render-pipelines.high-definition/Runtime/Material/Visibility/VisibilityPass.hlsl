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
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/AlixAwesomeVertAnimHack.hlsl"

//Declarations required by the attributes above

struct PackedVaryingsPassToPS
{
    uint batchID : ATTRIBUTE6;
#ifdef ENCODE_VIS_DEPTH
    float2 depthValue : ATTRIBUTE7;
#endif
};

struct VaryingsPassToPS
{
    uint batchID;
#ifdef ENCODE_VIS_DEPTH
    float2 depthValue;
#endif
};

PackedVaryingsPassToPS PackVaryingsPassToPS(VaryingsPassToPS vpass)
{
    PackedVaryingsPassToPS packedToPS;
    packedToPS.batchID = vpass.batchID;
#ifdef ENCODE_VIS_DEPTH
    packedToPS.depthValue = vpass.depthValue;
#endif
    return packedToPS;
}

float3 LoadPositionFromGeometryPool(AttributesMesh input)
{
    GeoPoolMetadataEntry metadata = _GeoPoolGlobalMetadataBuffer[(int)_DeferredMaterialInstanceData.x];
    GeoPoolVertex vertexData = GeometryPool::LoadVertex(input.vertexIndex, metadata);

#if ENABLE_HACK_VERTEX_ANIMATION
    AlixVertAnimHack::ApplyLocalAnim(vertexData);
#endif

    return vertexData.pos;
}

//required by VertMesh
#define CreateProceduralPositionOS LoadPositionFromGeometryPool
