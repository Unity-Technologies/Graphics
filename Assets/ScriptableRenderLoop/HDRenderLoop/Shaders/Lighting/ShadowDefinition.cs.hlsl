//
// This file was automatically generated from Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Lighting/ShadowDefinition.cs.  Please don't edit by hand.
//

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
	float4 worldToShadow0;
	float4 worldToShadow1;
	float4 worldToShadow2;
	float4 worldToShadow3;
	int shadowType;
	float3 unused;
};

//
// Accessors for UnityEngine.Experimental.ScriptableRenderLoop.PunctualShadowData
//
float4 GetWorldToShadow0(PunctualShadowData value)
{
	return value.worldToShadow0;
}
float4 GetWorldToShadow1(PunctualShadowData value)
{
	return value.worldToShadow1;
}
float4 GetWorldToShadow2(PunctualShadowData value)
{
	return value.worldToShadow2;
}
float4 GetWorldToShadow3(PunctualShadowData value)
{
	return value.worldToShadow3;
}
int GetShadowType(PunctualShadowData value)
{
	return value.shadowType;
}
float3 GetUnused(PunctualShadowData value)
{
	return value.unused;
}


