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

// TODO NdcToNcc and NccToNdc could be removed?

// from normalized-device-coordinates to normalized-cluster-coordinates
float2 NdcToNcc(float2 xy, float4 screenCoordScaleBias)
{
    // ndc to device-UV
    xy.y = -xy.y;
    xy = (xy + 1) * 0.5;
    xy = ScreenCoordApplyScaleBias(xy, screenCoordScaleBias);
    // cluster-UV to ncc
    xy = xy * 2 - 1;
    xy.y = -xy.y;
    return xy;
}

// from normalized-cluster-coordinates to normalized-device-coordinates
float2 NccToNdc(float2 xy, float4 screenCoordScaleBias)
{
    // ncc to cluster-UV
    xy.y = -xy.y;
    xy = (xy + 1) * 0.5;
    xy = ScreenCoordRemoveScaleBias(xy, screenCoordScaleBias);
    // device-UV to ndc
    xy = xy * 2 - 1;
    xy.y = -xy.y;
    return xy;
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
