// shared constant between lit and layered lit
float _AlphaCutoff;
float _AlphaCutoffShadow;
float _AlphaCutoffPrepass;
float _AlphaCutoffOpacityThreshold;

float4 _DoubleSidedConstants;

float _HorizonFade;

TEXTURE2D(_DiffuseLightingMap);
SAMPLER2D(sampler_DiffuseLightingMap);

float3 _EmissiveColor;
TEXTURE2D(_EmissiveColorMap);
SAMPLER2D(sampler_EmissiveColorMap);
float _EmissiveIntensity;

// Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
// value that exist to identify if the GI emission need to be enabled.
// In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
// TODO: Fix the code in legacy unity so we can customize the beahvior for GI
float3 _EmissionColor;

// Wind
float _InitialBend;
float _Stiffness;
float _Drag;
float _ShiverDrag;
float _ShiverDirectionality;

// Set of users variables
float4 _DiffuseColor;
TEXTURE2D(_DiffuseColorMap);
SAMPLER2D(sampler_DiffuseColorMap);
float4 _DiffuseColorMap_ST;

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

TEXTURE2D(_TangentMap);
SAMPLER2D(sampler_TangentMap);

float _Anisotropy;
TEXTURE2D(_AnisotropyMap);
SAMPLER2D(sampler_AnisotropyMap);

float _TexWorldScale;
float4 _UVMappingMask;
float4 _UVDetailsMappingMask;
