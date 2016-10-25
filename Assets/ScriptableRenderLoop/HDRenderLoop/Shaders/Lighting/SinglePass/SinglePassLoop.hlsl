//-----------------------------------------------------------------------------
// LightLoop
// ----------------------------------------------------------------------------

// bakeDiffuseLighting is part of the prototype so a user is able to implement a "base pass" with GI and multipass direct light (aka old unity rendering path)
void LightLoop(	float3 V, float3 positionWS, PreLightData prelightData, BSDFData bsdfData, float3 bakeDiffuseLighting,
                out float4 diffuseLighting,
                out float4 specularLighting)
{
    LightLoopContext context;
    ZERO_INITIALIZE(LightLoopContext, context);

    diffuseLighting = float4(0.0, 0.0, 0.0, 0.0);
    specularLighting = float4(0.0, 0.0, 0.0, 0.0);

    for (int i = 0; i < _PunctualLightCount; ++i)
    {
        float4 localDiffuseLighting;
        float4 localSpecularLighting;
        EvaluateBSDF_Punctual(context, V, positionWS, prelightData, _PunctualLightList[i], bsdfData, localDiffuseLighting, localSpecularLighting);
        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }

    /*
    for (int i = 0; i < 4; ++i)
    {
    float4 localDiffuseLighting;
    float4 localSpecularLighting;
    EvaluateBSDF_Area(0, V, positionWS, areaLightData[i], bsdfData, localDiffuseLighting, localSpecularLighting);
    diffuseLighting += localDiffuseLighting;
    specularLighting += localSpecularLighting;
    }
    */

    float4 iblDiffuseLighting = float4(0.0, 0.0, 0.0, 0.0);
    float4 iblSpecularLighting = float4(0.0, 0.0, 0.0, 0.0);

    for (int j = 0; j < _EnvLightCount; ++j)
    {
        float4 localDiffuseLighting;
        float4 localSpecularLighting;
        context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;
        EvaluateBSDF_Env(context, V, positionWS, prelightData, _EnvLightList[j], bsdfData, localDiffuseLighting, localSpecularLighting);
        iblDiffuseLighting.rgb = lerp(iblDiffuseLighting.rgb, localDiffuseLighting.rgb, localDiffuseLighting.a); // Should be remove by the compiler if it is smart as all is constant 0
        iblSpecularLighting.rgb = lerp(iblSpecularLighting.rgb, localSpecularLighting.rgb, localSpecularLighting.a);
    }

    /*
    // Sky Ibl
    {
        float4 localDiffuseLighting;
        float4 localSpecularLighting;
        context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_SKY;
        EvaluateBSDF_Env(context, V, positionWS, prelightData, _EnvLightSky, bsdfData, localDiffuseLighting, localSpecularLighting);
        iblDiffuseLighting.rgb = lerp(iblDiffuseLighting.rgb, localDiffuseLighting.rgb, localDiffuseLighting.a); // Should be remove by the compiler if it is smart as all is constant 0
        iblSpecularLighting.rgb = lerp(iblSpecularLighting.rgb, localSpecularLighting.rgb, localSpecularLighting.a);
    }
    */

    diffuseLighting += iblDiffuseLighting;
    specularLighting += iblSpecularLighting;

    diffuseLighting.rgb += bakeDiffuseLighting;
}
