#ifndef UNITY_LIGHTING_DEFINITION_INCLUDED 
#define UNITY_LIGHTING_DEFINITION_INCLUDED

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------

struct PunctualLightData
{
	float3	positionWS;
	float	invSqrAttenuationRadius;
	float3	color;
	float	unused;

	float3	forward;
	float	diffuseScale;

	float3	up;
	float	specularScale;

	float3	right;
	float	shadowDimmer;

	float	angleScale;
	float	angleOffset;
	float2	unused2;
};

struct AreaLightData
{
	float3	positionWS;
};

struct EnvLightData
{
	float3	positionWS;
};

struct PlanarLightData
{
	float3	positionWS;
};

#endif // UNITY_LIGHTING_DEFINITION_INCLUDED