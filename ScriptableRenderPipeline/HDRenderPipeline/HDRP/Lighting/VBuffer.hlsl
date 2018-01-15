#ifndef UNITY_VBUFFER_INCLUDED
#define UNITY_VBUFFER_INCLUDED

// Interpolation in the log space is non-linear.
// Therefore, given 'logEncodedDepth', we compute a new depth value
// which allows us to perform HW interpolation which is linear in the view space.
float ComputeLerpPositionForLogEncoding(float linearDepth, float logEncodedDepth,
                                        float4 VBufferScaleAndSliceCount,
                                        float4 VBufferDepthEncodingParams)
{
    float z = linearDepth;
    float d = logEncodedDepth;

    float numSlices    = VBufferScaleAndSliceCount.z;
    float rcpNumSlices = VBufferScaleAndSliceCount.w;

    float s0 = floor(d * numSlices - 0.5);
    float s1 = ceil(d * numSlices - 0.5);
    float d0 = saturate(s0 * rcpNumSlices + (0.5 * rcpNumSlices));
    float d1 = saturate(s1 * rcpNumSlices + (0.5 * rcpNumSlices));
    float z0 = DecodeLogarithmicDepth(d0, VBufferDepthEncodingParams);
    float z1 = DecodeLogarithmicDepth(d1, VBufferDepthEncodingParams);

    // Compute the linear interpolation weight.
    float t = saturate((z - z0) / (z1 - z0));
    return d0 + t * rcpNumSlices;
}

// Performs trilinear reconstruction of the V-Buffer.
// If (clampToEdge == false), out-of-bounds loads return 0.
float4 SampleVBuffer(TEXTURE3D_ARGS(VBufferLighting, trilinearSampler), bool clampToEdge,
                     float2 positionNDC, float linearDepth,
                     float4 VBufferScaleAndSliceCount,
                     float4 VBufferDepthEncodingParams)
{
    float numSlices    = VBufferScaleAndSliceCount.z;
    float rcpNumSlices = VBufferScaleAndSliceCount.w;

    // Account for the visible area of the V-Buffer.
    float2 uv = positionNDC * VBufferScaleAndSliceCount.xy;

    // The distance between slices is log-encoded.
    float z = linearDepth;
    float d = EncodeLogarithmicDepth(z, VBufferDepthEncodingParams);

    // Unity doesn't support samplers clamping to border, so we have to do it ourselves.
    // TODO: add the proper sampler support.
    bool isInBounds = Min3(uv.x, uv.y, d) > 0 && Max3(uv.x, uv.y, d) < 1;

    [branch] if (clampToEdge || isInBounds)
    {
    #if 0
        // We could ignore non-linearity at the cost of accuracy.
        // TODO: visually test this option (especially in motion).
        float w = d;
    #else
        // Adjust the texture coordinate for HW trilinear sampling.
        float w = ComputeLerpPositionForLogEncoding(z, d, VBufferScaleAndSliceCount, VBufferDepthEncodingParams);
    #endif

        return SAMPLE_TEXTURE3D_LOD(VBufferLighting, trilinearSampler, float3(uv, w), 0);
    }
    else
    {
        return 0;
    }
}

// Returns interpolated {volumetric radiance, transmittance}. The sampler clamps to edge.
float4 SampleInScatteredRadianceAndTransmittance(TEXTURE3D_ARGS(VBufferLighting, trilinearSampler),
                                                 float2 positionNDC, float linearDepth,
                                                 float4 VBufferResolution,
                                                 float4 VBufferScaleAndSliceCount,
                                                 float4 VBufferDepthEncodingParams)
{
#ifdef RECONSTRUCTION_FILTER_TRILINEAR
    float4 L = SampleVBuffer(TEXTURE3D_PARAM(VBufferLighting, trilinearSampler), true,
                             positionNDC, linearDepth,
                             VBufferScaleAndSliceCount, VBufferDepthEncodingParams);
#else
    // Perform biquadratic reconstruction in XY, linear in Z, using 4x trilinear taps.

    // Account for the visible area of the V-Buffer.
    float2 xy = positionNDC * (VBufferResolution.xy * VBufferScaleAndSliceCount.xy);
    float2 ic = floor(xy);
    float2 fc = frac(xy);

    // The distance between slices is log-encoded.
    float z = linearDepth;
    float d = EncodeLogarithmicDepth(z, VBufferDepthEncodingParams);

    // Adjust the texture coordinate for HW trilinear sampling.
    float w = ComputeLerpPositionForLogEncoding(z, d, VBufferScaleAndSliceCount, VBufferDepthEncodingParams);

    float2 weights[2], offsets[2];
    BiquadraticFilter(1 - fc, weights, offsets); // Inverse-translate the filter centered around 0.5

    float2 rcpRes = VBufferResolution.zw;

    // TODO: reconstruction should be performed in the perceptual space (e.i., after tone mapping).
    // But our VBuffer is linear. How to achieve that?
    // See "A Fresh Look at Generalized Sampling", p. 51.
    float4 L = (weights[0].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBufferLighting, trilinearSampler, float3((ic + float2(offsets[0].x, offsets[0].y)) * rcpRes, w), 0)  // Top left
             + (weights[1].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBufferLighting, trilinearSampler, float3((ic + float2(offsets[1].x, offsets[0].y)) * rcpRes, w), 0)  // Top right
             + (weights[0].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBufferLighting, trilinearSampler, float3((ic + float2(offsets[0].x, offsets[1].y)) * rcpRes, w), 0)  // Bottom left
             + (weights[1].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBufferLighting, trilinearSampler, float3((ic + float2(offsets[1].x, offsets[1].y)) * rcpRes, w), 0); // Bottom right
#endif

    // TODO: add some animated noise to the reconstructed radiance.
    return float4(L.rgb, Transmittance(L.a));
}

#endif // UNITY_VBUFFER_INCLUDED
