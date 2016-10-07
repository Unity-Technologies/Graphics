#ifndef __TILEDLIGHTINGTEMPLATE_H__
#define __TILEDLIGHTINGTEMPLATE_H__

uint FetchIndex(const uint tileOffs, const uint l);

uniform float4x4 g_mViewToWorld;
uniform float4x4 g_mScrProjection;

#include "LightingTemplate.hlsl"


void GetLightCountAndStart(out uint uStart, out uint uNrLights, uint2 tileIDX, int nrTilesX, int nrTilesY, float linDepth);

uniform uint g_widthRT;
uniform uint g_heightRT;

float3 ExecuteLightListTiled(uint2 pixCoord, float3 vP, float3 vPw, float3 Vworld)
{
	uint nrTilesX = (g_widthRT+15)/16; uint nrTilesY = (g_heightRT+15)/16;
	uint2 tileIDX = pixCoord / 16;

	uint start = 0, numLights = 0;
	GetLightCountAndStart(start, numLights, tileIDX, nrTilesX, nrTilesY, vP.z);

	return ExecuteLightList(start, numLights, vP, vPw, Vworld);
}

uniform float g_fClustScale;
uniform float g_fClustBase;
uniform float g_fNearPlane;
uniform float g_fFarPlane;
//uniform int	  g_iLog2NumClusters;		// numClusters = (1<<g_iLog2NumClusters)
uniform float g_fLog2NumClusters;
static int g_iLog2NumClusters;

#include "ClusteredUtils.h"

StructuredBuffer<uint> g_vLightListGlobal;
Buffer<uint> g_vLayeredOffsetsBuffer;
#ifdef ENABLE_DEPTH_TEXTURE_BACKPLANE
Buffer<float> g_logBaseBuffer;
#endif


void GetLightCountAndStart(out uint uStart, out uint uNrLights, uint2 tileIDX, int nrTilesX, int nrTilesY, float linDepth)
{
	g_iLog2NumClusters = (int) g_fLog2NumClusters;
#ifdef ENABLE_DEPTH_TEXTURE_BACKPLANE
	float logBase = g_logBaseBuffer[tileIDX.y*nrTilesX + tileIDX.x];
#else
	float logBase = g_fClustBase;
#endif
	int clustIdx = SnapToClusterIdx(linDepth, logBase);

	int nrClusters = (1<<g_iLog2NumClusters);
	const int idx = ((DIRECT_LIGHT*nrClusters + clustIdx)*nrTilesY + tileIDX.y)*nrTilesX + tileIDX.x;
	uint dataPair = g_vLayeredOffsetsBuffer[idx];
	uStart = dataPair&0x7ffffff;
	uNrLights = (dataPair>>27)&31;
}

uint FetchIndex(const uint tileOffs, const uint l)
{
	return g_vLightListGlobal[ tileOffs+l ];
}


float3 GetViewPosFromLinDepth(float2 v2ScrPos, float fLinDepth)
{
	float fSx = g_mScrProjection[0].x;
	//float fCx = g_mScrProjection[2].x;
	float fCx = g_mScrProjection[0].z;
	float fSy = g_mScrProjection[1].y;
	//float fCy = g_mScrProjection[2].y;
	float fCy = g_mScrProjection[1].z;	

#ifdef LEFT_HAND_COORDINATES
	return fLinDepth*float3( ((v2ScrPos.x-fCx)/fSx), ((v2ScrPos.y-fCy)/fSy), 1.0 );
#else
	return fLinDepth*float3( -((v2ScrPos.x+fCx)/fSx), -((v2ScrPos.y+fCy)/fSy), 1.0 );
#endif
}



#endif