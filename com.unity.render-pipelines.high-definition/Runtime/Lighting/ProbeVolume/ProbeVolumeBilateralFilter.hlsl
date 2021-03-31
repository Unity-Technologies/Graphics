#ifndef __PROBEVOLUME_BILATERAL_FILTER_HLSL__
#define __PROBEVOLUME_BILATERAL_FILTER_HLSL__

uint3 ComputeProbeVolumeTexel3DMinOffsetFromIndex(uint i)
{
    // Compute the offset grid coord and clamp to the probe grid boundary
    // Offset = 0 or 1 along each axis
    // TODO: Evaluate if using a static LUT for these offset calculations would be better / worse.
    return uint3(i, i >> 1, i >> 2) & uint3(1, 1, 1);
}

// (i == 0) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(0, 0 >> 1, 0 >> 2) & int3(1, 1, 1)) => int3(0, 0, 0)
// (i == 1) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(1, 1 >> 1, 1 >> 2) & int3(1, 1, 1)) => int3(1, 0, 0)
// (i == 2) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(2, 2 >> 1, 2 >> 2) & int3(1, 1, 1)) => int3(0, 1, 0)
// (i == 3) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(3, 3 >> 1, 3 >> 2) & int3(1, 1, 1)) => int3(1, 1, 0)

// (i == 4) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(4, 4 >> 1, 4 >> 2) & int3(1, 1, 1)) => int3(0, 0, 1)
// (i == 5) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(5, 5 >> 1, 5 >> 2) & int3(1, 1, 1)) => int3(1, 0, 1)
// (i == 6) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(6, 6 >> 1, 6 >> 2) & int3(1, 1, 1)) => int3(0, 1, 1)
// (i == 7) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(7, 7 >> 1, 7 >> 2) & int3(1, 1, 1)) => int3(1, 1, 1)
#define PROBEVOLUMES_PROBE_INDEX_1D_BSW (0)
#define PROBEVOLUMES_PROBE_INDEX_1D_BSE (1)
#define PROBEVOLUMES_PROBE_INDEX_1D_BNW (2)
#define PROBEVOLUMES_PROBE_INDEX_1D_BNE (3)

#define PROBEVOLUMES_PROBE_INDEX_1D_TSW (4)
#define PROBEVOLUMES_PROBE_INDEX_1D_TSE (5)
#define PROBEVOLUMES_PROBE_INDEX_1D_TNW (6)
#define PROBEVOLUMES_PROBE_INDEX_1D_TNE (7)

#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING_MODE == PROBEVOLUMESBILATERALFILTERINGMODES_OCTAHEDRAL_DEPTH
float3 ProbeVolumeEvaluateOctahedralDepthOcclusionDebugColor(
    float3 probeVolumeTexel3D,
    float3 probeVolumeTexel3DMin,
    float3 probeVolumeResolution,
    float3x3 probeVolumeWorldFromTexel3DRotationScale,
    float3 probeVolumeWorldFromTexel3DTranslation,
    float4 probeVolumeOctahedralDepthScaleBias,
    float4 probeVolumeAtlasOctahedralDepthResolutionAndInverse,
    float3 samplePositionUnbiasedWS,
    float3 samplePositionBiasedWS,
    float3 sampleNormalWS)
{
    // Convert from 3D [0, probeVolumeResolution] space into 2D slice (probe) array local space.
    int2 probeVolumeTexel2DMinBack = int2(
        (int)(floor(probeVolumeTexel3DMin.z) * probeVolumeResolution.x + probeVolumeTexel3DMin.x),
        (int)probeVolumeTexel3DMin.y
    );
    int2 probeVolumeTexel2DMinFront = int2(probeVolumeTexel2DMinBack.x + (int)probeVolumeResolution.x, probeVolumeTexel2DMinBack.y);

    // Convert from 2D slice (probe) array local space into 2D slice (octahedral depth) array local space
    const int OCTAHEDRAL_DEPTH_RESOLUTION = 8;
    probeVolumeTexel2DMinBack *= OCTAHEDRAL_DEPTH_RESOLUTION;
    probeVolumeTexel2DMinFront *= OCTAHEDRAL_DEPTH_RESOLUTION;

    uint i = 1;
    {
        float3 probeVolumeTexel3DMinOffset = (float3)ComputeProbeVolumeTexel3DMinOffsetFromIndex(i);
        float3 probeVolumeTexel3DCurrent = clamp(probeVolumeTexel3DMin + probeVolumeTexel3DMinOffset, 0.5, probeVolumeResolution - 0.5);

        float3 probePositionWS = mul(probeVolumeWorldFromTexel3DRotationScale, probeVolumeTexel3DCurrent) + probeVolumeWorldFromTexel3DTranslation;

        float3 probeToSampleBiasedWS = samplePositionBiasedWS - probePositionWS;
        float probeToSampleBiasedDistanceWS = length(probeToSampleBiasedWS);
        float3 probeToSampleBiasedDirectionWS = normalize(probeToSampleBiasedWS);

        // Clamp all of the multiplies. We can't let the weight go to zero because then it would be
        // possible for *all* weights to be equally low and get normalized
        // up to 1/n. We want to distinguish between weights that are
        // low because of different factors.

        // Computed without the biasing applied to the "dir" variable.
        // This test can cause reflection-map looking errors in the image
        // (stuff looks shiny) if the transition is poor.
        float3 probeToSampleUnbiasedDirectionWS = normalize(samplePositionUnbiasedWS - probePositionWS);

        // The naive soft backface weight would ignore a probe when
        // it is behind the surface. That's good for walls. But for small details inside of a
        // room, the normals on the details might rule out all of the probes that have mutual
        // visibility to the point. So, we instead use a "wrap shading" test below inspired by
        // NPR work.
        //
        // The small offset at the end reduces the "going to zero" impact
        // where this is really close to exactly opposite
        // weights[i] = Sq(dot(-probeToSampleUnbiasedDirectionWS, sampleNormalWS) * 0.5 + 0.5) + 0.2;

        float2 probeOctahedralDepthUV = PackNormalOctQuadEncode(sampleNormalWS) * 0.5 + 0.5; // [0, 1] from [-1, 1]
        // probeOctahedralDepthUV.x = 1.0 - probeOctahedralDepthUV.x;
        // probeOctahedralDepthUV.y = 1.0 - probeOctahedralDepthUV.y;
        probeOctahedralDepthUV = clamp(probeOctahedralDepthUV, 0.5 / OCTAHEDRAL_DEPTH_RESOLUTION, 1.0 - 0.5 / OCTAHEDRAL_DEPTH_RESOLUTION);
        int2 probeVolumeTexel2DMin = (probeVolumeTexel3DMinOffset.z == 0.0) ? probeVolumeTexel2DMinBack : probeVolumeTexel2DMinFront;
        float2 probeOctahedralDepthTexel2D = (probeOctahedralDepthUV + probeVolumeTexel3DMinOffset.xy) * (float)OCTAHEDRAL_DEPTH_RESOLUTION + (float2)probeVolumeTexel2DMin;
        float2 probeOctahedralDepthAtlasUV = probeOctahedralDepthTexel2D * probeVolumeAtlasOctahedralDepthResolutionAndInverse.zw + probeVolumeOctahedralDepthScaleBias.zw;

        // HACKS:
        {
            int2 probeVolumeTexel2DBack = int2(
                (int)(floor(probeVolumeTexel3D.z) * probeVolumeResolution.x + probeVolumeTexel3D.x),
                (int)probeVolumeTexel3D.y
            );
            // int2 probeVolumeTexel2DFront = int2(probeVolumeTexel2DBack.x + (int)probeVolumeResolution.x, probeVolumeTexel2DBack.y);
            probeVolumeTexel2DBack *= OCTAHEDRAL_DEPTH_RESOLUTION;
            // probeVolumeTexel2DFront *= OCTAHEDRAL_DEPTH_RESOLUTION;
            int2 probeVolumeTexel2D = probeVolumeTexel2DBack;//(probeVolumeTexel3DMinOffset.z == 0.0) ? probeVolumeTexel2DBack : probeVolumeTexel2DFront;

            probeOctahedralDepthAtlasUV = probeOctahedralDepthUV * (float)OCTAHEDRAL_DEPTH_RESOLUTION + (float2)probeVolumeTexel2D;
            probeOctahedralDepthAtlasUV = probeOctahedralDepthAtlasUV * probeVolumeAtlasOctahedralDepthResolutionAndInverse.zw + probeVolumeOctahedralDepthScaleBias.zw;
        }
        
        float2 temp = SAMPLE_TEXTURE2D_LOD(_ProbeVolumeAtlasOctahedralDepth, s_linear_clamp_sampler, probeOctahedralDepthAtlasUV, 0).xy;

        return float3(temp.x, temp.y, 0.0);
    }
}
#endif

#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING_MODE == PROBEVOLUMESBILATERALFILTERINGMODES_OCTAHEDRAL_DEPTH

float ProbeVolumeOctahedralDepthReduceLightBleeding(float chebyshevWeight, float tailClipThreshold)
{   
    // Remove the [0, tailClipThreshold] tail and linearly rescale (tailClipThreshold, 1].
    // return saturate((chebyshevWeight – tailClipThreshold) / (1.0 – tailClipThreshold));
    return saturate((chebyshevWeight - tailClipThreshold) / (1.0 - tailClipThreshold));
} 

void ProbeVolumeEvaluateOctahedralDepthOcclusionFilterWeights(
    out float weights[8],
    float3 probeVolumeTexel3DMin,
    float3 probeVolumeResolution,
    float3x3 probeVolumeWorldFromTexel3DRotationScale,
    float3 probeVolumeWorldFromTexel3DTranslation,
    float4 probeVolumeOctahedralDepthScaleBias,
    float4 probeVolumeAtlasOctahedralDepthResolutionAndInverse,
    float3 samplePositionUnbiasedWS,
    float3 samplePositionBiasedWS,
    float3 sampleNormalWS)
{
    // Convert from 3D [0, probeVolumeResolution] space into 2D slice (probe) array local space.
    int2 probeVolumeTexel2DMinBack = int2(
        (int)(floor(probeVolumeTexel3DMin.z) * probeVolumeResolution.x + probeVolumeTexel3DMin.x),
        (int)probeVolumeTexel3DMin.y
    );
    int2 probeVolumeTexel2DMinFront = int2(probeVolumeTexel2DMinBack.x + (int)probeVolumeResolution.x, probeVolumeTexel2DMinBack.y);

    // Convert from 2D slice (probe) array local space into 2D slice (octahedral depth) array local space
    const int OCTAHEDRAL_DEPTH_RESOLUTION = 8;
    probeVolumeTexel2DMinBack *= OCTAHEDRAL_DEPTH_RESOLUTION;
    probeVolumeTexel2DMinFront *= OCTAHEDRAL_DEPTH_RESOLUTION;

    // Iterate over adjacent probe cage
    for (uint i = 0; i < 8; ++i)
    {
        float3 probeVolumeTexel3DMinOffset = (float3)ComputeProbeVolumeTexel3DMinOffsetFromIndex(i);
        float3 probeVolumeTexel3D = clamp(probeVolumeTexel3DMin + probeVolumeTexel3DMinOffset, 0.5, probeVolumeResolution - 0.5);

        float3 probePositionWS = mul(probeVolumeWorldFromTexel3DRotationScale, probeVolumeTexel3D) + probeVolumeWorldFromTexel3DTranslation;

        // Bias the position at which visibility is computed; this avoids performing a shadow
        // test *at* a surface, which is a dangerous location because that is exactly the line
        // between shadowed and unshadowed. If the normal bias is too small, there will be
        // light and dark leaks. If it is too large, then samples can pass through thin occluders to
        // the other side (this can only happen if there are MULTIPLE occluders near each other, a wall surface
        // won't pass through itself.)
        float3 probeToSampleBiasedWS = samplePositionBiasedWS - probePositionWS;
        float probeToSampleBiasedDistanceWS = length(probeToSampleBiasedWS);
        float3 probeToSampleBiasedDirectionWS = normalize(probeToSampleBiasedWS);

        // Clamp all of the multiplies. We can't let the weight go to zero because then it would be
        // possible for *all* weights to be equally low and get normalized
        // up to 1/n. We want to distinguish between weights that are
        // low because of different factors.

        // Computed without the biasing applied to the "dir" variable.
        // This test can cause reflection-map looking errors in the image
        // (stuff looks shiny) if the transition is poor.
        float3 probeToSampleUnbiasedDirectionWS = normalize(samplePositionUnbiasedWS - probePositionWS);

        // The naive soft backface weight would ignore a probe when
        // it is behind the surface. That's good for walls. But for small details inside of a
        // room, the normals on the details might rule out all of the probes that have mutual
        // visibility to the point. So, we instead use a "wrap shading" test below inspired by
        // NPR work.
        //
        // The small offset at the end reduces the "going to zero" impact
        // where this is really close to exactly opposite
        weights[i] = Sq(dot(-probeToSampleUnbiasedDirectionWS, sampleNormalWS) * 0.5 + 0.5) + 0.2;

        float2 probeOctahedralDepthUV = PackNormalOctQuadEncode(probeToSampleBiasedDirectionWS) * 0.5 + 0.5; // [0, 1] from [-1, 1]
        // probeOctahedralDepthUV.x = 1.0 - probeOctahedralDepthUV.x;
        // probeOctahedralDepthUV.y = 1.0 - probeOctahedralDepthUV.y;
        probeOctahedralDepthUV = clamp(probeOctahedralDepthUV, 0.5 / OCTAHEDRAL_DEPTH_RESOLUTION, 1.0 - 0.5 / OCTAHEDRAL_DEPTH_RESOLUTION);
        int2 probeVolumeTexel2DMin = (probeVolumeTexel3DMinOffset.z == 0.0) ? probeVolumeTexel2DMinBack : probeVolumeTexel2DMinFront;
        float2 probeOctahedralDepthTexel2D = (probeOctahedralDepthUV + probeVolumeTexel3DMinOffset.xy) * (float)OCTAHEDRAL_DEPTH_RESOLUTION + (float2)probeVolumeTexel2DMin;
        float2 probeOctahedralDepthAtlasUV = probeOctahedralDepthTexel2D * probeVolumeAtlasOctahedralDepthResolutionAndInverse.zw + probeVolumeOctahedralDepthScaleBias.zw;
        float2 temp = SAMPLE_TEXTURE2D_LOD(_ProbeVolumeAtlasOctahedralDepth, s_linear_clamp_sampler, probeOctahedralDepthAtlasUV, 0).xy;
        
    #if 0
        // float mean = temp.x;
        // const float REGULARIZATION = 1e-5f;
        // float variance = max(REGULARIZATION, temp.y - mean * mean);

        // // http://www.punkuser.net/vsm/vsm_paper.pdf; equation 5
        // // Need the max in the denominator because biasing can cause a negative displacement
        // float chebyshevWeight = variance / (variance + Sq(max(probeToSampleBiasedDistanceWS - mean, 0.0)));

        // // Increase contrast in the weight
        // chebyshevWeight = chebyshevWeight * chebyshevWeight * chebyshevWeight;

        
    #else
        float2 filteredDistance = temp;// * 0.25f;//2.f * temp;

        float meanDistanceToSurface = filteredDistance.x;
        float variance = abs((filteredDistance.x * filteredDistance.x) - filteredDistance.y) * _ProbeVolumeBilateralFilterOctahedralDepthParameters.y;

        // HACK
        meanDistanceToSurface == max(0.0, meanDistanceToSurface - variance);

        float chebyshevWeight = 1.f;
        if(probeToSampleBiasedDistanceWS > meanDistanceToSurface) // In "shadow"
        {
            // v must be greater than 0, which is guaranteed by the if condition above.
            float v = probeToSampleBiasedDistanceWS - meanDistanceToSurface;
            chebyshevWeight = variance / (variance + (v * v));

            chebyshevWeight = ProbeVolumeOctahedralDepthReduceLightBleeding(chebyshevWeight, _ProbeVolumeBilateralFilterOctahedralDepthParameters.x);//_ProbeVolumeBilateralFilterOctahedralDepthParameters.y);
        
            // Increase the contrast in the weight
            chebyshevWeight = max((chebyshevWeight * chebyshevWeight * chebyshevWeight), 0.0);
        }
    #endif

        // // Avoid visibility weights ever going all of the way to zero because when *no* probe has
        // // visibility we need some fallback value.
        weights[i] *= max(0.05, chebyshevWeight); // _ProbeVolumeBilateralFilterOctahedralDepthParameters.x

        // weights[i] = max(0.05f, chebyshevWeight);

        // Avoid zero weight
        weights[i] = max(0.000001, weights[i]);

        // A tiny bit of light is really visible due to log perception, so
        // crush tiny weights but keep the curve continuous.
        const float CRUSH_THRESHOLD = 0.2;
        if (weights[i] < CRUSH_THRESHOLD)
        {
            weights[i] *= weights[i] * weights[i] * (1.0 / Sq(CRUSH_THRESHOLD));
        }
    }
}
#endif

// Compute Geometric Weights based on surface position + normal, and direction to probe (similar to projected area calculation for point lights).
// source: https://advances.realtimerendering.com/s2015/SIGGRAPH_2015_Remedy_Notes.pdf
void ProbeVolumeEvaluateGeometricFilterWeights(
    out float weights[8],
    float3 probeVolumeTexel3DMin,
    float3x3 probeVolumeWorldFromTexel3DRotationScale,
    float3 probeVolumeWorldFromTexel3DTranslation,
    float3 samplePositionBiasedWS,
    float3 sampleNormalWS)
{
    for (uint i = 0; i < 8; ++i)
    {
        float3 texel3DMinOffset = (float3)ComputeProbeVolumeTexel3DMinOffsetFromIndex(i);
        float3 probeVolumeTexel3DCurrent = probeVolumeTexel3DMin + texel3DMinOffset;
        float3 probePositionWS = mul(probeVolumeWorldFromTexel3DRotationScale, probeVolumeTexel3DCurrent) + probeVolumeWorldFromTexel3DTranslation;

        weights[i] = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(sampleNormalWS, normalize(probePositionWS - samplePositionBiasedWS))));
    }
}

void ProbeVolumeEvaluateValidityWeights(out float weights[8], float3 probeVolumeTexel3DMin, float3 probeVolumeResolutionInverse, float3 probeVolumeAtlasScale, float3 probeVolumeAtlasBias)
{
    // TODO: Rather than sampling validity data from a slice in our texture array, we could place it in a different texture resource entirely.
    // This would allow us to use a single channel format, rather than wasting memory with float4(validity, unused, unused, unused).
    // It would also allow us to use a different texture format (i.e: 1x8bpp rather than 4x16bpp).
    // Currently just using a texture slice for convenience, and with the idea that MAYBE we will end up using the remaining 3 channels.
    float3 probeVolumeAtlasTexel3DFromUVWScale = float3(
        _ProbeVolumeAtlasResolutionAndSliceCount.x,
        _ProbeVolumeAtlasResolutionAndSliceCount.y,
        _ProbeVolumeAtlasResolutionAndSliceCount.z * _ProbeVolumeAtlasResolutionAndSliceCount.w
    );
    float3 probeVolumeAtlasTexel3DFromTexel3DScale = probeVolumeResolutionInverse * probeVolumeAtlasScale * probeVolumeAtlasTexel3DFromUVWScale;
    float3 probeVolumeAtlasTexel3DFromTexel3DBias = probeVolumeAtlasBias * probeVolumeAtlasTexel3DFromUVWScale;
    for (uint i = 0; i < 8; ++i)
    {
        uint3 texel3DMinOffset = ComputeProbeVolumeTexel3DMinOffsetFromIndex(i);
        float3 probeVolumeTexel3DCurrent = probeVolumeTexel3DMin + (float3)texel3DMinOffset;
        float3 probeVolumeAtlasTexel3D = probeVolumeTexel3DCurrent * probeVolumeAtlasTexel3DFromTexel3DScale + probeVolumeAtlasTexel3DFromTexel3DBias;
        weights[i] = max(_ProbeVolumeBilateralFilterWeightMin, ProbeVolumeLoadValidity((int3)probeVolumeAtlasTexel3D));
    }
}

void ProbeVolumeEvaluateAndAccumulateTrilinearWeights(inout float weights[8], float3 probeVolumeTexel3D, float3 probeVolumeTexel3DMin)
{
    float3 probeWeightTrilinearMax = saturate(probeVolumeTexel3D - probeVolumeTexel3DMin);
    float3 probeWeightTrilinearMin = 1.0 - probeWeightTrilinearMax;

    // Probe Trilinear Weights:
    // Blend between Geometric Weights and simple trilinear filter weights based on user defined _ProbeVolumeBilateralFilterWeight.
    for (uint i = 0; i < 8; ++i)
    {
        uint3 texel3DMinOffset = ComputeProbeVolumeTexel3DMinOffsetFromIndex(i);
        float3 probeWeightTrilinear = float3(
            (texel3DMinOffset.x == 1) ? probeWeightTrilinearMax.x : probeWeightTrilinearMin.x,
            (texel3DMinOffset.y == 1) ? probeWeightTrilinearMax.y : probeWeightTrilinearMin.y,
            (texel3DMinOffset.z == 1) ? probeWeightTrilinearMax.z : probeWeightTrilinearMin.z
        );

        float probeWeightTrilinearScalar = max(1e-3f, probeWeightTrilinear.x * probeWeightTrilinear.y * probeWeightTrilinear.z);
        weights[i] = max(_ProbeVolumeBilateralFilterWeightMin, probeWeightTrilinearScalar * lerp(1.0, weights[i], _ProbeVolumeBilateralFilterWeight));
    }
}

void ProbeVolumeNormalizeWeights(inout float weights[8])
{
    float probeWeightTotal = 0.0;
    for (uint i = 0; i < 8; ++i)
    {
        probeWeightTotal += weights[i];
    }

    // Weights are enforced to be > 0.0 to guard against divide by zero.
    float probeWeightNormalization = 1.0 / probeWeightTotal;

    for (i = 0; i < 8; ++i)
    {
        weights[i] *= probeWeightNormalization;
    }
}

void ProbeVolumeComputeWorldFromTexelTransforms(
    out float3x3 probeVolumeWorldFromTexel3DRotationScale,
    out float3 probeVolumeWorldFromTexel3DTranslation,
    float3x3 obbFrame,
    float3 obbExtents,
    float3 obbCenter,
    float3 probeVolumeResolutionInverse
)
{
    // TODO: Evaluate if we should we build this 3x3 matrix and a float3 bias term cpu side to decrease alu at the cost of more bandwidth.
    float3 probeVolumeWorldFromTexel3DScale = probeVolumeResolutionInverse * 2.0 * obbExtents; // [0, resolution3D] to [0.0, probeVolumeSize3D]
    probeVolumeWorldFromTexel3DRotationScale = float3x3(
        obbFrame[0] * probeVolumeWorldFromTexel3DScale,
        obbFrame[1] * probeVolumeWorldFromTexel3DScale,
        obbFrame[2] * probeVolumeWorldFromTexel3DScale
    );
    probeVolumeWorldFromTexel3DTranslation = mul(obbFrame, -obbExtents) + obbCenter;
}

#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING_SAMPLE_MODE == PROBEVOLUMESBILATERALFILTERINGSAMPLEMODES_APPROXIMATE_SAMPLE
float3 ProbeVolumeComputeTexel3DFromBilateralFilter(
    float3 probeVolumeTexel3D,
    ProbeVolumeEngineData probeVolumeData,
    float3 positionUnbiasedWS,
    float3 positionBiasedWS,
    float3 normalWS,
    float3x3 obbFrame,
    float3 obbExtents,
    float3 obbCenter)
{
    if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_NORMAL_BIAS) { return probeVolumeTexel3D; }

    float3 probeVolumeTexel3DMin = floor(probeVolumeTexel3D - 0.5) + 0.5;

    float weights[8]; for (uint i = 0; i < 8; ++i) { weights[i] = 1.0; }

    if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_GEOMETRIC_FILTER)
    {
        float3x3 probeVolumeWorldFromTexel3DRotationScale;
        float3 probeVolumeWorldFromTexel3DTranslation;
        ProbeVolumeComputeWorldFromTexelTransforms(
            probeVolumeWorldFromTexel3DRotationScale,
            probeVolumeWorldFromTexel3DTranslation,
            obbFrame,
            obbExtents,
            obbCenter,
            probeVolumeData.resolutionInverse
        );

        ProbeVolumeEvaluateGeometricFilterWeights(
            weights,
            probeVolumeTexel3DMin,
            probeVolumeWorldFromTexel3DRotationScale,
            probeVolumeWorldFromTexel3DTranslation,
            positionBiasedWS,
            normalWS
        );
    }
    else if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_PROBE_VALIDITY_FILTER)
    {
        ProbeVolumeEvaluateValidityWeights(weights, probeVolumeTexel3DMin, probeVolumeData.resolutionInverse, probeVolumeData.scale, probeVolumeData.bias);
    }
#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING_MODE == PROBEVOLUMESBILATERALFILTERINGMODES_OCTAHEDRAL_DEPTH
    else if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_OCTAHEDRAL_DEPTH_OCCLUSION_FILTER)
    {
        float3x3 probeVolumeWorldFromTexel3DRotationScale;
        float3 probeVolumeWorldFromTexel3DTranslation;
        ProbeVolumeComputeWorldFromTexelTransforms(
            probeVolumeWorldFromTexel3DRotationScale,
            probeVolumeWorldFromTexel3DTranslation,
            obbFrame,
            obbExtents,
            obbCenter,
            probeVolumeData.resolutionInverse
        );

        ProbeVolumeEvaluateOctahedralDepthOcclusionFilterWeights(
            weights,
            probeVolumeTexel3DMin,
            probeVolumeData.resolution,
            probeVolumeWorldFromTexel3DRotationScale,
            probeVolumeWorldFromTexel3DTranslation,
            probeVolumeData.octahedralDepthScaleBias,
            _ProbeVolumeAtlasOctahedralDepthResolutionAndInverse,
            positionUnbiasedWS,
            positionBiasedWS,
            normalWS
        );
    }
#endif
    else
    {
        // Fallback to no bilateral filter if _ProbeVolumeLeakMitigationMode is configured to a mode unsupported in ShaderConfig.
        return probeVolumeTexel3D;
    }

    ProbeVolumeEvaluateAndAccumulateTrilinearWeights(weights, probeVolumeTexel3D, probeVolumeTexel3DMin);

    ProbeVolumeNormalizeWeights(weights);

    // Finally, update our texture coordinate based on our weights.
    // Half-texel offset has been baked into the coordinates.
    float3 probeVolumeTexel3DMinOffsetFiltered = 0.0;
    for (i = 0; i < 8; ++i)
    {
        uint3 texel3DMinOffset = ComputeProbeVolumeTexel3DMinOffsetFromIndex(i);
        probeVolumeTexel3DMinOffsetFiltered += (float3)texel3DMinOffset * weights[i];
    }

#ifdef DEBUG_DISPLAY
    // If we are visualizing validity data, we do not want to apply our bilateral filter texture coordinate modification
    // because ideally, our filter will avoid sampling from invalid data - making this debug mode useless.
    if (_DebugProbeVolumeMode != PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
#endif
    {
        probeVolumeTexel3D = probeVolumeTexel3DMin + probeVolumeTexel3DMinOffsetFiltered;
    }

    return probeVolumeTexel3D;
}
#endif // SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING_SAMPLE_MODE == PROBEVOLUMESBILATERALFILTERINGSAMPLEMODES_APPROXIMATE_SAMPLE

#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING_SAMPLE_MODE == PROBEVOLUMESBILATERALFILTERINGSAMPLEMODES_PRECISE_LOAD
void ProbeVolumeComputeWeightsFromBilateralFilter(
    out float weights[8],
    float3 probeVolumeTexel3D,
    ProbeVolumeEngineData probeVolumeData,
    float3 positionUnbiasedWS,
    float3 positionBiasedWS,
    float3 normalWS,
    float3x3 obbFrame,
    float3 obbExtents,
    float3 obbCenter)
{
    for (uint i = 0; i < 8; ++i) { weights[i] = 1.0; }

    float3 probeVolumeTexel3DMin = floor(probeVolumeTexel3D - 0.5) + 0.5;

    if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_GEOMETRIC_FILTER)
    {
        float3x3 probeVolumeWorldFromTexel3DRotationScale;
        float3 probeVolumeWorldFromTexel3DTranslation;
        ProbeVolumeComputeWorldFromTexelTransforms(
            probeVolumeWorldFromTexel3DRotationScale,
            probeVolumeWorldFromTexel3DTranslation,
            obbFrame,
            obbExtents,
            obbCenter,
            probeVolumeData.resolutionInverse
        );

        ProbeVolumeEvaluateGeometricFilterWeights(
            weights,
            probeVolumeTexel3DMin,
            probeVolumeWorldFromTexel3DRotationScale,
            probeVolumeWorldFromTexel3DTranslation,
            positionBiasedWS,
            normalWS
        );
    }
    else if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_PROBE_VALIDITY_FILTER)
    {
        ProbeVolumeEvaluateValidityWeights(weights, probeVolumeTexel3DMin, probeVolumeData.resolutionInverse, probeVolumeData.scale, probeVolumeData.bias);
    }
#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING_MODE == PROBEVOLUMESBILATERALFILTERINGMODES_OCTAHEDRAL_DEPTH
    else if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_OCTAHEDRAL_DEPTH_OCCLUSION_FILTER)
    {
        float3x3 probeVolumeWorldFromTexel3DRotationScale;
        float3 probeVolumeWorldFromTexel3DTranslation;
        ProbeVolumeComputeWorldFromTexelTransforms(
            probeVolumeWorldFromTexel3DRotationScale,
            probeVolumeWorldFromTexel3DTranslation,
            obbFrame,
            obbExtents,
            obbCenter,
            probeVolumeData.resolutionInverse
        );

        ProbeVolumeEvaluateOctahedralDepthOcclusionFilterWeights(
            weights,
            probeVolumeTexel3DMin,
            probeVolumeData.resolution,
            probeVolumeWorldFromTexel3DRotationScale,
            probeVolumeWorldFromTexel3DTranslation,
            probeVolumeData.octahedralDepthScaleBias,
            _ProbeVolumeAtlasOctahedralDepthResolutionAndInverse,
            positionUnbiasedWS,
            positionBiasedWS,
            normalWS
        );
    }
#endif

    ProbeVolumeEvaluateAndAccumulateTrilinearWeights(weights, probeVolumeTexel3D, probeVolumeTexel3DMin);

    ProbeVolumeNormalizeWeights(weights);
}
#endif // SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING_MODE != PROBEVOLUMESBILATERALFILTERINGMODES_DISABLED

#endif // __PROBEVOLUME_BILATERAL_FILTER_HLSL__
