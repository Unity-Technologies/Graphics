// Cluster Display Uniforms And Utilities
#ifndef UNITY_GLOBAL_SCREEN_SPACE_INCLUDED
#define UNITY_GLOBAL_SCREEN_SPACE_INCLUDED

// row 0: translation2d, scale 2d
// row 1: global screensize (xy) and its reciprocate (zw)
float4x4 _GlobalScreenSpaceParams;

float2 ScreenSpaceLocalToGlobalUV(float2 uv)
{
    return uv * _GlobalScreenSpaceParams[0].zw + _GlobalScreenSpaceParams[0].xy;
}

float2 ScreenSpaceGlobalToLocalUV(float2 uv)
{
    return (uv - _GlobalScreenSpaceParams[0].xy) / _GlobalScreenSpaceParams[0].zw;
}

float2 ClipSpaceLocalToGlobal(float2 pos)
{
	pos.y *= -1; // flip Y
	pos = (pos + 1) * 0.5; // [0, 1] local
	pos = ScreenSpaceLocalToGlobalUV(pos); // [0, 1] global
    return pos * 2 - 1; // [-1, 1] global
}

float2 ClipSpaceGlobalToLocal(float2 pos)
{
	pos = (pos + 1) * 0.5; // [0, 1] global
    pos = ScreenSpaceGlobalToLocalUV(pos); // [0, 1] local
    pos = pos * 2 - 1; // [-1, 1] local
    pos.y *= -1; // flip Y
    return pos;
}

// USING_GLOBAL_SCREEN_SPACE is enabled from Cluster Rendering Package
#if defined(USING_GLOBAL_SCREEN_SPACE)
    #define SCREEN_SPACE_GLOBAL_UV(uv) (ScreenSpaceLocalToGlobalUV(uv))
    #define SCREEN_SPACE_LOCAL_UV(uv)  (ScreenSpaceGlobalToLocalUV(uv))
    #define CLIP_SPACE_GLOBAL(pos)     (ClipSpaceLocalToGlobal(pos))
    #define CLIP_SPACE_LOCAL(pos)      (ClipSpaceGlobalToLocal(pos))
    #define ScreenSize _GlobalScreenSpaceParams[1]
#else
    #define SCREEN_SPACE_GLOBAL_UV(uv) uv
    #define SCREEN_SPACE_LOCAL_UV(uv) uv
    #define CLIP_SPACE_GLOBAL(pos) pos
    #define CLIP_SPACE_LOCAL(pos) pos
    #define ScreenSize _ScreenSize
#endif

#endif // UNITY_GLOBAL_SCREEN_SPACE_INCLUDED
