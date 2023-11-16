//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESGLOBAL_CS_HLSL
#define SHADERVARIABLESGLOBAL_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.ShaderVariablesGlobal:  static fields
//
#define RENDERING_LIGHT_LAYERS_MASK (255)
#define RENDERING_LIGHT_LAYERS_MASK_SHIFT (0)
#define RENDERING_DECAL_LAYERS_MASK (65280)
#define RENDERING_DECAL_LAYERS_MASK_SHIFT (8)
#define DEFAULT_RENDERING_LAYER_MASK (257)
#define DEFAULT_DECAL_LAYERS (255)

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesGlobal
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariablesGlobal, b0)
    float4x4 _ViewMatrix;
    float4x4 _CameraViewMatrix;
    float4x4 _InvViewMatrix;
    float4x4 _ProjMatrix;
    float4x4 _InvProjMatrix;
    float4x4 _ViewProjMatrix;
    float4x4 _CameraViewProjMatrix;
    float4x4 _InvViewProjMatrix;
    float4x4 _NonJitteredViewProjMatrix;
    float4x4 _PrevViewProjMatrix;
    float4x4 _PrevInvViewProjMatrix;
    float4 _WorldSpaceCameraPos_Internal;
    float4 _PrevCamPosRWS_Internal;
    float4 _ScreenSize;
    float4 _PostProcessScreenSize;
    float4 _RTHandleScale;
    float4 _RTHandleScaleHistory;
    float4 _RTHandlePostProcessScale;
    float4 _RTHandlePostProcessScaleHistory;
    float4 _DynamicResolutionFullscreenScale;
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
    int _FogEnabled;
    int _PBRFogEnabled;
    int _EnableVolumetricFog;
    float _MaxFogDistance;
    float4 _FogColor;
    float _FogColorMode;
    float _GlobalMipBias;
    float _GlobalMipBiasPow2;
    float _Pad0;
    float4 _MipFogParameters;
    float4 _HeightFogBaseScattering;
    float _HeightFogBaseExtinction;
    float _HeightFogBaseHeight;
    float _GlobalFogAnisotropy;
    int _VolumetricFilteringEnabled;
    float2 _HeightFogExponents;
    int _FogDirectionalOnly;
    float _FogGIDimmer;
    float4 _VBufferViewportSize;
    float4 _VBufferLightingViewportScale;
    float4 _VBufferLightingViewportLimit;
    float4 _VBufferDistanceEncodingParams;
    float4 _VBufferDistanceDecodingParams;
    uint _VBufferSliceCount;
    float _VBufferRcpSliceCount;
    float _VBufferRcpInstancedViewCount;
    float _VBufferLastSliceDist;
    float4 _ShadowAtlasSize;
    float4 _CascadeShadowAtlasSize;
    float4 _AreaShadowAtlasSize;
    float4 _CachedShadowAtlasSize;
    float4 _CachedAreaShadowAtlasSize;
    int _ReflectionsMode;
    int _UnusedPadding0;
    int _UnusedPadding1;
    int _UnusedPadding2;
    uint _DirectionalLightCount;
    uint _PunctualLightCount;
    uint _AreaLightCount;
    uint _EnvLightCount;
    int _EnvLightSkyEnabled;
    uint _CascadeShadowCount;
    int _DirectionalShadowIndex;
    uint _EnableLightLayers;
    uint _EnableSkyReflection;
    uint _EnableSSRefraction;
    float _SSRefractionInvScreenWeightDistance;
    float _ColorPyramidLodCount;
    float _DirectionalTransmissionMultiplier;
    float _ProbeExposureScale;
    float _ContactShadowOpacity;
    float _ReplaceDiffuseForIndirect;
    float4 _AmbientOcclusionParam;
    float _IndirectDiffuseLightingMultiplier;
    uint _IndirectDiffuseLightingLayers;
    float _ReflectionLightingMultiplier;
    uint _ReflectionLightingLayers;
    float _MicroShadowOpacity;
    uint _EnableProbeVolumes;
    uint _ProbeVolumeCount;
    float _SlopeScaleDepthBias;
    float4 _CookieAtlasSize;
    float4 _CookieAtlasData;
    float4 _ReflectionAtlasCubeData;
    float4 _ReflectionAtlasPlanarData;
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
    uint _EnableDecalLayers;
    float4 _ShapeParamsAndMaxScatterDists[16];
    float4 _TransmissionTintsAndFresnel0[16];
    float4 _WorldScalesAndFilterRadiiAndThicknessRemaps[16];
    uint4 _DiffusionProfileHashTable[16];
    uint _EnableSubsurfaceScattering;
    uint _TexturingModeFlags;
    uint _TransmissionFlags;
    uint _DiffusionProfileCount;
    float2 _DecalAtlasResolution;
    uint _EnableDecals;
    uint _DecalCount;
    float _OffScreenDownsampleFactor;
    uint _OffScreenRendering;
    uint _XRViewCount;
    int _FrameCount;
    float4 _CoarseStencilBufferSize;
    int _IndirectDiffuseMode;
    int _EnableRayTracedReflections;
    int _RaytracingFrameIndex;
    uint _EnableRecursiveRayTracing;
    int _TransparentCameraOnlyMotionVectors;
    float _GlobalTessellationFactorMultiplier;
    float _SpecularOcclusionBlend;
    float _DeExposureMultiplier;
    float4 _ScreenSizeOverride;
    float4 _ScreenCoordScaleBias;
    float4 _ColorPyramidUvScaleAndLimitPrevFrame;
CBUFFER_END


#endif
