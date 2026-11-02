#ifndef UNIVERSAL_BRDF_DEPRECATED_INCLUDED
#define UNIVERSAL_BRDF_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDFData.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (BRDF.hlsl).

BRDFData InitializeBRDFDataDirect(half3 albedo, half3 diffuse, half3 specular, half reflectivity, half oneMinusReflectivity, half smoothness, half alpha, bool alphaPremultiplyEnabled);
BRDFData InitializeBRDFData(half3 albedo, half metallic, half3 specular, half smoothness, half alpha, bool specularSetup, bool alphaPremultiplyEnabled);
BRDFData InitializeBRDFData(SurfaceData surfaceData, bool specularSetup, bool alphaPremultiplyEnabled);
BRDFData CreateEmptyBRDFData();
BRDFData InitializeBRDFDataClearCoat(half clearCoatMask, half clearCoatSmoothness, inout BRDFData baseBRDFData);
half3 DirectBRDF(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS, bool specularHighlights);

#if !defined(_ALPHAPREMULTIPLY_ON_KEYWORD_DECLARED)
    #if !defined(_ALPHAPREMULTIPLY_ON)
        #define _ALPHAPREMULTIPLY_ON 0
        #define _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_ALPHAPREMULTIPLY_ON)
        #undef _ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON 1
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

#if !defined(_CLEARCOAT_KEYWORD_DECLARED)
    #if !defined(_CLEARCOAT)
        #define _CLEARCOAT 0
        #define _CLEARCOAT_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_CLEARCOAT)
        #undef _CLEARCOAT
        #define _CLEARCOAT 1
    #endif
#endif

#if !defined(_CLEARCOATMAP_KEYWORD_DECLARED)
    #if !defined(_CLEARCOATMAP)
        #define _CLEARCOATMAP 0
        #define _CLEARCOATMAP_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_CLEARCOATMAP)
        #undef _CLEARCOATMAP
        #define _CLEARCOATMAP 1
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

// Deprecated. Use the BRDFData-returning InitializeBRDFDataDirect(..., alphaPremultiplyEnabled).
inline void InitializeBRDFDataDirect(half3 albedo, half3 diffuse, half3 specular, half reflectivity, half oneMinusReflectivity, half smoothness, inout half alpha, out BRDFData outBRDFData)
{
    outBRDFData = InitializeBRDFDataDirect(albedo, diffuse, specular, reflectivity, oneMinusReflectivity, smoothness, alpha, _ALPHAPREMULTIPLY_ON);
}

// Legacy: do not call, will not correctly initialize albedo property.
inline void InitializeBRDFDataDirect(half3 diffuse, half3 specular, half reflectivity, half oneMinusReflectivity, half smoothness, inout half alpha, out BRDFData outBRDFData)
{
    outBRDFData = InitializeBRDFDataDirect(half3(0.0, 0.0, 0.0), diffuse, specular, reflectivity, oneMinusReflectivity, smoothness, alpha, _ALPHAPREMULTIPLY_ON);
}

// Deprecated. Use the BRDFData-returning InitializeBRDFData(..., specularSetup, alphaPremultiplyEnabled).
inline void InitializeBRDFData(half3 albedo, half metallic, half3 specular, half smoothness, inout half alpha, out BRDFData outBRDFData)
{
    outBRDFData = InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, _SPECULAR_SETUP, _ALPHAPREMULTIPLY_ON);
}

// Deprecated. Use the BRDFData-returning InitializeBRDFData(..., specularSetup, alphaPremultiplyEnabled).
inline void InitializeBRDFData(inout SurfaceData surfaceData, out BRDFData brdfData)
{
    brdfData = InitializeBRDFData(surfaceData, _SPECULAR_SETUP, _ALPHAPREMULTIPLY_ON);
}

// Deprecated. Use the BRDFData-returning InitializeBRDFDataClearCoat(clearCoatMask, clearCoatSmoothness, baseBRDFData).
inline void InitializeBRDFDataClearCoat(half clearCoatMask, half clearCoatSmoothness, inout BRDFData baseBRDFData, out BRDFData outBRDFData)
{
    outBRDFData = InitializeBRDFDataClearCoat(clearCoatMask, clearCoatSmoothness, baseBRDFData);
}

// Deprecated. Gate InitializeBRDFDataClearCoat at the call site instead.
BRDFData CreateClearCoatBRDFData(SurfaceData surfaceData, inout BRDFData brdfData)
{
    BRDFData brdfDataClearCoat = CreateEmptyBRDFData();
    if (_CLEARCOAT || _CLEARCOATMAP)
        brdfDataClearCoat = InitializeBRDFDataClearCoat(surfaceData.clearCoatMask, surfaceData.clearCoatSmoothness, brdfData);
    return brdfDataClearCoat;
}

// Deprecated. Use DirectBRDF(..., specularHighlights).
half3 DirectBRDF(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
    return DirectBRDF(brdfData, normalWS, lightDirectionWS, viewDirectionWS, !_SPECULARHIGHLIGHTS_OFF);
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY)
    #undef _ALPHAPREMULTIPLY_ON_DEFINED_LOCALLY
    #undef _ALPHAPREMULTIPLY_ON
#endif

#if defined(_SPECULARHIGHLIGHTS_OFF_DEFINED_LOCALLY)
    #undef _SPECULARHIGHLIGHTS_OFF_DEFINED_LOCALLY
    #undef _SPECULARHIGHLIGHTS_OFF
#endif

#if defined(_CLEARCOAT_DEFINED_LOCALLY)
    #undef _CLEARCOAT_DEFINED_LOCALLY
    #undef _CLEARCOAT
#endif

#if defined(_CLEARCOATMAP_DEFINED_LOCALLY)
    #undef _CLEARCOATMAP_DEFINED_LOCALLY
    #undef _CLEARCOATMAP
#endif

#if defined(_SPECULAR_SETUP_DEFINED_LOCALLY)
    #undef _SPECULAR_SETUP_DEFINED_LOCALLY
    #undef _SPECULAR_SETUP
#endif

#endif // UNIVERSAL_BRDF_DEPRECATED_INCLUDED
