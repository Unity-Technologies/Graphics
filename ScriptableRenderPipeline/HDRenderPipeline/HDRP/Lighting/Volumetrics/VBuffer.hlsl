#ifndef UNITY_VBUFFER_INCLUDED
#define UNITY_VBUFFER_INCLUDED

// Interpolation in the log space is non-linear.
// Therefore, given 'logEncodedDepth', we compute a new depth value
// which allows us to perform HW interpolation which is linear in the view space.
float ComputeLerpPositionForLogEncoding(float  linearDepth,
                                        float  logEncodedDepth,
                                        float2 VBufferSliceCount,
                                        float4 VBufferDepthDecodingParams)
{
    float z = linearDepth;
    float d = logEncodedDepth;

    float numSlices    = VBufferSliceCount.x;
    float rcpNumSlices = VBufferSliceCount.y;

    float s  = d * numSlices - 0.5;
    float s0 = floor(s);
    float s1 = ceil(s);
    float d0 = saturate(s0 * rcpNumSlices + (0.5 * rcpNumSlices));
    float d1 = saturate(s1 * rcpNumSlices + (0.5 * rcpNumSlices));
    float z0 = DecodeLogarithmicDepthGeneralized(d0, VBufferDepthDecodingParams);
    float z1 = DecodeLogarithmicDepthGeneralized(d1, VBufferDepthDecodingParams);

    // Compute the linear interpolation weight.
    float t = saturate((z - z0) / (z1 - z0));

    // Do not saturate here, we want to know whether we are outside of the near/far plane bounds.
    return d0 + t * rcpNumSlices;
}

// if (correctLinearInterpolation), we use ComputeLerpPositionForLogEncoding() to correct weighting
// of both slices at the cost of extra ALUs.
//
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
                     float  linearDepth,
                     float2 viewportScale,
                     float4 VBufferResolution,
                     float2 VBufferSliceCount,
                     float4 VBufferDepthEncodingParams,
                     float4 VBufferDepthDecodingParams,
                     bool   correctLinearInterpolation,
                     bool   quadraticFilterXY,
                     bool   clampToBorder)
{
    float2 uv = positionNDC;
    float  w;

    // The distance between slices is log-encoded.
    float z = linearDepth;
    float d = EncodeLogarithmicDepthGeneralized(z, VBufferDepthEncodingParams);

    if (correctLinearInterpolation)
    {
        // Adjust the texture coordinate for HW linear filtering.
        w = ComputeLerpPositionForLogEncoding(z, d, VBufferSliceCount, VBufferDepthDecodingParams);
    }
    else
    {
        // Ignore non-linearity (for performance reasons) at the cost of accuracy.
        // The results are exact for a stationary camera, but can potentially cause some judder in motion.
        w = d;
    }

    // Always clamp UVs (clamp to edge) to avoid leaks due to suballocation and memory aliasing.
    // clamp to (vp_dim - 0.5) / tex_dim = vp_scale - (0.5 / tex_dim) = vp_scale - 0.5 * vp_scale / tex_dim.
    // Do not clamp along the W direction for now, it's not necessary (our slice count is fixed).
    // Warning: there will still be some leaks as long as the viewport, screen or texture size
    // are not multiples of the V-Buffer tile size (8 or 4 pixels). We ignore them for now since
    // it's not a problem in for a real game.
    // TODO: precompute this in a uniform...
    float2 maxUV = viewportScale * (1 - 0.5 * VBufferResolution.zw);

    float fadeWeight = 1;

    if (clampToBorder)
    {
        // Compute the distance to the edge, and remap it to the [0, 1] range.
        // Smoothly fade from the center of the edge texel to the black border color.
        float weightU = saturate((1 - 2 * abs(uv.x - 0.5)) * VBufferResolution.x);
        float weightV = saturate((1 - 2 * abs(uv.y - 0.5)) * VBufferResolution.y);
        float weightW = saturate((1 - 2 * abs(w    - 0.5)) * VBufferSliceCount.x);

        fadeWeight = weightU * weightV * weightW;
    }

    float4 result = 0;

    if (fadeWeight > 0)
    {
        if (quadraticFilterXY)
        {
            float2 xy = uv * VBufferResolution.xy;
            float2 ic = floor(xy);
            float2 fc = frac(xy);

            float2 weights[2], offsets[2];
            BiquadraticFilter(1 - fc, weights, offsets); // Inverse-translate the filter centered around 0.5

            // Apply the viewport scale right at the end.
            // TODO: precompute (VBufferResolution.zw * viewportScale).
            result = (weights[0].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[0].x, offsets[0].y)) * (VBufferResolution.zw * viewportScale), maxUV), w), 0)  // Top left
                   + (weights[1].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[1].x, offsets[0].y)) * (VBufferResolution.zw * viewportScale), maxUV), w), 0)  // Top right
                   + (weights[0].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[0].x, offsets[1].y)) * (VBufferResolution.zw * viewportScale), maxUV), w), 0)  // Bottom left
                   + (weights[1].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[1].x, offsets[1].y)) * (VBufferResolution.zw * viewportScale), maxUV), w), 0); // Bottom right
        }
        else
        {
            // Apply the viewport scale right at the end.
            result = SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min(uv * viewportScale, maxUV), w), 0);
        }

        result *= fadeWeight;
    }

    return result;
}

float4 SampleVBuffer(TEXTURE3D_ARGS(VBuffer, clampSampler),
                     float3   positionWS,
                     float4x4 viewProjMatrix,
                     float2   viewportScale,
                     float4   VBufferResolution,
                     float2   VBufferSliceCount,
                     float4   VBufferDepthEncodingParams,
                     float4   VBufferDepthDecodingParams,
                     bool     correctLinearInterpolation,
                     bool     quadraticFilterXY,
                     bool     clampToBorder)
{
    float2 positionNDC = ComputeNormalizedDeviceCoordinates(positionWS, viewProjMatrix);
    float  linearDepth = mul(viewProjMatrix, float4(positionWS, 1)).w;

    return SampleVBuffer(TEXTURE3D_PARAM(VBuffer, clampSampler),
                         positionNDC,
                         linearDepth,
                         viewportScale,
                         VBufferResolution,
                         VBufferSliceCount,
                         VBufferDepthEncodingParams,
                         VBufferDepthDecodingParams,
                         correctLinearInterpolation,
                         quadraticFilterXY,
                         clampToBorder);
}

// Returns interpolated {volumetric radiance, transmittance}.
float4 SampleVolumetricLighting(TEXTURE3D_ARGS(VBufferLighting, clampSampler),
                                float2 positionNDC,
                                float  linearDepth,
                                float4 VBufferResolution,
                                float2 VBufferSliceCount,
                                float4 VBufferDepthEncodingParams,
                                float4 VBufferDepthDecodingParams,
                                bool   correctLinearInterpolation,
                                bool   quadraticFilterXY)
{
    // TODO: add some slowly animated noise to the reconstructed value.
    return FastTonemapInvert(SampleVBuffer(TEXTURE3D_PARAM(VBufferLighting, clampSampler),
                                           positionNDC,
                                           linearDepth,
                                           GetViewportScaleCurrentFrame(),
                                           VBufferResolution,
                                           VBufferSliceCount,
                                           VBufferDepthEncodingParams,
                                           VBufferDepthDecodingParams,
                                           correctLinearInterpolation,
                                           quadraticFilterXY,
                                           false));
}

#endif // UNITY_VBUFFER_INCLUDED
