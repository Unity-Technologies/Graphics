#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

float ClosestDepth(float depthA, float depthB)
{
#if UNITY_REVERSED_Z
    return max(depthA, depthB);
#else
    return min(depthA, depthB);
#endif
}

float FarthestDepth(float depthA, float depthB)
{
#if UNITY_REVERSED_Z
    return min(depthA, depthB);
#else
    return max(depthA, depthB);
#endif
}

float ClosestDepth(float4 depths)
{
#if UNITY_REVERSED_Z
    return Max3(depths.x, depths.y, max(depths.z, depths.w));
#else
    return Min3(depths.x, depths.y, min(depths.z, depths.w));
#endif
}

float FarthestDepth(float4 depths)
{
#if UNITY_REVERSED_Z
    return Min3(depths.x, depths.y, min(depths.z, depths.w));
#else
    return Max3(depths.x, depths.y, max(depths.z, depths.w));
#endif
}

float EncodeDepthQuadIndex(float rawDepth, uint index)
{
    if (rawDepth != UNITY_RAW_FAR_CLIP_VALUE)
    {
        uint bits = asuint(rawDepth);
        bits = (bits & ~0x3) | index;
        rawDepth = asfloat(bits);
    }
    return rawDepth;
}

float4 EncodeDepthQuadIndex(float4 rawDepth)
{
    return float4(
        EncodeDepthQuadIndex(rawDepth.x, 0x0),
        EncodeDepthQuadIndex(rawDepth.y, 0x1),
        EncodeDepthQuadIndex(rawDepth.z, 0x2),
        EncodeDepthQuadIndex(rawDepth.w, 0x3));
}
