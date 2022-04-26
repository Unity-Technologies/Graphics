//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESPROBEVOLUMES_CS_HLSL
#define SHADERVARIABLESPROBEVOLUMES_CS_HLSL
//
// UnityEngine.Rendering.APVLeakReductionMode:  static fields
//
#define APVLEAKREDUCTIONMODE_NONE (0)
#define APVLEAKREDUCTIONMODE_VALIDITY_AND_NORMAL_BASED (1)

// Generated from UnityEngine.Rendering.ShaderVariablesProbeVolumes
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariablesProbeVolumes, b5)
    float4 _PoolDim_CellInMeters;
    float4 _MinCellPos_Noise;
    float4 _IndicesDim_IndexChunkSize;
    float4 _Biases_CellInMinBrick_MinBrickSize;
    float4 _LeakReductionParams;
    float4 _Weight_MinLoadedCell;
    float4 _MaxLoadedCell_FrameIndex;
    float4 _NormalizationClamp_Padding12;
CBUFFER_END


#endif
