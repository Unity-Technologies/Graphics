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
    output.capturePositionWS = float3(0.0, 0.0, 0.0);

    output.influenceForward = float3(0.0, 0.0, 1.0);
    output.influenceUp = float3(0.0, 1.0, 0.0);
    output.influenceRight = float3(1.0, 0.0, 0.0);
    output.influencePositionWS = float3(0.0, 0.0, 0.0);
    output.influenceExtents = float3(0.0, 0.0, 0.0);

    output.blendDistancePositive = float3(0.0, 0.0, 0.0);
    output.blendDistanceNegative = float3(0.0, 0.0, 0.0);
    output.blendNormalDistancePositive = float3(0.0, 0.0, 0.0);
    output.blendNormalDistanceNegative = float3(0.0, 0.0, 0.0);
    output.boxSideFadePositive = float3(0.0, 0.0, 0.0);
    output.boxSideFadeNegative = float3(0.0, 0.0, 0.0);
    output.dimmer = 1.0;
    output.sampleDirectionDiscardWS = float3(0.0, 0.0, 0.0);

    // proxy
    output.proxyForwardX = 0;   output.proxyForwardY = 0;   output.proxyForwardZ = 1;
    output.proxyUpX = 0;        output.proxyUpY = 1;        output.proxyUpZ = 0;
    output.proxyRightX = 1;     output.proxyRightY = 0;     output.proxyRightZ = 0;
    output.proxyPositionWS = float3(0.0, 0.0, 0.0);
    output.minProjectionDistance = 65504.0f;
    output.proxyExtents = float3(0.0, 0.0, 0.0);

    return output;
}

#endif // UNITY_LIGHT_UTILITIES_INCLUDED
