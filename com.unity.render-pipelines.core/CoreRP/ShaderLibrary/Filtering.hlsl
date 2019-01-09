#ifndef UNITY_FILTERING_INCLUDED
#define UNITY_FILTERING_INCLUDED

// Cardinal (interpolating) B-Spline of the 2nd degree (3rd order). Support = 3x3.
// The fractional coordinate of each part is assumed to be in the [0, 1] range (centered on 0.5).
// https://www.desmos.com/calculator/47j9r9lolm
real2 BSpline2IntLeft(real2 x)
{
    return 0.5 * x * x;
}

real2 BSpline2IntMiddle(real2 x)
{
    return (1 - x) * x + 0.5;
}

real2 BSpline2IntRight(real2 x)
{
    return (0.5 * x - 1) * x + 0.5;
}

// Compute weights & offsets for 4x bilinear taps for the biquadratic B-Spline filter.
// The fractional coordinate should be in the [0, 1] range (centered on 0.5).
// Inspired by: http://vec3.ca/bicubic-filtering-in-fewer-taps/

void BiquadraticFilter(float2 fracCoord, out float2 weights[2], out float2 offsets[2])
{
    float2 l = BSpline2IntLeft(fracCoord);
    float2 m = BSpline2IntMiddle(fracCoord);
    float2 r = 1 - l - m;

    // Compute offsets for 4x bilinear taps for the quadratic B-Spline reconstruction kernel.
    // 0: lerp between left and middle
    // 1: lerp between middle and right
    weights[0] = l + 0.5 * m;
    weights[1] = r + 0.5 * m;
    offsets[0] = -0.5 + 0.5 * m * rcp(weights[0]);
    offsets[1] =  0.5 + r * rcp(weights[1]);
}

// If half is natively supported, create another variant
#if HAS_HALF
void BiquadraticFilter(half2 fracCoord, out half2 weights[2], out half2 offsets[2])
{
    half2 l = BSpline2IntLeft(fracCoord);
    half2 m = BSpline2IntMiddle(fracCoord);
    half2 r = 1 - l - m;

    // Compute offsets for 4x bilinear taps for the quadratic B-Spline reconstruction kernel.
    // 0: lerp between left and middle
    // 1: lerp between middle and right
    weights[0] = l + 0.5 * m;
    weights[1] = r + 0.5 * m;
    offsets[0] = -0.5 + 0.5 * m * rcp(weights[0]);
    offsets[1] =  0.5 + r * rcp(weights[1]);
}
#endif

// Sources: https://gist.github.com/TheRealMJP/c83b8c0f46b63f3a88a5986f4fa982b1
// http://advances.realtimerendering.com/s2016/index.html
void BicubicFilter(float2 positionPixels, out float2 weights[3], out float2 uvs[3], const float sharpness)
{
    // We're going to sample a a 4x4 grid of texels surrounding the target UV coordinate. We'll do this by rounding
    // down the sample location to get the exact center of our starting texel. The starting texel will be at
    // location [1, 1] in the grid, where [0, 0] is the top left corner.
    float2 positionCenterPixels = floor(positionPixels - 0.5) + 0.5;

    // Compute the fractional offset from our starting texel to our original sample location, which we'll
    // feed into the Catmull-Rom spline function to get our filter weights.
    float2 a = positionPixels - positionCenterPixels;
    float2 a2 = a * a;
    float2 a3 = a * a2;

    // Compute the weights using the fractional offset that we calculated earlier.
    // These equations are pre-expanded based on our knowledge of where the texels will be located,
    // which lets us avoid having to evaluate a piece-wise function.
    float2 b = -sharpness * a3 + 2.0 * sharpness * a2 - sharpness * a;
    float2 c = (2.0 - sharpness) * a3 - (3.0 - sharpness) * a2 + 1.0;
    float2 d = sharpness * (a3 - a2);
    float2 e = -(2.0 - sharpness) * a3 + (3.0 - 2.0 * sharpness) * a2 + sharpness * a;

    // Work out weighting factors and sampling offsets that will let us use bilinear filtering to
    // simultaneously evaluate the middle 2 samples from the 4x4 grid.
    weights[0] = b;
    weights[1] = c + e;
    weights[2] = d;

    // Compute the final pixel coordinates we'll use for sampling.
    uvs[0] = positionCenterPixels - 1.0;
    uvs[1] = positionCenterPixels + e / weights[1];
    uvs[2] = positionCenterPixels + 2.0;
}

#endif // UNITY_FILTERING_INCLUDED
