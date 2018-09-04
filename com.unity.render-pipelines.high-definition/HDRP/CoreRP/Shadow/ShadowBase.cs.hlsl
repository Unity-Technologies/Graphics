//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef SHADOWBASE_CS_HLSL
#define SHADOWBASE_CS_HLSL
//
// UnityEngine.Experimental.Rendering.GPUShadowType:  static fields
//
#define GPUSHADOWTYPE_POINT (0)
#define GPUSHADOWTYPE_SPOT (1)
#define GPUSHADOWTYPE_DIRECTIONAL (2)
#define GPUSHADOWTYPE_MAX (3)
#define GPUSHADOWTYPE_UNKNOWN (3)
#define GPUSHADOWTYPE_ALL (3)

//
// UnityEngine.Experimental.Rendering.GPUShadowAlgorithm:  static fields
//
#define GPUSHADOWALGORITHM_PCF_1TAP (0)
#define GPUSHADOWALGORITHM_PCF_9TAP (1)
#define GPUSHADOWALGORITHM_PCF_TENT_3X3 (2)
#define GPUSHADOWALGORITHM_PCF_TENT_5X5 (3)
#define GPUSHADOWALGORITHM_PCF_TENT_7X7 (4)
#define GPUSHADOWALGORITHM_VSM (8)
#define GPUSHADOWALGORITHM_EVSM_2 (16)
#define GPUSHADOWALGORITHM_EVSM_4 (17)
#define GPUSHADOWALGORITHM_MSM_HAM (24)
#define GPUSHADOWALGORITHM_MSM_HAUS (25)
#define GPUSHADOWALGORITHM_PCSS (32)
#define GPUSHADOWALGORITHM_CUSTOM (256)

// Generated from UnityEngine.Experimental.Rendering.ShadowData
// PackingRules = Exact
struct ShadowData
{
    float4 proj;
    float3 pos;
    float3 rot0;
    float3 rot1;
    float3 rot2;
    float4 scaleOffset;
    float4 textureSize;
    float4 texelSizeRcp;
    uint id;
    uint shadowType;
    uint payloadOffset;
    float slice;
    float4 viewBias;
    float4 normalBias;
    float edgeTolerance;
    float3 _pad;
    float4x4 shadowToWorld;
};

//
// Accessors for UnityEngine.Experimental.Rendering.ShadowData
//
float4 GetProj(ShadowData value)
{
    return value.proj;
}
float3 GetPos(ShadowData value)
{
    return value.pos;
}
float3 GetRot0(ShadowData value)
{
    return value.rot0;
}
float3 GetRot1(ShadowData value)
{
    return value.rot1;
}
float3 GetRot2(ShadowData value)
{
    return value.rot2;
}
float4 GetScaleOffset(ShadowData value)
{
    return value.scaleOffset;
}
float4 GetTextureSize(ShadowData value)
{
    return value.textureSize;
}
float4 GetTexelSizeRcp(ShadowData value)
{
    return value.texelSizeRcp;
}
uint GetId(ShadowData value)
{
    return value.id;
}
uint GetShadowType(ShadowData value)
{
    return value.shadowType;
}
uint GetPayloadOffset(ShadowData value)
{
    return value.payloadOffset;
}
float GetSlice(ShadowData value)
{
    return value.slice;
}
float4 GetViewBias(ShadowData value)
{
    return value.viewBias;
}
float4 GetNormalBias(ShadowData value)
{
    return value.normalBias;
}
float GetEdgeTolerance(ShadowData value)
{
    return value.edgeTolerance;
}
float3 Get_pad(ShadowData value)
{
    return value._pad;
}
float4x4 GetShadowToWorld(ShadowData value)
{
    return value.shadowToWorld;
}


#endif
