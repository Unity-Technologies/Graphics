#ifndef UNITY_LIGHT_UTILITIES_INCLUDED
#define UNITY_LIGHT_UTILITIES_INCLUDED

#include "LightDefinition.cs.hlsl"

// The EnvLightData of the sky light contains a bunch of compile-time constants.
// This function sets them directly to allow the compiler to propagate them and optimize the code.
EnvLightData InitSkyEnvLightData(int envIndex)
{
    EnvLightData output;
    ZERO_INITIALIZE(EnvLightData, output);
    output.influenceShapeType = ENVSHAPETYPE_SKY;
    output.envIndex = envIndex;

    output.influenceForward = float3(0.0, 0.0, 1.0);
    output.influenceUp = float3(0.0, 1.0, 0.0);
    output.influenceRight = float3(1.0, 0.0, 0.0);
    output.influencePositionWS = float3(0.0, 0.0, 0.0);

    output.weight = 1.0;
    output.multiplier = 1.0;

    // proxy
    output.proxyForward = float3(0.0, 0.0, 1.0);
    output.proxyUp = float3(0.0, 1.0, 0.0);
    output.proxyRight = float3(1.0, 0.0, 0.0);
    output.minProjectionDistance = 65504.0f;

    return output;
}

#endif // UNITY_LIGHT_UTILITIES_INCLUDED
