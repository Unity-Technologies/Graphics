#ifndef LIGHTDEFINITION_CUSTOM_HLSL
#define LIGHTDEFINITION_CUSTOM_HLSL

//-----------------------------------------------------------------------------
// Packing accessors
//-----------------------------------------------------------------------------

#define GETTER_FLOAT3(Type, Name, field)\
float3 Name(Type data)\
{\
    return float3(data.##field##X, data.##field##Y, data.##field##Z);\
}

GETTER_FLOAT3(EnvLightData, GetCapturePositionWS, capturePositionWS)
GETTER_FLOAT3(EnvLightData, GetProxyPositionWS, proxyPositionWS)
GETTER_FLOAT3(EnvLightData, GetProxyForward, proxyForward)
GETTER_FLOAT3(EnvLightData, GetProxyUp, proxyUp)
GETTER_FLOAT3(EnvLightData, GetProxyRight, proxyRight)
GETTER_FLOAT3(EnvLightData, GetProxyExtents, proxyExtents)
GETTER_FLOAT3(EnvLightData, GetInfluencePositionWS, influencePositionWS)
GETTER_FLOAT3(EnvLightData, GetInfluenceForward, influenceForward)
GETTER_FLOAT3(EnvLightData, GetInfluenceUp, influenceUp)
GETTER_FLOAT3(EnvLightData, GetInfluenceRight, influenceRight)
GETTER_FLOAT3(EnvLightData, GetInfluenceExtents, influenceExtents)
GETTER_FLOAT3(EnvLightData, GetBlendDistancePositive, blendDistancePositive)
GETTER_FLOAT3(EnvLightData, GetBlendDistanceNegative, blendDistanceNegative)
GETTER_FLOAT3(EnvLightData, GetBlendNormalDistancePositive, blendNormalDistancePositive)
GETTER_FLOAT3(EnvLightData, GetBlendNormalDistanceNegative, blendNormalDistanceNegative)
GETTER_FLOAT3(EnvLightData, GetBoxSideFadePositive, boxSideFadePositive)
GETTER_FLOAT3(EnvLightData, GetBoxSideFadeNegative, boxSideFadeNegative)
GETTER_FLOAT3(EnvLightData, GetSampleDirectionDiscardWS, sampleDirectionDiscardWS)

#undef GETTER_FLOAT3

#endif