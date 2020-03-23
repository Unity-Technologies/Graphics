#ifndef PROBE_VOLUME_SHADER_VARIABLES
#define PROBE_VOLUME_SHADER_VARIABLES

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeLighting.cs.hlsl"

#if defined(SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE)
#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIALPASS
    // Evaluating probe volumes in the material pass requires a custom probe volume only light list.
    // Cannot reuse LightLoop g_vLightListGlobal and g_vLayeredOffsetsBuffer buffers, as they are in active use in forward rendered shader passes.
    StructuredBuffer<uint> g_vProbeVolumesLightListGlobal;
    StructuredBuffer<uint> g_vProbeVolumesLayeredOffsetsBuffer;
#endif
#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE != PROBEVOLUMESEVALUATIONMODES_DISABLED
    StructuredBuffer<OrientedBBox> _ProbeVolumeBounds;
    StructuredBuffer<ProbeVolumeEngineData> _ProbeVolumeDatas;

    TEXTURE3D(_ProbeVolumeAtlasSH);
    float4 _ProbeVolumeAtlasResolutionAndSliceCount;
    float4 _ProbeVolumeAtlasResolutionAndSliceCountInverse;
    TEXTURE2D(_ProbeVolumeAtlasOctahedralDepth);
    float4 _ProbeVolumeAtlasOctahedralDepthResolutionAndInverse;
    int _ProbeVolumeLeakMitigationMode;
    float _ProbeVolumeNormalBiasWS;
    float _ProbeVolumeBilateralFilterWeightMin;
    float _ProbeVolumeBilateralFilterWeight;
#endif
#endif // endof defined(SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE)

#endif // endof PROBE_VOLUME_SHADER_VARIABLES
