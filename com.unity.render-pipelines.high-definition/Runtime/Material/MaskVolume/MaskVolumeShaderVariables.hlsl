#ifndef MASK_VOLUME_SHADER_VARIABLES
#define MASK_VOLUME_SHADER_VARIABLES

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaskVolume/MaskVolumeRendering.cs.hlsl"

    // Evaluating mask volumes in the material pass requires a custom mask volume only light list.
    // Cannot reuse LightLoop g_vLightListGlobal and g_vLayeredOffsetsBuffer buffers, as they are in active use in forward rendered shader passes.
    StructuredBuffer<uint> g_vMaskVolumesLightListGlobal;
    StructuredBuffer<uint> g_vMaskVolumesLayeredOffsetsBuffer;

    StructuredBuffer<OrientedBBox> _MaskVolumeBounds;
    StructuredBuffer<MaskVolumeEngineData> _MaskVolumeDatas;

    TEXTURE3D(_MaskVolumeAtlasSH);

#endif // endof MASK_VOLUME_SHADER_VARIABLES
