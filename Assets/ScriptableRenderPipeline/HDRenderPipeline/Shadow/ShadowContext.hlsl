
// This can be custom for each project and needs to be in sync with the ShadowMgr

#define SHADOWCONTEXT_MAX_TEX2DARRAY   2
#define SHADOWCONTEXT_MAX_TEXCUBEARRAY 1
#define SHADOWCONTEXT_MAX_SAMPLER	   1
#define SHADOWCONTEXT_MAX_COMPSAMPLER  2

SHADOWCONTEXT_DECLARE( SHADOWCONTEXT_MAX_TEX2DARRAY, SHADOWCONTEXT_MAX_TEXCUBEARRAY, SHADOWCONTEXT_MAX_COMPSAMPLER, SHADOWCONTEXT_MAX_SAMPLER );

StructuredBuffer<ShadowData>	_ShadowDatasExp;
StructuredBuffer<int4>			_ShadowPayloads;
TEXTURE2D_ARRAY(_ShadowmapExp_Dir);
SAMPLER2D_SHADOW(sampler_ShadowmapExp_Dir);
TEXTURE2D_ARRAY(_ShadowmapExp_PointSpot);
SAMPLER2D_SHADOW(sampler_ShadowmapExp_PointSpot);

ShadowContext InitShadowContext()
{
	ShadowContext sc;
	sc.shadowDatas     = _ShadowDatasExp;
	sc.payloads        = _ShadowPayloads;
	sc.tex2DArray[0]   = _ShadowmapExp_Dir;
	sc.tex2DArray[1]   = _ShadowmapExp_PointSpot;
	sc.compSamplers[0] = sampler_ShadowmapExp_Dir;
	sc.compSamplers[1] = sampler_ShadowmapExp_PointSpot;
	return sc;
}

