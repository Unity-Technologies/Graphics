#ifndef LIGHTWEIGHT_INPUT_SURFACE_PBR_INCLUDED
#define LIGHTWEIGHT_INPUT_SURFACE_PBR_INCLUDED

#include "LWRP/ShaderLibrary/Core.hlsl"
#include "CoreRP/ShaderLibrary/CommonMaterial.hlsl"
#include "LWRP/ShaderLibrary/InputSurfaceCommon.hlsl"

CBUFFER_START(_Terrain)
half _Metallic0, _Metallic1, _Metallic2, _Metallic3;
half _Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3;

float4 _Control_ST;
half4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
CBUFFER_END

TEXTURE2D(_Control);    SAMPLER(sampler_Control);
TEXTURE2D(_Splat0);     SAMPLER(sampler_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);

#ifdef _TERRAIN_NORMAL_MAP
TEXTURE2D(_Normal0);     SAMPLER(sampler_Normal0);
TEXTURE2D(_Normal1);
TEXTURE2D(_Normal2);
TEXTURE2D(_Normal3);
#endif

TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);

#endif // LIGHTWEIGHT_INPUT_SURFACE_PBR_INCLUDED
