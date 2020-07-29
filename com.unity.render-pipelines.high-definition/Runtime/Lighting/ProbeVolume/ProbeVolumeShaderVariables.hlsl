#ifndef PROBE_VOLUME_SHADER_VARIABLES
#define PROBE_VOLUME_SHADER_VARIABLES

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeLighting.cs.hlsl"

#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS
    // Evaluating probe volumes in the material pass requires a custom probe volume only light list.
    // Cannot reuse LightLoop g_vLightListGlobal and g_vLayeredOffsetsBuffer buffers, as they are in active use in forward rendered shader passes.
    StructuredBuffer<uint> g_vProbeVolumesLightListGlobal;
    StructuredBuffer<uint> g_vProbeVolumesLayeredOffsetsBuffer;
#endif
#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE != PROBEVOLUMESEVALUATIONMODES_DISABLED
    StructuredBuffer<OrientedBBox> _ProbeVolumeBounds;
    StructuredBuffer<ProbeVolumeEngineData> _ProbeVolumeDatas;

    TEXTURE3D(_ProbeVolumeAtlasSH);
#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING == PROBEVOLUMESBILATERALFILTERINGMODES_OCTAHEDRAL_DEPTH
    TEXTURE2D(_ProbeVolumeAtlasOctahedralDepth);
#endif

#endif

#endif // endof PROBE_VOLUME_SHADER_VARIABLES
