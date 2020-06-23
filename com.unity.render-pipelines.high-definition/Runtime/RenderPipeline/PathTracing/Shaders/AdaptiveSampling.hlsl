#define CONVERGED_ALPHA asfloat(1073741824)

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

    float L = exposureMultiplier * Luminance(radiance.xyz);

    float4 accVariance = (iteration > 0) ? _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] : 0;
    accVariance.z += 1.0;

    // first compute the accumulated luminance
    float deltaL = L - accVariance.w;
    accVariance.w += deltaL / accVariance.z;

    // The pass this to Welford's online algorithm for variance computation
    float delta = accVariance.w - accVariance.x;
    accVariance.x += delta / accVariance.z;
    float delta2 = accVariance.w - accVariance.x;
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
        // accVariance.w += 1.0;
        _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;
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
