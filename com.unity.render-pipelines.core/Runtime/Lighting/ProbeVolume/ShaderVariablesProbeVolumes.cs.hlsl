//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESPROBEVOLUMES_CS_HLSL
#define SHADERVARIABLESPROBEVOLUMES_CS_HLSL
//
// UnityEngine.Rendering.APVLeakReductionMode:  static fields
//
#define APVLEAKREDUCTIONMODE_NONE (0)
#define APVLEAKREDUCTIONMODE_VALIDITY_BASED (1)
#define APVLEAKREDUCTIONMODE_NORMAL_BASED (2)
#define APVLEAKREDUCTIONMODE_VALIDITY_AND_NORMAL_BASED (3)

// Generated from UnityEngine.Rendering.ShaderVariablesProbeVolumes
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariablesProbeVolumes, b5)
    float3 _PoolDim;
    float _ViewBias;
    float3 _MinCellPosition;
    float _PVSamplingNoise;
    float3 _CellIndicesDim;
    float _CellInMeters;
    float4 _LeakReductionParams;
    float _CellInMinBricks;
    float _MinBrickSize;
    int _IndexChunkSize;
    float _NormalBias;
CBUFFER_END


#endif
