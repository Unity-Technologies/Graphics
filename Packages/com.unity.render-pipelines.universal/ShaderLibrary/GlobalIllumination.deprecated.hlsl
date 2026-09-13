#ifndef UNIVERSAL_GLOBAL_ILLUMINATION_DEPRECATED_INCLUDED
#define UNIVERSAL_GLOBAL_ILLUMINATION_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDFData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Light.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ExposureFunctions.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (GlobalIllumination.hlsl).

URP_LIGHT_ACCUM3 GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    URP_LIGHT_ACCUM3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV, bool clearCoatEnabled, bool environmentReflections);
URP_LIGHT_ACCUM3 GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    URP_LIGHT_ACCUM3 bakedGI, half occlusion,
    half3 normalWS, half3 viewDirectionWS, bool clearCoatEnabled, bool environmentReflections);
half3 GlossyEnvironmentReflection(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion, float2 normalizedScreenSpaceUV, bool environmentReflections);
half3 GlossyEnvironmentReflection(half3 reflectVector, half perceptualRoughness, half occlusion, bool environmentReflections);

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

#if !defined(_ENVIRONMENTREFLECTIONS_OFF_KEYWORD_DECLARED)
    #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
        #define _ENVIRONMENTREFLECTIONS_OFF 0
        #define _ENVIRONMENTREFLECTIONS_OFF_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_ENVIRONMENTREFLECTIONS_OFF)
        #undef _ENVIRONMENTREFLECTIONS_OFF
        #define _ENVIRONMENTREFLECTIONS_OFF 1
    #endif
#endif

// Deprecated. Use GlobalIllumination(..., normalizedScreenSpaceUV, clearCoatEnabled, environmentReflections).
URP_LIGHT_ACCUM3 GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    URP_LIGHT_ACCUM3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV)
{
    return GlobalIllumination(brdfData, brdfDataClearCoat, clearCoatMask, bakedGI, occlusion, positionWS, normalWS, viewDirectionWS, normalizedScreenSpaceUV, _CLEARCOAT || _CLEARCOATMAP, !_ENVIRONMENTREFLECTIONS_OFF);
}

#if !USE_CLUSTER_LIGHT_LOOP
// Deprecated. Use GlobalIllumination(..., normalizedScreenSpaceUV, clearCoatEnabled, environmentReflections).
URP_LIGHT_ACCUM3 GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    URP_LIGHT_ACCUM3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS)
{
    return GlobalIllumination(brdfData, brdfDataClearCoat, clearCoatMask, bakedGI, occlusion, positionWS, normalWS, viewDirectionWS, float2(0.0f, 0.0f), _CLEARCOAT || _CLEARCOATMAP, !_ENVIRONMENTREFLECTIONS_OFF);
}
#endif

// Deprecated. Use GlobalIllumination(..., clearCoatEnabled, environmentReflections).
URP_LIGHT_ACCUM3 GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    URP_LIGHT_ACCUM3 bakedGI, half occlusion,
    half3 normalWS, half3 viewDirectionWS)
{
    return GlobalIllumination(brdfData, brdfDataClearCoat, clearCoatMask, bakedGI, occlusion, normalWS, viewDirectionWS, _CLEARCOAT || _CLEARCOATMAP, !_ENVIRONMENTREFLECTIONS_OFF);
}

// Deprecated. Use GlobalIllumination(..., normalizedScreenSpaceUV, clearCoatEnabled, environmentReflections).
URP_LIGHT_ACCUM3 GlobalIllumination(BRDFData brdfData, URP_LIGHT_ACCUM3 bakedGI, half occlusion, float3 positionWS, half3 normalWS, half3 viewDirectionWS)
{
    const BRDFData noClearCoat = (BRDFData)0;
    return GlobalIllumination(brdfData, noClearCoat, 0.0, bakedGI, occlusion, positionWS, normalWS, viewDirectionWS, float2(0.0f, 0.0f), false, !_ENVIRONMENTREFLECTIONS_OFF);
}

// Deprecated. Use GlobalIllumination(..., clearCoatEnabled, environmentReflections).
URP_LIGHT_ACCUM3 GlobalIllumination(BRDFData brdfData, URP_LIGHT_ACCUM3 bakedGI, half occlusion, half3 normalWS, half3 viewDirectionWS)
{
    const BRDFData noClearCoat = (BRDFData)0;
    return GlobalIllumination(brdfData, noClearCoat, 0.0, bakedGI, occlusion, normalWS, viewDirectionWS, false, !_ENVIRONMENTREFLECTIONS_OFF);
}

// Deprecated. Use GlossyEnvironmentReflection(..., environmentReflections).
half3 GlossyEnvironmentReflection(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion, float2 normalizedScreenSpaceUV)
{
    return GlossyEnvironmentReflection(reflectVector, positionWS, perceptualRoughness, occlusion, normalizedScreenSpaceUV, !_ENVIRONMENTREFLECTIONS_OFF);
}

#if !USE_CLUSTER_LIGHT_LOOP
// Deprecated. Use GlossyEnvironmentReflection(..., environmentReflections).
half3 GlossyEnvironmentReflection(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion)
{
    return GlossyEnvironmentReflection(reflectVector, positionWS, perceptualRoughness, occlusion, float2(0.0f, 0.0f), !_ENVIRONMENTREFLECTIONS_OFF);
}
#endif

// Deprecated. Use GlossyEnvironmentReflection(..., environmentReflections).
half3 GlossyEnvironmentReflection(half3 reflectVector, half perceptualRoughness, half occlusion)
{
    return GlossyEnvironmentReflection(reflectVector, perceptualRoughness, occlusion, !_ENVIRONMENTREFLECTIONS_OFF);
}

void MixRealtimeAndBakedGI(inout Light light, half3 normalWS, inout URP_LIGHT_ACCUM3 bakedGI);

// Deprecated. Use MixRealtimeAndBakedGI(light, normalWS, bakedGI); the shadowMask argument was never used.
void MixRealtimeAndBakedGI(inout Light light, half3 normalWS, inout URP_LIGHT_ACCUM3 bakedGI, half4 shadowMask)
{
    MixRealtimeAndBakedGI(light, normalWS, bakedGI);
}

// Prevents leaking the fallback keyword definitions to shaders that include this file.
#if defined(_CLEARCOAT_DEFINED_LOCALLY)
    #undef _CLEARCOAT_DEFINED_LOCALLY
    #undef _CLEARCOAT
#endif

#if defined(_CLEARCOATMAP_DEFINED_LOCALLY)
    #undef _CLEARCOATMAP_DEFINED_LOCALLY
    #undef _CLEARCOATMAP
#endif

#if defined(_ENVIRONMENTREFLECTIONS_OFF_DEFINED_LOCALLY)
    #undef _ENVIRONMENTREFLECTIONS_OFF_DEFINED_LOCALLY
    #undef _ENVIRONMENTREFLECTIONS_OFF
#endif

#endif // UNIVERSAL_GLOBAL_ILLUMINATION_DEPRECATED_INCLUDED
