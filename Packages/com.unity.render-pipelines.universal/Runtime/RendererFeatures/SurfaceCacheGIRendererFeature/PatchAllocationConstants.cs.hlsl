//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SURFACECACHEGIPATCHALLOCATIONCONSTANTS_CS_HLSL
#define SURFACECACHEGIPATCHALLOCATIONCONSTANTS_CS_HLSL
// Generated from UnityEngine.Rendering.Universal.SurfaceCacheGIPatchAllocationConstants
// PackingRules = Exact
CBUFFER_START(SurfaceCacheGIPatchAllocationConstants)
    float4x4 _CurrentClipToWorldTransforms[2];
    float4x4 _PreviousClipToWorldTransforms[2];
    float4 _VolumeTargetPos;
    uint _FrameIdx;
    uint _StereoEyeIndex;
    uint _VolumeSpatialResolution;
    uint _VolumeCascadeCount;
    uint2 _LowResScreenSize;
    uint2 _FullResPixelOffset;
    uint _RingConfigOffset;
    int _PatchWarping;
    float _VolumeVoxelMinSize;
    uint _PatchAllocationPad0;
CBUFFER_END


#endif
