//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef SHADERVARIABLESLIGHTLOOP_CS_HLSL
#define SHADERVARIABLESLIGHTLOOP_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.ShaderVariablesLightLoop:  static fields
//
#define MAX_ENV2DLIGHT (32)

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesLightLoop
// PackingRules = Exact
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
    uint _EnvProxyCount;
    int _EnvLightSkyEnabled;
    int _DirectionalShadowIndex;
    float4 _CookieAtlasSize;
    float4 _CookieAtlasData;
    float4 _PlanarAtlasData;
    float _MicroShadowOpacity;
    float _DirectionalTransmissionMultiplier;
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
    uint _CascadeShadowCount;
    int _DebugSingleShadowIndex;
    int _EnvSliceSize;
    int _RaytracedIndirectDiffuse;


#endif
