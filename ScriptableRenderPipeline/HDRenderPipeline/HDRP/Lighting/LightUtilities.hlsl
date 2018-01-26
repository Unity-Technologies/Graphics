#ifndef UNITY_LIGHT_UTILITIES_INCLUDED
#define UNITY_LIGHT_UTILITIES_INCLUDED

#include "LightDefinition.cs.hlsl"

// The EnvLightData of the sky light contains a bunch of compile-time constants.
// This function sets them directly to allow the compiler to propagate them and optimize the code.
EnvLightData InitSkyEnvLightData(int envIndex)
{
    EnvLightData output;
    output.influenceShapeType = ENVSHAPETYPE_SKY;
    output.envIndex = envIndex;
    EnvLightData_Set_capturePositionWS(output, float3(0.0, 0.0, 0.0));

    EnvLightData_Set_influenceForward(output, float3(0.0, 0.0, 1.0));
    EnvLightData_Set_influenceUp(output, float3(0.0, 1.0, 0.0));
    EnvLightData_Set_influenceRight(output, float3(1.0, 0.0, 0.0));
    EnvLightData_Set_influencePositionWS(output, float3(0.0, 0.0, 0.0));
    EnvLightData_Set_influenceExtents(output, float3(0.0, 0.0, 0.0));

    EnvLightData_Set_blendDistancePositive(output, float3(0.0, 0.0, 0.0));
    EnvLightData_Set_blendDistanceNegative(output, float3(0.0, 0.0, 0.0));
    EnvLightData_Set_blendNormalDistancePositive(output, float3(0.0, 0.0, 0.0));
    EnvLightData_Set_blendNormalDistanceNegative(output, float3(0.0, 0.0, 0.0));
    EnvLightData_Set_boxSideFadePositive(output, float3(0.0, 0.0, 0.0));
    EnvLightData_Set_boxSideFadeNegative(output, float3(0.0, 0.0, 0.0));
    output.dimmer = 1.0;
    EnvLightData_Set_sampleDirectionDiscardWS(output, float3(0.0, 0.0, 0.0));

    // proxy
    EnvLightData_Set_proxyForward(output, float3(0.0, 0.0, 1.0));
    EnvLightData_Set_proxyUp(output, float3(0.0, 1.0, 0.0));
    EnvLightData_Set_proxyRight(output, float3(1.0, 0.0, 0.0));
    EnvLightData_Set_proxyPositionWS(output, float3(0.0, 0.0, 0.0));
    output.minProjectionDistance = 65504.0f;
    EnvLightData_Set_proxyExtents(output, float3(0.0, 0.0, 0.0));

    return output;
}

#endif // UNITY_LIGHT_UTILITIES_INCLUDED
