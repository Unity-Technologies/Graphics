#ifndef UNITY_FILTERING_INCLUDED
#define UNITY_FILTERING_INCLUDED

// Basic B-Spline of the 2nd degree (3rd order, support = 4).
// The fractional coordinate of each part is assumed to be in the [0, 1] range.
// https://www.desmos.com/calculator/479pgatwlt
//
// Sample use-case:
// float2 xy = uv * resolution.xy;
// float2 ic = floor(xy);    // Note-centered (primal grid)
// float2 fc = 1 - frac(xy); // Inverse-translate the filter centered around 0.5
// Then pass x = fc.
//
real2 BSpline2Left(real2 x)
{
    return 0.5 * x * x;
}

real2 BSpline2Middle(real2 x)
{
    return (1 - x) * x + 0.5;
}

real2 BSpline2Right(real2 x)
{
    return (0.5 * x - 1) * x + 0.5;
}

// Basic B-Spline of the 3nd degree (4th order, support = 4).
// The fractional coordinate of each part is assumed to be in the [0, 1] range.
// https://www.desmos.com/calculator/479pgatwlt
//
// Sample use-case:
// float2 xy = uv * resolution.xy;
// float2 ic = round(xy) + 0.5; // Cell-centered (dual grid)
// float2 fc = ic - xy;         // Inverse-translate the the filter around 0.5 with a wrap
// Then pass x = fc.
//
real2 BSpline3Leftmost(real2 x)
{
    return 0.16666667 * x * x * x;
}

real2 BSpline3MiddleLeft(real2 x)
{
    return 0.16666667 + x * (0.5 + x * (0.5 - x * 0.5));
}

real2 BSpline3MiddleRight(real2 x)
{
    return 0.66666667 + x * (-1.0 + 0.5 * x) * x;
}

real2 BSpline3Rightmost(real2 x)
{
    return 0.16666667 + x * (-0.5 + x * (0.5 - x * 0.16666667));
}

// Compute weights & offsets for 4x bilinear taps for the bicubic B-Spline filter.
// The fractional coordinate should be in the [0, 1] range (centered on 0.5).
// Inspired by: http://vec3.ca/bicubic-filtering-in-fewer-taps/
void BicubicFilter(float2 fracCoord, out float2 weights[2], out float2 offsets[2])
{
    float2 r  = BSpline3Rightmost(fracCoord);
    float2 mr = BSpline3MiddleRight(fracCoord);
    float2 ml = BSpline3MiddleLeft(fracCoord);
    float2 l  = 1.0 - mr - ml - r;

    weights[0] = r + mr;
    weights[1] = ml + l;
    offsets[0] = -1.0 + mr * rcp(weights[0]);
    offsets[1] =  1.0 + l * rcp(weights[1]);
}

// Compute weights & offsets for 4x bilinear taps for the biquadratic B-Spline filter.
// The fractional coordinate should be in the [0, 1] range (centered on 0.5).
// Inspired by: http://vec3.ca/bicubic-filtering-in-fewer-taps/
void BiquadraticFilter(float2 fracCoord, out float2 weights[2], out float2 offsets[2])
{
    float2 l = BSpline2Left(fracCoord);
    float2 m = BSpline2Middle(fracCoord);
    float2 r = 1.0 - l - m;

    // Compute offsets for 4x bilinear taps for the quadratic B-Spline reconstruction kernel.
    // 0: lerp between left and middle
    // 1: lerp between middle and right
    weights[0] = l + 0.5 * m;
    weights[1] = r + 0.5 * m;
    offsets[0] = -0.5 + 0.5 * m * rcp(weights[0]);
    offsets[1] =  0.5 + r * rcp(weights[1]);
}

// texSize = (width, height, 1/width, 1/height)
float4 SampleTexture2DBiquadratic(TEXTURE2D_PARAM(tex, smp), float2 coord, float4 texSize)
{
    float2 xy = coord * texSize.xy;
    float2 ic = floor(xy);
    float2 fc = frac(xy);

    float2 weights[2], offsets[2];
    BiquadraticFilter(1.0 - fc, weights, offsets); // Inverse-translate the filter centered around 0.5

    // Apply the viewport scale right at the end.
    return weights[0].x * weights[0].y * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[0].x, offsets[0].y)) * (texSize.zw * 1.0), 1.0), 0.0)  // Top left
         + weights[1].x * weights[0].y * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[1].x, offsets[0].y)) * (texSize.zw * 1.0), 1.0), 0.0)  // Top right
         + weights[0].x * weights[1].y * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[0].x, offsets[1].y)) * (texSize.zw * 1.0), 1.0), 0.0)  // Bottom left
         + weights[1].x * weights[1].y * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[1].x, offsets[1].y)) * (texSize.zw * 1.0), 1.0), 0.0); // Bottom right
}

// texSize = (width, height, 1/width, 1/height)
float4 SampleTexture2DBicubic(TEXTURE2D_PARAM(tex, smp), float2 coord, float4 texSize, float2 maxCoord, uint unused /* needed to match signature of texarray version below */)
{
    float2 xy = coord * texSize.xy + 0.5;
    float2 ic = floor(xy);
    float2 fc = frac(xy);

    float2 weights[2], offsets[2];
    BicubicFilter(fc, weights, offsets);

    return weights[0].y * (weights[0].x * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[0].x, offsets[0].y) - 0.5) * texSize.zw, maxCoord), 0.0)  +
                           weights[1].x * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[1].x, offsets[0].y) - 0.5) * texSize.zw, maxCoord), 0.0)) +
           weights[1].y * (weights[0].x * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[0].x, offsets[1].y) - 0.5) * texSize.zw, maxCoord), 0.0)  +
                           weights[1].x * SAMPLE_TEXTURE2D_LOD(tex, smp, min((ic + float2(offsets[1].x, offsets[1].y) - 0.5) * texSize.zw, maxCoord), 0.0));
}

float4 SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_PARAM(tex, smp), float2 coord, float slice, float4 texSize)
{
    float2 xy = coord * texSize.xy + 0.5;
    float2 ic = floor(xy);
    float2 fc = frac(xy);

    float2 weights[2], offsets[2];
    BicubicFilter(fc, weights, offsets);

    return weights[0].y * (weights[0].x * SAMPLE_TEXTURE2D_ARRAY(tex, smp, (ic + float2(offsets[0].x, offsets[0].y) - 0.5) * texSize.zw, slice).rgba  +
                           weights[1].x * SAMPLE_TEXTURE2D_ARRAY(tex, smp, (ic + float2(offsets[1].x, offsets[0].y) - 0.5) * texSize.zw, slice).rgba) +
           weights[1].y * (weights[0].x * SAMPLE_TEXTURE2D_ARRAY(tex, smp, (ic + float2(offsets[0].x, offsets[1].y) - 0.5) * texSize.zw, slice).rgba  +
                           weights[1].x * SAMPLE_TEXTURE2D_ARRAY(tex, smp, (ic + float2(offsets[1].x, offsets[1].y) - 0.5) * texSize.zw, slice).rgba);
}

float4 SampleTexture2DArrayBicubicLOD(TEXTURE2D_ARRAY_PARAM(tex, smp), float2 coord, float slice, float4 texSize, int lod)
{
    float2 xy = coord * texSize.xy + 0.5;
    float2 ic = floor(xy);
    float2 fc = frac(xy);

    float2 weights[2], offsets[2];
    BicubicFilter(fc, weights, offsets);

    return weights[0].y * (weights[0].x * SAMPLE_TEXTURE2D_ARRAY_LOD(tex, smp, (ic + float2(offsets[0].x, offsets[0].y) - 0.5) * texSize.zw, slice, lod).rgba  +
                           weights[1].x * SAMPLE_TEXTURE2D_ARRAY_LOD(tex, smp, (ic + float2(offsets[1].x, offsets[0].y) - 0.5) * texSize.zw, slice, lod).rgba) +
           weights[1].y * (weights[0].x * SAMPLE_TEXTURE2D_ARRAY_LOD(tex, smp, (ic + float2(offsets[0].x, offsets[1].y) - 0.5) * texSize.zw, slice, lod).rgba  +
                           weights[1].x * SAMPLE_TEXTURE2D_ARRAY_LOD(tex, smp, (ic + float2(offsets[1].x, offsets[1].y) - 0.5) * texSize.zw, slice, lod).rgba);
}

#if !defined(SHADER_API_GLES)
// texSize = (width, height, 1/width, 1/height)
// texture array version for XR single-pass
float4 SampleTexture2DBicubic(TEXTURE2D_ARRAY_PARAM(tex, smp), float2 coord, float4 texSize, float2 maxCoord, uint slice)
{
    float2 xy = coord * texSize.xy + 0.5;
    float2 ic = floor(xy);
    float2 fc = frac(xy);

    float2 weights[2], offsets[2];
    BicubicFilter(fc, weights, offsets);

    return weights[0].y * (weights[0].x * SAMPLE_TEXTURE2D_ARRAY_LOD(tex, smp, min((ic + float2(offsets[0].x, offsets[0].y) - 0.5) * texSize.zw, maxCoord), slice, 0.0)  +
                           weights[1].x * SAMPLE_TEXTURE2D_ARRAY_LOD(tex, smp, min((ic + float2(offsets[1].x, offsets[0].y) - 0.5) * texSize.zw, maxCoord), slice, 0.0)) +
           weights[1].y * (weights[0].x * SAMPLE_TEXTURE2D_ARRAY_LOD(tex, smp, min((ic + float2(offsets[0].x, offsets[1].y) - 0.5) * texSize.zw, maxCoord), slice, 0.0)  +
                           weights[1].x * SAMPLE_TEXTURE2D_ARRAY_LOD(tex, smp, min((ic + float2(offsets[1].x, offsets[1].y) - 0.5) * texSize.zw, maxCoord), slice, 0.0));
}
#else
#define SampleTexture2DBicubic ERROR_ON_UNSUPPORTED_FUNCTION(SampleTexture2DBicubic)
#endif
#endif // UNITY_FILTERING_INCLUDED
