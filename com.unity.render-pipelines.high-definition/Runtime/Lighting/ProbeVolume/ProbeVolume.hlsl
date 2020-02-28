#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolume.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinGIUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

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

void EvaluateProbeVolumeOctahedralDepthOcclusionFilterWeights(
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

void EvaluateProbeVolumes(PositionInputs posInput, BSDFData bsdfData, inout BuiltinData builtinData)
{
    const float3 noLight = float3(0.0, 0.0, 0.0);

    float3 probeVolumeDiffuseLighting = noLight;
    float probeVolumeHierarchyWeight = 0.0; // Max: 1.0

    // Determine if GI (from Lightmaps) is already present.
    // If so only process non-normal layer volumes.
    bool uninitialized = IsUninitializedGI(builtinData.bakeDiffuseLighting);
    if (!uninitialized)
    {
        probeVolumeDiffuseLighting = builtinData.bakeDiffuseLighting;
        probeVolumeHierarchyWeight = 1.0;
    }

    uint probeVolumeStart, probeVolumeCount;

    bool fastPath = false;
    // Fetch first probe volume to provide the scene proxy for screen space computation
#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    GetCountAndStart(posInput, LIGHTCATEGORY_PROBE_VOLUME, probeVolumeStart, probeVolumeCount);

#if SCALARIZE_LIGHT_LOOP
    // Fast path is when we all pixels in a wave is accessing same tile or cluster.
    uint probeVolumeStartFirstLane = WaveReadLaneFirst(probeVolumeStart);
    fastPath = WaveActiveAllTrue(probeVolumeStart == probeVolumeStartFirstLane);
#endif

#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    probeVolumeCount = _ProbeVolumeCount;
    probeVolumeStart = 0;
#endif

    // Reflection probes are sorted by volume (in the increasing order).

    // context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;
#if SCALARIZE_LIGHT_LOOP
    if (fastPath)
    {
        probeVolumeStart = probeVolumeStartFirstLane;
    }
#endif

    // Scalarized loop, same rationale of the punctual light version
    uint v_probeVolumeListOffset = 0;
    uint v_probeVolumeIdx = probeVolumeStart;
    while (v_probeVolumeListOffset < probeVolumeCount)
    {
        v_probeVolumeIdx = FetchIndex(probeVolumeStart, v_probeVolumeListOffset);
        uint s_probeVolumeIdx = v_probeVolumeIdx;

#if SCALARIZE_LIGHT_LOOP
        if (!fastPath)
        {
            s_probeVolumeIdx = WaveActiveMin(v_probeVolumeIdx);
            // If we are not in fast path, s_probeVolumeIdx is not scalar
           // If WaveActiveMin returns 0xffffffff it means that all lanes are actually dead, so we can safely ignore the loop and move forward.
           // This could happen as an helper lane could reach this point, hence having a valid v_lightIdx, but their values will be ignored by the WaveActiveMin
            if (s_probeVolumeIdx == -1)
            {
                break;
            }
        }
        // Note that the WaveReadLaneFirst should not be needed, but the compiler might insist in putting the result in VGPR.
        // However, we are certain at this point that the index is scalar.
        s_probeVolumeIdx = WaveReadLaneFirst(s_probeVolumeIdx);

#endif

        // Scalar load.
        ProbeVolumeEngineData s_probeVolumeData = _ProbeVolumeDatas[s_probeVolumeIdx];
        OrientedBBox s_probeVolumeBounds = _ProbeVolumeBounds[s_probeVolumeIdx];

        // If current scalar and vector light index match, we process the light. The v_envLightListOffset for current thread is increased.
        // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
        // end up with a unique v_envLightIdx value that is smaller than s_envLightIdx hence being stuck in a loop. All the active lanes will not have this problem.
        if (s_probeVolumeIdx >= v_probeVolumeIdx)
        {
            v_probeVolumeListOffset++;
            bool ignoreWeight = s_probeVolumeData.volumeBlendMode != VOLUMEBLENDMODE_NORMAL;
            if (probeVolumeHierarchyWeight < 1.0 || ignoreWeight)
            {
                // TODO: Implement light layer support for probe volumes.
                // if (IsMatchingLightLayer(s_probeVolumeData.lightLayers, builtinData.renderingLayers)) { EVALUATE_BSDF_ENV_SKY(s_probeVolumeData, TYPE, type) }

                // TODO: Implement per-probe user defined max weight.
                float weight = 0.0;
                float4 sampleShAr = 0.0;
                float4 sampleShAg = 0.0;
                float4 sampleShAb = 0.0;
                {
                    float3x3 obbFrame = float3x3(s_probeVolumeBounds.right, s_probeVolumeBounds.up, cross(s_probeVolumeBounds.right, s_probeVolumeBounds.up));
                    float3 obbExtents = float3(s_probeVolumeBounds.extentX, s_probeVolumeBounds.extentY, s_probeVolumeBounds.extentZ);

                    // Note: When normal bias is > 0, bounds using in tile / cluster assignment are conservatively dilated CPU side to handle worst case normal bias.
                    float3 samplePositionWS = bsdfData.normalWS * _ProbeVolumeNormalBiasWS + posInput.positionWS;
                    float3 samplePositionBS = mul(obbFrame, samplePositionWS - s_probeVolumeBounds.center);
                    float3 samplePositionBCS = samplePositionBS * rcp(obbExtents);

                    // TODO: Verify if this early out is actually improving performance.
                    bool isInsideProbeVolume = max(abs(samplePositionBCS.x), max(abs(samplePositionBCS.y), abs(samplePositionBCS.z))) < 1.0;
                    if (!isInsideProbeVolume) { continue; }

                    float3 samplePositionBNDC = samplePositionBCS * 0.5 + 0.5;

                    float fadeFactor = ProbeVolumeComputeFadeFactor(
                        samplePositionBNDC,
                        posInput.linearDepth,
                        s_probeVolumeData.rcpPosFaceFade,
                        s_probeVolumeData.rcpNegFaceFade,
                        s_probeVolumeData.rcpDistFadeLen,
                        s_probeVolumeData.endTimesRcpDistFadeLen
                    );

                    fadeFactor *= s_probeVolumeData.weight;

                    if (s_probeVolumeData.volumeBlendMode == VOLUMEBLENDMODE_ADDITIVE)
                        weight = fadeFactor;
                    else if (s_probeVolumeData.volumeBlendMode == VOLUMEBLENDMODE_SUBTRACTIVE)
                        weight = -fadeFactor;
                    else
                        // Alpha composite: weight = (1.0f - probeVolumeHierarchyWeight) * fadeFactor;
                        weight = probeVolumeHierarchyWeight * -fadeFactor + fadeFactor;

                    if (weight > 0.0 || ignoreWeight)
                    {
                        // TODO: Cleanup / optimize this math.
                        float3 probeVolumeUVW = clamp(samplePositionBNDC.xyz, 0.5 * s_probeVolumeData.resolutionInverse, 1.0 - s_probeVolumeData.resolutionInverse * 0.5);
                        float3 probeVolumeTexel3D = probeVolumeUVW * s_probeVolumeData.resolution;

                        if (_ProbeVolumeLeakMitigationMode != LEAKMITIGATIONMODE_NORMAL_BIAS)
                        {
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
                                probeWeightBSW = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(bsdfData.normalWS, normalize(float3(probeVolumeTexel3DMin.x + 0.0, probeVolumeTexel3DMin.y + 0.0, probeVolumeTexel3DMin.z + 0.0) - probeVolumeTexel3D))));
                                probeWeightBSE = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(bsdfData.normalWS, normalize(float3(probeVolumeTexel3DMin.x + 1.0, probeVolumeTexel3DMin.y + 0.0, probeVolumeTexel3DMin.z + 0.0) - probeVolumeTexel3D))));
                                probeWeightBNW = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(bsdfData.normalWS, normalize(float3(probeVolumeTexel3DMin.x + 0.0, probeVolumeTexel3DMin.y + 0.0, probeVolumeTexel3DMin.z + 1.0) - probeVolumeTexel3D))));
                                probeWeightBNE = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(bsdfData.normalWS, normalize(float3(probeVolumeTexel3DMin.x + 1.0, probeVolumeTexel3DMin.y + 0.0, probeVolumeTexel3DMin.z + 1.0) - probeVolumeTexel3D))));

                                probeWeightTSW = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(bsdfData.normalWS, normalize(float3(probeVolumeTexel3DMin.x + 0.0, probeVolumeTexel3DMin.y + 1.0, probeVolumeTexel3DMin.z + 0.0) - probeVolumeTexel3D))));
                                probeWeightTSE = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(bsdfData.normalWS, normalize(float3(probeVolumeTexel3DMin.x + 1.0, probeVolumeTexel3DMin.y + 1.0, probeVolumeTexel3DMin.z + 0.0) - probeVolumeTexel3D))));
                                probeWeightTNW = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(bsdfData.normalWS, normalize(float3(probeVolumeTexel3DMin.x + 0.0, probeVolumeTexel3DMin.y + 1.0, probeVolumeTexel3DMin.z + 1.0) - probeVolumeTexel3D))));
                                probeWeightTNE = max(_ProbeVolumeBilateralFilterWeightMin, saturate(dot(bsdfData.normalWS, normalize(float3(probeVolumeTexel3DMin.x + 1.0, probeVolumeTexel3DMin.y + 1.0, probeVolumeTexel3DMin.z + 1.0) - probeVolumeTexel3D))));
                            }
                            else if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_PROBE_VALIDITY_FILTER)
                            {
                                int2 probeVolumeTexel2DBackSW = int2(
                                    (int)(max(0.0, floor(probeVolumeTexel3D.z - 0.5)) * s_probeVolumeData.resolution.x + floor(probeVolumeTexel3D.x - 0.5) + 0.5),
                                    (int)(floor(probeVolumeTexel3D.y - 0.5) + 0.5)
                                    );
                                int2 probeVolumeTexel2DFrontSW = int2(probeVolumeTexel2DBackSW.x + (int)s_probeVolumeData.resolution.x, probeVolumeTexel2DBackSW.y);

                                // TODO: Rather than sampling validity data from a slice in our texture array, we could place it in a different texture resource entirely.
                                // This would allow us to use a single channel format, rather than wasting memory with float4(validity, unused, unused, unused).
                                // It would also allow us to use a different texture format (i.e: 1x8bpp rather than 4x16bpp).
                                // Currently just using a texture slice for convenience, and with the idea that MAYBE we will end up using the remaining 3 channels.
                                probeWeightBSW = max(_ProbeVolumeBilateralFilterWeightMin, LOAD_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, int2(probeVolumeTexel2DBackSW.x + 0, probeVolumeTexel2DBackSW.y + 0), 3, 0).x);
                                probeWeightBSE = max(_ProbeVolumeBilateralFilterWeightMin, LOAD_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, int2(probeVolumeTexel2DBackSW.x + 1, probeVolumeTexel2DBackSW.y + 0), 3, 0).x);
                                probeWeightBNW = max(_ProbeVolumeBilateralFilterWeightMin, LOAD_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, int2(probeVolumeTexel2DFrontSW.x + 0, probeVolumeTexel2DFrontSW.y + 0), 3, 0).x);
                                probeWeightBNE = max(_ProbeVolumeBilateralFilterWeightMin, LOAD_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, int2(probeVolumeTexel2DFrontSW.x + 1, probeVolumeTexel2DFrontSW.y + 0), 3, 0).x);

                                probeWeightTSW = max(_ProbeVolumeBilateralFilterWeightMin, LOAD_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, int2(probeVolumeTexel2DBackSW.x + 0, probeVolumeTexel2DBackSW.y + 1), 3, 0).x);
                                probeWeightTSE = max(_ProbeVolumeBilateralFilterWeightMin, LOAD_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, int2(probeVolumeTexel2DBackSW.x + 1, probeVolumeTexel2DBackSW.y + 1), 3, 0).x);
                                probeWeightTNW = max(_ProbeVolumeBilateralFilterWeightMin, LOAD_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, int2(probeVolumeTexel2DFrontSW.x + 0, probeVolumeTexel2DFrontSW.y + 1), 3, 0).x);
                                probeWeightTNE = max(_ProbeVolumeBilateralFilterWeightMin, LOAD_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, int2(probeVolumeTexel2DFrontSW.x + 1, probeVolumeTexel2DFrontSW.y + 1), 3, 0).x);
                            }
                            else if (_ProbeVolumeLeakMitigationMode == LEAKMITIGATIONMODE_OCTAHEDRAL_DEPTH_OCCLUSION_FILTER)
                            {
                                float3 probeVolumeTexel3DMin = floor(probeVolumeTexel3D - 0.5) + 0.5;

                                // TODO: Evaluate if we should we build this 3x3 matrix and a float3 bias term cpu side to decrease alu at the cost of more bandwidth.
                                float3 probeVolumeWorldFromTexel3DScale = s_probeVolumeData.resolutionInverse * 2.0 * obbExtents; // [0, resolution3D] to [0.0, probeVolumeSize3D]
                                float3x3 probeVolumeWorldFromTexel3DRotationScale = float3x3(
                                    obbFrame[0] * probeVolumeWorldFromTexel3DScale,
                                    obbFrame[1] * probeVolumeWorldFromTexel3DScale,
                                    obbFrame[2] * probeVolumeWorldFromTexel3DScale
                                );
                                float3 probeVolumeWorldFromTexel3DTranslation = mul(obbFrame, -obbExtents) + s_probeVolumeBounds.center;

                                float probeWeights[8];
                                EvaluateProbeVolumeOctahedralDepthOcclusionFilterWeights(
                                    probeWeights,
                                    probeVolumeTexel3DMin,
                                    s_probeVolumeData.resolution,
                                    probeVolumeWorldFromTexel3DRotationScale,
                                    probeVolumeWorldFromTexel3DTranslation,
                                    s_probeVolumeData.octahedralDepthScaleBias,
                                    _ProbeVolumeAtlasOctahedralDepthResolutionAndInverse,
                                    posInput.positionWS, // unbiased
                                    samplePositionWS, // biased
                                    bsdfData.normalWS
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

#if DEBUG_DISPLAY
                            // If we are visualizing validity data, we do not want to apply our bilateral filter texture coordinate modification
                            // because ideally, our filter will avoid sampling from invalid data - making this debug mode useless.
                            if (_DebugProbeVolumeMode != PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
#endif
                            {
                                probeVolumeTexel3D = floor(probeVolumeTexel3D - 0.5) + probeVolumeTexel3DFrac;
                            }
                        }

                        float2 probeVolumeTexel2DBack = float2(
                            max(0.0, floor(probeVolumeTexel3D.z - 0.5)) * s_probeVolumeData.resolution.x + probeVolumeTexel3D.x,
                            probeVolumeTexel3D.y
                            );

                        float2 probeVolumeTexel2DFront = float2(
                            max(0.0, floor(probeVolumeTexel3D.z + 0.5)) * s_probeVolumeData.resolution.x + probeVolumeTexel3D.x,
                            probeVolumeTexel3D.y
                            );

                        float2 probeVolumeAtlasUV2DBack = probeVolumeTexel2DBack * _ProbeVolumeAtlasResolutionAndInverse.zw + s_probeVolumeData.scaleBias.zw;
                        float2 probeVolumeAtlasUV2DFront = probeVolumeTexel2DFront * _ProbeVolumeAtlasResolutionAndInverse.zw + s_probeVolumeData.scaleBias.zw;

                        float lerpZ = frac(probeVolumeTexel3D.z - 0.5);

#if DEBUG_DISPLAY
                        if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
                        {
                            float validity = lerp(
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DBack, 3, 0).x,
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DFront, 3, 0).x,
                                lerpZ
                            );

                            // Pack validity into SH data so that we can access it later for our debug mode.
                            sampleShAr = float4(validity, 0.0, 0.0, 0.0);
                            sampleShAg = 0.0;
                            sampleShAb = 0.0;
                        }
                        else
#endif
                        {
                            sampleShAr = lerp(
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DBack, 0, 0),
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DFront, 0, 0),
                                lerpZ
                            );
                            sampleShAg = lerp(
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DBack, 1, 0),
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DFront, 1, 0),
                                lerpZ
                            );
                            sampleShAb = lerp(
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DBack, 2, 0),
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DFront, 2, 0),
                                lerpZ
                            );
                        }
                    }
                }

                float3 sampleOutgoingRadiance = EvaluateBSDF_LightProbeL1(builtinData, bsdfData, sampleShAr, sampleShAg, sampleShAb);

#ifdef DEBUG_DISPLAY
                if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS)
                {
                    probeVolumeDiffuseLighting += s_probeVolumeData.debugColor * weight;
                }
                else if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
                {
                    float validity = sampleShAr.x;
                    probeVolumeDiffuseLighting += lerp(float3(1, 0, 0), float3(0, 1, 0), validity) * weight;
                }
                else
#endif
                {
                    probeVolumeDiffuseLighting += sampleOutgoingRadiance * weight;
                }

                probeVolumeDiffuseLighting *= _IndirectLightingMultiplier.x;

                if (!ignoreWeight)
                    probeVolumeHierarchyWeight += weight;

            }
        }
    }

    builtinData.bakeDiffuseLighting = probeVolumeDiffuseLighting;
    builtinData.backBakeDiffuseLighting = noLight;
}
