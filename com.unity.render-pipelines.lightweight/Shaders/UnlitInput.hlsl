#ifndef LIGHTWEIGHT_UNLIT_INPUT_INCLUDED
#define LIGHTWEIGHT_UNLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half _Cutoff;
half _Glossiness;
half _Metallic;
CBUFFER_END

#endif
