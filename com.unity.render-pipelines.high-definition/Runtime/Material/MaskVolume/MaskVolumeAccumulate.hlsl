// This file should only be included inside of MaskVolume.hlsl.
// There are no #ifndef HEADER guards to stop multiple inclusion, as this is simply used for code gen.

#ifndef SCALARIZE_LIGHT_LOOP
// We perform scalarization only for forward rendering as for deferred loads will already be scalar since tiles will match waves and therefore all threads will read from the same tile.
// More info on scalarization: https://flashypixels.wordpress.com/2018/11/10/intro-to-gpu-scalarization-part-2-scalarize-all-the-lights/
#define SCALARIZE_LIGHT_LOOP (defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS) && !defined(LIGHTLOOP_DISABLE_TILE_AND_CLUSTER) && SHADERPASS == SHADERPASS_FORWARD)
#endif

void MaskVolumeAccumulate(PositionInputs posInput, float3 normalWS, uint renderingLayers, out MaskVolumeData coefficients, inout float weightHierarchy)
{
    ZERO_INITIALIZE(MaskVolumeData, coefficients);
    
    bool fastPath = false;

    uint maskVolumeStart, maskVolumeCount;
    // Fetch first mask volume to provide the scene proxy for screen space computation
    MaskVolumeGetCountAndStart(posInput, maskVolumeStart, maskVolumeCount);

#if SCALARIZE_LIGHT_LOOP
    uint maskStartLane0;
    fastPath = IsFastPath(maskVolumeStart, maskStartLane0);

    if (fastPath)
    {
        maskVolumeStart = maskStartLane0;
    }
#endif

    // Scalarized loop, same rationale of the punctual light version
    uint v_maskVolumeListOffset = 0;
    uint v_maskVolumeIdx = maskVolumeStart;
    while (v_maskVolumeListOffset < maskVolumeCount)
    {
        v_maskVolumeIdx = MaskVolumeFetchIndex(maskVolumeStart, v_maskVolumeListOffset);
#if SCALARIZE_LIGHT_LOOP
        uint s_maskVolumeIdx = ScalarizeElementIndex(v_maskVolumeIdx, fastPath);
#else
        uint s_maskVolumeIdx = v_maskVolumeIdx;
#endif

        if (s_maskVolumeIdx == -1) { break; }

        // Scalar load.
        MaskVolumeEngineData s_maskVolumeData = _MaskVolumeDatas[s_maskVolumeIdx];
        OrientedBBox s_maskVolumeBounds = _MaskVolumeBounds[s_maskVolumeIdx];

        if (MaskVolumeIsAllWavesComplete(weightHierarchy)) { break; }

        // If current scalar and vector light index match, we process the light. The v_maskVolumeListOffset for current thread is increased.
        // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
        // end up with a unique v_envLightIdx value that is smaller than s_envLightIdx hence being stuck in a loop. All the active lanes will not have this problem.
        if (s_maskVolumeIdx >= v_maskVolumeIdx)
        {
            v_maskVolumeListOffset++;

            if (weightHierarchy >= 1.0) { continue; }

            if (!IsMatchingLightLayer(s_maskVolumeData.lightLayers, renderingLayers)) { continue; }

            float weightCurrent = 0.0;
            {
                float3x3 obbFrame;
                float3 obbExtents;
                float3 obbCenter;
                MaskVolumeComputeOBBBoundsToFrame(s_maskVolumeBounds, obbFrame, obbExtents, obbCenter);
                
                // Note: When normal bias is > 0, bounds using in tile / cluster assignment are conservatively dilated CPU side to handle worst case normal bias.
                float3 samplePositionWS = normalWS * s_maskVolumeData.normalBiasWS + posInput.positionWS;

                float3 maskVolumeTexel3D;
                MaskVolumeComputeTexel3DAndWeight(
                    weightHierarchy,
                    s_maskVolumeData,
                    obbFrame,
                    obbExtents,
                    obbCenter,
                    samplePositionWS,
                    posInput.linearDepth,
                    maskVolumeTexel3D,
                    weightCurrent
                );

                float3 maskVolumeAtlasUVW = maskVolumeTexel3D * s_maskVolumeData.resolutionInverse * s_maskVolumeData.scale + s_maskVolumeData.bias;

                MaskVolumeSampleAccumulate(maskVolumeAtlasUVW, weightCurrent, coefficients);
            }

            weightHierarchy += weightCurrent;
        }
    }
}
