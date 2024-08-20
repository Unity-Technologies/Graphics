#ifndef VOLUMETRIC_MATERIAL_UTILS
#define VOLUMETRIC_MATERIAL_UTILS

float VBufferDistanceToSliceIndex(uint sliceIndex)
{
    float de = _VBufferRcpSliceCount; // Log-encoded distance between slices

    float e1 = ((float)sliceIndex + 0.5) * de + de;
    return DecodeLogarithmicDepthGeneralized(e1, _VBufferDistanceDecodingParams);
}

// Linear view space (eye) depth to Z buffer depth.
// Does NOT correctly handle oblique view frustums.
// Does NOT work with orthographic projection.
// zBufferParam (UNITY_REVERSED_Z) = { f/n - 1,   1, (1/n - 1/f), 1/f }
// zBufferParam                    = { 1 - f/n, f/n, (1/f - 1/n), 1/n }
float EyeDepthToLinear(float linearDepth, float4 zBufferParam)
{
    linearDepth = rcp(linearDepth);
    linearDepth -= zBufferParam.w;

    return linearDepth / zBufferParam.z;
}

#endif // VOLUMETRIC_MATERIAL_UTILS
