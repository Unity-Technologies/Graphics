#ifndef UNITY_DECALPROPERTIES_INCLUDED
#define UNITY_DECALPROPERTIES_INCLUDED

TEXTURE2D(_BaseColorMap);
SAMPLER(sampler_BaseColorMap);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);

float _DecalBlend;

#endif 