#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/PostProcessDefines.hlsl"

#define FXAA_HDR_MAPUNMAP   0
#define FXAA_SPAN_MAX       (8.0)
#define FXAA_REDUCE_MUL     (1.0 / 8.0)
#define FXAA_REDUCE_MIN     (1.0 / 128.0)

float3 Fetch(TEXTURE2D_X_PARAM(_InputTexture, _InputTextureSampler), float2 coords, float2 offset)
{
    float2 uv = saturate(coords + offset) * _RTHandleScale.xy;
    return SAMPLE_TEXTURE2D_X_LOD(_InputTexture, _InputTextureSampler, uv, 0.0).xyz;
}

CTYPE Load(TEXTURE2D_X(_InputTexture), int2 icoords, int idx, int idy)
{
    return LOAD_TEXTURE2D_X(_InputTexture, min(icoords + int2(idx, idy), _ScreenSize.xy - 1.0)).CTYPE_SWIZZLE;
}

float FetchAlpha(TEXTURE2D_X_PARAM(_InputTexture, _InputTextureSampler), float2 coords, float2 offset)
{
    float2 uv = saturate(coords + offset) * _RTHandleScale.xy;
    return SAMPLE_TEXTURE2D_X_LOD(_InputTexture, _InputTextureSampler, uv, 0.0).w;
}

void RunFXAA(TEXTURE2D_X_PARAM(_InputTexture, _InputTextureSampler), inout CTYPE outColor, uint2 positionSS, float2 positionNDC)
{
    {
        // Edge detection
        CTYPE rgbNW = Load(_InputTexture, positionSS, -1, -1);
        CTYPE rgbNE = Load(_InputTexture, positionSS, 1, -1);
        CTYPE rgbSW = Load(_InputTexture, positionSS, -1, 1);
        CTYPE rgbSE = Load(_InputTexture, positionSS, 1, 1);

#if !FXAA_HDR_MAPUNMAP
        rgbNW.xyz = saturate(rgbNW.xyz);
        rgbNE.xyz = saturate(rgbNE.xyz);
        rgbSW.xyz = saturate(rgbSW.xyz);
        rgbSE.xyz = saturate(rgbSE.xyz);
        outColor.xyz = saturate(outColor.xyz);
#endif

        float lumaNW = Luminance(rgbNW.xyz);
        float lumaNE = Luminance(rgbNE.xyz);
        float lumaSW = Luminance(rgbSW.xyz);
        float lumaSE = Luminance(rgbSE.xyz);
        float lumaM = Luminance(outColor.xyz);

        float2 dir;
        dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
        dir.y = ((lumaNW + lumaSW) - (lumaNE + lumaSE));

        float lumaSum = lumaNW + lumaNE + lumaSW + lumaSE;
        float dirReduce = max(lumaSum * (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);
        float rcpDirMin = rcp(min(abs(dir.x), abs(dir.y)) + dirReduce);

        dir = min((FXAA_SPAN_MAX).xx, max((-FXAA_SPAN_MAX).xx, dir * rcpDirMin)) * _ScreenSize.zw;

        // Blur
        float3 rgb03 = Fetch(TEXTURE2D_X_ARGS(_InputTexture, _InputTextureSampler), positionNDC, dir * (0.0 / 3.0 - 0.5));
        float3 rgb13 = Fetch(TEXTURE2D_X_ARGS(_InputTexture, _InputTextureSampler), positionNDC, dir * (1.0 / 3.0 - 0.5));
        float3 rgb23 = Fetch(TEXTURE2D_X_ARGS(_InputTexture, _InputTextureSampler), positionNDC, dir * (2.0 / 3.0 - 0.5));
        float3 rgb33 = Fetch(TEXTURE2D_X_ARGS(_InputTexture, _InputTextureSampler), positionNDC, dir * (3.0 / 3.0 - 0.5));

#if FXAA_HDR_MAPUNMAP
        rgb03 = FastTonemap(rgb03);
        rgb13 = FastTonemap(rgb13);
        rgb23 = FastTonemap(rgb23);
        rgb33 = FastTonemap(rgb33);
#else
        rgb03 = saturate(rgb03);
        rgb13 = saturate(rgb13);
        rgb23 = saturate(rgb23);
        rgb33 = saturate(rgb33);
#endif

        float3 rgbA = 0.5 * (rgb13 + rgb23);
        float3 rgbB = rgbA * 0.5 + 0.25 * (rgb03 + rgb33);

        float lumaB = Luminance(rgbB);

        float lumaMin = Min3(lumaM, lumaNW, Min3(lumaNE, lumaSW, lumaSE));
        float lumaMax = Max3(lumaM, lumaNW, Max3(lumaNE, lumaSW, lumaSE));

        float3 rgb = ((lumaB < lumaMin) || (lumaB > lumaMax)) ? rgbA : rgbB;

#if FXAA_HDR_MAPUNMAP
        outColor.xyz = FastTonemapInvert(rgb);
#else
        outColor.xyz = rgb;
#endif

#ifdef ENABLE_ALPHA
        // FXAA for the alpha channel: alpha can be completely decorelated from the RGB channels, so we might fetch different neighbors for the alpha!
        // For this reason we have to recompute the fetch direction
        lumaNW = rgbNW.w;
        lumaNE = rgbNE.w;
        lumaSW = rgbSW.w;
        lumaSE = rgbSE.w;
        lumaM = outColor.w;

        dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
        dir.y = ((lumaNW + lumaSW) - (lumaNE + lumaSE));

        lumaSum = lumaNW + lumaNE + lumaSW + lumaSE;
        dirReduce = max(lumaSum * (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);
        rcpDirMin = rcp(min(abs(dir.x), abs(dir.y)) + dirReduce);

        dir = min((FXAA_SPAN_MAX).xx, max((-FXAA_SPAN_MAX).xx, dir * rcpDirMin)) * _ScreenSize.zw;

        // Blur
        float a03 = FetchAlpha(TEXTURE2D_X_ARGS(_InputTexture, _InputTextureSampler), positionNDC, dir * (0.0 / 3.0 - 0.5));
        float a13 = FetchAlpha(TEXTURE2D_X_ARGS(_InputTexture, _InputTextureSampler), positionNDC, dir * (1.0 / 3.0 - 0.5));
        float a23 = FetchAlpha(TEXTURE2D_X_ARGS(_InputTexture, _InputTextureSampler), positionNDC, dir * (2.0 / 3.0 - 0.5));
        float a33 = FetchAlpha(TEXTURE2D_X_ARGS(_InputTexture, _InputTextureSampler), positionNDC, dir * (3.0 / 3.0 - 0.5));

        a03 = saturate(a03);
        a13 = saturate(a13);
        a23 = saturate(a23);
        a33 = saturate(a33);

        float A = 0.5 * (a13 + a23);
        float B = A * 0.5 + 0.25 * (a03 + a33);

        lumaMin = Min3(lumaM, lumaNW, Min3(lumaNE, lumaSW, lumaSE));
        lumaMax = Max3(lumaM, lumaNW, Max3(lumaNE, lumaSW, lumaSE));

        outColor.w = ((lumaB < lumaMin) || (lumaB > lumaMax)) ? A : B;
#endif

    }
}
