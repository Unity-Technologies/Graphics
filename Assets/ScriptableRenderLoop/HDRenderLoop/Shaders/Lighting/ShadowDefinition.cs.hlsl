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
	float4x4 worldToShadow;
	int shadowType;
	float3 unused;
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
float3 GetUnused(PunctualShadowData value)
{
	return value.unused;
}


