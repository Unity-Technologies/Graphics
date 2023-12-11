#ifndef DECODE_SH
# define DECODE_SH

// TODO: We're working on irradiance instead of radiance coefficients
//       Add safety margin 2 to avoid out-of-bounds values
#define APV_L1_ENCODING_SCALE 2.0 // Should be: 3/(2*sqrt(3)) * 2, but rounding to 2 to issues we are observing.
#define APV_L2_ENCODING_SCALE 3.5777088 // 4/sqrt(5) * 2

float3 DecodeSH(float l0, float3 l1)
{
    return (l1 - 0.5f) * (2.0f * APV_L1_ENCODING_SCALE * l0);
}

void DecodeSH_L2(inout float3 l0, inout float4 l2_R, inout float4 l2_G, inout float4 l2_B, inout float3 l2_C)
{
    l2_R = (l2_R - 0.5f) * (APV_L2_ENCODING_SCALE * l0.r);
    l2_G = (l2_G - 0.5f) * (APV_L2_ENCODING_SCALE * l0.g);
    l2_B = (l2_B - 0.5f) * (APV_L2_ENCODING_SCALE * l0.b);
    l2_C = (l2_C - 0.5f) * APV_L2_ENCODING_SCALE;

    l2_C.rgb *= l0;

    // Account for how L2 is encoded.
    l0.r -= l2_R.z;
    l0.g -= l2_G.z;
    l0.b -= l2_B.z;
    l2_R.z *= 3.0f;
    l2_G.z *= 3.0f;
    l2_B.z *= 3.0f;
}

void DecodeSH_L2(inout float3 l0, inout float4 l2_R, inout float4 l2_G, inout float4 l2_B, inout float4 l2_C)
{
    float3 outL2_C = l2_C.xyz;
    DecodeSH_L2(l0, l2_R, l2_G, l2_B, outL2_C);
    l2_C = float4(outL2_C.xyz, 0);
}

half3 DecodeSH(half l0, half3 l1)
{
    return (l1 - 0.5) * (2.0 * APV_L1_ENCODING_SCALE * l0);
}

void DecodeSH_L2(inout half3 l0, inout half4 l2_R, inout half4 l2_G, inout half4 l2_B, inout half3 l2_C)
{
    l2_R = (l2_R - 0.5) * (APV_L2_ENCODING_SCALE * l0.r);
    l2_G = (l2_G - 0.5) * (APV_L2_ENCODING_SCALE * l0.g);
    l2_B = (l2_B - 0.5) * (APV_L2_ENCODING_SCALE * l0.b);
    l2_C = (l2_C - 0.5) * APV_L2_ENCODING_SCALE;

    l2_C.rgb *= l0;

    // Account for how L2 is encoded.
    l0.r -= l2_R.z;
    l0.g -= l2_G.z;
    l0.b -= l2_B.z;
    l2_R.z *= 3.0;
    l2_G.z *= 3.0;
    l2_B.z *= 3.0;
}

void DecodeSH_L2(inout half3 l0, inout half4 l2_R, inout half4 l2_G, inout half4 l2_B, inout half4 l2_C)
{
    half3 outL2_C = l2_C.xyz;
    DecodeSH_L2(l0, l2_R, l2_G, l2_B, outL2_C);
    l2_C = half4(outL2_C.xyz, 0);
}

float3 EncodeSH(float l0, float3 l1)
{
    return l0 == 0.0f ? 0.5f : l1 * rcp(l0) / (2.0f * APV_L1_ENCODING_SCALE) + 0.5f;
}

#if !HALF_IS_FLOAT
half3 EncodeSH(half l0, half3 l1)
{
    return l0 == 0.0 ? 0.5 : l1 * rcp(l0) / (2.0 * APV_L1_ENCODING_SCALE) + 0.5;
}
#endif

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

    l2_R = 0.5f + l2_R * rcp(APV_L2_ENCODING_SCALE) * rcpl0.r;
    l2_G = 0.5f + l2_G * rcp(APV_L2_ENCODING_SCALE) * rcpl0.g;
    l2_B = 0.5f + l2_B * rcp(APV_L2_ENCODING_SCALE) * rcpl0.b;
    l2_C = 0.5f + l2_C * rcp(APV_L2_ENCODING_SCALE) * rcpl0;
}

#endif // DECODE_SH
