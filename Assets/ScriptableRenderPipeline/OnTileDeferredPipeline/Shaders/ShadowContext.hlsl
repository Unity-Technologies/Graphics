
// This can be custom for each project and needs to be in sync with the ShadowMgr

#define SHADOWCONTEXT_MAX_TEX2DARRAY   1
#define SHADOWCONTEXT_MAX_TEXCUBEARRAY 0
#define SHADOWCONTEXT_MAX_SAMPLER      0
#define SHADOWCONTEXT_MAX_COMPSAMPLER  1

SHADOWCONTEXT_DECLARE( SHADOWCONTEXT_MAX_TEX2DARRAY, SHADOWCONTEXT_MAX_TEXCUBEARRAY, SHADOWCONTEXT_MAX_COMPSAMPLER, SHADOWCONTEXT_MAX_SAMPLER );

TEXTURE2D_ARRAY(_ShadowmapExp_PCF);
SAMPLER2D_SHADOW(sampler_ShadowmapExp_PCF);

StructuredBuffer<ShadowData>	_ShadowDatasExp;
StructuredBuffer<int4>			_ShadowPayloads;

ShadowContext InitShadowContext()
{
	ShadowContext sc;
	sc.shadowDatas     = _ShadowDatasExp;
	sc.payloads        = _ShadowPayloads;
	sc.tex2DArray[0]   = _ShadowmapExp_PCF;
	sc.compSamplers[0] = sampler_ShadowmapExp_PCF;
	return sc;
}

