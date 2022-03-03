//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef GEOMETRYPOOLDEFS_CS_HLSL
#define GEOMETRYPOOLDEFS_CS_HLSL
//
// UnityEngine.Rendering.GeometryPoolConstants:  static fields
//
#define GEO_POOL_POS_BYTE_SIZE (12)
#define GEO_POOL_UV0BYTE_SIZE (8)
#define GEO_POOL_UV1BYTE_SIZE (8)
#define GEO_POOL_NORMAL_BYTE_SIZE (12)
#define GEO_POOL_TANGENT_BYTE_SIZE (12)
#define GEO_POOL_BATCH_INSTANCE_DATA_BYTE_SIZE (2)
#define GEO_POOL_BATCH_INSTANCES_PER_DWORD (2)
#define GEO_POOL_POS_BYTE_OFFSET (0)
#define GEO_POOL_UV0BYTE_OFFSET (12)
#define GEO_POOL_UV1BYTE_OFFSET (20)
#define GEO_POOL_NORMAL_BYTE_OFFSET (28)
#define GEO_POOL_TANGENT_BYTE_OFFSET (40)
#define GEO_POOL_INDEX_BYTE_SIZE (4)
#define GEO_POOL_VERTEX_BYTE_SIZE (64)

//
// UnityEngine.Rendering.GeoPoolInputFlags:  static fields
//
#define GEOPOOLINPUTFLAGS_NONE (0)
#define GEOPOOLINPUTFLAGS_HAS_UV1 (1)
#define GEOPOOLINPUTFLAGS_HAS_TANGENT (2)

// Generated from UnityEngine.Rendering.GeoPoolVertex
// PackingRules = Exact
struct GeoPoolVertex
{
    float3 pos;
    float2 uv;
    float2 uv1;
    float3 N;
    float3 T;
};

// Generated from UnityEngine.Rendering.GeoPoolSubMeshEntry
// PackingRules = Exact
struct GeoPoolSubMeshEntry
{
    int baseVertex;
    int indexStart;
    int indexCount;
    uint materialKey;
};

// Generated from UnityEngine.Rendering.GeoPoolMetadataEntry
// PackingRules = Exact
struct GeoPoolMetadataEntry
{
    int vertexOffset;
    int indexOffset;
    int subMeshLookupOffset;
    int subMeshEntryOffset_VertexFlags;
};

// Generated from UnityEngine.Rendering.GeoPoolBatchTableEntry
// PackingRules = Exact
struct GeoPoolBatchTableEntry
{
    int offset;
    int count;
};

//
// Accessors for UnityEngine.Rendering.GeoPoolSubMeshEntry
//
int GetBaseVertex(GeoPoolSubMeshEntry value)
{
    return value.baseVertex;
}
int GetIndexStart(GeoPoolSubMeshEntry value)
{
    return value.indexStart;
}
int GetIndexCount(GeoPoolSubMeshEntry value)
{
    return value.indexCount;
}
uint GetMaterialKey(GeoPoolSubMeshEntry value)
{
    return value.materialKey;
}
//
// Accessors for UnityEngine.Rendering.GeoPoolMetadataEntry
//
int GetVertexOffset(GeoPoolMetadataEntry value)
{
    return value.vertexOffset;
}
int GetIndexOffset(GeoPoolMetadataEntry value)
{
    return value.indexOffset;
}
int GetSubMeshLookupOffset(GeoPoolMetadataEntry value)
{
    return value.subMeshLookupOffset;
}
int GetSubMeshEntryOffset_VertexFlags(GeoPoolMetadataEntry value)
{
    return value.subMeshEntryOffset_VertexFlags;
}
//
// Accessors for UnityEngine.Rendering.GeoPoolBatchTableEntry
//
int GetOffset(GeoPoolBatchTableEntry value)
{
    return value.offset;
}
int GetCount(GeoPoolBatchTableEntry value)
{
    return value.count;
}

#endif
