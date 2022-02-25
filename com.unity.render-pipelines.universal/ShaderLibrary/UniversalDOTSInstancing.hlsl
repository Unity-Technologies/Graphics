#ifndef UNIVERSAL_DOTS_INSTANCING_INCLUDED
#define UNIVERSAL_DOTS_INSTANCING_INCLUDED

#ifdef UNITY_DOTS_INSTANCING_ENABLED

#define DEBUG_B_H4 float4
#define DEBUG_B_H  float

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
    UNITY_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_ProbesOcclusion)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_SpecCube0_HDR)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_LightmapST)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_LightmapIndex)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_DynamicLightmapST)
    UNITY_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHAr)
    UNITY_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHAg)
    UNITY_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHAb)
    UNITY_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHBr)
    UNITY_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHBg)
    UNITY_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHBb)
    UNITY_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHC)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_MatrixPreviousM)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_MatrixPreviousMI)
UNITY_DOTS_INSTANCING_END(BuiltinPropertyMetadata)

#define unity_LODFade               UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_LODFade)
#define unity_ProbesOcclusion       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_SECONDARY_DEFAULT(DEBUG_B_H4,   unity_ProbesOcclusion, unity_DOTS_ProbesOcclusion_Offset)
#define unity_SpecCube0_HDR         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_SECONDARY_DEFAULT(float4,   unity_SpecCube0_HDR, unity_DOTS_SpecCube0_HDR_Offset)
#define unity_LightmapST            UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_LightmapST)
#define unity_LightmapIndex         UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_LightmapIndex)
#define unity_DynamicLightmapST     UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_DynamicLightmapST)
#if 0
#define unity_SHAr                  unity_DOTS_SHAr
#define unity_SHAg                  unity_DOTS_SHAg
#define unity_SHAb                  unity_DOTS_SHAb
#define unity_SHBr                  unity_DOTS_SHBr
#define unity_SHBg                  unity_DOTS_SHBg
#define unity_SHBb                  unity_DOTS_SHBb
#define unity_SHC                   unity_DOTS_SHC
#else
#define unity_SHAr                  LoadDOTSInstancedData_DebugSH() // UNITY_ACCESS_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHAr)
#define unity_SHAg                  LoadDOTSInstancedData_DebugSH() // UNITY_ACCESS_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHAg)
#define unity_SHAb                  LoadDOTSInstancedData_DebugSH() // UNITY_ACCESS_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHAb)
#define unity_SHBr                  LoadDOTSInstancedData_DebugSH() // UNITY_ACCESS_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHBr)
#define unity_SHBg                  LoadDOTSInstancedData_DebugSH() // UNITY_ACCESS_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHBg)
#define unity_SHBb                  LoadDOTSInstancedData_DebugSH() // UNITY_ACCESS_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHBb)
#define unity_SHC                   LoadDOTSInstancedData_DebugSH() // UNITY_ACCESS_DOTS_INSTANCED_PROP(DEBUG_B_H4,   unity_SHC)
#endif
#define unity_LightData             LoadDOTSInstancedData_LightData()
#define unity_WorldTransformParams  LoadDOTSInstancedData_WorldTransformParams()
#define unity_RenderingLayer        LoadDOTSInstancedData_RenderingLayer()

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

#endif

#endif
