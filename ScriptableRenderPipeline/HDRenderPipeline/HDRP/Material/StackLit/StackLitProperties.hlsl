// ===========================================================================
//                              WARNING:
// On PS4, texture/sampler declarations need to be outside of CBuffers
// Otherwise those parameters are not bound correctly at runtime.
// ===========================================================================
TEXTURE2D(_DistortionVectorMap);
SAMPLER(sampler_DistortionVectorMap);

TEXTURE2D(_BaseColorMap);
SAMPLER(sampler_BaseColorMap);

TEXTURE2D(_MetallicMap);
SAMPLER(sampler_MetallicMap);

TEXTURE2D(_SmoothnessAMap);
SAMPLER(sampler_SmoothnessAMap);

TEXTURE2D(_SmoothnessBMap);
SAMPLER(sampler_SmoothnessBMap);

TEXTURE2D(_SubsurfaceMaskMap);
SAMPLER(sampler_SubsurfaceMaskMap);

TEXTURE2D(_ThicknessMap);
SAMPLER(sampler_ThicknessMap);

TEXTURE2D(_EmissiveColorMap);
SAMPLER(sampler_EmissiveColorMap);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);


CBUFFER_START(UnityPerMaterial)

float4 _BaseColor;
float4 _BaseColorMap_ST;
float4 _BaseColorMap_TexelSize;
float4 _BaseColorMap_MipInfo;
float _BaseColorMapUV;
float _BaseColorMapUVLocal;

float _Metallic;
float _MetallicUseMap;
float _MetallicMapUV;
float _MetallicMapUVLocal;
float4 _MetallicMap_ST;
float4 _MetallicMap_TexelSize;
float4 _MetallicMap_MipInfo;
float4 _MetallicMapChannelMask;
float4 _MetallicRange;

float _SmoothnessA;
float _SmoothnessAUseMap;
float _SmoothnessAMapUV;
float _SmoothnessAMapUVLocal;
float4 _SmoothnessAMap_ST;
float4 _SmoothnessAMap_TexelSize;
float4 _SmoothnessAMap_MipInfo;
float4 _SmoothnessAMapChannelMask;
float4 _SmoothnessARange;
float _SmoothnessB;
float _SmoothnessBUseMap;
float _SmoothnessBMapUV;
float _SmoothnessBMapUVLocal;
float4 _SmoothnessBMap_ST;
float4 _SmoothnessBMap_TexelSize;
float4 _SmoothnessBMap_MipInfo;
float4 _SmoothnessBMapChannelMask;
float4 _SmoothnessBRange;
float _LobeMix;

float _NormalScale;
float _NormalMapUV;
float4 _NormalMap_ST;
float4 _NormalMap_TexelSize;
float4 _NormalMap_MipInfo;

float3 _EmissiveColor;
float4 _EmissiveColorMap_ST;
float4 _EmissiveColorMap_TexelSize;
float4 _EmissiveColorMap_MipInfo;
float _EmissiveColorMapUV;
float _EmissiveColorMapUVLocal;
float _EmissiveIntensity;
float _AlbedoAffectEmissive;

float _SubsurfaceMask;
float _SubsurfaceMaskUseMap;
float _SubsurfaceMaskMapUV;
float4 _SubsurfaceMaskMap_ST;
float4 _SubsurfaceMaskMap_TexelSize;
float4 _SubsurfaceMaskMap_MipInfo;
float4 _SubsurfaceMaskMapChannelMask;
float4 _SubsurfaceMaskRange;

float _Thickness;
float _ThicknessUseMap;
float _ThicknessMapUV;
float4 _ThicknessMap_ST;
float4 _ThicknessMap_TexelSize;
float4 _ThicknessMap_MipInfo;
float4 _ThicknessMapChannelMask;
float4 _ThicknessRange;

float _AlphaCutoff;
float4 _DoubleSidedConstants;

float _DistortionScale;
float _DistortionVectorScale;
float _DistortionVectorBias;
float _DistortionBlurScale;
float _DistortionBlurRemapMin;
float _DistortionBlurRemapMax;

// Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
// value that exist to identify if the GI emission need to be enabled.
// In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
// TODO: Fix the code in legacy unity so we can customize the behavior for GI
float3 _EmissionColor;

CBUFFER_END
