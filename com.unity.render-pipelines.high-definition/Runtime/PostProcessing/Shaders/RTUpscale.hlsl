#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"


float3 Sample(TEXTURE2D_X_PARAM(_InputTexture, _InputTextureSampler), float2 UV)
{
    float2 ScaledUV = ClampAndScaleUVForPoint(UV);
    return SAMPLE_TEXTURE2D_X_LOD(_InputTexture, _InputTextureSampler, ScaledUV, 0).xyz;
}

float3 Nearest(TEXTURE2D_X(_InputTexture), float2 UV)
{
    return Sample(TEXTURE2D_X_ARGS(_InputTexture, s_point_clamp_sampler), UV);
}


float3 Bilinear(TEXTURE2D_X(_InputTexture), float2 UV)
{
    return Sample(TEXTURE2D_X_ARGS(_InputTexture, s_linear_clamp_sampler), UV);
}

float3 CatmullRomFourSamples(TEXTURE2D_X(_InputTexture), float2 UV)
{
    float2 TexSize = _ScreenSize.xy * rcp(_ScreenToTargetScale.xy);
    float4 bicubicWnd = float4(TexSize, 1.0 / (TexSize));

    return SampleTexture2DBicubic(  TEXTURE2D_X_ARGS(_InputTexture, s_linear_clamp_sampler),
                                    UV * _ScreenToTargetScale.xy,
                                    bicubicWnd,
                                    (1.0f - 0.5f * _ScreenSize.zw) * _ScreenToTargetScale.xy,
                                    unity_StereoEyeIndex).xyz;
}



// https://en.wikipedia.org/wiki/Lanczos_resampling
// TODO: Revisit derivation.
float3 Lanczos(TEXTURE2D_X(_InputTexture), float2 inUV)
{
    // Lanczos 3
    const float a = 3.0;

    float2 TexSize = _ScreenSize.xy * (_ScreenToTargetScale.xy); 
    float2 TexelSize = rcp(TexSize);
    float2 texelLoc = inUV * TexSize;
    float2 center = floor(texelLoc - 0.5) + 0.5;

    float2 x = texelLoc - center + (a - 1.0);
    float2 xPI = x * PI;

    // Note lanczos weights are sinc(x)*sinc(x/a)
    float2 sinXPI = sin(xPI);
    float2 sinXPIOverA = sin(xPI / a);
    float2 cosXPIOverA = cos(xPI / a);

    float2 sinLancz = sinXPI * sinXPIOverA;

    // This is really cosXPIOverA * sqrt(a) [We need this following trig derivation starting from pythogorean identities / angle additions].
    // But we're never going to use cosXPIOverA as is.
    const float sqrtA = 1.73205080757;
    cosXPIOverA *= sqrtA;
    float2 sinXCosXOverA = cosXPIOverA * sinXPI;


    // Find UVs
    float2 UV_2 = (center - 2) * TexelSize;
    float2 UV_1 = (center - 1) * TexelSize;
    float2 UV0 = (center)* TexelSize;
    float2 UV2 = (center + 2) * TexelSize;
    float2 UV3 = (center + 3) * TexelSize;

    // Find weights. 
    float2 xMin1 = x - 1.0;
    float2 xMin2 = x - 2.0;
    float2 xMin3 = x - 3.0;
    float2 xMin4 = x - 4.0;
    float2 xMin5 = x - 5.0;

    float2 w14 = -sinLancz + sinXCosXOverA;
    float2 w25 = -sinLancz - sinXCosXOverA;

    float2 weight0 = sinLancz        / (x  * x);
    float2 weight1 = w14             / (xMin1 * xMin1);
    float2 weight2 = w25             / (xMin2 * xMin2);
    float2 weight3 = (2.0*sinLancz) / (xMin3 * xMin3);
    float2 weight4 = w14             / (xMin4 * xMin4);
    float2 weight5 = w25             / (xMin5 * xMin5);

    float2 weight23 = weight2 + weight3; // Readapt since we are leveraging bilinear.


    // Correct UV to account for bilinear adjustment
    UV0 += (weight3 / weight23) * TexelSize;


    float4 accumulation = 0;
    // Corners are dropped (similarly to what Jimenez suggested for Bicubic) 
    accumulation += float4(Bilinear(_InputTexture, float2(UV_2.x, UV0.y)), 1) * weight0.x * weight23.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV_1.x, UV_1.y)), 1) * weight1.x * weight1.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV_1.x, UV0.y)), 1) * weight1.x * weight23.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV_1.x, UV2.y)), 1) * weight1.x * weight4.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV0.x, UV_2.y)), 1) * weight23.x * weight0.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV0.x, UV_1.y)), 1) * weight23.x * weight1.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV0.x, UV0.y)), 1) * weight23.x * weight23.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV0.x, UV2.y)), 1) * weight23.x * weight4.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV0.x, UV3.y)), 1) * weight23.x * weight5.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV2.x, UV_1.y)), 1) * weight4.x * weight1.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV2.x, UV0.y)), 1) * weight4.x * weight23.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV2.x, UV2.y)), 1) * weight4.x * weight4.y;
    accumulation += float4(Bilinear(_InputTexture, float2(UV3.x, UV0.y)), 1) * weight5.x * weight23.y;

    return accumulation.xyz /= accumulation.w;
}
