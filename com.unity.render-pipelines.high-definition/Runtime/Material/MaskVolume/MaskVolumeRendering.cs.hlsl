//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef MASKVOLUMERENDERING_CS_HLSL
#define MASKVOLUMERENDERING_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.MaskVolumeEngineData
// PackingRules = Exact
struct MaskVolumeEngineData
{
    float3 debugColor;
    float weight;
    float3 rcpPosFaceFade;
    float rcpDistFadeLen;
    float3 rcpNegFaceFade;
    float endTimesRcpDistFadeLen;
    float3 scale;
    int payloadIndex;
    float3 bias;
    int blendMode;
    float3 resolution;
    uint lightLayers;
    float3 resolutionInverse;
    float normalBiasWS;
};

//
// Accessors for UnityEngine.Rendering.HighDefinition.MaskVolumeEngineData
//
float3 GetDebugColor(MaskVolumeEngineData value)
{
    return value.debugColor;
}
float GetWeight(MaskVolumeEngineData value)
{
    return value.weight;
}
float3 GetRcpPosFaceFade(MaskVolumeEngineData value)
{
    return value.rcpPosFaceFade;
}
float GetRcpDistFadeLen(MaskVolumeEngineData value)
{
    return value.rcpDistFadeLen;
}
float3 GetRcpNegFaceFade(MaskVolumeEngineData value)
{
    return value.rcpNegFaceFade;
}
float GetEndTimesRcpDistFadeLen(MaskVolumeEngineData value)
{
    return value.endTimesRcpDistFadeLen;
}
float3 GetScale(MaskVolumeEngineData value)
{
    return value.scale;
}
int GetPayloadIndex(MaskVolumeEngineData value)
{
    return value.payloadIndex;
}
float3 GetBias(MaskVolumeEngineData value)
{
    return value.bias;
}
int GetBlendMode(MaskVolumeEngineData value)
{
    return value.blendMode;
}
float3 GetResolution(MaskVolumeEngineData value)
{
    return value.resolution;
}
uint GetLightLayers(MaskVolumeEngineData value)
{
    return value.lightLayers;
}
float3 GetResolutionInverse(MaskVolumeEngineData value)
{
    return value.resolutionInverse;
}
float GetNormalBiasWS(MaskVolumeEngineData value)
{
    return value.normalBiasWS;
}

#endif
