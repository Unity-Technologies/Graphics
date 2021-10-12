//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESPROBEVOLUMES_CS_HLSL
#define SHADERVARIABLESPROBEVOLUMES_CS_HLSL
// Generated from UnityEngine.Rendering.ShaderVariablesProbeVolumes
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariablesProbeVolumes, b5)
    float3 _CellIndicesDim;
    float _CellInMeters;
    float3 _PoolDim;
    float _ViewBias;
    float3 _MinCellPosition;
    float _PVSamplingNoise;
    float _CellInMinBricks;
    float _MinBrickSize;
    float _NormalBias;
	int _IndexChunkSize;
CBUFFER_END


#endif
