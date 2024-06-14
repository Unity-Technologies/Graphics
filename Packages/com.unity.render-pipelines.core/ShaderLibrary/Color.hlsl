#ifndef UNITY_COLOR_INCLUDED
#define UNITY_COLOR_INCLUDED

#if SHADER_API_MOBILE || SHADER_API_GLES3 || SHADER_API_SWITCH || defined(UNITY_UNIFIED_SHADER_PRECISION_MODEL)
#pragma warning (disable : 3205) // conversion of larger type to smaller
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ACES.hlsl"

//-----------------------------------------------------------------------------
// Gamma space - Assume positive values
//-----------------------------------------------------------------------------

// Gamma20
real Gamma20ToLinear(real c)
{
    return c * c;
}

real3 Gamma20ToLinear(real3 c)
{
    return c.rgb * c.rgb;
}

real4 Gamma20ToLinear(real4 c)
{
    return real4(Gamma20ToLinear(c.rgb), c.a);
}

real LinearToGamma20(real c)
{
    return sqrt(c);
}

real3 LinearToGamma20(real3 c)
{
    return sqrt(c.rgb);
}

real4 LinearToGamma20(real4 c)
{
    return real4(LinearToGamma20(c.rgb), c.a);
}

// Gamma22
real Gamma22ToLinear(real c)
{
    return PositivePow(c, real(2.2));
}

real3 Gamma22ToLinear(real3 c)
{
    return PositivePow(c.rgb, real3(2.2, 2.2, 2.2));
}

real4 Gamma22ToLinear(real4 c)
{
    return real4(Gamma22ToLinear(c.rgb), c.a);
}

real LinearToGamma22(real c)
{
    return PositivePow(c, real(0.454545454545455));
}

real3 LinearToGamma22(real3 c)
{
    return PositivePow(c.rgb, real3(0.454545454545455, 0.454545454545455, 0.454545454545455));
}

real4 LinearToGamma22(real4 c)
{
    return real4(LinearToGamma22(c.rgb), c.a);
}

// sRGB
real SRGBToLinear(real c)
{
#if defined(UNITY_COLORSPACE_GAMMA) && REAL_IS_HALF
    c = min(c, 100.0); // Make sure not to exceed HALF_MAX after the pow() below
#endif
    real linearRGBLo  = c / 12.92;
    real linearRGBHi  = PositivePow((c + 0.055) / 1.055, real(2.4));
    real linearRGB    = (c <= 0.04045) ? linearRGBLo : linearRGBHi;
    return linearRGB;
}

real2 SRGBToLinear(real2 c)
{
    return real2(SRGBToLinear(c.r), SRGBToLinear(c.g));
}

real3 SRGBToLinear(real3 c)
{
    return real3(SRGBToLinear(c.r), SRGBToLinear(c.g), SRGBToLinear(c.b));
}

real4 SRGBToLinear(real4 c)
{
    return real4(SRGBToLinear(c.r), SRGBToLinear(c.g), SRGBToLinear(c.b), c.a);
}

real LinearToSRGB(real c)
{
    return (c <= 0.0031308) ? (c * 12.9232102) : 1.055 * PositivePow(c, 1.0 / 2.4) - 0.055;
}

real2 LinearToSRGB(real2 c)
{
    return real2(LinearToSRGB(c.r), LinearToSRGB(c.g));
}

real3 LinearToSRGB(real3 c)
{
    return real3(LinearToSRGB(c.r), LinearToSRGB(c.g), LinearToSRGB(c.b));
}

real4 LinearToSRGB(real4 c)
{
    return real4(LinearToSRGB(c.r), LinearToSRGB(c.g), LinearToSRGB(c.b), c.a);
}

// TODO: Seb - To verify and refit!
// Ref: http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
real FastSRGBToLinear(real c)
{
    return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
}

real2 FastSRGBToLinear(real2 c)
{
    return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
}

real3 FastSRGBToLinear(real3 c)
{
    return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
}

real4 FastSRGBToLinear(real4 c)
{
    return real4(FastSRGBToLinear(c.rgb), c.a);
}

real FastLinearToSRGB(real c)
{
    return saturate(1.055 * PositivePow(c, real(0.416666667)) - 0.055);
}

real2 FastLinearToSRGB(real2 c)
{
    return saturate(1.055 * PositivePow(c, real(0.416666667)) - 0.055);
}

real3 FastLinearToSRGB(real3 c)
{
    return saturate(1.055 * PositivePow(c, real(0.416666667)) - 0.055);
}

real4 FastLinearToSRGB(real4 c)
{
    return real4(FastLinearToSRGB(c.rgb), c.a);
}

//-----------------------------------------------------------------------------
// Color space
//-----------------------------------------------------------------------------

// Convert rgb to luminance
// with rgb in linear space with sRGB primaries and D65 white point
#ifndef BUILTIN_TARGET_API
real Luminance(real3 linearRgb)
{
    return dot(linearRgb, real3(0.2126729, 0.7151522, 0.0721750));
}
#endif

real Luminance(real4 linearRgba)
{
    return Luminance(linearRgba.rgb);
}

real AcesLuminance(real3 linearRgb)
{
    return dot(linearRgb, AP1_RGB2Y);
}

real AcesLuminance(real4 linearRgba)
{
    return AcesLuminance(linearRgba.rgb);
}

// Scotopic luminance approximation - input is in XYZ space
// Note: the range of values returned is approximately [0;4]
// "A spatial postprocessing algorithm for images of night scenes"
// William B. Thompson, Peter Shirley, and James A. Ferwerda
real ScotopicLuminance(real3 xyzRgb)
{
    real X = xyzRgb.x;
    real Y = xyzRgb.y;
    real Z = xyzRgb.z;
    return Y * (1.33 * (1.0 + (Y + Z) / X) - 1.68);
}

real ScotopicLuminance(real4 xyzRgba)
{
    return ScotopicLuminance(xyzRgba.rgb);
}

// This function take a rgb color (best is to provide color in sRGB space)
// and return a YCoCg color in [0..1] space for 8bit (An offset is apply in the function)
// Ref: http://www.nvidia.com/object/real-time-ycocg-dxt-compression.html
#define YCOCG_CHROMA_BIAS (128.0 / 255.0)
real3 RGBToYCoCg(real3 rgb)
{
    real3 YCoCg;
    YCoCg.x = dot(rgb, real3(0.25, 0.5, 0.25));
    YCoCg.y = dot(rgb, real3(0.5, 0.0, -0.5)) + YCOCG_CHROMA_BIAS;
    YCoCg.z = dot(rgb, real3(-0.25, 0.5, -0.25)) + YCOCG_CHROMA_BIAS;

    return YCoCg;
}

real3 YCoCgToRGB(real3 YCoCg)
{
    real Y = YCoCg.x;
    real Co = YCoCg.y - YCOCG_CHROMA_BIAS;
    real Cg = YCoCg.z - YCOCG_CHROMA_BIAS;

    real3 rgb;
    rgb.r = Y + Co - Cg;
    rgb.g = Y + Cg;
    rgb.b = Y - Co - Cg;

    return rgb;
}

// Following function can be use to reconstruct chroma component for a checkboard YCoCg pattern
// Reference: The Compact YCoCg Frame Buffer
real YCoCgCheckBoardEdgeFilter(real centerLum, real2 a0, real2 a1, real2 a2, real2 a3)
{
    real4 lum = real4(a0.x, a1.x, a2.x, a3.x);
    // Optimize: real4 w = 1.0 - step(30.0 / 255.0, abs(lum - centerLum));
    real4 w = 1.0 - saturate((abs(lum.xxxx - centerLum) - 30.0 / 255.0) * HALF_MAX);
    real W = w.x + w.y + w.z + w.w;
    // handle the special case where all the weights are zero.
    return  (W == 0.0) ? a0.y : (w.x * a0.y + w.y* a1.y + w.z* a2.y + w.w * a3.y) / W;
}

// Converts linear RGB to LMS
// Full float precision to avoid precision artefact when using ACES tonemapping
float3 LinearToLMS(float3 x)
{
    const real3x3 LIN_2_LMS_MAT = {
        3.90405e-1, 5.49941e-1, 8.92632e-3,
        7.08416e-2, 9.63172e-1, 1.35775e-3,
        2.31082e-2, 1.28021e-1, 9.36245e-1
    };

    return mul(LIN_2_LMS_MAT, x);
}

// Full float precision to avoid precision artefact when using ACES tonemapping
float3 LMSToLinear(float3 x)
{
    const real3x3 LMS_2_LIN_MAT = {
        2.85847e+0, -1.62879e+0, -2.48910e-2,
        -2.10182e-1,  1.15820e+0,  3.24281e-4,
        -4.18120e-2, -1.18169e-1,  1.06867e+0
    };

    return mul(LMS_2_LIN_MAT, x);
}

// Hue, Saturation, Value
// Ranges:
//  Hue [0.0, 1.0]
//  Sat [0.0, 1.0]
//  Lum [0.0, HALF_MAX]
real3 RgbToHsv(real3 c)
{
    const real4 K = real4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    real4 p = lerp(real4(c.bg, K.wz), real4(c.gb, K.xy), step(c.b, c.g));
    real4 q = lerp(real4(p.xyw, c.r), real4(c.r, p.yzx), step(p.x, c.r));
    real d = q.x - min(q.w, q.y);
    const real e = 1.0e-4;
    return real3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

real3 HsvToRgb(real3 c)
{
    const real4 K = real4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    real3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

real RotateHue(real value, real low, real hi)
{
    return (value < low)
            ? value + hi
            : (value > hi)
                ? value - hi
                : value;
}

// CIE xyY to CIE 1931 XYZ
float3 xyYtoXYZ(float3 xyY)
{
    float x = xyY.x;
    float y = xyY.y;
    float Y = xyY.z;

    float X = (Y / y) * x;
    float Z = (Y / y) * (1.0 - x - y);

    return float3(X, Y, Z);
}

// CIE 1931 XYZ to CIE xy (Y component not returned)
float2 XYZtoxy(float3 XYZ)
{
    return XYZ.xy / (dot(XYZ, 1));
}

// Soft-light blending mode use for split-toning. Works in HDR as long as `blend` is [0;1] which is
// fine for our use case.
float3 SoftLight(float3 base, float3 blend)
{
    float3 r1 = 2.0 * base * blend + base * base * (1.0 - 2.0 * blend);
    float3 r2 = sqrt(base) * (2.0 * blend - 1.0) + 2.0 * base * (1.0 - blend);
    float3 t = step(0.5, blend);
    return r2 * t + (1.0 - t) * r1;
}

// Alexa LogC converters (El 1000)
// See http://www.vocas.nl/webfm_send/964
// Max range is ~58.85666

// Set to 1 to use more precise but more expensive log/linear conversions. I haven't found a proper
// use case for the high precision version yet so I'm leaving this to 0.
#define USE_PRECISE_LOGC 0

struct ParamsLogC
{
    real cut;
    real a, b, c, d, e, f;
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

real LinearToLogC_Precise(real x)
{
    real o;
    if (x > LogC.cut)
        o = LogC.c * log10(max(LogC.a * x + LogC.b, 0.0)) + LogC.d;
    else
        o = LogC.e * x + LogC.f;
    return o;
}

// Full float precision to avoid precision artefact when using ACES tonemapping
float3 LinearToLogC(float3 x)
{
#if USE_PRECISE_LOGC
    return real3(
        LinearToLogC_Precise(x.x),
        LinearToLogC_Precise(x.y),
        LinearToLogC_Precise(x.z)
    );
#else
    return LogC.c * log10(max(LogC.a * x + LogC.b, 0.0)) + LogC.d;
#endif
}

real LogCToLinear_Precise(real x)
{
    real o;
    if (x > LogC.e * LogC.cut + LogC.f)
        o = (pow(10.0, (x - LogC.d) / LogC.c) - LogC.b) / LogC.a;
    else
        o = (x - LogC.f) / LogC.e;
    return o;
}

// Full float precision to avoid precision artefact when using ACES tonemapping
float3 LogCToLinear(float3 x)
{
#if USE_PRECISE_LOGC
    return real3(
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

real3 Desaturate(real3 value, real saturation)
{
    // Saturation = Colorfulness / Brightness.
    // https://munsell.com/color-blog/difference-chroma-saturation/
    real  mean = Avg3(value.r, value.g, value.b);
    real3 dev  = value - mean;

    return mean + dev * saturation;
}

// Fast reversible tonemapper
// http://gpuopen.com/optimized-reversible-tonemapper-for-resolve/
real FastTonemapPerChannel(real c)
{
    return c * rcp(c + 1.0);
}

real2 FastTonemapPerChannel(real2 c)
{
    return c * rcp(c + 1.0);
}

real3 FastTonemap(real3 c)
{
    return c * rcp(Max3(c.r, c.g, c.b) + 1.0);
}

real4 FastTonemap(real4 c)
{
    return real4(FastTonemap(c.rgb), c.a);
}

real3 FastTonemap(real3 c, real w)
{
    return c * (w * rcp(Max3(c.r, c.g, c.b) + 1.0));
}

real4 FastTonemap(real4 c, real w)
{
    return real4(FastTonemap(c.rgb, w), c.a);
}

real FastTonemapPerChannelInvert(real c)
{
    return c * rcp(1.0 - c);
}

real2 FastTonemapPerChannelInvert(real2 c)
{
    return c * rcp(1.0 - c);
}

real3 FastTonemapInvert(real3 c)
{
    return c * rcp(1.0 - Max3(c.r, c.g, c.b));
}

real4 FastTonemapInvert(real4 c)
{
    return real4(FastTonemapInvert(c.rgb), c.a);
}

// 3D LUT grading
// scaleOffset = (1 / lut_size, lut_size - 1)
float3 ApplyLut3D(TEXTURE3D_PARAM(tex, samplerTex), float3 uvw, float2 scaleOffset)
{
    uvw.xyz = uvw.xyz * scaleOffset.yyy * scaleOffset.xxx + scaleOffset.xxx * 0.5;
    return SAMPLE_TEXTURE3D_LOD(tex, samplerTex, uvw, 0.0).rgb;
}

// 2D LUT grading
// scaleOffset = (1 / lut_width, 1 / lut_height, lut_height - 1)
float3 ApplyLut2D(TEXTURE2D_PARAM(tex, samplerTex), float3 uvw, float3 scaleOffset)
{
    // Strip format where `height = sqrt(width)`
    uvw.z *= scaleOffset.z;
    float shift = floor(uvw.z);
    uvw.xy = uvw.xy * scaleOffset.z * scaleOffset.xy + scaleOffset.xy * 0.5;
    uvw.x += shift * scaleOffset.y;
    uvw.xyz = lerp(
        SAMPLE_TEXTURE2D_LOD(tex, samplerTex, uvw.xy, 0.0).rgb,
        SAMPLE_TEXTURE2D_LOD(tex, samplerTex, uvw.xy + float2(scaleOffset.y, 0.0), 0.0).rgb,
        uvw.z - shift
    );
    return uvw;
}

// Returns the default value for a given position on a 2D strip-format color lookup table
// params = (lut_height, 0.5 / lut_width, 0.5 / lut_height, lut_height / lut_height - 1)
float3 GetLutStripValue(float2 uv, float4 params)
{
    // scale up x from [0,1] to [0,lut_height], so that x is an integer at LUT boundaries (arranged in a strip)
    uv.x *= params.x;

    // make x vary between 0 and 1 within its LUT
    float lutBase = floor(uv.x);
    uv.x -= lutBase;

    // get the LUT index as value between 0 and (lut_height - 1)/lut_height
    float lutBaseU = lutBase * 2.0 * params.z;

    // shift the UV to vary between 0 and (lut_height - 1)/lut_height, arrange as color
    float3 color;
    color.rg = uv - params.zz;
    color.b = lutBaseU;

    // scale to vary between 0 and 1
    return color * params.w;
}

// Neutral tonemapping (Hable/Hejl/Frostbite)
// Input is linear RGB
// More accuracy to avoid NaN on extremely high values.
real3 NeutralCurve(float3 x, real a, real b, real c, real d, real e, real f)
{
    return real3(((x * (a * x + c * b) + d * e) / (x * (a * x + b) + d * f)) - e / f);
}

#define TONEMAPPING_CLAMP_MAX 435.18712 //(-b + sqrt(b * b - 4 * a * (HALF_MAX - d * f))) / (2 * a * whiteScale)
//Extremely high values cause NaN output when using fp16, we clamp to avoid the performace hit of switching to fp32
//The overflow happens in (x * (a * x + b) + d * f) of the NeutralCurve, highest value that avoids fp16 precision errors is ~571.56873
//Since whiteScale is constant (~1.31338) max input is ~435.18712

real3 NeutralTonemap(real3 x)
{
    // Make sure negative channels are clamped to 0.0 as this neutral tonemapper can't deal with them properly (unlike ACES)
    x = max((0.0).xxx, x);
    
    // Tonemap
    const real a = 0.2;
    const real b = 0.29;
    const real c = 0.24;
    const real d = 0.272;
    const real e = 0.02;
    const real f = 0.3;
    const real whiteLevel = 5.3;
    const real whiteClip = 1.0;

#if REAL_IS_HALF
    x = min(x, TONEMAPPING_CLAMP_MAX);
#endif

    real3 whiteScale = (1.0).xxx / NeutralCurve(whiteLevel, a, b, c, d, e, f);
    x = NeutralCurve(x * whiteScale, a, b, c, d, e, f);
    x *= whiteScale;

    // Post-curve white point adjustment
    x /= whiteClip.xxx;

    return x;
}

// Raw, unoptimized version of John Hable's artist-friendly tone curve
// Input is linear RGB
real EvalCustomSegment(real x, real4 segmentA, real2 segmentB)
{
    const real kOffsetX = segmentA.x;
    const real kOffsetY = segmentA.y;
    const real kScaleX  = segmentA.z;
    const real kScaleY  = segmentA.w;
    const real kLnA     = segmentB.x;
    const real kB       = segmentB.y;

    real x0 = (x - kOffsetX) * kScaleX;
    real y0 = (x0 > 0.0) ? exp(kLnA + kB * log(x0)) : 0.0;
    return y0 * kScaleY + kOffsetY;
}

real EvalCustomCurve(real x, real3 curve, real4 toeSegmentA, real2 toeSegmentB, real4 midSegmentA, real2 midSegmentB, real4 shoSegmentA, real2 shoSegmentB)
{
    real4 segmentA;
    real2 segmentB;

    if (x < curve.y)
    {
        segmentA = toeSegmentA;
        segmentB = toeSegmentB;
    }
    else if (x < curve.z)
    {
        segmentA = midSegmentA;
        segmentB = midSegmentB;
    }
    else
    {
        segmentA = shoSegmentA;
        segmentB = shoSegmentB;
    }

    return EvalCustomSegment(x, segmentA, segmentB);
}

// curve: x: inverseWhitePoint, y: x0, z: x1
real3 CustomTonemap(real3 x, real3 curve, real4 toeSegmentA, real2 toeSegmentB, real4 midSegmentA, real2 midSegmentB, real4 shoSegmentA, real2 shoSegmentB)
{
    real3 normX = x * curve.x;
    real3 ret;
    ret.x = EvalCustomCurve(normX.x, curve, toeSegmentA, toeSegmentB, midSegmentA, midSegmentB, shoSegmentA, shoSegmentB);
    ret.y = EvalCustomCurve(normX.y, curve, toeSegmentA, toeSegmentB, midSegmentA, midSegmentB, shoSegmentA, shoSegmentB);
    ret.z = EvalCustomCurve(normX.z, curve, toeSegmentA, toeSegmentB, midSegmentA, midSegmentB, shoSegmentA, shoSegmentB);
    return ret;
}

// Coming from STP, to replace when STP lands.
#define SAT 8.0f
real3 InvertibleTonemap(real3 x)
{
    real y = rcp(real(SAT) + Max3(x.r, x.g, x.b));
    return saturate(x * real(y));
}

real3 InvertibleTonemapInverse(real3 x)
{
    real y = rcp(max(real(1.0 / 32768.0), saturate(real(1.0 / SAT) - Max3(x.r, x.g, x.b) * real(1.0 / SAT))));
    return x * y;
}

// Filmic tonemapping (ACES fitting, unless TONEMAPPING_USE_FULL_ACES is set to 1)
// Input is ACES2065-1 (AP0 w/ linear encoding)
#ifndef TONEMAPPING_USE_FULL_ACES
#define TONEMAPPING_USE_FULL_ACES 0
#endif

float3 AcesTonemap(float3 aces)
{
#if TONEMAPPING_USE_FULL_ACES

    float3 oces = RRT(half3(aces));
    float3 odt = ODT_RGBmonitor_100nits_dim(oces);
    return odt;

#else

    // --- Glow module --- //
    half saturation = rgb_2_saturation(half3(aces));
    half ycIn = rgb_2_yc(half3(aces));
    half s = sigmoid_shaper((saturation - 0.4) / 0.2);
    float addedGlow = 1.0 + glow_fwd(ycIn, RRT_GLOW_GAIN * s, RRT_GLOW_MID);
    aces *= addedGlow;

    // --- Red modifier --- //
    half hue = rgb_2_hue(half3(aces));
    half centeredHue = center_hue(hue, RRT_RED_HUE);
    float hueWeight;
    {
        //hueWeight = cubic_basis_shaper(centeredHue, RRT_RED_WIDTH);
        hueWeight = smoothstep(0.0, 1.0, 1.0 - abs(2.0 * centeredHue / RRT_RED_WIDTH));
        hueWeight *= hueWeight;
    }

    aces.r += hueWeight * saturation * (RRT_RED_PIVOT - aces.r) * (1.0 - RRT_RED_SCALE);

    // --- ACES to RGB rendering space --- //
    float3 acescg = max(0.0, ACES_to_ACEScg(aces));

    // --- Global desaturation --- //
    //acescg = mul(RRT_SAT_MAT, acescg);
    acescg = lerp(dot(acescg, AP1_RGB2Y).xxx, acescg, RRT_SAT_FACTOR.xxx);

    // Apply RRT and ODT
    // https://github.com/TheRealMJP/BakingLab/blob/master/BakingLab/ACES.hlsl
    const float a = 0.0245786f;
    const float b = 0.000090537f;
    const float c = 0.983729f;
    const float d = 0.4329510f;
    const float e = 0.238081f;

#if defined(SHADER_API_SWITCH)
    // To reduce the likelyhood of extremely large values, we avoid using the x^2 term and therefore
    // divide numerator and denominator by it. This will lead to the constant factors of the
    // quadratic in the numerator and denominator to be divided by x; we add a tiny epsilon to avoid divide by 0.
    float3 rcpAcesCG = rcp(acescg + FLT_MIN);
    float3 rgbPost = (acescg + a - b * rcpAcesCG) /
        (acescg * c + d + e * rcpAcesCG);
#else
    float3 rgbPost = (acescg * (acescg + a) - b) /
        (acescg * (c * acescg + d) + e);
#endif

    // Scale luminance to linear code value
    // float3 linearCV = Y_2_linCV(rgbPost, CINEMA_WHITE, CINEMA_BLACK);

    // Apply gamma adjustment to compensate for dim surround
    float3 linearCV = darkSurround_to_dimSurround(half3(rgbPost));

    // Apply desaturation to compensate for luminance difference
    //linearCV = mul(ODT_SAT_MAT, color);
    linearCV = lerp(dot(linearCV, AP1_RGB2Y).xxx, linearCV, ODT_SAT_FACTOR.xxx);

    // Convert to display primary encoding
    // Rendering space RGB to XYZ
    float3 XYZ = mul(AP1_2_XYZ_MAT, linearCV);

    // Apply CAT from ACES white point to assumed observer adapted white point
    XYZ = mul(D60_2_D65_CAT, XYZ);

    // CIE XYZ to display primaries
    linearCV = mul(XYZ_2_REC709_MAT, XYZ);

    return linearCV;

#endif
}

// RGBM encode/decode
static const half kRGBMRange = 8.0;

half4 EncodeRGBM(half3 color)
{
    color *= 1.0 / kRGBMRange;
    half m = max(max(color.x, color.y), max(color.z, 1e-5));
    m = ceil(m * 255) / 255;
    return half4(color / m, m);
}

half3 DecodeRGBM(half4 rgbm)
{
    return rgbm.xyz * rgbm.w * kRGBMRange;
}

#if SHADER_API_MOBILE || SHADER_API_GLES3 || SHADER_API_SWITCH
#pragma warning (enable : 3205) // conversion of larger type to smaller
#endif

#endif // UNITY_COLOR_INCLUDED
