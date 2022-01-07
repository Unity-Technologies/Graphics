#ifndef UNITY_SCREEN_COORD_OVERRIDE_INCLUDED
#define UNITY_SCREEN_COORD_OVERRIDE_INCLUDED

float2 ScreenCoordApplyScaleBias(float2 xy, float4 screenCoordScaleBias)
{
    return screenCoordScaleBias.zw + xy * screenCoordScaleBias.xy;
}

float2 ScreenCoordRemoveScaleBias(float2 xy, float4 screenCoordScaleBias)
{
    return (xy - screenCoordScaleBias.zw) / screenCoordScaleBias.xy;
}

#if defined(SCREEN_COORD_OVERRIDE)
    #define SCREEN_COORD_APPLY_SCALEBIAS(xy)          ScreenCoordApplyScaleBias(xy, _ScreenCoordScaleBias)
    #define SCREEN_COORD_REMOVE_SCALEBIAS(xy)         ScreenCoordRemoveScaleBias(xy, _ScreenCoordScaleBias)
    #define SCREEN_SIZE_OVERRIDE                     _ScreenSizeOverride
#else
    #define SCREEN_COORD_APPLY_SCALEBIAS(xy)          xy
    #define SCREEN_COORD_REMOVE_SCALEBIAS(xy)         xy
    #define SCREEN_SIZE_OVERRIDE                     _ScreenSize
#endif

#endif // UNITY_SCREEN_COORD_OVERRIDE_INCLUDED
