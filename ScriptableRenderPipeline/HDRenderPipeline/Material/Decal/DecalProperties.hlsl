#ifndef UNITY_DECALPROPERTIES_INCLUDED
#define UNITY_DECALPROPERTIES_INCLUDED


TEXTURE2D(_BaseColorMap);
SAMPLER(sampler_BaseColorMap);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

float _DecalBlend;

CBUFFER_START(Decal)
float4x4 _WorldToDecal;
float4x4 _DecalToWorldR;
CBUFFER_END

#endif 