#ifndef __TILEDLIGHTINGTEMPLATE_H__
#define __TILEDLIGHTINGTEMPLATE_H__


#include "TiledLightingUtils.hlsl"
#include "LightingTemplate.hlsl"



float3 ExecuteLightList(out uint numLightsProcessed, uint2 pixCoord, float3 vP, float3 vPw, float3 Vworld)
{
    uint nrTilesX = (g_widthRT+15)/16; uint nrTilesY = (g_heightRT+15)/16;
    uint2 tileIDX = pixCoord / 16;

    uint start = 0, numLights = 0;
    GetCountAndStart(start, numLights, tileIDX, nrTilesX, nrTilesY, vP.z, DIRECT_LIGHT);

    numLightsProcessed = numLights;     // mainly for debugging/heat maps
    return ExecuteLightList(start, numLights, vP, vPw, Vworld);
}

#endif
