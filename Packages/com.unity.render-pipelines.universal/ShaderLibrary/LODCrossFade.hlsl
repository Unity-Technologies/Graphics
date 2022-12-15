#ifndef UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED
#define UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED

float _DitheringTextureInvSize;

TEXTURE2D(_DitheringTexture);
SAMPLER(sampler_DitheringTexture);

half CopySign(half x, half s)
{
    return (s >= 0) ? abs(x) : -abs(x);
}

void LODFadeCrossFade(float4 positionCS)
{
    half2 uv = positionCS.xy * _DitheringTextureInvSize;

    half d = SAMPLE_TEXTURE2D(_DitheringTexture, sampler_DitheringTexture, uv).a;

    d = unity_LODFade.x - CopySign(d, unity_LODFade.x);

    clip(d);
}

#endif
