#define HDR_MAPUNMAP        1
#define CLIP_AABB           1
#define RADIUS              0.75
#define FEEDBACK_MIN        0.96
#define FEEDBACK_MAX        0.91
#define SHARPEN             1
#define SHARPEN_STRENGTH    0.6

#define CLAMP_MAX       65472.0 // HALF_MAX minus one (2 - 2^-9) * 2^15

#if UNITY_REVERSED_Z
    #define COMPARE_DEPTH(a, b) step(b, a)
#else
    #define COMPARE_DEPTH(a, b) step(a, b)
#endif

SAMPLER(sampler_LinearClamp);

float3 Fetch(TEXTURE2D_X(tex), float2 coords, float2 offset, float2 scale)
{
    float2 uv = (coords + offset * _ScreenSize.zw) * scale;
    return SAMPLE_TEXTURE2D_X_LOD(tex, sampler_LinearClamp, uv, 0).xyz;
}

float2 Fetch2(TEXTURE2D_X(tex), float2 coords, float2 offset, float2 scale)
{
    float2 uv = (coords + offset * _ScreenSize.zw) * scale;
    return SAMPLE_TEXTURE2D_X_LOD(tex, sampler_LinearClamp, uv, 0).xy;
}


float4 Fetch4(TEXTURE2D_X(tex), float2 coords, float2 offset, float2 scale)
{
    float2 uv = (coords + offset * _ScreenSize.zw) * scale;
    return SAMPLE_TEXTURE2D_X_LOD(tex, sampler_LinearClamp, uv, 0);
}

float2 Fetch4Array(Texture2DArray tex, uint slot, float2 coords, float2 offset, float2 scale)
{
    float2 uv = (coords + offset * _ScreenSize.zw) * scale;
    return SAMPLE_TEXTURE2D_ARRAY_LOD(tex, sampler_LinearClamp, uv, slot, 0).xy;
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

float2 GetClosestFragment(PositionInputs posInputs)
{
    float center  = LoadCameraDepth(posInputs.positionSS);
    float nw = LoadCameraDepth(posInputs.positionSS + int2(-1, -1));
    float ne = LoadCameraDepth(posInputs.positionSS + int2( 1, -1));
    float sw = LoadCameraDepth(posInputs.positionSS + int2(-1,  1));
    float se = LoadCameraDepth(posInputs.positionSS + int2( 1,  1));

    float4 neighborhood = float4(nw, ne, sw, se);

    float3 closest = float3(0.0, 0.0, center);
    closest = lerp(closest, float3(-1.0, -1.0, neighborhood.x), COMPARE_DEPTH(neighborhood.x, closest.z));
    closest = lerp(closest, float3( 1.0, -1.0, neighborhood.y), COMPARE_DEPTH(neighborhood.y, closest.z));
    closest = lerp(closest, float3(-1.0,  1.0, neighborhood.z), COMPARE_DEPTH(neighborhood.z, closest.z));
    closest = lerp(closest, float3( 1.0,  1.0, neighborhood.w), COMPARE_DEPTH(neighborhood.w, closest.z));

    return posInputs.positionSS + closest.xy;
}

float3 ClipToAABB(float3 color, float3 minimum, float3 maximum)
{
    // note: only clips towards aabb center (but fast!)
    float3 center  = 0.5 * (maximum + minimum);
    float3 extents = 0.5 * (maximum - minimum);

    // This is actually `distance`, however the keyword is reserved
    float3 offset = color - center;
    
    float3 ts = abs(extents) / max(abs(offset), 1e-4);
    float t = saturate(Min3(ts.x, ts.y,  ts.z));
    return center + offset * t;
}
