#ifndef _PATHTRACING_FETCHTERRAINGEOMETRY_HLSL_
#define _PATHTRACING_FETCHTERRAINGEOMETRY_HLSL_

#include "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/Shaders/TerrainRayMarching.hlsl"

UnifiedRT::HitGeomAttributes FetchTerrainHitGeomAttributes(UnifiedRT::Hit hit, int terrainIndex)
{
    UnifiedRT::TerrainData terrainData = g_TerrainList[terrainIndex];
    float numCells = 1.0 / terrainData.invHeightmapWidthInTexels - 1.0;
    float2 heightmapUV = hit.uvBarycentrics * numCells;

    float3 localPos, localNormal;
    ComputeTerrainLocalPosAndNormal(terrainData, terrainIndex, heightmapUV, localPos, localNormal);

    UnifiedRT::HitGeomAttributes result = (UnifiedRT::HitGeomAttributes) 0;
    result.position = localPos;
    result.normal = localNormal;
    result.faceNormal = localNormal;
    result.uv0 = hit.uvBarycentrics;
    result.uv1 = hit.uvBarycentrics;

    return result;
}


#endif // _PATHTRACING_FETCHTERRAINGEOMETRY_HLSL_
