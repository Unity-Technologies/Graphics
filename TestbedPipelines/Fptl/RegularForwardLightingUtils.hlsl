#ifndef __REGULARFORWARDLIGHTINGUTILS_H__
#define __REGULARFORWARDLIGHTINGUTILS_H__


#include "LightingUtils.hlsl"


StructuredBuffer<SFiniteLightData> g_vLightData;
StructuredBuffer<uint> g_vLightListMeshInst;          // build on CPU if in use. direct lights first, then reflection probes. (don't support Buffer yet in unity, so using structured)

uniform uint4 unity_LightIndicesOffsetAndCount;
uniform uint4 unity_ReflectionProbeIndicesOffsetAndCount;

void GetCountAndStart(out uint start, out uint nrLights, uint model)
{
    start = model==REFLECTION_LIGHT ? unity_ReflectionProbeIndicesOffsetAndCount.x : unity_LightIndicesOffsetAndCount.x;
    nrLights = model==REFLECTION_LIGHT ? unity_ReflectionProbeIndicesOffsetAndCount.y : unity_LightIndicesOffsetAndCount.y;
}

uint FetchIndex(const uint start, const uint l)
{
    return g_vLightListMeshInst[start+l];
}

#endif
