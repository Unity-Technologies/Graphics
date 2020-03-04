//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef SHADERVARIABLESGLOBAL_CS_HLSL
#define SHADERVARIABLESGLOBAL_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.ShaderVariablesGlobal:  static fields
//
#define DEFAULT_LIGHT_LAYERS (255)
#define MAX_ENV2DLIGHT (32)

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesGlobal
// PackingRules = Exact
cbuffer ShaderVariablesGlobal
{
    float4x4 _ViewMatrix;
    float4x4 _InvViewMatrix;
    float4x4 _ProjMatrix;
    float4x4 _InvProjMatrix;
    float4x4 _ViewProjMatrix;
    float4x4 _CameraViewProjMatrix;
    float4x4 _InvViewProjMatrix;
    float4x4 _NonJitteredViewProjMatrix;
    float4x4 _PrevViewProjMatrix;
    float4x4 _PrevInvViewProjMatrix;
    float3 _WorldSpaceCameraPos_Internal;
    float _Pad0;
    float3 _PrevCamPosRWS_Internal;
    float _Pad1;
    float4 _ScreenSize;
    float4 _RTHandleScale;
    float4 _RTHandleScaleHistory;
    float4 _ZBufferParams;
    float4 _ProjectionParams;
    float4 unity_OrthoParams;
    float4 _ScreenParams;
    float4 _FrustumPlanes[6];
    float4 _ShadowFrustumPlanes[6];
    float4 _TaaFrameInfo;
    float4 _TaaJitterStrength;
    float4 _Time;
    float4 _SinTime;
    float4 _CosTime;
    float4 unity_DeltaTime;
    float4 _TimeParameters;
    float4 _LastTimeParameters;
    float3 _HeightFogBaseScattering;
    float _HeightFogBaseExtinction;
    float2 _HeightFogExponents;
    float _HeightFogBaseHeight;
    float4 _VBufferViewportSize;
    uint _VBufferSliceCount;
    float _VBufferRcpSliceCount;
    float _VBufferRcpInstancedViewCount;
    float _ContactShadowOpacity;
    float4 _VBufferSharedUvScaleAndLimit;
    float4 _VBufferDistanceEncodingParams;
    float4 _VBufferDistanceDecodingParams;
    float _VBufferLastSliceDist;
    int _EnableVolumetricFog;
    float4 _ShadowAtlasSize;
    float4 _CascadeShadowAtlasSize;
    float4 _AreaShadowAtlasSize;
    float4x4 _Env2DCaptureVP[32];
    float _Env2DCaptureForward[96];
    float4 _Env2DAtlasScaleOffset[32];
    uint _DirectionalLightCount;
    uint _PunctualLightCount;
    uint _AreaLightCount;
    uint _EnvLightCount;
    int _EnvLightSkyEnabled;
    uint _CascadeShadowCount;
    int _DirectionalShadowIndex;
    uint _EnableLightLayers;
    float _ReplaceDiffuseForIndirect;
    uint _EnableSkyReflection;
    uint _EnableSSRefraction;
    float _MicroShadowOpacity;
    float _DirectionalTransmissionMultiplier;
    float4 _CookieAtlasSize;
    float4 _CookieAtlasData;
    float4 _PlanarAtlasData;
    uint _NumTileFtplX;
    uint _NumTileFtplY;
    float g_fClustScale;
    float g_fClustBase;
    float g_fNearPlane;
    float g_fFarPlane;
    int g_iLog2NumClusters;
    uint g_isLogBaseBufferEnabled;
    uint _NumTileClusteredX;
    uint _NumTileClusteredY;
    int _EnvSliceSize;
    int _RaytracedIndirectDiffuse;
    float4 _CameraMotionVectorsSize;
    float4 _ColorPyramidScale;
    float4 _DepthPyramidScale;
    float4 _CameraMotionVectorsScale;
    float4 _AmbientOcclusionParam;
    float4 _IndirectLightingMultiplier;
    float _SSRefractionInvScreenWeightDistance;
    int _FogEnabled;
    int _PBRFogEnabled;
    float _MaxFogDistance;
    float _FogColorMode;
    float _SkyTextureMipCount;
    float4 _FogColor;
    float4 _MipFogParameters;
    float4 _ThicknessRemaps[16];
    float4 _ShapeParams[16];
    float4 _TransmissionTintsAndFresnel0[16];
    float4 _WorldScales[16];
    float _DiffusionProfileHashTable[16];
    uint _EnableSubsurfaceScattering;
    float _TexturingModeFlags;
    float _TransmissionFlags;
    uint _DiffusionProfileCount;
    float2 _DecalAtlasResolution;
    uint _EnableDecals;
    uint _DecalCount;
    uint _OffScreenRendering;
    uint _OffScreenDownsampleFactor;
    uint _XRViewCount;
    int _FrameCount;
    float _ProbeExposureScale;
    int _UseRayTracedReflections;
    int _RaytracingFrameIndex;
    float4 _CoarseStencilBufferSize;
};


#endif
