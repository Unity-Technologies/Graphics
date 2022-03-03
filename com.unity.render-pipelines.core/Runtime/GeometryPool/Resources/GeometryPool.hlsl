#ifndef GEOMETRY_POOL_H
#define GEOMETRY_POOL_H

#include "Packages/com.unity.render-pipelines.core/Runtime/GeometryPool/Resources/GeometryPoolDefs.cs.hlsl"

ByteAddressBuffer _GeoPoolGlobalVertexBuffer;
ByteAddressBuffer _GeoPoolGlobalIndexBuffer;
ByteAddressBuffer _GeoPoolGlobalSubMeshLookupBuffer;
StructuredBuffer<GeoPoolSubMeshEntry> _GeoPoolGlobalSubMeshEntryBuffer;
StructuredBuffer<GeoPoolMetadataEntry> _GeoPoolGlobalMetadataBuffer;
StructuredBuffer<GeoPoolBatchTableEntry> _GeoPoolGlobalBatchTableBuffer;
ByteAddressBuffer _GeoPoolGlobalBatchInstanceBuffer;
float4 _GeoPoolGlobalParams;

namespace GeometryPool
{

int getSubMeshEntryOffset(in GeoPoolMetadataEntry metadata)
{
    return (metadata.subMeshEntryOffset_VertexFlags >> 16);
}

int getGeoPoolInputFlags(in GeoPoolMetadataEntry metadata)
{
    return (metadata.subMeshEntryOffset_VertexFlags & 0xFFFF);
}

void StoreVertex(
    int vertexIndex,
    in GeoPoolVertex vertex,
    int vertexFlags,
    int outputBufferSize,
    RWByteAddressBuffer output)
{
    int componentOffset = outputBufferSize * GEO_POOL_POS_BYTE_OFFSET;
    output.Store3(componentOffset + vertexIndex * GEO_POOL_POS_BYTE_SIZE, asuint(vertex.pos));

    componentOffset = outputBufferSize * GEO_POOL_UV0BYTE_OFFSET;
    output.Store2(componentOffset + vertexIndex * GEO_POOL_UV0BYTE_SIZE, asuint(vertex.uv));

    componentOffset = outputBufferSize * GEO_POOL_UV1BYTE_OFFSET;
    if ((vertexFlags & GEOPOOLINPUTFLAGS_HAS_UV1) != 0)
        output.Store2(componentOffset + vertexIndex * GEO_POOL_UV1BYTE_SIZE, asuint(vertex.uv1));

    componentOffset = outputBufferSize * GEO_POOL_COL_BYTE_OFFSET;
    if ((vertexFlags & GEOPOOLINPUTFLAGS_HAS_COLOR) != 0)
        output.Store3(componentOffset + vertexIndex * GEO_POOL_COL_BYTE_SIZE, asuint(vertex.C));

    componentOffset = outputBufferSize * GEO_POOL_NORMAL_BYTE_OFFSET;
    output.Store3(componentOffset + vertexIndex * GEO_POOL_NORMAL_BYTE_SIZE, asuint(vertex.N));

    componentOffset = outputBufferSize * GEO_POOL_TANGENT_BYTE_OFFSET;
    if ((vertexFlags & GEOPOOLINPUTFLAGS_HAS_TANGENT) != 0)
        output.Store3(componentOffset + vertexIndex * GEO_POOL_TANGENT_BYTE_SIZE, asuint(vertex.T));
}

void LoadVertex(
    int vertexIndex,
    int vertexBufferSize,
    int vertexFlags,
    ByteAddressBuffer vertexBuffer,
    out GeoPoolVertex outputVertex)
{
    int componentOffset = vertexBufferSize * GEO_POOL_POS_BYTE_OFFSET;
    outputVertex.pos = asfloat(vertexBuffer.Load3(componentOffset + vertexIndex * GEO_POOL_POS_BYTE_SIZE));

    componentOffset = vertexBufferSize * GEO_POOL_UV0BYTE_OFFSET;
    outputVertex.uv = asfloat(vertexBuffer.Load2(componentOffset + vertexIndex * GEO_POOL_UV0BYTE_SIZE));

    componentOffset = vertexBufferSize * GEO_POOL_UV1BYTE_OFFSET;
    if ((vertexFlags & GEOPOOLINPUTFLAGS_HAS_UV1) != 0)
        outputVertex.uv1 = asfloat(vertexBuffer.Load2(componentOffset + vertexIndex * GEO_POOL_UV1BYTE_SIZE));
    else
        outputVertex.uv1 = outputVertex.uv;

    componentOffset = vertexBufferSize * GEO_POOL_COL_BYTE_OFFSET;
    if ((vertexFlags & GEOPOOLINPUTFLAGS_HAS_COLOR) != 0)
    {
        uint packedRGB = vertexBuffer.Load(componentOffset + vertexIndex * GEO_POOL_COL_BYTE_SIZE);
        outputVertex.C = float3(float(packedRGB & 0xFF)/255.0,float((packedRGB >> 8) & 0xFF)/255.0,float((packedRGB >> 16) & 0xFF)/255.0);
    }
    else
        outputVertex.C = float3(0, 0, 0);

    componentOffset = vertexBufferSize * GEO_POOL_NORMAL_BYTE_OFFSET;
    outputVertex.N = asfloat(vertexBuffer.Load3(componentOffset + vertexIndex * GEO_POOL_NORMAL_BYTE_SIZE));

    componentOffset = vertexBufferSize * GEO_POOL_TANGENT_BYTE_OFFSET;
    if ((vertexFlags & GEOPOOLINPUTFLAGS_HAS_TANGENT) != 0)
        outputVertex.T = asfloat(vertexBuffer.Load3(componentOffset + vertexIndex * GEO_POOL_TANGENT_BYTE_SIZE));
}

GeoPoolVertex LoadVertex(
    int vertexIndex,
    GeoPoolMetadataEntry metadata)
{
    GeoPoolVertex outputVertex;
    LoadVertex(metadata.vertexOffset + vertexIndex, (int)_GeoPoolGlobalParams.x, getGeoPoolInputFlags(metadata), _GeoPoolGlobalVertexBuffer, outputVertex);
    return outputVertex;
}

void SubMeshLookupBucketShiftMask(int index, out int bucketId, out int shift, out int mask)
{
    bucketId = index >> 2;
    shift = (index & 0x3) << 3;
    mask = 0xff;
}

void PackSubMeshLookup(int index, int value, out int bucketId, out uint packedValue)
{
    int shift, mask;
    SubMeshLookupBucketShiftMask(index, bucketId, shift, mask);
    packedValue = (uint)(value & mask) << (uint)shift;
}

uint GetMaterialKey(GeoPoolMetadataEntry metadata, uint primitiveID, ByteAddressBuffer subMeshLookupBuffer, StructuredBuffer<GeoPoolSubMeshEntry> subMeshEntryBuffer)
{
    int primitiveBucket, primitiveShift, primitiveMask;
    SubMeshLookupBucketShiftMask(metadata.subMeshLookupOffset + primitiveID, primitiveBucket, primitiveShift, primitiveMask);
    int submeshEntryIndex = ((int)subMeshLookupBuffer.Load(primitiveBucket << 2) >> primitiveShift) & primitiveMask;
    GeoPoolSubMeshEntry submeshEntry = subMeshEntryBuffer[getSubMeshEntryOffset(metadata) + submeshEntryIndex];
    return submeshEntry.materialKey;
}

uint GetMaterialKey(GeoPoolMetadataEntry metadata, uint primitiveID)
{
    return GetMaterialKey(metadata, primitiveID, _GeoPoolGlobalSubMeshLookupBuffer, _GeoPoolGlobalSubMeshEntryBuffer);
}

GeoPoolMetadataEntry GetMetadataEntry(int instanceID, int batchID)
{
    GeoPoolBatchTableEntry tableEntry = _GeoPoolGlobalBatchTableBuffer[batchID];
    uint globalInstanceIndex = tableEntry.offset + instanceID;
    uint pair = _GeoPoolGlobalBatchInstanceBuffer.Load((globalInstanceIndex >> 1) << 2);
    uint metadataIdx = (globalInstanceIndex & 0x1) ? (pair >> 16) : (pair & 0xFF);
    return _GeoPoolGlobalMetadataBuffer[metadataIdx];
}

}

#endif
