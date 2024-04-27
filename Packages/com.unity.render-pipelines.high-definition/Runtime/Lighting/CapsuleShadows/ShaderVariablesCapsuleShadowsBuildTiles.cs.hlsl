//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESCAPSULESHADOWSBUILDTILES_CS_HLSL
#define SHADERVARIABLESCAPSULESHADOWSBUILDTILES_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesCapsuleShadowsBuildTiles
// PackingRules = Exact
CBUFFER_START(ShaderVariablesCapsuleShadowsBuildTiles)
    float4 _CapsuleUpscaledSize;
    uint _CapsuleCoarseTileSizeInFineTilesX;
    uint _CapsuleCoarseTileSizeInFineTilesY;
    uint _CapsuleRenderSizeInCoarseTilesX;
    uint _CapsuleRenderSizeInCoarseTilesY;
    uint _CapsuleOccluderCount;
    uint _CapsuleCasterCount;
    uint _CapsuleShadowFlags;
    uint _CapsuleShadowsViewCount;
    uint _CapsuleDepthMipOffsetX;
    uint _CapsuleDepthMipOffsetY;
    float _CapsuleIndirectRangeFactor;
    uint _CapsuleBuildTilesPad0;
CBUFFER_END


#endif
