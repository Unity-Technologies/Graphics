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
float4 _HeightMap_TexelSize; // Unity facility. This will provide the size of the heightmap to the shader

float _HeightAmplitude;
float _HeightCenter;

TEXTURE2D(_TangentMap);
SAMPLER2D(sampler_TangentMap);

float _Anisotropy;
TEXTURE2D(_AnisotropyMap);
SAMPLER2D(sampler_AnisotropyMap);

int   _MaterialID;
int   _SubsurfaceProfile;
float _SubsurfaceRadius;
float _Thickness;
TEXTURE2D(_SubsurfaceRadiusMap);
SAMPLER2D(sampler_SubsurfaceRadiusMap);
TEXTURE2D(_ThicknessMap);
SAMPLER2D(sampler_ThicknessMap);

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

float _HorizonFade;

float _TexWorldScale;
float _UVMappingPlanar;
float4 _UVMappingMask;
float4 _UVDetailsMappingMask;

float _PPDMaxSamples;
float _PPDMinSamples;
float _PPDLodThreshold;

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
float4 _NormalMap0_TexelSize; // Unity facility. This will provide the size of the base normal to the shader

PROP_DECL_TEX2D(_HeightMap);
float4 _HeightMap0_TexelSize;
float4 _HeightMap1_TexelSize;
float4 _HeightMap2_TexelSize;
float4 _HeightMap3_TexelSize;

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

PROP_DECL(float, _HeightAmplitude);
PROP_DECL(float, _HeightCenter);

TEXTURE2D(_DiffuseLightingMap);
SAMPLER2D(sampler_DiffuseLightingMap);

TEXTURE2D(_DistortionVectorMap);
SAMPLER2D(sampler_DistortionVectorMap);

TEXTURE2D(_LayerMaskMap);
SAMPLER2D(sampler_LayerMaskMap);

float _BlendUsingHeight1;
float _BlendUsingHeight2;
float _BlendUsingHeight3;
PROP_DECL(float, _LayerHeightAmplitude);
PROP_DECL(float, _LayerCenterOffset);
PROP_DECL(float, _MinimumOpacity);
PROP_DECL(float, _OpacityAsDensity);
float _InheritBaseNormal1;
float _InheritBaseNormal2;
float _InheritBaseNormal3;
float _InheritBaseHeight1;
float _InheritBaseHeight2;
float _InheritBaseHeight3;
float _InheritBaseColor1;
float _InheritBaseColor2;
float _InheritBaseColor3;
float _InheritBaseColorThreshold1;
float _InheritBaseColorThreshold2;
float _InheritBaseColorThreshold3;
PROP_DECL(float, _LayerTiling);

float3 _EmissiveColor;
TEXTURE2D(_EmissiveColorMap);
SAMPLER2D(sampler_EmissiveColorMap);
float _EmissiveIntensity;

PROP_DECL(float, _TexWorldScale);
PROP_DECL(float, _UVMappingPlanar);  
PROP_DECL(float4, _UVMappingMask);
PROP_DECL(float4, _UVDetailsMappingMask);

float _AlphaCutoff;

float _HorizonFade;

float _PPDMaxSamples;
float _PPDMinSamples;
float _PPDLodThreshold;

#endif // LAYERED_LIT_SHADER

// Tessellation specific

#ifdef TESSELLATION_ON
float _TessellationFactor;
float _TessellationFactorMinDistance;
float _TessellationFactorMaxDistance;
float _TessellationFactorTriangleSize;
float _TessellationShapeFactor;
float _TessellationBackFaceCullEpsilon;
float _TessellationObjectScale;
#endif
