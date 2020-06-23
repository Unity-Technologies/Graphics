#define CONVERGED_ALPHA asfloat(2147483648u)

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

    // Radiance is measured in gamma/perceptual space
    float L = Luminance(LinearToGamma22(radiance));

    // Welford's online algorithm
    float4 accVariance = (iteration > 0) ? _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] : 0;
    float delta = L - accVariance.x;
    accVariance.x += delta / (accVariance.z + 1);
    float delta2 = L - accVariance.x;
    accVariance.y += delta * delta2;
    accVariance.z += 1.0;
    _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;

    return false;
}

bool CheckVariance(uint2 pixelCoords, float2 threshold)
{
    float4 accVariance = _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)];

    float variance = (accVariance.z > 0) ? accVariance.y / accVariance.z : 0;

    if (variance < threshold.x)
    {
        // update history
        accVariance.w += 1.0;
        if (accVariance.w > threshold.y)
        {
            return true;
        }
        _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;
    }
    // reset history
    accVariance.w = 0;
    _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;
    return false;
}

float3 VisualizeVariance(uint2 pixelCoords, float minVariance, float maxVariance)
{
    float4 accVariance = _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)];

    float variance = (accVariance.z > 0) ? accVariance.y / accVariance.z : 1000;
    variance = clamp(variance, minVariance, maxVariance);
    float hue = (0.6 * variance - minVariance) / (maxVariance - minVariance);
    hue = 0.6 - hue;
    return HsvToRgb(float3(hue, 1.0, 1.0));
}
