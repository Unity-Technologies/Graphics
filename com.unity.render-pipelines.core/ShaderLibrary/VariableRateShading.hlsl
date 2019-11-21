#ifndef UNITY_VARIABLE_RATE_SHADING_INCLUDED
#define UNITY_VARIABLE_RATE_SHADING_INCLUDED

// Density : normalized float to represent the shading rate. 0 and negative values are considered fully culled.
// ShadingRate : integer value representing either an direct shading rate or an index into a palette

CBUFFER_START(UnityVRS)
    //uint _ShadingRateMode;
    uint _ShadingRateMin;
    uint _ShadingRateMax;
CBUFFER_END

uint4 VRS_DensityToShadingRate(float density)
{
    // Fully culled
    if (density <= 0.0f)
        return (0).xxxx;

    return lerp(_ShadingRateMin, _ShadingRateMax, saturate(density)).xxxx;
}

float VRS_ShadingRateToDensity(uint shadingRate)
{
    return (float)(shadingRate - _ShadingRateMin) / (float)(_ShadingRateMax - _ShadingRateMin);
}

#endif // UNITY_VARIABLE_RATE_SHADING_INCLUDED
