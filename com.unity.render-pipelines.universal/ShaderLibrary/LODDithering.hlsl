#ifndef UNIVERSAL_PIPELINE_LODDITHERING_INCLUDED
#define UNIVERSAL_PIPELINE_LODDITHERING_INCLUDED

static const int k_LODDitherTypeBayerMatrix = 0;
static const int k_LODDitherTypeWhiteNoise = 1;
static const int k_LODDitherTypeBlueNoise = 2;

static const int k_LODDitherType = k_LODDitherTypeBlueNoise;

float GetBlueNoiseDithering(uint2 seed)
{
    const float blueNoiseTexSize = 64.0;

    float2 uv = seed / blueNoiseTexSize;

    return SAMPLE_TEXTURE2D(_BlueNoiseDitheringTexture, sampler_BlueNoiseDitheringTexture, uv).a;
}

float GetWhiteNoiseDithering(uint2 seed)
{
    return GenerateHashedRandomFloat(seed);
}

float GetBayerMatrixDithering(uint2 seed)
{
    const float4x4 k_BayerMatrix = 
    {
        1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
    };

    return k_BayerMatrix[seed.x % 4][seed.y % 4];
}

float GetLODDithering(float2 fadeMaskSeed, float ditherFactor, int ditherType)
{
    float d;

    //@ Could this be a shader_feature instead and ditherType a project parameter?
    switch (ditherType)
    {
    case k_LODDitherTypeWhiteNoise:
        d = GetWhiteNoiseDithering(fadeMaskSeed);
        break;
    case k_LODDitherTypeBlueNoise:
        d = GetBlueNoiseDithering(fadeMaskSeed);
        break;
    case k_LODDitherTypeBayerMatrix:
    default:
        d = GetBayerMatrixDithering(fadeMaskSeed);
        break;
    }

    return ditherFactor - CopySign(d, ditherFactor);
}

void LODDitheringTransition(float2 fadeMaskSeed, float ditherFactor, int ditherType)
{
    float d = GetLODDithering(fadeMaskSeed, ditherFactor, ditherType);

    clip(d);
}

void LODDitheringTransition(float2 fadeMaskSeed, float ditherFactor, int ditherType, inout float4 color)
{
#if _SURFACE_TYPE_TRANSPARENT
    bool alphaToMask = false;
#else
    bool alphaToMask = _AlphaToMaskEnabled;
#endif

    if (!alphaToMask)
    {
        LODDitheringTransition(fadeMaskSeed, ditherFactor, ditherType);
    }
    else
    {
        float d = GetLODDithering(fadeMaskSeed, ditherFactor, ditherType);

        color.a = d * 5.0 + 1.0;

        clip(color.a - 0.0001);
    }
}

#endif
