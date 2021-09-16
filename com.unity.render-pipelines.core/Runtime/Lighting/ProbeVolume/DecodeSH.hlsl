#ifndef DECODE_SH
# define DECODE_SH

float3 DecodeSH(float l0, float3 l1)
{
    // TODO: We're working on irradiance instead of radiance coefficients
    //       Add safety margin 2 to avoid out-of-bounds values
    const float l1scale = 2.0f; // Should be: 3/(2*sqrt(3)) * 2, but rounding to 2 to issues we are observing.

    return (l1 - 0.5f) * 2.0f * l1scale * l0;
}

void DecodeSH_L2(inout float3 l0, inout float4 l2_R, inout float4 l2_G, inout float4 l2_B, inout float4 l2_C)
{
    // TODO: We're working on irradiance instead of radiance coefficients
    //       Add safety margin 2 to avoid out-of-bounds values
    const float l2scale = 3.5777088f; // 4/sqrt(5) * 2

    l2_R = (l2_R - 0.5f) * l2scale * l0.r;
    l2_G = (l2_G - 0.5f) * l2scale * l0.g;
    l2_B = (l2_B - 0.5f) * l2scale * l0.b;
    l2_C = (l2_C - 0.5f) * l2scale;

    l2_C.r *= l0.r;
    l2_C.g *= l0.g;
    l2_C.b *= l0.b;

    // Account for how L2 is encoded.
    l0.r -= l2_R.z;
    l0.g -= l2_G.z;
    l0.b -= l2_B.z;
    l2_R.z *= 3.0f;
    l2_G.z *= 3.0f;
    l2_B.z *= 3.0f;
}

#endif // DECODE_SH
