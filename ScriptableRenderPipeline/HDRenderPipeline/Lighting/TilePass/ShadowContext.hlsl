
// This can be custom for each project and needs to be in sync with the ShadowMgr

#define SHADOWCONTEXT_MAX_TEX2DARRAY   4
#define SHADOWCONTEXT_MAX_TEXCUBEARRAY 0
#define SHADOWCONTEXT_MAX_SAMPLER      3
#define SHADOWCONTEXT_MAX_COMPSAMPLER  1

SHADOWCONTEXT_DECLARE( SHADOWCONTEXT_MAX_TEX2DARRAY, SHADOWCONTEXT_MAX_TEXCUBEARRAY, SHADOWCONTEXT_MAX_COMPSAMPLER, SHADOWCONTEXT_MAX_SAMPLER );

TEXTURE2D_ARRAY(_ShadowmapExp_VSM_0);
SAMPLER2D(sampler_ShadowmapExp_VSM_0);

TEXTURE2D_ARRAY(_ShadowmapExp_VSM_1);
SAMPLER2D(sampler_ShadowmapExp_VSM_1);

TEXTURE2D_ARRAY(_ShadowmapExp_VSM_2);
SAMPLER2D(sampler_ShadowmapExp_VSM_2);

TEXTURE2D_ARRAY(_ShadowmapExp_PCF);
SAMPLER2D_SHADOW(sampler_ShadowmapExp_PCF);

StructuredBuffer<ShadowData>	_ShadowDatasExp;
StructuredBuffer<int4>			_ShadowPayloads;

ShadowContext InitShadowContext()
{
	ShadowContext sc;
	sc.shadowDatas     = _ShadowDatasExp;
	sc.payloads        = _ShadowPayloads;
	sc.tex2DArray[0]   = _ShadowmapExp_VSM_0;
	sc.tex2DArray[1]   = _ShadowmapExp_VSM_1;
	sc.tex2DArray[2]   = _ShadowmapExp_VSM_2;
	sc.tex2DArray[3]   = _ShadowmapExp_PCF;
	sc.samplers[0]	   = sampler_ShadowmapExp_VSM_0;
	sc.samplers[1]	   = sampler_ShadowmapExp_VSM_1;
	sc.samplers[2]	   = sampler_ShadowmapExp_VSM_2;
	sc.compSamplers[0] = sampler_ShadowmapExp_PCF;
	return sc;
}

