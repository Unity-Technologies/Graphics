#ifndef BLOOM_COMMON
#define BLOOM_COMMON

// Quadratic color thresholding
// curve = (threshold - knee, knee * 2, 0.25 / knee)
float3 QuadraticThreshold(float3 color, float threshold, float3 curve)
{
    // Pixel brightness
    float br = Max3(color.r, color.g, color.b);

    // Under-threshold part
    float rq = clamp(br - curve.x, 0.0, curve.y);
    rq = curve.z * rq * rq;

    // Combine and apply the brightness response curve
    color *= max(rq, br - threshold) / max(br, 1e-4);

    return color;
}

#endif // BLOOM_COMMON
