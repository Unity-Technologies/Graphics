//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit / Render Pipeline / Generate Shader Includes ] instead
//

#ifndef VOLUMETRICLIGHTING_CS_HLSL
#define VOLUMETRICLIGHTING_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.DensityVolumeFalloffMode:  static fields
//
#define DENSITYVOLUMEFALLOFFMODE_LINEAR (0)
#define DENSITYVOLUMEFALLOFFMODE_EXPONENTIAL (1)

// Generated from UnityEngine.Rendering.HighDefinition.DensityVolumeData
// PackingRules = Exact
struct DensityVolumeData
{
    float3 right;
    float extentX;
    float3 up;
    float extentY;
    float3 center;
    float extentZ;
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
    uint _Pad2_SVV;
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
// Accessors for UnityEngine.Rendering.HighDefinition.DensityVolumeData
//
float3 GetRight(DensityVolumeData value)
{
    return value.right;
}
float GetExtentX(DensityVolumeData value)
{
    return value.extentX;
}
float3 GetUp(DensityVolumeData value)
{
    return value.up;
}
float GetExtentY(DensityVolumeData value)
{
    return value.extentY;
}
float3 GetCenter(DensityVolumeData value)
{
    return value.center;
}
float GetExtentZ(DensityVolumeData value)
{
    return value.extentZ;
}
float3 GetScattering(DensityVolumeData value)
{
    return value.scattering;
}
float GetExtinction(DensityVolumeData value)
{
    return value.extinction;
}
float3 GetTextureTiling(DensityVolumeData value)
{
    return value.textureTiling;
}
int GetInvertFade(DensityVolumeData value)
{
    return value.invertFade;
}
float3 GetTextureScroll(DensityVolumeData value)
{
    return value.textureScroll;
}
float GetRcpDistFadeLen(DensityVolumeData value)
{
    return value.rcpDistFadeLen;
}
float3 GetRcpPosFaceFade(DensityVolumeData value)
{
    return value.rcpPosFaceFade;
}
float GetEndTimesRcpDistFadeLen(DensityVolumeData value)
{
    return value.endTimesRcpDistFadeLen;
}
float3 GetRcpNegFaceFade(DensityVolumeData value)
{
    return value.rcpNegFaceFade;
}
int GetUseVolumeMask(DensityVolumeData value)
{
    return value.useVolumeMask;
}
float3 GetAtlasOffset(DensityVolumeData value)
{
    return value.atlasOffset;
}
int GetFalloffMode(DensityVolumeData value)
{
    return value.falloffMode;
}
float4 GetMaskSize(DensityVolumeData value)
{
    return value.maskSize;
}

#endif
