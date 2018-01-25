#ifndef LIGHTDEFINITION_CUSTOM_HLSL
#define LIGHTDEFINITION_CUSTOM_HLSL

//-----------------------------------------------------------------------------
// Packing accessors
//-----------------------------------------------------------------------------

float3 EnvLightData_GetProxyForward(EnvLightData lightData)
{
    return float3(lightData.proxyForwardX, lightData.proxyForwardY, lightData.proxyForwardZ);
}

float3 EnvLightData_GetProxyUp(EnvLightData lightData)
{
    return float3(lightData.proxyUpX, lightData.proxyUpY, lightData.proxyUpZ);
}

float3 EnvLightData_GetProxyRight(EnvLightData lightData)
{
    return float3(lightData.proxyRightX, lightData.proxyRightY, lightData.proxyRightZ);
}

#endif