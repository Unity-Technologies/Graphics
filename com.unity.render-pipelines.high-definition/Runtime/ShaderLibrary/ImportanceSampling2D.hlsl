#ifndef UNITY_IMPORTANCE_SAMPLING_2D
#define UNITY_IMPORTANCE_SAMPLING_2D

//void ImportanceSamplingLatLong(out float2 uv, out float3 w, float2 xi, TEXTURE2D_PARAM(marginalRow, s_linear_clamp_sampler), TEXTURE2D_PARAM(conditionalMarginal, s_linear_clamp_sampler))
float ImportanceSamplingLatLong(out float2 uv, out float3 w, float2 xi, TEXTURE2D_PARAM(marginalRow, used_samplerMarg), TEXTURE2D_PARAM(conditionalMarginal, used_samplerCondMarg))
{
    uv.y = saturate(SAMPLE_TEXTURE2D_LOD(marginalRow,           used_samplerMarg,     float2(0.0f, xi.x), 0).x);
    uv.x = saturate(SAMPLE_TEXTURE2D_LOD(conditionalMarginal,   used_samplerCondMarg, float2(xi.y, uv.y), 0).x);

    w = normalize(LatlongToDirectionCoordinate(uv));

    // The pdf (without jacobian) stored on the y channel
    return SAMPLE_TEXTURE2D_LOD(conditionalMarginal, used_samplerCondMarg, uv, 0).y;
}

#endif // UNITY_IMPORTANCE_SAMPLING_2D
