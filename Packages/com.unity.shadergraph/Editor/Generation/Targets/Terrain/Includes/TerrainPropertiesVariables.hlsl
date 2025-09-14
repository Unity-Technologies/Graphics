#ifndef UNITY_TERRAIN_PREVIEW_TERRAINPROPS
#define UNITY_TERRAIN_PREVIEW_TERRAINPROPS

// check against !defined(SHADERGRAPH_PREVIEW_MAIN) since the runtime header where
// _TerrainBasemapDistance is defined is TerrainLitInput.hlsl which is the shared URP Terrain header and is
// included as part of the Terrain subtarget. It is not included in node previews, however
#if defined(SHADERGRAPH_PREVIEW) && !defined(SHADERGRAPH_PREVIEW_MAIN)

float4 _TerrainHeightmapScale;
float _TerrainBasemapDistance;
uint _NumLayersCount;

#endif
#endif
