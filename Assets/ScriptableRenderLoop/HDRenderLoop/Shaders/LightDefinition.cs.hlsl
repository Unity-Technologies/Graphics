//
// This file was automatically generated from Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/LightDefinition.cs.  Please don't edit by hand.
//

// Generated from UnityEngine.ScriptableRenderLoop.PunctualLightData
// PackingRules = Exact
struct PunctualLightData
{
	float3 positionWS;
	float invSqrAttenuationRadius;
	float3 color;
	float useDistanceAttenuation;
	float3 forward;
	float diffuseScale;
	float3 up;
	float specularScale;
	float3 right;
	float shadowDimmer;
	float angleScale;
	float angleOffset;
	float2 unused2;
};

// Generated from UnityEngine.ScriptableRenderLoop.AreaLightData
// PackingRules = Exact
struct AreaLightData
{
	float3 positionWS;
};

// Generated from UnityEngine.ScriptableRenderLoop.EnvLightData
// PackingRules = Exact
struct EnvLightData
{
	float3 positionWS;
};

// Generated from UnityEngine.ScriptableRenderLoop.PlanarLightData
// PackingRules = Exact
struct PlanarLightData
{
	float3 positionWS;
};

//
// Accessors for UnityEngine.ScriptableRenderLoop.PunctualLightData
//
float3 GetPositionWS(PunctualLightData value)
{
	return value.positionWS;
}
float GetInvSqrAttenuationRadius(PunctualLightData value)
{
	return value.invSqrAttenuationRadius;
}
float3 GetColor(PunctualLightData value)
{
	return value.color;
}
float GetUseDistanceAttenuation(PunctualLightData value)
{
	return value.useDistanceAttenuation;
}
float3 GetForward(PunctualLightData value)
{
	return value.forward;
}
float GetDiffuseScale(PunctualLightData value)
{
	return value.diffuseScale;
}
float3 GetUp(PunctualLightData value)
{
	return value.up;
}
float GetSpecularScale(PunctualLightData value)
{
	return value.specularScale;
}
float3 GetRight(PunctualLightData value)
{
	return value.right;
}
float GetShadowDimmer(PunctualLightData value)
{
	return value.shadowDimmer;
}
float GetAngleScale(PunctualLightData value)
{
	return value.angleScale;
}
float GetAngleOffset(PunctualLightData value)
{
	return value.angleOffset;
}
float2 GetUnused2(PunctualLightData value)
{
	return value.unused2;
}

//
// Accessors for UnityEngine.ScriptableRenderLoop.AreaLightData
//
float3 GetPositionWS(AreaLightData value)
{
	return value.positionWS;
}

//
// Accessors for UnityEngine.ScriptableRenderLoop.EnvLightData
//
float3 GetPositionWS(EnvLightData value)
{
	return value.positionWS;
}

//
// Accessors for UnityEngine.ScriptableRenderLoop.PlanarLightData
//
float3 GetPositionWS(PlanarLightData value)
{
	return value.positionWS;
}


