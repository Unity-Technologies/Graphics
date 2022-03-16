#ifndef BUILTIN_DOTS_INSTANCING_INCLUDED
#define BUILTIN_DOTS_INSTANCING_INCLUDED

#ifdef UNITY_DOTS_INSTANCING_ENABLED

#undef unity_ObjectToWorld
#undef unity_WorldToObject
UNITY_DOTS_INSTANCING_START(BuiltinPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_ObjectToWorld)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_WorldToObject)
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
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_ProbesOcclusion)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_MatrixPreviousM)
    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_MatrixPreviousMI)
    UNITY_DOTS_INSTANCED_PROP(uint2,    unity_EntityId)
UNITY_DOTS_INSTANCING_END(BuiltinPropertyMetadata)

// Note: Macros for unity_ObjectToWorld and unity_WorldToObject are declared in UnityInstancing.hlsl
// because of some special handling
#define unity_LODFade               LoadDOTSInstancedData_LODFade()
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
#define unity_ProbesOcclusion       UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_ProbesOcclusion)

#define unity_RenderingLayer        LoadDOTSInstancedData_RenderingLayer()
#define unity_MotionVectorsParams   LoadDOTSInstancedData_MotionVectorsParams()
#define unity_WorldTransformParams  LoadDOTSInstancedData_WorldTransformParams()
#endif

#endif
