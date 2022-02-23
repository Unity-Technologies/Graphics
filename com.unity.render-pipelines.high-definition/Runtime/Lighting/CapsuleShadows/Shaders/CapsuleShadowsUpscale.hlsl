#ifndef CAPSULE_SHADOWS_UPSCALE_DEF
#define CAPSULE_SHADOWS_UPSCALE_DEF

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/Shaders/CapsuleShadowsGlobals.hlsl"

#define CAPSULE_UPSCALE_DEPTH_TOLERANCE_IN_PIXELS   8.f

float MaxElement(float2 v)
{
    return max(v.x, v.y);
}
float SumElements(float4 v)
{
    return v.x + v.y + v.z + v.w;
}

float4 CapsuleLinearFromDeviceDepth(float4 deviceDepths)
{
    // TODO: handle orthographic/oblique
    return float4(
        LinearEyeDepth(deviceDepths.x, _ZBufferParams),
        LinearEyeDepth(deviceDepths.y, _ZBufferParams),
        LinearEyeDepth(deviceDepths.z, _ZBufferParams),
        LinearEyeDepth(deviceDepths.w, _ZBufferParams));
}

float4 GetCapsuleShadowsUpscaleWeights(
    float2 positionSS,
    float targetLinearDepth,
    float2 firstDepthMipOffset,
    float2 depthPyramidTextureSizeRcp,
    float2 upscaledSizeRcp)
{
    float2 halfResPositionSS = .5f*positionSS;
    float2 t = frac(halfResPositionSS + .5f);
    float2 s = 1.f - t;
    float4 bilinearWeights = float4(s.x, t.x, t.x, s.x)*float4(t.y, t.y, s.y, s.y);

    float2 depthUV = (firstDepthMipOffset + halfResPositionSS)*depthPyramidTextureSizeRcp;
    float4 gatherDeviceDepths = GATHER_RED_TEXTURE2D_X(_CameraDepthTexture, s_linear_clamp_sampler, depthUV);
    float4 gatherLinearDepths = CapsuleLinearFromDeviceDepth(gatherDeviceDepths);

    // TODO: handle orthographic/oblique
    float2 viewFromClipScale = float2(UNITY_MATRIX_I_P._m00, UNITY_MATRIX_I_P._m11);
    float2 pixelHalfExtentXY = abs(viewFromClipScale*upscaledSizeRcp);
    float unitDepthInPixels = .5f/MaxElement(pixelHalfExtentXY);

    float4 gatherDepthsInTargetPixels = gatherLinearDepths*(unitDepthInPixels/targetLinearDepth);
    float4 depthDistancesInPixels = max(abs(gatherDepthsInTargetPixels - unitDepthInPixels) - CAPSULE_UPSCALE_DEPTH_TOLERANCE_IN_PIXELS, 1.f);

    float4 combinedWeights = bilinearWeights/depthDistancesInPixels;
    return combinedWeights/SumElements(combinedWeights);
}

#endif // ndef CAPSULE_SHADOWS_UPSCALE_DEF
