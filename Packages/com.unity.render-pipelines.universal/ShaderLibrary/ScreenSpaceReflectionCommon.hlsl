#ifndef SCREEN_SPACE_REFLECTION_COMMON_INCLUDED
#define SCREEN_SPACE_REFLECTION_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// x : Blurriness - Exposed as a volume parameter; see ScreenSpaceReflectionVolumeSettings.blurriness.
// y : Delta between the screen Space Reflection texture last mip index and a reference last mip index.
// z : Screen Space Reflection texture last valid mip index
// w : Delta between the screen Space Reflection texture last mip index and the ray distance texture last mip index.
float4 _ScreenSpaceReflectionParam2;

// x : Contact hardening scale. Zero disables contact hardening.
// y : Contact distance bias
// zw : Unused
float4 _ScreenSpaceReflectionParam3;

TEXTURE2D_X(_ScreenSpaceReflectionRayDistanceTexture);

float GetSSRBlurriness()
{
    return _ScreenSpaceReflectionParam2.x;
}

float GetSSRTextureMipOffset()
{
    return _ScreenSpaceReflectionParam2.y;
}

float GetSSRTextureLastValidMipIndex()
{
    return _ScreenSpaceReflectionParam2.z;
}

float GetSSRRayDistanceMipOffset()
{
    return GetSSRTextureMipOffset() - _ScreenSpaceReflectionParam2.w;
}

float GetSSRRayDistanceLastValidMipIndex()
{
    return GetSSRTextureLastValidMipIndex() - _ScreenSpaceReflectionParam2.w;
}

float GetSSRContactHardeningScale()
{
    return _ScreenSpaceReflectionParam3.x;
}

float GetSSRContactDistanceBias()
{
    return _ScreenSpaceReflectionParam3.y;
}

float GetSSRBlurConeHalfAngle(float perceptualRoughness)
{
    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

    // Ref: "Moving Frostbite to PBR", p. 72. But using a different E value to match the reflection probe
    // roughness curve.
    //
    // const float e = 0.85;
    // return atan(e*roughness/(1.0 - e));

    // (Hopefully) Faster polynomial approximation of above
    float shininess = 1.0 - roughness;
    float shininess2 = shininess * shininess;
    return 0.2094 * (roughness + 5.6667 * (1.0 - shininess2 * shininess2));
}

float GetSSRContactFactor(float2 uv, float radius, float distanceToReflector)
{
    // Read the blurred ray distance and validity at the given blur radius
    const float k_MinimumRadius = 0.001;
    float guideMip = log2(max(k_MinimumRadius, radius)) + GetSSRRayDistanceMipOffset();
    guideMip = clamp(guideMip, 0, GetSSRRayDistanceLastValidMipIndex());

    float4 rayDistancesAndValidity = SAMPLE_TEXTURE2D_X_LOD(_ScreenSpaceReflectionRayDistanceTexture, sampler_TrilinearClamp, uv, guideMip);
    float blurredRayDistance = rayDistancesAndValidity.x;
    float blurredRcpRayDistance = rayDistancesAndValidity.y;
    float blurredValidity = rayDistancesAndValidity.z;
    float blurredSkyValidity = rayDistancesAndValidity.w;

    // Use these to calculate the arithmetic and harmonic mean of the ray distance.
    // Arithmetic mean biases towards large values (long distances) and harmonic mean
    // biases towards small values (short distances). We let the user tune the weighting
    // by lerping between these metrics.
    float arithmeticMeanRayDistance = blurredRayDistance * rcp(max(blurredValidity, HALF_EPS));
    float harmonicMeanRayDistance = blurredValidity * rcp(max(blurredRcpRayDistance, HALF_EPS));
    float representativeRayDistance = lerp(arithmeticMeanRayDistance, harmonicMeanRayDistance, GetSSRContactDistanceBias());

    // Divide by distance to reflector so the factor is independent of scene scale.
    float contactFactor = saturate(representativeRayDistance * rcp(distanceToReflector * GetSSRContactHardeningScale()));

    // Sky is infinitely far away, fully blurred. When the footprint partially covers the
    // sky, we lerp between the geometric contact factor and full blur.
    float totalValidity = blurredValidity + blurredSkyValidity;
    return lerp(contactFactor, 1.0, blurredSkyValidity * rcp(max(totalValidity, FLT_EPS)));
}

// Blur radius of the reflection footprint, expressed in pixels of the reference resolution.
float GetSSRBlurRadiusFromPerceptualRoughness(float3 positionWS, float perceptualRoughness, float2 uv)
{
    // Map perceptual roughness to a blur cone radius
    float blurConeAngle = GetSSRBlurConeHalfAngle(perceptualRoughness);
    float blurRadius = GetSSRBlurriness() * tan(blurConeAngle);

    // Using unity_StereoEyeIndex to index unity_StereoWorldSpaceCameraPos directly causes
    // FXC to miscompile this code. Do NOT attempt to optimize this.
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    float3 cameraPos = unity_StereoEyeIndex == 0 ? unity_StereoWorldSpaceCameraPos[0] : unity_StereoWorldSpaceCameraPos[1];
#else
    float3 cameraPos = GetCameraPositionWS();
#endif

    UNITY_BRANCH
    if (GetSSRContactHardeningScale() > 0.0)
    {
        float distanceToReflector = distance(positionWS, cameraPos);

        // Calculate a contact factor using a radius based _only_ on roughness.
        float contactFactor = GetSSRContactFactor(uv, blurRadius, distanceToReflector);

        // Refine the contact factor by adjusting the previous radius with the contact factor.
        // We could repeat this to a fixed point, but only take 1 iteration for performance reasons.
        contactFactor = GetSSRContactFactor(uv, blurRadius * contactFactor, distanceToReflector);

        blurRadius *= contactFactor;
    }

    return blurRadius;
}

float GetSSRMipLevelFromPerceptualRoughness(float3 positionWS, float perceptualRoughness, float2 uv)
{
    float blurRadius = GetSSRBlurRadiusFromPerceptualRoughness(positionWS, perceptualRoughness, uv);

    // Map this blur radius back to a mip level, but assuming the reference resolution.
    const float k_MinimumRadius = 0.001;
    float mipLevel = log2(max(k_MinimumRadius, blurRadius));

    // Adjust for resolution: shift that mip by the difference between the actual SSR mip count and the reference one,
    // so the same material roughness produces approximately the same screen-space blur in pixels regardless of
    // reflection buffer resolution. The minimum-radius clamp keeps roughness 0 at mip 0 up to resolutions of
    // k_SSRBlurReferenceResolution * 2^-log2(k_MinimumRadius) ~= 1024 * 1024 = 1M pixels wide. The subtraction below
    // keeps roughness 1 at the highest mip level.
    mipLevel += GetSSRTextureMipOffset();
    return clamp(mipLevel, 0, GetSSRTextureLastValidMipIndex());
}

#endif
