//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SURFACECACHEGISCREENRESOLVELOOKUPCONSTANTS_CS_HLSL
#define SURFACECACHEGISCREENRESOLVELOOKUPCONSTANTS_CS_HLSL
// Generated from UnityEngine.Rendering.Universal.SurfaceCacheGIScreenResolveLookupConstants
// PackingRules = Exact
CBUFFER_START(SurfaceCacheGIScreenResolveLookupConstants)
    float4x4 _ClipToWorldTransforms[2];
    float4 _VolumeTargetPos;
    uint _VolumeSpatialResolution;
    uint _VolumeCascadeCount;
    uint _SampleCount;
    int _PatchWarping;
    uint _DistanceFallback;
    float _VolumeVoxelMinSize;
    uint _ScreenResolveLookupPad0;
    uint _ScreenResolveLookupPad1;
CBUFFER_END


#endif
