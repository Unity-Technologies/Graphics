#ifndef VOLUMETRIC_MATERIAL_UTILS
#define VOLUMETRIC_MATERIAL_UTILS

float VBufferDistanceToSliceIndex(uint sliceIndex)
{
    float de = _VBufferRcpSliceCount; // Log-encoded distance between slices

    float e1 = ((float)sliceIndex + 0.5) * de + de;
    return DecodeLogarithmicDepthGeneralized(e1, _VBufferDistanceDecodingParams);
}

float EyeDepthToLinear(float linearDepth, float4 zBufferParam)
{
    linearDepth = rcp(linearDepth);
    linearDepth -= zBufferParam.w;

    return linearDepth / zBufferParam.z;
}

#endif // VOLUMETRIC_MATERIAL_UTILS
