#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

//Attributes
#define ATTRIBUTE_NEEDS_PROCEDURAL_POSITION
#define ATTRIBUTES_NEED_VERTEX_ID

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"

float3 LoadPositionFromGeometryPool(AttributesMesh input)
{
    GeoPoolMetadataEntry metadata = _GeoPoolGlobalMetadataBuffer[(int)_DeferredMaterialInstanceData.x];
    GeoPoolVertex vertexData = GeometryPool::LoadVertex(input.vertexIndex, metadata);
    return vertexData.pos;
}

//required by VertMesh
#define CreateProceduralPositionOS LoadPositionFromGeometryPool

//empty functions to satisfy ShaderPassDepthOnly.hlsl
struct SurfaceData {};
struct BuiltinData {};
void GetSurfaceAndBuiltinData(in FragInputs input, float3 V, in PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData) {}
