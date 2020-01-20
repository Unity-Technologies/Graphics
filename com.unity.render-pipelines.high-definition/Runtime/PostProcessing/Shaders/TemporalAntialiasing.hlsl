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

    return positionSS + closest.xy;
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
