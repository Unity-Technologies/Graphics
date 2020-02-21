
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

#define HDR_MAPUNMAP        1
#define CLIP_AABB           1
#define RADIUS              0.75
#define FEEDBACK_MIN        0.96
#define FEEDBACK_MAX        0.91
#define SHARPEN             1

#define CLAMP_MAX       65472.0 // HALF_MAX minus one (2 - 2^-9) * 2^15

#if !defined(CTYPE)
    #define CTYPE float3
#endif

#if UNITY_REVERSED_Z
    #define COMPARE_DEPTH(a, b) step(b, a)
#else
    #define COMPARE_DEPTH(a, b) step(a, b)
#endif

float3 Fetch(TEXTURE2D_X(tex), float2 coords, float2 offset, float2 scale)
{
    float2 uv = (coords + offset * _ScreenSize.zw) * scale;
    return SAMPLE_TEXTURE2D_X_LOD(tex, s_linear_clamp_sampler, uv, 0).xyz;
}

float2 Fetch2(TEXTURE2D_X(tex), float2 coords, float2 offset, float2 scale)
{
    float2 uv = (coords + offset * _ScreenSize.zw) * scale;
    return SAMPLE_TEXTURE2D_X_LOD(tex, s_linear_clamp_sampler, uv, 0).xy;
}


float4 Fetch4(TEXTURE2D_X(tex), float2 coords, float2 offset, float2 scale)
{
    float2 uv = (coords + offset * _ScreenSize.zw) * scale;
    return SAMPLE_TEXTURE2D_X_LOD(tex, s_linear_clamp_sampler, uv, 0);
}

float4 Fetch4Array(Texture2DArray tex, uint slot, float2 coords, float2 offset, float2 scale)
{
    float2 uv = (coords + offset * _ScreenSize.zw) * scale;
    return SAMPLE_TEXTURE2D_ARRAY_LOD(tex, s_linear_clamp_sampler, uv, slot, 0);
}

float3 Map(float3 x)
{
    #if HDR_MAPUNMAP
    return FastTonemap(x);
    #else
    return x;
    #endif
}

float3 Unmap(float3 x)
{
    #if HDR_MAPUNMAP
    return FastTonemapInvert(x);
    #else
    return x;
    #endif
}

float MapPerChannel(float x)
{
    #if HDR_MAPUNMAP
    return FastTonemapPerChannel(x);
    #else
    return x;
    #endif
}

float UnmapPerChannel(float x)
{
    #if HDR_MAPUNMAP
    return FastTonemapPerChannelInvert(x);
    #else
    return x;
    #endif
}

float2 MapPerChannel(float2 x)
{
    #if HDR_MAPUNMAP
    return FastTonemapPerChannel(x);
    #else
    return x;
    #endif
}

float2 UnmapPerChannel(float2 x)
{
    #if HDR_MAPUNMAP
    return FastTonemapPerChannelInvert(x);
    #else
    return x;
    #endif
}

float2 GetClosestFragment(float2 positionSS)
{
    float center  = LoadCameraDepth(positionSS);
    float nw = LoadCameraDepth(positionSS + int2(-1, -1));
    float ne = LoadCameraDepth(positionSS + int2( 1, -1));
    float sw = LoadCameraDepth(positionSS + int2(-1,  1));
    float se = LoadCameraDepth(positionSS + int2( 1,  1));

    float4 neighborhood = float4(nw, ne, sw, se);

    float3 closest = float3(0.0, 0.0, center);
    closest = lerp(closest, float3(-1.0, -1.0, neighborhood.x), COMPARE_DEPTH(neighborhood.x, closest.z));
    closest = lerp(closest, float3( 1.0, -1.0, neighborhood.y), COMPARE_DEPTH(neighborhood.y, closest.z));
    closest = lerp(closest, float3(-1.0,  1.0, neighborhood.z), COMPARE_DEPTH(neighborhood.z, closest.z));
    closest = lerp(closest, float3( 1.0,  1.0, neighborhood.w), COMPARE_DEPTH(neighborhood.w, closest.z));


    return positionSS + float2(1.0, -1.0);
}

CTYPE ClipToAABB(CTYPE color, CTYPE minimum, CTYPE maximum)
{
    // note: only clips towards aabb center (but fast!)
    CTYPE center  = 0.5 * (maximum + minimum);
    CTYPE extents = 0.5 * (maximum - minimum);

    // This is actually `distance`, however the keyword is reserved
    CTYPE offset = color - center;
    
    CTYPE ts = abs(extents) / max(abs(offset), 1e-4);
    float t = saturate(Min3(ts.x, ts.y,  ts.z));
    return center + offset * t;
}

float3 ClipToAABB2(float3 color, float3 minimum, float3 maximum)
{
    // note: only clips towards aabb center (but fast!)
    float3 center = 0.5 * (maximum + minimum);
    float3 extents = 0.5 * (maximum - minimum);

    // This is actually `distance`, however the keyword is reserved
    float3 offset = color - center;

    float3 ts = abs(extents) / max(abs(offset), 1e-4);
    float t = saturate(Min3(ts.x, ts.y, ts.z));
    return center + offset * t;
}

// ---- Options to get history ----

// --------------------------------------
// Higher level options
// --------------------------------------


// --------------------------------------
// History fetching utilities
// --------------------------------------

#define LOAD 0
#define BICUBIC 1
#define HISTORY_LOAD_METHOD LOAD


float4 FetchHistoryLoad(TEXTURE2D_X(tex), float2 UV)
{
    return Fetch4(tex, UV, 0.0, _RTHandleScaleHistory.zw);
}

float4 FetchHistoryBicubic4(TEXTURE2D_X(tex), float2 UV)
{
    float2 TexSize = _ScreenSize.xy * rcp(_RTHandleScale.xy);
    float4 bicubicWnd = float4(TexSize, 1.0 / (TexSize));

    return SampleTexture2DBicubic(TEXTURE2D_X_ARGS(tex, s_linear_clamp_sampler),
        UV * _RTHandleScale.xy,
        bicubicWnd,
        (1.0f - 0.5f * _ScreenSize.zw) * _RTHandleScale.xy,
        unity_StereoEyeIndex);
}

float3 HistoryCatmull5Tap(TEXTURE2D_X(_InputTexture), float2 UV, float4 Size, float Velocity)
{
    float2 screenPos = UV * Size.xy;
    float2 centerPosition = floor(screenPos - 0.5) + 0.5;
    float2 f = screenPos - centerPosition;
    float2 f2 = f * f;

    const float   c = 0.5;  // Add sharpening
    float2 w0, w1, w2, w3;

    // Horners form of polynomial
    w1 = f * (f * (((2.0 - c) * f) - ((3.0 - c)))) + 1.0;
    w2 = f * (f * ((-(2.0 - c) * f) + ((3.0 - 2.0 * c))) + c);

    float2 w12 = w1 + w2;
    float2 rcpw12 = rcp(w12);
    float2 tc12 = Size.zw * (centerPosition + w2 * rcpw12); 
    float3 centerColor = SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_linear_clamp_sampler, float2(tc12.x, tc12.y), 0).xyz;

    w0 = f * (f * ((-c * f) + (2.0 * c)) - (c));
    w3 = f2 * (f * c - c);
    float2 tc0 = Size.zw * (centerPosition - 1.0);
    float2 tc3 = Size.zw * (centerPosition + 2.0);

    float4 color = float4(SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_linear_clamp_sampler, float2(tc12.x, tc0.y), 0).rgb, 1.0) * (w12.x * w0.y) +
        float4(SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_linear_clamp_sampler, float2(tc0.x, tc12.y), 0).rgb, 1.0) * (w0.x * w12.y) +
        float4(centerColor, 1.0) * (w12.x * w12.y) +
        float4(SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_linear_clamp_sampler, float2(tc3.x, tc12.y), 0).rgb, 1.0) * (w3.x * w12.y) +
        float4(SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_linear_clamp_sampler, float2(tc12.x, tc3.y), 0).rgb, 1.0) * (w12.x * w3.y);

    return color.rgb * rcp(color.a);
}

void PLAN()
{

     // Work in YCoCG space

    // Find closest sample and pick velocity from closest. TODO: Experiment with size of offset, maybe 2? 

    // Find reprojected velocity ? ??? << verify

    // Filter history with catmull rom?

    // Compute neighbourhood (try variance, simple min/max, distance)

    // 


}
