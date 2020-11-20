#ifndef __PROBEVOLUME_HLSL__
#define __PROBEVOLUME_HLSL__

#include "Packages/com.unity.render-pipelines.high-definition-config/Runtime/ShaderConfig.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolume.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeLightLoopDef.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeAtlas.hlsl"

// Copied from VolumeVoxelization.compute
float ProbeVolumeComputeFadeFactor(
    float3 samplePositionBoxNDC,
    float depthWS,
    float3 rcpPosFaceFade,
    float3 rcpNegFaceFade,
    float rcpDistFadeLen,
    float endTimesRcpDistFadeLen)
{
    float3 posF = Remap10(samplePositionBoxNDC, rcpPosFaceFade, rcpPosFaceFade);
    float3 negF = Remap01(samplePositionBoxNDC, rcpNegFaceFade, 0);
    float  dstF = Remap10(depthWS, rcpDistFadeLen, endTimesRcpDistFadeLen);
    float  fade = posF.x * posF.y * posF.z * negF.x * negF.y * negF.z;

    return dstF * fade;
}

#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING == PROBEVOLUMESBILATERALFILTERINGMODES_OCTAHEDRAL_DEPTH
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
        (int)(probeVolumeTexel3DMin.z * probeVolumeResolution.x + probeVolumeTexel3DMin.x),
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
        // Compute the offset grid coord and clamp to the probe grid boundary
        // Offset = 0 or 1 along each axis
        // TODO: Evaluate if using a static LUT for these offset calculations would be better / worse.
        float3 probeVolumeTexel3DOffset = (float3)(uint3(i, i >> 1, i >> 2) & uint3(1, 1, 1));
        float3 probeVolumeTexel3D = clamp(probeVolumeTexel3DMin + probeVolumeTexel3DOffset, probeVolumeResolution * 0.5, probeVolumeResolution * -0.5 + 1.0);

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

        float2 probeOctahedralDepthUV = PackNormalOctRectEncode(probeToSampleBiasedDirectionWS);
        int2 probeVolumeTexel2DMin = (probeVolumeTexel3DOffset.z == 0.0) ? probeVolumeTexel2DMinBack : probeVolumeTexel2DMinFront;
        float2 probeOctahedralDepthTexel2D = (probeOctahedralDepthUV + probeVolumeTexel3DOffset.xy) * (float)OCTAHEDRAL_DEPTH_RESOLUTION + (float2)probeVolumeTexel2DMin;
        float2 probeOctahedralDepthAtlasUV = probeOctahedralDepthTexel2D * probeVolumeAtlasOctahedralDepthResolutionAndInverse.zw + probeVolumeOctahedralDepthScaleBias.zw;

        float2 temp = SAMPLE_TEXTURE2D_LOD(_ProbeVolumeAtlasOctahedralDepth, s_linear_clamp_sampler, probeOctahedralDepthAtlasUV, 0).xy;
        float mean = temp.x;
        float variance = temp.y;

        // http://www.punkuser.net/vsm/vsm_paper.pdf; equation 5
        // Need the max in the denominator because biasing can cause a negative displacement
        float chebyshevWeight = variance / (variance + Sq(max(probeToSampleBiasedDistanceWS - mean, 0.0)));

        // Increase contrast in the weight
        chebyshevWeight = max(chebyshevWeight * chebyshevWeight * chebyshevWeight, 0.0);

        // Avoid visibility weights ever going all of the way to zero because when *no* probe has
        // visibility we need some fallback value.
        weights[i] *= max(0.2, ((probeToSampleBiasedDistanceWS <= mean) ? 1.0 : chebyshevWeight));

        // Avoid zero weight
        weights[i] = max(0.000001, weights[i]);

        // A tiny bit of light is really visible due to log perception, so
        // crush tiny weights but keep the curve continuous.
        const float CRUSH_THRESHOLD = 0.2;
        if (weights[i] < CRUSH_THRESHOLD)
        {
            weights[i] *= weights[i] * weights[i] * (1.0 / Sq(CRUSH_THRESHOLD));
        }

        // Aggressively prevent weights from going anywhere near 0.0f no matter
        // what the compiler (or, for that matter, the algorithm) thinks.
        const bool RECURSIVE = true;
        weights[i] = clamp(weights[i], (RECURSIVE ? 0.1 : 0.0), 1.01);
    }
}
#endif

void ProbeVolumeComputeOBBBoundsToFrame(OrientedBBox probeVolumeBounds, out float3x3 obbFrame, out float3 obbExtents, out float3 obbCenter)
{
    obbFrame = float3x3(probeVolumeBounds.right, probeVolumeBounds.up, cross(probeVolumeBounds.right, probeVolumeBounds.up));
    obbExtents = float3(probeVolumeBounds.extentX, probeVolumeBounds.extentY, probeVolumeBounds.extentZ);
    obbCenter = probeVolumeBounds.center;
}


void ProbeVolumeComputeTexel3DAndWeight(
    float weightHierarchy,
    ProbeVolumeEngineData probeVolumeData,
    float3x3 obbFrame,
    float3 obbExtents,
    float3 obbCenter,
    float3 samplePositionWS,
    float samplePositionLinearDepth,
    out float3 probeVolumeTexel3D,
    out float weight)
{
    float3 samplePositionBS = mul(obbFrame, samplePositionWS - obbCenter);
    float3 samplePositionBCS = samplePositionBS * rcp(obbExtents);
    float3 samplePositionBNDC = samplePositionBCS * 0.5 + 0.5;
    float3 probeVolumeUVW = clamp(samplePositionBNDC.xyz, 0.5 * probeVolumeData.resolutionInverse, 1.0 - probeVolumeData.resolutionInverse * 0.5);
    probeVolumeTexel3D = probeVolumeUVW * probeVolumeData.resolution;

    float fadeFactor = ProbeVolumeComputeFadeFactor(
        samplePositionBNDC,
        samplePositionLinearDepth,
        probeVolumeData.rcpPosFaceFade,
        probeVolumeData.rcpNegFaceFade,
        probeVolumeData.rcpDistFadeLen,
        probeVolumeData.endTimesRcpDistFadeLen
    );

    weight = fadeFactor * probeVolumeData.weight;

#if SHADEROPTIONS_PROBE_VOLUMES_ADDITIVE_BLENDING
    if (probeVolumeData.volumeBlendMode == VOLUMEBLENDMODE_ADDITIVE)
        weight = fadeFactor;
    else if (probeVolumeData.volumeBlendMode == VOLUMEBLENDMODE_SUBTRACTIVE)
        weight = -fadeFactor;
    else
#endif
    {
        // Alpha composite: weight = (1.0f - weightHierarchy) * fadeFactor;
        weight = weightHierarchy * -fadeFactor + fadeFactor;
    }
}

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
#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING == PROBEVOLUMESBILATERALFILTERINGMODES_DISABLED
    return probeVolumeTexel3D;
#else
    if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_NORMAL_BIAS) { return probeVolumeTexel3D; }

    float3 probeVolumeTexel3DMin = floor(probeVolumeTexel3D - 0.5) + 0.5;

    float probeWeightBSW = 1.0;
    float probeWeightBSE = 1.0;
    float probeWeightBNW = 1.0;
    float probeWeightBNE = 1.0;
    float probeWeightTSW = 1.0;
    float probeWeightTSE = 1.0;
    float probeWeightTNW = 1.0;
    float probeWeightTNE = 1.0;
    if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_GEOMETRIC_FILTER)
    {
        // Compute Geometric Weights based on surface position + normal, and direction to probe (similar to projected area calculation for point lights).
        // source: https://advances.realtimerendering.com/s2015/SIGGRAPH_2015_Remedy_Notes.pdf
        probeWeightBSW = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(normalWS, normalize(float3(probeVolumeTexel3DMin.x + 0.0, probeVolumeTexel3DMin.y + 0.0, probeVolumeTexel3DMin.z + 0.0) - probeVolumeTexel3D))));
        probeWeightBSE = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(normalWS, normalize(float3(probeVolumeTexel3DMin.x + 1.0, probeVolumeTexel3DMin.y + 0.0, probeVolumeTexel3DMin.z + 0.0) - probeVolumeTexel3D))));
        probeWeightBNW = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(normalWS, normalize(float3(probeVolumeTexel3DMin.x + 0.0, probeVolumeTexel3DMin.y + 0.0, probeVolumeTexel3DMin.z + 1.0) - probeVolumeTexel3D))));
        probeWeightBNE = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(normalWS, normalize(float3(probeVolumeTexel3DMin.x + 1.0, probeVolumeTexel3DMin.y + 0.0, probeVolumeTexel3DMin.z + 1.0) - probeVolumeTexel3D))));

        probeWeightTSW = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(normalWS, normalize(float3(probeVolumeTexel3DMin.x + 0.0, probeVolumeTexel3DMin.y + 1.0, probeVolumeTexel3DMin.z + 0.0) - probeVolumeTexel3D))));
        probeWeightTSE = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(normalWS, normalize(float3(probeVolumeTexel3DMin.x + 1.0, probeVolumeTexel3DMin.y + 1.0, probeVolumeTexel3DMin.z + 0.0) - probeVolumeTexel3D))));
        probeWeightTNW = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(normalWS, normalize(float3(probeVolumeTexel3DMin.x + 0.0, probeVolumeTexel3DMin.y + 1.0, probeVolumeTexel3DMin.z + 1.0) - probeVolumeTexel3D))));
        probeWeightTNE = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(normalWS, normalize(float3(probeVolumeTexel3DMin.x + 1.0, probeVolumeTexel3DMin.y + 1.0, probeVolumeTexel3DMin.z + 1.0) - probeVolumeTexel3D))));
    }
    else if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_PROBE_VALIDITY_FILTER)
    {
        // TODO: Rather than sampling validity data from a slice in our texture array, we could place it in a different texture resource entirely.
        // This would allow us to use a single channel format, rather than wasting memory with float4(validity, unused, unused, unused).
        // It would also allow us to use a different texture format (i.e: 1x8bpp rather than 4x16bpp).
        // Currently just using a texture slice for convenience, and with the idea that MAYBE we will end up using the remaining 3 channels.
        probeWeightBSW = max(_ProbeVolumeBilateralFilterWeightMin, ProbeVolumeLoadValidity(int3(probeVolumeTexel3DMin.x + 0, probeVolumeTexel3DMin.y + 0, probeVolumeTexel3DMin.z + 0)));
        probeWeightBSE = max(_ProbeVolumeBilateralFilterWeightMin, ProbeVolumeLoadValidity(int3(probeVolumeTexel3DMin.x + 1, probeVolumeTexel3DMin.y + 0, probeVolumeTexel3DMin.z + 0)));
        probeWeightBNW = max(_ProbeVolumeBilateralFilterWeightMin, ProbeVolumeLoadValidity(int3(probeVolumeTexel3DMin.x + 0, probeVolumeTexel3DMin.y + 0, probeVolumeTexel3DMin.z + 1)));
        probeWeightBNE = max(_ProbeVolumeBilateralFilterWeightMin, ProbeVolumeLoadValidity(int3(probeVolumeTexel3DMin.x + 1, probeVolumeTexel3DMin.y + 0, probeVolumeTexel3DMin.z + 1)));

        probeWeightTSW = max(_ProbeVolumeBilateralFilterWeightMin, ProbeVolumeLoadValidity(int3(probeVolumeTexel3DMin.x + 0, probeVolumeTexel3DMin.y + 1, probeVolumeTexel3DMin.z + 0)));
        probeWeightTSE = max(_ProbeVolumeBilateralFilterWeightMin, ProbeVolumeLoadValidity(int3(probeVolumeTexel3DMin.x + 1, probeVolumeTexel3DMin.y + 1, probeVolumeTexel3DMin.z + 1)));
        probeWeightTNW = max(_ProbeVolumeBilateralFilterWeightMin, ProbeVolumeLoadValidity(int3(probeVolumeTexel3DMin.x + 0, probeVolumeTexel3DMin.y + 1, probeVolumeTexel3DMin.z + 1)));
        probeWeightTNE = max(_ProbeVolumeBilateralFilterWeightMin, ProbeVolumeLoadValidity(int3(probeVolumeTexel3DMin.x + 1, probeVolumeTexel3DMin.y + 1, probeVolumeTexel3DMin.z + 1)));
    }
#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING == PROBEVOLUMESBILATERALFILTERINGMODES_OCTAHEDRAL_DEPTH
    else if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_OCTAHEDRAL_DEPTH_OCCLUSION_FILTER)
    {
        // TODO: Evaluate if we should we build this 3x3 matrix and a float3 bias term cpu side to decrease alu at the cost of more bandwidth.
        float3 probeVolumeWorldFromTexel3DScale = probeVolumeData.resolutionInverse * 2.0 * obbExtents; // [0, resolution3D] to [0.0, probeVolumeSize3D]
        float3x3 probeVolumeWorldFromTexel3DRotationScale = float3x3(
            obbFrame[0] * probeVolumeWorldFromTexel3DScale,
            obbFrame[1] * probeVolumeWorldFromTexel3DScale,
            obbFrame[2] * probeVolumeWorldFromTexel3DScale
        );
        float3 probeVolumeWorldFromTexel3DTranslation = mul(obbFrame, -obbExtents) + obbCenter;

        float probeWeights[8];
        ProbeVolumeEvaluateOctahedralDepthOcclusionFilterWeights(
            probeWeights,
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
        probeWeightBSW = probeWeights[0]; // (i == 0) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(0, 0 >> 1, 0 >> 2) & int3(1, 1, 1)) => int3(0, 0, 0)
        probeWeightBSE = probeWeights[1]; // (i == 1) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(1, 1 >> 1, 1 >> 2) & int3(1, 1, 1)) => int3(1, 0, 0)
        probeWeightBNW = probeWeights[2]; // (i == 2) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(2, 2 >> 1, 2 >> 2) & int3(1, 1, 1)) => int3(0, 1, 0)
        probeWeightBNE = probeWeights[3]; // (i == 3) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(3, 3 >> 1, 3 >> 2) & int3(1, 1, 1)) => int3(1, 1, 0)

        probeWeightTSW = probeWeights[4]; // (i == 4) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(4, 4 >> 1, 4 >> 2) & int3(1, 1, 1)) => int3(0, 0, 1)
        probeWeightTSE = probeWeights[5]; // (i == 5) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(5, 5 >> 1, 5 >> 2) & int3(1, 1, 1)) => int3(1, 0, 1)
        probeWeightTNW = probeWeights[6]; // (i == 6) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(6, 6 >> 1, 6 >> 2) & int3(1, 1, 1)) => int3(0, 1, 1)
        probeWeightTNE = probeWeights[7]; // (i == 7) => (int3(i, i >> 1, i >> 2) & int3(1, 1, 1)) => (int3(7, 7 >> 1, 7 >> 2) & int3(1, 1, 1)) => int3(1, 1, 1)
    }
#endif
    else
    {
        // Fallback to no bilateral filter if _ProbeVolumeLeakMitigationMode is configured to a mode unsupported in ShaderConfig.
        return probeVolumeTexel3D;
    }

    // Blend between Geometric Weights and simple trilinear filter weights based on user defined _ProbeVolumeBilateralFilterWeight.
    {
        float3 probeWeightTrilinearMax = frac(probeVolumeTexel3D - 0.5);
        float3 probeWeightTrilinearMin = 1.0 - probeWeightTrilinearMax;

        float probeWeightTrilinearBSW = probeWeightTrilinearMin.x * probeWeightTrilinearMin.y * probeWeightTrilinearMin.z;
        float probeWeightTrilinearBSE = probeWeightTrilinearMax.x * probeWeightTrilinearMin.y * probeWeightTrilinearMin.z;
        float probeWeightTrilinearBNW = probeWeightTrilinearMin.x * probeWeightTrilinearMin.y * probeWeightTrilinearMax.z;
        float probeWeightTrilinearBNE = probeWeightTrilinearMax.x * probeWeightTrilinearMin.y * probeWeightTrilinearMax.z;
        float probeWeightTrilinearTSW = probeWeightTrilinearMin.x * probeWeightTrilinearMax.y * probeWeightTrilinearMin.z;
        float probeWeightTrilinearTSE = probeWeightTrilinearMax.x * probeWeightTrilinearMax.y * probeWeightTrilinearMin.z;
        float probeWeightTrilinearTNW = probeWeightTrilinearMin.x * probeWeightTrilinearMax.y * probeWeightTrilinearMax.z;
        float probeWeightTrilinearTNE = probeWeightTrilinearMax.x * probeWeightTrilinearMax.y * probeWeightTrilinearMax.z;

        probeWeightBSW = lerp(probeWeightTrilinearBSW, probeWeightTrilinearBSW * probeWeightBSW, _ProbeVolumeBilateralFilterWeight);
        probeWeightBSE = lerp(probeWeightTrilinearBSE, probeWeightTrilinearBSE * probeWeightBSE, _ProbeVolumeBilateralFilterWeight);
        probeWeightBNW = lerp(probeWeightTrilinearBNW, probeWeightTrilinearBNW * probeWeightBNW, _ProbeVolumeBilateralFilterWeight);
        probeWeightBNE = lerp(probeWeightTrilinearBNE, probeWeightTrilinearBNE * probeWeightBNE, _ProbeVolumeBilateralFilterWeight);

        probeWeightTSW = lerp(probeWeightTrilinearTSW, probeWeightTrilinearTSW * probeWeightTSW, _ProbeVolumeBilateralFilterWeight);
        probeWeightTSE = lerp(probeWeightTrilinearTSE, probeWeightTrilinearTSE * probeWeightTSE, _ProbeVolumeBilateralFilterWeight);
        probeWeightTNW = lerp(probeWeightTrilinearTNW, probeWeightTrilinearTNW * probeWeightTNW, _ProbeVolumeBilateralFilterWeight);
        probeWeightTNE = lerp(probeWeightTrilinearTNE, probeWeightTrilinearTNE * probeWeightTNE, _ProbeVolumeBilateralFilterWeight);
    }

    float probeWeightTotal =
        probeWeightBSW +
        probeWeightBSE +
        probeWeightBNW +
        probeWeightBNE +
        probeWeightTSW +
        probeWeightTSE +
        probeWeightTNW +
        probeWeightTNE;

    // Weights are enforced to be > 0.0 to guard against divide by zero.
    float probeWeightNormalization = 1.0 / probeWeightTotal;

    probeWeightBSW *= probeWeightNormalization;
    probeWeightBSE *= probeWeightNormalization;
    probeWeightBNW *= probeWeightNormalization;
    probeWeightBNE *= probeWeightNormalization;
    probeWeightTSW *= probeWeightNormalization;
    probeWeightTSE *= probeWeightNormalization;
    probeWeightTNW *= probeWeightNormalization;
    probeWeightTNE *= probeWeightNormalization;

    // Finally, update our texture coordinate based on our weights.
    // Half-texel offset has been baked into the coordinates.
    float3 probeVolumeTexel3DFrac =
        float3(0.5, 0.5, 0.5) * probeWeightBSW +
        float3(1.5, 0.5, 0.5) * probeWeightBSE +
        float3(0.5, 0.5, 1.5) * probeWeightBNW +
        float3(1.5, 0.5, 1.5) * probeWeightBNE +
        float3(0.5, 1.5, 0.5) * probeWeightTSW +
        float3(1.5, 1.5, 0.5) * probeWeightTSE +
        float3(0.5, 1.5, 1.5) * probeWeightTNW +
        float3(1.5, 1.5, 1.5) * probeWeightTNE;

#ifdef DEBUG_DISPLAY
    // If we are visualizing validity data, we do not want to apply our bilateral filter texture coordinate modification
    // because ideally, our filter will avoid sampling from invalid data - making this debug mode useless.
    if (_DebugProbeVolumeMode != PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
#endif
    {
        probeVolumeTexel3D = floor(probeVolumeTexel3D - 0.5) + probeVolumeTexel3DFrac;
    }

    return probeVolumeTexel3D;
#endif
}

float3 ProbeVolumeEvaluateSphericalHarmonicsL0(float3 normalWS, ProbeVolumeSphericalHarmonicsL0 coefficients)
{

#ifdef DEBUG_DISPLAY
    if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS)
    {
        float3 debugColors = coefficients.data[0].rgb;
        return debugColors;
    }
    else if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
    {
        float validity = coefficients.data[0].x;
        return lerp(float3(1, 0, 0), float3(0, 1, 0), validity);
    }
    else
#endif
    {
        float3 sampleOutgoingRadiance = coefficients.data[0].rgb;
        return sampleOutgoingRadiance;
    }
}

float3 ProbeVolumeEvaluateSphericalHarmonicsL1(float3 normalWS, ProbeVolumeSphericalHarmonicsL1 coefficients)
{

#ifdef DEBUG_DISPLAY
    if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS)
    {
        float3 debugColors = coefficients.data[0].rgb;
        return debugColors;
    }
    else if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
    {
        float validity = coefficients.data[0].x;
        return lerp(float3(1, 0, 0), float3(0, 1, 0), validity);
    }
    else
#endif
    {
        float3 sampleOutgoingRadiance = SHEvalLinearL0L1(normalWS, coefficients.data[0], coefficients.data[1], coefficients.data[2]);
        return sampleOutgoingRadiance;
    }
}

float3 ProbeVolumeEvaluateSphericalHarmonicsL2(float3 normalWS, ProbeVolumeSphericalHarmonicsL2 coefficients)
{

#ifdef DEBUG_DISPLAY
    if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS)
    {
        float3 debugColors = coefficients.data[0].rgb;
        return debugColors;
    }
    else if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
    {
        float validity = coefficients.data[0].x;
        return lerp(float3(1, 0, 0), float3(0, 1, 0), validity);
    }
    else
#endif
    {
        float3 sampleOutgoingRadiance = SampleSH9(coefficients.data, normalWS);
        return sampleOutgoingRadiance;
    }
}

// Fallback to global ambient probe lighting when probe volume lighting weight is not fully saturated.
float3 ProbeVolumeEvaluateAmbientProbeFallback(float3 normalWS, float weightHierarchy)
{
    float3 sampleAmbientProbeOutgoingRadiance = float3(0.0, 0.0, 0.0);
    if (weightHierarchy < 1.0
#ifdef DEBUG_DISPLAY
        && (_DebugProbeVolumeMode != PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS)
        && (_DebugProbeVolumeMode != PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
#endif
    )
    {

        sampleAmbientProbeOutgoingRadiance = SampleSH9(_ProbeVolumeAmbientProbeFallbackPackedCoeffs, normalWS) * (1.0 - weightHierarchy);
    }

    return sampleAmbientProbeOutgoingRadiance;
}

// Generate ProbeVolumeAccumulateSphericalHarmonicsL0 function:
#define PROBE_VOLUMES_ACCUMULATE_MODE PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L0
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeAccumulate.hlsl"
#undef PROBE_VOLUMES_ACCUMULATE_MODE

// Generate ProbeVolumeAccumulateSphericalHarmonicsL1 function:
#define PROBE_VOLUMES_ACCUMULATE_MODE PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeAccumulate.hlsl"
#undef PROBE_VOLUMES_ACCUMULATE_MODE

// Generate ProbeVolumeAccumulateSphericalHarmonicsL2 function:
#define PROBE_VOLUMES_ACCUMULATE_MODE PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeAccumulate.hlsl"
#undef PROBE_VOLUMES_ACCUMULATE_MODE

#ifndef PROBE_VOLUMES_SAMPLING_MODE
// Default to sampling probe volumes at native atlas encoding mode.
// Users can override this by defining PROBE_VOLUMES_SAMPLING_MODE before including LightLoop.hlsl
// TODO: It's likely we will want to extend this out to simply be shader LOD quality levels,
// as there are other parameters such as bilateral filtering, additive blending, and normal bias
// that we will want to disable for a low quality high performance mode.
#define PROBE_VOLUMES_SAMPLING_MODE SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE
#endif

void ProbeVolumeEvaluateSphericalHarmonics(PositionInputs posInput, float3 normalWS, float3 backNormalWS, uint renderingLayers, float weightHierarchy, inout float3 bakeDiffuseLighting, inout float3 backBakeDiffuseLighting)
{
#if PROBE_VOLUMES_SAMPLING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L0
        ProbeVolumeSphericalHarmonicsL0 coefficients;
        ProbeVolumeAccumulateSphericalHarmonicsL0(posInput, normalWS, renderingLayers, coefficients, weightHierarchy);
        bakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL0(normalWS, coefficients);
        backBakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL0(backNormalWS, coefficients);

#elif PROBE_VOLUMES_SAMPLING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
        ProbeVolumeSphericalHarmonicsL1 coefficients;
        ProbeVolumeAccumulateSphericalHarmonicsL1(posInput, normalWS, renderingLayers, coefficients, weightHierarchy);
        bakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL1(normalWS, coefficients);
        backBakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL1(backNormalWS, coefficients);

#elif PROBE_VOLUMES_SAMPLING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
        ProbeVolumeSphericalHarmonicsL2 coefficients;
        ProbeVolumeAccumulateSphericalHarmonicsL2(posInput, normalWS, renderingLayers, coefficients, weightHierarchy);
        bakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL2(normalWS, coefficients);
        backBakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL2(backNormalWS, coefficients);

#endif

        bakeDiffuseLighting += ProbeVolumeEvaluateAmbientProbeFallback(normalWS, weightHierarchy);
        backBakeDiffuseLighting += ProbeVolumeEvaluateAmbientProbeFallback(backNormalWS, weightHierarchy);
}

#endif // __PROBEVOLUME_HLSL__
