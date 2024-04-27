#ifndef CAPSULE_SHADOWS_UPSCALE_HLSL
#define CAPSULE_SHADOWS_UPSCALE_HLSL

TEXTURE2D_X_UINT(_CapsuleShadowTileBits);
TEXTURE2D_ARRAY(_CapsuleShadowVisibility);
float4 _CapsuleShadowsRenderOutputSize;

float CapsuleShadowsVisibilityMomentUpscaled(
    uint2 positionSS,
    bool isQuarterRes,
    float linearDepth,
    uint casterIndex,
    out bool isValidTile)
{
    // check tile bits to avoid reading outside of written tiles
    uint2 coarsePixelCoord = positionSS >> (isQuarterRes ? 2 : 1);
    uint2 tileCoord = (coarsePixelCoord + 4)/8;
    uint tileBits = LOAD_TEXTURE2D_X(_CapsuleShadowTileBits, tileCoord);
    isValidTile = ((tileBits & (1U << casterIndex)) != 0);

    float visibility = 1.f;
    if (isValidTile)
    {
        float coordScale = isQuarterRes ? .25f : .5f;
        float2 uv = coordScale*(float2(positionSS) + .5f)*_CapsuleShadowsRenderOutputSize.zw;

        float2 depth_stats = SAMPLE_TEXTURE2D_ARRAY_LOD(_CapsuleShadowVisibility, s_linear_clamp_sampler, uv, INDEX_TEXTURE2D_ARRAY_X(0), 0.f).xy;
        float d = depth_stats.x;
        float s_d = depth_stats.y;

        float2 vis_stats = SAMPLE_TEXTURE2D_ARRAY_LOD(_CapsuleShadowVisibility, s_linear_clamp_sampler, uv, INDEX_TEXTURE2D_ARRAY_X(1 + casterIndex), 0.f).xy;
        float v = vis_stats.x;
        float s_vd = vis_stats.y;

        float eps = .0001f;
        float beta = s_vd/(s_d*s_d + eps);
        float alpha = v - beta*d;

        visibility = saturate(alpha + beta*linearDepth);
    }
    return visibility;
}

#endif // ndef CAPSULE_SHADOWS_UPSCALE_HLSL
