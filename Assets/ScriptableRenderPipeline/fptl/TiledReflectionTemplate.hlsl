#ifndef __TILEDREFLECTIONTEMPLATE_H__
#define __TILEDREFLECTIONTEMPLATE_H__


#include "TiledLightingUtils.hlsl"
#include "ReflectionTemplate.hlsl"



float3 ExecuteReflectionList(out uint numReflectionProbesProcessed, uint2 pixCoord, float3 vP, float3 vNw, float3 Vworld, float smoothness)
{
    uint nrTilesX = (g_widthRT+15)/16; uint nrTilesY = (g_heightRT+15)/16;
    uint2 tileIDX = pixCoord / 16;

    uint start = 0, numReflectionProbes = 0;
    GetCountAndStart(start, numReflectionProbes, tileIDX, nrTilesX, nrTilesY, vP.z, REFLECTION_LIGHT);

    numReflectionProbesProcessed = numReflectionProbes;     // mainly for debugging/heat maps
    return ExecuteReflectionList(start, numReflectionProbes, vP, vNw, Vworld, smoothness);
}


#endif
