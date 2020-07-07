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
// if (clampToBorder), samples outside of the buffer return 0 (border color).
// Otherwise, the sampler simply clamps the texture coordinate to the edge of the texture.
// Warning: clamping to border may not work as expected with the quadratic filter due to its extent.
//
// if (biasLookup), we apply a constant bias to the look-up to avoid light leaks through geometry.
float4 SampleVBuffer(TEXTURE3D_PARAM(VBuffer, clampSampler),
                     float2 positionNDC,
                     float  linearDistance,
                     float4 VBufferViewportSize,
                     float3 VBufferViewportScale,
                     float3 VBufferViewportLimit,
                     float4 VBufferDistanceEncodingParams,
                     float4 VBufferDistanceDecodingParams,
                     bool   biasLookup,
                     bool   quadraticFilterXY,
                     bool   clampToBorder)
{
    // These are the viewport coordinates.
    float2 uv = positionNDC;
    float  w  = EncodeLogarithmicDepthGeneralized(linearDistance, VBufferDistanceEncodingParams);

    if (biasLookup)
    {
        // The value is higher than 0.5 (we use half of the length the diagonal of a unit cube).
        // to account for varying angles of incidence.
        // TODO: XR?
        w -= (sqrt(3)/2) * _VBufferRcpSliceCount;
    }

    bool coordIsInsideFrustum;

    if (clampToBorder)
    {
        // Coordinates are always clamped to the edge. We just introduce a clipping operation.
        float3 positionCS = float3(uv, w) * 2 - 1;

        coordIsInsideFrustum = Max3(abs(positionCS.x), abs(positionCS.y), abs(positionCS.z)) < 1;
    }
    else
    {
        coordIsInsideFrustum = true; // No clipping, only clamping
    }

    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        // With XR single-pass, one 3D buffer is used to store all views (split along w)
        w = (w + unity_StereoEyeIndex) * _VBufferRcpInstancedViewCount;

        // Manually clamp w with a safe limit to avoid linear interpolation from the others views
        float limitSliceRange = 0.5f * _VBufferRcpSliceCount;
        float lowerSliceRange = (unity_StereoEyeIndex + 0) * _VBufferRcpInstancedViewCount;
        float upperSliceRange = (unity_StereoEyeIndex + 1) * _VBufferRcpInstancedViewCount;

        w = clamp(w, lowerSliceRange + limitSliceRange, upperSliceRange - limitSliceRange);
    #endif

    float4 result = 0;

    if (coordIsInsideFrustum)
    {
        if (quadraticFilterXY)
        {
            float2 xy = uv * VBufferViewportSize.xy;
            float2 ic = floor(xy);
            float2 fc = frac(xy);

            float2 weights[2], offsets[2];
            BiquadraticFilter(1 - fc, weights, offsets); // Inverse-translate the filter centered around 0.5

            // Don't want to pass another shader parameter...
            const float2 rcpBufDim = VBufferViewportScale.xy * VBufferViewportSize.zw; // (vp_dim / buf_dim) * (1 / vp_dim)

            // And these are the texture coordinates.
            // TODO: will the compiler eliminate redundant computations?
            float2 texUv0 = (ic + float2(offsets[0].x, offsets[0].y)) * rcpBufDim; // Top left
            float2 texUv1 = (ic + float2(offsets[1].x, offsets[0].y)) * rcpBufDim; // Top right
            float2 texUv2 = (ic + float2(offsets[0].x, offsets[1].y)) * rcpBufDim; // Bottom left
            float2 texUv3 = (ic + float2(offsets[1].x, offsets[1].y)) * rcpBufDim; // Bottom right
            float  texW   = w * VBufferViewportScale.z;

            // The sampler clamps to the edge (so UVWs < 0 are OK).
            // TODO: perform per-sample (4, in this case) bilateral filtering, rather than per-pixel. This should reduce leaking.
            // Currently we don't do it, since it is expensive and doesn't appear to be helpful/necessary in practice.
            result = (weights[0].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, min(float3(texUv0, texW), VBufferViewportLimit), 0)
                   + (weights[1].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, min(float3(texUv1, texW), VBufferViewportLimit), 0)
                   + (weights[0].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, min(float3(texUv2, texW), VBufferViewportLimit), 0)
                   + (weights[1].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, min(float3(texUv3, texW), VBufferViewportLimit), 0);
        }
        else
        {
            // And these are the texture coordinates.
            float3 texUVW = float3(uv, w) * VBufferViewportScale;
            // The sampler clamps to the edge (so UVWs < 0 are OK).
            result = SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, min(texUVW, VBufferViewportLimit), 0);
        }
    }

    return result;
}

float4 SampleVBuffer(TEXTURE3D_PARAM(VBuffer, clampSampler),
                     float3   positionWS,
                     float3   cameraPositionWS,
                     float4x4 viewProjMatrix,
                     float4   VBufferViewportSize,
                     float3   VBufferViewportScale,
                     float3   VBufferViewportLimit,
                     float4   VBufferDistanceEncodingParams,
                     float4   VBufferDistanceDecodingParams,
                     bool     biasLookup,
                     bool     quadraticFilterXY,
                     bool     clampToBorder)
{
    float2 positionNDC    = ComputeNormalizedDeviceCoordinates(positionWS, viewProjMatrix);
    float  linearDistance = distance(positionWS, cameraPositionWS);

    return SampleVBuffer(TEXTURE3D_ARGS(VBuffer, clampSampler),
                         positionNDC,
                         linearDistance,
                         VBufferViewportSize,
                         VBufferViewportScale,
                         VBufferViewportLimit,
                         VBufferDistanceEncodingParams,
                         VBufferDistanceDecodingParams,
                         biasLookup,
                         quadraticFilterXY,
                         clampToBorder);
}

#endif // UNITY_VBUFFER_INCLUDED
