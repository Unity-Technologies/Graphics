#define CONVERGED_ALPHA asfloat(1073741824)
#define FILTER_RADIUS 0

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

    //Note: perceptual color space looks like a win for dark areas, but can make bright areas worse. Investigate...
    float L = Luminance(LinearToGamma22(exposureMultiplier * radiance.xyz));

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

float GetVariance(uint2 pixelCoords, uint iteration)
{
    float4 accVariance = _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)];
    return (accVariance.z > 0) ? accVariance.y / accVariance.z : 0;
}

bool CheckVariance(uint2 pixelCoords, uint iteration, float2 threshold)
{
    float maxVariance = 0;

    if (iteration > 0)
    {
        uint2 start = clamp(pixelCoords - FILTER_RADIUS, uint2(0, 0), _ScreenSize.xy - uint2(1, 1));
        uint2 end = clamp(pixelCoords + FILTER_RADIUS, uint2(0, 0), _ScreenSize.xy - uint2(1, 1));
        for (uint i = start.x; i <= end.x; ++i)
        {
            for (uint j = start.y; j <= end.y; ++j)
            {
                // Note: max filter creates block artifacts 
                maxVariance = max(maxVariance, GetVariance(uint2(i,j), iteration));
            }
        }
    }

    if (maxVariance < threshold.x)
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
