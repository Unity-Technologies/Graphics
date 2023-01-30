#ifndef UNIVERSAL_DOTS_INSTANCING_INCLUDED
#define UNIVERSAL_DOTS_INSTANCING_INCLUDED

#ifdef UNITY_DOTS_INSTANCING_ENABLED

#undef unity_ObjectToWorld
#undef unity_WorldToObject
#undef unity_MatrixPreviousM
#undef unity_MatrixPreviousMI
// TODO: This might not work correctly in all cases, double check!
UNITY_DOTS_INSTANCING_START(BuiltinPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_ObjectToWorld)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_WorldToObject)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_LODFade)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_RenderingLayer)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_SpecCube0_HDR)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_LightmapST)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_LightmapIndex)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_DynamicLightmapST)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_MatrixPreviousM)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_MatrixPreviousMI)
    UNITY_DOTS_INSTANCED_PROP(SH,       unity_SHCoefficients)
    UNITY_DOTS_INSTANCED_PROP(uint2,    unity_EntityId)
UNITY_DOTS_INSTANCING_END(BuiltinPropertyMetadata)

#define unity_LODFade               UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_LODFade)
#define unity_SpecCube0_HDR         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_CUSTOM_DEFAULT(float4, unity_SpecCube0_HDR, unity_DOTS_SpecCube0_HDR)
#define unity_LightmapST            UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_LightmapST)
#define unity_LightmapIndex         UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_LightmapIndex)
#define unity_DynamicLightmapST     UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_DynamicLightmapST)
#define unity_SHAr                  LoadDOTSInstancedData_SHAr()
#define unity_SHAg                  LoadDOTSInstancedData_SHAg()
#define unity_SHAb                  LoadDOTSInstancedData_SHAb()
#define unity_SHBr                  LoadDOTSInstancedData_SHBr()
#define unity_SHBg                  LoadDOTSInstancedData_SHBg()
#define unity_SHBb                  LoadDOTSInstancedData_SHBb()
#define unity_SHC                   LoadDOTSInstancedData_SHC()
#define unity_ProbesOcclusion       LoadDOTSInstancedData_ProbesOcclusion()
#define unity_LightData             LoadDOTSInstancedData_LightData()
#define unity_WorldTransformParams  LoadDOTSInstancedData_WorldTransformParams()
#define unity_RenderingLayer        LoadDOTSInstancedData_RenderingLayer()

#define UNITY_SETUP_DOTS_SH_COEFFS  SetupDOTSSHCoeffs(UNITY_DOTS_INSTANCED_METADATA_NAME(SH, unity_SHCoefficients))
#define UNITY_SETUP_DOTS_RENDER_BOUNDS  SetupDOTSRendererBounds(UNITY_DOTS_MATRIX_M)

// Not supported by BatchRendererGroup. Just define them as constants.
// ------------------------------------------------------------------------------
static const float2x4 unity_LightIndices = float2x4(0,0,0,0, 0,0,0,0);

static const float4 unity_SpecCube0_BoxMax = float4(1,1,1,1);
static const float4 unity_SpecCube0_BoxMin = float4(0,0,0,0);
static const float4 unity_SpecCube0_ProbePosition = float4(0,0,0,0);

static const float4 unity_SpecCube1_BoxMax = float4(1,1,1,1);
static const float4 unity_SpecCube1_BoxMin = float4(0,0,0,0);
static const float4 unity_SpecCube1_ProbePosition = float4(0,0,0,0);
static const float4 unity_SpecCube1_HDR = float4(0,0,0,0);

static const float4 unity_MotionVectorsParams = float4(0,1,0,1);
static const float4 unity_RendererBounds_Min = float4(0,0,0,0);
static const float4 unity_RendererBounds_Max = float4(0,0,0,0);

// Set up by BRG picking/selection code
int unity_SubmeshIndex;
#define unity_SelectionID UNITY_ACCESS_DOTS_INSTANCED_SELECTION_VALUE(unity_EntityId, unity_SubmeshIndex, _SelectionID)

#else

#define unity_SelectionID _SelectionID
#define UNITY_SETUP_DOTS_RENDER_BOUNDS

#endif

#endif
