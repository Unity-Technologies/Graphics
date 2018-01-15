#ifndef FPTL_SHADOW_CONTEXT_HLSL
#define FPTL_SHADOW_CONTEXT_HLSL

#define SHADOWCONTEXT_MAX_TEX2DARRAY   1
#define SHADOWCONTEXT_MAX_TEXCUBEARRAY 0
#define SHADOWCONTEXT_MAX_SAMPLER      0
#define SHADOWCONTEXT_MAX_COMPSAMPLER  1

#include "CoreRP/ShaderLibrary/Shadow/Shadow.hlsl"

TEXTURE2D_ARRAY(_ShadowmapExp_PCF);
SAMPLER_CMP(sampler_ShadowmapExp_PCF);

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

#endif // FPTL_SHADOW_CONTEXT_HLSL
