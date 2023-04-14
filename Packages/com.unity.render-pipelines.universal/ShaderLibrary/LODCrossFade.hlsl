#ifndef UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED
#define UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

float _DitheringTextureInvSize;

TEXTURE2D(_DitheringTexture);

half CopySign(half x, half s)
{
    return (s >= 0) ? abs(x) : -abs(x);
}

void LODFadeCrossFade(float4 positionCS)
{
    half2 uv = positionCS.xy * _DitheringTextureInvSize;

    half d = SAMPLE_TEXTURE2D(_DitheringTexture, sampler_PointRepeat, uv).a;

    d = unity_LODFade.x - CopySign(d, unity_LODFade.x);

    clip(d);
}

#endif
