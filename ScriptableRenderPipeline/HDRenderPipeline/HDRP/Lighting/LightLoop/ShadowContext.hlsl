#ifndef LIGHTLOOP_SHADOW_CONTEXT_HLSL
#define LIGHTLOOP_SHADOW_CONTEXT_HLSL

#define SHADOWCONTEXT_MAX_TEX2DARRAY   4
#define SHADOWCONTEXT_MAX_TEXCUBEARRAY 0
#define SHADOWCONTEXT_MAX_SAMPLER      3
#define SHADOWCONTEXT_MAX_COMPSAMPLER  1
#define SHADOW_OPTIMIZE_REGISTER_USAGE 1

#include "CoreRP/ShaderLibrary/Shadow/Shadow.hlsl"

TEXTURE2D_ARRAY(_ShadowmapExp_VSM_0);
SAMPLER(sampler_ShadowmapExp_VSM_0);

TEXTURE2D_ARRAY(_ShadowmapExp_VSM_1);
SAMPLER(sampler_ShadowmapExp_VSM_1);

TEXTURE2D_ARRAY(_ShadowmapExp_VSM_2);
SAMPLER(sampler_ShadowmapExp_VSM_2);

TEXTURE2D_ARRAY(_ShadowmapExp_PCF);
SAMPLER_CMP(sampler_ShadowmapExp_PCF);

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

#endif // LIGHTLOOP_SHADOW_CONTEXT_HLSL