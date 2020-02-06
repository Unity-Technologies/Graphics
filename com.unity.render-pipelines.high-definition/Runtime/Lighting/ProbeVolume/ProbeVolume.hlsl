#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolume.cs.hlsl"

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

void EvaluateProbeVolumes(PositionInputs posInput, BSDFData bsdfData, BuiltinData builtinData,
    out float3 probeVolumeDiffuseLighting)
{
    float probeVolumeHierarchyWeight = 0.0; // Max: 1.0

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
            if (probeVolumeHierarchyWeight < 1.0 || s_probeVolumeData.volumeBlendMode != VOLUMEBLENDMODE_NORMAL)
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

                    if (s_probeVolumeData.volumeBlendMode == VOLUMEBLENDMODE_ADDITIVE)
                        weight = fadeFactor;
                    else if (s_probeVolumeData.volumeBlendMode == VOLUMEBLENDMODE_SUBTRACTIVE)
                        weight = -fadeFactor;
                    else
                        // Alpha composite: weight = (1.0f - probeVolumeHierarchyWeight) * fadeFactor;
                        weight = probeVolumeHierarchyWeight * -fadeFactor + fadeFactor;

                    if (weight > 0.0 || s_probeVolumeData.volumeBlendMode != VOLUMEBLENDMODE_NORMAL)
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

                float3 sampleOutgoingRadiance = SHEvalLinearL0L1(bsdfData.normalWS, sampleShAr, sampleShAg, sampleShAb);

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
                probeVolumeHierarchyWeight = probeVolumeHierarchyWeight + weight;

            }
        }
    }

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_PROBE_VOLUME
        || _DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS
        || _DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
    {
        builtinData.bakeDiffuseLighting = 0.0;
        builtinData.backBakeDiffuseLighting = 0.0;
    }
    else
#endif
    {
        probeVolumeDiffuseLighting *= bsdfData.diffuseColor;
    }
}
