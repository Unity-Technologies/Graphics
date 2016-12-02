#ifndef UNITY_LIGHT_UTILITIES_INCLUDED
#define UNITY_LIGHT_UTILITIES_INCLUDED

#include "LightDefinition.cs.hlsl"

// The EnvLightData of the sky light contains a bunch of compile-time constants.
// This function sets them directly to allow the compiler to propagate them and optimize the code.
EnvLightData InitSkyEnvLightData(int envIndex)
{
    EnvLightData output;
    output.envShapeType  = ENVSHAPETYPE_SKY;
    output.envIndex      = envIndex;
    output.forward       = float3(0.0, 0.0, 1.0);
    output.up            = float3(0.0, 1.0, 0.0);
    output.right         = float3(1.0, 0.0, 0.0);
    output.positionWS    = float3(0.0, 0.0, 0.0);
    output.offsetLS      = float3(0.0, 0.0, 0.0);
    output.innerDistance = float3(0.0, 0.0, 0.0);
    output.blendDistance = 1.0;

    return output;
}

#endif // UNITY_LIGHT_UTILITIES_INCLUDED
