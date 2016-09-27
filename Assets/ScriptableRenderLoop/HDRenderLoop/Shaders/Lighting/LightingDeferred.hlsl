#ifndef UNITY_LIGHTING_DEFERRED_INCLUDED 
#define UNITY_LIGHTING_DEFERRED_INCLUDED

//-----------------------------------------------------------------------------
// Simple deferred loop architecture
//-----------------------------------------------------------------------------

StructuredBuffer<PunctualLightData> g_punctualLightList;
int g_punctualLightCount;

// Currently just a copy/paste of forward ligthing as it is just for validation
void DeferredLighting(float3 V, float3 positionWS, BSDFData material,
	out float4 diffuseLighting,
	out float4 specularLighting)
{
	diffuseLighting = float4(0.0, 0.0, 0.0, 0.0);
	specularLighting = float4(0.0, 0.0, 0.0, 0.0);

	for (int i = 0; i < g_punctualLightCount; ++i)
	{
		float4 localDiffuseLighting;
		float4 localSpecularLighting;
		EvaluateBSDF_Punctual(V, positionWS, g_punctualLightList[i], material, localDiffuseLighting, localSpecularLighting);
		diffuseLighting += localDiffuseLighting;
		specularLighting += localSpecularLighting;
	}
}

#endif