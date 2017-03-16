#ifndef __TILEDREFLECTIONTEMPLATE_H__
#define __TILEDREFLECTIONTEMPLATE_H__


#include "TiledLightingUtils.hlsl"
#include "ReflectionTemplate.hlsl"



float3 ExecuteReflectionList(out uint numReflectionProbesProcessed, uint2 pixCoord, float3 vP, float3 vNw, float3 Vworld, float smoothness)
{
    uint start = 0, numReflectionProbes = 0;
    GetCountAndStart(start, numReflectionProbes, pixCoord, vP.z, REFLECTION_LIGHT);

    numReflectionProbesProcessed = numReflectionProbes;     // mainly for debugging/heat maps
    return ExecuteReflectionList(start, numReflectionProbes, vP, vNw, Vworld, smoothness);
}


#endif
