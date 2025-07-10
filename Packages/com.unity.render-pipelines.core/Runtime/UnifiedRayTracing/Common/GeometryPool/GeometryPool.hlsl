#ifndef GEOMETRY_POOL_H
#define GEOMETRY_POOL_H

namespace GeometryPool
{

float2 msign(float2 v)
{
    return float2(
        (v.x >= 0.0) ? 1.0 : -1.0,
        (v.y >= 0.0) ? 1.0 : -1.0);
}

uint NormalToOctahedral32(float3 normal)
{
    normal.xy /= (abs(normal.x) + abs(normal.y) + abs(normal.z));
    normal.xy = (normal.z >= 0.0) ? normal.xy : (1.0 - abs(normal.yx)) * msign(normal.xy);

    uint2 d = uint2(round(32767.5 + normal.xy * 32767.5));
    return d.x | (d.y << 16u);
}

float3 Octahedral32ToNormal(uint data)
{
    uint2 iv = uint2(data, data >> 16u) & 65535u;
    float2 v = float2(iv) / 32767.5 - 1.0;

    float3 normal = float3(v, 1.0 - abs(v.x) - abs(v.y));
    float t = max(-normal.z, 0.0);
    normal.x += (normal.x > 0.0) ? -t : t;
    normal.y += (normal.y > 0.0) ? -t : t;

    return normalize(normal);
}

uint UvsToUint32(float2 uv)
{
    return (uint(uv.x * 65535.0f) & 0xFFFF) | (uint(uv.y * 65535.0f) << 16);
}

float2 Uint32ToUvs(uint data)
{
    return float2((data & 0xFFFF) * (1.0f/65535.0f), (data >> 16u) * (1.0f/65535.0f));
}

void StoreUvs(RWStructuredBuffer<uint> output, uint index, float2 uv)
{
#ifdef GEOMETRY_POOL_USE_COMPRESSED_UVS
    output[index] = UvsToUint32(uv);
#else
    output[index] = asuint(uv.x);
    output[index + 1] = asuint(uv.y);
#endif
}

float2 LoadUvs(StructuredBuffer<uint> vertexBuffer, uint index)
{
#ifdef GEOMETRY_POOL_USE_COMPRESSED_UVS
    return Uint32ToUvs(vertexBuffer[index]);
#else
    return asfloat(uint2(vertexBuffer[index], vertexBuffer[index + 1]));
#endif
}


void StoreVertex(
    uint vertexIndex,
    in GeoPoolVertex vertex,
    int outputBufferSize,
    RWStructuredBuffer<uint> output)
{
    uint posIndex = vertexIndex * GEO_POOL_VERTEX_BYTE_SIZE / 4;
    output[posIndex] = asuint(vertex.pos.x);
    output[posIndex+1] = asuint(vertex.pos.y);
    output[posIndex+2] = asuint(vertex.pos.z);

    uint uv0Index = (vertexIndex * GEO_POOL_VERTEX_BYTE_SIZE + GEO_POOL_UV0BYTE_OFFSET) / 4;
    StoreUvs(output, uv0Index, vertex.uv0);

    uint uv1Index = (vertexIndex * GEO_POOL_VERTEX_BYTE_SIZE + GEO_POOL_UV1BYTE_OFFSET) / 4;
    StoreUvs(output, uv1Index, vertex.uv1);

    uint normalIndex = (vertexIndex * GEO_POOL_VERTEX_BYTE_SIZE + GEO_POOL_NORMAL_BYTE_OFFSET) / 4;
    output[normalIndex] = NormalToOctahedral32(vertex.N);
}

void LoadVertex(
    uint vertexIndex,
    int vertexFlags,
    StructuredBuffer<uint> vertexBuffer,
    out GeoPoolVertex outputVertex)
{
    uint posIndex = vertexIndex * GEO_POOL_VERTEX_BYTE_SIZE / 4;
    float3 pos = asfloat(uint3(vertexBuffer[posIndex], vertexBuffer[posIndex + 1], vertexBuffer[posIndex + 2]));

    uint uv0Index = (vertexIndex * GEO_POOL_VERTEX_BYTE_SIZE + GEO_POOL_UV0BYTE_OFFSET) / 4;
    float2 uv0 = LoadUvs(vertexBuffer, uv0Index);

    uint uv1Index = (vertexIndex * GEO_POOL_VERTEX_BYTE_SIZE + GEO_POOL_UV1BYTE_OFFSET) / 4;
    float2 uv1 = LoadUvs(vertexBuffer, uv1Index);

    uint normalIndex = (vertexIndex * GEO_POOL_VERTEX_BYTE_SIZE + GEO_POOL_NORMAL_BYTE_OFFSET) / 4;
    uint normal = uint(vertexBuffer[normalIndex]);

    outputVertex.pos = pos;
    outputVertex.uv0 = uv0;
    outputVertex.uv1 = uv1;
    outputVertex.N = Octahedral32ToNormal(normal);
}

}

#endif
