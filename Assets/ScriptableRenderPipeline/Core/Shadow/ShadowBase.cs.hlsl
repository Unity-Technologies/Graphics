//
// This file was automatically generated from Assets/ScriptableRenderPipeline/Core/Shadow/ShadowBase.cs.  Please don't edit by hand.
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
#define GPUSHADOWALGORITHM_CUSTOM (256)

// Generated from UnityEngine.Experimental.Rendering.ShadowData
// PackingRules = Exact
struct ShadowData
{
    float4x4 worldToShadow;
    float4 scaleOffset;
    float4 texelSizeRcp;
    uint id;
    uint shadowType;
    uint payloadOffset;
    float bias;
    float normalBias;
};

//
// Accessors for UnityEngine.Experimental.Rendering.ShadowData
//
float4x4 GetWorldToShadow(ShadowData value)
{
	return value.worldToShadow;
}
float4 GetScaleOffset(ShadowData value)
{
	return value.scaleOffset;
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
float GetBias(ShadowData value)
{
	return value.bias;
}
float GetNormalBias(ShadowData value)
{
	return value.normalBias;
}


#endif
