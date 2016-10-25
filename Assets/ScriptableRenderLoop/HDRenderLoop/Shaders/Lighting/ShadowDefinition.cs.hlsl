//
// This file was automatically generated from Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Lighting/ShadowDefinition.cs.  Please don't edit by hand.
//

#ifndef SHADOWDEFINITION_CS_HLSL
#define SHADOWDEFINITION_CS_HLSL
//
// UnityEngine.Experimental.ScriptableRenderLoop.ShadowType:  static fields
//
#define SHADOWTYPE_SPOT (0)
#define SHADOWTYPE_DIRECTIONAL (1)
#define SHADOWTYPE_POINT (2)

// Generated from UnityEngine.Experimental.ScriptableRenderLoop.PunctualShadowData
// PackingRules = Exact
struct PunctualShadowData
{
	float4x4 worldToShadow;
	int shadowType;
	float bias;
	float2 unused;
};

//
// Accessors for UnityEngine.Experimental.ScriptableRenderLoop.PunctualShadowData
//
float4x4 GetWorldToShadow(PunctualShadowData value)
{
	return value.worldToShadow;
}
int GetShadowType(PunctualShadowData value)
{
	return value.shadowType;
}
float GetBias(PunctualShadowData value)
{
	return value.bias;
}
float2 GetUnused(PunctualShadowData value)
{
	return value.unused;
}


#endif
