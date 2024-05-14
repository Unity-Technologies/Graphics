//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESPROBEVOLUMES_CS_HLSL
#define SHADERVARIABLESPROBEVOLUMES_CS_HLSL
//
// UnityEngine.Rendering.APVLeakReductionMode:  static fields
//
#define APVLEAKREDUCTIONMODE_NONE (0)
#define APVLEAKREDUCTIONMODE_PERFORMANCE (1)
#define APVLEAKREDUCTIONMODE_QUALITY (2)
#define APVLEAKREDUCTIONMODE_VALIDITY_BASED (1)
#define APVLEAKREDUCTIONMODE_VALIDITY_AND_NORMAL_BASED (2)

//
// UnityEngine.Rendering.APVDefinitions:  static fields
//
#define PROBE_INDEX_CHUNK_SIZE (243)
#define PROBE_VALIDITY_THRESHOLD (0.05)
#define PROBE_MAX_REGION_COUNT (4)

// Generated from UnityEngine.Rendering.ShaderVariablesProbeVolumes
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariablesProbeVolumes, b6)
    float4 _Offset_LayerCount;
    float4 _MinLoadedCellInEntries_IndirectionEntryDim;
    float4 _MaxLoadedCellInEntries_RcpIndirectionEntryDim;
    float4 _PoolDim_MinBrickSize;
    float4 _RcpPoolDim_XY;
    float4 _MinEntryPos_Noise;
    uint4 _EntryCount_X_XY_LeakReduction;
    float4 _Biases_NormalizationClamp;
    float4 _FrameIndex_Weights;
    uint4 _ProbeVolumeLayerMask;
CBUFFER_END


#endif
