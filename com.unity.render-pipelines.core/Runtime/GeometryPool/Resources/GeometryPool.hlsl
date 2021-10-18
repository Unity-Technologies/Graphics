#ifndef GEOMETRY_POOL_H
#define GEOMETRY_POOL_H

#include "Packages/com.unity.render-pipelines.core/Runtime/GeometryPool/Resources/GeometryPoolDefs.cs.hlsl"

ByteAddressBuffer _GeoPoolGlobalVertexBuffer;
ByteAddressBuffer _GeoPoolGlobalIndexBuffer;
StructuredBuffer<GeoPoolMetadataEntry> _GeoPoolGlobalMetadataBuffer;
float4 _GeoPoolGlobalParams;

namespace GeometryPool
{

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

    componentOffset = vertexBufferSize * GEO_POOL_NORMAL_BYTE_OFFSET;
    outputVertex.N = asfloat(vertexBuffer.Load3(componentOffset + vertexIndex * GEO_POOL_NORMAL_BYTE_SIZE));

    componentOffset = vertexBufferSize * GEO_POOL_TANGENT_BYTE_OFFSET;
    if ((vertexFlags & GEOPOOLINPUTFLAGS_HAS_TANGENT) != 0)
        outputVertex.T = asfloat(vertexBuffer.Load3(componentOffset + vertexIndex * GEO_POOL_TANGENT_BYTE_SIZE));
}

void LoadVertex(
    int vertexIndex,
    GeoPoolMetadataEntry metadata,
    out GeoPoolVertex outputVertex)
{
    LoadVertex(metadata.vertexOffset + vertexIndex, (int)_GeoPoolGlobalParams.x, 0xfffff, _GeoPoolGlobalVertexBuffer, outputVertex);
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

}

#endif
