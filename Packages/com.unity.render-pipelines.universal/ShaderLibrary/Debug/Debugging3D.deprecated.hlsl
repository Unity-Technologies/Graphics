#ifndef UNIVERSAL_DEBUGGING3D_DEPRECATED_INCLUDED
#define UNIVERSAL_DEBUGGING3D_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

#if defined(DEBUG_DISPLAY)

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (Debugging3D.hlsl).

bool UpdateSurfaceAndInputDataForDebug(inout SurfaceData surfaceData, inout InputData inputData, bool normalMap);

#if !defined(_NORMALMAP_KEYWORD_DECLARED)
    #if !defined(_NORMALMAP)
        #define _NORMALMAP 0
        #define _NORMALMAP_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_NORMALMAP)
        #undef _NORMALMAP
        #define _NORMALMAP 1
    #endif
#endif

// Deprecated. Use UpdateSurfaceAndInputDataForDebug(surfaceData, inputData, normalMap).
bool UpdateSurfaceAndInputDataForDebug(inout SurfaceData surfaceData, inout InputData inputData)
{
    return UpdateSurfaceAndInputDataForDebug(surfaceData, inputData, _NORMALMAP);
}

// Prevents leaking the fallback keyword definition to shaders that include this file.
#if defined(_NORMALMAP_DEFINED_LOCALLY)
    #undef _NORMALMAP_DEFINED_LOCALLY
    #undef _NORMALMAP
#endif

#endif // defined(DEBUG_DISPLAY)

#endif // UNIVERSAL_DEBUGGING3D_DEPRECATED_INCLUDED
