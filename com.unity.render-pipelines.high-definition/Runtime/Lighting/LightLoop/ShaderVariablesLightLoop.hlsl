#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Core/Utilities/GeometryUtils.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/VolumetricLighting.cs.hlsl" // For 'DensityVolumeData'

// It appears that, due to our include structure, we have to always declare these.
StructuredBuffer<uint> _CoarseTileBuffer;
StructuredBuffer<uint> _FineTileBuffer;
StructuredBuffer<uint> _zBinBuffer;

#ifdef USE_INDIRECT
    StructuredBuffer<uint> g_TileFeatureFlags;
#endif

// Directional lights + 1x list per BoundedEntityCategory.
StructuredBuffer<DirectionalLightData> _DirectionalLightData;

// NEVER ACCESS THESE DIRECTLY.
StructuredBuffer<LightData>         _PunctualLightData;
StructuredBuffer<LightData>         _AreaLightData;
StructuredBuffer<EnvLightData>      _ReflectionProbeData;
// StructuredBuffer<DecalData> 		_DecalData; // Defined elsewhere
StructuredBuffer<DensityVolumeData> _DensityVolumeData;

// Used by directional and spot lights
TEXTURE2D(_CookieAtlas);

// Use texture array for reflection (or LatLong 2D array for mobile)
TEXTURECUBE_ARRAY_ABSTRACT(_EnvCubemapTextures);
TEXTURE2D(_Env2DTextures);

// Contact shadows
TEXTURE2D_X_UINT(_ContactShadowTexture);

// Screen space shadows
TEXTURE2D_ARRAY(_ScreenSpaceShadowsTexture);

// Indirect Diffuse Texture
TEXTURE2D_X(_IndirectDiffuseTexture);
