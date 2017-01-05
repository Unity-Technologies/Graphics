#ifndef LAYERED_LIT_SHADER

// Set of users variables
float4 _BaseColor;
TEXTURE2D(_BaseColorMap);
SAMPLER2D(sampler_BaseColorMap);
float4 _BaseColorMap_ST;

float _Metallic;
float _Smoothness;
TEXTURE2D(_MaskMap);
SAMPLER2D(sampler_MaskMap);
TEXTURE2D(_SpecularOcclusionMap);
SAMPLER2D(sampler_SpecularOcclusionMap);

TEXTURE2D(_NormalMap);
SAMPLER2D(sampler_NormalMap);
float _NormalScale;

TEXTURE2D(_DetailMask);
SAMPLER2D(sampler_DetailMask);
TEXTURE2D(_DetailMap);
SAMPLER2D(sampler_DetailMap);
float4 _DetailMap_ST;
float _DetailAlbedoScale;
float _DetailNormalScale;
float _DetailSmoothnessScale;
float _DetailHeightScale;
float _DetailAOScale;

TEXTURE2D(_HeightMap);
SAMPLER2D(sampler_HeightMap);

float _HeightScale;
float _HeightBias;

TEXTURE2D(_TangentMap);
SAMPLER2D(sampler_TangentMap);

float _Anisotropy;
TEXTURE2D(_AnisotropyMap);
SAMPLER2D(sampler_AnisotropyMap);

//float _SubSurfaceRadius;
//TEXTURE2D(_SubSurfaceRadiusMap);
//SAMPLER2D(sampler_SubSurfaceRadiusMap);

// float _Thickness;
//TEXTURE2D(_ThicknessMap);
//SAMPLER2D(sampler_ThicknessMap);

// float _CoatCoverage;
//TEXTURE2D(_CoatCoverageMap);
//SAMPLER2D(sampler_CoatCoverageMap);

// float _CoatRoughness;
//TEXTURE2D(_CoatRoughnessMap);
//SAMPLER2D(sampler_CoatRoughnessMap);

TEXTURE2D(_DiffuseLightingMap);
SAMPLER2D(sampler_DiffuseLightingMap);

TEXTURE2D(_DistortionVectorMap);
SAMPLER2D(sampler_DistortionVectorMap);

float3 _EmissiveColor;
TEXTURE2D(_EmissiveColorMap);
SAMPLER2D(sampler_EmissiveColorMap);
float _EmissiveIntensity;

float _AlphaCutoff;

float _TexWorldScale;
float _UVMappingPlanar;
float4 _UVMappingMask;
float4 _UVDetailsMappingMask;

#else // LAYERED_LIT_SHADER

// Set of users variables
#define PROP_DECL(type, name) type name, name##0, name##1, name##2, name##3;
#define PROP_DECL_TEX2D(name)\
    TEXTURE2D(name##0); \
    SAMPLER2D(sampler##name##0); \
    TEXTURE2D(name##1); \
    TEXTURE2D(name##2); \
    TEXTURE2D(name##3);

// Set of users variables
PROP_DECL(float4, _BaseColor);
PROP_DECL_TEX2D(_BaseColorMap);
float4 _BaseColorMap0_ST;
float4 _BaseColorMap1_ST;
float4 _BaseColorMap2_ST;
float4 _BaseColorMap3_ST;

PROP_DECL(float, _Metallic);
PROP_DECL(float, _Smoothness);
PROP_DECL_TEX2D(_MaskMap);
PROP_DECL_TEX2D(_SpecularOcclusionMap);

PROP_DECL_TEX2D(_NormalMap);
PROP_DECL(float, _NormalScale);

PROP_DECL_TEX2D(_HeightMap);

PROP_DECL_TEX2D(_DetailMask);
PROP_DECL_TEX2D(_DetailMap);
float4 _DetailMap0_ST;
float4 _DetailMap1_ST;
float4 _DetailMap2_ST;
float4 _DetailMap3_ST;
PROP_DECL(float, _UVDetail);
PROP_DECL(float, _DetailAlbedoScale);
PROP_DECL(float, _DetailNormalScale);
PROP_DECL(float, _DetailSmoothnessScale);
PROP_DECL(float, _DetailHeightScale);
PROP_DECL(float, _DetailAOScale);

PROP_DECL(float, _HeightScale);
PROP_DECL(float, _HeightBias);

TEXTURE2D(_DiffuseLightingMap);
SAMPLER2D(sampler_DiffuseLightingMap);

TEXTURE2D(_DistortionVectorMap);
SAMPLER2D(sampler_DistortionVectorMap);

TEXTURE2D(_LayerMaskMap);
SAMPLER2D(sampler_LayerMaskMap);

float _HeightOffset1;
float _HeightOffset2;
float _HeightOffset3;
float _HeightFactor1;
float _HeightFactor2;
float _HeightFactor3;
float _BlendSize1;
float _BlendSize2;
float _BlendSize3;
float _VertexColorHeightFactor;

float3 _EmissiveColor;
TEXTURE2D(_EmissiveColorMap);
SAMPLER2D(sampler_EmissiveColorMap);
float _EmissiveIntensity;

PROP_DECL(float, _TexWorldScale);
PROP_DECL(float, _UVMappingPlanar);  
PROP_DECL(float4, _UVMappingMask);
PROP_DECL(float4, _UVDetailsMappingMask);

float _AlphaCutoff;

#endif // LAYERED_LIT_SHADER
