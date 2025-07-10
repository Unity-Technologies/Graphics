#ifndef _UNIFIEDRAYTRACING_FETCHGEOMETRY_HLSL_
#define _UNIFIEDRAYTRACING_FETCHGEOMETRY_HLSL_

#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common/GeometryPool/GeometryPoolDefs.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common/GeometryPool/GeometryPool.hlsl"

#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/CommonStructs.hlsl"

#define INTERPOLATE_ATTRIBUTE(attr, barCoords) v.attr = v0.attr * (1.0 - barCoords.x - barCoords.y) + v1.attr * barCoords.x + v2.attr * barCoords.y

StructuredBuffer<UnifiedRT::InstanceData>     g_AccelStructInstanceList;

StructuredBuffer<uint>             g_globalIndexBuffer;
StructuredBuffer<uint>             g_globalVertexBuffer;
int                                g_globalVertexBufferStride;
StructuredBuffer<GeoPoolMeshChunk> g_MeshList;

namespace UnifiedRT {

namespace Internal {
    GeoPoolVertex InterpolateVertices(GeoPoolVertex v0, GeoPoolVertex v1, GeoPoolVertex v2, float2 barycentricCoords)
    {
        GeoPoolVertex v;
        INTERPOLATE_ATTRIBUTE(pos, barycentricCoords);
        INTERPOLATE_ATTRIBUTE(N, barycentricCoords);
        INTERPOLATE_ATTRIBUTE(uv0, barycentricCoords);
        INTERPOLATE_ATTRIBUTE(uv1, barycentricCoords);
        return v;
    }

    uint3 FetchTriangleIndices(GeoPoolMeshChunk meshInfo, uint triangleID)
    {
        return uint3(
            g_globalIndexBuffer[meshInfo.indexOffset + 3 * triangleID],
            g_globalIndexBuffer[meshInfo.indexOffset + 3 * triangleID + 1],
            g_globalIndexBuffer[meshInfo.indexOffset + 3 * triangleID + 2]);
    }


    GeoPoolVertex FetchVertex(GeoPoolMeshChunk meshInfo, uint vertexIndex)
    {
        GeoPoolVertex v;
        GeometryPool::LoadVertex(meshInfo.vertexOffset + (int)vertexIndex, 0, g_globalVertexBuffer, v);
        return v;
    }
}


static const uint kGeomAttribPosition = 1 << 0;
static const uint kGeomAttribNormal = 1 << 1;
static const uint kGeomAttribTexCoord0 = 1 << 4;
static const uint kGeomAttribTexCoord1 = 1 << 8;
static const uint kGeomAttribFaceNormal = 1 << 16;
static const uint kGeomAttribAll = 0xFFFFFFFF;

struct HitGeomAttributes
{
    float3 position;
    float3 normal;
    float3 faceNormal;
    float2 uv0;
    float2 uv1;
};

HitGeomAttributes FetchHitGeomAttributes(int geometryIndex, int primitiveIndex, float2 uvBarycentrics, uint attributesToFetch = kGeomAttribAll)
{
    HitGeomAttributes result = (HitGeomAttributes)0;

    GeoPoolMeshChunk meshInfo = g_MeshList[geometryIndex];
    uint3 triangleVertexIndices = Internal::FetchTriangleIndices(meshInfo, primitiveIndex);

    GeoPoolVertex v0, v1, v2;
    v0 = Internal::FetchVertex(meshInfo, triangleVertexIndices.x);
    v1 = Internal::FetchVertex(meshInfo, triangleVertexIndices.y);
    v2 = Internal::FetchVertex(meshInfo, triangleVertexIndices.z);

    GeoPoolVertex v = Internal::InterpolateVertices(v0, v1, v2, uvBarycentrics);

    if (attributesToFetch & kGeomAttribFaceNormal)
        result.faceNormal = cross(v1.pos - v0.pos, v2.pos - v0.pos);

    if (attributesToFetch & kGeomAttribPosition)
        result.position = v.pos;

    if (attributesToFetch & kGeomAttribNormal)
        result.normal = v.N;

    if (attributesToFetch & kGeomAttribTexCoord0)
        result.uv0 = v.uv0;

    if (attributesToFetch & kGeomAttribTexCoord1)
        result.uv1 = v.uv1;

    return result;
}


HitGeomAttributes FetchHitGeomAttributes(Hit hit, uint attributesToFetch = kGeomAttribAll)
{
    int geometryIndex = g_AccelStructInstanceList[hit.instanceID].geometryIndex;
    return FetchHitGeomAttributes(geometryIndex, hit.primitiveIndex, hit.uvBarycentrics, attributesToFetch);
}

HitGeomAttributes FetchHitGeomAttributesInWorldSpace(UnifiedRT::InstanceData instanceInfo, UnifiedRT::Hit hit)
{
    UnifiedRT::HitGeomAttributes res = UnifiedRT::FetchHitGeomAttributes(hit);

    HitGeomAttributes wsRes = res;
    wsRes.position = mul(instanceInfo.localToWorld, float4(res.position, 1)).xyz;
    wsRes.normal = normalize(mul((float3x3)instanceInfo.localToWorldNormals, res.normal));
    wsRes.faceNormal = normalize(mul((float3x3)instanceInfo.localToWorldNormals, res.faceNormal));

    return wsRes;
}

InstanceData GetInstance(uint instanceID)
{
    return g_AccelStructInstanceList[instanceID];
}

} // namespace UnifiedRT

#endif // UNIFIEDRAYTRACING_FETCH_GEOMETRY_HLSL
