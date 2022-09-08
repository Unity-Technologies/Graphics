#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Core/Utilities/GeometryUtils.cs.hlsl"

// don't support Buffer yet in unity
StructuredBuffer<uint>  g_vBigTileLightList;

StructuredBuffer<uint>  g_vLightListTile;
StructuredBuffer<uint>  g_vLightListCluster;

StructuredBuffer<uint>  g_vLayeredOffsetsBuffer;
StructuredBuffer<float> g_logBaseBuffer;

#ifdef USE_INDIRECT
    StructuredBuffer<uint> g_TileFeatureFlags;
#endif

StructuredBuffer<DirectionalLightData> _DirectionalLightDatas;
StructuredBuffer<LightData>            _LightDatas;
StructuredBuffer<EnvLightData>         _EnvLightDatas;

// Used by directional and spot lights
TEXTURE2D(_CookieAtlas);

// Used by cube and planar reflection probes
TEXTURE2D_ARRAY(_ReflectionAtlas);

// Contact shadows
TEXTURE2D_X_UINT(_ContactShadowTexture);

// Screen space shadows
TEXTURE2D_ARRAY(_ScreenSpaceShadowsTexture);

// Indirect Diffuse Texture
TEXTURE2D_X(_IndirectDiffuseTexture);
