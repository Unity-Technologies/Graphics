float4  _UnlitColor;
TEXTURE2D(_UnlitColorMap);
SAMPLER(sampler_UnlitColorMap);
float4 _UnlitColorMap_ST;

TEXTURE2D(_DistortionVectorMap);
SAMPLER(sampler_DistortionVectorMap);

float3 _EmissiveColor;
TEXTURE2D(_EmissiveColorMap);
SAMPLER(sampler_EmissiveColorMap);
float4 _EmissiveColorMap_ST;

float _EmissiveIntensity;

float _AlphaCutoff;
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
