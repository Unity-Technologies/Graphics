#ifndef VRS_MAINTEX_INCLUDED
#define VRS_MAINTEX_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Vrs/Shaders/VrsShadingRates.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

TEXTURE2D_X(_VrsMainTex);

StructuredBuffer<float4> _VrsMainTexLut;

float4 LoadVrsMainTex(uint2 tid)
{
    return LOAD_TEXTURE2D_X(_VrsMainTex, tid);
}

float4 SampleVrsMainTexFromCoords(float2 coords)
{
    return SAMPLE_TEXTURE2D_X_LOD(_VrsMainTex, sampler_PointClamp, coords, 0);
}

#endif // VRS_MAINTEX_INCLUDED
