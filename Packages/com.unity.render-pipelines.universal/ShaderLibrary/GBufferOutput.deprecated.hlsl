#ifndef UNIVERSAL_GBUFFEROUTPUT_DEPRECATED_INCLUDED
#define UNIVERSAL_GBUFFEROUTPUT_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDFData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferFragOutput.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (GBufferOutput.hlsl).

GBufferFragOutput PackGBuffersSurfaceData(SurfaceData surfaceData, InputData inputData, half3 globalIllumination, bool receiveShadows);
GBufferFragOutput PackGBuffersBRDFData(BRDFData brdfData, InputData inputData, half smoothness, half3 globalIllumination, half occlusion, bool receiveShadows, bool specularSetup, bool specularHighlights);

#if !defined(_RECEIVE_SHADOWS_OFF_KEYWORD_DECLARED)
    #if !defined(_RECEIVE_SHADOWS_OFF)
        #define _RECEIVE_SHADOWS_OFF 0
        #define _RECEIVE_SHADOWS_OFF_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_RECEIVE_SHADOWS_OFF)
        #undef _RECEIVE_SHADOWS_OFF
        #define _RECEIVE_SHADOWS_OFF 1
    #endif
#endif

#if !defined(_SPECULARHIGHLIGHTS_OFF_KEYWORD_DECLARED)
    #if !defined(_SPECULARHIGHLIGHTS_OFF)
        #define _SPECULARHIGHLIGHTS_OFF 0
        #define _SPECULARHIGHLIGHTS_OFF_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SPECULARHIGHLIGHTS_OFF)
        #undef _SPECULARHIGHLIGHTS_OFF
        #define _SPECULARHIGHLIGHTS_OFF 1
    #endif
#endif

#if !defined(_SPECULAR_SETUP_KEYWORD_DECLARED)
    #if !defined(_SPECULAR_SETUP)
        #define _SPECULAR_SETUP 0
        #define _SPECULAR_SETUP_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SPECULAR_SETUP)
        #undef _SPECULAR_SETUP
        #define _SPECULAR_SETUP 1
    #endif
#endif

// Deprecated. Use PackGBuffersSurfaceData(surfaceData, inputData, globalIllumination, receiveShadows).
GBufferFragOutput PackGBuffersSurfaceData(SurfaceData surfaceData, InputData inputData, half3 globalIllumination)
{
    return PackGBuffersSurfaceData(surfaceData, inputData, globalIllumination, !_RECEIVE_SHADOWS_OFF);
}

// Deprecated. Use PackGBuffersBRDFData(brdfData, inputData, smoothness, globalIllumination, occlusion, receiveShadows, specularSetup, specularHighlights).
GBufferFragOutput PackGBuffersBRDFData(BRDFData brdfData, InputData inputData, half smoothness, half3 globalIllumination, half occlusion = 1.0)
{
    return PackGBuffersBRDFData(brdfData, inputData, smoothness, globalIllumination, occlusion, !_RECEIVE_SHADOWS_OFF, _SPECULAR_SETUP, !_SPECULARHIGHLIGHTS_OFF);
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_RECEIVE_SHADOWS_OFF_DEFINED_LOCALLY)
    #undef _RECEIVE_SHADOWS_OFF_DEFINED_LOCALLY
    #undef _RECEIVE_SHADOWS_OFF
#endif

#if defined(_SPECULARHIGHLIGHTS_OFF_DEFINED_LOCALLY)
    #undef _SPECULARHIGHLIGHTS_OFF_DEFINED_LOCALLY
    #undef _SPECULARHIGHLIGHTS_OFF
#endif

#if defined(_SPECULAR_SETUP_DEFINED_LOCALLY)
    #undef _SPECULAR_SETUP_DEFINED_LOCALLY
    #undef _SPECULAR_SETUP
#endif

#endif // UNIVERSAL_GBUFFEROUTPUT_DEPRECATED_INCLUDED
