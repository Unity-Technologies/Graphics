//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef VOLUMETRICLIGHTING_CS_HLSL
#define VOLUMETRICLIGHTING_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.DensityVolumeFalloffMode:  static fields
//
#define DENSITYVOLUMEFALLOFFMODE_LINEAR (0)
#define DENSITYVOLUMEFALLOFFMODE_EXPONENTIAL (1)

// Generated from UnityEngine.Rendering.HighDefinition.DensityVolumeEngineData
// PackingRules = Exact
struct DensityVolumeEngineData
{
    float3 scattering;
    float extinction;
    float3 textureTiling;
    int invertFade;
    float3 textureScroll;
    float rcpDistFadeLen;
    float3 rcpPosFaceFade;
    float endTimesRcpDistFadeLen;
    float3 rcpNegFaceFade;
    int useVolumeMask;
    float3 atlasOffset;
    int falloffMode;
    float4 maskSize;
};

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesVolumetric
// PackingRules = Exact
CBUFFER_START(ShaderVariablesVolumetric)
    float4x4 _VBufferCoordToViewDirWS[2];
    float _VBufferUnitDepthTexelSpacing;
    uint _NumVisibleDensityVolumes;
    float _CornetteShanksConstant;
    uint _VBufferHistoryIsValid;
    float4 _VBufferSampleOffset;
    float4 _VolumeMaskDimensions;
    float4 _AmbientProbeCoeffs[7];
    float _VBufferVoxelSize;
    float _HaveToPad;
    float _OtherwiseTheBuffer;
    float _IsFilledWithGarbage;
    float4 _VBufferPrevViewportSize;
    float4 _VBufferHistoryViewportScale;
    float4 _VBufferHistoryViewportLimit;
    float4 _VBufferPrevDistanceEncodingParams;
    float4 _VBufferPrevDistanceDecodingParams;
    uint _NumTileBigTileX;
    uint _NumTileBigTileY;
    uint _Pad0_SVV;
    uint _Pad1_SVV;
CBUFFER_END

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
int GetInvertFade(DensityVolumeEngineData value)
{
    return value.invertFade;
}
float3 GetTextureScroll(DensityVolumeEngineData value)
{
    return value.textureScroll;
}
float GetRcpDistFadeLen(DensityVolumeEngineData value)
{
    return value.rcpDistFadeLen;
}
float3 GetRcpPosFaceFade(DensityVolumeEngineData value)
{
    return value.rcpPosFaceFade;
}
float GetEndTimesRcpDistFadeLen(DensityVolumeEngineData value)
{
    return value.endTimesRcpDistFadeLen;
}
float3 GetRcpNegFaceFade(DensityVolumeEngineData value)
{
    return value.rcpNegFaceFade;
}
int GetUseVolumeMask(DensityVolumeEngineData value)
{
    return value.useVolumeMask;
}
float3 GetAtlasOffset(DensityVolumeEngineData value)
{
    return value.atlasOffset;
}
int GetFalloffMode(DensityVolumeEngineData value)
{
    return value.falloffMode;
}
float4 GetMaskSize(DensityVolumeEngineData value)
{
    return value.maskSize;
}

#endif
