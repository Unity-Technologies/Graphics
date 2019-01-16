#ifndef UNITY_VBUFFER_INCLUDED
#define UNITY_VBUFFER_INCLUDED

// if (quadraticFilterXY), we perform biquadratic (3x3) reconstruction for each slice to reduce
// aliasing at the cost of extra ALUs and bandwidth.
// Warning: you MUST pass a linear sampler in order for the quadratic filter to work.
//
// Note: for correct filtering, the data has to be stored in the perceptual space.
// This means storing tone mapped radiance and transmittance instead of optical depth.
// See "A Fresh Look at Generalized Sampling", p. 51.
//
// if (clampToBorder), samples outside of the buffer return 0 (we perform a smooth fade).
// Otherwise, the sampler simply clamps the texture coordinate to the edge of the texture.
// Warning: clamping to border may not work as expected with the quadratic filter due to its extent.
float4 SampleVBuffer(TEXTURE3D_ARGS(VBuffer, clampSampler),
                     float2 positionNDC,
                     float  linearDistance,
                     float4 VBufferResolution,
                     float2 VBufferUvScale,
                     float2 VBufferUvLimit,
                     float4 VBufferDistanceEncodingParams,
                     float4 VBufferDistanceDecodingParams,
                     bool   quadraticFilterXY,
                     bool   clampToBorder,
                     bool   cubicFilterXY)
{
    // These are the viewport coordinates.
    float2 uv = positionNDC;
    float  w  = EncodeLogarithmicDepthGeneralized(linearDistance, VBufferDistanceEncodingParams);

    bool coordIsInsideFrustum = true;

    if (clampToBorder)
    {
        // Coordinates are always clamped to edge. We just introduce a clipping operation.
        float3 positionCS = float3(uv, w) * 2 - 1;

        coordIsInsideFrustum = Max3(abs(positionCS.x), abs(positionCS.y), abs(positionCS.z)) < 1;
    }

    float4 result = 0;

    if (coordIsInsideFrustum)
    {
        if (quadraticFilterXY)
        {
            float2 xy = uv * VBufferResolution.xy;
            float2 ic = floor(xy);
            float2 fc = frac(xy);

            float2 weights[2], offsets[2];
            BiquadraticFilter(1 - fc, weights, offsets); // Inverse-translate the filter centered around 0.5

            const float2 ssToUv = VBufferResolution.zw * VBufferUvScale;

            // The sampler clamps to edge. This takes care of 4 frustum faces out of 6.
            // Due to the RTHandle scaling system, we must take care of the other 2 manually.
            // TODO: perform per-sample (4, in this case) bilateral filtering, rather than per-pixel. This should reduce leaking.
            result = (weights[0].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[0].x, offsets[0].y)) * ssToUv, VBufferUvLimit), w), 0)  // Top left
                   + (weights[1].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[1].x, offsets[0].y)) * ssToUv, VBufferUvLimit), w), 0)  // Top right
                   + (weights[0].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[0].x, offsets[1].y)) * ssToUv, VBufferUvLimit), w), 0)  // Bottom left
                   + (weights[1].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[1].x, offsets[1].y)) * ssToUv, VBufferUvLimit), w), 0); // Bottom right
        }
        else if (cubicFilterXY)
        {
            float2 positionPixels = uv * VBufferResolution.xy;

            float2 weights[3], uvs[3];
            const float BICUBIC_SHARPNESS_BSPLINE = 0.0f;
            const float BICUBIC_SHARPNESS_MITCHELL = 1.0f / 3.0f;
            const float BICUBIC_SHARPNESS_CATMULL_ROM = 0.5f;
            BicubicFilter(positionPixels, weights, uvs, BICUBIC_SHARPNESS_MITCHELL); // Inverse-translate the filter centered around 0.5

            // Apply the viewport scale right at the end.
            const float2 ssToUv = VBufferResolution.zw * VBufferUvScale;

            // Rather than taking the full 9 hardware-filtered taps to resolve our bicubic filter, we drop the (lowest weight) corner samples, computing our bicubic filter with only 5-taps.
            // Visually, error is low enough to be visually indistinguishable in our test cases.
            // Source:
            // Filmic SMAA: Sharp Morphological and Temporal Antialiasing
            // http://advances.realtimerendering.com/s2016/index.html
            result =
                     (weights[1].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min(float2(uvs[1].x, uvs[0].y) * ssToUv, VBufferUvLimit), w), 0)
                   + (weights[0].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min(float2(uvs[0].x, uvs[1].y) * ssToUv, VBufferUvLimit), w), 0)
                   + (weights[1].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min(float2(uvs[1].x, uvs[1].y) * ssToUv, VBufferUvLimit), w), 0)
                   + (weights[2].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min(float2(uvs[2].x, uvs[1].y) * ssToUv, VBufferUvLimit), w), 0)
                   + (weights[1].x * weights[2].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min(float2(uvs[1].x, uvs[2].y) * ssToUv, VBufferUvLimit), w), 0);
        }
        else
        {
            // The sampler clamps to edge. This takes care of 4 frustum faces out of 6.
            // Due to the RTHandle scaling system, we must take care of the other 2 manually.
            result = SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min(uv * VBufferUvScale, VBufferUvLimit), w), 0);
        }
    }

    return result;
}

float4 SampleVBuffer(TEXTURE3D_ARGS(VBuffer, clampSampler),
                     float3   positionWS,
                     float3   cameraPositionWS,
                     float4x4 viewProjMatrix,
                     float4   VBufferResolution,
                     float2   VBufferUvScale,
                     float2   VBufferUvLimit,
                     float4   VBufferDistanceEncodingParams,
                     float4   VBufferDistanceDecodingParams,
                     bool     quadraticFilterXY,
                     bool     clampToBorder,
                     bool     cubicFilterXY)
{
    float2 positionNDC = ComputeNormalizedDeviceCoordinates(positionWS, viewProjMatrix);
    float  linearDistance = distance(positionWS, cameraPositionWS);

    return SampleVBuffer(TEXTURE3D_PARAM(VBuffer, clampSampler),
                         positionNDC,
                         linearDistance,
                         VBufferResolution,
                         VBufferUvScale,
                         VBufferUvLimit,
                         VBufferDistanceEncodingParams,
                         VBufferDistanceDecodingParams,
                         quadraticFilterXY,
                         clampToBorder,
                         cubicFilterXY);
}

#endif // UNITY_VBUFFER_INCLUDED
