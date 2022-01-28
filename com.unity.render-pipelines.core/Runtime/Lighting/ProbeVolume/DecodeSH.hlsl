#ifndef DECODE_SH
# define DECODE_SH

// TODO: We're working on irradiance instead of radiance coefficients
//       Add safety margin 2 to avoid out-of-bounds values
#define l1scale 2.0f // Should be: 3/(2*sqrt(3)) * 2, but rounding to 2 to issues we are observing.
#define l2scale 3.5777088f // 4/sqrt(5) * 2

float3 DecodeSH(float l0, float3 l1)
{
    return (l1 - 0.5f) * 2.0f * l1scale * l0;
}

void DecodeSH_L2(inout float3 l0, inout float4 l2_R, inout float4 l2_G, inout float4 l2_B, inout float4 l2_C)
{
    l2_R = (l2_R - 0.5f) * l2scale * l0.r;
    l2_G = (l2_G - 0.5f) * l2scale * l0.g;
    l2_B = (l2_B - 0.5f) * l2scale * l0.b;
    l2_C = (l2_C - 0.5f) * l2scale;

    l2_C.rgb *= l0;

    // Account for how L2 is encoded.
    l0.r -= l2_R.z;
    l0.g -= l2_G.z;
    l0.b -= l2_B.z;
    l2_R.z *= 3.0f;
    l2_G.z *= 3.0f;
    l2_B.z *= 3.0f;
}

float3 EncodeSH(float l0, float3 l1)
{
    return l0 == 0.0f ? 0.5f : l1 * rcp(l0) / (2.0f * l1scale) + 0.5f;
}

void EncodeSH_L2(inout float3 l0, inout float4 l2_R, inout float4 l2_G, inout float4 l2_B, inout float3 l2_C)
{
    // Account for how L2 is encoded.
    l2_R.z /= 3.0f;
    l2_G.z /= 3.0f;
    l2_B.z /= 3.0f;
    l0.r += l2_R.z;
    l0.g += l2_G.z;
    l0.b += l2_B.z;

    float3 rcpl0 = rcp(l0);
    rcpl0 = float3(l0.x == 0.0f ? 0.0f : rcpl0.x, l0.y == 0.0f ? 0.0f : rcpl0.y, l0.z == 0.0f ? 0.0f : rcpl0.z);

    l2_R = 0.5f + l2_R * rcp(l2scale) * rcpl0.r;
    l2_G = 0.5f + l2_G * rcp(l2scale) * rcpl0.g;
    l2_B = 0.5f + l2_B * rcp(l2scale) * rcpl0.b;
    l2_C = 0.5f + l2_C * rcp(l2scale) * rcpl0;
}

#undef l1scale
#undef l2scale

#endif // DECODE_SH
