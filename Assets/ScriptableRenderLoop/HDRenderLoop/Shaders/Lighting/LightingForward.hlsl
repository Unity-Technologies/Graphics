#ifndef UNITY_LIGHTING_FORWARD_INCLUDED
#define UNITY_LIGHTING_FORWARD_INCLUDED

//-----------------------------------------------------------------------------
// Simple forward loop architecture
//-----------------------------------------------------------------------------

StructuredBuffer<PunctualLightData> _PunctualLightList;
int _PunctualLightCount;

UNITY_DECLARE_ENV(_EnvTextures);
StructuredBuffer<EnvLightData> _EnvLightList;
int _EnvLightCount;

// TODO: Think about how to apply Disney diffuse preconvolve on indirect diffuse => must be done during GBuffer layout! Else emissive will be fucked...
// That's mean we need to read DFG texture during Gbuffer...
void ForwardLighting(	float3 V, float3 positionWS, PreLightData prelightData, BSDFData bsdfData,
                        out float4 diffuseLighting,
                        out float4 specularLighting)
{
    diffuseLighting = float4(0.0, 0.0, 0.0, 0.0);
    specularLighting = float4(0.0, 0.0, 0.0, 0.0);

    for (int i = 0; i < _PunctualLightCount; ++i)
    {
        float4 localDiffuseLighting;
        float4 localSpecularLighting;
        EvaluateBSDF_Punctual(V, positionWS, prelightData, _PunctualLightList[i], bsdfData, localDiffuseLighting, localSpecularLighting);
        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }

    /*
    for (int i = 0; i < 4; ++i)
    {
    float4 localDiffuseLighting;
    float4 localSpecularLighting;
    EvaluateBSDF_Area(V, positionWS, areaLightData[i], bsdfData, localDiffuseLighting, localSpecularLighting);
    diffuseLighting += localDiffuseLighting;
    specularLighting += localSpecularLighting;
    }
    */

    for (int j = 0; j < _EnvLightCount; ++j)
    {
        float4 localDiffuseLighting;
        float4 localSpecularLighting;
        EvaluateBSDF_Env(V, positionWS, prelightData, _EnvLightList[j], bsdfData, UNITY_PASS_ENV(_EnvTextures), localDiffuseLighting, localSpecularLighting);
        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }
}

#endif // UNITY_LIGHTING_FORWARD_INCLUDED
