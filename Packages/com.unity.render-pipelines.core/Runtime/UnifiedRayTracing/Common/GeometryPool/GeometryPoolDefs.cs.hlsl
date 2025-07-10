#ifndef GEOMETRYPOOLDEFS_CS_HLSL
#define GEOMETRYPOOLDEFS_CS_HLSL

//#define GEOMETRY_POOL_USE_COMPRESSED_UVS

//
// UnityEngine.Rendering.UnifiedRayTracing.GeoPoolVertexAttribs:  static fields
//
#define GEOPOOLVERTEXATTRIBS_POSITION (1)
#define GEOPOOLVERTEXATTRIBS_NORMAL (2)
#define GEOPOOLVERTEXATTRIBS_UV0 (4)
#define GEOPOOLVERTEXATTRIBS_UV1 (8)

//
// UnityEngine.Rendering.UnifiedRayTracing.GeometryPoolConstants:  static fields
//
#define GEO_POOL_POS_BYTE_SIZE (12)
#ifdef GEOMETRY_POOL_USE_COMPRESSED_UVS
#define GEO_POOL_UV0BYTE_SIZE (4)
#define GEO_POOL_UV1BYTE_SIZE (4)
#else
#define GEO_POOL_UV0BYTE_SIZE (8)
#define GEO_POOL_UV1BYTE_SIZE (8)
#endif
#define GEO_POOL_NORMAL_BYTE_SIZE (4)

#define GEO_POOL_POS_BYTE_OFFSET (0)
#define GEO_POOL_UV0BYTE_OFFSET (GEO_POOL_POS_BYTE_SIZE)
#define GEO_POOL_UV1BYTE_OFFSET (GEO_POOL_POS_BYTE_SIZE+GEO_POOL_UV0BYTE_SIZE)
#define GEO_POOL_NORMAL_BYTE_OFFSET (GEO_POOL_POS_BYTE_SIZE+GEO_POOL_UV0BYTE_SIZE+GEO_POOL_UV1BYTE_SIZE)
#define GEO_POOL_INDEX_BYTE_SIZE (4)
#define GEO_POOL_VERTEX_BYTE_SIZE (GEO_POOL_NORMAL_BYTE_OFFSET+GEO_POOL_NORMAL_BYTE_SIZE)

// Generated from UnityEngine.Rendering.UnifiedRayTracing.GeoPoolMeshChunk
struct GeoPoolMeshChunk
{
    int indexOffset;
    int indexCount;
    int vertexOffset;
    int vertexCount;
};

// Generated from UnityEngine.Rendering.UnifiedRayTracing.GeoPoolVertex
struct GeoPoolVertex
{
    float3 pos;
    float2 uv0;
    float2 uv1;
    float3 N;
};


#endif
