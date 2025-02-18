//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef HDRENDERPIPELINE_VOLUMETRICLIGHTING_CS_HLSL
#define HDRENDERPIPELINE_VOLUMETRICLIGHTING_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.LocalVolumetricFogBlendingMode:  static fields
//
#define LOCALVOLUMETRICFOGBLENDINGMODE_OVERWRITE (0)
#define LOCALVOLUMETRICFOGBLENDINGMODE_ADDITIVE (1)
#define LOCALVOLUMETRICFOGBLENDINGMODE_MULTIPLY (2)
#define LOCALVOLUMETRICFOGBLENDINGMODE_MIN (3)
#define LOCALVOLUMETRICFOGBLENDINGMODE_MAX (4)

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
    int falloffMode;
    float3 textureTiling;
    int invertFade;
    float3 textureScroll;
    float rcpDistFadeLen;
    float3 rcpPosFaceFade;
    float endTimesRcpDistFadeLen;
    float3 rcpNegFaceFade;
    int blendingMode;
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
    uint _MaxSliceCount;
    float _MaxVolumetricFogDistance;
    float4 _CameraRight;
    float4x4 _CameraInverseViewProjection_NO;
    uint _VolumeCount;
    uint _IsObliqueProjectionMatrix;
    float _HalfVoxelArcLength;
    uint _Padding2;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.VolumetricMaterialDataCBuffer
// PackingRules = Exact
CBUFFER_START(VolumetricMaterialDataCBuffer)
    float4 _VolumetricMaterialObbRight;
    float4 _VolumetricMaterialObbUp;
    float4 _VolumetricMaterialObbExtents;
    float4 _VolumetricMaterialObbCenter;
    float4 _VolumetricMaterialRcpPosFaceFade;
    float4 _VolumetricMaterialRcpNegFaceFade;
    float _VolumetricMaterialInvertFade;
    float _VolumetricMaterialRcpDistFadeLen;
    float _VolumetricMaterialEndTimesRcpDistFadeLen;
    float _VolumetricMaterialFalloffMode;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.VolumetricMaterialRenderingData
// PackingRules = Exact
struct VolumetricMaterialRenderingData
{
    float4 viewSpaceBounds;
    uint startSliceIndex;
    uint sliceCount;
    uint padding0;
    uint padding1;
    float4 obbVertexPositionWS[8];
};

//
// Accessors for UnityEngine.Rendering.HighDefinition.LocalVolumetricFogEngineData
//
float3 GetScattering(LocalVolumetricFogEngineData value)
{
    return value.scattering;
}
int GetFalloffMode(LocalVolumetricFogEngineData value)
{
    return value.falloffMode;
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
int GetBlendingMode(LocalVolumetricFogEngineData value)
{
    return value.blendingMode;
}

#endif
