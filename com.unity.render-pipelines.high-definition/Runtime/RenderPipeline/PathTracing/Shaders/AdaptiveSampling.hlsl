#define CONVERGED_ALPHA asfloat(1073741824)

// Storage format:
// x: the mean value
// y: the squared distance to the mean
// z: sample count
// w: counts how many successive times this pixel passed the variance test
RW_TEXTURE2D_X(float4, _AccumulatedVariance);

bool UpdatePerPixelVariance(uint2 pixelCoords, uint iteration, float4 radiance)
{
    if (radiance.w == CONVERGED_ALPHA)
    {
        return true;
    }

    float L = Luminance(radiance.xyz);

    // Welford's online algorithm
    float4 accVariance = (iteration > 0) ? _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] : 0;
    accVariance.z += 1.0;
    float delta = L - accVariance.x;
    accVariance.x += delta / accVariance.z;
    float delta2 = L - accVariance.x;
    accVariance.y += delta * delta2;

    _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;

    return false;
}

bool CheckVariance(uint2 pixelCoords, uint iteration, float2 threshold)
{
    float4 accVariance = (iteration > 0) ? _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] : 0;

    float variance = (accVariance.z > 0) ? accVariance.y / accVariance.z : 0;

    if (variance < threshold.x)
    {
        // update history
        accVariance.w += 1.0;
        _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;
        if (accVariance.w > threshold.y)
        {
            return true;
        }
    }
    // reset history
    accVariance.w = 0;
    _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;
    return false;
}

float3 VisualizeVariance(uint2 pixelCoords, float minVariance, float maxVariance)
{
    float4 accVariance = _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)];

    float variance = (accVariance.z > 0) ? accVariance.y / accVariance.z : 0;
    variance = clamp(variance, minVariance, maxVariance);
    float hue = (0.6 * variance - minVariance) / (maxVariance - minVariance);
    hue = 0.6 - hue;
    return HsvToRgb(float3(hue, 1.0, 1.0));
}
