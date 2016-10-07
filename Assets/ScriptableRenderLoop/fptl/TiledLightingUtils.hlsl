#ifndef __TILEDLIGHTINGUTILS_H__
#define __TILEDLIGHTINGUTILS_H__


uniform float4x4 g_mViewToWorld;
uniform float4x4 g_mScrProjection;
uniform float4x4 g_mInvScrProjection;


uniform uint g_widthRT;
uniform uint g_heightRT;

StructuredBuffer<uint> g_vLightListGlobal;

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
//uniform int	  g_iLog2NumClusters;		// numClusters = (1<<g_iLog2NumClusters)
uniform float g_fLog2NumClusters;
static int g_iLog2NumClusters;

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
		g_iLog2NumClusters = (int) g_fLog2NumClusters;
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

float GetLinearZFromSVPosW(float posW)
{
#ifdef LEFT_HAND_COORDINATES
	float linZ = posW;
#else
	float linZ = -posW;
#endif

	return linZ;
}

float GetLinearDepth(float zDptBufSpace)	// 0 is near 1 is far
{
	float3 vP = float3(0.0f,0.0f,zDptBufSpace);
	float4 v4Pres = mul(g_mInvScrProjection, float4(vP,1.0));
	return v4Pres.z / v4Pres.w;
}



float3 OverlayHeatMap(uint numLights, float3 c)
{
	/////////////////////////////////////////////////////////////////////
	//
	const float4 kRadarColors[12] = 
	{
		float4(0.0,0.0,0.0,0.0),   // black
		float4(0.0,0.0,0.6,0.5),   // dark blue
		float4(0.0,0.0,0.9,0.5),   // blue
		float4(0.0,0.6,0.9,0.5),   // light blue
		float4(0.0,0.9,0.9,0.5),   // cyan
		float4(0.0,0.9,0.6,0.5),   // blueish green
		float4(0.0,0.9,0.0,0.5),   // green
		float4(0.6,0.9,0.0,0.5),   // yellowish green
		float4(0.9,0.9,0.0,0.5),   // yellow
		float4(0.9,0.6,0.0,0.5),   // orange
		float4(0.9,0.0,0.0,0.5),   // red
		float4(1.0,0.0,0.0,0.9)    // strong red
	};

	float maxNrLightsPerTile = 31;



	int nColorIndex = numLights==0 ? 0 : (1 + (int) floor(10 * (log2((float)numLights) / log2(maxNrLightsPerTile))) );
	nColorIndex = nColorIndex<0 ? 0 : nColorIndex;
	float4 col = nColorIndex>11 ? float4(1.0,1.0,1.0,1.0) : kRadarColors[nColorIndex];

	return lerp(c, pow(col.xyz, 2.2), 0.3*col.w);
}



#endif