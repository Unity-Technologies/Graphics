#ifndef UNITY_LIGHTING_FORWARD_INCLUDED 
#define UNITY_LIGHTING_FORWARD_INCLUDED

//-----------------------------------------------------------------------------
// Simple forward loop architecture
//-----------------------------------------------------------------------------

StructuredBuffer<PunctualLightData> g_punctualLightList;
int g_punctualLightCount;

// TODO: Think about how to apply Disney diffuse preconvolve on indirect diffuse => must be done during GBuffer layout! Else emissive will be fucked...
// That's mean we need to read DFG texture during Gbuffer...
void ForwardLighting(	float3 V, float3 positionWS, BSDFData bsdfData,
                        out float4 diffuseLighting,
                        out float4 specularLighting)
{
    diffuseLighting = float4(0.0, 0.0, 0.0, 0.0);
    specularLighting = float4(0.0, 0.0, 0.0, 0.0);

    for (int i = 0; i < g_punctualLightCount; ++i)
    {
        float4 localDiffuseLighting;
        float4 localSpecularLighting;
        EvaluateBSDF_Punctual(V, positionWS, g_punctualLightList[i], bsdfData, localDiffuseLighting, localSpecularLighting);
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
}

#endif // UNITY_LIGHTING_FORWARD_INCLUDED
