#ifndef SHADER_VARIABLES_CAPSULE_SHADOWS_BUILD_TILES_HLSL
#define SHADER_VARIABLES_CAPSULE_SHADOWS_BUILD_TILES_HLSL

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/ShaderVariablesCapsuleShadowsBuildTiles.cs.hlsl"

#define _CapsuleCoarseTileSizeInFineTiles   uint2(_CapsuleCoarseTileSizeInFineTilesX, _CapsuleCoarseTileSizeInFineTilesY)
#define _CapsuleRenderSizeInCoarseTiles     uint2(_CapsuleRenderSizeInCoarseTilesX, _CapsuleRenderSizeInCoarseTilesY)
#define _CapsuleDepthMipOffset              uint2(_CapsuleDepthMipOffsetX, _CapsuleDepthMipOffsetY)

#endif // ndef SHADER_VARIABLES_CAPSULE_SHADOWS_BUILD_TILES_HLSL
