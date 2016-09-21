#ifndef UNITY_LIGHTING_INCLUDED 
#define UNITY_LIGHTING_INCLUDED

#include "../Material/Material.hlsl"

//-----------------------------------------------------------------------------
// Simple forward loop architecture
//-----------------------------------------------------------------------------

StructuredBuffer<PunctualLightData> g_punctualLightData;
float g_lightCount;

// TODO: Think about how to apply Disney diffuse preconvolve on indirect diffuse => must be done during GBuffer layout! Else emissive will be fucked...
// That's mean we need to read DFG texture during Gbuffer...
void ForwardLighting(	float3 V, float3 positionWS, BSDFData material,
						out float4 diffuseLighting,
						out float4 specularLighting)
{
	diffuseLighting		= float4(0.0, 0.0, 0.0, 0.0);
	specularLighting	= float4(0.0, 0.0, 0.0, 0.0);

	for (uint i = 0; i < (uint)g_lightCount; ++i)
	{
		float4 localDiffuseLighting;
		float4 localSpecularLighting;
		EvaluateBSDF_Punctual(V, positionWS, g_punctualLightData[i], material, localDiffuseLighting, localSpecularLighting);
		diffuseLighting += localDiffuseLighting;
		specularLighting += localSpecularLighting;
	}

	/*
	for (int i = 0; i < 4; ++i)
	{
		float4 localDiffuseLighting;
		float4 localSpecularLighting;
		EvaluateBSDF_Area(V, positionWS, areaLightData[i], material, localDiffuseLighting, localSpecularLighting);
		diffuseLighting += localDiffuseLighting;
		specularLighting += localSpecularLighting;
	}
	*/
}


#endif