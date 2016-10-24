// Users can use SHADERPASS_FORWARD here to dinstinguish behavior between deferred and forward.

StructuredBuffer<PunctualLightData> _PunctualLightList;
int _PunctualLightCount;

StructuredBuffer<EnvLightData> _EnvLightList;
int _EnvLightCount;

// Use texture array for reflection
UNITY_DECLARE_TEXCUBEARRAY(_EnvTextures);

/*
// Use texture atlas for shadow map
StructuredBuffer<PunctualShadowData> _PunctualShadowList;
UNITY_DECLARE_SHADOWMAP(_ShadowMapAtlas);
*/

// bakeDiffuseLighting is part of the prototype so a user is able to implement a "base pass" with GI and multipass direct light (aka old unity rendering path)
void LightingLoop(	float3 V, float3 positionWS, PreLightData prelightData, BSDFData bsdfData, float3 bakeDiffuseLighting,
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

    float4 iblDiffuseLighting = float4(0.0, 0.0, 0.0, 0.0);
    float4 iblSpecularLighting = float4(0.0, 0.0, 0.0, 0.0);

    for (int j = 0; j < _EnvLightCount; ++j)
    {
        float4 localDiffuseLighting;
        float4 localSpecularLighting;
        EvaluateBSDF_Env(V, positionWS, prelightData, _EnvLightList[j], bsdfData, UNITY_PASS_TEXCUBEARRAY(_EnvTextures), localDiffuseLighting, localSpecularLighting);
        iblDiffuseLighting.rgb = lerp(iblDiffuseLighting.rgb, localDiffuseLighting.rgb, localDiffuseLighting.a); // Should be remove by the compiler if it is smart as all is constant 0
        iblSpecularLighting.rgb = lerp(iblSpecularLighting.rgb, localSpecularLighting.rgb, localSpecularLighting.a);
    }

    diffuseLighting += iblDiffuseLighting;
    diffuseLighting.rgb += bakeDiffuseLighting;

    specularLighting += iblSpecularLighting;
}