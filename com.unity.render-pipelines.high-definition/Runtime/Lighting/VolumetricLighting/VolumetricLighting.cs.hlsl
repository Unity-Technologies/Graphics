//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef VOLUMETRICLIGHTING_CS_HLSL
#define VOLUMETRICLIGHTING_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.DensityVolumeEngineData
// PackingRules = Exact
struct DensityVolumeEngineData
{
    float3 scattering;
    float extinction;
    float3 textureTiling;
    int textureIndex;
    float3 textureScroll;
    int invertFade;
    float3 rcpPosFaceFade;
    float rcpDistFadeLen;
    float3 rcpNegFaceFade;
    float endTimesRcpDistFadeLen;
};

//
// Accessors for UnityEngine.Rendering.HighDefinition.DensityVolumeEngineData
//
float3 GetScattering(DensityVolumeEngineData value)
{
    return value.scattering;
}
float GetExtinction(DensityVolumeEngineData value)
{
    return value.extinction;
}
float3 GetTextureTiling(DensityVolumeEngineData value)
{
    return value.textureTiling;
}
int GetTextureIndex(DensityVolumeEngineData value)
{
    return value.textureIndex;
}
float3 GetTextureScroll(DensityVolumeEngineData value)
{
    return value.textureScroll;
}
int GetInvertFade(DensityVolumeEngineData value)
{
    return value.invertFade;
}
float3 GetRcpPosFaceFade(DensityVolumeEngineData value)
{
    return value.rcpPosFaceFade;
}
float GetRcpDistFadeLen(DensityVolumeEngineData value)
{
    return value.rcpDistFadeLen;
}
float3 GetRcpNegFaceFade(DensityVolumeEngineData value)
{
    return value.rcpNegFaceFade;
}
float GetEndTimesRcpDistFadeLen(DensityVolumeEngineData value)
{
    return value.endTimesRcpDistFadeLen;
}

#endif
