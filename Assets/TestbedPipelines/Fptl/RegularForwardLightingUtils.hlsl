#ifndef __REGULARFORWARDLIGHTINGUTILS_H__
#define __REGULARFORWARDLIGHTINGUTILS_H__


#include "LightingUtils.hlsl"


StructuredBuffer<SFiniteLightData> g_vLightData;
StructuredBuffer<uint> g_vLightListMeshInst;          // build on CPU if in use. direct lights first, then reflection probes. (don't support Buffer yet in unity, so using structured)

uniform int g_numLights;
uniform int g_numReflectionProbes;

void GetCountAndStart(out uint start, out uint nrLights, uint model)
{
    start = model==REFLECTION_LIGHT ? g_numLights : 0;  // offset by numLights entries
    nrLights = model==REFLECTION_LIGHT ? g_numReflectionProbes : g_numLights;
}

uint FetchIndex(const uint start, const uint l)
{
    return g_vLightListMeshInst[start+l];
}

#endif
