//-----------------------------------------------------------------------------
// LightLoop
// ----------------------------------------------------------------------------

// bakeDiffuseLighting is part of the prototype so a user is able to implement a "base pass" with GI and multipass direct light (aka old unity rendering path)
void LightLoop(	float3 V, float3 positionWS, Coordinate coord, PreLightData prelightData, BSDFData bsdfData, float3 bakeDiffuseLighting,
                out float3 diffuseLighting,
                out float3 specularLighting)
{
    LightLoopContext context;
    ZERO_INITIALIZE(LightLoopContext, context);

    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    uint i = 0; // Declare once to avoid the D3D11 compiler warning.

    for (i = 0; i < _DirectionalLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;

        EvaluateBSDF_Directional(   context, V, positionWS, prelightData, _DirectionalLightList[i], bsdfData,
                                    localDiffuseLighting, localSpecularLighting);

        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }

    for (i = 0; i < _PunctualLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;

        EvaluateBSDF_Punctual(context, V, positionWS, prelightData, _PunctualLightList[i], bsdfData,
                              localDiffuseLighting, localSpecularLighting);

        diffuseLighting  += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }

    for (i = 0; i < _AreaLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;

        if (_AreaLightList[i].lightType == GPULIGHTTYPE_LINE)
        {
            EvaluateBSDF_Line(context, V, positionWS, prelightData, _AreaLightList[i], bsdfData,
                              localDiffuseLighting, localSpecularLighting);
        }
        else
        {
            EvaluateBSDF_Area(context, V, positionWS, prelightData, _AreaLightList[i], bsdfData,
                              localDiffuseLighting, localSpecularLighting);
        }

        diffuseLighting  += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }

    float3 iblDiffuseLighting  = float3(0.0, 0.0, 0.0);
    float3 iblSpecularLighting = float3(0.0, 0.0, 0.0);

    for (i = 0; i < _EnvLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;
        float2 weight;
        context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;
        EvaluateBSDF_Env(context, V, positionWS, prelightData, _EnvLightList[i], bsdfData, localDiffuseLighting, localSpecularLighting, weight);
        iblDiffuseLighting  = lerp(iblDiffuseLighting,  localDiffuseLighting,  weight.x); // Should be remove by the compiler if it is smart as all is constant 0
        iblSpecularLighting = lerp(iblSpecularLighting, localSpecularLighting, weight.y);
    }

    // Only apply sky IBL if the sky texture is available.
    if (_EnvLightSkyEnabled)
    {
        float3 localDiffuseLighting, localSpecularLighting;
        float2 weight;
        context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_SKY;
        EnvLightData envLightSky = InitSkyEnvLightData(0);
        EvaluateBSDF_Env(context, V, positionWS, prelightData, envLightSky, bsdfData, localDiffuseLighting, localSpecularLighting, weight);
        iblDiffuseLighting  = lerp(iblDiffuseLighting,  localDiffuseLighting,  weight.x); // Should be remove by the compiler if it is smart as all is constant 0
        iblSpecularLighting = lerp(iblSpecularLighting, localSpecularLighting, weight.y);
    }

    diffuseLighting  += iblDiffuseLighting;
    specularLighting += iblSpecularLighting;

    // Add indirect diffuse + emissive (if any)
    diffuseLighting += bakeDiffuseLighting;
}
