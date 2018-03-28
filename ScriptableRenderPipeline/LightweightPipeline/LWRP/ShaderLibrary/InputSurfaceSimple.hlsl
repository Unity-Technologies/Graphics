#ifndef LIGHTWEIGHT_INPUT_SURFACE_SIMPLE_INCLUDED
#define LIGHTWEIGHT_INPUT_SURFACE_SIMPLE_INCLUDED

#include "Core.hlsl"
#include "InputSurfaceCommon.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _MainTex_ST;
half4 _Color;
half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;
half _Shininess;
CBUFFER_END

TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);

half4 SampleSpecularGloss(half2 uv, half alpha)
{
    half4 specularGloss = half4(0, 0, 0, 1);
#ifdef _SPECGLOSSMAP
    specularGloss = SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv);
#elif defined(_SPECULAR_COLOR)
    specularGloss = _SpecColor;
#endif

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    specularGloss.a = alpha;
#endif
    return specularGloss;
}

#endif // LIGHTWEIGHT_INPUT_SURFACE_SIMPLE_INCLUDED
