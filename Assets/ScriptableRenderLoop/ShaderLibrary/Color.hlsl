#ifndef UNITY_COLOR_INCLUDED
#define UNITY_COLOR_INCLUDED

//-----------------------------------------------------------------------------
// Gamma space - Assume positive values
//-----------------------------------------------------------------------------

// Gamma20
float Gamma20ToLinear(float c)
{
    return c * c;
}

float3 Gamma20ToLinear(float3 c)
{
    return c.rgb * c.rgb;
}

float4 Gamma20ToLinear(float4 c)
{
    return float4(Gamma20ToLinear(c.rgb), c.a);
}

float LinearToGamma20(float c)
{
    return sqrt(c);
}

float3 LinearToGamma20(float3 c)
{
    return sqrt(c.rgb);
}

float4 LinearToGamma20(float4 c)
{
    return float4(LinearToGamma20(c.rgb), c.a);
}

// Gamma22
float Gamma22ToLinear(float c)
{
    return pow(c, 2.2);
}

float3 Gamma22ToLinear(float3 c)
{
    return pow(c.rgb, float3(2.2, 2.2, 2.2));
}

float4 Gamma22ToLinear(float4 c)
{
    return float4(Gamma22ToLinear(c.rgb), c.a);
}

float LinearToGamma22(float c)
{
    return pow(c, 0.454545454545455);
}

float3 LinearToGamma22(float3 c)
{
    return pow(c.rgb, float3(0.454545454545455, 0.454545454545455, 0.454545454545455));
}

float4 LinearToGamma22(float4 c)
{
    return float4(LinearToGamma22(c.rgb), c.a);
}

// sRGB
float3 SRGBToLinear(float3 c)
{
    float3 linearRGBLo  = c / 12.92;
    float3 linearRGBHi  = pow((c + 0.055) / 1.055, float3(2.4, 2.4, 2.4));
    float3 linearRGB    = (c <= 0.04045) ? linearRGBLo : linearRGBHi;
    return linearRGB;
}

float4 SRGBToLinear(float4 c)
{
    return float4(SRGBToLinear(c.rgb), c.a);
}

float3 LinearToSRGB(float3 c)
{
    float3 sRGBLo = c * 12.92;
    float3 sRGBHi = (pow(c, float3(1.0/2.4, 1.0/2.4, 1.0/2.4)) * 1.055) - 0.055;
    float3 sRGB   = (c <= 0.0031308) ? sRGBLo : sRGBHi;
    return sRGB;
}

float4 LinearToSRGB(float4 c)
{
    return float4(LinearToSRGB(c.rgb), c.a);
}

// TODO: Seb - To verify and refit!
// Ref: http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
float3 FastSRGBToLinear(float3 c)
{
    return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
}

float4 FastSRGBToLinear(float4 c)
{
    return float4(FastSRGBToLinear(c.rgb), c.a);
}

float3 FastLinearToSRGB(float3 c)
{
    return max(1.055 * pow(c, 0.416666667) - 0.055, 0.0);
}

float4 FastLinearToSRGB(float4 c)
{
    return float4(FastLinearToSRGB(c.rgb), c.a);
}

//-----------------------------------------------------------------------------
// Color space
//-----------------------------------------------------------------------------

// Convert rgb to luminance
// with rgb in linear space with sRGB primaries and D65 white point
float Luminance(float3 linearRgb)
{
    return dot(linearRgb, float3(0.2126729f, 0.7151522f, 0.0721750f));
}

// Ref: http://realtimecollisiondetection.net/blog/?p=15
float4 PackLogLuv(float3 vRGB)
{
    // M matrix, for encoding
    const float3x3 M = float3x3(
        0.2209, 0.3390, 0.4184,
        0.1138, 0.6780, 0.7319,
        0.0102, 0.1130, 0.2969);

    float4 vResult;
    float3 Xp_Y_XYZp = mul(vRGB, M);
    Xp_Y_XYZp = max(Xp_Y_XYZp, float3(1e-6, 1e-6, 1e-6));
    vResult.xy = Xp_Y_XYZp.xy / Xp_Y_XYZp.z;
    float Le = 2.0 * log2(Xp_Y_XYZp.y) + 127.0;
    vResult.w = frac(Le);
    vResult.z = (Le - (floor(vResult.w*255.0f))/255.0f)/255.0f;
    return vResult;
}

float3 UnpackLogLuv(float4 vLogLuv)
{
    // Inverse M matrix, for decoding
    const float3x3 InverseM = float3x3(
        6.0014, -2.7008, -1.7996,
       -1.3320,  3.1029, -5.7721,
        0.3008, -1.0882,  5.6268);

    float Le = vLogLuv.z * 255.0 + vLogLuv.w;
    float3 Xp_Y_XYZp;
    Xp_Y_XYZp.y = exp2((Le - 127.0) / 2.0);
    Xp_Y_XYZp.z = Xp_Y_XYZp.y / vLogLuv.y;
    Xp_Y_XYZp.x = vLogLuv.x * Xp_Y_XYZp.z;
    float3 vRGB = mul(Xp_Y_XYZp, InverseM);
    return max(vRGB, float3(0.0, 0.0, 0.0));
}

// TODO: check what is really use by the lightmap... should be hardcoded
// This function must handle various crappy case of lightmap ?
float4 UnityEncodeRGBM (float3 rgb, float maxRGBM)
{
    float kOneOverRGBMMaxRange = 1.0 / maxRGBM;
    const float kMinMultiplier = 2.0 * 1e-2;

    float4 rgbm = float4(rgb * kOneOverRGBMMaxRange, 1.0);
    rgbm.a = max(max(rgbm.r, rgbm.g), max(rgbm.b, kMinMultiplier));
    rgbm.a = ceil(rgbm.a * 255.0) / 255.0;

    // Division-by-zero warning from d3d9, so make compiler happy.
    rgbm.a = max(rgbm.a, kMinMultiplier);

    rgbm.rgb /= rgbm.a;
    return rgbm;
}

// Alternative...
#define RGBMRANGE (8.0)
float4 packRGBM(float3 color)
{
    float4 rgbm;
    color *= (1.0 / RGBMRANGE);
    rgbm.a = saturate( max( max( color.r, color.g ), max( color.b, 1e-6 ) ) );
    rgbm.a = ceil( rgbm.a * 255.0 ) / 255.0;
    rgbm.rgb = color / rgbm.a;
    return rgbm;
}

float3 unpackRGBM(float4 rgbm)
{
    return RGBMRANGE * rgbm.rgb * rgbm.a;
}

// Ref: http://www.nvidia.com/object/real-time-ycocg-dxt-compression.html
#define CHROMA_BIAS (0.5 * 256.0 / 255.0)
float3 RGBToYCoCg(float3 rgb)
{
    float3 YCoCg;
    YCoCg.x = dot(rgb, float3(0.25, 0.5, 0.25));
    YCoCg.y = dot(rgb, float3(0.5, 0.0, -0.5)) + CHROMA_BIAS;
    YCoCg.z = dot(rgb, float3(-0.25, 0.5, -0.25)) + CHROMA_BIAS;

    return YCoCg;
}

float3 YCoCgToRGB(float3 YCoCg)
{
    float Y = YCoCg.x;
    float Co = YCoCg.y - CHROMA_BIAS;
    float Cg = YCoCg.z - CHROMA_BIAS;

    float3 rgb;
    rgb.r = Y + Co - Cg;
    rgb.g = Y + Cg;
    rgb.b = Y - Co - Cg;

    return rgb;
}
#endif // UNITY_COLOR_INCLUDED
