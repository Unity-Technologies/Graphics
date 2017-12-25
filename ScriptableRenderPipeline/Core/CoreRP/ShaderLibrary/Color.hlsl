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

float Luminance(float4 linearRgba)
{
    return Luminance(linearRgba.rgb);
}

// This function take a rgb color (best is to provide color in sRGB space)
// and return a YCoCg color in [0..1] space for 8bit (An offset is apply in the function)
// Ref: http://www.nvidia.com/object/real-time-ycocg-dxt-compression.html
#define YCOCG_CHROMA_BIAS (128.0 / 255.0)
float3 RGBToYCoCg(float3 rgb)
{
    float3 YCoCg;
    YCoCg.x = dot(rgb, float3(0.25, 0.5, 0.25));
    YCoCg.y = dot(rgb, float3(0.5, 0.0, -0.5)) + YCOCG_CHROMA_BIAS;
    YCoCg.z = dot(rgb, float3(-0.25, 0.5, -0.25)) + YCOCG_CHROMA_BIAS;

    return YCoCg;
}

float3 YCoCgToRGB(float3 YCoCg)
{
    float Y = YCoCg.x;
    float Co = YCoCg.y - YCOCG_CHROMA_BIAS;
    float Cg = YCoCg.z - YCOCG_CHROMA_BIAS;

    float3 rgb;
    rgb.r = Y + Co - Cg;
    rgb.g = Y + Cg;
    rgb.b = Y - Co - Cg;

    return rgb;
}

// Following function can be use to reconstruct chroma component for a checkboard YCoCg pattern
// Reference: The Compact YCoCg Frame Buffer
float YCoCgCheckBoardEdgeFilter(float centerLum, float2 a0, float2 a1, float2 a2, float2 a3)
{
    float4 lum = float4(a0.x, a1.x, a2.x, a3.x);
    // Optimize: float4 w = 1.0 - step(30.0 / 255.0, abs(lum - centerLum));
    float4 w = 1.0 - saturate((abs(lum.xxxx - centerLum) - 30.0 / 255.0) * HALF_MAX);
    float W = w.x + w.y + w.z + w.w;
    // handle the special case where all the weights are zero.
    return  (W == 0.0) ? a0.y : (w.x * a0.y + w.y* a1.y + w.z* a2.y + w.w * a3.y) / W;
}

// Hue, Saturation, Value
// Ranges:
//  Hue [0.0, 1.0]
//  Sat [0.0, 1.0]
//  Lum [0.0, HALF_MAX]
float3 RgbToHsv(float3 c)
{
    const float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    const float e = 1.0e-4;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 HsvToRgb(float3 c)
{
    const float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

// SMPTE ST.2084 (PQ) transfer functions
// 1.0 = 100nits, 100.0 = 10knits
#define DEFAULT_MAX_PQ 100.0

struct ParamsPQ
{
    float N, M;
    float C1, C2, C3;
};

static const ParamsPQ PQ =
{
    2610.0 / 4096.0 / 4.0,   // N
    2523.0 / 4096.0 * 128.0, // M
    3424.0 / 4096.0,         // C1
    2413.0 / 4096.0 * 32.0,  // C2
    2392.0 / 4096.0 * 32.0,  // C3
};

float3 LinearToPQ(float3 x, float maxPQValue)
{
    x = PositivePow(x / maxPQValue, PQ.N);
    float3 nd = (PQ.C1 + PQ.C2 * x) / (1.0 + PQ.C3 * x);
    return PositivePow(nd, PQ.M);
}

float3 LinearToPQ(float3 x)
{
    return LinearToPQ(x, DEFAULT_MAX_PQ);
}

float3 PQToLinear(float3 x, float maxPQValue)
{
    x = PositivePow(x, rcp(PQ.M));
    float3 nd = max(x - PQ.C1, 0.0) / (PQ.C2 - (PQ.C3 * x));
    return PositivePow(nd, rcp(PQ.N)) * maxPQValue;
}

float3 PQToLinear(float3 x)
{
    return PQToLinear(x, DEFAULT_MAX_PQ);
}

// Alexa LogC converters (El 1000)
// See http://www.vocas.nl/webfm_send/964
// Max range is ~58.85666

// Set to 1 to use more precise but more expensive log/linear conversions. I haven't found a proper
// use case for the high precision version yet so I'm leaving this to 0.
#define USE_PRECISE_LOGC 0

struct ParamsLogC
{
    float cut;
    float a, b, c, d, e, f;
};

static const ParamsLogC LogC =
{
    0.011361, // cut
    5.555556, // a
    0.047996, // b
    0.244161, // c
    0.386036, // d
    5.301883, // e
    0.092819  // f
};

float LinearToLogC_Precise(half x)
{
    float o;
    if (x > LogC.cut)
        o = LogC.c * log10(LogC.a * x + LogC.b) + LogC.d;
    else
        o = LogC.e * x + LogC.f;
    return o;
}

float3 LinearToLogC(float3 x)
{
#if USE_PRECISE_LOGC
    return float3(
        LinearToLogC_Precise(x.x),
        LinearToLogC_Precise(x.y),
        LinearToLogC_Precise(x.z)
    );
#else
    return LogC.c * log10(LogC.a * x + LogC.b) + LogC.d;
#endif
}

float LogCToLinear_Precise(float x)
{
    float o;
    if (x > LogC.e * LogC.cut + LogC.f)
        o = (pow(10.0, (x - LogC.d) / LogC.c) - LogC.b) / LogC.a;
    else
        o = (x - LogC.f) / LogC.e;
    return o;
}

float3 LogCToLinear(float3 x)
{
#if USE_PRECISE_LOGC
    return float3(
        LogCToLinear_Precise(x.x),
        LogCToLinear_Precise(x.y),
        LogCToLinear_Precise(x.z)
    );
#else
    return (pow(10.0, (x - LogC.d) / LogC.c) - LogC.b) / LogC.a;
#endif
}

//-----------------------------------------------------------------------------
// Utilities
//-----------------------------------------------------------------------------

// Fast reversible tonemapper
// http://gpuopen.com/optimized-reversible-tonemapper-for-resolve/
float3 FastTonemap(float3 c)
{
    return c * rcp(Max3(c.r, c.g, c.b) + 1.0);
}

float4 FastTonemap(float4 c)
{
    return float4(FastTonemap(c.rgb), c.a);
}

float3 FastTonemap(float3 c, float w)
{
    return c * (w * rcp(Max3(c.r, c.g, c.b) + 1.0));
}

float4 FastTonemap(float4 c, float w)
{
    return float4(FastTonemap(c.rgb, w), c.a);
}

float3 FastTonemapInvert(float3 c)
{
    return c * rcp(1.0 - Max3(c.r, c.g, c.b));
}

float4 FastTonemapInvert(float4 c)
{
    return float4(FastTonemapInvert(c.rgb), c.a);
}

// 3D LUT grading
// scaleOffset = (1 / lut_size, lut_size - 1)
float3 ApplyLut3D(TEXTURE3D_ARGS(tex, samplerTex), float3 uvw, float2 scaleOffset)
{
    float shift = floor(uvw.z);
    uvw.xy = uvw.xy * scaleOffset.y * scaleOffset.xx + scaleOffset.xx * 0.5;
    uvw.x += shift * scaleOffset.x;
    return SAMPLE_TEXTURE3D(tex, samplerTex, uvw).rgb;
}

// 2D LUT grading
// scaleOffset = (1 / lut_width, 1 / lut_height, lut_height - 1)
float3 ApplyLut2D(TEXTURE2D_ARGS(tex, samplerTex), float3 uvw, float3 scaleOffset)
{
    // Strip format where `height = sqrt(width)`
    uvw.z *= scaleOffset.z;
    float shift = floor(uvw.z);
    uvw.xy = uvw.xy * scaleOffset.z * scaleOffset.xy + scaleOffset.xy * 0.5;
    uvw.x += shift * scaleOffset.y;
    uvw.xyz = lerp(
        SAMPLE_TEXTURE2D(tex, samplerTex, uvw.xy).rgb,
        SAMPLE_TEXTURE2D(tex, samplerTex, uvw.xy + float2(scaleOffset.y, 0.0)).rgb,
        uvw.z - shift
    );
    return uvw;
}

// Returns the default value for a given position on a 2D strip-format color lookup table
// params = (lut_height, 0.5 / lut_width, 0.5 / lut_height, lut_height / lut_height - 1)
float3 GetLutStripValue(float2 uv, float4 params)
{
    uv -= params.yz;
    float3 color;
    color.r = frac(uv.x * params.x);
    color.b = uv.x - color.r / params.x;
    color.g = uv.y;
    return color * params.w;
}

#endif // UNITY_COLOR_INCLUDED
