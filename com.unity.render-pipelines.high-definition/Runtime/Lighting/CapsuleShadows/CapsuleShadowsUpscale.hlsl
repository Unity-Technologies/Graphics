#ifndef CAPSULE_SHADOWS_UPSCALE_DEF
#define CAPSULE_SHADOWS_UPSCALE_DEF

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleShadowsGlobals.hlsl"

#define CAPSULE_UPSCALE_DEPTH_TOLERANCE_IN_PIXELS   8.f

float MaxElement(float2 v)
{
    return max(v.x, v.y);
}
float SumElements(float4 v)
{
    return v.x + v.y + v.z + v.w;
}

struct CapsuleShadowDepthWeightParams
{
    float unitDepthInPixels;
    float unitDepthInTargetPixels;
};

CapsuleShadowDepthWeightParams GetCapsuleShadowsDepthWeightParams(float targetLinearDepth, float2 upscaledSizeRcp)
{
    // TODO: handle orthographic/oblique
    float2 viewFromClipScale = float2(UNITY_MATRIX_I_P._m00, UNITY_MATRIX_I_P._m11);
    float2 pixelHalfExtentXY = abs(viewFromClipScale*upscaledSizeRcp);

    CapsuleShadowDepthWeightParams params;
    params.unitDepthInPixels = .5f/MaxElement(pixelHalfExtentXY);
    params.unitDepthInTargetPixels = params.unitDepthInPixels/targetLinearDepth;
    return params;
}

float GetCapsuleShadowsDepthWeight(float linearDepth, CapsuleShadowDepthWeightParams params)
{
    float linearDepthInTargetPixels = linearDepth*params.unitDepthInTargetPixels;
    return 1.f/max(abs(linearDepthInTargetPixels - params.unitDepthInPixels) - CAPSULE_UPSCALE_DEPTH_TOLERANCE_IN_PIXELS, 1.f);
}
float4 GetCapsuleShadowsDepthWeight(float4 linearDepth, CapsuleShadowDepthWeightParams params)
{
    float4 linearDepthInTargetPixels = linearDepth*params.unitDepthInTargetPixels;
    return 1.f/max(abs(linearDepthInTargetPixels - params.unitDepthInPixels) - CAPSULE_UPSCALE_DEPTH_TOLERANCE_IN_PIXELS, 1.f);
}

#endif // ndef CAPSULE_SHADOWS_UPSCALE_DEF
