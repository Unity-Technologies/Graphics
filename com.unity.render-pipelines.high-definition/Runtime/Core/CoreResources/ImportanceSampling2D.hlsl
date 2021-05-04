#ifndef UNITY_IMPORTANCE_SAMPLING_2D
#define UNITY_IMPORTANCE_SAMPLING_2D

float2 FixPDFInfo(float2 info)
{
    float pdf = info.x;
    float cdf = info.y;

    //if (pdf < 1e-6f)
    //    pdf = 0.0f;

    return float2(pdf, cdf);
}

#ifdef HORIZONTAL
void GetImportanceSampledUV(out float2 uv, float2 xi, float3 pixHeightsInfos, TEXTURE2D_PARAM(marginalRow, used_samplerMarg), TEXTURE2D_PARAM(conditionalMarginal, used_samplerCondMarg))
{
    uv.y = saturate(SAMPLE_TEXTURE2D_LOD(marginalRow,           used_samplerMarg,     float2(0.0f, xi.x), 0).x);
    // pixInfos: x: Height, y: 1.0f/Height, z: 0.5f/Height
    uv.y = floor(uv.y*pixHeightsInfos.x)*pixHeightsInfos.y + pixHeightsInfos.z;
    uv.x = saturate(SAMPLE_TEXTURE2D_LOD(conditionalMarginal,   used_samplerCondMarg, float2(xi.y, uv.y), 0).x);
}

void GetImportanceSampledUVArray(out float2 uv, float2 xi, float3 pixHeightsInfos, TEXTURE2D_ARRAY_PARAM(marginalRow, used_samplerMarg), TEXTURE2D_ARRAY_PARAM(conditionalMarginal, used_samplerCondMarg))
{
    uv.y = saturate(SAMPLE_TEXTURE2D_ARRAY_LOD(marginalRow,           used_samplerMarg,     float2(0.0f, xi.x), 0, 0).x);
    // pixInfos: x: Height, y: 1.0f/Height, z: 0.5f/Height
    uv.y = floor(uv.y*pixHeightsInfos.x)*pixHeightsInfos.y + pixHeightsInfos.z;
    uv.x = saturate(SAMPLE_TEXTURE2D_ARRAY_LOD(conditionalMarginal,   used_samplerCondMarg, float2(xi.y, uv.y), 0, 0).x);
}
#elif defined(VERTICAL)
void GetImportanceSampledUV(out float2 uv, float2 xi, float3 pixHeightsInfos, TEXTURE2D_PARAM(marginalRow, used_samplerMarg), TEXTURE2D_PARAM(conditionalMarginal, used_samplerCondMarg))
{
    uv.x = saturate(SAMPLE_TEXTURE2D_LOD(marginalRow,           used_samplerMarg,     float2(xi.x, 0.0f), 0).x);
    // pixInfos: x: Width, y: 1.0f/Width, z: 0.5f/Width
    uv.x = floor(uv.x*pixHeightsInfos.x)*pixHeightsInfos.y + pixHeightsInfos.z;
    uv.y = saturate(SAMPLE_TEXTURE2D_LOD(conditionalMarginal,   used_samplerCondMarg, float2(uv.x, xi.y), 0).x);
}

void GetImportanceSampledUVArray(out float2 uv, float2 xi, float3 pixHeightsInfos, TEXTURE2D_ARRAY_PARAM(marginalRow, used_samplerMarg), TEXTURE2D_ARRAY_PARAM(conditionalMarginal, used_samplerCondMarg))
{
    uv.x = saturate(SAMPLE_TEXTURE2D_ARRAY_LOD(marginalRow,           used_samplerMarg,     float2(xi.x, 0.0f), 0, 0).x);
    // pixInfos: x: Width, y: 1.0f/Width, z: 0.5f/Width
    uv.x = floor(uv.x*pixHeightsInfos.x)*pixHeightsInfos.y + pixHeightsInfos.z;
    uv.y = saturate(SAMPLE_TEXTURE2D_ARRAY_LOD(conditionalMarginal,   used_samplerCondMarg, float2(uv.x, xi.y), 0, 0).x);
}
#else
#error ImportanceSampling2D.hlsl must define if HORIZONTAL or VERTICAL
#endif

float2 ImportanceSamplingHemiLatLong(out float2 uv, out float3 w, float2 xi, float3 pixHeightsInfos, TEXTURE2D_PARAM(marginalRow, used_samplerMarg), TEXTURE2D_PARAM(conditionalMarginal, used_samplerCondMarg))
{
    GetImportanceSampledUV(uv, xi, pixHeightsInfos, marginalRow, used_samplerMarg, conditionalMarginal, used_samplerCondMarg);
    // The pdf (without jacobian) stored on the y channel
    float2 info = SAMPLE_TEXTURE2D_LOD(conditionalMarginal, used_samplerCondMarg, uv, 0).yz;

    w = normalize(LatlongToDirectionCoordinate(saturate(uv*float2(1.0f, 0.5f))));

    return FixPDFInfo(info);
}

float2 ImportanceSamplingHemiLatLongArray(out float2 uv, out float3 w, float2 xi, float3 pixHeightsInfos, TEXTURE2D_ARRAY_PARAM(marginalRow, used_samplerMarg), TEXTURE2D_ARRAY_PARAM(conditionalMarginal, used_samplerCondMarg))
{
    GetImportanceSampledUVArray(uv, xi, pixHeightsInfos, marginalRow, used_samplerMarg, conditionalMarginal, used_samplerCondMarg);
    // The pdf (without jacobian) stored on the y channel
    float2 info = SAMPLE_TEXTURE2D_ARRAY_LOD(conditionalMarginal, used_samplerCondMarg, uv, 0, 0).yz;

    w = normalize(LatlongToDirectionCoordinate(saturate(uv*float2(1.0f, 0.5f))));

    return FixPDFInfo(info);
}

float2 ImportanceSamplingLatLong(out float2 uv, out float3 w, float2 xi, float3 pixHeightsInfos, TEXTURE2D_PARAM(marginalRow, used_samplerMarg), TEXTURE2D_PARAM(conditionalMarginal, used_samplerCondMarg))
{
    GetImportanceSampledUV(uv, xi, pixHeightsInfos, marginalRow, used_samplerMarg, conditionalMarginal, used_samplerCondMarg);
    // The pdf (without jacobian) stored on the y channel
    float2 info = SAMPLE_TEXTURE2D_LOD(conditionalMarginal, used_samplerCondMarg, uv, 0).yz;

    w = normalize(LatlongToDirectionCoordinate(saturate(uv)));

    return FixPDFInfo(info);
}

float2 ImportanceSamplingLatLongArray(out float2 uv, out float3 w, float2 xi, float3 pixHeightsInfos, TEXTURE2D_ARRAY_PARAM(marginalRow, used_samplerMarg), TEXTURE2D_ARRAY_PARAM(conditionalMarginal, used_samplerCondMarg))
{
    GetImportanceSampledUVArray(uv, xi, pixHeightsInfos, marginalRow, used_samplerMarg, conditionalMarginal, used_samplerCondMarg);
    // The pdf (without jacobian) stored on the y channel
    float2 info = SAMPLE_TEXTURE2D_ARRAY_LOD(conditionalMarginal, used_samplerCondMarg, uv, 0, 0).yz;

    w = normalize(LatlongToDirectionCoordinate(saturate(uv)));

    return FixPDFInfo(info);
}

#endif // UNITY_IMPORTANCE_SAMPLING_2D
