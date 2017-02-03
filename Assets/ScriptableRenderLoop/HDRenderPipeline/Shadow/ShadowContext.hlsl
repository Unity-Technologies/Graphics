
// This can be custom for each project and needs to be in sync with the ShadowMgr

#define SHADOWCONTEXT_MAX_TEX2DARRAY   2
#define SHADOWCONTEXT_MAX_TEXCUBEARRAY 2
#define SHADOWCONTEXT_MAX_SAMPLER	   2
#define SHADOWCONTEXT_MAX_COMPSAMPLER  1

SHADOWCONTEXT_DECLARE( SHADOWCONTEXT_MAX_TEX2DARRAY, SHADOWCONTEXT_MAX_TEXCUBEARRAY, SHADOWCONTEXT_MAX_COMPSAMPLER, SHADOWCONTEXT_MAX_SAMPLER );

StructuredBuffer<ShadowData>	_ShadowDatasExp;
StructuredBuffer<int>			_ShadowPayloads;
TEXTURE2D_ARRAY(_ShadowmapExp);
SAMPLER2D_SHADOW(sampler_ShadowmapExp);
TEXTURE2D_ARRAY(_ShadowmapMomentum);
SAMPLER2D(sampler_ShadowmapMomentum);


ShadowContext InitShadowContext()
{
	ShadowContext sc;
	sc.shadowDatas     = _ShadowDatasExp;
	sc.payloads        = _ShadowPayloads;
	sc.tex2DArray[0]   = _ShadowmapExp;
	sc.tex2DArray[1]   = _ShadowmapMomentum;
	sc.compSamplers[0] = sampler_ShadowmapExp;
	sc.samplers[0]     = sampler_ShadowmapMomentum;
	return sc;
}
