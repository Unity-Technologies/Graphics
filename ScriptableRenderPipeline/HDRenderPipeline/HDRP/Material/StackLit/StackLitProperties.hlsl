// ===========================================================================
//                              WARNING:
// On PS4, texture/sampler declarations need to be outside of CBuffers
// Otherwise those parameters are not bound correctly at runtime.
// ===========================================================================
TEXTURE2D(_DistortionVectorMap);
SAMPLER(sampler_DistortionVectorMap);

TEXTURE2D(_EmissiveColorMap);
SAMPLER(sampler_EmissiveColorMap);

TEXTURE2D(_BaseColorMap);
SAMPLER(sampler_BaseColorMap);

TEXTURE2D(_MaskMapA);
SAMPLER(sampler_MaskMapA);

TEXTURE2D(_MaskMapB);
SAMPLER(sampler_MaskMapB);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);


CBUFFER_START(UnityPerMaterial)

float4 _BaseColor;
float4 _BaseColorMap_ST;
float4 _BaseColorMap_TexelSize;
float4 _BaseColorMap_MipInfo;

float _Metallic;
float _SmoothnessA;
float _SmoothnessARemapMin;
float _SmoothnessARemapMax;
float _SmoothnessB;
float _SmoothnessBRemapMin;
float _SmoothnessBRemapMax;
float _LobeMix;

float _Anisotropy;
float _CoatSmoothness;
float _CoatIor;
float _CoatThickness;
float4 _CoatExtinction;

float _NormalScale;

float4 _UVMappingMask;


float3 _EmissiveColor;
float4 _EmissiveColorMap_ST;
float _EmissiveIntensity;
float _AlbedoAffectEmissive;

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
