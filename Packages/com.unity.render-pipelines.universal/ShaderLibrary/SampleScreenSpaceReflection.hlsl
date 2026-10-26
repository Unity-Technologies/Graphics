#ifndef SAMPLE_SCREEN_SPACE_REFLECTION_INCLUDED
#define SAMPLE_SCREEN_SPACE_REFLECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ScreenSpaceReflectionCommon.hlsl"

// SSR reflection color
TEXTURE2D_X(_ScreenSpaceReflectionTexture);

half4 SampleScreenSpaceReflection(float2 normalizedScreenSpaceUV, float3 positionWS, float perceptualRoughness)
{
    float2 uv = UnityStereoTransformScreenSpaceTex(normalizedScreenSpaceUV);

    // Fade out reflections for pixels that have smoothness below our minimum.
    float perceptualSmoothness = PerceptualRoughnessToPerceptualSmoothness(perceptualRoughness);
    float fadeStart = _ScreenSpaceReflectionParam.y;
    float fadeEnd = _ScreenSpaceReflectionParam.z;
    float fade = smoothstep(fadeStart, fadeEnd, perceptualSmoothness);
    float4 reflColor = float4(0,0,0,0);

    if (fade > 1e-3)
    {
        // Map roughness to mip level to get blur.
        float mipLevel = GetSSRMipLevelFromPerceptualRoughness(positionWS, perceptualRoughness, uv);
        reflColor = SAMPLE_TEXTURE2D_X_LOD(_ScreenSpaceReflectionTexture, sampler_TrilinearClamp, uv, mipLevel);
        if (reflColor.a > 0)
            reflColor.rgb *= rcp(reflColor.a);

        reflColor.a *= fade;
    }

    return reflColor;
}

half4 GetScreenSpaceReflection(float2 normalizedScreenSpaceUV, float3 positionWS, float perceptualRoughness)
{
#if _SCREEN_SPACE_REFLECTION_KEYWORD_DECLARED
    if (_SCREEN_SPACE_REFLECTION)
        return SampleScreenSpaceReflection(normalizedScreenSpaceUV, positionWS, perceptualRoughness) * _ScreenSpaceReflectionParam.x;
    else
#endif
    return 0;
}

#endif
