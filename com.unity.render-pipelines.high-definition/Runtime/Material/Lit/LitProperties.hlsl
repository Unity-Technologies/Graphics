// ===========================================================================
//                              WARNING:
// On PS4, texture/sampler declarations need to be outside of CBuffers
// Otherwise those parameters are not bound correctly at runtime.
// ===========================================================================

TEXTURE2D(_DistortionVectorMap);
SAMPLER(sampler_DistortionVectorMap);

TEXTURE2D(_EmissiveColorMap);
SAMPLER(sampler_EmissiveColorMap);

#ifndef LAYERED_LIT_SHADER

TEXTURE2D(_DiffuseLightingMap);
SAMPLER(sampler_DiffuseLightingMap);

TEXTURE2D(_BaseColorMap);
SAMPLER(sampler_BaseColorMap);

TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);
TEXTURE2D(_BentNormalMap); // Reuse sampler from normal map
SAMPLER(sampler_BentNormalMap);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_NormalMapOS);
SAMPLER(sampler_NormalMapOS);

TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);

TEXTURE2D(_HeightMap);
SAMPLER(sampler_HeightMap);

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

TEXTURE2D(_IridescenceThicknessMap);
SAMPLER(sampler_IridescenceThicknessMap);

TEXTURE2D(_IridescenceMaskMap);
SAMPLER(sampler_IridescenceMaskMap);

TEXTURE2D(_SpecularColorMap);
SAMPLER(sampler_SpecularColorMap);

TEXTURE2D(_TransmittanceColorMap);
SAMPLER(sampler_TransmittanceColorMap);

TEXTURE2D(_CoatMaskMap);
SAMPLER(sampler_CoatMaskMap);

#else

// Set of users variables
#define PROP_DECL(type, name) type name##0, name##1, name##2, name##3
// sampler are share by texture type inside a layered material but we need to support that a particualr layer have no texture, so we take the first sampler of available texture as the share one
// mean we must declare all sampler
#define PROP_DECL_TEX2D(name)\
    TEXTURE2D(MERGE_NAME(name, 0)); \
    SAMPLER(MERGE_NAME(MERGE_NAME(sampler, name), 0)); \
    TEXTURE2D(MERGE_NAME(name, 1)); \
    SAMPLER(MERGE_NAME(MERGE_NAME(sampler, name), 1)); \
    TEXTURE2D(MERGE_NAME(name, 2)); \
    SAMPLER(MERGE_NAME(MERGE_NAME(sampler, name), 2)); \
    TEXTURE2D(MERGE_NAME(name, 3)); \
    SAMPLER(MERGE_NAME(MERGE_NAME(sampler, name), 3))


PROP_DECL_TEX2D(_BaseColorMap);
PROP_DECL_TEX2D(_MaskMap);
PROP_DECL_TEX2D(_BentNormalMap);
PROP_DECL_TEX2D(_NormalMap);
PROP_DECL_TEX2D(_NormalMapOS);
PROP_DECL_TEX2D(_DetailMap);
PROP_DECL_TEX2D(_HeightMap);

PROP_DECL_TEX2D(_SubsurfaceMaskMap);
PROP_DECL_TEX2D(_ThicknessMap);

TEXTURE2D(_LayerMaskMap);
SAMPLER(sampler_LayerMaskMap);
TEXTURE2D(_LayerInfluenceMaskMap);
SAMPLER(sampler_LayerInfluenceMaskMap);

#endif

CBUFFER_START(UnityPerMaterial)

// shared constant between lit and layered lit
float _AlphaCutoff;
float _UseShadowThreshold;
float _AlphaCutoffShadow;
float _AlphaCutoffPrepass;
float _AlphaCutoffPostpass;
float4 _DoubleSidedConstants;
float _DistortionScale;
float _DistortionVectorScale;
float _DistortionVectorBias;
float _DistortionBlurScale;
float _DistortionBlurRemapMin;
float _DistortionBlurRemapMax;

float _PPDMaxSamples;
float _PPDMinSamples;
float _PPDLodThreshold;

float3 _EmissiveColor;
float _AlbedoAffectEmissive;
float _EmissiveExposureWeight;

float _EnableSpecularOcclusion;

// Transparency
float3 _TransmittanceColor;
float _Ior;
float _ATDistance;
float _ThicknessMultiplier;

// Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
// value that exist to identify if the GI emission need to be enabled.
// In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
// TODO: Fix the code in legacy unity so we can customize the beahvior for GI
float3 _EmissionColor;
float4 _EmissiveColorMap_ST;
float _TexWorldScaleEmissive;
float4 _UVMappingMaskEmissive;

float4 _InvPrimScale; // Only XY are used

// Wind
float _InitialBend;
float _Stiffness;
float _Drag;
float _ShiverDrag;
float _ShiverDirectionality;

// Specular AA
float _EnableGeometricSpecularAA;
float _SpecularAAScreenSpaceVariance;
float _SpecularAAThreshold;

#ifndef LAYERED_LIT_SHADER

// Set of users variables
float4 _BaseColor;
float4 _BaseColorMap_ST;
float4 _BaseColorMap_TexelSize;
float4 _BaseColorMap_MipInfo;

float _Metallic;
float _Smoothness;
float _SmoothnessRemapMin;
float _SmoothnessRemapMax;
float _AORemapMin;
float _AORemapMax;

float _NormalScale;

float4 _DetailMap_ST;
float _DetailAlbedoScale;
float _DetailNormalScale;
float _DetailSmoothnessScale;

float4 _HeightMap_TexelSize; // Unity facility. This will provide the size of the heightmap to the shader

float _HeightAmplitude;
float _HeightCenter;

float _Anisotropy;

float _DiffusionProfileHash;
float _SubsurfaceMask;
float _Thickness;
float4 _ThicknessRemap;


float _IridescenceThickness;
float4 _IridescenceThicknessRemap;
float _IridescenceMask;

float _CoatMask;

float4 _SpecularColor;
float _EnergyConservingSpecularColor;

float _TexWorldScale;
float _InvTilingScale;
float4 _UVMappingMask;
float4 _UVDetailsMappingMask;
float _LinkDetailsWithBase;

#else // LAYERED_LIT_SHADER

// Set of users variables
PROP_DECL(float4, _BaseColor);
float4 _BaseColorMap0_ST;
float4 _BaseColorMap1_ST;
float4 _BaseColorMap2_ST;
float4 _BaseColorMap3_ST;

float4 _BaseColorMap0_TexelSize;
float4 _BaseColorMap0_MipInfo;

PROP_DECL(float, _Metallic);
PROP_DECL(float, _Smoothness);
PROP_DECL(float, _SmoothnessRemapMin);
PROP_DECL(float, _SmoothnessRemapMax);
PROP_DECL(float, _AORemapMin);
PROP_DECL(float, _AORemapMax);

PROP_DECL(float, _NormalScale);
float4 _NormalMap0_TexelSize; // Unity facility. This will provide the size of the base normal to the shader

float4 _HeightMap0_TexelSize;
float4 _HeightMap1_TexelSize;
float4 _HeightMap2_TexelSize;
float4 _HeightMap3_TexelSize;

float4 _DetailMap0_ST;
float4 _DetailMap1_ST;
float4 _DetailMap2_ST;
float4 _DetailMap3_ST;
PROP_DECL(float, _UVDetail);
PROP_DECL(float, _DetailAlbedoScale);
PROP_DECL(float, _DetailNormalScale);
PROP_DECL(float, _DetailSmoothnessScale);

PROP_DECL(float, _HeightAmplitude);
PROP_DECL(float, _HeightCenter);

PROP_DECL(float, _DiffusionProfileHash);
PROP_DECL(float, _SubsurfaceMask);
PROP_DECL(float, _Thickness);
PROP_DECL(float4, _ThicknessRemap);

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
PROP_DECL(float, _HeightOffset);
float _HeightTransition;

float4 _LayerMaskMap_ST;
float _TexWorldScaleBlendMask;
PROP_DECL(float, _TexWorldScale);
PROP_DECL(float, _InvTilingScale);
float4 _UVMappingMaskBlendMask;
PROP_DECL(float4, _UVMappingMask);
PROP_DECL(float4, _UVDetailsMappingMask);
PROP_DECL(float, _LinkDetailsWithBase);

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
float _TessellationTilingScale;
#endif

// Following two variables are feeded by the C++ Editor for Scene selection
int _ObjectId;
int _PassValue;

CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(_BaseColor)
    UNITY_DOTS_INSTANCED_PROP(_Metallic)
    UNITY_DOTS_INSTANCED_PROP(_Smoothness)
    UNITY_DOTS_INSTANCED_PROP(_SmoothnessRemapMin)
    UNITY_DOTS_INSTANCED_PROP(_SmoothnessRemapMax)
    UNITY_DOTS_INSTANCED_PROP(_AORemapMin)
    UNITY_DOTS_INSTANCED_PROP(_AORemapMax)
    UNITY_DOTS_INSTANCED_PROP(_NormalScale)
    UNITY_DOTS_INSTANCED_PROP(_UVDetail)
    UNITY_DOTS_INSTANCED_PROP(_DetailAlbedoScale)
    UNITY_DOTS_INSTANCED_PROP(_DetailNormalScale)
    UNITY_DOTS_INSTANCED_PROP(_DetailSmoothnessScale)
    UNITY_DOTS_INSTANCED_PROP(_HeightAmplitude)
    UNITY_DOTS_INSTANCED_PROP(_HeightCenter)
    UNITY_DOTS_INSTANCED_PROP(_DiffusionProfileHash)
    UNITY_DOTS_INSTANCED_PROP(_SubsurfaceMask)
    UNITY_DOTS_INSTANCED_PROP(_Thickness)
    UNITY_DOTS_INSTANCED_PROP(_ThicknessRemap)
    UNITY_DOTS_INSTANCED_PROP(_OpacityAsDensity)
    UNITY_DOTS_INSTANCED_PROP(_HeightOffset)
    UNITY_DOTS_INSTANCED_PROP(_TexWorldScale)
    UNITY_DOTS_INSTANCED_PROP(_InvTilingScale)
    UNITY_DOTS_INSTANCED_PROP(_UVMappingMask)
    UNITY_DOTS_INSTANCED_PROP(_UVDetailsMappingMask)
    UNITY_DOTS_INSTANCED_PROP(_LinkDetailsWithBase)
UNITY_DOTS_INSTANCING_END

#define _BaseColor             UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4, Metadata__BaseColor)
#define _Metallic              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__Metallic)
#define _Smoothness            UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__Smoothness)
#define _SmoothnessRemapMin    UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__SmoothnessRemapMin)
#define _SmoothnessRemapMax    UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__SmoothnessRemapMax)
#define _AORemapMin            UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__AORemapMin)
#define _AORemapMax            UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__AORemapMax)
#define _NormalScale           UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__NormalScale)
#define _UVDetail              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__UVDetail)
#define _DetailAlbedoScale     UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__DetailAlbedoScale)
#define _DetailNormalScale     UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__DetailNormalScale)
#define _DetailSmoothnessScale UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__DetailSmoothnessScale)
#define _HeightAmplitude       UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__HeightAmplitude)
#define _HeightCenter          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__HeightCenter)
#define _DiffusionProfileHash  UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__DiffusionProfileHash)
#define _SubsurfaceMask        UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__SubsurfaceMask)
#define _Thickness             UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__Thickness)
#define _ThicknessRemap        UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4, Metadata__ThicknessRemap)
#define _OpacityAsDensity      UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__OpacityAsDensity)
#define _HeightOffset          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__HeightOffset)
#define _TexWorldScale         UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__TexWorldScale)
#define _InvTilingScale        UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__InvTilingScale)
#define _UVMappingMask         UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4, Metadata__UVMappingMask)
#define _UVDetailsMappingMask  UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4, Metadata__UVDetailsMappingMask)
#define _LinkDetailsWithBase   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float , Metadata__LinkDetailsWithBase)

#endif

