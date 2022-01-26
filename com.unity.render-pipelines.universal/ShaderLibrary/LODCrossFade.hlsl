#ifndef UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED
#define UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED

static const int k_LODCrossFadeTypeBayerMatrixDither = 0;
static const int k_LODCrossFadeTypeBlueNoiseDither = 1;
static const int k_LODCrossFadeTypeHashDither = 2;

int _LODCrossFadeType;

TEXTURE2D(_DitheringTexture);
SAMPLER(sampler_DitheringTexture);

half CopySign(half x, half s)
{
    return (s >= 0) ? abs(x) : -abs(x);
}

half GetBayerMatrixDithering(float2 seed)
{
    const half k_BayerMatrixTexSize = 4.0; 

    half2 uv = seed / k_BayerMatrixTexSize;

    return SAMPLE_TEXTURE2D(_DitheringTexture, sampler_DitheringTexture, uv).a;
}

half GetBlueNoiseDithering(float2 seed)
{
    const half k_BlueNoiseTexSize = 64.0;

    half2 uv = seed / k_BlueNoiseTexSize;

    return SAMPLE_TEXTURE2D(_DitheringTexture, sampler_DitheringTexture, uv).a;
}

half GetLODDithering(float2 crossFadeSeed, half crossFadeFactor)
{
    half d;

    switch (_LODCrossFadeType)
    {
    case k_LODCrossFadeTypeHashDither:
        d = GenerateHashedRandomFloat(crossFadeSeed);
        break;
    case k_LODCrossFadeTypeBlueNoiseDither:
        d = GetBlueNoiseDithering(crossFadeSeed);
        break;
    case k_LODCrossFadeTypeBayerMatrixDither:
    default:
        d = GetBayerMatrixDithering(crossFadeSeed);
        break;
    }

    return crossFadeFactor - CopySign(d, crossFadeFactor);
}

void LODFadeCrossFade(float4 positionCS)
{
    half d = GetLODDithering(positionCS.xy, unity_LODFade.x);

    clip(d);
}

#endif
