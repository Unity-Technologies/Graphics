//
// This file was automatically generated from Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Lighting/ShadowDefinition.cs.  Please don't edit by hand.
//

//
// UnityEngine.Experimental.ScriptableRenderLoop.ShadowType:  static fields
//
#define SHADOWTYPE_SPOT (0)
#define SHADOWTYPE_POINT (1)

// Generated from UnityEngine.Experimental.ScriptableRenderLoop.PunctualShadowData
// PackingRules = Exact
struct PunctualShadowData
{
	float4 shadowMatrix1;
	float4 shadowMatrix2;
	float4 shadowMatrix3;
	float4 shadowMatrix4;
	int shadowType;
	float3 unused;
};

//
// Accessors for UnityEngine.Experimental.ScriptableRenderLoop.PunctualShadowData
//
float4 GetShadowMatrix1(PunctualShadowData value)
{
	return value.shadowMatrix1;
}
float4 GetShadowMatrix2(PunctualShadowData value)
{
	return value.shadowMatrix2;
}
float4 GetShadowMatrix3(PunctualShadowData value)
{
	return value.shadowMatrix3;
}
float4 GetShadowMatrix4(PunctualShadowData value)
{
	return value.shadowMatrix4;
}
int GetShadowType(PunctualShadowData value)
{
	return value.shadowType;
}
float3 GetUnused(PunctualShadowData value)
{
	return value.unused;
}


