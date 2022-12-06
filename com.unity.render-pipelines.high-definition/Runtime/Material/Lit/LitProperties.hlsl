// ===========================================================================
//                              WARNING:
// On PS4, texture/sampler declarations need to be outside of CBuffers
// Otherwise those parameters are not bound correctly at runtime.
// ===========================================================================

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
TEXTURE2D(_BentNormalMapOS);
SAMPLER(sampler_BentNormalMapOS);

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
TEXTURE2D(_TransmissionMaskMap);
SAMPLER(sampler_TransmissionMaskMap);
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
    TEXTURE2D(CALL_MERGE_NAME(name, 0)); \
    SAMPLER(CALL_MERGE_NAME(CALL_MERGE_NAME(sampler, name), 0)); \
    TEXTURE2D(CALL_MERGE_NAME(name, 1)); \
    SAMPLER(CALL_MERGE_NAME(CALL_MERGE_NAME(sampler, name), 1)); \
    TEXTURE2D(CALL_MERGE_NAME(name, 2)); \
    SAMPLER(CALL_MERGE_NAME(CALL_MERGE_NAME(sampler, name), 2)); \
    TEXTURE2D(CALL_MERGE_NAME(name, 3)); \
    SAMPLER(CALL_MERGE_NAME(CALL_MERGE_NAME(sampler, name), 3))


PROP_DECL_TEX2D(_BaseColorMap);
PROP_DECL_TEX2D(_MaskMap);
PROP_DECL_TEX2D(_BentNormalMap);
PROP_DECL_TEX2D(_NormalMap);
PROP_DECL_TEX2D(_NormalMapOS);
PROP_DECL_TEX2D(_DetailMap);
PROP_DECL_TEX2D(_HeightMap);

PROP_DECL_TEX2D(_SubsurfaceMaskMap);
PROP_DECL_TEX2D(_TransmissionMaskMap);
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
float _BlendMode;
float _EnableBlendModePreserveSpecularLighting;

float _PPDMaxSamples;
float _PPDMinSamples;
float _PPDLodThreshold;

float3 _EmissiveColor;
float _AlbedoAffectEmissive;
float _EmissiveExposureWeight;

int  _SpecularOcclusionMode;

// Transparency
float3 _TransmittanceColor;
float _Ior;
float _ATDistance;

// Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
// value that exist to identify if the GI emission need to be enabled.
// In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
// TODO: Fix the code in legacy unity so we can customize the beahvior for GI
float3 _EmissionColor;
float4 _EmissiveColorMap_ST;
float _TexWorldScaleEmissive;
float4 _UVMappingMaskEmissive;
float _ObjectSpaceUVMappingEmissive;

float4 _InvPrimScale; // Only XY are used

// Specular AA
float _EnableGeometricSpecularAA;
float _SpecularAAScreenSpaceVariance;
float _SpecularAAThreshold;

// Raytracing
float _RayTracing;

#ifndef LAYERED_LIT_SHADER

// Set of users variables
float4 _BaseColor;
float4 _BaseColorMap_ST;
float4 _BaseColorMap_TexelSize;
float4 _BaseColorMap_MipInfo;

float _Metallic;
float _MetallicRemapMin;
float _MetallicRemapMax;
float _Smoothness;
float _SmoothnessRemapMin;
float _SmoothnessRemapMax;
float _AlphaRemapMin;
float _AlphaRemapMax;
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
float _TransmissionMask;
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
float _ObjectSpaceUVMapping;

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
PROP_DECL(float, _MetallicRemapMin);
PROP_DECL(float, _MetallicRemapMax);
PROP_DECL(float, _Smoothness);
PROP_DECL(float, _SmoothnessRemapMin);
PROP_DECL(float, _SmoothnessRemapMax);
PROP_DECL(float, _AlphaRemapMin);
PROP_DECL(float, _AlphaRemapMax);
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
PROP_DECL(float, _TransmissionMask);
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

CBUFFER_END

// Following three variables are feeded by the C++ Editor for Scene selection
// It need to be outside the UnityPerMaterial buffer to have Material compatible with SRP Batcher
int _ObjectId;
int _PassValue;
float4 _SelectionID;

#if defined(UNITY_DOTS_INSTANCING_ENABLED)
#if defined(LAYERED_LIT_SHADER)
// TODO: Do we want to expose all of this?
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor0)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor1)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor2)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor3)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic0)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic1)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic2)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic3)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMin0)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMin1)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMin2)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMin3)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMax0)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMax1)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMax2)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMax3)
    UNITY_DOTS_INSTANCED_PROP(float3, _EmissiveColor0)
    UNITY_DOTS_INSTANCED_PROP(float3, _EmissiveColor1)
    UNITY_DOTS_INSTANCED_PROP(float3, _EmissiveColor2)
    UNITY_DOTS_INSTANCED_PROP(float3, _EmissiveColor3)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecularColor0)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecularColor1)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecularColor2)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecularColor3)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaCutoff0);
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaCutoff1);
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaCutoff2);
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaCutoff3);
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness0)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness1)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness2)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness3)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMin0)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMin1)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMin2)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMin3)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMax0)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMax1)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMax2)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMax3)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMin0)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMin1)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMin2)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMin3)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMax0)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMax1)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMax2)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMax3)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMin0)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMin1)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMin2)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMin3)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMax0)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMax1)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMax2)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMax3)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoScale0)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoScale1)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoScale2)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoScale3)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalScale0)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalScale1)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalScale2)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalScale3)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailSmoothnessScale0)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailSmoothnessScale1)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailSmoothnessScale2)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailSmoothnessScale3)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffusionProfileHash0)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffusionProfileHash1)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffusionProfileHash2)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffusionProfileHash3)
    UNITY_DOTS_INSTANCED_PROP(float , _Thickness0)
    UNITY_DOTS_INSTANCED_PROP(float , _Thickness1)
    UNITY_DOTS_INSTANCED_PROP(float , _Thickness2)
    UNITY_DOTS_INSTANCED_PROP(float , _Thickness3)
    UNITY_DOTS_INSTANCED_PROP(float4, _ThicknessRemap0)
    UNITY_DOTS_INSTANCED_PROP(float4, _ThicknessRemap1)
    UNITY_DOTS_INSTANCED_PROP(float4, _ThicknessRemap2)
    UNITY_DOTS_INSTANCED_PROP(float4, _ThicknessRemap3)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor0              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor0)
#define _BaseColor1              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor1)
#define _BaseColor2              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor2)
#define _BaseColor3              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor3)
#define _Metallic0               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic0)
#define _Metallic1               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic1)
#define _Metallic2               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic2)
#define _Metallic3               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic3)
#define _MetallicRemapMin0       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMin0)
#define _MetallicRemapMin1       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMin1)
#define _MetallicRemapMin2       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMin2)
#define _MetallicRemapMin3       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMin3)
#define _MetallicRemapMax0       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMax0)
#define _MetallicRemapMax1       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMax1)
#define _MetallicRemapMax2       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMax2)
#define _MetallicRemapMax3       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMax3)
#define _EmissiveColor0          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3, _EmissiveColor0)
#define _EmissiveColor1          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3, _EmissiveColor1)
#define _EmissiveColor2          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3, _EmissiveColor2)
#define _EmissiveColor3          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3, _EmissiveColor3)
#define _SpecularColor0          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SpecularColor0)
#define _SpecularColor1          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SpecularColor1)
#define _SpecularColor2          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SpecularColor2)
#define _SpecularColor3          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SpecularColor3)
#define _AlphaCutoff0            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaCutoff0)
#define _AlphaCutoff1            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaCutoff1)
#define _AlphaCutoff2            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaCutoff2)
#define _AlphaCutoff3            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaCutoff3)
#define _Smoothness0             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness0)
#define _Smoothness1             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness1)
#define _Smoothness2             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness2)
#define _Smoothness3             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness3)
#define _SmoothnessRemapMin0     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMin0)
#define _SmoothnessRemapMin1     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMin1)
#define _SmoothnessRemapMin2     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMin2)
#define _SmoothnessRemapMin3     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMin3)
#define _SmoothnessRemapMax0     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMax0)
#define _SmoothnessRemapMax1     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMax1)
#define _SmoothnessRemapMax2     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMax2)
#define _SmoothnessRemapMax3     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMax3)
#define _AlphaRemapMin0          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMin0)
#define _AlphaRemapMin1          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMin1)
#define _AlphaRemapMin2          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMin2)
#define _AlphaRemapMin3          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMin3)
#define _AlphaRemapMax0          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMax0)
#define _AlphaRemapMax1          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMax1)
#define _AlphaRemapMax2          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMax2)
#define _AlphaRemapMax3          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMax3)
#define _AORemapMin0             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMin0)
#define _AORemapMin1             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMin1)
#define _AORemapMin2             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMin2)
#define _AORemapMin3             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMin3)
#define _AORemapMax0             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMax0)
#define _AORemapMax1             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMax1)
#define _AORemapMax2             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMax2)
#define _AORemapMax3             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMax3)
#define _DetailAlbedoScale0      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoScale0)
#define _DetailAlbedoScale1      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoScale1)
#define _DetailAlbedoScale2      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoScale2)
#define _DetailAlbedoScale3      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoScale3)
#define _DetailNormalScale0      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalScale0)
#define _DetailNormalScale1      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalScale1)
#define _DetailNormalScale2      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalScale2)
#define _DetailNormalScale3      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalScale3)
#define _DetailSmoothnessScale0  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailSmoothnessScale0)
#define _DetailSmoothnessScale1  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailSmoothnessScale1)
#define _DetailSmoothnessScale2  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailSmoothnessScale2)
#define _DetailSmoothnessScale3  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailSmoothnessScale3)
#define _DiffusionProfileHash0   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffusionProfileHash0)
#define _DiffusionProfileHash1   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffusionProfileHash1)
#define _DiffusionProfileHash2   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffusionProfileHash2)
#define _DiffusionProfileHash3   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffusionProfileHash3)
#define _Thickness0              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Thickness0)
#define _Thickness1              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Thickness1)
#define _Thickness2              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Thickness2)
#define _Thickness3              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Thickness3)
#define _ThicknessRemap0         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ThicknessRemap0)
#define _ThicknessRemap1         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ThicknessRemap1)
#define _ThicknessRemap2         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ThicknessRemap2)
#define _ThicknessRemap3         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ThicknessRemap3)

#else

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMin)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMax)
    UNITY_DOTS_INSTANCED_PROP(float3, _EmissiveColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecularColor)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaCutoff);
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMin)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMax)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMin)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMax)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMin)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMax)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoScale)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalScale)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailSmoothnessScale)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffusionProfileHash)
    UNITY_DOTS_INSTANCED_PROP(float , _Thickness)
    UNITY_DOTS_INSTANCED_PROP(float4, _ThicknessRemap)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
#define _Metallic               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic)
#define _MetallicRemapMin       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMin)
#define _MetallicRemapMax       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMax)
#define _Smoothness             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness)
#define _EmissiveColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3, _EmissiveColor)
#define _SpecularColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SpecularColor)
#define _AlphaCutoff            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaCutoff)
#define _Smoothness             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness)
#define _SmoothnessRemapMin     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMin)
#define _SmoothnessRemapMax     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMax)
#define _AlphaRemapMin          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMin)
#define _AlphaRemapMax          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMax)
#define _AORemapMin             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMin)
#define _AORemapMax             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMax)
#define _DetailAlbedoScale      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoScale)
#define _DetailNormalScale      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalScale)
#define _DetailSmoothnessScale  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailSmoothnessScale)
#define _DiffusionProfileHash   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffusionProfileHash)
#define _Thickness              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Thickness)
#define _ThicknessRemap         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ThicknessRemap)

#endif
#endif
