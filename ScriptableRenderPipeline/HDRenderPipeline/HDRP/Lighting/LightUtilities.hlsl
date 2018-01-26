#ifndef UNITY_LIGHT_UTILITIES_INCLUDED
#define UNITY_LIGHT_UTILITIES_INCLUDED

#include "LightDefinition.cs.hlsl"

#define SETTER_FLOAT3(data, field, value)\
 data.##field##X = value.x;\
 data.##field##Y = value.y;\
 data.##field##Z = value.z

// The EnvLightData of the sky light contains a bunch of compile-time constants.
// This function sets them directly to allow the compiler to propagate them and optimize the code.
EnvLightData InitSkyEnvLightData(int envIndex)
{
    EnvLightData output;
    ZERO_INITIALIZE(EnvLightData, output);
    output.influenceShapeType = ENVSHAPETYPE_SKY;
    output.envIndex = envIndex;

    SETTER_FLOAT3(output, influenceForward, float3(0.0, 0.0, 1.0));
    SETTER_FLOAT3(output, influenceUp, float3(0.0, 1.0, 0.0));
    SETTER_FLOAT3(output, influenceRight, float3(1.0, 0.0, 0.0));
    SETTER_FLOAT3(output, influencePositionWS, float3(0.0, 0.0, 0.0));

    output.dimmer = 1.0;

    // proxy
    SETTER_FLOAT3(output, proxyForward, float3(0.0, 0.0, 1.0));
    SETTER_FLOAT3(output, proxyUp, float3(0.0, 1.0, 0.0));
    SETTER_FLOAT3(output, proxyRight, float3(1.0, 0.0, 0.0));
    output.minProjectionDistance = 65504.0f;

    return output;
}

#undef SETTER_FLOAT3

#endif // UNITY_LIGHT_UTILITIES_INCLUDED
