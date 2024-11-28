#ifndef VRS_SHADING_RATES_INCLUDED
#define VRS_SHADING_RATES_INCLUDED

// Must be kept in sync with ShadingRateFragmentSize
#define SHADING_RATE_FRAGMENT_SIZE_1X1   0 // native value = 0b0000 -> 0
#define SHADING_RATE_FRAGMENT_SIZE_1X2   1 // native value = 0b0001 -> 1
#define SHADING_RATE_FRAGMENT_SIZE_2X1   2 // native value = 0b0100 -> 4
#define SHADING_RATE_FRAGMENT_SIZE_2X2   3 // native value = 0b0101 -> 5
#define SHADING_RATE_FRAGMENT_SIZE_1x4   4 // native value = 0b0010 -> 2
#define SHADING_RATE_FRAGMENT_SIZE_4x1   5 // native value = 0b1000 -> 8
#define SHADING_RATE_FRAGMENT_SIZE_2x4   6 // native value = 0b0110 -> 6
#define SHADING_RATE_FRAGMENT_SIZE_4x2   7 // native value = 0b1001 -> 9
#define SHADING_RATE_FRAGMENT_SIZE_4x4   8 // native value = 0b1010 -> 10
#define SHADING_RATE_FRAGMENT_SIZE_COUNT 9

StructuredBuffer<uint> _ShadingRateNativeValues;

/// <summary>
/// Unpack a shading rate native value into its horizontal and vertical components.
/// </summary>
/// <param name="shadingRateNativeValue">Shading rate native value to unpack.</param>
/// <returns>Unpacked value where x component is the horizontal shading rate and y is the vertical shading rate.</returns>
uint2 UnpackShadingRate(uint shadingRateNativeValue)
{
    return uint2((shadingRateNativeValue >> 2) & 0x03, shadingRateNativeValue & 0x03);
}

/// <summary>
/// Pack an unpacked shading rates into its native value.
/// </summary>
/// <param name="unpackedShadingRate">Unpacked shading rate.</param>
/// <returns>Shading rate native value.</returns>
uint PackShadingRate(uint2 unpackedShadingRate)
{
    // If using 4x4 rate, be careful to check for invalid rates
    // if (shadingRate.x == 2 && shadingRate.y == 0)
    //     shadingRate.y = 1;
    //
    // if (shadingRate.x == 0 && shadingRate.y == 2)
    //     shadingRate.x = 1;

    return (unpackedShadingRate.x << 2) | unpackedShadingRate.y;
}

#endif // VRS_SHADING_RATES_INCLUDED
