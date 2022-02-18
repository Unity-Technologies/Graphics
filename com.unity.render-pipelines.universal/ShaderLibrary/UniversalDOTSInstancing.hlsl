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
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_ProbesOcclusion)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_SpecCube0_HDR)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_LightmapST)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_LightmapIndex)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_DynamicLightmapST)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_SHAr)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_SHAg)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_SHAb)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_SHBr)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_SHBg)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_SHBb)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_SHC)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_MatrixPreviousM)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_MatrixPreviousMI)
UNITY_DOTS_INSTANCING_END(BuiltinPropertyMetadata)

#define unity_LODFade               UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_LODFade)
#define unity_ProbesOcclusion       UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_ProbesOcclusion)
#define unity_SpecCube0_HDR         UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_SpecCube0_HDR)
#define unity_LightmapST            UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_LightmapST)
#define unity_LightmapIndex         UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_LightmapIndex)
#define unity_DynamicLightmapST     UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_DynamicLightmapST)
#define unity_SHAr                  UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_SHAr)
#define unity_SHAg                  UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_SHAg)
#define unity_SHAb                  UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_SHAb)
#define unity_SHBr                  UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_SHBr)
#define unity_SHBg                  UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_SHBg)
#define unity_SHBb                  UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_SHBb)
#define unity_SHC                   UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_SHC)
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
