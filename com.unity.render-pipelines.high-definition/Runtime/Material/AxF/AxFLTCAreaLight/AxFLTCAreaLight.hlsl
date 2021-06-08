#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"

// Expects NdotV clamped in [0,1]
float2  LTCGetSamplingUV(float NdotV, float perceptualRoughness)
{
    float2 xy;
    xy.x = perceptualRoughness;
    xy.y = sqrt( 1 - NdotV );

// Original code
//    return LTC_LUT_OFFSET + LTC_LUT_SCALE * float2( perceptualRoughness, theta * INV_HALF_PI );

    xy *= (LTC_LUT_SIZE-1);     // 0 is pixel 0, 1 = last pixel in the table
    xy += 0.5;                  // Perfect pixel sampling starts at the center
    return xy / LTC_LUT_SIZE;   // Finally, return UVs in [0,1]
}

// Fetches the transposed M^-1 matrix need for runtime LTC estimate
float3x3 LTCSampleMatrix(float2 UV, uint BRDFIndex)
{
    // Note we load the matrix transpose (to avoid having to transpose it in shader)
    float3x3    invM = 0.0;
                invM._m22 = 1.0;
                invM._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, UV, BRDFIndex, 0);

    return invM;
}
