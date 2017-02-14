float4  _Color;
TEXTURE2D(_ColorMap);
SAMPLER2D(sampler_ColorMap);

TEXTURE2D(_DistortionVectorMap);
SAMPLER2D(sampler_DistortionVectorMap);

float3 _EmissiveColor;
TEXTURE2D(_EmissiveColorMap);
SAMPLER2D(sampler_EmissiveColorMap);
float _EmissiveIntensity;

float _AlphaCutoff;
