#ifndef __TILEDLIGHTINGUTILS_H__
#define __TILEDLIGHTINGUTILS_H__


#include "LightingUtils.hlsl"


StructuredBuffer<SFiniteLightData> g_vLightData;
Buffer<uint> g_vLightListGlobal;


void GetCountAndStartOpaque(out uint uStart, out uint uNrLights, uint2 tileIDX, int nrTilesX, int nrTilesY, float linDepth, uint model)
{
    const int tileOffs = (tileIDX.y+model*nrTilesY)*nrTilesX+tileIDX.x;

    uNrLights = g_vLightListGlobal[ 16*tileOffs + 0]&0xffff;
    uStart = tileOffs;
}

uint FetchIndexOpaque(const uint tileOffs, const uint l)
{
    const uint l1 = l+1;
    return (g_vLightListGlobal[ 16*tileOffs + (l1>>1)]>>((l1&1)*16))&0xffff;
}

#ifdef OPAQUES_ONLY

void GetCountAndStart(out uint uStart, out uint uNrLights, uint2 tileIDX, int nrTilesX, int nrTilesY, float linDepth, uint model)
{
    GetCountAndStartOpaque(uStart, uNrLights, tileIDX, nrTilesX, nrTilesY, linDepth, model);
}

uint FetchIndex(const uint tileOffs, const uint l)
{
    return FetchIndexOpaque(tileOffs, l);
}

#else

uniform float g_fClustScale;
uniform float g_fClustBase;
uniform float g_fNearPlane;
uniform float g_fFarPlane;
uniform int g_iLog2NumClusters;	// We need to always define these to keep constant buffer layouts compatible

#include "ClusteredUtils.h"

Buffer<uint> g_vLayeredOffsetsBuffer;
Buffer<float> g_logBaseBuffer;

uniform uint g_isLogBaseBufferEnabled;
uniform uint g_isOpaquesOnlyEnabled;


void GetCountAndStart(out uint uStart, out uint uNrLights, uint2 tileIDX, int nrTilesX, int nrTilesY, float linDepth, uint model)
{
    if(g_isOpaquesOnlyEnabled)
    {
        GetCountAndStartOpaque(uStart, uNrLights, tileIDX, nrTilesX, nrTilesY, linDepth, model);
    }
    else
    {
        float logBase = g_fClustBase;
        if(g_isLogBaseBufferEnabled)
            logBase = g_logBaseBuffer[tileIDX.y*nrTilesX + tileIDX.x];

        int clustIdx = SnapToClusterIdxFlex(linDepth, logBase, g_isLogBaseBufferEnabled!=0);

        int nrClusters = (1<<g_iLog2NumClusters);
        const int idx = ((model*nrClusters + clustIdx)*nrTilesY + tileIDX.y)*nrTilesX + tileIDX.x;
        uint dataPair = g_vLayeredOffsetsBuffer[idx];
        uStart = dataPair&0x7ffffff;
        uNrLights = (dataPair>>27)&31;
    }
}

uint FetchIndex(const uint tileOffs, const uint l)
{
    if(g_isOpaquesOnlyEnabled)
        return FetchIndexOpaque(tileOffs, l);
    else
        return g_vLightListGlobal[ tileOffs+l ];
}

#endif



#endif
