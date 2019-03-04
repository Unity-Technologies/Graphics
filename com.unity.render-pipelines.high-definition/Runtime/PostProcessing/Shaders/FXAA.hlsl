#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#define FXAA_HDR_MAPUNMAP   0
#define FXAA_SPAN_MAX       (8.0)
#define FXAA_REDUCE_MUL     (1.0 / 8.0)
#define FXAA_REDUCE_MIN     (1.0 / 128.0)

float3 Fetch(TEXTURE2D_X_PARAM(_InputTexture, _InputTextureSampler), float2 coords, float2 offset)
{
    float2 uv = saturate(coords + offset) * _ScreenToTargetScale.xy;
    return SAMPLE_TEXTURE2D_X_LOD(_InputTexture, _InputTextureSampler, uv, 0.0).xyz;
}

float3 Load(TEXTURE2D_X(_InputTexture), int2 icoords, int idx, int idy)
{
    return LOAD_TEXTURE2D_X(_InputTexture, min(icoords + int2(idx, idy), _ScreenSize.xy - 1.0)).xyz;
}

void RunFXAA(TEXTURE2D_X_PARAM(_InputTexture, _InputTextureSampler), inout float3 outColor, uint2 positionSS, float2 positionNDC)
{
    {
        // Edge detection
        float3 rgbNW = Load(_InputTexture, positionSS, -1, -1);
        float3 rgbNE = Load(_InputTexture, positionSS, 1, -1);
        float3 rgbSW = Load(_InputTexture, positionSS, -1, 1);
        float3 rgbSE = Load(_InputTexture, positionSS, 1, 1);

#if !FXAA_HDR_MAPUNMAP
        rgbNW = saturate(rgbNW);
        rgbNE = saturate(rgbNE);
        rgbSW = saturate(rgbSW);
        rgbSE = saturate(rgbSE);
        outColor = saturate(outColor);
#endif

        float lumaNW = Luminance(rgbNW);
        float lumaNE = Luminance(rgbNE);
        float lumaSW = Luminance(rgbSW);
        float lumaSE = Luminance(rgbSE);
        float lumaM = Luminance(outColor);

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
        outColor = FastTonemapInvert(rgb);
#else
        outColor = rgb;
#endif
    }
}
