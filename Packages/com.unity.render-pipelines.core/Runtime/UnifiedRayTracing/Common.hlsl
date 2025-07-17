#ifndef _UNIFIEDRAYTRACING_COMMON_HLSL_
#define _UNIFIEDRAYTRACING_COMMON_HLSL_

#define K_T_MAX                 400000
#ifndef FLT_EPSILON
#define FLT_EPSILON             1.192092896e-07F
#endif

#ifndef FLT_MAX
#define FLT_MAX 3.402823e+38
#endif

#define K_T_MAX                 400000

float Max3(float3 val)
{
    return max(max(val.x, val.y), val.z);
}

// Adapted from RayTracing Gems, A Fast and Robust Method for Avoiding Self-Intersection
// - Dropped the exact +N ulp computation, instead use, N * epsilon
// - Use max of distance components instead of per component offset
// - Use less conservative factors for error estimation
float3 OffsetRayOrigin(float3 p, float3 n, float customOffset = 0.0f)
{
    float distanceToOrigin = Max3(abs(p));
    float offset = (distanceToOrigin < 1 / 32.0f) ? FLT_EPSILON * 64.0f : FLT_EPSILON * 64.0f * distanceToOrigin;

    return p + (offset + customOffset) * n;
}

#endif
