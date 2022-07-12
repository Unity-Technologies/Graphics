#ifndef UNITY_SCREEN_COORD_OVERRIDE_INCLUDED
#define UNITY_SCREEN_COORD_OVERRIDE_INCLUDED

float2 ScreenCoordApplyScaleBias(float2 xy, float4x4 screenCoordScaleBias, int instanceID)
{
    return screenCoordScaleBias[instanceID].zw + xy * screenCoordScaleBias[instanceID].xy;
}

float2 ScreenCoordRemoveScaleBias(float2 xy, float4x4 screenCoordScaleBias, int instanceID)
{
    return (xy - screenCoordScaleBias[instanceID].zw) / screenCoordScaleBias[instanceID].xy;
}

// Note that SCREEN_SIZE_OVERRIDE will be redefined in HDRP to use _PostProcessScreenSize.
#if defined(SCREEN_COORD_OVERRIDE)
    #define SCREEN_COORD_APPLY_SCALEBIAS(xy, instanceID)          ScreenCoordApplyScaleBias(xy, _ScreenCoordScaleBias, instanceID)
    #define SCREEN_COORD_REMOVE_SCALEBIAS(xy, instanceID)         ScreenCoordRemoveScaleBias(xy, _ScreenCoordScaleBias, instanceID)
    #define SCREEN_SIZE_OVERRIDE                     _ScreenSizeOverride
#else
    #define SCREEN_COORD_APPLY_SCALEBIAS(xy, instanceID)          xy
    #define SCREEN_COORD_REMOVE_SCALEBIAS(xy, instanceID)         xy
    #define SCREEN_SIZE_OVERRIDE                     _ScreenSize
#endif

#endif // UNITY_SCREEN_COORD_OVERRIDE_INCLUDED
