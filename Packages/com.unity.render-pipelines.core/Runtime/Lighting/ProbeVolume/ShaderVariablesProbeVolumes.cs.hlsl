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
#define APVLEAKREDUCTIONMODE_VALIDITY_AND_NORMAL_BASED (2)

//
// UnityEngine.Rendering.APVDefinitions:  static fields
//
#define PROBE_INDEX_CHUNK_SIZE (243)

// Generated from UnityEngine.Rendering.ShaderVariablesProbeVolumes
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariablesProbeVolumes, b6)
    float4 _Offset_IndirectionEntryDim;
    float4 _Weight_MinLoadedCellInEntries;
    float4 _PoolDim_MinBrickSize;
    float4 _RcpPoolDim_XY;
    float4 _MinEntryPos_Noise;
    float4 _IndicesDim_FrameIndex;
    float4 _Biases_NormalizationClamp;
    float4 _LeakReduction_SkyOcclusion;
    float4 _MaxLoadedCellInEntries_Padding;
CBUFFER_END


#endif
