//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef HDRENDERPIPELINE_VOLUMETRICLIGHTING_CS_HLSL
#define HDRENDERPIPELINE_VOLUMETRICLIGHTING_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.LocalVolumetricFogFalloffMode:  static fields
//
#define LOCALVOLUMETRICFOGFALLOFFMODE_LINEAR (0)
#define LOCALVOLUMETRICFOGFALLOFFMODE_EXPONENTIAL (1)

// Generated from UnityEngine.Rendering.HighDefinition.LocalVolumetricFogEngineData
// PackingRules = Exact
struct LocalVolumetricFogEngineData
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
    uint _NumVisibleLocalVolumetricFog;
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
// Accessors for UnityEngine.Rendering.HighDefinition.LocalVolumetricFogEngineData
//
float3 GetScattering(LocalVolumetricFogEngineData value)
{
    return value.scattering;
}
float GetExtinction(LocalVolumetricFogEngineData value)
{
    return value.extinction;
}
float3 GetTextureTiling(LocalVolumetricFogEngineData value)
{
    return value.textureTiling;
}
int GetInvertFade(LocalVolumetricFogEngineData value)
{
    return value.invertFade;
}
float3 GetTextureScroll(LocalVolumetricFogEngineData value)
{
    return value.textureScroll;
}
float GetRcpDistFadeLen(LocalVolumetricFogEngineData value)
{
    return value.rcpDistFadeLen;
}
float3 GetRcpPosFaceFade(LocalVolumetricFogEngineData value)
{
    return value.rcpPosFaceFade;
}
float GetEndTimesRcpDistFadeLen(LocalVolumetricFogEngineData value)
{
    return value.endTimesRcpDistFadeLen;
}
float3 GetRcpNegFaceFade(LocalVolumetricFogEngineData value)
{
    return value.rcpNegFaceFade;
}
int GetUseVolumeMask(LocalVolumetricFogEngineData value)
{
    return value.useVolumeMask;
}
float3 GetAtlasOffset(LocalVolumetricFogEngineData value)
{
    return value.atlasOffset;
}
int GetFalloffMode(LocalVolumetricFogEngineData value)
{
    return value.falloffMode;
}
float4 GetMaskSize(LocalVolumetricFogEngineData value)
{
    return value.maskSize;
}

#endif
