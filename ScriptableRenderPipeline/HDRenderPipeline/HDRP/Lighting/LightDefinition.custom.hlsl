#ifndef LIGHTDEFINITION_CUSTOM_HLSL
#define LIGHTDEFINITION_CUSTOM_HLSL

//-----------------------------------------------------------------------------
// Packing accessors
//-----------------------------------------------------------------------------

float3 EnvLightData_GetCapturePositionWS(EnvLightData lightData)
{
    return float3(lightData.capturePositionWSX, lightData.capturePositionWSY, lightData.capturePositionWSZ);
}

float3 EnvLightData_GetProxyPositionWS(EnvLightData lightData)
{
    return float3(lightData.proxyPositionWSX, lightData.proxyPositionWSY, lightData.proxyPositionWSZ);
}

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

float3 EnvLightData_GetProxyExtents(EnvLightData lightData)
{
    return float3(lightData.proxyExtentsX, lightData.proxyExtentsY, lightData.proxyExtentsZ);
}

float3 EnvLightData_GetInfluencePositionWS(EnvLightData lightData)
{
    return float3(lightData.influencePositionWSX, lightData.influencePositionWSY, lightData.influencePositionWSZ);
}

float3 EnvLightData_GetInfluenceForward(EnvLightData lightData)
{
    return float3(lightData.influenceForwardX, lightData.influenceForwardY, lightData.influenceForwardZ);
}

float3 EnvLightData_GetInfluenceUp(EnvLightData lightData)
{
    return float3(lightData.influenceUpX, lightData.influenceUpY, lightData.influenceUpZ);
}

float3 EnvLightData_GetInfluenceRight(EnvLightData lightData)
{
    return float3(lightData.influenceRightX, lightData.influenceRightY, lightData.influenceRightZ);
}

float3 EnvLightData_GetInfluenceExtents(EnvLightData lightData)
{
    return float3(lightData.influenceExtentsX, lightData.influenceExtentsY, lightData.influenceExtentsZ);
}

float3 EnvLightData_GetBlendDistancePositive(EnvLightData lightData)
{
    return float3(lightData.influenceExtentsX, lightData.blendDistancePositiveY, lightData.blendDistancePositiveZ);
}

float3 EnvLightData_GetBlendDistanceNegative(EnvLightData lightData)
{
    return float3(lightData.influenceExtentsX, lightData.blendDistanceNegativeY, lightData.blendDistanceNegativeZ);
}

float3 EnvLightData_GetBlendNormalDistancePositive(EnvLightData lightData)
{
    return float3(lightData.influenceExtentsX, lightData.blendNormalDistancePositiveY, lightData.blendNormalDistancePositiveZ);
}

float3 EnvLightData_GetBlendNormalDistanceNegative(EnvLightData lightData)
{
    return float3(lightData.influenceExtentsX, lightData.blendNormalDistanceNegativeY, lightData.blendNormalDistanceNegativeZ);
}

#endif