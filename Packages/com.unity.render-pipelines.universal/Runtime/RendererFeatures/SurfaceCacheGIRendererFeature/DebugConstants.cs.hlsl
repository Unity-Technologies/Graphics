//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SURFACECACHEGIDEBUGCONSTANTS_CS_HLSL
#define SURFACECACHEGIDEBUGCONSTANTS_CS_HLSL
// Generated from UnityEngine.Rendering.Universal.SurfaceCacheGIDebugConstants
// PackingRules = Exact
CBUFFER_START(SurfaceCacheGIDebugConstants)
    float4x4 _ClipToWorldTransforms[2];
    float4 _VolumeTargetPos;
    uint _RingConfigOffset;
    uint _VolumeSpatialResolution;
    uint _VolumeCascadeCount;
    uint _ViewMode;
    uint _ShowSamplePosition;
    uint _FrameIdx;
    int _PatchWarping;
    float _MaterialAtlasTexelSize;
    uint _TileOffsetY;
    uint _TileHeight;
    uint _DummyBlackbox;
    float _VolumeVoxelMinSize;
CBUFFER_END


#endif
