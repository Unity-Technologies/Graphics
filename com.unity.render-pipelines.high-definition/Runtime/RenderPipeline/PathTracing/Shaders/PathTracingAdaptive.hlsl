// Adaptive sampling resources
RW_TEXTURE2D_X(float4, _VarianceTexture);
RW_TEXTURE2D_X(uint, _MaxVariance);         // Unused!
RW_TEXTURE2D_X(uint, _ScratchBuffer);       // Unused!

/**
* Variance estimate modes:
* VARIANCE_ESTIMATE 0: Textbook variance imlpementation, with precision issues: https://en.wikipedia.org/wiki/Variance
* VARIANCE_ESTIMATE 1: Running average implementation described in SVGF paper : https://cg.ivd.kit.edu/publications/2017/svgf/svgf_preprint.pdf
*/
#define VARIANCE_ESTIMATE   1
#define MIN_ITERATIONS      32
#define VARIANCE_THRESHOLD  0.0001
#define HYSTERISIS          0.15
#define FILTER_RADIUS       2             // 0 disables variance filtering
#define FILTER_SHARPNESS    1.0
#define HEATMAP_MAX         0.01
#define FILTER_FUNC         MaxFilter
#define ENABLE_ADAPTIVE_SAMPLING
#define USE_GAMMA_SPACE
#define ENABLE_HEATMAP
#define INVERT_HEATMAP      // When inverted, a red hue means high variance and blue low 

//========================= FILTER FUNCTIONS =======================

void BoxFilter(float r2, float4 newSample, inout float accumColor, inout float accumWeight)
{
    accumColor += newSample;
    accumWeight += 1.0;
}

void MaxFilter(float r2, float4 newSample, inout float accumColor, inout float accumWeight )
{
    accumColor = max(accumColor, newSample);
    accumWeight = 1.0;
}

void GaussFilter(float r2, float4 newSample, inout float accumColor, inout float accumWeight)
{
    float weight = exp(-FILTER_SHARPNESS * r2);
    accumColor += weight * newSample;
    accumWeight += weight;
}

//======================================================================

void ClearPerPixelVariance(uint2 currentPixelCoord)
{
    _VarianceTexture[int3(currentPixelCoord, 0)] = float4(0.0, 0.0, 0.0, 0.0);
}

void UpdatePerPixelVariance(uint2 currentPixelCoord, float3 colorIn)
{
#ifdef USE_GAMMA_SPACE
    float L = Luminance(LinearToGamma22(colorIn));
#else
    float L = Luminance(colorIn);
#endif

    uint3 crd = uint3(currentPixelCoord, 0);
#if (VARIANCE_ESTIMATE == 0)
    // Note: This version has serious precision issues for large sequences
    _VarianceTexture[crd].x += L;               // first moment
    _VarianceTexture[crd].y += L * L;           // second moment
#elif (VARIANCE_ESTIMATE == 1)
    _VarianceTexture[crd].x = lerp(_VarianceTexture[crd].x, L, HYSTERISIS);     // first moment
    _VarianceTexture[crd].y = lerp(_VarianceTexture[crd].y, L * L, HYSTERISIS); // second moment
#endif
}

float EstimateUnfilteredVariance(uint2 currentPixelCoord)
{
    uint3 crd = uint3(currentPixelCoord, 0);
    float value = 0;
    if(_RaytracingFrameIndex > 0)
    {
#if (VARIANCE_ESTIMATE == 0)
        float m_1 = _VarianceTexture[crd].x / _RaytracingFrameIndex;
        m_1 *= m_1;
        value = abs(_VarianceTexture[crd].y / _RaytracingFrameIndex - m_1);
#elif (VARIANCE_ESTIMATE == 1)
        value = 10 * abs(_VarianceTexture[crd].y - _VarianceTexture[crd].x * _VarianceTexture[crd].x);
#endif
    }
    return value; 
}

// TODO: brute force place holder until the compute shader works
float EstimateFilteredVariance(uint2 currentPixelCoord)
{
    uint width, height, _unused;
    _VarianceTexture.GetDimensions(width, height, _unused);
    float accumVariance = 0.0;
    float accumWeight = 0.0;
    if (any(currentPixelCoord < FILTER_RADIUS) || any(currentPixelCoord > uint2(width - FILTER_RADIUS, height - FILTER_RADIUS)))
    {
        return EstimateUnfilteredVariance(currentPixelCoord);
    }

    for (int x = currentPixelCoord.x - FILTER_RADIUS; x <= currentPixelCoord.x + FILTER_RADIUS; ++x)
    {
        for (int y = currentPixelCoord.y - FILTER_RADIUS; y <= currentPixelCoord.y + FILTER_RADIUS; ++y)
        {
            int3 crd = int3(x, y, 0);
                float squareDist = dot(crd - currentPixelCoord, crd - currentPixelCoord);
            FILTER_FUNC(squareDist, EstimateUnfilteredVariance(crd), accumVariance, accumWeight);
        }
    }
    return accumVariance / accumWeight;

}

float EstimateVariance(uint2 currentPixelCoord)
{
#if (FILTER_RADIUS == 0)
    return EstimateUnfilteredVariance(currentPixelCoord);
#else
    return EstimateFilteredVariance(currentPixelCoord);
#endif
}

float4 VarianceHeatMap(float variance)
{
    float maxVariance = HEATMAP_MAX;  //asfloat(_MaxVariance[uint3(0,0,0)]);
    float minVariance = 0;
    variance = clamp(variance, 0.0, maxVariance);
    // 60% of the hue range goes from red to blue (we don't want magenta in the heatmap)
    float hue =  (0.6 * variance - minVariance)  / (maxVariance - minVariance);
#ifdef INVERT_HEATMAP
    hue = 0.6 - hue;
#endif
    return float4(HsvToRgb(float3(hue, 1.0, 1.0)), 1.0);
}
