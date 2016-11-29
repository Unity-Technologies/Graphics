#ifndef UNITY_LIGHT_UTILITIES_INCLUDED
#define UNITY_LIGHT_UTILITIES_INCLUDED

// The EnvLightData of the sky light contains a bunch of compile-time constants.
// This function sets them directly to allow the compiler to propagate them and optimize the code.
void InitSkyEnvLightData(int index)
{
    _EnvLightSky.envShapeType  = ENVSHAPETYPE_SKY;
    _EnvLightSky.envIndex      = index;
    _EnvLightSky.forward       = float3(0.0, 0.0, 1.0);
    _EnvLightSky.up            = float3(0.0, 1.0, 0.0);
    _EnvLightSky.right         = float3(1.0, 0.0, 0.0);
    _EnvLightSky.positionWS    = float3(0.0, 0.0, 0.0);
    _EnvLightSky.offsetLS      = float3(0.0, 0.0, 0.0);
    _EnvLightSky.innerDistance = float3(0.0, 0.0, 0.0);
    _EnvLightSky.blendDistance = 1.0;
}

#endif // UNITY_LIGHT_UTILITIES_INCLUDED