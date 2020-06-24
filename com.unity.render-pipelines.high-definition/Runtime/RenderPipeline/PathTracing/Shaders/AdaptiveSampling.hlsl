#define CONVERGED_ALPHA asfloat(1073741824)
#define FILTER_RADIUS 4
#define PACK_SUCCESSIVE_HITS

// Storage format:
// x: the mean value
// y: the squared distance to the mean
// z: sample count
// w: unfitered luminance 
RW_TEXTURE2D_X(float4, _AccumulatedVariance);

#ifndef PACK_SUCCESSIVE_HITS

float IncrementSampleCount(inout float4 data)
{
    data.z += 1.0;
    return data.z;
}

float ReadSampleCount(float4 data)
{
    return data.z;
}

#else

float IncrementSampleCount(inout float4 data)
{
    uint bits = asuint(data.z);
    bits += 1;
    data.z = asfloat(bits);
    return bits & 0xFFFF;
}

int IncrementHitCount(inout float4 data)
{
    int bits = asint(data.z);
    bits += (1 << 16);
    data.z = asfloat(bits);
    return bits >> 16;
}

void ResetHitCount(inout float4 data)
{
    uint bits = asuint(data.z);
    bits &= 0xFFFF;
}

float ReadSampleCount(float4 data)
{
    uint bits = asuint(data.z);
    return bits & 0xFFFF;
}

float ReadHitCount(float4 data)
{
    uint bits = asuint(data.z);
    return bits >> 16;
}

#endif

bool UpdatePerPixelVariance(uint2 pixelCoords, uint iteration, float exposureMultiplier, float4 radiance)
{
    if (radiance.w == CONVERGED_ALPHA)
    {
        return true;
    }

    float L = Luminance(LinearToGamma22(exposureMultiplier * radiance.xyz));

    float4 accVariance = (iteration > 0) ? _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] : 0;
    float count = IncrementSampleCount(accVariance);

    // first compute the (unfiltered) accumulated luminance
    float deltaL = L - accVariance.w;
    accVariance.w += deltaL / count;

    // Then find the variance of this value using Welford's online algorithm
    float delta = accVariance.w - accVariance.x;
    accVariance.x += delta / count;
    float delta2 = accVariance.w - accVariance.x;
    accVariance.y += delta * delta2;

    _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;

    return false;
}

float DecodeVariance(float4 data)
{
    float count = ReadSampleCount(data);
    return (count > 0) ? data.y / count : 0.0f;
}

float GetVariance(uint2 pixelCoords)
{
    float4 data = _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)];
    return DecodeVariance(data);
}

float4 FetchPackedVariance(uint2 pixelCoords, uint iteration)
{
   return (iteration > 0) ? _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] : 0;
}

bool CheckVariance(uint2 pixelCoords, float2 threshold, inout uint iteration)
{
    // first read the current pixel (offset 0,0) to extract variance and iteration number
    float4 data = FetchPackedVariance(pixelCoords, iteration);
    float totalVariance = DecodeVariance(data);
    // update the iteration number with the per-pixel one
    iteration = ReadSampleCount(data);

    if (iteration > 0)
    {
        // partially unrolled, to avoid reading again the (0, 0) coordinate
        for (int i = 1; i <= FILTER_RADIUS; ++i)
        {
            for (int j = 1; j <= FILTER_RADIUS; ++j)
            {
                uint2 crd = clamp(pixelCoords + int2(i, j), int2(0, 0), _ScreenSize.xy - int2(1, 1));
                totalVariance = max(totalVariance, GetVariance(crd));
                crd = clamp(pixelCoords + int2(-i, j), int2(0, 0), _ScreenSize.xy - int2(1, 1));
                totalVariance = max(totalVariance, GetVariance(crd));
                crd = clamp(pixelCoords + int2(i, -j), int2(0, 0), _ScreenSize.xy - int2(1, 1));
                totalVariance = max(totalVariance, GetVariance(crd));
                crd = clamp(pixelCoords + int2(-i, -j), int2(0, 0), _ScreenSize.xy - int2(1, 1));
                totalVariance = max(totalVariance, GetVariance(crd));
            }
        }

        for (int j = 1; j <= FILTER_RADIUS; ++j)
        {
            uint2 crd = clamp(pixelCoords + int2(0, j), int2(0, 0), _ScreenSize.xy - int2(1, 1));
            totalVariance = max(totalVariance, GetVariance(crd));
            crd = clamp(pixelCoords + int2(0, -j), int2(0, 0), _ScreenSize.xy - int2(1, 1));
            totalVariance = max(totalVariance, GetVariance(crd));

            crd = clamp(pixelCoords + int2(j, 0), int2(0, 0), _ScreenSize.xy - int2(1, 1));
            totalVariance = max(totalVariance, GetVariance(crd));
            crd = clamp(pixelCoords + int2(-j, 0), int2(0, 0), _ScreenSize.xy - int2(1, 1));
            totalVariance = max(totalVariance, GetVariance(crd));
        }
    }

    if (totalVariance < threshold.x)
    {
#ifdef PACK_SUCCESSIVE_HITS
        // update history
        int hits = IncrementHitCount(data);
        _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = data;
        if (hits > threshold.y)
        {
            return true;
        }
        return false;
#else
        return true;
#endif
    }
#ifdef PACK_SUCCESSIVE_HITS
    // reset history
    ResetHitCount(data);
    _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = data;
#endif
    return false;
}

float3 VisualizeVariance(uint2 pixelCoords, float minVariance, float maxVariance)
{
    float variance = GetVariance(pixelCoords);
    variance = clamp(variance, minVariance, maxVariance);
    float hue = (variance - minVariance) / (maxVariance - minVariance);
    hue *= 0.6f;
    hue = 0.6 - hue;
    return HsvToRgb(float3(hue, 1.0, 1.0));
}
