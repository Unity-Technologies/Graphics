//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESPROBEVOLUMES_CS_HLSL
#define SHADERVARIABLESPROBEVOLUMES_CS_HLSL
// Generated from UnityEngine.Rendering.ShaderVariablesProbeVolumes
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariablesProbeVolumes, b5)
    float4x4 _WStoRS;
    float3 _IndexDim;
    float _NormalBias;
    float3 _PoolDim;
    float _ViewBias;
    float _PVSamplingNoise;
    float3 _MinCellPosition;
    float3 _CellIndicesDim;
    float _CellInMeters;
    float _CellInMinBricks;
    float _MinBrickSize;
    float2 pad0;
CBUFFER_END


#endif
