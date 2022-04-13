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
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_WorldTransformParams)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_RenderingLayer)
    UNITY_DOTS_INSTANCED_PROP(float4,   unity_LightData)
    UNITY_DOTS_INSTANCED_PROP(float2x4, unity_LightIndices)
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

// Note: Macros for unity_ObjectToWorld, unity_WorldToObject, unity_MatrixPreviousM and unity_MatrixPreviousMI are declared in UnityInstancing.hlsl
// because of some special handling
#define unity_LODFade               UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_LODFade)
#define unity_WorldTransformParams  UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_WorldTransformParams)
#define unity_RenderingLayer        UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_RenderingLayer)
#define unity_LightData             UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_LightData)
#define unity_LightIndices          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float2x4, Metadataunity_LightIndices)
#define unity_ProbesOcclusion       UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_ProbesOcclusion)
#define unity_SpecCube0_HDR         UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_SpecCube0_HDR)
#define unity_LightmapST            UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_LightmapST)
#define unity_LightmapIndex         UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_LightmapIndex)
#define unity_DynamicLightmapST     UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_DynamicLightmapST)
#define unity_SHAr                  UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_SHAr)
#define unity_SHAg                  UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_SHAg)
#define unity_SHAb                  UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_SHAb)
#define unity_SHBr                  UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_SHBr)
#define unity_SHBg                  UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_SHBg)
#define unity_SHBb                  UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_SHBb)
#define unity_SHC                   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4,   Metadataunity_SHC)
#endif

#endif
