// To use in case the color have already been encoded in checkboard YCoCg
float4 EncodeSSSDataToBuffer(float2 Ychroma, float subsurfaceRadius, uint subsurfaceProfile, float thickness)
{
    // subsurfaceRadius is like a mask (a bit like metal parameters and don't need a lot of precision, store it on 4 bit
    // currently we support up to 16 SSS profile (SSS_N_PROFILES) - caution if this number change (for a higher value), code below must change!
    return float4(Ychroma, thickness, PackFloatInt8bit(subsurfaceRadius, subsurfaceProfile, 16.0));
}

// This function encode all data require for ScreenSpace SubsurfaceScattering (SSSSS)
// Aim to be use with a GBuffer
float4 EncodeSSSDataToBuffer(float3 color, float subsurfaceRadius, uint subsurfaceProfile, float thickness, uint2 positionSS)
{
    // RGBToYCoCg have better precision with a sRGB color, use cheap gamma 2 instead
    // Take care that the render target use linear format
    float3 YCoCg = RGBToYCoCg(LinearToGamma20(color));

    // Note: when we are in forward we don't need to store the thickness as it is already apply. So a potential optimization
    // when we know that we are in full forward only is to not store the thickness and store the full baseColor (so not cost at decode)
    // as this function aim to be share between hybrid deferred/forward, we can't assume it.

    // subsurfaceRadius is like a mask (a bit like metal parameters and don't need a lot of precision, store it on 4 bit
    // currently we support up to 16 SSS profile (SSS_N_PROFILES) - caution if this number change (for a higher value), code below must change!
    return EncodeSSSDataToBuffer((positionSS.x & 1) == (positionSS.y & 1) ? YCoCg.rb : YCoCg.rg, subsurfaceRadius, thickness, subsurfaceProfile, thickness);
}

float4 DecodeSSSDataFromBuffer(TEXTURE2D_ARGS_NOSAMPLER(SSSBuffer), uint2 positionSS, float4 inBuffer, out float3 color, out float subsurfaceRadius, out int subsurfaceProfileout, out float thickness)
{
    // unpack
    float2 YChroma0 = inBuffer.rg;
    thickness = inBuffer.b;
    UnpackFloatInt8bit(inBuffer.a, 16.0, subsurfaceRadius, subsurfaceProfile);

    // Reconstruct color
    // Note: We don't care about pixel at border, will be handled by the edge filter (as it will be black)
    float2 a0 = LOAD_TEXTURE2D(SSSBuffer, positionSS + uint2(1, 0)).rg;
    float2 a1 = LOAD_TEXTURE2D(SSSBuffer, positionSS - uint2(1, 0)).rg;
    float2 a2 = LOAD_TEXTURE2D(SSSBuffer, positionSS + uint2(0, 1)).rg;
    float2 a3 = LOAD_TEXTURE2D(SSSBuffer, positionSS - uint2(0, 1)).rg;
    float chroma1 = YCoCgCheckBoardEdgeFilter(YChroma0.r, a0, a1, a2, a3);
    float3 YCoCg = (positionSS.x & 1) == (positionSS.y & 1) ? float3(YChroma0.r, chroma1, YChroma0.g) : float3(YChroma0.rg, chroma1);
    color = Gamma20ToLinear(YCoCgToRGB(YCoCg));
}
