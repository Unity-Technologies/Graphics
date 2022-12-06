#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Core/Utilities/GeometryUtils.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.hlsl"

// don't support Buffer yet in unity
StructuredBuffer<uint>  g_vBigTileLightList;

StructuredBuffer<uint>  g_vLightListTile;
StructuredBuffer<uint>  g_vLightListCluster;

StructuredBuffer<uint>  g_vLayeredOffsetsBuffer;
StructuredBuffer<float> g_logBaseBuffer;

#ifdef USE_INDIRECT
    StructuredBuffer<uint> g_TileFeatureFlags;
#endif

GLOBAL_RESOURCE(StructuredBuffer<DirectionalLightData>, _DirectionalLightDatas, RAY_TRACING_DIRECTIONAL_LIGHT_DATAS_REGISTER);
GLOBAL_RESOURCE(StructuredBuffer<LightData>, _LightDatas, RAY_TRACING_LIGHT_DATAS_REGISTER);
GLOBAL_RESOURCE(StructuredBuffer<EnvLightData>, _EnvLightDatas, RAY_TRACING_ENV_LIGHT_DATA_REGISTER);

// Used by directional and spot lights
GLOBAL_TEXTURE2D(_CookieAtlas, RAY_TRACING_COOKIE_ATLAS_REGISTER);

// Used by cube and planar reflection probes
GLOBAL_TEXTURE2D_ARRAY(_ReflectionAtlas, RAY_TRACING_REFLECTION_ATLAS_REGISTER);

// Contact shadows
TEXTURE2D_X_UINT(_ContactShadowTexture);

// Screen space shadows
TEXTURE2D_ARRAY(_ScreenSpaceShadowsTexture);

// Indirect Diffuse Texture
TEXTURE2D_X(_IndirectDiffuseTexture);
