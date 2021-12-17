#ifndef UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED
#define UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED

static const int k_LODCrossFadeTypeBayerMatrixDither = 0;
static const int k_LODCrossFadeTypeWhiteNoiseDither = 1;
static const int k_LODCrossFadeTypeBlueNoiseDither = 2;

int _LODCrossFadeType;

TEXTURE2D(_BlueNoiseDitheringTexture);
SAMPLER(sampler_BlueNoiseDitheringTexture);

half CopySign(half x, half s)
{
    return (s >= 0) ? abs(x) : -abs(x);
}

half GetBlueNoiseDithering(uint2 seed)
{
    const half k_BlueNoiseTexSize = 64.0;

    half2 uv = seed / k_BlueNoiseTexSize;

    return SAMPLE_TEXTURE2D(_BlueNoiseDitheringTexture, sampler_BlueNoiseDitheringTexture, uv).a;
}

//@ We can make white noise camera rotation independent. See ComputeFadeMaskSeed. But that would require world space view vector for all passes.
half GetWhiteNoiseDithering(uint2 seed)
{
    return GenerateHashedRandomFloat(seed);
}

half GetBayerMatrixDithering(uint2 seed)
{
    const half4x4 k_BayerMatrix = 
    {
        1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
    };

    return k_BayerMatrix[seed.x % 4][seed.y % 4];
}

half GetLODDithering(float2 crossFadeSeed, half crossFadeFactor)
{
    half d;

    switch (_LODCrossFadeType)
    {
    case k_LODCrossFadeTypeWhiteNoiseDither:
        d = GetWhiteNoiseDithering(crossFadeSeed);
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

void ApplyLODCrossFade(float4 positionCS)
{
#ifdef LOD_FADE_CROSSFADE
    half d = GetLODDithering(positionCS.xy, unity_LODFade.x);

    clip(d);
#endif
}

void ApplyLODCrossFade(float4 positionCS, inout half4 color)
{
#ifdef LOD_FADE_CROSSFADE
    #if _SURFACE_TYPE_TRANSPARENT
        bool alphaToMask = false;
    #else
        bool alphaToMask = _AlphaToMaskEnabled;
    #endif

        if (!alphaToMask)
        {
            ApplyLODCrossFade(positionCS);
        }
        else
        {
            half d = GetLODDithering(positionCS.xy, unity_LODFade.x);

            color.a = d * 5.0 + 1.0;

            clip(color.a - 0.0001);
        }
#endif
}

#endif
