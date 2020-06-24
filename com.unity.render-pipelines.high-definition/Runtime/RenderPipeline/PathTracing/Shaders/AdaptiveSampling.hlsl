#define CONVERGED_ALPHA asfloat(1073741824)
#define FILTER_RADIUS 4

// Storage format:
// x: the mean value
// y: the squared distance to the mean
// z: sample count
// w: luminance 
// TODO: count how many successive times this pixel passed the variance test
RW_TEXTURE2D_X(float4, _AccumulatedVariance);

bool UpdatePerPixelVariance(uint2 pixelCoords, uint iteration, float exposureMultiplier, float4 radiance)
{
    if (radiance.w == CONVERGED_ALPHA)
    {
        return true;
    }

    float L = Luminance(LinearToGamma22(exposureMultiplier * radiance.xyz));

    float4 accVariance = (iteration > 0) ? _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] : 0;
    accVariance.z += 1.0;

    // first compute the (unfiltered) accumulated luminance
    float deltaL = L - accVariance.w;
    accVariance.w += deltaL / accVariance.z;

    // Then find the variance of this value using Welford's online algorithm
    float delta = accVariance.w - accVariance.x;
    accVariance.x += delta / accVariance.z;
    float delta2 = accVariance.w - accVariance.x;
    accVariance.y += delta * delta2;

    _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;

    return false;
}

float DecodeVariance(float4 data)
{
    return (data.z > 0) ? data.y / data.z : 0;
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
    float4 data = FetchPackedVariance(pixelCoords, iteration);
    float totalVariance = DecodeVariance(data);
    // update the iteration number with the per pixel data
    iteration = data.z;

    if (iteration > 0)
    {
        // partially unrolled, to avoid reading agian the (0, 0) coordinate
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
        }

        for (int k = 1; k <= FILTER_RADIUS; ++k)
        {
            uint2 crd = clamp(pixelCoords + int2(k, 0), int2(0, 0), _ScreenSize.xy - int2(1, 1));
            totalVariance = max(totalVariance, GetVariance(crd));
            crd = clamp(pixelCoords + int2(-k, 0), int2(0, 0), _ScreenSize.xy - int2(1, 1));
            totalVariance = max(totalVariance, GetVariance(crd));
        }
    }

    if (totalVariance < threshold.x)
    {
        // update history
        // accVariance.w += 1.0;
        // _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;
        // if (accVariance.w > threshold.y)
        {
            return true;
        }
        //return false;
    }
    // reset history
    //accVariance.w = 0;
    //_AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;
    return false;
}

float3 VisualizeVariance(uint2 pixelCoords, float minVariance, float maxVariance)
{
    float4 accVariance = _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)];

    float variance = (accVariance.z > 0) ? accVariance.y / accVariance.z : 0;
    variance = clamp(variance, minVariance, maxVariance);
    float hue = (variance - minVariance) / (maxVariance - minVariance);
    hue *= 0.6f;
    hue = 0.6 - hue;
    return HsvToRgb(float3(hue, 1.0, 1.0));
}
