#ifndef UNITY_SCREEN_COORD_OVERRIDE_HDRP_INCLUDED
#define UNITY_SCREEN_COORD_OVERRIDE_HDRP_INCLUDED

#ifndef UNITY_SCREEN_COORD_OVERRIDE_INCLUDED
    #error UNITY_SCREEN_COORD_OVERRIDE_INCLUDED not defined, you should include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ScreenCoordOverride.hlsl"
#else
    // We must redefine SCREEN_SIZE_OVERRIDE on HDRP to use _PostProcessScreenSize.
    #undef SCREEN_SIZE_OVERRIDE
    #if defined(SCREEN_COORD_OVERRIDE)
        #define SCREEN_SIZE_OVERRIDE _ScreenSizeOverride
    #else
        #define SCREEN_SIZE_OVERRIDE _PostProcessScreenSize
    #endif
#endif

#endif // UNITY_SCREEN_COORD_OVERRIDE_HDRP_INCLUDED
