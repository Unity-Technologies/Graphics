#ifndef PROBE_VOLUME_LIGHT_LOOP_DEF
#define PROBE_VOLUME_LIGHT_LOOP_DEF

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeShaderVariables.hlsl"

#ifndef SCALARIZE_LIGHT_LOOP
// We perform scalarization only for forward rendering as for deferred loads will already be scalar since tiles will match waves and therefore all threads will read from the same tile.
// More info on scalarization: https://flashypixels.wordpress.com/2018/11/10/intro-to-gpu-scalarization-part-2-scalarize-all-the-lights/
#define SCALARIZE_LIGHT_LOOP (defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS) && !defined(LIGHTLOOP_DISABLE_TILE_AND_CLUSTER) && SHADERPASS == SHADERPASS_FORWARD)
#endif

#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS
// Cluster helper functions copied and lightly modified from ClusteredUtils.hlsl with ENABLE_DEPTH_TEXTURE_BACKPLANE undefined

// List of globals required for sampling probe volume light list data structure:
//
// g_fClustBase: Should be set same as same value used in lightloop clustered light list.
// g_iLog2NumClusters: Should be set same as value used in lightloop clustered light list.
// _NumTileClusteredX: Should be set same as value used in lightloop clustered light list.
// _NumTileClusteredY: Should be set same as value used in lightloop clustered light list.
// unity_StereoEyeIndex: Should be set same as value used in lightloop clustered light list.
// g_vProbeVolumesLayeredOffsetsBuffer renamed from g_vLayeredOffsetsBuffer
// g_vProbeVolumesLightListGlobal renamed from g_vLightListGlobal

uint ProbeVolumeGenerateLayeredOffsetBufferIndex(uint lightCategory, uint2 tileIndex, uint clusterIndex, uint numTilesX, uint numTilesY, int numClusters, uint eyeIndex)
{
    // Each eye is split into category, cluster, x, y
    // TODO: (Nick): Remove lightCatagory data stride as we only ever have one lightCatagory in the ProbeVolumeLightList: PROBE_VOLUME
    uint eyeOffset = eyeIndex * LIGHTCATEGORY_COUNT * numClusters * numTilesX * numTilesY;
    int lightOffset = ((lightCategory * numClusters + clusterIndex) * numTilesY + tileIndex.y) * numTilesX + tileIndex.x;

    return (eyeOffset + lightOffset);
}

// Cluster acessor functions copied and lightly modified from LightLoopDef.hlsl USE_CLUSTERED_LIGHTLIST section.

uint ProbeVolumeGetTileSize()
{
    return TILE_SIZE_CLUSTERED;
}

float ProbeVolumeLogBase(float x, float b)
{
    return log2(x) / log2(b);
}

int ProbeVolumeSnapToClusterIdxFlex(float z_in, float suggestedBase, bool logBasePerTile)
{
#if USE_LEFT_HAND_CAMERA_SPACE
    float z = z_in;
#else
    float z = -z_in;
#endif

    const int C = 1 << g_iLog2NumClusters;
    const float rangeFittedDistance = max(0, z - g_fNearPlane) / (g_fFarPlane - g_fNearPlane);
    return (int)clamp( ProbeVolumeLogBase( lerp(1.0, PositivePow(suggestedBase, (float) C), rangeFittedDistance), suggestedBase), 0.0, (float)(C - 1));
}

uint ProbeVolumeGetLightClusterIndex(uint2 tileIndex, float linearDepth)
{
    const bool k_IsLogBaseBufferEnabled = false;
    return ProbeVolumeSnapToClusterIdxFlex(linearDepth, g_fClustBase, k_IsLogBaseBufferEnabled);
}

void ProbeVolumeMaterialPassGetCountAndStartCluster(uint2 tileIndex, uint clusterIndex, uint lightCategory, out uint start, out uint lightCount)
{
    int nrClusters = (1 << g_iLog2NumClusters);

    const int idx = ProbeVolumeGenerateLayeredOffsetBufferIndex(lightCategory, tileIndex, clusterIndex, _NumTileClusteredX, _NumTileClusteredY, nrClusters, unity_StereoEyeIndex);

    uint dataPair = g_vProbeVolumesLayeredOffsetsBuffer[idx];
    start = dataPair & 0x7ffffff;
    lightCount = (dataPair >> 27) & 31;
}

void ProbeVolumeMaterialPassGetCountAndStartCluster(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    // Note: XR depends on unity_StereoEyeIndex already being defined,
    // which means ShaderVariables.hlsl needs to be defined ahead of this!

    uint2 tileIndex    = posInput.tileCoord;
    uint  clusterIndex = ProbeVolumeGetLightClusterIndex(tileIndex, posInput.linearDepth);

    ProbeVolumeMaterialPassGetCountAndStartCluster(tileIndex, clusterIndex, lightCategory, start, lightCount);
}

void ProbeVolumeMaterialPassGetCountAndStart(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    ProbeVolumeMaterialPassGetCountAndStartCluster(posInput, lightCategory, start, lightCount);
}

uint ProbeVolumeMaterialPassFetchIndex(uint lightStart, uint lightOffset)
{
    return g_vProbeVolumesLightListGlobal[lightStart + lightOffset];
}

#endif // endof SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS

void ProbeVolumeGetCountAndStart(PositionInputs posInput, out uint probeVolumeStart, out uint probeVolumeCount)
{
#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS
    // Access probe volume data from custom probe volume light list data structure.
    ProbeVolumeMaterialPassGetCountAndStart(posInput, LIGHTCATEGORY_PROBE_VOLUME, probeVolumeStart, probeVolumeCount);
#else // #if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_LIGHT_LOOP
    // Access probe volume data from standard lightloop light list data structure.
    GetCountAndStart(posInput, LIGHTCATEGORY_PROBE_VOLUME, probeVolumeStart, probeVolumeCount);
#endif

#else // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    probeVolumeCount = _ProbeVolumeCount;
    probeVolumeStart = 0;
#endif
}

void ProbeVolumeGetCountAndStartAndFastPath(PositionInputs posInput, out uint probeVolumeStart, out uint probeVolumeCount, out bool fastPath)
{
    fastPath = false;

#if SCALARIZE_LIGHT_LOOP
    // Fast path is when we all pixels in a wave is accessing same tile or cluster.
    uint probeVolumeStartFirstLane = WaveReadLaneFirst(probeVolumeStart);
    fastPath = WaveActiveAllTrue(probeVolumeStart == probeVolumeStartFirstLane);
    if (fastPath)
    {
        probeVolumeStart = probeVolumeStartFirstLane;
    }
#endif
}

uint ProbeVolumeFetchIndex(uint probeVolumeStart, uint probeVolumeOffset)
{
  #if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS
    // Access probe volume data from custom probe volume light list data structure.
    return ProbeVolumeMaterialPassFetchIndex(probeVolumeStart, probeVolumeOffset);
#else // #if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_LIGHT_LOOP
    // Access probe volume data from standard lightloop light list data structure.
    return FetchIndex(probeVolumeStart, probeVolumeOffset);
#endif
}

// This function scalarize an index accross all lanes. To be effecient it must be used in the context
// of the scalarization of a loop. It is to use with IsFastPath so it can optimize the number of
// element to load, which is optimal when all the lanes are contained into a tile.
uint ProbeVolumeScalarizeElementIndex(uint v_elementIdx, bool fastPath)
{
    uint s_elementIdx = v_elementIdx;
#if SCALARIZE_LIGHT_LOOP
    if (!fastPath)
    {
        // If we are not in fast path, v_elementIdx is not scalar, so we need to query the Min value across the wave.
        s_elementIdx = WaveActiveMin(v_elementIdx);
        // If WaveActiveMin returns 0xffffffff it means that all lanes are actually dead, so we can safely ignore the loop and move forward.
        // This could happen as an helper lane could reach this point, hence having a valid v_elementIdx, but their values will be ignored by the WaveActiveMin
        if (s_elementIdx == -1)
        {
            return -1;
        }
    }
    // Note that the WaveReadLaneFirst should not be needed, but the compiler might insist in putting the result in VGPR.
    // However, we are certain at this point that the index is scalar.
    s_elementIdx = WaveReadLaneFirst(s_elementIdx);
#endif
    return s_elementIdx;
}

bool ProbeVolumeIsAllWavesComplete(float weightHierarchy, int volumeBlendMode)
{
    // Probe volumes are sorted primarily by blend mode, and secondarily by size.
    // This means we will evaluate all Additive and Subtractive blending volumes first, and finally our Normal (over) blending volumes.
    // This allows us to early out if our weightHierarchy reaches 1.0, since we know we will only ever process more VOLUMEBLENDMODE_NORMAL volumes,
    // whos weight will always evaluate to zero.
#if defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS)
    if (WaveActiveMin(weightHierarchy) >= 1.0
#if SHADEROPTIONS_PROBE_VOLUMES_ADDITIVE_BLENDING
        && WaveActiveAllTrue(volumeBlendMode == VOLUMEBLENDMODE_NORMAL)
#endif
    )
    {
        return true;
    }
#endif

    return false;
}

#endif // endof PROBE_VOLUME_LIGHT_LOOP_DEF
