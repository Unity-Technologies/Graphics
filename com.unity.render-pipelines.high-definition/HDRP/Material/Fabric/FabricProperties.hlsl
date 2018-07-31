// ===========================================================================
//                              WARNING:
// On PS4, texture/sampler declarations need to be outside of CBuffers
// Otherwise those parameters are not bound correctly at runtime.
// ===========================================================================

TEXTURE2D(_BaseColorMap);
SAMPLER(sampler_BaseColorMap);

TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);
TEXTURE2D(_BentNormalMap); // Reuse sampler from normal map
SAMPLER(sampler_BentNormalMap);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

TEXTURE2D(_DetailMask);
SAMPLER(sampler_DetailMask);
TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);

TEXTURE2D(_TangentMap);
SAMPLER(sampler_TangentMap);
TEXTURE2D(_TangentMapOS);
SAMPLER(sampler_TangentMapOS);

TEXTURE2D(_AnisotropyMap);
SAMPLER(sampler_AnisotropyMap);

TEXTURE2D(_SubsurfaceMaskMap);
SAMPLER(sampler_SubsurfaceMaskMap);
TEXTURE2D(_ThicknessMap);
SAMPLER(sampler_ThicknessMap);

CBUFFER_START(UnityPerMaterial)

float4 _BaseColor;
float4 _BaseColorMap_ST;
float4 _BaseColorMap_TexelSize;
float4 _BaseColorMap_MipInfo;

float _AlphaCutoff;

float _EnableSpecularOcclusion;

float _Smoothness;
float _SmoothnessRemapMin;
float _SmoothnessRemapMax;
float _AORemapMin;
float _AORemapMax;

float _NormalScale;

float4 _DetailMap_ST;
float _DetailFuzz1;
float _DetailAOScale;
float _DetailNormalScale;
float _DetailSmoothnessScale;

float _Anisotropy;

int   _DiffusionProfile;
float _SubsurfaceMask;
float _Thickness;
float4 _ThicknessRemap;

CBUFFER_END
