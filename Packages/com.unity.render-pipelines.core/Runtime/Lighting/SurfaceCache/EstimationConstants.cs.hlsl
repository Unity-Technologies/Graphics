//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SURFACECACHEESTIMATIONCONSTANTS_CS_HLSL
#define SURFACECACHEESTIMATIONCONSTANTS_CS_HLSL
// Generated from UnityEngine.Rendering.SurfaceCacheEstimationConstants
// PackingRules = Exact
CBUFFER_START(SurfaceCacheEstimationConstants)
    float4 _VolumeTargetPos;
    float4 _DirectionalLightDirection;
    float4 _DirectionalLightIntensity;
    uint _PunctualLightCount;
    uint _FrameIdx;
    uint _VolumeSpatialResolution;
    uint _VolumeCascadeCount;
    uint _MultiBounce;
    uint _SampleCount;
    uint _WarmUpSampleMultiplier;
    uint _RingConfigOffset;
    int _PatchWarping;
    float _AlbedoBoost;
    float _EnvironmentIntensityMultiplier;
    float _EmissiveTriangleIntensityMultiplier;
    uint _PunctualLightSampleCount;
    uint _BouncePatchAllocation;
    float _VolumeVoxelMinSize;
    float _ShortHysteresis;
    float _MaterialAtlasTexelSize;
    uint _PunctualLightCandidateCount;
    float _RayTracingNormalBias;
    uint _EstimationPad0;
CBUFFER_END


#endif
