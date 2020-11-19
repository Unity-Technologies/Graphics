// This file should only be included inside of ProbeVolume.hlsl.
// There are no #ifndef HEADER guards to stop multiple inclusion, as this is simply used for code gen.

#ifndef SCALARIZE_LIGHT_LOOP
// We perform scalarization only for forward rendering as for deferred loads will already be scalar since tiles will match waves and therefore all threads will read from the same tile.
// More info on scalarization: https://flashypixels.wordpress.com/2018/11/10/intro-to-gpu-scalarization-part-2-scalarize-all-the-lights/
#define SCALARIZE_LIGHT_LOOP (defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS) && !defined(LIGHTLOOP_DISABLE_TILE_AND_CLUSTER) && SHADERPASS == SHADERPASS_FORWARD)
#endif


#ifndef PROBE_VOLUMES_ACCUMULATE_MODE
    #error "PROBE_VOLUMES_ACCUMULATE_MODE must be defined as 0, 1, or 2 before including ProbeVolumeAccumulate.hlsl. 0 triggers generation of SH0 variant, 1 triggers generation of SH1 variant, and 2 triggers generation of SH2 variant.";
#endif

#if (PROBE_VOLUMES_ACCUMULATE_MODE < 0) || (PROBE_VOLUMES_ACCUMULATE_MODE > 2)
    #error "PROBE_VOLUMES_ACCUMULATE_MODE must be defined as 0, 1, or 2 before including ProbeVolumeAccumulate.hlsl. 0 triggers generation of SH0 variant, 1 triggers generation of SH1 variant, and 2 triggers generation of SH2 variant.";
#endif

#if PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L0
    void ProbeVolumeAccumulateSphericalHarmonicsL0(
#elif PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
    void ProbeVolumeAccumulateSphericalHarmonicsL1(
#elif PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
    void ProbeVolumeAccumulateSphericalHarmonicsL2(
#endif
        PositionInputs posInput, float3 normalWS, uint renderingLayers,
#if PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L0
        out ProbeVolumeSphericalHarmonicsL0 coefficients,
#elif PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
        out ProbeVolumeSphericalHarmonicsL1 coefficients,
#elif PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
        out ProbeVolumeSphericalHarmonicsL2 coefficients,
#endif
        inout float weightHierarchy)
{

#if PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L0
        ZERO_INITIALIZE(ProbeVolumeSphericalHarmonicsL0, coefficients);
#elif PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
        ZERO_INITIALIZE(ProbeVolumeSphericalHarmonicsL1, coefficients);
#elif PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
        ZERO_INITIALIZE(ProbeVolumeSphericalHarmonicsL2, coefficients);
#endif


#if !SHADEROPTIONS_PROBE_VOLUMES_ADDITIVE_BLENDING
    if (weightHierarchy >= 1.0) { return; }
#endif

    bool fastPath = false;


    uint probeVolumeStart, probeVolumeCount;
    // Fetch first probe volume to provide the scene proxy for screen space computation
    ProbeVolumeGetCountAndStart(posInput, probeVolumeStart, probeVolumeCount);

#if SCALARIZE_LIGHT_LOOP
    uint probeStartLane0;
    fastPath = IsFastPath(probeVolumeStart, probeStartLane0);

    if (fastPath)
    {
        probeVolumeStart = probeStartLane0;
    }
#endif

    // Scalarized loop, same rationale of the punctual light version
    uint v_probeVolumeListOffset = 0;
    uint v_probeVolumeIdx = probeVolumeStart;
    while (v_probeVolumeListOffset < probeVolumeCount)
    {
        v_probeVolumeIdx = ProbeVolumeFetchIndex(probeVolumeStart, v_probeVolumeListOffset);
#if SCALARIZE_LIGHT_LOOP
        uint s_probeVolumeIdx = ScalarizeElementIndex(v_probeVolumeIdx, fastPath);
#else
        uint s_probeVolumeIdx = v_probeVolumeIdx;
#endif

        if (s_probeVolumeIdx == -1) { break; }

        // Scalar load.
        ProbeVolumeEngineData s_probeVolumeData = _ProbeVolumeDatas[s_probeVolumeIdx];
        OrientedBBox s_probeVolumeBounds = _ProbeVolumeBounds[s_probeVolumeIdx];

        if (ProbeVolumeIsAllWavesComplete(weightHierarchy, s_probeVolumeData.volumeBlendMode)) { break; }

        // If current scalar and vector light index match, we process the light. The v_probeVolumeListOffset for current thread is increased.
        // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
        // end up with a unique v_envLightIdx value that is smaller than s_envLightIdx hence being stuck in a loop. All the active lanes will not have this problem.
        if (s_probeVolumeIdx >= v_probeVolumeIdx)
        {
            v_probeVolumeListOffset++;

#if SHADEROPTIONS_PROBE_VOLUMES_ADDITIVE_BLENDING
            bool isWeightAccumulated = s_probeVolumeData.volumeBlendMode == VOLUMEBLENDMODE_NORMAL;
#else
            const bool isWeightAccumulated = true;
#endif

            if (weightHierarchy >= 1.0 && isWeightAccumulated) { continue; }

            if (!IsMatchingLightLayer(s_probeVolumeData.lightLayers, renderingLayers)) { continue; }

            float weightCurrent = 0.0;
            {
                float3x3 obbFrame;
                float3 obbExtents;
                float3 obbCenter;
                ProbeVolumeComputeOBBBoundsToFrame(s_probeVolumeBounds, obbFrame, obbExtents, obbCenter);

                // Note: When normal bias is > 0, bounds using in tile / cluster assignment are conservatively dilated CPU side to handle worst case normal bias.
                float3 samplePositionWS = normalWS * s_probeVolumeData.normalBiasWS + posInput.positionWS;

                float3 probeVolumeTexel3D;
                ProbeVolumeComputeTexel3DAndWeight(
                    weightHierarchy,
                    s_probeVolumeData,
                    obbFrame,
                    obbExtents,
                    obbCenter,
                    samplePositionWS,
                    posInput.linearDepth,
                    probeVolumeTexel3D,
                    weightCurrent
                );

                probeVolumeTexel3D = ProbeVolumeComputeTexel3DFromBilateralFilter(
                    probeVolumeTexel3D,
                    s_probeVolumeData,
                    posInput.positionWS, // unbiased
                    samplePositionWS, // biased
                    normalWS,
                    obbFrame,
                    obbExtents,
                    obbCenter
                );
                float3 probeVolumeAtlasUVW = probeVolumeTexel3D * s_probeVolumeData.resolutionInverse * s_probeVolumeData.scale + s_probeVolumeData.bias;

#ifdef DEBUG_DISPLAY
                if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS)
                {
                    // Pack debug color into SH data so that we can access it later for our debug mode.
                    coefficients.data[0].xyz += s_probeVolumeData.debugColor * weightCurrent;
                }
                else if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
                {
                    float validity = ProbeVolumeSampleValidity(probeVolumeAtlasUVW);

                    // Pack validity into SH data so that we can access it later for our debug mode.
                    coefficients.data[0].x += validity * weightCurrent;
                }
                else
#endif
                {
#if PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L0
                    ProbeVolumeSampleAccumulateSphericalHarmonicsL0(probeVolumeAtlasUVW, weightCurrent, coefficients);
#elif PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
                    ProbeVolumeSampleAccumulateSphericalHarmonicsL1(probeVolumeAtlasUVW, weightCurrent, coefficients);
#elif PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
                    ProbeVolumeSampleAccumulateSphericalHarmonicsL2(probeVolumeAtlasUVW, weightCurrent, coefficients);
#endif
                }
            }

            if (isWeightAccumulated)
                weightHierarchy += weightCurrent;
        }
    }

#if PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L0
    ProbeVolumeSwizzleAndNormalizeSphericalHarmonicsL0(coefficients);
#elif PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
    ProbeVolumeSwizzleAndNormalizeSphericalHarmonicsL1(coefficients);
#elif PROBE_VOLUMES_ACCUMULATE_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
    ProbeVolumeSwizzleAndNormalizeSphericalHarmonicsL2(coefficients);
#endif
}
