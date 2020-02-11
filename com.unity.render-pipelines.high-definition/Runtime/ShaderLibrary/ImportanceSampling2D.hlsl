#ifndef UNITY_IMPORTANCE_SAMPLING_2D
#define UNITY_IMPORTANCE_SAMPLING_2D

//void ImportanceSamplingLatLong(out float2 uv, out float3 w, float2 xi, TEXTURE2D_PARAM(marginalRow, s_linear_clamp_sampler), TEXTURE2D_PARAM(conditionalMarginal, s_linear_clamp_sampler))
float2 ImportanceSamplingLatLong(out float2 uv, out float3 w, float2 xi, TEXTURE2D_ARRAY_PARAM(marginalRow, used_samplerMarg), TEXTURE2D_ARRAY_PARAM(conditionalMarginal, used_samplerCondMarg))
{
    /*

    float sliceInvCDFValue = SAMPLE_TEXTURE2D_LOD(_SliceInvCDF, sampler_LinearClamp, float2(0, u.x), 0).x;
    float invCDFValue      = SAMPLE_TEXTURE2D_LOD(     _InvCDF, sampler_LinearClamp, float2(u.y, sliceInvCDFValue), 0).x;

    float2 smpl = float2(saturate(invCDFValue), saturate(sliceInvCDFValue));

    _Output[id.xy] = float4(smpl.xy, 0, 1);

    */
    // textureName, samplerName, coord2, index, lod
    uv.y = saturate(SAMPLE_TEXTURE2D_ARRAY_LOD(marginalRow,           used_samplerMarg,     float2(0.0f, xi.x), 0, 0).x);
    uv.x = saturate(SAMPLE_TEXTURE2D_ARRAY_LOD(conditionalMarginal,   used_samplerCondMarg, float2(xi.y, uv.y), 0, 0).x);

    //w = LatlongToDirectionCoordinate(saturate(uv));
    w = normalize(LatlongToDirectionCoordinate(saturate(uv)));

    // The pdf (without jacobian) stored on the y channel
    //return SAMPLE_TEXTURE2D_LOD(conditionalMarginal, used_samplerCondMarg, uv, 0).y;
    float2 info = SAMPLE_TEXTURE2D_ARRAY_LOD(conditionalMarginal, used_samplerCondMarg, uv, 0, 0).yz;

    //return info.x/max(info.y, 1e-4);
    //if (info.y > 0.0f)
    //    return info.x/max(info.y, 1e-4);
    //else
        return info;

    //float2 info =
    //            SAMPLE_TEXTURE2D_ARRAY_LOD(conditionalMarginal, used_samplerCondMarg, uv, 0, 0).y
    //    / //  -------------------------------------------------------------------------------------
    //            max(SAMPLE_TEXTURE2D_ARRAY_LOD(conditionalMarginal, used_samplerCondMarg, uv, 0, 0).z, 1e-4);

    //return SAMPLE_TEXTURE2D_ARRAY_LOD(conditionalMarginal, used_samplerCondMarg, uv, 0, 0).y;
}

#endif // UNITY_IMPORTANCE_SAMPLING_2D
