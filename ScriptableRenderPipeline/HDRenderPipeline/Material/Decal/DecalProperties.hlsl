#ifndef UNITY_DECALPROPERTIES_INCLUDED
#define UNITY_DECALPROPERTIES_INCLUDED


TEXTURE2D(_BaseColorMap);
SAMPLER2D(sampler_BaseColorMap);
TEXTURE2D(_NormalMap);
SAMPLER2D(sampler_NormalMap);

float _DecalBlend;

CBUFFER_START(Decal)
float4x4 _WorldToDecal;
float4x4 _DecalToWorldR;
CBUFFER_END

#endif 