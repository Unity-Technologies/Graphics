#ifndef __TILEDLIGHTINGUTILS_H__
#define __TILEDLIGHTINGUTILS_H__


#include "LightingUtils.hlsl"

// these uniforms are only needed for when OPAQUES_ONLY is NOT defined
// but there's a problem with our front-end compilation of compute shaders with multiple kernels causing it to error
//#ifndef OPAQUES_ONLY
uniform float g_fClustScale;
uniform float g_fClustBase;
uniform float g_fNearPlane;
uniform float g_fFarPlane;
uniform int g_iLog2NumClusters; // We need to always define these to keep constant buffer layouts compatible

uniform uint g_isLogBaseBufferEnabled;
uniform uint g_isOpaquesOnlyEnabled;
//#endif


StructuredBuffer<SFiniteLightData> g_vLightData;
StructuredBuffer<uint> g_vLightListGlobal;      // don't support Buffer yet in unity


void GetCountAndStartOpaque(out uint uStart, out uint uNrLights, uint2 pixCoord, float linDepth, uint model)
{
    uint tileSize = 16;
    uint nrTilesX = ((uint) (g_widthRT+(tileSize-1)))/tileSize; uint nrTilesY = ((uint) (g_heightRT+(tileSize-1)))/tileSize;
    uint2 tileIDX = pixCoord / tileSize;
    const int tileOffs = (tileIDX.y+model*nrTilesY)*nrTilesX+tileIDX.x;

    uNrLights = g_vLightListGlobal[ 16*tileOffs + 0]&0xffff;
    uStart = tileOffs;
}

#ifdef OPAQUES_ONLY

void GetCountAndStart(out uint uStart, out uint uNrLights, uint2 pixCoord, float linDepth, uint model)
{
    GetCountAndStartOpaque(uStart, uNrLights, pixCoord, linDepth, model);
}

uint FetchIndex(const uint tileOffs, const uint l)
{
    const uint l1 = l+1;
    return (g_vLightListGlobal[ 16*tileOffs + (l1>>1)]>>((l1&1)*16))&0xffff;
}

#else

#include "ClusteredUtils.h"

StructuredBuffer<uint> g_vLayeredOffsetsBuffer;         // don't support Buffer yet in unity
StructuredBuffer<float> g_logBaseBuffer;                // don't support Buffer yet in unity


void GetCountAndStart(out uint uStart, out uint uNrLights, uint2 pixCoord, float linDepth, uint model)
{
    if(g_isOpaquesOnlyEnabled)
    {
        GetCountAndStartOpaque(uStart, uNrLights, pixCoord, linDepth, model);
    }
    else
    {
        uint nrTilesX = ((uint) (g_widthRT+(TILE_SIZE_CLUSTERED-1))) / ((uint) TILE_SIZE_CLUSTERED);
        uint nrTilesY = ((uint) (g_heightRT+(TILE_SIZE_CLUSTERED-1))) / ((uint) TILE_SIZE_CLUSTERED);
        uint2 tileIDX = pixCoord / ((uint) TILE_SIZE_CLUSTERED);

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
    uint offs = tileOffs+l;
    const uint l1 = l+1;

    if(g_isOpaquesOnlyEnabled)
        offs = 16*tileOffs + (l1>>1);

    // Avoid generated HLSL bytecode to always access g_vLightListGlobal with
    // two different offsets, fixes out of bounds issue
    uint value = g_vLightListGlobal[offs];
    return (g_isOpaquesOnlyEnabled ? ((value >>((l1&1)*16))&0xffff) : value);
}

#endif



#endif
