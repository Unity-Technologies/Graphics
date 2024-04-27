//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESCAPSULESHADOWSRENDER_CS_HLSL
#define SHADERVARIABLESCAPSULESHADOWSRENDER_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesCapsuleShadowsRender
// PackingRules = Exact
CBUFFER_START(ShaderVariablesCapsuleShadowsRender)
    float4 _CapsuleUpscaledSize;
    uint _CapsuleCasterCount;
    uint _CapsuleOccluderCount;
    uint _CapsuleShadowFlags;
    float _CapsuleIndirectRangeFactor;
    uint _CapsuleUpscaledSizeInTilesX;
    uint _CapsuleUpscaledSizeInTilesY;
    uint _CapsuleCoarseTileSizeInFineTilesX;
    uint _CapsuleCoarseTileSizeInFineTilesY;
    uint _CapsuleRenderSizeInTilesX;
    uint _CapsuleRenderSizeInTilesY;
    uint _CapsuleRenderSizeInCoarseTilesX;
    uint _CapsuleRenderSizeInCoarseTilesY;
    uint _CapsuleRenderSizeX;
    uint _CapsuleRenderSizeY;
    uint _CapsuleTileDebugMode;
    uint _CapsuleDebugCasterIndex;
    uint _CapsuleDepthMipOffsetX;
    uint _CapsuleDepthMipOffsetY;
    float _CapsuleIndirectCosAngle;
    uint _CapsuleShadowsRenderPad1;
    float3 _CapsuleShadowsLUTCoordScale;
    uint _CapsuleShadowsRenderPad2;
    float3 _CapsuleShadowsLUTCoordOffset;
    uint _CapsuleShadowsRenderPad3;
CBUFFER_END


#endif
