#ifndef SHADER_VARIABLES_CAPSULE_SHADOWS_RENDER_HLSL
#define SHADER_VARIABLES_CAPSULE_SHADOWS_RENDER_HLSL

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/ShaderVariablesCapsuleShadowsRender.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/CapsuleShadows/CapsuleShadowsFlags.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleShadowsCommon.hlsl"

#define _CapsuleCoarseTileSizeInFineTiles   uint2(_CapsuleCoarseTileSizeInFineTilesX, _CapsuleCoarseTileSizeInFineTilesY)
#define _CapsuleRenderSizeInCoarseTiles     uint2(_CapsuleRenderSizeInCoarseTilesX, _CapsuleRenderSizeInCoarseTilesY)
#define _CapsuleRenderSizeInTiles           uint2(_CapsuleRenderSizeInTilesX, _CapsuleRenderSizeInTilesY)
#define _CapsuleUpscaledSizeInTiles         uint2(_CapsuleUpscaledSizeInTilesX, _CapsuleUpscaledSizeInTilesY)
#define _CapsuleRenderSize                  uint2(_CapsuleRenderSizeX, _CapsuleRenderSizeY)
#define _CapsuleDepthMipOffset              uint2(_CapsuleDepthMipOffsetX, _CapsuleDepthMipOffsetY)

uint GetCapsuleDirectOcclusionFlags()
{
    uint flags = 0;
    if ((_CapsuleShadowFlags & CAPSULESHADOWFLAGS_FADE_SELF_SHADOW) != 0)
        flags |= CAPSULE_OCCLUSION_FLAG_FADE_SELF_SHADOW;
    if ((_CapsuleShadowFlags & CAPSULESHADOWFLAGS_FULL_CAPSULE_OCCLUSION) != 0)
        flags |= CAPSULE_OCCLUSION_FLAG_INCLUDE_AXIS;
    if ((_CapsuleShadowFlags & CAPSULESHADOWFLAGS_SHOW_RAY_TRACED_REFERENCE) != 0)
        flags |= CAPSULE_OCCLUSION_FLAG_RAY_TRACED_REFERENCE;
    return flags;
}

uint GetCapsuleIndirectOcclusionFlags()
{
    return GetCapsuleDirectOcclusionFlags();
}

uint GetCapsuleAmbientOcclusionFlags()
{
    uint flags = 0;
    if ((_CapsuleShadowFlags & CAPSULESHADOWFLAGS_FULL_CAPSULE_AMBIENT_OCCLUSION) != 0)
        flags |= CAPSULE_AMBIENT_OCCLUSION_FLAG_INCLUDE_AXIS;
    if ((_CapsuleShadowFlags & CAPSULESHADOWFLAGS_SHOW_RAY_TRACED_REFERENCE) != 0)
        flags |= CAPSULE_AMBIENT_OCCLUSION_RAY_TRACED_REFERENCE;
    return flags;
}

#endif // ndef SHADER_VARIABLES_CAPSULE_SHADOWS_RENDER_HLSL
