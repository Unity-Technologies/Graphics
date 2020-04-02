#ifndef PROBE_VOLUME_LIGHT_LOOP_DEF
#define PROBE_VOLUME_LIGHT_LOOP_DEF

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeShaderVariables.hlsl"

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

void ProbeVolumeGetCountAndStartCluster(uint2 tileIndex, uint clusterIndex, uint lightCategory, out uint start, out uint lightCount)
{
    int nrClusters = (1 << g_iLog2NumClusters);

    const int idx = ProbeVolumeGenerateLayeredOffsetBufferIndex(lightCategory, tileIndex, clusterIndex, _NumTileClusteredX, _NumTileClusteredY, nrClusters, unity_StereoEyeIndex);

    uint dataPair = g_vProbeVolumesLayeredOffsetsBuffer[idx];
    start = dataPair & 0x7ffffff;
    lightCount = (dataPair >> 27) & 31;
}

void ProbeVolumeGetCountAndStartCluster(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    // Note: XR depends on unity_StereoEyeIndex already being defined,
    // which means ShaderVariables.hlsl needs to be defined ahead of this!

    uint2 tileIndex    = posInput.tileCoord;
    uint  clusterIndex = ProbeVolumeGetLightClusterIndex(tileIndex, posInput.linearDepth);

    ProbeVolumeGetCountAndStartCluster(tileIndex, clusterIndex, lightCategory, start, lightCount);
}

void ProbeVolumeGetCountAndStart(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    ProbeVolumeGetCountAndStartCluster(posInput, lightCategory, start, lightCount);
}

uint ProbeVolumeFetchIndex(uint lightStart, uint lightOffset)
{
    return g_vProbeVolumesLightListGlobal[lightStart + lightOffset];
}

#endif // endof SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS

#endif // endof PROBE_VOLUME_LIGHT_LOOP_DEF
