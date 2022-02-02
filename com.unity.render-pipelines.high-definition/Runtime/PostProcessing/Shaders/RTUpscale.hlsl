#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/PostProcessDefines.hlsl"

CTYPE Sample(TEXTURE2D_X_PARAM(_InputTexture, _InputTextureSampler), float2 UV)
{
    float2 ScaledUV = ClampAndScaleUVForPoint(UV);
    return SAMPLE_TEXTURE2D_X_LOD(_InputTexture, _InputTextureSampler, ScaledUV, 0).CTYPE_SWIZZLE;
}

CTYPE Nearest(TEXTURE2D_X(_InputTexture), float2 UV)
{
    return Sample(TEXTURE2D_X_ARGS(_InputTexture, s_point_clamp_sampler), UV);
}

CTYPE CatmullRomFourSamples(TEXTURE2D_X(_InputTexture), float2 UV)
{
    float2 TexSize = _ScreenSize.xy * rcp(_RTHandleScale.xy);
    float4 bicubicWnd = float4(TexSize, 1.0 / (TexSize));

    return SampleTexture2DBicubic(  TEXTURE2D_X_ARGS(_InputTexture, s_linear_clamp_sampler),
                                    UV * _RTHandleScale.xy,
                                    bicubicWnd,
                                    (1.0f - 0.5f * _ScreenSize.zw) * _RTHandleScale.xy,
                                    unity_StereoEyeIndex).CTYPE_SWIZZLE;
}
