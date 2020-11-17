#ifndef PROBE_VOLUME_SHADER_VARIABLES
#define PROBE_VOLUME_SHADER_VARIABLES

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeLighting.cs.hlsl"

#if SHADEROPTIONS_ENABLE_PROBE_VOLUMES == 1
    StructuredBuffer<OrientedBBox> _ProbeVolumeBounds;
    StructuredBuffer<ProbeVolumeEngineData> _ProbeVolumeDatas;

    TEXTURE3D(_ProbeVolumeAtlasSH);
#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING == PROBEVOLUMESBILATERALFILTERINGMODES_OCTAHEDRAL_DEPTH
    TEXTURE2D(_ProbeVolumeAtlasOctahedralDepth);
#endif

#endif

#endif // endof PROBE_VOLUME_SHADER_VARIABLES
