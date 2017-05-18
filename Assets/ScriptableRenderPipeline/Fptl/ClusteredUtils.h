#ifndef __CLUSTEREDUTILS_H__
#define __CLUSTEREDUTILS_H__

#ifndef FLT_EPSILON
    #define FLT_EPSILON     1.192092896e-07f
#endif

// Using pow often result to a warning like this
// "pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them"
// PositivePow remove this warning when you know the value is positive and avoid inf/NAN.
float PositivePow(float base, float power)
{
    return pow(max(abs(base), float(FLT_EPSILON)), power);
}

float2 PositivePow(float2 base, float2 power)
{
    return pow(max(abs(base), float2(FLT_EPSILON, FLT_EPSILON)), power);
}

float3 PositivePow(float3 base, float3 power)
{
    return pow(max(abs(base), float3(FLT_EPSILON, FLT_EPSILON, FLT_EPSILON)), power);
}

float4 PositivePow(float4 base, float4 power)
{
    return pow(max(abs(base), float4(FLT_EPSILON, FLT_EPSILON, FLT_EPSILON, FLT_EPSILON)), power);
}

float GetScaleFromBase(float base)
{
    const float C = (float)(1 << g_iLog2NumClusters);
    const float geomSeries = (1.0 - PositivePow(base, C)) / (1 - base);     // geometric series: sum_k=0^{C-1} base^k
    return geomSeries / (g_fFarPlane - g_fNearPlane);
}

int SnapToClusterIdxFlex(float z_in, float suggestedBase, bool logBasePerTile)
{
#if USE_LEFTHAND_CAMERASPACE
    float z = z_in;
#else
    float z = -z_in;
#endif

    float userscale = g_fClustScale;
    if (logBasePerTile)
        userscale = GetScaleFromBase(suggestedBase);

    // using the inverse of the geometric series
    const float dist = max(0, z - g_fNearPlane);
    return (int)clamp(log2(dist * userscale * (suggestedBase - 1.0f) + 1) / log2(suggestedBase), 0.0, (float)((1 << g_iLog2NumClusters) - 1));
}

int SnapToClusterIdx(float z_in, float suggestedBase)
{
#ifdef ENABLE_DEPTH_TEXTURE_BACKPLANE
    bool logBasePerTile = true;     // resolved compile time
#else
    bool logBasePerTile = false;
#endif

    return SnapToClusterIdxFlex(z_in, suggestedBase, logBasePerTile);
}

float ClusterIdxToZFlex(int k, float suggestedBase, bool logBasePerTile)
{
    float res;

    float userscale = g_fClustScale;
    if (logBasePerTile)
        userscale = GetScaleFromBase(suggestedBase);

    float dist = (PositivePow(suggestedBase, (float)k) - 1.0) / (userscale * (suggestedBase - 1.0f));
    res = dist + g_fNearPlane;

#if USE_LEFTHAND_CAMERASPACE
    return res;
#else
    return -res;
#endif
}

float ClusterIdxToZ(int k, float suggestedBase)
{
#ifdef ENABLE_DEPTH_TEXTURE_BACKPLANE
    bool logBasePerTile = true;     // resolved compile time
#else
    bool logBasePerTile = false;
#endif

    return ClusterIdxToZFlex(k, suggestedBase, logBasePerTile);
}

// generate a log-base value such that half of the clusters are consumed from near plane to max. opaque depth of tile.
float SuggestLogBase50(float tileFarPlane)
{
    const float C = (float)(1 << g_iLog2NumClusters);
    float normDist = clamp((tileFarPlane - g_fNearPlane) / (g_fFarPlane - g_fNearPlane), FLT_EPSILON, 1.0);
    float suggested_base = pow((1.0 + sqrt(max(0.0, 1.0 - 4.0 * normDist * (1.0 - normDist)))) / (2.0 * normDist), 2.0 / C);      //
    return max(g_fClustBase, suggested_base);
}

// generate a log-base value such that (approximately) a quarter of the clusters are consumed from near plane to max. opaque depth of tile.
float SuggestLogBase25(float tileFarPlane)
{
    const float C = (float)(1 << g_iLog2NumClusters);
    float normDist = clamp((tileFarPlane - g_fNearPlane) / (g_fFarPlane - g_fNearPlane), FLT_EPSILON, 1.0);
    float suggested_base = pow((1 / 2.3) * max(0.0, (0.8 / normDist) - 1), 4.0 / (C * 2));     // approximate inverse of d*x^4 + (-x) + (1-d) = 0       - d is normalized distance
    return max(g_fClustBase, suggested_base);
}

#endif
